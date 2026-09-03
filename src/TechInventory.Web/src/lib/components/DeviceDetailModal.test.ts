/**
 * DeviceDetailModal — C-12 (detail-actions surface). The list-row surface
 * is covered by `deviceRowActions.svelte.test.ts` / `DevicePwaRow.test.ts`;
 * this component keeps its own inline copy of the same role rules (see its
 * docstring), so it's the second of the "two surfaces not yet asserted"
 * the coverage matrix calls out. Only the role-gated affordance set is
 * exercised here — not the modal's fetch/loading/retirement flows, which
 * are out of scope for this gap.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import type { CurrentUser } from '$lib/stores/auth';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const { getMock, listTagsMock, bulkUpdateMock } = vi.hoisted(() => ({
	getMock: vi.fn(),
	listTagsMock: vi.fn(),
	bulkUpdateMock: vi.fn()
}));

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		devices: { ...actual.devices, get: getMock, listTags: listTagsMock, bulkUpdate: bulkUpdateMock }
	};
});

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return { authStore: writable({ currentUser: null, isLoading: false, error: null }) };
});

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	const initialState: ReferenceDataState = {
		brands: [],
		categories: [],
		owners: [],
		locations: [],
		networks: [],
		tags: [],
		isLoading: false,
		error: null
	};
	return { referenceDataStore: writable(initialState) };
});

import { authStore } from '$lib/stores/auth';
import DeviceDetailModal from './DeviceDetailModal.svelte';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'owner-000000-4000-8000-000000000000', entraObjectId: null, displayName: 'Test User', role };
}

describe('DeviceDetailModal (C-12 role-gated detail actions)', () => {
	beforeEach(() => {
		resetFactories();
		getMock.mockReset();
		listTagsMock.mockReset().mockResolvedValue([]);
		bulkUpdateMock.mockReset();
	});

	it('hides the entire actions menu for a Viewer, even when they own the device', async () => {
		const device = createDeviceResponse();
		getMock.mockResolvedValue(device);
		authStore.set({
			currentUser: makeUser('Viewer'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });
		await screen.findByRole('heading', { level: 2 });

		// The Viewer owns the device fixture (see makeUser's id vs. the
		// factory default ownerId), but Viewer is read-only per constitution
		// §5.2 — with Edit/Delete/History/Claim/Release/Change Status all
		// denied, `DeviceActionsMenu` has zero actions to offer, so its
		// "More actions" trigger doesn't render at all (B1/B3 review fix; a
		// prior revision only checked the individual menu items and asserted
		// Release remained available for an owning Viewer).
		expect(screen.queryByRole('button', { name: /more actions/i })).not.toBeInTheDocument();
	});

	it('shows Edit/Delete/History for an Admin', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse();
		getMock.mockResolvedValue(device);
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });

		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);

		expect(screen.getByRole('menuitem', { name: 'Edit' })).toBeInTheDocument();
		expect(screen.getAllByRole('menuitem', { name: 'Delete' }).length).toBeGreaterThan(0);
		expect(screen.getByRole('menuitem', { name: 'View change history' })).toBeInTheDocument();
		// #131: Clone Device is gated the same way as Edit (canEdit) — Admin
		// gets both, pointing the clone action at the create-flow deep link.
		expect(screen.getByRole('menuitem', { name: 'Clone Device' })).toHaveAttribute(
			'href',
			`/devices/new?cloneFrom=${device.id}`
		);
		// Admin owns the device fixture (see makeUser's id vs. the factory
		// default ownerId) — Release Ownership must still be offered to a
		// mutating role, proving B3's role gate doesn't over-block Admin/Member.
		expect(screen.getByRole('menuitem', { name: 'Release Ownership' })).toBeInTheDocument();
	});

	it('has no accessibility violations for an Admin session', async () => {
		const device = createDeviceResponse();
		getMock.mockResolvedValue(device);
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		const { container } = render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });
		await screen.findByRole('button', { name: /more actions/i });

		expect(await axe(container)).toHaveNoViolations();
	});
});

describe('DeviceDetailModal — Change Status (#127, shared BulkUpdateModal reuse)', () => {
	beforeEach(() => {
		resetFactories();
		getMock.mockReset();
		listTagsMock.mockReset().mockResolvedValue([]);
		bulkUpdateMock.mockReset();
	});

	it('opens the shared change-status dialog preselected to the device\'s current status and applies a transition via devices.bulkUpdate', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse({ status: 'Active', name: 'Aqua Flosser' });
		getMock.mockResolvedValue(device);
		bulkUpdateMock.mockResolvedValue({ correlationId: 'corr-1', affectedCount: 1 });
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });

		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);
		await user.click(screen.getByRole('menuitem', { name: 'Change Status' }));

		const select = (await screen.findByLabelText(/change status/i)) as HTMLSelectElement;
		expect(select.value).toBe('Active');

		await user.selectOptions(select, 'Retired');
		await user.click(screen.getByRole('button', { name: /apply changes/i }));

		await waitFor(() => {
			expect(bulkUpdateMock).toHaveBeenCalledWith({
				deviceIds: [device.id],
				changes: { status: 'Retired' }
			});
		});
		await waitFor(() => {
			expect(screen.queryByLabelText(/change status/i)).not.toBeInTheDocument();
		});
	});

	it('closes the dialog without calling devices.bulkUpdate when cancelled', async () => {
		const user = userEvent.setup();
		const device = createDeviceResponse({ status: 'Active' });
		getMock.mockResolvedValue(device);
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });

		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);
		await user.click(screen.getByRole('menuitem', { name: 'Change Status' }));

		await screen.findByLabelText(/change status/i);
		await user.click(screen.getByRole('button', { name: /^cancel$/i }));

		expect(screen.queryByLabelText(/change status/i)).not.toBeInTheDocument();
		expect(bulkUpdateMock).not.toHaveBeenCalled();
	});
});

/**
 * /devices/[id]/edit — C-13 (client route guard refuses a Viewer session).
 * This is UX only, not the security boundary — the API still enforces the
 * same rule server-side. See the guard's inline comment in +page.svelte for
 * the cross-reference to the API-side coverage (H-04 in the coverage
 * matrix).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import type { CurrentUser } from '$lib/stores/auth';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const { goto, getMock, listTagsMock, updateMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	getMock: vi.fn(),
	listTagsMock: vi.fn(),
	updateMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto, beforeNavigate: vi.fn() }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/devices/device-1/edit'),
			params: { id: 'device-1' },
			route: { id: '/(authenticated)/devices/[id]/edit' },
			status: 200,
			error: null,
			data: {},
			form: null
		})
	};
});

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		devices: { ...actual.devices, get: getMock, listTags: listTagsMock, update: updateMock }
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
	return { referenceDataStore: writable(initialState), fetchReferenceData: vi.fn(async () => {}) };
});

import { authStore } from '$lib/stores/auth';
import Page from './+page.svelte';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

describe('/devices/[id]/edit (C-13 Viewer route guard)', () => {
	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		getMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1' }));
		listTagsMock.mockReset().mockResolvedValue([]);
		updateMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1' }));
	});

	it('redirects a Viewer session away from the edit form to the device detail page', async () => {
		authStore.set({
			currentUser: makeUser('Viewer'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);

		await vi.waitFor(() => expect(goto).toHaveBeenCalledWith('/devices/device-1'));
	});

	it('does not redirect an Admin session and renders the form', async () => {
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);

		await screen.findByLabelText(/^Name/);
		expect(goto).not.toHaveBeenCalled();
	});

	it('does not redirect a Member session and renders the form', async () => {
		authStore.set({
			currentUser: makeUser('Member'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);

		await screen.findByLabelText(/^Name/);
		expect(goto).not.toHaveBeenCalled();
	});
});

describe('/devices/[id]/edit — #148 edit form wrapper containment contract', () => {
	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The edit page wraps <DeviceForm> in a div. That wrapper must carry
	// overflow-x-hidden so narrow-viewport native inputs (iOS date, number)
	// cannot push the page wider than the viewport — mirroring the
	// AddDeviceModal scroll-body fix from #148.
	beforeEach(() => {
		resetFactories();
		getMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1' }));
		listTagsMock.mockReset().mockResolvedValue([]);
		authStore.set({
			currentUser: { id: 'u1', role: 'Admin', displayName: 'T', entraObjectId: null },
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});
	});

	it('form wrapper carries overflow-x-hidden to contain native input overflow on iOS', async () => {
		const { container } = render(Page);
		await screen.findByLabelText(/^Name/);

		// The form wrapper is the only non-loading, non-breadcrumb div that
		// wraps DeviceForm; it carries overflow-x-hidden + rounded-lg + border.
		const wrapper = container.querySelector('[class*="overflow-x-hidden"][class*="rounded-lg"]');
		expect(
			wrapper,
			'edit page form wrapper must exist with overflow-x-hidden and rounded-lg'
		).toBeTruthy();
		expect(
			wrapper!.className,
			'edit page form wrapper must carry overflow-x-hidden to match AddDeviceModal #148 fix'
		).toMatch(/\boverflow-x-hidden\b/);
	});
});

describe('/devices/[id]/edit — status preservation on submit (#133)', () => {
	// Valid RFC4122-shaped UUIDs so the zod deviceFormSchema (which validates
	// categoryId/ownerId/locationId as `.uuid()`) accepts the pre-populated
	// values and the form can actually reach onSubmit.
	const categoryId = '00000000-0000-4000-8000-000000000201';
	const ownerId = '00000000-0000-4000-8000-000000000401';
	const locationId = '00000000-0000-4000-8000-000000000501';

	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		listTagsMock.mockReset().mockResolvedValue([]);
		updateMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1' }));

		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});
	});

	it.each(['InRepair', 'Lent'] as const)(
		'forwards the current status (%s) on an ordinary edit instead of letting it silently reset to Active',
		async (status) => {
			const user = userEvent.setup();
			getMock.mockReset().mockResolvedValue(
				createDeviceResponse({
					id: 'device-1',
					categoryId,
					ownerId,
					locationId,
					brandId: null,
					networkId: null,
					status
				})
			);

			render(Page);

			const nameInput = await screen.findByLabelText(/^Name/);
			await user.clear(nameInput);
			await user.type(nameInput, 'Renamed Device');

			const saveButton = screen.getByRole('button', { name: /save/i });
			await waitFor(() => expect(saveButton).toBeEnabled());
			await user.click(saveButton);

			await waitFor(() => expect(updateMock).toHaveBeenCalled());
			const [, payload] = updateMock.mock.calls[0];
			expect(payload.status).toBe(status);
		}
	);

	it('preserves retiredDate/disposalMethod on a Retired device so a notes-only edit does not throw the "read-only" conflict', async () => {
		// Regression: the API's "retired devices are read-only" guard does a
		// strict equality check on retiredDate/disposalMethod against the
		// stored values for a device that stays Retired. Omitting them (as the
		// form did before #133) makes even an unrelated edit fail once status
		// is correctly forwarded instead of resetting to Active.
		const user = userEvent.setup();
		getMock.mockReset().mockResolvedValue(
			createDeviceResponse({
				id: 'device-1',
				categoryId,
				ownerId,
				locationId,
				brandId: null,
				networkId: null,
				status: 'Retired',
				retiredDate: '2026-01-15',
				disposalMethod: 'Donated',
				notes: 'Old note'
			})
		);

		render(Page);

		const notesInput = await screen.findByLabelText(/notes/i);
		await user.type(notesInput, ' updated');

		const saveButton = screen.getByRole('button', { name: /save/i });
		await waitFor(() => expect(saveButton).toBeEnabled());
		await user.click(saveButton);

		await waitFor(() => expect(updateMock).toHaveBeenCalled());
		const [, payload] = updateMock.mock.calls[0];
		expect(payload.status).toBe('Retired');
		expect(payload.retiredDate).toBe('2026-01-15');
		expect(payload.disposalMethod).toBe('Donated');
	});

	it('allows changing status away from Retired via the Status control and submits the newly selected status', async () => {
		const user = userEvent.setup();
		getMock.mockReset().mockResolvedValue(
			createDeviceResponse({
				id: 'device-1',
				categoryId,
				ownerId,
				locationId,
				brandId: null,
				networkId: null,
				status: 'Retired',
				retiredDate: '2026-01-15',
				disposalMethod: null
			})
		);

		render(Page);

		const statusSelect = await screen.findByLabelText(/^Status/);
		await user.selectOptions(statusSelect, 'Active');

		const saveButton = screen.getByRole('button', { name: /save/i });
		await waitFor(() => expect(saveButton).toBeEnabled());
		await user.click(saveButton);

		await waitFor(() => expect(updateMock).toHaveBeenCalled());
		const [, payload] = updateMock.mock.calls[0];
		expect(payload.status).toBe('Active');
	});
});

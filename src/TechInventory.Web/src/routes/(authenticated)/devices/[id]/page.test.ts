/**
 * /devices/[id] — C-18 (route-level axe harness) and C-04 (role-gated
 * claim/release affordances on the direct detail route). This route keeps
 * its own inline `canClaim`/`canRelease` derivation independent of
 * `DeviceDetailModal.svelte` and `deviceRowActions.svelte.ts` (see the
 * B3 review finding), so it needs its own Viewer-negative /
 * Admin-and-Member-positive assertions rather than relying on the other two
 * surfaces' tests. Uses the same `devices.get`/`listTags` +
 * auth/referenceData mocking pattern already proven in
 * `DeviceDetailModal.test.ts` and the edit-route guard test (C-13).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import type { CurrentUser } from '$lib/stores/auth';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const { goto, getMock, listTagsMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	getMock: vi.fn(),
	listTagsMock: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));

vi.mock('$app/stores', async () => {
	const { readable } = await vi.importActual<typeof import('svelte/store')>('svelte/store');
	return {
		page: readable({
			url: new URL('http://localhost/devices/device-1'),
			params: { id: 'device-1' },
			route: { id: '/(authenticated)/devices/[id]' },
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
		devices: { ...actual.devices, get: getMock, listTags: listTagsMock }
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

describe('/devices/[id] (C-18 axe)', () => {
	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		getMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1', name: 'Living Room TV' }));
		listTagsMock.mockReset().mockResolvedValue([]);
		authStore.set({
			currentUser: makeUser('Admin'),
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});
	});

	it('renders the loaded device detail view', async () => {
		render(Page);

		expect(await screen.findByRole('heading', { name: 'Living Room TV' })).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(Page);
		await screen.findByRole('heading', { name: 'Living Room TV' });

		expect(await axe(container)).toHaveNoViolations();
	});
});

describe('/devices/[id] (B3 role-gated claim/release actions)', () => {
	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		listTagsMock.mockReset().mockResolvedValue([]);
	});

	it('hides the entire actions menu for a Viewer, even when they own the device', async () => {
		const viewer = makeUser('Viewer');
		getMock
			.mockReset()
			.mockResolvedValue(createDeviceResponse({ id: 'device-1', name: 'Living Room TV', ownerId: viewer.id }));
		authStore.set({
			currentUser: viewer,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);
		await screen.findByRole('heading', { name: 'Living Room TV' });

		// Viewer is read-only per constitution §5.2 — with Edit/Delete/History/
		// Claim/Release/Change Status all denied, `DeviceActionsMenu` has zero
		// actions to offer, so its "More actions" trigger doesn't render at all,
		// even though this Viewer owns the device fixture (ownerId set above).
		expect(screen.queryByRole('button', { name: /more actions/i })).not.toBeInTheDocument();
	});

	it('offers Release Ownership to an Admin who owns the device', async () => {
		const user = userEvent.setup();
		const admin = makeUser('Admin');
		getMock
			.mockReset()
			.mockResolvedValue(createDeviceResponse({ id: 'device-1', name: 'Living Room TV', ownerId: admin.id }));
		authStore.set({
			currentUser: admin,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);
		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);

		expect(screen.getByRole('menuitem', { name: 'Release Ownership' })).toBeInTheDocument();
	});

	it('offers Claim Ownership to a Member who does not own the device', async () => {
		const user = userEvent.setup();
		const member = makeUser('Member');
		getMock
			.mockReset()
			.mockResolvedValue(
				createDeviceResponse({ id: 'device-1', name: 'Living Room TV', ownerId: 'someone-else' })
			);
		authStore.set({
			currentUser: member,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);
		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);

		expect(screen.getByRole('menuitem', { name: 'Claim Ownership' })).toBeInTheDocument();
	});

	it('offers Change Status to an Admin regardless of device ownership (#127)', async () => {
		const user = userEvent.setup();
		const admin = makeUser('Admin');
		getMock
			.mockReset()
			.mockResolvedValue(
				createDeviceResponse({ id: 'device-1', name: 'Living Room TV', ownerId: 'someone-else' })
			);
		authStore.set({
			currentUser: admin,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		render(Page);
		const menuButton = await screen.findByRole('button', { name: /more actions/i });
		await user.click(menuButton);

		expect(screen.getByRole('menuitem', { name: 'Change Status' })).toBeInTheDocument();
	});
});

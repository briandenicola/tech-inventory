/**
 * /devices/[id]/edit — C-13 (client route guard refuses a Viewer session).
 * This is UX only, not the security boundary — the API still enforces the
 * same rule server-side. See the guard's inline comment in +page.svelte for
 * the cross-reference to the API-side coverage (H-04 in the coverage
 * matrix).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import type { CurrentUser } from '$lib/stores/auth';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const { goto, getMock, listTagsMock } = vi.hoisted(() => ({
	goto: vi.fn(),
	getMock: vi.fn(),
	listTagsMock: vi.fn()
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

describe('/devices/[id]/edit (C-13 Viewer route guard)', () => {
	beforeEach(() => {
		resetFactories();
		goto.mockReset();
		getMock.mockReset().mockResolvedValue(createDeviceResponse({ id: 'device-1' }));
		listTagsMock.mockReset().mockResolvedValue([]);
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

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { get } from 'svelte/store';
import { authStore, clearAuth, fetchCurrentUser, updateCurrentUserDisplayName } from './auth';

const { ownersMe } = vi.hoisted(() => ({ ownersMe: vi.fn() }));

vi.mock('$lib/api/client', () => ({
	owners: { me: ownersMe }
}));

describe('fetchCurrentUser denial UI (C-03)', () => {
	beforeEach(() => {
		ownersMe.mockReset();
	});

	it('sets auth.unauthorized on a 401 response', async () => {
		ownersMe.mockRejectedValue({ status: 401, message: 'Unauthorized' });

		await fetchCurrentUser();

		const state = get(authStore);
		expect(state.isAuthenticated).toBe(false);
		expect(state.currentUser).toBeNull();
		expect(state.error).toBe('You must be signed in to access this page.');
	});

	it('sets auth.accessDenied on a 403 response', async () => {
		ownersMe.mockRejectedValue({ status: 403, message: 'Forbidden' });

		await fetchCurrentUser();

		const state = get(authStore);
		expect(state.isAuthenticated).toBe(false);
		expect(state.error).toBe('Access denied. You do not have permission to view this page.');
	});

	it('falls back to a generic message for non-401/403 failures', async () => {
		ownersMe.mockRejectedValue(new Error('network down'));

		await fetchCurrentUser();

		const state = get(authStore);
		expect(state.error).toBe('Failed to load your household profile.');
	});

	it('uses a user-facing message when the profile endpoint is unavailable', async () => {
		ownersMe.mockRejectedValue(new Error('404 Not Found'));

		await fetchCurrentUser();

		const state = get(authStore);
		expect(state.error).toBe('Your household profile is not available yet.');
	});
});

describe('clearAuth', () => {
	const STORAGE_KEY_TOKEN = 'ti_local_token';
	const STORAGE_KEY_META = 'ti_local_meta';

	beforeEach(() => {
		sessionStorage.clear();
	});

	it('resets the auth store to a signed-out state', () => {
		authStore.set({
			currentUser: {
				id: 'owner-1',
				entraObjectId: '11111111-1111-1111-1111-111111111111',
				displayName: 'Alice',
				role: 'Admin'
			},
			isAuthenticated: true,
			isLoading: false,
			error: 'stale error',
			authMethod: 'entra',
			mustChangePassword: true
		});

		clearAuth();

		expect(get(authStore)).toEqual({
			currentUser: null,
			isAuthenticated: false,
			isLoading: false,
			error: null,
			authMethod: null,
			mustChangePassword: false
		});
	});

	it('clears the local-session sessionStorage keys (F025 clearLocalSession)', () => {
		sessionStorage.setItem(STORAGE_KEY_TOKEN, 'fake.jwt.token');
		sessionStorage.setItem(
			STORAGE_KEY_META,
			JSON.stringify({
				subjectId: 'local-1',
				displayName: 'Local User',
				role: 'Viewer',
				mustChangePassword: false,
				expiresAtUtc: Math.floor(Date.now() / 1000) + 3600
			})
		);

		clearAuth();

		expect(sessionStorage.getItem(STORAGE_KEY_TOKEN)).toBeNull();
		expect(sessionStorage.getItem(STORAGE_KEY_META)).toBeNull();
	});

	it('is safe to call when no local session or user was ever set', () => {
		expect(() => clearAuth()).not.toThrow();
		expect(get(authStore).isAuthenticated).toBe(false);
	});
});

describe('updateCurrentUserDisplayName', () => {
	beforeEach(() => {
		clearAuth();
	});

	it('is a no-op when no current user is loaded', () => {
		updateCurrentUserDisplayName('Anything');
		expect(get(authStore).currentUser).toBeNull();
	});

	it('updates only the displayName field on the existing user', () => {
		authStore.set({
			currentUser: {
				id: 'owner-1',
				entraObjectId: '11111111-1111-1111-1111-111111111111',
				displayName: 'Old Name',
				role: 'Admin'
			},
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		updateCurrentUserDisplayName('New Name');

		const state = get(authStore);
		expect(state.currentUser).toEqual({
			id: 'owner-1',
			entraObjectId: '11111111-1111-1111-1111-111111111111',
			displayName: 'New Name',
			role: 'Admin'
		});
		expect(state.isAuthenticated).toBe(true);
	});
});

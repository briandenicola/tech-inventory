import { describe, it, expect, beforeEach } from 'vitest';
import { get } from 'svelte/store';
import { authStore, clearAuth, updateCurrentUserDisplayName } from './auth';

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
			error: null
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

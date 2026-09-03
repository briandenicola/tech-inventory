/**
 * (authenticated)/+layout.ts — Protected route guard tests (C-01)
 *
 * Per specs/002-frontend-mvp/spec.md §5, J13: unauthenticated users are
 * redirected to /auth/login; a still-loading auth state renders through
 * (no redirect) so the layout can show its own loading UI; an authenticated
 * user passes through untouched.
 */
import { describe, it, expect, beforeEach } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import { authStore } from '$lib/stores/auth';
import { load } from './+layout';

type LoadEvent = Parameters<typeof load>[0];

function callLoad() {
	return load({} as LoadEvent);
}

describe('(authenticated)/+layout.ts guard', () => {
	beforeEach(() => {
		authStore.set({
			currentUser: null,
			isAuthenticated: false,
			isLoading: true,
			error: null,
			authMethod: null,
			mustChangePassword: false
		});
	});

	it('redirects an unauthenticated, finished-loading session to /auth/login', async () => {
		authStore.set({
			currentUser: null,
			isAuthenticated: false,
			isLoading: false,
			error: 'Failed to load user profile',
			authMethod: null,
			mustChangePassword: false
		});

		await expect(callLoad()).rejects.toSatisfy((thrown: unknown) => {
			expect(isRedirect(thrown)).toBe(true);
			expect((thrown as { status: number; location: string }).status).toBe(302);
			expect((thrown as { status: number; location: string }).location).toBe('/auth/login');
			return true;
		});
	});

	it('does not redirect while the auth store is still loading (lets the layout show its own loading UI)', async () => {
		authStore.set({
			currentUser: null,
			isAuthenticated: false,
			isLoading: true,
			error: null,
			authMethod: null,
			mustChangePassword: false
		});

		await expect(callLoad()).resolves.toEqual({});
	});

	it('allows an authenticated user through without redirecting', async () => {
		authStore.set({
			currentUser: {
				id: 'owner-1',
				entraObjectId: null,
				displayName: 'Alice',
				role: 'Member'
			},
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		await expect(callLoad()).resolves.toEqual({});
	});

	it('redirects when isAuthenticated is true but currentUser is missing (defensive: state can only mean one user)', async () => {
		authStore.set({
			currentUser: null,
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		});

		await expect(callLoad()).rejects.toSatisfy((thrown: unknown) => {
			expect(isRedirect(thrown)).toBe(true);
			return true;
		});
	});
});

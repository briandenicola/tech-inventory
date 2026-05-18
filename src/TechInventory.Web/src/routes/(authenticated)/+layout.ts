/**
 * T12: Protected Route Guard — (authenticated) route group
 * 
 * Per Spec §5, J13: Unauthenticated users → redirect to /auth/login
 * Role-based guards at deeper levels (e.g., (authenticated)/admin/+layout.ts)
 * 
 * Related: specs/002-frontend-mvp/spec.md §5, J13
 */

import { redirect } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { authStore } from '$lib/stores/auth';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async () => {
const auth = get(authStore);

// Wait for auth store to finish loading (T10 fetches /owners/me on app mount)
// If still loading, allow render (layout will show loading state)
if (auth.isLoading) {
return {};
}

// If not authenticated, redirect to login
if (!auth.isAuthenticated || !auth.currentUser) {
throw redirect(302, '/auth/login');
}

// Authenticated — allow access
return {};
};

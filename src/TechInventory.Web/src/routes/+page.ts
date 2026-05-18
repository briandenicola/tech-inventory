/**
 * Home page — redirect to auth or devices
 * Per T09, J1: Unauthenticated users → /auth/login, authenticated → /devices
 */

import { redirect } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { authStore } from '$lib/stores/auth';
import type { PageLoad } from './$types';

export const load: PageLoad = async () => {
	const auth = get(authStore);

	// If authenticated, go to devices
	if (auth.isAuthenticated && auth.currentUser) {
		throw redirect(302, '/devices');
	}

	// Otherwise, go to login
	throw redirect(302, '/auth/login');
};

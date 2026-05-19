/**
 * Home page — redirect to auth or devices
 * Per T09, J1: Unauthenticated users → /auth/login, authenticated → /devices
 *
 * IMPORTANT: this load runs before +layout.svelte's onMount, so it must
 * NOT redirect away from `/` when either (a) MSAL's redirect flow is in
 * flight (the URL fragment carries the auth code from Entra) or (b) the
 * auth store hasn't finished hydrating yet. Either case would strip the
 * fragment and trap the user at /auth/login (see prod sign-in loop,
 * 2026-05-19).
 */

import { browser } from '$app/environment';
import { redirect } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { authStore } from '$lib/stores/auth';
import type { PageLoad } from './$types';

export const load: PageLoad = async () => {
	if (browser) {
		// Entra returns with `code=...` (or `error=...`) in the URL fragment.
		// `response_mode=fragment` is MSAL's default for SPAs. Bail so the
		// root +layout.svelte can call handleRedirectPromise() and navigate
		// us to /devices itself.
		const hash = window.location.hash.slice(1);
		if (/(?:^|&)(code|id_token|error)=/.test(hash)) {
			return {};
		}
	}

	const auth = get(authStore);

	// Auth store starts with isLoading=true; the root layout flips it to
	// false once MSAL bootstrap + /owners/me settle. Redirecting before
	// then races the bootstrap and loses the fragment on a fresh visit.
	if (auth.isLoading) {
		return {};
	}

	// If authenticated, go to devices
	if (auth.isAuthenticated && auth.currentUser) {
		throw redirect(302, '/devices');
	}

	// Otherwise, go to login
	throw redirect(302, '/auth/login');
};

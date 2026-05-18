<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { initializeMsal, handleRedirectPromise, getActiveAccount } from '$lib/auth';
	import { fetchCurrentUser, authStore } from '$lib/stores/auth';
	import '$lib/api'; // Wire MSAL token provider into API client
	import '../app.css';
	import { t } from '$lib/i18n';

	let { children } = $props();

	// T05 + T10: Bootstrap MSAL.js + populate auth store
	// - initializeMsal() must be called before any other MSAL operation (v3+ requirement)
	// - handleRedirectPromise() processes auth code from Entra redirect (if user just signed in)
	// - If MSAL has active account, fetch /api/v1/owners/me to populate auth store (T10)
	onMount(async () => {
		try {
			await initializeMsal();
			const authResult = await handleRedirectPromise();

			// If user just signed in via redirect, navigate to devices list
			if (authResult && authResult.account) {
				console.log('[auth] Sign-in successful, redirecting to /devices');
				await fetchCurrentUser(); // Populate auth store
				goto('/devices');
			} else {
				// Check if user is already authenticated (page reload, direct navigation)
				const account = getActiveAccount();
				if (account) {
					// User has active MSAL session; fetch current user from API
					await fetchCurrentUser();
				} else {
					// No active account — mark auth store as not loading
					authStore.set({
						currentUser: null,
						isAuthenticated: false,
						isLoading: false,
						error: null
					});
				}
			}
		} catch (error) {
			console.error('[auth] MSAL bootstrap failed:', error);
			// Don't block app render — auth errors will surface when user tries to access protected routes
			authStore.set({
				currentUser: null,
				isAuthenticated: false,
				isLoading: false,
				error: error instanceof Error ? error.message : 'Auth bootstrap failed'
			});
		}
	});
</script>

<a href="#main-content" class="skip-link">{t('app.nav.skipToContent')}</a>

<main id="main-content">
	{@render children()}
</main>

<style>
	.skip-link {
		position: absolute;
		left: -9999px;
		top: 0;
		z-index: 10000;
	}
	.skip-link:focus {
		left: 0;
	}
</style>

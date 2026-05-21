<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { initializeMsal, handleRedirectPromise, tryAcquireApiTokenSilent } from '$lib/auth';
	import { fetchCurrentUser, authStore, hydrateLocalSession } from '$lib/stores/auth';
	import { initializeTheme } from '$lib/stores/theme.svelte';
	import '$lib/api'; // Wire MSAL token provider into API client
	import '../app.css';
	import { t } from '$lib/i18n';
	import PwaUpdatePrompt from '$lib/components/PwaUpdatePrompt.svelte';

	const SILENT_SSO_TIMEOUT_MS = 3000;
	let { children } = $props();

	function setUnauthenticatedState(error: string | null = null) {
		authStore.set({
			currentUser: null,
			isAuthenticated: false,
			isLoading: false,
			error,
			authMethod: null,
			mustChangePassword: false
		});
	}

	function shouldRedirectToDevices(pathname: string) {
		return pathname === '/' || pathname === '/auth/login';
	}

	// T05 + T10: Bootstrap MSAL.js + populate auth store.
	// F025 local-account sessions hydrate first so the break-glass flow stays in
	// sessionStorage only. For Entra sessions, attempt a silent token refresh
	// before revealing the login page; if that fails with interaction_required,
	// we fall back to the existing login button instead of flashing the page.
	onMount(async () => {
		initializeTheme();

		try {
			if (hydrateLocalSession()) {
				return;
			}

			await initializeMsal();
			const authResult = await handleRedirectPromise();
			const silentResult =
				authResult ?? (await tryAcquireApiTokenSilent({ timeoutMs: SILENT_SSO_TIMEOUT_MS }));

			if (silentResult?.account) {
				await fetchCurrentUser();
				const authState = $authStore;
				if (
					authState.isAuthenticated &&
					authState.currentUser &&
					shouldRedirectToDevices(window.location.pathname)
				) {
					await goto('/devices');
				}
				return;
			}

			setUnauthenticatedState();
		} catch (error) {
			console.error('[auth] MSAL bootstrap failed:', error);
			// Don't block app render — auth errors will surface when user tries to access protected routes
			setUnauthenticatedState(error instanceof Error ? error.message : 'Auth bootstrap failed');
		}
	});

	// F025 — force the change-password flow for any local-auth session that
	// still has `mustChangePassword=true`. Runs reactively on every store +
	// route change so a refresh, deep-link, or navigation all funnel through.
	$effect(() => {
		const state = $authStore;
		if (!state.isAuthenticated || state.authMethod !== 'local' || !state.mustChangePassword) {
			return;
		}
		const path = $page.url.pathname;
		if (path !== '/auth/change-password') {
			goto('/auth/change-password');
		}
	});
</script>

<a href="#main-content" class="skip-link">{t('app.nav.skipToContent')}</a>

<main id="main-content">
	{@render children()}
</main>

<PwaUpdatePrompt />

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

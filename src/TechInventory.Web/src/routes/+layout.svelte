<script lang="ts">
	import { onMount } from 'svelte';
	import { initializeMsal, handleRedirectPromise } from '$lib/auth';
	import '$lib/api'; // Wire MSAL token provider into API client
	import '../app.css';
	import { t } from '$lib/i18n';

	let { children } = $props();

	// T05: Bootstrap MSAL.js at app startup
	// - initializeMsal() must be called before any other MSAL operation (v3+ requirement)
	// - handleRedirectPromise() processes auth code from Entra redirect (if user just signed in)
	onMount(async () => {
		try {
			await initializeMsal();
			await handleRedirectPromise();
		} catch (error) {
			console.error('[auth] MSAL bootstrap failed:', error);
			// Don't block app render — auth errors will surface when user tries to access protected routes
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

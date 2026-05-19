<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';
	import { t } from '$lib/i18n';

	// Once the root layout's MSAL bootstrap settles authStore (isLoading flips
	// to false), route the user to /devices or /auth/login. We can't do this
	// in +page.ts because that runs before the layout's handleRedirectPromise
	// — see comment in +page.ts. The static greeting below is only visible
	// for the brief moment between mount and authStore resolution.
	$effect(() => {
		const state = $authStore;
		if (state.isLoading) return;
		if (state.isAuthenticated && state.currentUser) {
			goto('/devices');
		} else {
			goto('/auth/login');
		}
	});
</script>

<div class="container">
	<h1 class="title">{t('app.title')}</h1>
	<p class="tagline">{t('app.tagline')}</p>
</div>

<style>
	.container {
		padding: var(--space-2xl);
		text-align: center;
	}
	
	.title {
		font-size: var(--text-3xl);
		color: var(--color-text);
		margin-bottom: var(--space-md);
	}
	
	.tagline {
		font-size: var(--text-lg);
		color: var(--color-text-secondary);
	}
</style>

<script lang="ts">
	import { goto } from '$app/navigation';
	import {
		clearAutoInteractiveSignInSuppression,
		clearSilentSsoSuppression,
		shouldAutoStartInteractiveSignIn
	} from '$lib/auth';
	import { msalInstance, loginRequest, ensureMsalInitialized } from '$lib/auth/msal';
	import { authStore } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import LocalLoginForm from '$lib/components/LocalLoginForm.svelte';

	// Silent Entra restoration runs in the root layout. This page stays on a
	// splash state until that check finishes so returning users do not see a
	// login flash before we can route them back into the app.
	// No LoadingSkeleton here — there's no list/table content to skeleton;
	// the delayed spinner below (showLoadingSpinner) is the right affordance
	// for a splash screen.
	let isRedirecting = $state(false);
	let showLocalForm = $state(false);
	let showLoadingSpinner = $state(false);
	let attemptedAutoSignIn = $state(false);

	$effect(() => {
		const state = $authStore;
		if (!state.isLoading && state.isAuthenticated && state.currentUser) {
			goto('/devices');
		}
	});

	$effect(() => {
		if (!$authStore.isLoading) {
			showLoadingSpinner = false;
			return;
		}

		const timer = window.setTimeout(() => {
			showLoadingSpinner = true;
		}, 600);

		return () => {
			window.clearTimeout(timer);
			showLoadingSpinner = false;
		};
	});

	$effect(() => {
		const state = $authStore;
		if (
			attemptedAutoSignIn ||
			state.isLoading ||
			state.isAuthenticated ||
			isRedirecting ||
			showLocalForm
		) {
			return;
		}

		clearAutoInteractiveSignInSuppression();
		if (!shouldAutoStartInteractiveSignIn()) {
			return;
		}

		attemptedAutoSignIn = true;
		void handleSignIn();
	});

	async function handleSignIn() {
		try {
			isRedirecting = true;
			// Clear any prior redirect suppression before trying Entra so an
			// interrupted PWA auth attempt can be retried on the next launch
			// instead of leaving the user stuck on the sign-in button.
			clearSilentSsoSuppression();
			clearAutoInteractiveSignInSuppression();
			await ensureMsalInitialized();
			// Per T09 DoD + J1: Call loginRedirect with API scope + OIDC scopes.
			// PKCE is maintained by MSAL's redirect flow.
			await msalInstance.loginRedirect(loginRequest);
		} catch (error) {
			console.error('[auth/login] Sign-in failed:', error);
			isRedirecting = false;
			// TODO: Show error toast (T43 toast system)
		}
	}
</script>

<div class="flex min-h-screen items-center justify-center bg-neutral-50 px-4 dark:bg-neutral-900">
	<div class="w-full max-w-md space-y-8">
		<!-- Logo + Title -->
		<div class="text-center">
			<img src="/icons/icon.svg" alt={t('app.title')} class="mx-auto h-24 w-24" />
			<h1 class="mt-6 text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('auth.signIn.title')}
			</h1>
			<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
				{t('auth.signIn.description')}
			</p>
		</div>

		{#if $authStore.isLoading}
			<div
				class="mt-8 rounded-xl border border-neutral-200 bg-white p-6 text-center shadow-sm dark:border-neutral-800 dark:bg-neutral-950/40"
			>
				<p class="text-base font-semibold text-neutral-900 dark:text-neutral-100">
					{t('auth.signIn.checkingTitle')}
				</p>
				<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
					{t('auth.signIn.checkingDescription')}
				</p>
				{#if showLoadingSpinner}
					<div
						class="mt-4 flex items-center justify-center gap-2 text-sm text-neutral-500 dark:text-neutral-400"
						role="status"
						aria-live="polite"
					>
						<svg
							class="h-4 w-4 animate-spin"
							xmlns="http://www.w3.org/2000/svg"
							fill="none"
							viewBox="0 0 24 24"
							aria-hidden="true"
						>
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
						</svg>
						<span>{t('common.states.loading')}</span>
					</div>
				{/if}
			</div>
		{:else if showLocalForm}
			<div
				class="rounded-xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950/40"
			>
				<LocalLoginForm onCancel={() => (showLocalForm = false)} />
			</div>
		{:else}
			<!-- Sign In Button -->
			<div class="mt-8 space-y-3">
				<button
					type="button"
					class="flex w-full items-center justify-center rounded-lg bg-primary-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 disabled:cursor-not-allowed disabled:opacity-50"
					disabled={isRedirecting}
					onclick={() => handleSignIn()}
				>
					{#if isRedirecting}
						<svg
							class="mr-2 h-4 w-4 animate-spin"
							xmlns="http://www.w3.org/2000/svg"
							fill="none"
							viewBox="0 0 24 24"
						>
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
						</svg>
						{t('common.states.loading')}
					{:else}
						{t('auth.signIn.button')}
					{/if}
				</button>
				<button
					type="button"
					class="block w-full text-center text-xs font-medium text-primary-700 hover:underline dark:text-primary-300"
					onclick={() => (showLocalForm = true)}
				>
					{t('auth.local.useLocalAccount')}
				</button>
			</div>
		{/if}

		<!-- Footer -->
		<p class="mt-8 text-center text-xs text-neutral-500 dark:text-neutral-400">
			{t('app.tagline')}
		</p>
	</div>
</div>

<style>
	/* Use design tokens for animations */
	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}
	.animate-spin {
		animation: spin 1s linear infinite;
	}
</style>

<script lang="ts">
import { onMount } from 'svelte';
import { goto } from '$app/navigation';
import { msalInstance, loginRequest, ensureMsalInitialized } from '$lib/auth/msal';
import { getActiveAccount } from '$lib/auth';
import { t } from '$lib/i18n';

// T09: Login page — redirect to Entra via MSAL loginRedirect
// Per J1: Click "Sign In" → redirect to Entra → auth → callback → sessionStorage JWT

let isRedirecting = false;

// If user is already signed in, redirect to devices list
onMount(async () => {
await ensureMsalInitialized();
const account = getActiveAccount();
if (account) {
goto('/devices');
}
});

async function handleSignIn() {
try {
isRedirecting = true;
await ensureMsalInitialized();
// Per T09 DoD + J1: Call loginRedirect with API scope + OIDC scopes
// This will redirect the user to Entra; they'll return via handleRedirectPromise in +layout.svelte
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
<img
src="/icons/icon.svg"
alt={t('app.title')}
class="mx-auto h-24 w-24"
/>
<h1 class="mt-6 text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
{t('auth.signIn.title')}
</h1>
<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
{t('auth.signIn.description')}
</p>
</div>

<!-- Sign In Button -->
<div class="mt-8">
<button
type="button"
class="flex w-full items-center justify-center rounded-lg bg-primary-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 disabled:cursor-not-allowed disabled:opacity-50"
disabled={isRedirecting}
onclick={handleSignIn}
>
{#if isRedirecting}
<svg class="mr-2 h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
</svg>
{t('common.states.loading')}
{:else}
{t('auth.signIn.button')}
{/if}
</button>
</div>

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

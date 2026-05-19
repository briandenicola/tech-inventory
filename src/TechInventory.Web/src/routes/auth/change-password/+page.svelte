<script lang="ts">
	/**
	 * F025 — forced password rotation for local-account sign-ins.
	 *
	 * Reachable two ways:
	 * 1. Auto-redirect from the root layout when the active local session has
	 *    `mustChangePassword=true`.
	 * 2. Deep-link from a user voluntarily rotating their break-glass password
	 *    (rare; UI for that lives in a future F025b iteration).
	 *
	 * On success we drop the session and bounce back to /auth/login so the user
	 * signs in again with the new password.
	 */
	import { goto } from '$app/navigation';
	import { localSignOut } from '$lib/stores/auth';
	import { localAuth } from '$lib/api/client';
	import { t } from '$lib/i18n';

	let currentPassword = $state('');
	let newPassword = $state('');
	let confirmPassword = $state('');
	let isSubmitting = $state(false);
	let errorKey = $state<string | null>(null);
	let successKey = $state<string | null>(null);

	const MIN_LENGTH = 12;

	function validate(): string | null {
		if (newPassword.length < MIN_LENGTH) {
			return 'auth.local.changePassword.minLength';
		}
		if (newPassword === currentPassword) {
			return 'auth.local.changePassword.mustDifferFromCurrent';
		}
		if (newPassword !== confirmPassword) {
			return 'auth.local.changePassword.mustMatch';
		}
		return null;
	}

	async function handleSubmit(event: Event) {
		event.preventDefault();
		if (isSubmitting) return;

		errorKey = validate();
		if (errorKey) return;

		isSubmitting = true;
		successKey = null;
		try {
			await localAuth.changePassword({ currentPassword, newPassword });
			successKey = 'auth.local.changePassword.success';
			// Force the user to sign in again with the fresh password; this is
			// simpler than refreshing tokens client-side and gives us a clean
			// audit trail.
			localSignOut();
			setTimeout(() => goto('/auth/login'), 1500);
		} catch (error) {
			const isAuthFailure =
				error instanceof Error &&
				(error.message.includes('401') || error.message.toLowerCase().includes('unauthorized'));
			errorKey = isAuthFailure
				? 'auth.local.invalidCredentials'
				: 'auth.local.unknownError';
			console.error('[auth/local] change-password failed:', error);
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="flex min-h-screen items-center justify-center bg-neutral-50 px-4 dark:bg-neutral-900">
	<div class="w-full max-w-md space-y-8">
		<div class="text-center">
			<h1 class="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('auth.local.changePassword.title')}
			</h1>
			<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
				{t('auth.local.changePassword.description')}
			</p>
		</div>

		<form
			class="space-y-4 rounded-xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950/40"
			onsubmit={handleSubmit}
		>
			<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
				{t('auth.local.changePassword.currentLabel')}
				<input
					type="password"
					autocomplete="current-password"
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-50"
					bind:value={currentPassword}
					required
					disabled={isSubmitting}
				/>
			</label>

			<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
				{t('auth.local.changePassword.newLabel')}
				<input
					type="password"
					autocomplete="new-password"
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-50"
					bind:value={newPassword}
					required
					minlength={MIN_LENGTH}
					maxlength={256}
					disabled={isSubmitting}
				/>
			</label>

			<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
				{t('auth.local.changePassword.confirmLabel')}
				<input
					type="password"
					autocomplete="new-password"
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-50"
					bind:value={confirmPassword}
					required
					minlength={MIN_LENGTH}
					maxlength={256}
					disabled={isSubmitting}
				/>
			</label>

			{#if errorKey}
				<p
					role="alert"
					class="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-200"
				>
					{t(errorKey)}
				</p>
			{/if}

			{#if successKey}
				<p
					role="status"
					class="rounded-md border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-800 dark:border-green-900/50 dark:bg-green-950/30 dark:text-green-200"
				>
					{t(successKey)}
				</p>
			{/if}

			<button
				type="submit"
				class="flex w-full items-center justify-center rounded-lg bg-primary-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60"
				disabled={isSubmitting}
			>
				{isSubmitting
					? t('auth.local.changePassword.submitting')
					: t('auth.local.changePassword.submit')}
			</button>
		</form>
	</div>
</div>

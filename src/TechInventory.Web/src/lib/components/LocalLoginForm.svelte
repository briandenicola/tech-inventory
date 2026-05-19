<script lang="ts">
	/**
	 * F025 — local-account sign-in form. Surfaced behind a toggle on the
	 * primary login page so the Entra path stays the obvious default. Reports
	 * a uniform error for any failure (mirrors the API's
	 * "InvalidCredentials" semantics).
	 */
	import { goto } from '$app/navigation';
	import { localSignIn } from '$lib/stores/auth';
	import { t } from '$lib/i18n';

	let { onCancel }: { onCancel?: () => void } = $props();

	let username = $state('');
	let password = $state('');
	let isSubmitting = $state(false);
	let errorKey = $state<string | null>(null);

	async function handleSubmit(event: Event) {
		event.preventDefault();
		if (isSubmitting) return;
		isSubmitting = true;
		errorKey = null;

		try {
			const meta = await localSignIn(username.trim(), password);
			if (meta.mustChangePassword) {
				await goto('/auth/change-password');
			} else {
				await goto('/devices');
			}
		} catch (error) {
			const isAuthFailure =
				error instanceof Error &&
				(error.message.includes('401') || error.message.toLowerCase().includes('unauthorized'));
			errorKey = isAuthFailure
				? 'auth.local.invalidCredentials'
				: 'auth.local.unknownError';
			console.error('[auth/local] sign-in failed:', error);
		} finally {
			isSubmitting = false;
		}
	}
</script>

<form
	class="space-y-4"
	onsubmit={handleSubmit}
	aria-describedby="local-login-description"
>
	<div>
		<h2 class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
			{t('auth.local.formTitle')}
		</h2>
		<p id="local-login-description" class="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
			{t('auth.local.formDescription')}
		</p>
	</div>

	<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
		{t('auth.local.usernameLabel')}
		<input
			type="text"
			autocomplete="username"
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-50"
			bind:value={username}
			required
			minlength={3}
			maxlength={64}
			disabled={isSubmitting}
		/>
	</label>

	<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
		{t('auth.local.passwordLabel')}
		<input
			type="password"
			autocomplete="current-password"
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-50"
			bind:value={password}
			required
			minlength={8}
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

	<button
		type="submit"
		class="flex w-full items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60"
		disabled={isSubmitting}
	>
		{isSubmitting ? t('auth.local.submitting') : t('auth.local.submit')}
	</button>

	{#if onCancel}
		<button
			type="button"
			class="block w-full text-center text-xs font-medium text-primary-700 hover:underline dark:text-primary-300"
			onclick={onCancel}
			disabled={isSubmitting}
		>
			{t('auth.local.backToSso')}
		</button>
	{/if}
</form>

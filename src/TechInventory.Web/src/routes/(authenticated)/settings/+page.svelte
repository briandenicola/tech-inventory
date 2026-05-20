<script lang="ts">
	/**
	 * F020 v1 — Settings page (Profile tab, display name only).
	 *
	 * Scope is intentionally narrow:
	 *  - Display name editable for the current user (PATCH /api/v1/owners/me).
	 *  - Email/oid + role rendered read-only (Entra owns those).
	 *
	 * Out of scope (deferred to F020b): avatar upload,
	 * default landing page, density, session tab. The page leaves room for
	 * those by sectioning Profile vs future panels.
	 */
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import api from '$lib/api/client';
	import { authStore, updateCurrentUserDisplayName } from '$lib/stores/auth';
	import { addToast } from '$lib/stores/toast';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';

	const currentUser = $derived($authStore.currentUser);

	let originalName = $state('');
	let displayName = $state('');
	let loading = $state(true);
	let saving = $state(false);
	let loadError = $state<string | null>(null);
	let fieldError = $state<string | null>(null);

	onMount(async () => {
		await loadProfile();
	});

	async function loadProfile() {
		loading = true;
		loadError = null;
		try {
			const me = await api.owners.me();
			originalName = me.displayName ?? '';
			displayName = originalName;
		} catch (err: unknown) {
			console.error('[Settings] Load failed:', err);
			loadError = err instanceof Error ? err.message : 'Failed to load profile';
		} finally {
			loading = false;
		}
	}

	const dirty = $derived(
		displayName.trim() !== originalName.trim() && displayName.trim().length > 0
	);
	const canSubmit = $derived(dirty && !saving);

	function validate(): boolean {
		fieldError = null;
		const trimmed = displayName.trim();
		if (trimmed.length === 0) {
			fieldError = t('settings.profile.errors.required');
			return false;
		}
		if (trimmed.length > 200) {
			fieldError = t('settings.profile.errors.tooLong');
			return false;
		}
		return true;
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!validate()) return;
		saving = true;
		try {
			const updated = await api.owners.updateMyProfile({ displayName: displayName.trim() });
			const newName = updated.displayName ?? displayName.trim();
			originalName = newName;
			displayName = newName;
			updateCurrentUserDisplayName(newName);
			addToast({ type: 'success', message: t('settings.profile.toast.success') });
		} catch (err: unknown) {
			console.error('[Settings] Update failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to update profile';
			addToast({ type: 'error', message });
		} finally {
			saving = false;
		}
	}

	function handleReset() {
		displayName = originalName;
		fieldError = null;
	}
</script>

<svelte:head>
	<title>{t('settings.title')} · {t('app.title')}</title>
</svelte:head>

<div class="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-8">
		<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
			{t('settings.title')}
		</h1>
		<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
			{t('settings.subtitle')}
		</p>
	</div>

	{#if loading}
		<LoadingSkeleton rows={4} />
	{:else if loadError}
		<ErrorState error={loadError} onRetry={loadProfile} />
	{:else if currentUser}
		<section
			class="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
			aria-labelledby="profile-heading"
		>
			<h2
				id="profile-heading"
				class="text-lg font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{t('settings.profile.heading')}
			</h2>
			<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
				{t('settings.profile.subheading')}
			</p>

			<form onsubmit={handleSubmit} class="mt-6 space-y-6">
				<div>
					<label
						for="displayName"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('settings.profile.fields.displayName')}
					</label>
					<input
						id="displayName"
						type="text"
						bind:value={displayName}
						maxlength={200}
						class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
						class:border-danger-500={fieldError}
						aria-invalid={fieldError ? 'true' : 'false'}
						aria-describedby={fieldError ? 'displayName-error' : 'displayName-help'}
					/>
					{#if fieldError}
						<p id="displayName-error" class="mt-2 text-sm text-danger-600 dark:text-danger-400">
							{fieldError}
						</p>
					{:else}
						<p id="displayName-help" class="mt-2 text-sm text-neutral-500 dark:text-neutral-400">
							{t('settings.profile.fields.displayNameHelp')}
						</p>
					{/if}
				</div>

				<dl class="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
					<div>
						<dt class="text-sm font-medium text-neutral-500 dark:text-neutral-400">
							{t('settings.profile.fields.role')}
						</dt>
						<dd class="mt-1">
							<span
								class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
								class:bg-primary-100={currentUser.role === 'Admin'}
								class:text-primary-700={currentUser.role === 'Admin'}
								class:dark:bg-primary-900={currentUser.role === 'Admin'}
								class:dark:text-primary-300={currentUser.role === 'Admin'}
								class:bg-success-100={currentUser.role === 'Member'}
								class:text-success-700={currentUser.role === 'Member'}
								class:dark:bg-success-900={currentUser.role === 'Member'}
								class:dark:text-success-300={currentUser.role === 'Member'}
								class:bg-neutral-200={currentUser.role === 'Viewer'}
								class:text-neutral-700={currentUser.role === 'Viewer'}
								class:dark:bg-neutral-700={currentUser.role === 'Viewer'}
								class:dark:text-neutral-300={currentUser.role === 'Viewer'}
							>
								{currentUser.role}
							</span>
						</dd>
						<p class="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
							{t('settings.profile.fields.roleHelp')}
						</p>
					</div>
					<div>
						<dt class="text-sm font-medium text-neutral-500 dark:text-neutral-400">
							{t('settings.profile.fields.entraObjectId')}
						</dt>
						<dd
							class="mt-1 break-all font-mono text-xs text-neutral-700 dark:text-neutral-300"
						>
							{currentUser.entraObjectId ?? '—'}
						</dd>
						<p class="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
							{t('settings.profile.fields.entraObjectIdHelp')}
						</p>
					</div>
				</dl>

				<div class="flex items-center justify-end gap-3">
					<button
						type="button"
						onclick={handleReset}
						disabled={!dirty || saving}
						class="rounded-md border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('common.actions.reset')}
					</button>
					<button
						type="submit"
						disabled={!canSubmit}
						class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
					>
						{saving ? t('settings.profile.saving') : t('common.actions.save')}
					</button>
				</div>
			</form>
		</section>

		<!-- F029: Appearance (theme toggle) -->
		<section
			class="mt-6 rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
			aria-labelledby="theme-heading"
		>
			<h2
				id="theme-heading"
				class="text-lg font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{t('settings.theme.heading')}
			</h2>
			<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
				{t('settings.theme.subheading')}
			</p>

			<div class="mt-4">
				<ThemeToggle />
			</div>
		</section>

		<section
			class="mt-6 rounded-2xl border border-dashed border-neutral-300 bg-neutral-50 p-6 text-sm text-neutral-600 dark:border-neutral-700 dark:bg-neutral-900/40 dark:text-neutral-400"
			aria-label={t('settings.deferred.heading')}
		>
			<h2 class="text-base font-semibold text-neutral-900 dark:text-neutral-100">
				{t('settings.deferred.heading')}
			</h2>
			<p class="mt-1">
				{t('settings.deferred.body')}
			</p>
		</section>
	{/if}
</div>

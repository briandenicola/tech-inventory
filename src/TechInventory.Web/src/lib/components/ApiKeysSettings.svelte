<!--
	ApiKeysSettings.svelte — #149 / spec 005 US-6.

	Personal API key management, rendered as a card inside the Settings page rather
	than at its own /settings/api-keys route: keys are a per-user preference and sit
	naturally beside the other account settings, which is also how they were specced
	visually.

	Revoking is destructive and unrecoverable — the key cannot be restored, only
	replaced — so it goes through a confirmation. Deliberately a native confirm():
	it is unmissable, keyboard- and screen-reader-accessible for free, and cannot be
	dismissed by a stray click the way a custom modal can.
-->
<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { apiKeys } from '$lib/api/client';
	import { addToast } from '$lib/stores/toast';
	import { getApiErrorMessage } from '$lib/utils/apiErrors';
	import ApiKeySecretDisplay from '$lib/components/ApiKeySecretDisplay.svelte';

	type ApiKeyScope = 'inventory.read' | 'inventory.write';

	interface ApiKeyItem {
		id: string;
		name: string;
		selector: string;
		scope: string;
		createdAt: string;
		expiresAt: string;
		revokedAt?: string | null;
		isActive: boolean;
	}

	/** Mirrors ApiKey.MaxActiveKeysPerPrincipal; used only for the hint, the server enforces it. */
	const MAX_ACTIVE_KEYS = 5;

	let keys = $state<ApiKeyItem[]>([]);
	let loading = $state(true);
	let loadError = $state<string | null>(null);

	let newKeyName = $state('');
	let newKeyScope = $state<ApiKeyScope>('inventory.read');
	let creating = $state(false);
	let createError = $state<string | null>(null);

	let revokingId = $state<string | null>(null);
	let createdSecret = $state<string | null>(null);

	const activeCount = $derived(keys.filter((key) => key.isActive).length);
	const atQuota = $derived(activeCount >= MAX_ACTIVE_KEYS);

	async function load() {
		loading = true;
		loadError = null;
		try {
			const response = await apiKeys.list({ pageSize: 50 });
			keys = (response?.items ?? []) as ApiKeyItem[];
		} catch (error) {
			loadError = getApiErrorMessage(error, t('settings.apiKeys.errors.load'));
		} finally {
			loading = false;
		}
	}

	onMount(load);

	async function create(event: SubmitEvent) {
		event.preventDefault();
		createError = null;

		const name = newKeyName.trim();
		if (name.length === 0) {
			createError = t('settings.apiKeys.errors.nameRequired');
			return;
		}

		creating = true;
		try {
			const created = await apiKeys.create({ name, scope: newKeyScope });
			// Surfaced before the list refresh: this is the only time the plaintext key
			// exists, so it must not depend on a second request succeeding.
			createdSecret = created?.key ?? null;
			newKeyName = '';
			addToast({ type: 'success', message: t('settings.apiKeys.toast.created') });
			await load();
		} catch (error) {
			createError = getApiErrorMessage(error, t('settings.apiKeys.errors.create'));
		} finally {
			creating = false;
		}
	}

	async function revoke(key: ApiKeyItem) {
		if (!confirm(t('settings.apiKeys.revokeConfirm'))) {
			return;
		}

		revokingId = key.id;
		try {
			await apiKeys.revoke(key.id);
			addToast({ type: 'success', message: t('settings.apiKeys.toast.revoked') });
			await load();
		} catch (error) {
			addToast({
				type: 'error',
				message: getApiErrorMessage(error, t('settings.apiKeys.errors.revoke'))
			});
		} finally {
			revokingId = null;
		}
	}

	function formatDate(value: string): string {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString();
	}

	function scopeLabel(scope: string): string {
		return scope === 'inventory.write'
			? t('settings.apiKeys.scope.write')
			: t('settings.apiKeys.scope.read');
	}

	/** The muted second line: what the key can do, and when it was made. */
	function describe(key: ApiKeyItem): string {
		const state = key.revokedAt
			? t('settings.apiKeys.revoked')
			: key.isActive
				? scopeLabel(key.scope)
				: t('settings.apiKeys.expired');

		return `${state} · ${t('settings.apiKeys.createdLabel', { date: formatDate(key.createdAt) })}`;
	}
</script>

<section
	class="mt-6 rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	aria-labelledby="api-keys-heading"
>
	<h2 id="api-keys-heading" class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
		{t('settings.apiKeys.heading')}
	</h2>
	<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
		{t('settings.apiKeys.subheading')}
	</p>

	{#if createdSecret}
		<div class="mt-4">
			<ApiKeySecretDisplay secret={createdSecret} onDismiss={() => (createdSecret = null)} />
		</div>
	{/if}

	<div class="mt-4">
		{#if loading}
			<p class="text-sm text-neutral-500 dark:text-neutral-400" data-testid="api-keys-loading">
				{t('settings.apiKeys.loading')}
			</p>
		{:else if loadError}
			<div
				class="rounded-xl border border-danger-200 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-900 dark:bg-danger-950 dark:text-danger-200"
				role="alert"
			>
				{loadError}
			</div>
		{:else if keys.length === 0}
			<p class="text-sm text-neutral-500 dark:text-neutral-400" data-testid="api-keys-empty">
				{t('settings.apiKeys.empty')}
			</p>
		{:else}
			<ul class="flex flex-col gap-2" aria-label={t('settings.apiKeys.heading')}>
				{#each keys as key (key.id)}
					<li
						class="flex items-center justify-between gap-3 rounded-xl border border-neutral-200 px-4 py-3 dark:border-neutral-800"
						data-testid="api-key-row"
					>
						<div class="min-w-0">
							<p class="truncate text-sm font-medium text-neutral-900 dark:text-neutral-100">
								{key.name}
							</p>
							<p class="truncate text-xs text-neutral-500 dark:text-neutral-400">
								{describe(key)}
							</p>
						</div>
						{#if !key.revokedAt}
							<button
								type="button"
								onclick={() => revoke(key)}
								disabled={revokingId === key.id}
								class="inline-flex h-11 shrink-0 items-center rounded-lg border border-danger-300 px-4 text-sm font-medium text-danger-700 transition-colors hover:bg-danger-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-danger-800 dark:text-danger-300 dark:hover:bg-danger-950"
							>
								{revokingId === key.id
									? t('settings.apiKeys.revoking')
									: t('settings.apiKeys.revoke')}
							</button>
						{/if}
					</li>
				{/each}
			</ul>
		{/if}
	</div>

	<form class="mt-4 flex flex-col gap-3 sm:flex-row sm:items-start" onsubmit={create}>
		<div class="min-w-0 flex-1">
			<label class="sr-only" for="new-api-key-name">{t('settings.apiKeys.nameLabel')}</label>
			<input
				id="new-api-key-name"
				type="text"
				bind:value={newKeyName}
				placeholder={t('settings.apiKeys.namePlaceholder')}
				maxlength="100"
				disabled={creating || atQuota}
				aria-invalid={createError ? 'true' : undefined}
				aria-describedby={createError ? 'new-api-key-error' : undefined}
				class="h-11 w-full rounded-lg border border-neutral-300 bg-white px-3 text-sm text-neutral-900 placeholder:text-neutral-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
			/>
		</div>

		<fieldset class="shrink-0" disabled={creating || atQuota}>
			<legend class="sr-only">{t('settings.apiKeys.scopeLabel')}</legend>
			<div
				class="flex h-11 items-center rounded-lg border border-neutral-300 p-0.5 dark:border-neutral-700"
			>
				{#each [{ value: 'inventory.read' as const, label: t('settings.apiKeys.scope.read') }, { value: 'inventory.write' as const, label: t('settings.apiKeys.scope.write') }] as option (option.value)}
					<button
						type="button"
						onclick={() => (newKeyScope = option.value)}
						aria-pressed={newKeyScope === option.value}
						class="h-full rounded-md px-3 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-inset"
						class:bg-primary-600={newKeyScope === option.value}
						class:text-white={newKeyScope === option.value}
						class:text-neutral-600={newKeyScope !== option.value}
						class:dark:text-neutral-300={newKeyScope !== option.value}
					>
						{option.label}
					</button>
				{/each}
			</div>
		</fieldset>

		<button
			type="submit"
			disabled={creating || atQuota}
			class="inline-flex h-11 shrink-0 items-center justify-center rounded-lg bg-primary-600 px-4 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
		>
			{creating ? t('settings.apiKeys.creating') : t('settings.apiKeys.create')}
		</button>
	</form>

	{#if createError}
		<p id="new-api-key-error" class="mt-2 text-sm text-danger-700 dark:text-danger-300" role="alert">
			{createError}
		</p>
	{/if}

	{#if !loading && !loadError}
		<p class="mt-2 text-xs text-neutral-500 dark:text-neutral-400">
			{t('settings.apiKeys.quotaHint', { count: activeCount, max: MAX_ACTIVE_KEYS })}
		</p>
	{/if}
</section>

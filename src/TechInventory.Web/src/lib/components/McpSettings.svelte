<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { mcpSettings } from '$lib/api/client';
	import { addToast } from '$lib/stores/toast';
	import { getApiErrorMessage } from '$lib/utils/apiErrors';

	let enabled = $state(false);
	let loading = $state(true);
	let saving = $state(false);
	let loadError = $state<string | null>(null);

	async function load() {
		loading = true;
		loadError = null;
		try {
			const response = await mcpSettings.get();
			enabled = response.enabled ?? false;
		} catch (error) {
			loadError = getApiErrorMessage(error, t('settings.mcp.errors.load'));
		} finally {
			loading = false;
		}
	}

	onMount(load);

	async function updateEnabled(nextEnabled: boolean) {
		const previousEnabled = enabled;
		enabled = nextEnabled;
		saving = true;

		try {
			const response = await mcpSettings.update({ enabled: nextEnabled });
			enabled = response.enabled ?? nextEnabled;
			addToast({
				type: 'success',
				message: t(enabled ? 'settings.mcp.toast.enabled' : 'settings.mcp.toast.disabled')
			});
		} catch (error) {
			enabled = previousEnabled;
			addToast({
				type: 'error',
				message: getApiErrorMessage(error, t('settings.mcp.errors.save'))
			});
		} finally {
			saving = false;
		}
	}
</script>

<section
	class="mt-6 rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	aria-labelledby="mcp-heading"
>
	<h2 id="mcp-heading" class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
		{t('settings.mcp.heading')}
	</h2>
	<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
		{t('settings.mcp.subheading')}
	</p>

	{#if loading}
		<p class="mt-4 text-sm text-neutral-500 dark:text-neutral-400" data-testid="mcp-loading">
			{t('settings.mcp.loading')}
		</p>
	{:else if loadError}
		<div
			class="mt-4 rounded-xl border border-danger-200 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-900 dark:bg-danger-950 dark:text-danger-200"
			role="alert"
		>
			<p>{loadError}</p>
			<button
				type="button"
				onclick={load}
				class="mt-3 inline-flex min-h-11 items-center rounded-lg border border-danger-300 px-4 font-medium hover:bg-danger-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-500 dark:border-danger-800 dark:hover:bg-danger-900"
			>
				{t('common.actions.retry')}
			</button>
		</div>
	{:else}
		<div class="mt-4 flex min-h-11 items-center justify-between gap-4">
			<span>
				<span
					id="mcp-enable-label"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('settings.mcp.enableLabel')}
				</span>
				<span
					id="mcp-enable-help"
					class="mt-1 block text-xs text-neutral-500 dark:text-neutral-400"
				>
					{t('settings.mcp.enableHelp')}
				</span>
			</span>
			<button
				type="button"
				role="switch"
				aria-checked={enabled}
				aria-labelledby="mcp-enable-label"
				aria-describedby="mcp-enable-help"
				disabled={saving}
				onclick={() => updateEnabled(!enabled)}
				class="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:cursor-wait disabled:opacity-60"
			>
				<span
					class="flex h-6 w-11 items-center rounded-full p-0.5 transition-colors"
					class:bg-primary-600={enabled}
					class:bg-neutral-300={!enabled}
					class:dark:bg-primary-500={enabled}
					class:dark:bg-neutral-700={!enabled}
					aria-hidden="true"
				>
					<span
						class="block h-5 w-5 rounded-full bg-white shadow-sm transition-transform"
						class:translate-x-5={enabled}
					></span>
				</span>
			</button>
		</div>
	{/if}
</section>

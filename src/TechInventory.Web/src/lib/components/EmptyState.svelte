<!--
	EmptyState.svelte — Shared empty state for list/table pages

	Per T15: Friendly empty-state component — icon + message + CTA.
	Used by the devices list (filtered/unfiltered) and by admin reference-data
	pages (brands, categories, locations, networks, owners, tags) via the
	`message`/`icon` overrides.
-->
<script lang="ts">
	import type { Snippet } from 'svelte';
	import { t } from '$lib/i18n';

	interface Props {
		filtered?: boolean;
		message?: string;
		icon?: Snippet;
		onAdd?: () => void;
		showAddAction?: boolean;
		addHref?: string;
	}

	let {
		filtered = false,
		message,
		icon,
		onAdd,
		showAddAction = true,
		addHref = '/devices/new'
	}: Props = $props();
</script>

<div
	class="flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950"
>
	<!-- Icon -->
	{#if icon}
		{@render icon()}
	{:else}
		<svg
			class="h-16 w-16 text-neutral-400 dark:text-neutral-600"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			aria-hidden="true"
		>
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="1.5"
				d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"
			/>
		</svg>
	{/if}

	<!-- Message -->
	<h2 class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
		{message ?? (filtered ? t('devices.list.emptyFiltered') : t('devices.list.emptyState'))}
	</h2>

	<!-- CTA (only show if not filtered and the user can create devices) -->
	{#if !filtered && showAddAction}
		{#if onAdd}
			<button
				type="button"
				onclick={onAdd}
				class="mt-6 inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				<svg
					class="h-5 w-5"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				{t('devices.list.addButton')}
			</button>
		{:else}
			<a
				href={addHref}
				class="mt-6 inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				<svg
					class="h-5 w-5"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
				</svg>
				{t('devices.list.addButton')}
			</a>
		{/if}
	{/if}
</div>

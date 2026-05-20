<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		count: number;
		onClear: () => void;
		onDelete: () => void;
		onMerge: () => void;
		deleteDisabled?: boolean;
		mergeDisabled?: boolean;
	}

	let {
		count,
		onClear,
		onDelete,
		onMerge,
		deleteDisabled = false,
		mergeDisabled = false
	}: Props = $props();

	let regionElement = $state<HTMLDivElement | undefined>(undefined);
	let previousCount = $state(0);

	$effect(() => {
		if (count > 0 && previousCount === 0) {
			regionElement?.focus();
		}

		previousCount = count;
	});
</script>

{#if count > 0}
	<div
		bind:this={regionElement}
		class="fixed inset-x-0 bottom-0 z-30 border-t border-neutral-200 bg-white/95 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/95"
		role="region"
		aria-label={t('admin.bulk.actions')}
		tabindex="-1"
	>
		<div class="mx-auto flex max-w-7xl flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
			<div class="flex items-center gap-2">
				<span class="inline-flex items-center rounded-full bg-primary-100 px-3 py-1 text-sm font-medium text-primary-800 dark:bg-primary-900 dark:text-primary-200">
					{t('admin.bulk.selected', { count })}
				</span>
				<button
					type="button"
					onclick={onClear}
					class="text-sm font-medium text-neutral-600 underline-offset-2 hover:underline focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-400"
				>
					{t('admin.bulk.clear')}
				</button>
			</div>

			<div class="ml-auto flex flex-wrap items-center gap-2">
				<button
					type="button"
					onclick={onDelete}
					disabled={deleteDisabled}
					class="inline-flex min-h-11 items-center justify-center rounded-full border border-danger-300 px-4 py-2 text-sm font-medium text-danger-700 transition-colors hover:bg-danger-50 focus:outline-none focus:ring-2 focus:ring-danger-500 disabled:cursor-not-allowed disabled:opacity-50 dark:border-danger-800 dark:text-danger-300 dark:hover:bg-danger-950"
				>
					{t('admin.bulk.deleteSelected')}
				</button>
				<button
					type="button"
					onclick={onMerge}
					disabled={mergeDisabled}
					class="inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-700 dark:hover:bg-primary-800"
				>
					{t('admin.bulk.mergeSelected')}
				</button>
			</div>
		</div>
	</div>
{/if}

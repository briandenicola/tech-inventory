<!--
	BulkActionBar.svelte — F024 sticky bottom action bar shown while devices are selected.

	Hidden when count === 0. Renders selection count + per-field "change" buttons +
	destructive "Delete selected" CTA. Admin-only actions are caller's responsibility
	to gate (passing the corresponding handler as undefined hides the button).
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	type BulkField = 'category' | 'owner' | 'brand' | 'location' | 'status';

	interface Props {
		count: number;
		onClear: () => void;
		onChangeField: (field: BulkField) => void;
		onDelete?: () => void;
	}

	let { count, onClear, onChangeField, onDelete }: Props = $props();
</script>

{#if count > 0}
	<div
		class="fixed inset-x-0 bottom-0 z-30 border-t border-neutral-200 bg-white/95 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/95"
		role="region"
		aria-label={t('devices.bulk.actions')}
	>
		<div class="mx-auto flex max-w-7xl flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
			<div class="flex items-center gap-2">
				<span class="inline-flex items-center rounded-full bg-primary-100 px-3 py-1 text-sm font-medium text-primary-800 dark:bg-primary-900 dark:text-primary-200">
					{t('devices.bulk.countSelected', { count })}
				</span>
				<button
					type="button"
					onclick={onClear}
					class="text-sm font-medium text-neutral-600 underline-offset-2 hover:underline focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-400"
				>
					{t('devices.bulk.clear')}
				</button>
			</div>

			<div class="ml-auto flex flex-wrap items-center gap-2">
				<button
					type="button"
					onclick={() => onChangeField('category')}
					class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('devices.bulk.changeCategory')}
				</button>
				<button
					type="button"
					onclick={() => onChangeField('owner')}
					class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('devices.bulk.changeOwner')}
				</button>
				<button
					type="button"
					onclick={() => onChangeField('brand')}
					class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('devices.bulk.changeBrand')}
				</button>
				<button
					type="button"
					onclick={() => onChangeField('location')}
					class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('devices.bulk.changeLocation')}
				</button>
				<button
					type="button"
					onclick={() => onChangeField('status')}
					class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('devices.bulk.changeStatus')}
				</button>
				{#if onDelete}
					<button
						type="button"
						onclick={onDelete}
						class="rounded-lg bg-danger-600 px-3 py-2 text-sm font-medium text-white hover:bg-danger-700 focus:outline-none focus:ring-2 focus:ring-danger-500 focus:ring-offset-2 dark:bg-danger-500 dark:hover:bg-danger-600"
					>
						{t('devices.bulk.delete')}
					</button>
				{/if}
			</div>
		</div>
	</div>
{/if}

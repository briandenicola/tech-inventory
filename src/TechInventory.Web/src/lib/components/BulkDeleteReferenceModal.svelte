<script lang="ts">
	import api from '$lib/api/client';
	import { t } from '$lib/i18n';
	import { addToast } from '$lib/stores/toast';
	import {
		fetchReferenceDeviceCount,
		type MergeEntityOption,
		type MergeEntityType
	} from '$lib/utils/referenceMerge';

	interface Props {
		entityType: MergeEntityType;
		items: MergeEntityOption[];
		isOpen: boolean;
		onDeleted: () => Promise<void> | void;
		onCancel: () => void;
	}

	let { entityType, items, isOpen, onDeleted, onCancel }: Props = $props();

	let isSubmitting = $state(false);
	let isLoadingCounts = $state(false);
	let errorMessage = $state<string | null>(null);
	let countsById = $state<Record<string, number>>({});
	let modalElement = $state<HTMLDivElement | undefined>(undefined);
	let loadRequestId = 0;

	const entityLabels: Record<MergeEntityType, { singular: string; plural: string }> = {
		brand: {
			singular: t('common.nouns.brand'),
			plural: t('common.nouns.brands')
		},
		category: {
			singular: t('common.nouns.category'),
			plural: t('common.nouns.categories')
		},
		location: {
			singular: t('common.nouns.location'),
			plural: t('common.nouns.locations')
		},
		network: {
			singular: t('common.nouns.network'),
			plural: t('common.nouns.networks')
		}
	};

	const copy = $derived(entityLabels[entityType]);
	const itemsWithCounts = $derived(
		items.map((item) => ({
			...item,
			deviceCount: countsById[item.id] ?? null
		}))
	);
	const conflictingItems = $derived(
		itemsWithCounts.filter((item) => typeof item.deviceCount === 'number' && item.deviceCount > 0)
	);
	const canSubmit = $derived(
		!isSubmitting && !isLoadingCounts && conflictingItems.length === 0 && items.length > 0
	);

	$effect(() => {
		if (!isOpen || items.length === 0) {
			return;
		}

		errorMessage = null;
		countsById = {};
		isLoadingCounts = true;
		const currentRequestId = ++loadRequestId;

		void Promise.all(
			items.map(async (item) => [item.id, await fetchReferenceDeviceCount(entityType, item.id)] as const)
		)
			.then((entries) => {
				if (loadRequestId !== currentRequestId) {
					return;
				}

				countsById = Object.fromEntries(entries);
			})
			.catch((error: unknown) => {
				console.error('[BulkDeleteReferenceModal] Device count lookup failed:', error);
				if (loadRequestId === currentRequestId) {
					errorMessage =
						error instanceof Error ? error.message : t('admin.merge.error');
				}
			})
			.finally(() => {
				if (loadRequestId === currentRequestId) {
					isLoadingCounts = false;
				}
			});
	});

	$effect(() => {
		if (!modalElement) {
			return;
		}

		const focusables = modalElement.querySelectorAll<HTMLElement>(
			'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
		);
		const first = focusables[0];
		const last = focusables[focusables.length - 1];

		function trap(event: KeyboardEvent) {
			if (event.key !== 'Tab') {
				return;
			}

			if (event.shiftKey && document.activeElement === first) {
				event.preventDefault();
				last?.focus();
			} else if (!event.shiftKey && document.activeElement === last) {
				event.preventDefault();
				first?.focus();
			}
		}

		modalElement.addEventListener('keydown', trap);
		first?.focus();
		return () => modalElement?.removeEventListener('keydown', trap);
	});

	function handleKeydown(event: KeyboardEvent): void {
		if (event.key === 'Escape' && !isSubmitting) {
			onCancel();
		}
	}

	async function handleConfirm(): Promise<void> {
		if (!canSubmit) {
			return;
		}

		isSubmitting = true;
		errorMessage = null;
		try {
			const ids = items.map((item) => item.id);
			const response = await (async () => {
				switch (entityType) {
					case 'brand':
						return api.brands.bulkDelete({ ids });
					case 'category':
						return api.categories.bulkDelete({ ids });
					case 'location':
						return api.locations.bulkDelete({ ids });
					case 'network':
						return api.networks.bulkDelete({ ids });
				}
			})();
			const affectedCount = response.deletedCount ?? response.affectedCount ?? ids.length;
			addToast({
				type: 'success',
				message: t('admin.bulk.deleteSuccess', { count: affectedCount })
			});
			onCancel();
			await onDeleted();
		} catch (error: unknown) {
			console.error('[BulkDeleteReferenceModal] Delete failed:', error);
			const message = error instanceof Error ? error.message : t('admin.merge.error');
			errorMessage = message;
			addToast({ type: 'error', message });
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<div class="ti-modal-backdrop fixed inset-0 z-40" aria-hidden="true"></div>

	<div
		bind:this={modalElement}
		class="fixed inset-0 z-50 flex items-center justify-center p-4"
		role="dialog"
		aria-labelledby="reference-bulk-delete-title"
		aria-describedby="reference-bulk-delete-description"
		aria-modal="true"
	>
		<div class="ti-modal-surface w-full max-w-xl overflow-hidden rounded-2xl bg-white shadow-xl dark:bg-neutral-900">
			<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
				<h2
					id="reference-bulk-delete-title"
					class="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
				>
					{t('admin.bulk.deleteConfirmTitle')}
				</h2>
				<p
					id="reference-bulk-delete-description"
					class="mt-2 text-sm text-neutral-600 dark:text-neutral-300"
				>
					{t('admin.bulk.deleteConfirmMessage', {
						count: items.length,
						entityPlural: copy.plural.toLowerCase()
					})}
				</p>
			</div>

			<div class="space-y-4 px-6 py-4">
				<ul class="space-y-2" aria-label={copy.plural}>
					{#each itemsWithCounts as item (item.id)}
						<li class="flex items-center justify-between gap-3 rounded-xl border border-neutral-200 px-3 py-2 text-sm dark:border-neutral-800">
							<span class="font-medium text-neutral-900 dark:text-neutral-50">{item.name}</span>
							{#if typeof item.deviceCount === 'number'}
								<span class="text-xs text-neutral-500 dark:text-neutral-400">
									{t('admin.bulk.deviceCount', { count: item.deviceCount })}
								</span>
							{/if}
						</li>
					{/each}
				</ul>

				{#if isLoadingCounts}
					<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('common.states.loading')}</p>
				{/if}

				{#if conflictingItems.length > 0}
					<div class="rounded-2xl border border-warning-200 bg-warning-50 p-4 text-sm text-warning-900 dark:border-warning-900 dark:bg-warning-950 dark:text-warning-100">
						<p>{t('admin.bulk.cannotDeleteHasDevices')}</p>
						<ul class="mt-3 list-disc space-y-1 pl-5">
							{#each conflictingItems as item (item.id)}
								<li>{item.name}</li>
							{/each}
						</ul>
					</div>
				{/if}

				{#if errorMessage}
					<div
						class="rounded-2xl border border-danger-200 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-900 dark:bg-danger-950 dark:text-danger-200"
						role="alert"
					>
						{errorMessage}
					</div>
				{/if}

				<div class="flex flex-col-reverse gap-3 border-t border-neutral-200 pt-4 dark:border-neutral-800 sm:flex-row sm:justify-end">
					<button
						type="button"
						onclick={onCancel}
						disabled={isSubmitting}
						class="inline-flex min-h-11 items-center justify-center rounded-full border border-neutral-300 px-5 py-2.5 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('common.actions.cancel')}
					</button>
					<button
						type="button"
						onclick={handleConfirm}
						disabled={!canSubmit}
						class="inline-flex min-h-11 items-center justify-center rounded-full bg-danger-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-danger-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-danger-500 dark:hover:bg-danger-600"
					>
						{isSubmitting ? t('common.states.deleting') : t('admin.bulk.deleteSelected')}
					</button>
				</div>
			</div>
		</div>
	</div>
{/if}

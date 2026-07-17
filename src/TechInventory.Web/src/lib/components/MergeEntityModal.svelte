<script lang="ts">
	import { t } from '$lib/i18n';
	import type { MergeEntityOption, MergeEntityType } from '$lib/utils/referenceMerge';

	interface Props {
		entityType: MergeEntityType;
		sourceEntity?: MergeEntityOption | null;
		sourceEntities?: MergeEntityOption[];
		entities: MergeEntityOption[];
		isOpen: boolean;
		isSubmitting?: boolean;
		errorMessage?: string | null;
		onConfirm: (targetId: string) => Promise<void>;
		onCancel: () => void;
	}

	let {
		entityType,
		sourceEntity = null,
		sourceEntities = [],
		entities,
		isOpen,
		isSubmitting = false,
		errorMessage = null,
		onConfirm,
		onCancel
	}: Props = $props();

	let searchQuery = $state('');
	let selectedTargetId = $state('');
	let searchInput = $state<HTMLInputElement | undefined>(undefined);

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
	const activeSourceEntities = $derived.by(() =>
		sourceEntities.length > 0 ? sourceEntities : sourceEntity ? [sourceEntity] : []
	);
	const isBulkMerge = $derived(activeSourceEntities.length > 1);
	const totalAffectedCount = $derived.by(() => {
		if (activeSourceEntities.some((entity) => typeof entity.deviceCount !== 'number')) {
			return '…';
		}

		return activeSourceEntities.reduce((total, entity) => total + (entity.deviceCount ?? 0), 0);
	});
	const mergeIntoCount = $derived(Math.max(0, activeSourceEntities.length - 1));
	const targetOptions = $derived.by(() => {
		const query = searchQuery.trim().toLowerCase();
		return entities
			.filter((entity) => {
				if (!isBulkMerge && sourceEntity && entity.id === sourceEntity.id) {
					return false;
				}

				return query.length === 0 || entity.name.toLowerCase().includes(query);
			})
			.sort((left, right) => left.name.localeCompare(right.name));
	});
	const selectedTarget = $derived(
		targetOptions.find((entity) => entity.id === selectedTargetId) ??
			entities.find((entity) => entity.id === selectedTargetId) ??
			null
	);

	$effect(() => {
		if (isOpen) {
			searchQuery = '';
			selectedTargetId = '';
			searchInput?.focus();
		}
	});

	function handleKeydown(event: KeyboardEvent): void {
		if (event.key === 'Escape' && !isSubmitting) {
			onCancel();
		}
	}

	async function handleConfirm(): Promise<void> {
		if (!selectedTargetId || isSubmitting) {
			return;
		}

		await onConfirm(selectedTargetId);
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen && activeSourceEntities.length > 0}
	<div class="ti-modal-backdrop fixed inset-0 z-40" aria-hidden="true"></div>

	<div
		class="fixed inset-0 z-50 flex items-center justify-center px-4"
		role="dialog"
		aria-modal="true"
		aria-labelledby="merge-entity-title"
	>
		<div class="ti-modal-surface w-full max-w-2xl rounded-2xl bg-white p-6 shadow-2xl dark:bg-neutral-900">
			<div class="space-y-2">
				<h2
					id="merge-entity-title"
					class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
				>
					{t('admin.merge.title', { entity: copy.singular })}
				</h2>
				<p class="text-sm text-neutral-600 dark:text-neutral-300">
					{#if isBulkMerge}
						{t('admin.merge.bulkDescription', {
							count: activeSourceEntities.length,
							entityPlural: copy.plural.toLowerCase()
						})}
					{:else}
						{t('admin.merge.description', {
							source: activeSourceEntities[0].name,
							entity: copy.singular.toLowerCase()
						})}
					{/if}
				</p>
			</div>

			<div class="mt-6 grid gap-4 md:grid-cols-2">
				<div class="rounded-2xl border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-800 dark:bg-neutral-950">
					<p class="text-sm font-medium text-neutral-500 dark:text-neutral-400">
						{#if isBulkMerge}
							{t('admin.merge.sourcesLabel', { entityPlural: copy.plural })}
						{:else}
							{t('admin.merge.sourceLabel', { entity: copy.singular })}
						{/if}
					</p>

					{#if isBulkMerge}
						<ul class="mt-3 space-y-2" aria-label={t('admin.merge.sourcesLabel', { entityPlural: copy.plural })}>
							{#each activeSourceEntities as entity (entity.id)}
								<li class="flex items-center justify-between gap-3 rounded-xl border border-neutral-200 bg-white px-3 py-2 text-sm dark:border-neutral-800 dark:bg-neutral-900">
									<span class="font-medium text-neutral-900 dark:text-neutral-50">{entity.name}</span>
									{#if typeof entity.deviceCount === 'number'}
										<span class="text-xs text-neutral-500 dark:text-neutral-400">
											{t('admin.bulk.deviceCount', { count: entity.deviceCount })}
										</span>
									{/if}
								</li>
							{/each}
						</ul>
					{:else}
						<p class="mt-2 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
							{activeSourceEntities[0].name}
						</p>
					{/if}
				</div>

				<div class="space-y-3 rounded-2xl border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950">
					<label for="merge-search" class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('admin.merge.searchLabel')}
					</label>
					<input
						id="merge-search"
						bind:this={searchInput}
						type="search"
						bind:value={searchQuery}
						placeholder={t('admin.merge.searchPlaceholder', {
							entityPlural: copy.plural.toLowerCase()
						})}
						class="min-h-11 w-full rounded-xl border border-neutral-300 px-3 py-2 text-sm text-neutral-900 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-50"
					/>

					<label for="merge-target" class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('admin.merge.targetLabel')}
					</label>
					<select
						id="merge-target"
						bind:value={selectedTargetId}
						class="min-h-11 w-full rounded-xl border border-neutral-300 px-3 py-2 text-sm text-neutral-900 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-50"
					>
						<option value="">{t('admin.merge.targetPlaceholder')}</option>
						{#each targetOptions as entity (entity.id)}
							<option value={entity.id}>{entity.name}</option>
						{/each}
					</select>
					<p class="text-xs text-neutral-500 dark:text-neutral-400">
						{t('admin.merge.targetHint', { entity: copy.singular.toLowerCase() })}
					</p>
				</div>
			</div>

			<div class="mt-6 rounded-2xl border border-primary-200 bg-primary-50 p-4 text-sm text-primary-900 dark:border-primary-900 dark:bg-primary-950 dark:text-primary-100">
				{#if selectedTarget}
					{#if isBulkMerge}
						{t('admin.merge.bulkConfirmation', {
							count: mergeIntoCount,
							devices: totalAffectedCount,
							target: selectedTarget.name,
							entityPlural: copy.plural.toLowerCase()
						})}
					{:else}
						{t('admin.merge.confirmation', {
							count: totalAffectedCount,
							source: activeSourceEntities[0].name,
							target: selectedTarget.name
						})}
					{/if}
				{:else}
					{t('admin.merge.selectTargetPrompt', { entity: copy.singular.toLowerCase() })}
				{/if}
			</div>

			{#if errorMessage}
				<div
					class="mt-4 rounded-2xl border border-danger-200 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-900 dark:bg-danger-950 dark:text-danger-200"
					role="alert"
				>
					{errorMessage}
				</div>
			{/if}

			<div class="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
				<button
					type="button"
					onclick={onCancel}
					disabled={isSubmitting}
					class="inline-flex min-h-11 items-center justify-center rounded-full border border-neutral-300 px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors duration-150 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('common.actions.cancel')}
				</button>
				<button
					type="button"
					onclick={handleConfirm}
					disabled={!selectedTargetId || isSubmitting}
					class="inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors duration-150 hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-700 dark:hover:bg-primary-800"
				>
					{isSubmitting ? t('common.states.loading') : t('common.actions.confirm')}
				</button>
			</div>
		</div>
	</div>
{/if}

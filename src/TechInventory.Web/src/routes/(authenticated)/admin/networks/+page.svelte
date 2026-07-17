<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { NetworkResponse } from '$lib/api/types';
	import { networkSchema, type NetworkFormData } from '$lib/schemas/network';
	import { addToast } from '$lib/stores/toast';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { fetchReferenceData } from '$lib/stores/referenceData';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import BulkDeleteReferenceModal from '$lib/components/BulkDeleteReferenceModal.svelte';
	import MergeEntityModal from '$lib/components/MergeEntityModal.svelte';
	import ReferenceDataBulkBar from '$lib/components/ReferenceDataBulkBar.svelte';
	import DeactivateConfirmModal from '$lib/components/admin/DeactivateConfirmModal.svelte';

	import {
		fetchReferenceDeviceCount,
		mergeReferenceEntities,
		mergeReferenceEntitySelection,
		type MergeEntityOption
	} from '$lib/utils/referenceMerge';
	import {
		clearReferenceSelection,
		getVisibleReferenceSelectionState,
		toggleAllVisibleReferenceSelections,
		toggleReferenceSelection
	} from '$lib/utils/referenceSelection';

	/**
	 * T31: Networks Admin — paginated list with Add/Edit/Deactivate
	 * Fields: name (required), description (optional)
	 */

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

	const urlParams = $derived.by(() => {
		const params = $page.url.searchParams;
		return {
			page: parseInt(params.get('page') || '1', 10),
			pageSize: parseInt(params.get('pageSize') || '25', 10),
			includeInactive: params.get('includeInactive') === 'true'
		};
	});

	let networks = $state<NetworkResponse[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	let formModalOpen = $state(false);
	let editingNetwork = $state<NetworkResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingNetwork = $state<NetworkResponse | null>(null);
	let mergeModalOpen = $state(false);
	let mergeSourceNetworks = $state<MergeEntityOption[]>([]);
	let mergeTargetOptions = $state<MergeEntityOption[]>([]);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);
	let selectedIds = $state<Set<string>>(new Set());
	let bulkDeleteModalOpen = $state(false);

	let formData = $state<NetworkFormData>({ name: '', description: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);


	const visibleNetworkIds = $derived(
		networks.map((network) => network.id).filter((networkId): networkId is string => !!networkId)
	);
	const selectionState = $derived(getVisibleReferenceSelectionState(selectedIds, visibleNetworkIds));
	const allVisibleSelected = $derived(selectionState.allVisibleSelected);
	const someVisibleSelected = $derived(selectionState.someVisibleSelected);
	const selectedNetworks = $derived.by(() =>
		networks.filter(
			(network): network is NetworkResponse & { id: string; name: string } =>
				!!network.id && !!network.name && selectedIds.has(network.id)
		)
	);
	const selectedNetworkOptions = $derived(
		selectedNetworks.map((network) => ({ id: network.id, name: network.name }))
	);
	const selectedActiveNetworkOptions = $derived(
		selectedNetworks
			.filter((network) => network.isActive)
			.map((network) => ({ id: network.id, name: network.name }))
	);
	const canBulkMerge = $derived(
		selectedActiveNetworkOptions.length >= 2 &&
			selectedActiveNetworkOptions.length === selectedNetworks.length
	);

	$effect(() => {
		loadNetworks();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadNetworks);
		return unregister;
	});

	$effect(() => {
		void urlParams.page;
		void urlParams.pageSize;
		void urlParams.includeInactive;
		selectedIds = clearReferenceSelection();
	});

	async function loadNetworks() {
		loading = true;
		error = null;
		try {
			const response = await api.networks.list(urlParams);
			networks = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[NetworksAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load networks';
		} finally {
			loading = false;
		}
	}

	function openAddModal() {
		editingNetwork = null;
		formData = { name: '', description: '' };
		formErrors = {};
		formModalOpen = true;
	}

	function openEditModal(network: NetworkResponse) {
		editingNetwork = network;
		formData = {
			name: network.name ?? '',
			description: network.description ?? ''
		};
		formErrors = {};
		formModalOpen = true;
	}

	function closeFormModal() {
		formModalOpen = false;
		editingNetwork = null;
		formData = { name: '', description: '' };
		formErrors = {};
	}

	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		const result = networkSchema.safeParse(formData);
		if (!result.success) {
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			if (editingNetwork?.id) {
				await api.networks.update(editingNetwork.id, result.data);
				addToast({ type: 'success', message: 'Network updated successfully' });
			} else {
				await api.networks.create(result.data);
				addToast({ type: 'success', message: 'Network created successfully' });
			}
			closeFormModal();
			await loadNetworks();
		} catch (err: unknown) {
			console.error('[NetworksAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save network';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	function openDeactivateModal(network: NetworkResponse) {
		deactivatingNetwork = network;
		deactivateModalOpen = true;
	}

	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingNetwork = null;
	}

	async function handleDeactivate() {
		if (!deactivatingNetwork?.id) return;
		try {
			await api.networks.deactivate(deactivatingNetwork.id);
			addToast({ type: 'success', message: t('networks.deactivate.success') });
			closeDeactivateModal();
			await loadNetworks();
		} catch (err: unknown) {
			console.error('[NetworksAdmin] Deactivate failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to deactivate network';
			addToast({ type: 'error', message });
		}
	}

	async function buildMergeSourceNetworks(
		items: MergeEntityOption[]
	): Promise<MergeEntityOption[]> {
		return Promise.all(
			items.map(async (item) => {
				try {
					return {
						...item,
						deviceCount: await fetchReferenceDeviceCount('network', item.id)
					};
				} catch (err: unknown) {
					console.error('[NetworksAdmin] Merge count failed:', err);
					return {
						...item,
						deviceCount: 0
					};
				}
			})
		);
	}

	async function openMergeModal(items: MergeEntityOption[], targets: MergeEntityOption[]) {
		mergeModalOpen = true;
		mergeError = null;
		mergeSubmitting = false;
		mergeSourceNetworks = items.map((item) => ({ ...item, deviceCount: null }));
		mergeTargetOptions = [...targets].sort((left, right) => left.name.localeCompare(right.name));
		mergeSourceNetworks = await buildMergeSourceNetworks(items);
	}

	function openBulkMergeModal() {
		if (!canBulkMerge) {
			return;
		}

		void openMergeModal(selectedActiveNetworkOptions, selectedActiveNetworkOptions);
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergeSourceNetworks = [];
		mergeTargetOptions = [];
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (mergeSourceNetworks.length === 0) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetNetwork = mergeTargetOptions.find((network) => network.id === targetId);
		const isBulkMerge = mergeSourceNetworks.length > 1;

		try {
			if (isBulkMerge) {
				const mergedCount = await mergeReferenceEntitySelection(
					'network',
					mergeSourceNetworks.map((network) => network.id),
					targetId
				);
				addToast({
					type: 'success',
					message: t('admin.bulk.mergeSuccess', {
						target: targetNetwork?.name ?? '',
						count: mergedCount
					})
				});
			} else {
				const sourceNetwork = mergeSourceNetworks[0];
				const response = await mergeReferenceEntities('network', {
					sourceId: sourceNetwork.id,
					targetId
				});
				addToast({
					type: 'success',
					message: t('admin.merge.success', {
						source: sourceNetwork.name,
						target: targetNetwork?.name ?? '',
						count: response.mergedCount
					})
				});
			}
			closeMergeModal();
			clearSelection();
			await Promise.all([loadNetworks(), fetchReferenceData({ force: true })]);
		} catch (err: unknown) {
			console.error('[NetworksAdmin] Merge failed:', err);
			mergeError = err instanceof Error ? err.message : t('admin.merge.error');
		} finally {
			mergeSubmitting = false;
		}
	}

	function handlePageChange(newPage: number, newPageSize: number) {
		const params = new URLSearchParams($page.url.searchParams);
		if (newPage !== 1) params.set('page', newPage.toString());
		else params.delete('page');
		if (newPageSize !== 25) params.set('pageSize', newPageSize.toString());
		else params.delete('pageSize');
		goto(`?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function setIndeterminate(node: HTMLInputElement, value: boolean) {
		node.indeterminate = value;
		return {
			update(next: boolean) {
				node.indeterminate = next;
			}
		};
	}

	function toggleSelect(id: string) {
		selectedIds = toggleReferenceSelection(selectedIds, id);
	}

	function toggleSelectAllVisible() {
		selectedIds = toggleAllVisibleReferenceSelections(selectedIds, visibleNetworkIds);
	}

	function clearSelection() {
		selectedIds = clearReferenceSelection();
	}

	async function handleBulkDeleteSuccess() {
		clearSelection();
		bulkDeleteModalOpen = false;
		await Promise.all([loadNetworks(), fetchReferenceData({ force: true })]);
	}

	const primaryActionButtonClass =
		'text-sm font-medium text-primary-600 hover:text-primary-700 hover:underline dark:text-primary-400 dark:hover:text-primary-300';
	const warningActionButtonClass =
		'text-sm font-medium text-warning-600 hover:text-warning-700 hover:underline dark:text-warning-400 dark:hover:text-warning-300';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<svelte:head>
	<title>{t('networks.list.title')} — {t('app.title')}</title>
</svelte:head>

<div class="-mt-8">
	<!-- Sticky page header -->
	<div class="sticky top-[73px] z-20 -mx-4 sm:-mx-6 lg:-mx-8 border-b border-neutral-200 bg-white dark:border-neutral-800 dark:bg-neutral-900">
		<div class="flex items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
			<h1 class="text-xl font-bold text-neutral-900 dark:text-neutral-50 sm:text-2xl">
				{t('networks.list.title')}
			</h1>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('networks.list.addButton')}
			</button>
		</div>
	</div>

	{#if !loading && !error && networks.length > 0}
		<div class="mt-6 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
			<div class="overflow-x-auto">
				<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
					<caption class="sr-only">{t('networks.list.title')}</caption>
					<thead class="bg-neutral-50 dark:bg-neutral-900">
						<tr>
							<th scope="col" class="w-12 px-4 py-3 text-left">
								<input
									type="checkbox"
									class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
									checked={allVisibleSelected}
									use:setIndeterminate={!allVisibleSelected && someVisibleSelected}
									onchange={toggleSelectAllVisible}
									aria-label={t('admin.bulk.selectAllVisible')}
								/>
							</th>
							<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('networks.columns.name')}</th>
							<th scope="col" class="hidden px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300 sm:table-cell">{t('networks.columns.description')}</th>
							<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('common.labels.actions')}</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
						{#each networks as network (network.id ?? network.name ?? '')}
							{@const selected = network.id ? selectedIds.has(network.id) : false}
							<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900 {selected ? 'bg-primary-50 dark:bg-primary-950/30' : ''}">
								<td class="w-12 px-4 py-3">
									{#if network.id}
										<input
											type="checkbox"
											class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
											checked={selected}
											onchange={() => toggleSelect(network.id ?? '')}
											aria-label={t('admin.bulk.selectRow', { name: network.name ?? '' })}
										/>
									{/if}
								</td>
								<td class="px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">
									{network.name}
									{#if !network.isActive}
										<span class="ml-2 inline-flex rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400">{t('common.states.inactive')}</span>
									{/if}
								</td>
								<td class="hidden px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300 sm:table-cell">{network.description || '—'}</td>
								<td class="px-4 py-3 text-right">
									<div class="flex items-center justify-end gap-3">
										{@render networkActionButtons(network)}
									</div>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</div>

		<div class="mt-6">
			<PaginationControls
				currentPage={urlParams.page}
				pageSize={urlParams.pageSize}
				{totalCount}
				onPageChange={handlePageChange}
				itemLabel={t('common.nouns.networks')}
			/>
		</div>
	{:else if loading}
		<div class="mt-6">
			<LoadingSkeleton />
		</div>
	{:else if error}
		<div class="mt-6">
			<ErrorState {error} onRetry={loadNetworks} />
		</div>
	{:else if networks.length === 0}
		<div class="mt-6">
			<EmptyState message={t('networks.list.emptyState')} showAddAction={false} />
		</div>
	{/if}
</div>

{#snippet networkActionButtons(network: NetworkResponse)}
	<button type="button" onclick={() => openEditModal(network)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if network.isActive}
		<button type="button" onclick={() => openDeactivateModal(network)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

{#if mergeModalOpen && mergeSourceNetworks.length > 0}
	<MergeEntityModal
		entityType="network"
		sourceEntity={mergeSourceNetworks[0] ?? null}
		sourceEntities={mergeSourceNetworks}
		entities={mergeTargetOptions}
		isOpen={mergeModalOpen}
		isSubmitting={mergeSubmitting}
		errorMessage={mergeError}
		onConfirm={handleMergeConfirm}
		onCancel={closeMergeModal}
	/>
{/if}

{#if bulkDeleteModalOpen}
	<BulkDeleteReferenceModal
		entityType="network"
		items={selectedNetworkOptions}
		isOpen={bulkDeleteModalOpen}
		onDeleted={handleBulkDeleteSuccess}
		onCancel={() => (bulkDeleteModalOpen = false)}
	/>
{/if}

<ReferenceDataBulkBar
	count={selectedIds.size}
	onClear={clearSelection}
	onDelete={() => (bulkDeleteModalOpen = true)}
	onMerge={openBulkMergeModal}
	mergeDisabled={!canBulkMerge}
/>

{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="network-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="network-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingNetwork ? t('networks.edit.title') : t('networks.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<div class="mb-4">
					<label
						for="network-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('networks.fields.name')}
					</label>
					<input
						id="network-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('networks.fields.namePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<div class="mb-6">
					<label
						for="network-description"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('networks.fields.description')}
					</label>
					<textarea
						id="network-description"
						bind:value={formData.description}
						placeholder={t('networks.fields.descriptionPlaceholder')}
						rows="3"
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.description}
					></textarea>
					{#if formErrors.description}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.description}</p>
					{/if}
				</div>

				<div class="flex justify-end gap-3">
					<button
						type="button"
						onclick={closeFormModal}
						disabled={formSubmitting}
						class="inline-flex min-h-11 items-center justify-center rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('common.actions.cancel')}
					</button>
					<button
						type="submit"
						disabled={formSubmitting}
						class="inline-flex min-h-11 items-center justify-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-700 dark:hover:bg-primary-800"
					>
						{formSubmitting ? t('common.states.loading') : t('common.actions.save')}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

{#if deactivateModalOpen && deactivatingNetwork}
	<DeactivateConfirmModal
		entityName={deactivatingNetwork.name ?? ''}
		entityType="network"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

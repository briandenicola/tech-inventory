<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { BrandResponse } from '$lib/api/types';
	import { brandSchema, type BrandFormData } from '$lib/schemas/brand';
	import { addToast } from '$lib/stores/toast';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { fetchReferenceData } from '$lib/stores/referenceData';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
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
	 * T27: Brands Admin — paginated list with Add/Edit/Deactivate
	 *
	 * Features:
	 * - Paginated list (pageSize 25)
	 * - Add/Edit modal with Zod validation
	 * - Deactivate with confirmation
	 * - Show Inactive toggle
	 * - Admin role gate (enforced by layout + backend)
	 *
	 * Decision: D-091 (Inline edit modal vs separate route — chose inline modal for all 4 admin pages)
	 */

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	// Check admin role client-side (belt-and-suspenders — backend also enforces)
	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

	// URL params
	const urlParams = $derived.by(() => {
		const params = $page.url.searchParams;
		return {
			page: parseInt(params.get('page') || '1', 10),
			pageSize: parseInt(params.get('pageSize') || '25', 10),
			includeInactive: params.get('includeInactive') === 'true'
		};
	});

	// Brands query state
	let brands = $state<BrandResponse[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	// Modal states
	let formModalOpen = $state(false);
	let editingBrand = $state<BrandResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingBrand = $state<BrandResponse | null>(null);
	let mergeModalOpen = $state(false);
	let mergeSourceBrands = $state<MergeEntityOption[]>([]);
	let mergeTargetOptions = $state<MergeEntityOption[]>([]);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);
	let selectedIds = $state<Set<string>>(new Set());
	let bulkDeleteModalOpen = $state(false);

	// Form state
	let formData = $state<BrandFormData>({ name: '', website: '', notes: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	const visibleBrandIds= $derived(
		brands.map((brand) => brand.id).filter((brandId): brandId is string => !!brandId)
	);
	const selectionState = $derived(getVisibleReferenceSelectionState(selectedIds, visibleBrandIds));
	const allVisibleSelected = $derived(selectionState.allVisibleSelected);
	const someVisibleSelected = $derived(selectionState.someVisibleSelected);
	const selectedBrands = $derived.by(() =>
		brands.filter(
			(brand): brand is BrandResponse & { id: string; name: string } =>
				!!brand.id && !!brand.name && selectedIds.has(brand.id)
		)
	);
	const selectedBrandOptions = $derived(
		selectedBrands.map((brand) => ({ id: brand.id, name: brand.name }))
	);
	const selectedActiveBrandOptions = $derived(
		selectedBrands
			.filter((brand) => brand.isActive)
			.map((brand) => ({ id: brand.id, name: brand.name }))
	);
	const canBulkMerge = $derived(
		selectedActiveBrandOptions.length >= 2 && selectedActiveBrandOptions.length === selectedBrands.length
	);

	// Load brands on mount + URL params change
	$effect(() => {
		loadBrands();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadBrands);
		return unregister;
	});

	$effect(() => {
		void urlParams.page;
		void urlParams.pageSize;
		void urlParams.includeInactive;
		selectedIds = clearReferenceSelection();
	});

	async function loadBrands() {
		loading = true;
		error = null;
		try {
			const response = await api.brands.list(urlParams);
			brands = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load brands';
		} finally {
			loading = false;
		}
	}

	// Open add modal
	function openAddModal() {
		editingBrand = null;
		formData = { name: '', website: '', notes: '' };
		formErrors = {};
		formModalOpen = true;
	}

	// Open edit modal
	function openEditModal(brand: BrandResponse) {
		editingBrand = brand;
		formData = {
			name: brand.name ?? '',
			website: brand.website ?? '',
			notes: brand.notes ?? ''
		};
		formErrors = {};
		formModalOpen = true;
	}

	// Close form modal
	function closeFormModal() {
		formModalOpen = false;
		editingBrand = null;
		formData = { name: '', website: '', notes: '' };
		formErrors = {};
	}

	// Submit form (create or update)
	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		// Zod validation
		const result = brandSchema.safeParse(formData);
		if (!result.success) {
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			if (editingBrand?.id) {
				// Update
				await api.brands.update(editingBrand.id, result.data);
				addToast({ type: 'success', message: 'Brand updated successfully' });
			} else {
				// Create
				await api.brands.create(result.data);
				addToast({ type: 'success', message: 'Brand created successfully' });
			}
			closeFormModal();
			await loadBrands();
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save brand';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	// Open deactivate modal
	function openDeactivateModal(brand: BrandResponse) {
		deactivatingBrand = brand;
		deactivateModalOpen = true;
	}

	// Close deactivate modal
	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingBrand = null;
	}

	// Confirm deactivate
	async function handleDeactivate() {
		if (!deactivatingBrand?.id) return;
		try {
			await api.brands.deactivate(deactivatingBrand.id);
			addToast({ type: 'success', message: t('brands.deactivate.success') });
			closeDeactivateModal();
			await loadBrands();
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Deactivate failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to deactivate brand';
			addToast({ type: 'error', message });
		}
	}

	async function buildMergeSourceBrands(items: MergeEntityOption[]): Promise<MergeEntityOption[]> {
		return Promise.all(
			items.map(async (item) => {
				try {
					return {
						...item,
						deviceCount: await fetchReferenceDeviceCount('brand', item.id)
					};
				} catch (err: unknown) {
					console.error('[BrandsAdmin] Merge count failed:', err);
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
		mergeSourceBrands = items.map((item) => ({ ...item, deviceCount: null }));
		mergeTargetOptions = [...targets].sort((left, right) => left.name.localeCompare(right.name));
		mergeSourceBrands = await buildMergeSourceBrands(items);
	}

	function openBulkMergeModal() {
		if (!canBulkMerge) {
			return;
		}

		void openMergeModal(selectedActiveBrandOptions, selectedActiveBrandOptions);
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergeSourceBrands = [];
		mergeTargetOptions = [];
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (mergeSourceBrands.length === 0) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetBrand = mergeTargetOptions.find((brand) => brand.id === targetId);
		const isBulkMerge = mergeSourceBrands.length > 1;

		try {
			if (isBulkMerge) {
				const mergedCount = await mergeReferenceEntitySelection(
					'brand',
					mergeSourceBrands.map((brand) => brand.id),
					targetId
				);
				addToast({
					type: 'success',
					message: t('admin.bulk.mergeSuccess', {
						target: targetBrand?.name ?? '',
						count: mergedCount
					})
				});
			} else {
				const sourceBrand = mergeSourceBrands[0];
				const response = await mergeReferenceEntities('brand', {
					sourceId: sourceBrand.id,
					targetId
				});
				addToast({
					type: 'success',
					message: t('admin.merge.success', {
						source: sourceBrand.name,
						target: targetBrand?.name ?? '',
						count: response.mergedCount
					})
				});
			}
			closeMergeModal();
			clearSelection();
			await Promise.all([loadBrands(), fetchReferenceData()]);
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Merge failed:', err);
			mergeError = err instanceof Error ? err.message : t('admin.merge.error');
		} finally {
			mergeSubmitting = false;
		}
	}

	// Pagination handler
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
		selectedIds = toggleAllVisibleReferenceSelections(selectedIds, visibleBrandIds);
	}

	function clearSelection() {
		selectedIds = clearReferenceSelection();
	}

	async function handleBulkDeleteSuccess() {
		clearSelection();
		bulkDeleteModalOpen = false;
		await Promise.all([loadBrands(), fetchReferenceData()]);
	}

	const primaryActionButtonClass =
		'text-sm font-medium text-primary-600 hover:text-primary-700 hover:underline dark:text-primary-400 dark:hover:text-primary-300';
	const warningActionButtonClass =
		'text-sm font-medium text-warning-600 hover:text-warning-700 hover:underline dark:text-warning-400 dark:hover:text-warning-300';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<svelte:head>
	<title>{t('brands.list.title')} — {t('app.title')}</title>
</svelte:head>

<div class="-mt-8">
	<!-- Sticky page header -->
	<div class="sticky top-[73px] z-30 -mx-4 sm:-mx-6 lg:-mx-8 border-b border-neutral-200/70 bg-white/85 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-900/85 md:top-[142px]">
		<div class="flex items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
			<h1 class="text-xl font-bold text-neutral-900 dark:text-neutral-50 sm:text-2xl">
				{t('brands.list.title')}
			</h1>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('brands.list.addButton')}
			</button>
		</div>
	</div>

	<!-- Loading -->
	{#if loading}
		<div class="mt-6">
			<LoadingSkeleton />
		</div>
	{:else if error}
		<div class="mt-6">
			<ErrorState {error} onRetry={loadBrands} />
		</div>
	{:else if brands.length === 0}
		<div
			class="mt-6 flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950"
		>
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
					d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
				/>
			</svg>
			<p class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
				{t('brands.list.emptyState')}
			</p>
		</div>
	{:else}
		<div class="mt-6 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
			<div class="overflow-x-auto">
				<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
					<caption class="sr-only">{t('brands.list.title')}</caption>
					<thead class="bg-neutral-50 dark:bg-neutral-900">
						<tr>
							<th scope="col" class="w-12 px-4 py-3 text-left">
								<input
									type="checkbox"
									class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
									checked={allVisibleSelected}
									use:setIndeterminate={!allVisibleSelected && someVisibleSelected}
									onchange={toggleSelectAllVisible}
									aria-label={t('admin.bulk.selectAllVisible')}
								/>
							</th>
							<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
								{t('brands.columns.name')}
							</th>
							<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
								{t('common.labels.actions')}
							</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
						{#each brands as brand (brand.id ?? brand.name ?? '')}
							{@const selected = brand.id ? selectedIds.has(brand.id) : false}
							<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900 {selected ? 'bg-primary-50 dark:bg-primary-950/30' : ''}">
								<td class="w-12 px-4 py-3">
									{#if brand.id}
										<input
											type="checkbox"
											class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
											checked={selected}
											onchange={() => toggleSelect(brand.id ?? '')}
											aria-label={t('admin.bulk.selectRow', { name: brand.name ?? '' })}
										/>
									{/if}
								</td>
								<td class="px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">
									{brand.name}
									{#if !brand.isActive}
										<span class="ml-2 inline-flex rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400">{t('common.states.inactive')}</span>
									{/if}
								</td>
								<td class="px-4 py-3 text-right">
									<div class="flex items-center justify-end gap-3">
										{@render brandActionButtons(brand)}
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
			/>
		</div>
	{/if}
</div>

{#snippet brandActionButtons(brand: BrandResponse)}
	<button type="button" onclick={() => openEditModal(brand)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if brand.isActive}
		<button type="button" onclick={() => openDeactivateModal(brand)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

{#if mergeModalOpen && mergeSourceBrands.length > 0}
	<MergeEntityModal
		entityType="brand"
		sourceEntity={mergeSourceBrands[0] ?? null}
		sourceEntities={mergeSourceBrands}
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
		entityType="brand"
		items={selectedBrandOptions}
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

<!-- Form Modal -->
{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="brand-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="brand-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingBrand ? t('brands.edit.title') : t('brands.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<!-- Name -->
				<div class="mb-4">
					<label
						for="brand-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('brands.fields.name')}
					</label>
					<input
						id="brand-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('brands.fields.namePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<!-- Website -->
				<div class="mb-4">
					<label
						for="brand-website"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('brands.fields.website')}
					</label>
					<input
						id="brand-website"
						type="text"
						bind:value={formData.website}
						placeholder={t('brands.fields.websitePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.website}
					/>
					{#if formErrors.website}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.website}</p>
					{/if}
				</div>

				<!-- Notes -->
				<div class="mb-6">
					<label
						for="brand-notes"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('brands.fields.notes')}
					</label>
					<textarea
						id="brand-notes"
						bind:value={formData.notes}
						placeholder={t('brands.fields.notesPlaceholder')}
						rows="3"
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.notes}
					></textarea>
					{#if formErrors.notes}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.notes}</p>
					{/if}
				</div>

				<!-- Actions -->
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

<!-- Deactivate Modal -->
{#if deactivateModalOpen && deactivatingBrand}
	<DeactivateConfirmModal
		entityName={deactivatingBrand.name ?? ''}
		entityType="brand"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

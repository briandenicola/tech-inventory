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
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import MergeEntityModal from '$lib/components/MergeEntityModal.svelte';
	import DeactivateConfirmModal from '$lib/components/admin/DeactivateConfirmModal.svelte';
	import {
		buildMergeTargetOptions,
		fetchMergeDeviceCount,
		mergeReferenceEntities,
		toMergeEntityOption,
		type MergeEntityOption
	} from '$lib/utils/referenceMerge';

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
	let mergingBrand = $state<MergeEntityOption | null>(null);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);

	// Form state
	let formData = $state<BrandFormData>({ name: '', website: '', notes: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	const referenceBrands = $derived.by(() => {
		if ($referenceDataStore.brands.length > 0) {
			return $referenceDataStore.brands;
		}

		return brands
			.filter((brand): brand is BrandResponse & { id: string; name: string } => !!brand.id && !!brand.name && !!brand.isActive)
			.map((brand) => ({ id: brand.id, name: brand.name }));
	});
	const mergeTargetOptions = $derived.by(() =>
		mergingBrand ? buildMergeTargetOptions(referenceBrands, mergingBrand.id) : []
	);

	// Load brands on mount + URL params change
	$effect(() => {
		loadBrands();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadBrands);
		return unregister;
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

	async function openMergeModal(brand: BrandResponse) {
		const candidate = toMergeEntityOption(brand);
		if (!candidate) {
			return;
		}

		mergeModalOpen = true;
		mergeError = null;
		mergingBrand = { ...candidate, deviceCount: null };

		try {
			const deviceCount = await fetchMergeDeviceCount('brand', candidate.id);
			if (mergingBrand?.id === candidate.id) {
				mergingBrand = { ...candidate, deviceCount };
			}
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Merge count failed:', err);
			if (mergingBrand?.id === candidate.id) {
				mergingBrand = { ...candidate, deviceCount: 0 };
			}
		}
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergingBrand = null;
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (!mergingBrand?.id) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetBrand = mergeTargetOptions.find((brand) => brand.id === targetId);

		try {
			const response = await mergeReferenceEntities('brand', {
				sourceId: mergingBrand.id,
				targetId
			});
			addToast({
				type: 'success',
				message: t('admin.merge.success', {
					source: mergingBrand.name,
					target: targetBrand?.name ?? '',
					count: response.mergedCount
				})
			});
			closeMergeModal();
			await Promise.all([loadBrands(), fetchReferenceData()]);
		} catch (err: unknown) {
			console.error('[BrandsAdmin] Merge failed:', err);
			mergeError = err instanceof Error ? err.message : t('admin.merge.error');
		} finally {
			mergeSubmitting = false;
		}
	}

	// Toggle inactive
	function toggleInactive() {
		const params = new URLSearchParams($page.url.searchParams);
		if (urlParams.includeInactive) {
			params.delete('includeInactive');
		} else {
			params.set('includeInactive', 'true');
		}
		goto(`?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
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
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<!-- Header -->
	<div class="mb-6 flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
				{t('brands.list.title')}
			</h1>
		</div>
		<div class="flex items-center gap-3">
			<!-- Show Inactive Toggle -->
			<label class="flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
				<input
					type="checkbox"
					checked={urlParams.includeInactive}
					onchange={toggleInactive}
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				/>
				{t('brands.list.showInactive')}
			</label>
			<!-- Add Button -->
			<button
				type="button"
				onclick={openAddModal}
				class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800"
			>
				{t('brands.list.addButton')}
			</button>
		</div>
	</div>

	<!-- Loading -->
	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState {error} onRetry={loadBrands} />
	{:else if brands.length === 0}
		<div
			class="flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950"
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
		<!-- Table -->
		<div
			class="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950"
		>
			<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
				<thead class="bg-neutral-50 dark:bg-neutral-900">
					<tr>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('brands.columns.name')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('brands.columns.website')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('brands.columns.notes')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('common.labels.actions')}
						</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
					{#each brands as brand (brand.id)}
						<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
							<td
								class="whitespace-nowrap px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50"
							>
								{brand.name}
							</td>
							<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">
								{#if brand.website}
									<a
										href={brand.website}
										target="_blank"
										rel="noopener noreferrer"
										class="text-primary-600 hover:underline dark:text-primary-400"
									>
										{brand.website}
									</a>
								{:else}
									—
								{/if}
							</td>
							<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">
								{brand.notes || '—'}
							</td>
							<td class="whitespace-nowrap px-4 py-3 text-right text-sm">
								<button
									type="button"
									onclick={() => openEditModal(brand)}
									class="mr-3 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
								>
									{t('common.actions.edit')}
								</button>
								{#if brand.isActive}
									<button
										type="button"
										onclick={() => openMergeModal(brand)}
										class="mr-3 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
									>
										{t('common.actions.merge')}
									</button>
									<button
										type="button"
										onclick={() => openDeactivateModal(brand)}
										class="text-warning-600 hover:text-warning-700 dark:text-warning-400 dark:hover:text-warning-300"
									>
										{t('common.actions.deactivate')}
									</button>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<!-- Pagination -->
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

{#if mergeModalOpen && mergingBrand}
	<MergeEntityModal
		entityType="brand"
		sourceEntity={mergingBrand}
		entities={mergeTargetOptions}
		isOpen={mergeModalOpen}
		isSubmitting={mergeSubmitting}
		errorMessage={mergeError}
		onConfirm={handleMergeConfirm}
		onCancel={closeMergeModal}
	/>
{/if}

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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
						class="rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('common.actions.cancel')}
					</button>
					<button
						type="submit"
						disabled={formSubmitting}
						class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-700 dark:hover:bg-primary-800"
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

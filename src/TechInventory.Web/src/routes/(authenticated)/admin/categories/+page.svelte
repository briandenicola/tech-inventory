<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { CategoryResponse } from '$lib/api/types';
	import { categorySchema, type CategoryFormData } from '$lib/schemas/category';
	import { addToast } from '$lib/stores/toast';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { fetchReferenceData } from '$lib/stores/referenceData';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
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
		toggleReferenceSelection
	} from '$lib/utils/referenceSelection';

	/**
	 * T28: Categories Admin — tree view with expand/collapse
	 *
	 * Features:
	 * - Tree view with expand/collapse (flat list with depth indentation)
	 * - Add root or child (parent selector = searchable dropdown)
	 * - Edit: name, parent, icon (emoji string)
	 * - Deactivate: cascades to subtree (backend handles)
	 * - Show Inactive toggle
	 * - Search filter (collapses non-matching subtrees)
	 * - No pagination (tree view; load all)
	 *
	 * Decision: D-116 (tree component = flat list with depth-prefix indentation, not recursive)
	 * Decision: D-117 (parent selector = searchable dropdown, not tree-picker)
	 * Decision: D-118 (simple text filter — collapse non-matching subtrees)
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
			includeInactive: params.get('includeInactive') === 'true'
		};
	});

	// Categories state
	let categories = $state<CategoryResponse[]>([]);
	let loading = $state(true);
	let error = $state<string | null>(null);

	// Search state
	let searchQuery = $state('');
	let expandedIds = $state<Set<string>>(new Set());

	// Modal states
	let formModalOpen = $state(false);
	let editingCategory = $state<CategoryResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingCategory = $state<CategoryResponse | null>(null);
	let mergeModalOpen = $state(false);
	let mergeSourceCategories = $state<MergeEntityOption[]>([]);
	let mergeTargetOptions = $state<MergeEntityOption[]>([]);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);
	let selectedIds = $state<Set<string>>(new Set());
	let bulkDeleteModalOpen = $state(false);

	// Form state
	let formData = $state<CategoryFormData>({ name: '', parentId: '', icon: '', depth: 1 });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);



	// Load categories on mount + URL params change
	$effect(() => {
		loadCategories();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadCategories);
		return unregister;
	});

	$effect(() => {
		void urlParams.includeInactive;
		void searchQuery;
		selectedIds = clearReferenceSelection();
	});

	async function loadCategories() {
		loading = true;
		error = null;
		try {
			const response = await api.categories.list({ includeInactive: urlParams.includeInactive });
			categories = response.items ?? [];
			// Auto-expand all on load
			expandedIds = new Set(categories.map((c) => c.id).filter((id): id is string => !!id));
		} catch (err: unknown) {
			console.error('[CategoriesAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load categories';
		} finally {
			loading = false;
		}
	}

	// Filtered & tree-structured categories
	const displayedCategories = $derived.by(() => {
		if (!searchQuery.trim()) return categories;

		const lowerQuery = searchQuery.toLowerCase();
		const matchingIds = new Set<string>();
		const ancestorIds = new Set<string>();

		// Find matches
		categories.forEach((cat) => {
			if (cat.name?.toLowerCase().includes(lowerQuery) && cat.id) {
				matchingIds.add(cat.id);
				// Add all ancestors
				let current = categories.find((c) => c.id === cat.parentId);
				while (current?.id) {
					ancestorIds.add(current.id);
					current = categories.find((c) => c.id === current?.parentId);
				}
			}
		});

		return categories.filter((c) => (c.id ? matchingIds.has(c.id) || ancestorIds.has(c.id) : false));
	});
	const selectedCategories = $derived.by(() =>
		displayedCategories.filter(
			(category): category is CategoryResponse & { id: string; name: string } =>
				!!category.id && !!category.name && selectedIds.has(category.id)
		)
	);
	const selectedCategoryOptions = $derived(
		selectedCategories.map((category) => ({ id: category.id, name: category.name }))
	);
	const selectedActiveCategoryOptions = $derived(
		selectedCategories
			.filter((category) => category.isActive)
			.map((category) => ({ id: category.id, name: category.name }))
	);
	const canBulkMerge = $derived(
		selectedActiveCategoryOptions.length >= 2 &&
			selectedActiveCategoryOptions.length === selectedCategories.length
	);

	// Open add modal
	function openAddModal() {
		editingCategory = null;
		formData = { name: '', parentId: '', icon: '', depth: 1 };
		formErrors = {};
		formModalOpen = true;
	}

	// Open edit modal
	function openEditModal(category: CategoryResponse) {
		editingCategory = category;
		formData = {
			name: category.name ?? '',
			parentId: category.parentId ?? '',
			icon: category.icon ?? '',
			depth: category.depth ?? 1
		};
		formErrors = {};
		formModalOpen = true;
	}

	// Close form modal
	function closeFormModal() {
		formModalOpen = false;
		editingCategory = null;
		formData = { name: '', parentId: '', icon: '', depth: 1 };
		formErrors = {};
	}

	// Submit form (create or update)
	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		// Zod validation
		const result = categorySchema.safeParse(formData);
		if (!result.success) {
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			const payload = {
				name: result.data.name,
				parentId: result.data.parentId || null,
				icon: result.data.icon || null,
				depth: result.data.depth
			};

			if (editingCategory?.id) {
				// Update
				await api.categories.update(editingCategory.id, payload);
				addToast({ type: 'success', message: t('admin.categories.edit.success') });
			} else {
				// Create
				await api.categories.create(payload);
				addToast({ type: 'success', message: t('admin.categories.create.success') });
			}
			closeFormModal();
			await loadCategories();
		} catch (err: unknown) {
			console.error('[CategoriesAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save category';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	// Open deactivate modal
	function openDeactivateModal(category: CategoryResponse) {
		deactivatingCategory = category;
		deactivateModalOpen = true;
	}

	// Close deactivate modal
	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingCategory = null;
	}

	// Confirm deactivate
	async function handleDeactivate() {
		if (!deactivatingCategory?.id) return;
		try {
			await api.categories.deactivate(deactivatingCategory.id);
			addToast({ type: 'success', message: t('admin.categories.deactivate.success') });
			closeDeactivateModal();
			await loadCategories();
		} catch (err: unknown) {
			console.error('[CategoriesAdmin] Deactivate failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to deactivate category';
			addToast({ type: 'error', message });
		}
	}

	async function buildMergeSourceCategories(
		items: MergeEntityOption[]
	): Promise<MergeEntityOption[]> {
		return Promise.all(
			items.map(async (item) => {
				try {
					return {
						...item,
						deviceCount: await fetchReferenceDeviceCount('category', item.id)
					};
				} catch (err: unknown) {
					console.error('[CategoriesAdmin] Merge count failed:', err);
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
		mergeSourceCategories = items.map((item) => ({ ...item, deviceCount: null }));
		mergeTargetOptions = [...targets].sort((left, right) => left.name.localeCompare(right.name));
		mergeSourceCategories = await buildMergeSourceCategories(items);
	}

	function openBulkMergeModal() {
		if (!canBulkMerge) {
			return;
		}

		void openMergeModal(selectedActiveCategoryOptions, selectedActiveCategoryOptions);
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergeSourceCategories = [];
		mergeTargetOptions = [];
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (mergeSourceCategories.length === 0) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetCategory = mergeTargetOptions.find((category) => category.id === targetId);
		const isBulkMerge = mergeSourceCategories.length > 1;

		try {
			if (isBulkMerge) {
				const mergedCount = await mergeReferenceEntitySelection(
					'category',
					mergeSourceCategories.map((category) => category.id),
					targetId
				);
				addToast({
					type: 'success',
					message: t('admin.bulk.mergeSuccess', {
						target: targetCategory?.name ?? '',
						count: mergedCount
					})
				});
			} else {
				const sourceCategory = mergeSourceCategories[0];
				const response = await mergeReferenceEntities('category', {
					sourceId: sourceCategory.id,
					targetId
				});
				addToast({
					type: 'success',
					message: t('admin.merge.success', {
						source: sourceCategory.name,
						target: targetCategory?.name ?? '',
						count: response.mergedCount
					})
				});
			}
			closeMergeModal();
			clearSelection();
			await Promise.all([loadCategories(), fetchReferenceData({ force: true })]);
		} catch (err: unknown) {
			console.error('[CategoriesAdmin] Merge failed:', err);
			mergeError = err instanceof Error ? err.message : t('admin.merge.error');
		} finally {
			mergeSubmitting = false;
		}
	}

	function toggleSelect(id: string) {
		selectedIds = toggleReferenceSelection(selectedIds, id);
	}

	function clearSelection() {
		selectedIds = clearReferenceSelection();
	}

	async function handleBulkDeleteSuccess() {
		clearSelection();
		bulkDeleteModalOpen = false;
		await Promise.all([loadCategories(), fetchReferenceData({ force: true })]);
	}

	// Toggle expand/collapse
	function toggleExpanded(id: string) {
		if (expandedIds.has(id)) {
			expandedIds.delete(id);
		} else {
			expandedIds.add(id);
		}
		expandedIds = new Set(expandedIds);
	}

	// Parent change handler
	function handleParentChange(e: Event) {
		const target = e.target as HTMLSelectElement;
		const selectedId = target.value;
		formData.parentId = selectedId;
		if (selectedId) {
			const parent = categories.find((c) => c.id === selectedId);
			formData.depth = parent?.depth ? parent.depth + 1 : 1;
		} else {
			formData.depth = 1;
		}
	}

	// Check if category has children
	function hasChildren(categoryId: string) {
		return displayedCategories.some((c) => c.parentId === categoryId);
	}

	const primaryActionButtonClass =
		'text-sm font-medium text-primary-600 hover:text-primary-700 hover:underline dark:text-primary-400 dark:hover:text-primary-300';
	const warningActionButtonClass =
		'text-sm font-medium text-warning-600 hover:text-warning-700 hover:underline dark:text-warning-400 dark:hover:text-warning-300';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<svelte:head>
	<title>{t('admin.categories.list.title')} — {t('app.title')}</title>
</svelte:head>

<div class="-mt-8">
	<!-- Sticky page header -->
	<div class="sticky top-[73px] z-20 -mx-4 sm:-mx-6 lg:-mx-8 border-b border-neutral-200 bg-white dark:border-neutral-800 dark:bg-neutral-900">
		<div class="flex items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
			<h1 class="text-xl font-bold text-neutral-900 dark:text-neutral-50 sm:text-2xl">
				{t('admin.categories.list.title')}
			</h1>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('admin.categories.list.addButton')}
			</button>
		</div>
	</div>

	<!-- Search -->
	<div class="mt-6 mb-4">
		<input
			type="text"
			bind:value={searchQuery}
			placeholder={t('admin.categories.list.searchPlaceholder')}
			class="block min-h-11 w-full rounded-md border border-neutral-300 px-4 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
		/>
	</div>

	<!-- Loading -->
	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState {error} onRetry={loadCategories} />
	{:else if displayedCategories.length === 0}
		<EmptyState message={t('admin.categories.list.emptyState')} showAddAction={false}>
			{#snippet icon()}
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
						d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"
					/>
				</svg>
			{/snippet}
		</EmptyState>
	{:else}
		<div class="mt-6 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950">
			<div class="divide-y divide-neutral-200 dark:divide-neutral-800">
				{#each displayedCategories.filter((c) => c.parentId === null) as category (category.id)}
					{@render categoryRow(category, 0)}
					{#if expandedIds.has(category.id ?? '')}
						{#each displayedCategories.filter((c) => c.parentId === category.id) as child (child.id)}
							{@render categoryRow(child, 1)}
							{#if expandedIds.has(child.id ?? '')}
								{#each displayedCategories.filter((c) => c.parentId === child.id) as grandchild (grandchild.id)}
									{@render categoryRow(grandchild, 2)}
								{/each}
							{/if}
						{/each}
					{/if}
				{/each}
			</div>
		</div>
	{/if}
</div>

{#snippet categoryActionButtons(category: CategoryResponse)}
	<button type="button" onclick={() => openEditModal(category)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if category.isActive}
		<button type="button" onclick={() => openDeactivateModal(category)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

{#if mergeModalOpen && mergeSourceCategories.length > 0}
	<MergeEntityModal
		entityType="category"
		sourceEntity={mergeSourceCategories[0] ?? null}
		sourceEntities={mergeSourceCategories}
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
		entityType="category"
		items={selectedCategoryOptions}
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
		aria-labelledby="category-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="category-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingCategory ? t('admin.categories.edit.title') : t('admin.categories.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<!-- Name -->
				<div class="mb-4">
					<label
						for="category-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.categories.fields.name')} <span class="text-error-600">*</span>
					</label>
					<input
						id="category-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('admin.categories.fields.namePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<!-- Parent -->
				<div class="mb-4">
					<label
						for="category-parent"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.categories.fields.parent')}
					</label>
					<select
						id="category-parent"
						value={formData.parentId}
						onchange={handleParentChange}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.parentId}
					>
						<option value="">{t('admin.categories.fields.parentNone')}</option>
						{#each categories.filter((c) => c.isActive && (c.depth ?? 0) < 3 && c.id !== editingCategory?.id) as cat (cat.id)}
							<option value={cat.id}>
								{'\u00A0'.repeat(((cat.depth ?? 1) - 1) * 4)}{cat.icon || ''} {cat.name}
							</option>
						{/each}
					</select>
					{#if formErrors.parentId}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.parentId}</p>
					{/if}
				</div>

				<!-- Icon -->
				<div class="mb-6">
					<label
						for="category-icon"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.categories.fields.icon')}
					</label>
					<input
						id="category-icon"
						type="text"
						bind:value={formData.icon}
						placeholder={t('admin.categories.fields.iconPlaceholder')}
						maxlength="100"
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.icon}
					/>
					{#if formErrors.icon}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.icon}</p>
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
{#if deactivateModalOpen && deactivatingCategory}
	<DeactivateConfirmModal
		entityName={deactivatingCategory.name ?? ''}
		entityType="category"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

{#snippet categoryRow(category: CategoryResponse, level: number)}
	{@const categoryId = category.id ?? ''}
	{@const selected = categoryId ? selectedIds.has(categoryId) : false}
	<div class="flex items-center justify-between gap-3 px-4 py-3 hover:bg-neutral-50 dark:hover:bg-neutral-900 {selected ? 'bg-primary-50 dark:bg-primary-950/30' : ''}">
		<div class="flex min-w-0 items-center gap-2" style="padding-left: {level * 2}rem;">
			{#if categoryId}
				<input
					type="checkbox"
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
					checked={selected}
					onchange={() => toggleSelect(categoryId)}
					aria-label={t('admin.bulk.selectRow', { name: category.name ?? '' })}
				/>
			{/if}
			{#if hasChildren(categoryId)}
				<button
					type="button"
					onclick={() => toggleExpanded(categoryId)}
					class="flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-full text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-200 lg:h-6 lg:w-6"
					aria-label={expandedIds.has(categoryId) ? 'Collapse' : 'Expand'}
				>
					{#if expandedIds.has(categoryId)}
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M19 9l-7 7-7-7"
							/>
						</svg>
					{:else}
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M9 5l7 7-7 7"
							/>
						</svg>
					{/if}
				</button>
			{:else}
				<span class="w-11 flex-shrink-0 lg:w-6"></span>
			{/if}
			{#if category.icon}
				<span class="text-lg">{category.icon}</span>
			{/if}
			<span class="truncate text-sm font-medium text-neutral-900 dark:text-neutral-50">
				{category.name}
			</span>
			{#if !category.isActive}
				<span
					class="ml-2 inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200"
				>
					{t('common.states.inactive')}
				</span>
			{/if}
		</div>
		<div class="flex items-center justify-end gap-3">
			{@render categoryActionButtons(category)}
		</div>
	</div>
{/snippet}

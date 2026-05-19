<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import { categorySchema, type CategoryFormData } from '$lib/schemas/category';
	import { addToast } from '$lib/stores/toast';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import DeactivateConfirmModal from '$lib/components/admin/DeactivateConfirmModal.svelte';

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
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let categories = $state<any[]>([]);
	let loading = $state(true);
	let error = $state<string | null>(null);

	// Search state
	let searchQuery = $state('');
	let expandedIds = $state<Set<string>>(new Set());

	// Modal states
	let formModalOpen = $state(false);
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let editingCategory = $state<any | null>(null);
	let deactivateModalOpen = $state(false);
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let deactivatingCategory = $state<any | null>(null);

	// Form state
	let formData = $state<CategoryFormData>({ name: '', parentId: '', icon: '', depth: 1 });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	// Load categories on mount + URL params change
	$effect(() => {
		loadCategories();
	});

	async function loadCategories() {
		loading = true;
		error = null;
		try {
			const response = await api.categories.list({ includeInactive: urlParams.includeInactive });
			categories = response.items || [];
			// Auto-expand all on load
			expandedIds = new Set(categories.map((c: any) => c.id));
		} catch (err: any) {
			console.error('[CategoriesAdmin] Load failed:', err);
			error = err.message || 'Failed to load categories';
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
		categories.forEach((cat: any) => {
			if (cat.name.toLowerCase().includes(lowerQuery)) {
				matchingIds.add(cat.id);
				// Add all ancestors
				let current = categories.find((c: any) => c.id === cat.parentId);
				while (current) {
					ancestorIds.add(current.id);
					current = categories.find((c: any) => c.id === current?.parentId);
				}
			}
		});

		return categories.filter((c: any) => matchingIds.has(c.id) || ancestorIds.has(c.id));
	});

	// Open add modal
	function openAddModal() {
		editingCategory = null;
		formData = { name: '', parentId: '', icon: '', depth: 1 };
		formErrors = {};
		formModalOpen = true;
	}

	// Open edit modal
	function openEditModal(category: any) {
		editingCategory = category;
		formData = {
			name: category.name || '',
			parentId: category.parentId || '',
			icon: category.icon || '',
			depth: category.depth || 1
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
			result.error.errors.forEach((err) => {
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

			if (editingCategory) {
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
		} catch (err: any) {
			console.error('[CategoriesAdmin] Submit failed:', err);
			addToast({ type: 'error', message: err.message || 'Failed to save category' });
		} finally {
			formSubmitting = false;
		}
	}

	// Open deactivate modal
	function openDeactivateModal(category: any) {
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
		if (!deactivatingCategory) return;
		try {
			await api.categories.deactivate(deactivatingCategory.id);
			addToast({ type: 'success', message: t('admin.categories.deactivate.success') });
			closeDeactivateModal();
			await loadCategories();
		} catch (err: any) {
			console.error('[CategoriesAdmin] Deactivate failed:', err);
			addToast({ type: 'error', message: err.message || 'Failed to deactivate category' });
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
			const parent = categories.find((c: any) => c.id === selectedId);
			formData.depth = parent ? parent.depth + 1 : 1;
		} else {
			formData.depth = 1;
		}
	}

	// Get visible children (for expand/collapse UI)
	function getVisibleChildren(parentId: string | null) {
		return displayedCategories.filter((c: any) => c.parentId === parentId);
	}

	// Check if category has children
	function hasChildren(categoryId: string) {
		return displayedCategories.some((c: any) => c.parentId === categoryId);
	}
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<!-- Header -->
	<div class="mb-6 flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
				{t('admin.categories.list.title')}
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
				{t('admin.categories.list.showInactive')}
			</label>
			<!-- Add Button -->
			<button
				type="button"
				onclick={openAddModal}
				class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800"
			>
				{t('admin.categories.list.addButton')}
			</button>
		</div>
	</div>

	<!-- Search -->
	<div class="mb-4">
		<input
			type="text"
			bind:value={searchQuery}
			placeholder={t('admin.categories.list.searchPlaceholder')}
			class="block w-full rounded-md border border-neutral-300 px-4 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
		/>
	</div>

	<!-- Loading -->
	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState error={error} onRetry={loadCategories} />
	{:else if displayedCategories.length === 0}
		<div class="flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950">
			<svg class="h-16 w-16 text-neutral-400 dark:text-neutral-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
			</svg>
			<p class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">{t('admin.categories.list.emptyState')}</p>
		</div>
	{:else}
		<!-- Tree View -->
		<div class="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950">
			<div class="divide-y divide-neutral-200 dark:divide-neutral-800">
				{#each displayedCategories.filter((c) => c.parentId === null) as category (category.id)}
					{@render categoryRow(category, 0)}
					{#if expandedIds.has(category.id)}
						{#each displayedCategories.filter((c) => c.parentId === category.id) as child (child.id)}
							{@render categoryRow(child, 1)}
							{#if expandedIds.has(child.id)}
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

<!-- Form Modal -->
{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="category-form-title"
	>
		<div class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900">
			<h2 id="category-form-title" class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50">
				{editingCategory ? t('admin.categories.edit.title') : t('admin.categories.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<!-- Name -->
				<div class="mb-4">
					<label for="category-name" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('admin.categories.fields.name')} <span class="text-error-600">*</span>
					</label>
					<input
						id="category-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('admin.categories.fields.namePlaceholder')}
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<!-- Parent -->
				<div class="mb-4">
					<label for="category-parent" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('admin.categories.fields.parent')}
					</label>
					<select
						id="category-parent"
						value={formData.parentId}
						onchange={handleParentChange}
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.parentId}
					>
						<option value="">{t('admin.categories.fields.parentNone')}</option>
						{#each categories.filter((c) => c.isActive && c.depth < 3 && c.id !== editingCategory?.id) as cat (cat.id)}
							<option value={cat.id}>
								{'\u00A0'.repeat((cat.depth - 1) * 4)}{cat.icon || ''} {cat.name}
							</option>
						{/each}
					</select>
					{#if formErrors.parentId}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.parentId}</p>
					{/if}
				</div>

				<!-- Icon -->
				<div class="mb-6">
					<label for="category-icon" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('admin.categories.fields.icon')}
					</label>
					<input
						id="category-icon"
						type="text"
						bind:value={formData.icon}
						placeholder={t('admin.categories.fields.iconPlaceholder')}
						maxlength="100"
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
{#if deactivateModalOpen && deactivatingCategory}
	<DeactivateConfirmModal
		entityName={deactivatingCategory.name}
		entityType="category"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

{#snippet categoryRow(category: any, level: number)}
	<div class="flex items-center justify-between px-4 py-3 hover:bg-neutral-50 dark:hover:bg-neutral-900">
		<div class="flex items-center gap-2" style="padding-left: {level * 2}rem;">
			{#if hasChildren(category.id)}
				<button
					type="button"
					onclick={() => toggleExpanded(category.id)}
					class="flex h-6 w-6 items-center justify-center text-neutral-500 hover:text-neutral-700 dark:text-neutral-400 dark:hover:text-neutral-200"
					aria-label={expandedIds.has(category.id) ? 'Collapse' : 'Expand'}
				>
					{#if expandedIds.has(category.id)}
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					{:else}
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
						</svg>
					{/if}
				</button>
			{:else}
				<span class="w-6"></span>
			{/if}
			{#if category.icon}
				<span class="text-lg">{category.icon}</span>
			{/if}
			<span class="text-sm font-medium text-neutral-900 dark:text-neutral-50">
				{category.name}
			</span>
			{#if !category.isActive}
				<span class="ml-2 inline-flex rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200">
					Inactive
				</span>
			{/if}
		</div>
		<div class="flex items-center gap-3">
			<button
				type="button"
				onclick={() => openEditModal(category)}
				class="text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
			>
				{t('common.actions.edit')}
			</button>
			{#if category.isActive}
				<button
					type="button"
					onclick={() => openDeactivateModal(category)}
					class="text-sm text-warning-600 hover:text-warning-700 dark:text-warning-400 dark:hover:text-warning-300"
				>
					Deactivate
				</button>
			{/if}
		</div>
	</div>
{/snippet}

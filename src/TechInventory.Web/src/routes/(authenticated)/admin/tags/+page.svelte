<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import { tagSchema, type TagFormData, TAG_PRESET_COLORS } from '$lib/schemas/tag';
	import { addToast } from '$lib/stores/toast';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import DeactivateConfirmModal from '$lib/components/admin/DeactivateConfirmModal.svelte';

	/**
	 * T32: Tags Admin — paginated list with Add/Edit/Deactivate
	 * Fields: name (required), color (hex, preset picker per D-089)
	 * Tag preview chip in list
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

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let tags = $state<any[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	let formModalOpen = $state(false);
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let editingTag = $state<any | null>(null);
	let deactivateModalOpen = $state(false);
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let deactivatingTag = $state<any | null>(null);

	let formData = $state<TagFormData>({ name: '', color: TAG_PRESET_COLORS[0] });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	$effect(() => {
		loadTags();
	});

	async function loadTags() {
		loading = true;
		error = null;
		try {
			const response = await api.tags.list(urlParams);
			tags = response.items || [];
			totalCount = response.totalCount || 0;
		} catch (err: any) {
			console.error('[TagsAdmin] Load failed:', err);
			error = err.message || 'Failed to load tags';
		} finally {
			loading = false;
		}
	}

	function openAddModal() {
		editingTag = null;
		formData = { name: '', color: TAG_PRESET_COLORS[0] };
		formErrors = {};
		formModalOpen = true;
	}

	function openEditModal(tag: any) {
		editingTag = tag;
		formData = {
			name: tag.name || '',
			color: tag.color || TAG_PRESET_COLORS[0]
		};
		formErrors = {};
		formModalOpen = true;
	}

	function closeFormModal() {
		formModalOpen = false;
		editingTag = null;
		formData = { name: '', color: TAG_PRESET_COLORS[0] };
		formErrors = {};
	}

	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		const result = tagSchema.safeParse(formData);
		if (!result.success) {
			result.error.errors.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			if (editingTag) {
				await api.tags.update(editingTag.id, result.data);
				addToast({ type: 'success', message: 'Tag updated successfully' });
			} else {
				await api.tags.create(result.data);
				addToast({ type: 'success', message: 'Tag created successfully' });
			}
			closeFormModal();
			await loadTags();
		} catch (err: any) {
			console.error('[TagsAdmin] Submit failed:', err);
			addToast({ type: 'error', message: err.message || 'Failed to save tag' });
		} finally {
			formSubmitting = false;
		}
	}

	function openDeactivateModal(tag: any) {
		deactivatingTag = tag;
		deactivateModalOpen = true;
	}

	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingTag = null;
	}

	async function handleDeactivate() {
		if (!deactivatingTag) return;
		try {
			await api.tags.deactivate(deactivatingTag.id);
			addToast({ type: 'success', message: t('tags.deactivate.success') });
			closeDeactivateModal();
			await loadTags();
		} catch (err: any) {
			console.error('[TagsAdmin] Deactivate failed:', err);
			addToast({ type: 'error', message: err.message || 'Failed to deactivate tag' });
		}
	}

	function toggleInactive() {
		const params = new URLSearchParams($page.url.searchParams);
		if (urlParams.includeInactive) {
			params.delete('includeInactive');
		} else {
			params.set('includeInactive', 'true');
		}
		goto(`?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function handlePageChange(newPage: number, newPageSize: number) {
		const params = new URLSearchParams($page.url.searchParams);
		if (newPage !== 1) params.set('page', newPage.toString());
		else params.delete('page');
		if (newPageSize !== 25) params.set('pageSize', newPageSize.toString());
		else params.delete('pageSize');
		goto(`?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
	}

	const totalPages = $derived(Math.ceil(totalCount / urlParams.pageSize));
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-6 flex items-center justify-between">
		<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
			{t('tags.list.title')}
		</h1>
		<div class="flex items-center gap-3">
			<label class="flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
				<input
					type="checkbox"
					checked={urlParams.includeInactive}
					onchange={toggleInactive}
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				/>
				{t('tags.list.showInactive')}
			</label>
			<button
				type="button"
				onclick={openAddModal}
				class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800"
			>
				{t('tags.list.addButton')}
			</button>
		</div>
	</div>

	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState error={error} onRetry={loadTags} />
	{:else if tags.length === 0}
		<div class="flex min-h-[400px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950">
<svg class="h-16 w-16 text-neutral-400 dark:text-neutral-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
</svg>
<p class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">{t('tags.list.emptyState')}</p>
</div>
	{:else}
		<div class="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950">
			<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
				<thead class="bg-neutral-50 dark:bg-neutral-900">
					<tr>
						<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
							{t('tags.columns.name')}
						</th>
						<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
							Preview
						</th>
						<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
							Actions
						</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
					{#each tags as tag (tag.id)}
						<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
							<td class="whitespace-nowrap px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">
								{tag.name}
							</td>
							<td class="px-4 py-3">
								<span
									class="inline-flex items-center rounded-full px-3 py-1 text-xs font-medium text-white"
									style="background-color: {tag.color}"
								>
									{tag.name}
								</span>
							</td>
							<td class="whitespace-nowrap px-4 py-3 text-right text-sm">
								<button
									type="button"
									onclick={() => openEditModal(tag)}
									class="mr-3 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
								>
									{t('common.actions.edit')}
								</button>
								{#if tag.isActive}
									<button
										type="button"
										onclick={() => openDeactivateModal(tag)}
										class="text-warning-600 hover:text-warning-700 dark:text-warning-400 dark:hover:text-warning-300"
									>
										Deactivate
									</button>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<div class="mt-6">
			<PaginationControls
				currentPage={urlParams.page}
				
				pageSize={urlParams.pageSize}
				totalCount={totalCount}
				onPageChange={handlePageChange}
			/>
		</div>
	{/if}
</div>

{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="tag-form-title"
	>
		<div class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900">
			<h2 id="tag-form-title" class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50">
				{editingTag ? t('tags.edit.title') : t('tags.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<div class="mb-4">
					<label for="tag-name" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('tags.fields.name')}
					</label>
					<input
						id="tag-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('tags.fields.namePlaceholder')}
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<div class="mb-6">
					<label class="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-2">
						{t('tags.fields.color')}
					</label>
					<div class="grid grid-cols-8 gap-2">
						{#each TAG_PRESET_COLORS as presetColor}
							<button
								type="button"
								onclick={() => (formData.color = presetColor)}
								class="h-10 w-full rounded-md border-2 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
								class:border-neutral-900={formData.color === presetColor}
								class:dark:border-neutral-50={formData.color === presetColor}
								class:border-neutral-300={formData.color !== presetColor}
								class:dark:border-neutral-600={formData.color !== presetColor}
								style="background-color: {presetColor}"
								aria-label="Select color {presetColor}"
							></button>
						{/each}
					</div>
					{#if formErrors.color}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.color}</p>
					{/if}
					<!-- Preview -->
					<div class="mt-3">
						<span
							class="inline-flex items-center rounded-full px-3 py-1 text-xs font-medium text-white"
							style="background-color: {formData.color}"
						>
							{formData.name || 'Tag Preview'}
						</span>
					</div>
				</div>

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

{#if deactivateModalOpen && deactivatingTag}
	<DeactivateConfirmModal
		entityName={deactivatingTag.name}
		entityType="tag"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}





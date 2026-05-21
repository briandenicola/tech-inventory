<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { TagResponse } from '$lib/api/types';
	import { tagSchema, type TagFormData, TAG_PRESET_COLORS } from '$lib/schemas/tag';
	import { addToast } from '$lib/stores/toast';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
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

	let tags = $state<TagResponse[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	let formModalOpen = $state(false);
	let editingTag = $state<TagResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingTag = $state<TagResponse | null>(null);

	let formData = $state<TagFormData>({ name: '', color: TAG_PRESET_COLORS[0] });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	$effect(() => {
		loadTags();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadTags);
		return unregister;
	});

	async function loadTags() {
		loading = true;
		error = null;
		try {
			const response = await api.tags.list(urlParams);
			tags = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[TagsAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load tags';
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

	function openEditModal(tag: TagResponse) {
		editingTag = tag;
		formData = {
			name: tag.name ?? '',
			color: tag.color ?? TAG_PRESET_COLORS[0]
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
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			if (editingTag?.id) {
				await api.tags.update(editingTag.id, result.data);
				addToast({ type: 'success', message: 'Tag updated successfully' });
			} else {
				await api.tags.create(result.data);
				addToast({ type: 'success', message: 'Tag created successfully' });
			}
			closeFormModal();
			await loadTags();
		} catch (err: unknown) {
			console.error('[TagsAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save tag';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	function openDeactivateModal(tag: TagResponse) {
		deactivatingTag = tag;
		deactivateModalOpen = true;
	}

	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingTag = null;
	}

	async function handleDeactivate() {
		if (!deactivatingTag?.id) return;
		try {
			await api.tags.deactivate(deactivatingTag.id);
			addToast({ type: 'success', message: t('tags.deactivate.success') });
			closeDeactivateModal();
			await loadTags();
		} catch (err: unknown) {
			console.error('[TagsAdmin] Deactivate failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to deactivate tag';
			addToast({ type: 'error', message });
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

	const primaryActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-primary-300 px-4 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-800 dark:text-primary-300 dark:hover:bg-primary-950';
	const warningActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-warning-300 px-4 py-2 text-sm font-medium text-warning-700 transition-colors hover:bg-warning-50 focus:outline-none focus:ring-2 focus:ring-warning-500 dark:border-warning-800 dark:text-warning-300 dark:hover:bg-warning-950';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<!-- Sticky page header -->
	<div class="sticky top-[73px] z-30 -mx-4 sm:-mx-6 lg:-mx-8 border-b border-neutral-200/70 bg-white/85 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-900/85 md:top-[142px]">
		<div class="flex items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
			<h1 class="text-xl font-bold text-neutral-900 dark:text-neutral-50 sm:text-2xl">
				{t('tags.list.title')}
			</h1>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('tags.list.addButton')}
			</button>
		</div>
	</div>

	{#if loading}
		<div class="mt-6">
			<LoadingSkeleton />
		</div>
	{:else if error}
		<div class="mt-6">
			<ErrorState {error} onRetry={loadTags} />
		</div>
	{:else if tags.length === 0}
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
				{t('tags.list.emptyState')}
			</p>
		</div>
	{:else}
		<div class="mt-6 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
			<div class="overflow-x-auto">
				<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
					<caption class="sr-only">{t('tags.list.title')}</caption>
					<thead class="bg-neutral-50 dark:bg-neutral-900">
						<tr>
							<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('tags.columns.name')}</th>
							<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('common.labels.actions')}</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
						{#each tags as tag (tag.id ?? tag.name ?? '')}
							<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
								<td class="px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">
									<span class="inline-flex items-center gap-2">
										{#if tag.color}
											<span class="inline-block h-3 w-3 rounded-full" style="background-color: {tag.color}"></span>
										{/if}
										{tag.name}
									</span>
									{#if !tag.isActive}
										<span class="ml-2 inline-flex rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400">{t('common.states.inactive')}</span>
									{/if}
								</td>
								<td class="px-4 py-3 text-right">
									<div class="flex flex-wrap justify-end gap-2">
										{@render tagActionButtons(tag)}
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

{#snippet tagActionButtons(tag: TagResponse)}
	<button type="button" onclick={() => openEditModal(tag)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if tag.isActive}
		<button type="button" onclick={() => openDeactivateModal(tag)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="tag-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="tag-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingTag ? t('tags.edit.title') : t('tags.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<div class="mb-4">
					<label
						for="tag-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('tags.fields.name')}
					</label>
					<input
						id="tag-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('tags.fields.namePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<fieldset class="mb-6 border-0 p-0">
					<legend class="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-2">
						{t('tags.fields.color')}
					</legend>
					<div class="grid grid-cols-8 gap-2">
						{#each TAG_PRESET_COLORS as presetColor}
							<button
								type="button"
								onclick={() => (formData.color = presetColor)}
								class="h-11 w-full rounded-md border-2 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
								class:border-neutral-900={formData.color === presetColor}
								class:dark:border-neutral-50={formData.color === presetColor}
								class:border-neutral-300={formData.color !== presetColor}
								class:dark:border-neutral-600={formData.color !== presetColor}
								style="background-color: {presetColor}"
								aria-label="Select color {presetColor}"
								aria-pressed={formData.color === presetColor}
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
				</fieldset>

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

{#if deactivateModalOpen && deactivatingTag}
	<DeactivateConfirmModal
		entityName={deactivatingTag.name ?? ''}
		entityType="tag"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api, { ApiError } from '$lib/api/client';
	import type { OwnerResponse } from '$lib/api/types';
	import { ownerSchema, type OwnerFormData } from '$lib/schemas/owner';
	import { addToast } from '$lib/stores/toast';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import DeactivateConfirmModal from '$lib/components/admin/DeactivateConfirmModal.svelte';
	import ResponsiveAdminList from '$lib/components/admin/ResponsiveAdminList.svelte';

	/**
	 * T29: Owners Admin — list with role badge + deactivate 409 guard
	 *
	 * Features:
	 * - Paginated list with role badge (Admin / Member / Viewer — colored chip)
	 * - Add: displayName, role, entraObjectId (optional UUID)
	 * - Edit: role change (also name/entraObjectId editable)
	 * - Deactivate: BLOCKS with friendly error if devices still reference (backend 409)
	 * - Show Inactive toggle
	 * - Inline modals
	 * - No pagination (typically <20 owners per household)
	 * - Role gate: Admin only
	 *
	 * Decision: D-119 (Owners role gate pattern — client redirect + backend enforce)
	 * Decision: D-120 (Owner deactivate 409 error display — toast with reason from backend)
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

	// Owners query state
	let owners = $state<OwnerResponse[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	// Modal states
	let formModalOpen = $state(false);
	let editingOwner = $state<OwnerResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingOwner = $state<OwnerResponse | null>(null);

	// Form state
	let formData = $state<OwnerFormData>({ displayName: '', role: 'Member', entraObjectId: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	// Load owners on mount + URL params change
	$effect(() => {
		loadOwners();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadOwners);
		return unregister;
	});

	async function loadOwners() {
		loading = true;
		error = null;
		try {
			const response = await api.owners.list(urlParams);
			owners = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[OwnersAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load owners';
		} finally {
			loading = false;
		}
	}

	// Open add modal
	function openAddModal() {
		editingOwner = null;
		formData = { displayName: '', role: 'Member', entraObjectId: '' };
		formErrors = {};
		formModalOpen = true;
	}

	// Open edit modal
	function openEditModal(owner: OwnerResponse) {
		editingOwner = owner;
		formData = {
			displayName: owner.displayName ?? '',
			role: (owner.role as OwnerFormData['role']) ?? 'Member',
			entraObjectId: owner.entraObjectId ?? ''
		};
		formErrors = {};
		formModalOpen = true;
	}

	// Close form modal
	function closeFormModal() {
		formModalOpen = false;
		editingOwner = null;
		formData = { displayName: '', role: 'Member', entraObjectId: '' };
		formErrors = {};
	}

	// Submit form (create or update)
	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		// Zod validation
		const result = ownerSchema.safeParse(formData);
		if (!result.success) {
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			const payload = {
				displayName: result.data.displayName,
				role: result.data.role,
				entraObjectId: result.data.entraObjectId || null
			};

			if (editingOwner?.id) {
				// Update
				await api.owners.update(editingOwner.id, payload);
				addToast({ type: 'success', message: t('admin.owners.edit.success') });
			} else {
				// Create
				await api.owners.create(payload);
				addToast({ type: 'success', message: t('admin.owners.create.success') });
			}
			closeFormModal();
			await loadOwners();
		} catch (err: unknown) {
			console.error('[OwnersAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save owner';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	// Open deactivate modal
	function openDeactivateModal(owner: OwnerResponse) {
		deactivatingOwner = owner;
		deactivateModalOpen = true;
	}

	// Close deactivate modal
	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingOwner = null;
	}

	// Confirm deactivate
	async function handleDeactivate() {
		if (!deactivatingOwner?.id) return;
		try {
			await api.owners.deactivate(deactivatingOwner.id);
			addToast({ type: 'success', message: t('admin.owners.deactivate.success') });
			closeDeactivateModal();
			await loadOwners();
		} catch (err: unknown) {
			console.error('[OwnersAdmin] Deactivate failed:', err);
			// Backend returns 409 if devices still reference this owner
			if (err instanceof ApiError && err.status === 409) {
				addToast({
					type: 'error',
					message: err.detail || 'Cannot deactivate owner: devices still reference this owner'
				});
			} else {
				const message = err instanceof Error ? err.message : 'Failed to deactivate owner';
				addToast({ type: 'error', message });
			}
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

	// Role badge color
	function getRoleBadgeClass(role: string | null | undefined) {
		if (role === 'Admin') {
			return 'bg-error-100 text-error-800 dark:bg-error-900 dark:text-error-200';
		}
		if (role === 'Member') {
			return 'bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-200';
		}
		return 'bg-neutral-100 text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200';
	}

	const primaryActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-primary-300 px-4 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-800 dark:text-primary-300 dark:hover:bg-primary-950';
	const warningActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-warning-300 px-4 py-2 text-sm font-medium text-warning-700 transition-colors hover:bg-warning-50 focus:outline-none focus:ring-2 focus:ring-warning-500 dark:border-warning-800 dark:text-warning-300 dark:hover:bg-warning-950';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<!-- Header -->
	<div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
		<div>
			<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
				{t('admin.owners.list.title')}
			</h1>
		</div>
		<div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
			<label class="flex min-h-11 items-center gap-3 text-sm text-neutral-700 dark:text-neutral-300">
				<input
					type="checkbox"
					checked={urlParams.includeInactive}
					onchange={toggleInactive}
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				/>
				{t('admin.owners.list.showInactive')}
			</label>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('admin.owners.list.addButton')}
			</button>
		</div>
	</div>

	<!-- Loading -->
	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState {error} onRetry={loadOwners} />
	{:else if owners.length === 0}
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
					d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
				/>
			</svg>
			<p class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
				{t('admin.owners.list.emptyState')}
			</p>
		</div>
	{:else}
		<ResponsiveAdminList
			items={owners}
			tableLabel={t('admin.owners.list.title')}
			cardsLabel={t('admin.owners.list.title')}
			keyExtractor={(owner) => owner.id ?? owner.displayName ?? ''}
		>
			{#snippet tableHead()}
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('admin.owners.columns.name')}</th>
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('admin.owners.columns.role')}</th>
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('admin.owners.columns.entraObjectId')}</th>
				<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('common.labels.actions')}</th>
			{/snippet}

			{#snippet desktopRow(owner: OwnerResponse)}
				<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
					<td class="whitespace-nowrap px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">
						{owner.displayName}
						{#if !owner.isActive}
							<span class="ml-2 inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200">{t('common.states.inactive')}</span>
						{/if}
					</td>
					<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">
						<span class="inline-flex rounded-full px-2.5 py-1 text-xs font-semibold {getRoleBadgeClass(owner.role)}">
							{t(`admin.owners.roles.${(owner.role ?? 'Member').toLowerCase()}`)}
						</span>
					</td>
					<td class="break-all px-4 py-3 text-sm font-mono text-neutral-700 dark:text-neutral-300">{owner.entraObjectId || '—'}</td>
					<td class="px-4 py-3 text-right">
						<div class="flex flex-wrap justify-end gap-2">
							{@render ownerActionButtons(owner)}
						</div>
					</td>
				</tr>
			{/snippet}

			{#snippet mobileCard(owner: OwnerResponse)}
				<article class="rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
					<div class="flex items-start justify-between gap-3">
						<div class="min-w-0">
							<h2 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">{owner.displayName}</h2>
							<div class="mt-2 flex flex-wrap items-center gap-2">
								<span class="inline-flex rounded-full px-2.5 py-1 text-xs font-semibold {getRoleBadgeClass(owner.role)}">{t(`admin.owners.roles.${(owner.role ?? 'Member').toLowerCase()}`)}</span>
								{#if !owner.isActive}
									<span class="inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200">{t('common.states.inactive')}</span>
								{/if}
							</div>
						</div>
					</div>
					<p class="mt-3 break-all text-sm text-neutral-700 dark:text-neutral-300">
						<span class="font-medium text-neutral-500 dark:text-neutral-400">{t('admin.owners.columns.entraObjectId')}:</span>
						{owner.entraObjectId || '—'}
					</p>
					<div class="mt-4 flex flex-wrap gap-2">
						{@render ownerActionButtons(owner)}
					</div>
				</article>
			{/snippet}
		</ResponsiveAdminList>

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

{#snippet ownerActionButtons(owner: OwnerResponse)}
	<button type="button" onclick={() => openEditModal(owner)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if owner.isActive}
		<button type="button" onclick={() => openDeactivateModal(owner)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

<!-- Form Modal -->
{#if formModalOpen}
	<div
		class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
		role="dialog"
		aria-modal="true"
		aria-labelledby="owner-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="owner-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingOwner ? t('admin.owners.edit.title') : t('admin.owners.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<!-- Display Name -->
				<div class="mb-4">
					<label
						for="owner-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.owners.fields.displayName')} <span class="text-error-600">*</span>
					</label>
					<input
						id="owner-name"
						type="text"
						bind:value={formData.displayName}
						placeholder={t('admin.owners.fields.displayNamePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.displayName}
					/>
					{#if formErrors.displayName}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.displayName}</p>
					{/if}
				</div>

				<!-- Role -->
				<div class="mb-4">
					<label
						for="owner-role"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.owners.fields.role')} <span class="text-error-600">*</span>
					</label>
					<select
						id="owner-role"
						bind:value={formData.role}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.role}
					>
						<option value="Admin">{t('admin.owners.roles.admin')}</option>
						<option value="Member">{t('admin.owners.roles.member')}</option>
						<option value="Viewer">{t('admin.owners.roles.viewer')}</option>
					</select>
					{#if formErrors.role}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.role}</p>
					{/if}
				</div>

				<!-- Entra Object ID -->
				<div class="mb-6">
					<label
						for="owner-entra"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('admin.owners.fields.entraObjectId')}
					</label>
					<input
						id="owner-entra"
						type="text"
						bind:value={formData.entraObjectId}
						placeholder={t('admin.owners.fields.entraObjectIdPlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 font-mono text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.entraObjectId}
					/>
					{#if formErrors.entraObjectId}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">
							{formErrors.entraObjectId}
						</p>
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
{#if deactivateModalOpen && deactivatingOwner}
	<DeactivateConfirmModal
		entityName={deactivatingOwner.displayName ?? ''}
		entityType="owner"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

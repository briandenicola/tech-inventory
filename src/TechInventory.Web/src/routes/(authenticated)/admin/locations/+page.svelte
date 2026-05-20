<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { LocationResponse } from '$lib/api/types';
	import { locationSchema, type LocationFormData } from '$lib/schemas/location';
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
	import ResponsiveAdminList from '$lib/components/admin/ResponsiveAdminList.svelte';
	import ResponsiveListCard from '$lib/components/ResponsiveListCard.svelte';
	import {
		fetchReferenceDeviceCount,
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
	 * T30: Locations Admin — paginated list with Add/Edit/Deactivate
	 * Fields: name (required), type (enum: Home|Storage|External), notes (optional)
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

	let locations = $state<LocationResponse[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	let formModalOpen = $state(false);
	let editingLocation = $state<LocationResponse | null>(null);
	let deactivateModalOpen = $state(false);
	let deactivatingLocation = $state<LocationResponse | null>(null);
	let mergeModalOpen = $state(false);
	let mergeSourceLocations = $state<MergeEntityOption[]>([]);
	let mergeTargetOptions = $state<MergeEntityOption[]>([]);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);
	let selectedIds = $state<Set<string>>(new Set());
	let bulkDeleteModalOpen = $state(false);

	let formData = $state<LocationFormData>({ name: '', type: 'Home', notes: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	const visibleLocationIds = $derived(
		locations.map((location) => location.id).filter((locationId): locationId is string => !!locationId)
	);
	const selectionState = $derived(
		getVisibleReferenceSelectionState(selectedIds, visibleLocationIds)
	);
	const allVisibleSelected = $derived(selectionState.allVisibleSelected);
	const someVisibleSelected = $derived(selectionState.someVisibleSelected);
	const selectedLocations = $derived.by(() =>
		locations.filter(
			(location): location is LocationResponse & { id: string; name: string } =>
				!!location.id && !!location.name && selectedIds.has(location.id)
		)
	);
	const selectedLocationOptions = $derived(
		selectedLocations.map((location) => ({ id: location.id, name: location.name }))
	);
	const selectedActiveLocationOptions = $derived(
		selectedLocations
			.filter((location) => location.isActive)
			.map((location) => ({ id: location.id, name: location.name }))
	);
	const canBulkMerge = $derived(
		selectedActiveLocationOptions.length >= 2 &&
			selectedActiveLocationOptions.length === selectedLocations.length
	);

	$effect(() => {
		loadLocations();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadLocations);
		return unregister;
	});

	$effect(() => {
		void urlParams.page;
		void urlParams.pageSize;
		void urlParams.includeInactive;
		selectedIds = clearReferenceSelection();
	});

	async function loadLocations() {
		loading = true;
		error = null;
		try {
			const response = await api.locations.list(urlParams);
			locations = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[LocationsAdmin] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load locations';
		} finally {
			loading = false;
		}
	}

	function openAddModal() {
		editingLocation = null;
		formData = { name: '', type: 'Home', notes: '' };
		formErrors = {};
		formModalOpen = true;
	}

	function openEditModal(location: LocationResponse) {
		editingLocation = location;
		const notes = (location as LocationResponse & { notes?: string | null }).notes;
		formData = {
			name: location.name ?? '',
			type: (location.type as LocationFormData['type']) ?? 'Home',
			notes: notes ?? ''
		};
		formErrors = {};
		formModalOpen = true;
	}

	function closeFormModal() {
		formModalOpen = false;
		editingLocation = null;
		formData = { name: '', type: 'Home', notes: '' };
		formErrors = {};
	}

	async function handleFormSubmit(e: Event) {
		e.preventDefault();
		formErrors = {};

		const result = locationSchema.safeParse(formData);
		if (!result.success) {
			result.error.issues.forEach((err) => {
				if (err.path[0]) formErrors[err.path[0] as string] = err.message;
			});
			return;
		}

		formSubmitting = true;
		try {
			if (editingLocation?.id) {
				await api.locations.update(editingLocation.id, result.data);
				addToast({ type: 'success', message: 'Location updated successfully' });
			} else {
				await api.locations.create(result.data);
				addToast({ type: 'success', message: 'Location created successfully' });
			}
			closeFormModal();
			await loadLocations();
		} catch (err: unknown) {
			console.error('[LocationsAdmin] Submit failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to save location';
			addToast({ type: 'error', message });
		} finally {
			formSubmitting = false;
		}
	}

	function openDeactivateModal(location: LocationResponse) {
		deactivatingLocation = location;
		deactivateModalOpen = true;
	}

	function closeDeactivateModal() {
		deactivateModalOpen = false;
		deactivatingLocation = null;
	}

	async function handleDeactivate() {
		if (!deactivatingLocation?.id) return;
		try {
			await api.locations.deactivate(deactivatingLocation.id);
			addToast({ type: 'success', message: t('locations.deactivate.success') });
			closeDeactivateModal();
			await loadLocations();
		} catch (err: unknown) {
			console.error('[LocationsAdmin] Deactivate failed:', err);
			const message = err instanceof Error ? err.message : 'Failed to deactivate location';
			addToast({ type: 'error', message });
		}
	}

	async function buildMergeSourceLocations(
		items: MergeEntityOption[]
	): Promise<MergeEntityOption[]> {
		return Promise.all(
			items.map(async (item) => {
				try {
					return {
						...item,
						deviceCount: await fetchReferenceDeviceCount('location', item.id)
					};
				} catch (err: unknown) {
					console.error('[LocationsAdmin] Merge count failed:', err);
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
		mergeSourceLocations = items.map((item) => ({ ...item, deviceCount: null }));
		mergeTargetOptions = [...targets].sort((left, right) => left.name.localeCompare(right.name));
		mergeSourceLocations = await buildMergeSourceLocations(items);
	}

	function openBulkMergeModal() {
		if (!canBulkMerge) {
			return;
		}

		void openMergeModal(selectedActiveLocationOptions, selectedActiveLocationOptions);
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergeSourceLocations = [];
		mergeTargetOptions = [];
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (mergeSourceLocations.length === 0) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetLocation = mergeTargetOptions.find((location) => location.id === targetId);

		try {
			const mergedCount = await mergeReferenceEntitySelection(
				'location',
				mergeSourceLocations.map((location) => location.id),
				targetId
			);
			addToast({
				type: 'success',
				message: t('admin.bulk.mergeSuccess', {
					target: targetLocation?.name ?? '',
					count: mergedCount
				})
			});
			closeMergeModal();
			clearSelection();
			await Promise.all([loadLocations(), fetchReferenceData()]);
		} catch (err: unknown) {
			console.error('[LocationsAdmin] Merge failed:', err);
			mergeError = err instanceof Error ? err.message : t('admin.merge.error');
		} finally {
			mergeSubmitting = false;
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
		selectedIds = toggleAllVisibleReferenceSelections(selectedIds, visibleLocationIds);
	}

	function clearSelection() {
		selectedIds = clearReferenceSelection();
	}

	async function handleBulkDeleteSuccess() {
		clearSelection();
		bulkDeleteModalOpen = false;
		await Promise.all([loadLocations(), fetchReferenceData()]);
	}

	const inactiveBadge = {
		text: t('common.states.inactive'),
		className:
			'inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200'
	};

	function getLocationCardFields(location: LocationResponse) {
		return [
			{
				key: 'type',
				label: t('locations.columns.type'),
				value: t(`locations.types.${(location.type ?? 'Home').toLowerCase()}`)
			},
			{
				key: 'description',
				label: t('locations.columns.description'),
				value: (location as LocationResponse & { notes?: string | null }).notes ?? null,
				valueClass: 'break-words'
			}
		];
	}

	function getLocationActionItems(location: LocationResponse) {
		const actionKey = location.id ?? location.name ?? 'location';
		const items: Array<{
			id: string;
			label: string;
			onSelect: () => void;
			tone: 'primary' | 'warning';
		}> = [
			{
				id: `edit-${actionKey}`,
				label: t('common.actions.edit'),
				onSelect: () => openEditModal(location),
				tone: 'primary' as const
			}
		];

		if (location.isActive) {
			items.push({
				id: `deactivate-${actionKey}`,
				label: t('common.actions.deactivate'),
				onSelect: () => openDeactivateModal(location),
				tone: 'warning' as const
			});
		}

		return items;
	}

	const primaryActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-primary-300 px-4 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-800 dark:text-primary-300 dark:hover:bg-primary-950';
	const warningActionButtonClass =
		'inline-flex min-h-11 items-center rounded-full border border-warning-300 px-4 py-2 text-sm font-medium text-warning-700 transition-colors hover:bg-warning-50 focus:outline-none focus:ring-2 focus:ring-warning-500 dark:border-warning-800 dark:text-warning-300 dark:hover:bg-warning-950';
	const primarySolidButtonClass =
		'inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800';
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
		<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
			{t('locations.list.title')}
		</h1>
		<div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
			<label class="flex min-h-11 items-center gap-3 text-sm text-neutral-700 dark:text-neutral-300">
				<input
					type="checkbox"
					checked={urlParams.includeInactive}
					onchange={toggleInactive}
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				/>
				{t('locations.list.showInactive')}
			</label>
			<button type="button" onclick={openAddModal} class={primarySolidButtonClass}>
				{t('locations.list.addButton')}
			</button>
		</div>
	</div>

	{#if !loading && !error && locations.length > 0}
		<div class="mb-4 flex items-center gap-3 rounded-lg border border-neutral-200 bg-white px-4 py-3 text-sm text-neutral-700 dark:border-neutral-800 dark:bg-neutral-950 dark:text-neutral-300">
			<input
				type="checkbox"
				class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				checked={allVisibleSelected}
				use:setIndeterminate={!allVisibleSelected && someVisibleSelected}
				onchange={toggleSelectAllVisible}
				aria-label={t('admin.bulk.selectAllVisible')}
			/>
			<span>{t('admin.bulk.selectAllVisible')}</span>
		</div>
	{/if}

	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState {error} onRetry={loadLocations} />
	{:else if locations.length === 0}
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
				{t('locations.list.emptyState')}
			</p>
		</div>
	{:else}
		<ResponsiveAdminList
			items={locations}
			tableLabel={t('locations.list.title')}
			cardsLabel={t('locations.list.title')}
			keyExtractor={(location) => location.id ?? location.name ?? ''}
		>
			{#snippet tableHead()}
				<th scope="col" class="w-12 px-4 py-3 text-left">
					<span class="sr-only">{t('common.actions.select')}</span>
				</th>
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('locations.columns.name')}</th>
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('locations.columns.type')}</th>
				<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('locations.columns.description')}</th>
				<th scope="col" class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">{t('common.labels.actions')}</th>
			{/snippet}

			{#snippet desktopRow(location: LocationResponse)}
				{@const selected = location.id ? selectedIds.has(location.id) : false}
				<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900 {selected ? 'bg-primary-50 dark:bg-primary-950/30' : ''}">
					<td class="w-12 px-4 py-3">
						{#if location.id}
							<input
								type="checkbox"
								class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
								checked={selected}
								onchange={() => toggleSelect(location.id ?? '')}
								aria-label={t('admin.bulk.selectRow', { name: location.name ?? '' })}
							/>
						{/if}
					</td>
					<td class="whitespace-nowrap px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50">{location.name}</td>
					<td class="whitespace-nowrap px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">{t(`locations.types.${(location.type ?? 'Home').toLowerCase()}`)}</td>
					<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">{(location as LocationResponse & { notes?: string | null }).notes || '—'}</td>
					<td class="px-4 py-3 text-right">
						<div class="flex flex-wrap justify-end gap-2">
							{@render locationActionButtons(location)}
						</div>
					</td>
				</tr>
			{/snippet}

			{#snippet mobileCard(location: LocationResponse)}
				{@const selected = location.id ? selectedIds.has(location.id) : false}
				<ResponsiveListCard
					title={location.name ?? '—'}
					titleId={`location-card-${location.id ?? 'item'}`}
					selected={selected}
					checked={selected}
					selectLabel={location.id ? t('admin.bulk.selectRow', { name: location.name ?? '' }) : null}
					onToggleSelect={location.id ? () => toggleSelect(location.id ?? '') : undefined}
					badge={!location.isActive ? inactiveBadge : null}
					fields={getLocationCardFields(location)}
					actionItems={getLocationActionItems(location)}
					actionMenuLabel={t('common.actions.moreActions')}
					actionMenuTitle={t('common.labels.actions')}
				/>
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

{#snippet locationActionButtons(location: LocationResponse)}
	<button type="button" onclick={() => openEditModal(location)} class={primaryActionButtonClass}>
		{t('common.actions.edit')}
	</button>
	{#if location.isActive}
		<button type="button" onclick={() => openDeactivateModal(location)} class={warningActionButtonClass}>
			{t('common.actions.deactivate')}
		</button>
	{/if}
{/snippet}

{#if mergeModalOpen && mergeSourceLocations.length > 0}
	<MergeEntityModal
		entityType="location"
		sourceEntities={mergeSourceLocations}
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
		entityType="location"
		items={selectedLocationOptions}
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
		aria-labelledby="location-form-title"
	>
		<div
			class="relative mx-4 w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-neutral-900"
		>
			<h2
				id="location-form-title"
				class="mb-4 text-xl font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{editingLocation ? t('locations.edit.title') : t('locations.create.title')}
			</h2>
			<form onsubmit={handleFormSubmit}>
				<div class="mb-4">
					<label
						for="location-name"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('locations.fields.name')}
					</label>
					<input
						id="location-name"
						type="text"
						bind:value={formData.name}
						placeholder={t('locations.fields.namePlaceholder')}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.name}
					/>
					{#if formErrors.name}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.name}</p>
					{/if}
				</div>

				<div class="mb-4">
					<label
						for="location-type"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('locations.fields.type')}
					</label>
					<select
						id="location-type"
						bind:value={formData.type}
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.type}
					>
						<option value="Home">{t('locations.types.home')}</option>
						<option value="Storage">{t('locations.types.storage')}</option>
						<option value="External">{t('locations.types.external')}</option>
					</select>
					{#if formErrors.type}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.type}</p>
					{/if}
				</div>

				<div class="mb-6">
					<label
						for="location-notes"
						class="block text-sm font-medium text-neutral-700 dark:text-neutral-300"
					>
						{t('locations.fields.notes')}
					</label>
					<textarea
						id="location-notes"
						bind:value={formData.notes}
						placeholder={t('locations.fields.notesPlaceholder')}
						rows="3"
						class="mt-1 block min-h-11 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
						class:border-error-600={formErrors.notes}
					></textarea>
					{#if formErrors.notes}
						<p class="mt-1 text-xs text-error-600 dark:text-error-400">{formErrors.notes}</p>
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

{#if deactivateModalOpen && deactivatingLocation}
	<DeactivateConfirmModal
		entityName={deactivatingLocation.name ?? ''}
		entityType="location"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

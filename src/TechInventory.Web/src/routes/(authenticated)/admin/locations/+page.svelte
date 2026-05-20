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
	let mergingLocation = $state<MergeEntityOption | null>(null);
	let mergeError = $state<string | null>(null);
	let mergeSubmitting = $state(false);

	let formData = $state<LocationFormData>({ name: '', type: 'Home', notes: '' });
	let formErrors = $state<Record<string, string>>({});
	let formSubmitting = $state(false);

	const referenceLocations = $derived.by(() => {
		if ($referenceDataStore.locations.length > 0) {
			return $referenceDataStore.locations;
		}

		return locations
			.filter((location): location is LocationResponse & { id: string; name: string } => !!location.id && !!location.name && !!location.isActive)
			.map((location) => ({ id: location.id, name: location.name }));
	});
	const mergeTargetOptions = $derived.by(() =>
		mergingLocation ? buildMergeTargetOptions(referenceLocations, mergingLocation.id) : []
	);

	$effect(() => {
		loadLocations();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, loadLocations);
		return unregister;
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

	async function openMergeModal(location: LocationResponse) {
		const candidate = toMergeEntityOption(location);
		if (!candidate) {
			return;
		}

		mergeModalOpen = true;
		mergeError = null;
		mergingLocation = { ...candidate, deviceCount: null };

		try {
			const deviceCount = await fetchMergeDeviceCount('location', candidate.id);
			if (mergingLocation?.id === candidate.id) {
				mergingLocation = { ...candidate, deviceCount };
			}
		} catch (err: unknown) {
			console.error('[LocationsAdmin] Merge count failed:', err);
			if (mergingLocation?.id === candidate.id) {
				mergingLocation = { ...candidate, deviceCount: 0 };
			}
		}
	}

	function closeMergeModal() {
		mergeModalOpen = false;
		mergingLocation = null;
		mergeError = null;
		mergeSubmitting = false;
	}

	async function handleMergeConfirm(targetId: string) {
		if (!mergingLocation?.id) {
			return;
		}

		mergeSubmitting = true;
		mergeError = null;
		const targetLocation = mergeTargetOptions.find((location) => location.id === targetId);

		try {
			const response = await mergeReferenceEntities('location', {
				sourceId: mergingLocation.id,
				targetId
			});
			addToast({
				type: 'success',
				message: t('admin.merge.success', {
					source: mergingLocation.name,
					target: targetLocation?.name ?? '',
					count: response.mergedCount
				})
			});
			closeMergeModal();
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
</script>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-6 flex items-center justify-between">
		<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
			{t('locations.list.title')}
		</h1>
		<div class="flex items-center gap-3">
			<label class="flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
				<input
					type="checkbox"
					checked={urlParams.includeInactive}
					onchange={toggleInactive}
					class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				/>
				{t('locations.list.showInactive')}
			</label>
			<button
				type="button"
				onclick={openAddModal}
				class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-700 dark:hover:bg-primary-800"
			>
				{t('locations.list.addButton')}
			</button>
		</div>
	</div>

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
							{t('locations.columns.name')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('locations.columns.type')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('locations.columns.description')}
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
					{#each locations as location (location.id)}
						<tr class="hover:bg-neutral-50 dark:hover:bg-neutral-900">
							<td
								class="whitespace-nowrap px-4 py-3 text-sm font-medium text-neutral-900 dark:text-neutral-50"
							>
								{location.name}
							</td>
							<td
								class="whitespace-nowrap px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300"
							>
								{t(`locations.types.${(location.type ?? 'Home').toLowerCase()}`)}
							</td>
							<td class="px-4 py-3 text-sm text-neutral-700 dark:text-neutral-300">
								{(location as LocationResponse & { notes?: string | null }).notes || '—'}
							</td>
							<td class="whitespace-nowrap px-4 py-3 text-right text-sm">
								<button
									type="button"
									onclick={() => openEditModal(location)}
									class="mr-3 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
								>
									{t('common.actions.edit')}
								</button>
								{#if location.isActive}
									<button
										type="button"
										onclick={() => openMergeModal(location)}
										class="mr-3 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
									>
										{t('common.actions.merge')}
									</button>
									<button
										type="button"
										onclick={() => openDeactivateModal(location)}
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

{#if mergeModalOpen && mergingLocation}
	<MergeEntityModal
		entityType="location"
		sourceEntity={mergingLocation}
		entities={mergeTargetOptions}
		isOpen={mergeModalOpen}
		isSubmitting={mergeSubmitting}
		errorMessage={mergeError}
		onConfirm={handleMergeConfirm}
		onCancel={closeMergeModal}
	/>
{/if}

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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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
						class="mt-1 block w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-50"
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

{#if deactivateModalOpen && deactivatingLocation}
	<DeactivateConfirmModal
		entityName={deactivatingLocation.name ?? ''}
		entityType="location"
		onConfirm={handleDeactivate}
		onCancel={closeDeactivateModal}
	/>
{/if}

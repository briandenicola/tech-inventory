<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { useDevices, type DeviceFilters as DeviceFiltersType } from '$lib/queries/devices.svelte';
	import {
		getDevicesDefaultView,
		setDevicesDefaultView,
		clearDevicesDefaultView,
		normalizeQueryString
	} from '$lib/stores/userPrefs';
	import { showToast } from '$lib/stores/toast';
	import DeviceTable from '$lib/components/DeviceTable.svelte';
	import DeviceFilters from '$lib/components/DeviceFilters.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import AddDeviceModal from '$lib/components/AddDeviceModal.svelte';
	import BulkActionBar from '$lib/components/BulkActionBar.svelte';
	import BulkUpdateModal from '$lib/components/BulkUpdateModal.svelte';
	import BulkDeleteModal from '$lib/components/BulkDeleteModal.svelte';
	import { devices as devicesApi } from '$lib/api/client';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import { groupDevices } from '$lib/utils/groupDevices';

	/**
	 * T15: Devices list page — paginated table with filters, sort, and pagination.
	 * 
	 * States: loading → success/empty/error
	 * Mobile: stack columns as cards (360px+)
	 * URL-backed: page, pageSize, search, filters, sort via $page.url.searchParams
	 * 
	 * Related: specs/002-frontend-mvp/spec.md §5, J4
	 */

	const currentUser = $derived($authStore.currentUser);

	// Parse filters from URL searchParams (reactive)
	const urlFilters = $derived.by(() => {
		const params = $page.url.searchParams;
		const filters: DeviceFiltersType = {
			page: parseInt(params.get('page') || '1', 10),
			pageSize: parseInt(params.get('pageSize') || '25', 10),
			search: params.get('search') || undefined,
			brandId: params.get('brandId') || undefined,
			categoryId: params.get('categoryId') || undefined,
			ownerId: params.get('ownerId') || undefined,
			locationId: params.get('locationId') || undefined,
			networkId: params.get('networkId') || undefined,
			status: params.get('status')
				? [params.get('status')! as 'Active' | 'Retired' | 'Disposed' | 'InRepair' | 'Lent']
				: undefined,
			purchaseYearMin: params.get('yearMin')
				? parseInt(params.get('yearMin')!, 10)
				: undefined,
			purchaseYearMax: params.get('yearMax')
				? parseInt(params.get('yearMax')!, 10)
				: undefined,
			sort: (params.get('sort') as 'name' | 'purchaseDate' | 'createdAt') || undefined,
			sortDir: (params.get('sortDir') as 'asc' | 'desc') || 'asc',
			groupBy:
				(params.get('groupBy') as 'category' | 'owner' | 'year' | null) || undefined
		};
		return filters;
	});

	// Devices query (reactive — pass a getter so filter changes propagate)
	// When grouping is active we fetch the max page size (200, capped by the
	// API's ListDevicesQueryValidator) so groups span the largest practical
	// result set. For households with >200 devices, grouping a filtered
	// subset still works; a future enhancement could loop pages.
	const effectiveFilters = $derived.by(() => {
		if (urlFilters.groupBy) {
			return { ...urlFilters, page: 1, pageSize: 200 };
		}
		return urlFilters;
	});
	const query = useDevices(() => effectiveFilters);

	// Mobile drawer state
	let filtersOpen = $state(false);

	// Add Device modal state (D-137 — Apple-elegant modal replaces /devices/new flow)
	let createModalOpen = $state(false);

	// Update URL when filters change
	function updateFilters(newFilters: DeviceFiltersType) {
		const params = new URLSearchParams();

		if (newFilters.page && newFilters.page !== 1) params.set('page', newFilters.page.toString());
		if (newFilters.pageSize && newFilters.pageSize !== 25)
			params.set('pageSize', newFilters.pageSize.toString());
		if (newFilters.search) params.set('search', newFilters.search);
		if (newFilters.brandId) params.set('brandId', newFilters.brandId);
		if (newFilters.categoryId) params.set('categoryId', newFilters.categoryId);
		if (newFilters.ownerId) params.set('ownerId', newFilters.ownerId);
		if (newFilters.locationId) params.set('locationId', newFilters.locationId);
		if (newFilters.networkId) params.set('networkId', newFilters.networkId);
		if (newFilters.status && newFilters.status.length > 0)
			params.set('status', newFilters.status[0]);
		if (newFilters.purchaseYearMin) params.set('yearMin', newFilters.purchaseYearMin.toString());
		if (newFilters.purchaseYearMax) params.set('yearMax', newFilters.purchaseYearMax.toString());
		if (newFilters.sort) params.set('sort', newFilters.sort);
		if (newFilters.sortDir && newFilters.sortDir !== 'asc')
			params.set('sortDir', newFilters.sortDir);
		if (newFilters.groupBy) params.set('groupBy', newFilters.groupBy);

		const url = params.toString() ? `?${params.toString()}` : $page.url.pathname;
		goto(url, { replaceState: true, keepFocus: true, noScroll: true });
	}

	// Handle sort (toggle asc ↔ desc)
	function handleSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		const newDir =
			urlFilters.sort === column && urlFilters.sortDir === 'asc' ? 'desc' : 'asc';
		updateFilters({ ...urlFilters, sort: column, sortDir: newDir });
	}

	// Handle pagination
	function handlePageChange(page: number, pageSize: number) {
		updateFilters({ ...urlFilters, page, pageSize });
	}

	// Determine if filters are active (for empty state logic)
	const hasActiveFilters = $derived(
		!!(
			urlFilters.search ||
			urlFilters.brandId ||
			urlFilters.categoryId ||
			urlFilters.ownerId ||
			urlFilters.locationId ||
			urlFilters.networkId ||
			(urlFilters.status && urlFilters.status.length > 0) ||
			urlFilters.purchaseYearMin ||
			urlFilters.purchaseYearMax
		)
	);

	// F022: per-user default view persistence (localStorage).
	// Apply a stored default exactly once on first mount when the URL is bare;
	// explicit deep-links always win.
	let storedDefault = $state<string | null>(null);
	function refreshStoredDefault() {
		storedDefault = getDevicesDefaultView(currentUser?.id);
	}
	onMount(() => {
		refreshStoredDefault();
		const bareEntry = $page.url.search === '' || $page.url.search === '?';
		if (bareEntry && storedDefault) {
			void goto(`?${storedDefault}`, { replaceState: true, keepFocus: true, noScroll: true });
		}
	});

	const currentQueryNormalized = $derived(normalizeQueryString($page.url.search));
	const hasStoredDefault = $derived(storedDefault !== null);
	const canSaveDefault = $derived(currentQueryNormalized !== storedDefault);

	function handleSaveDefault() {
		if (!currentUser?.id) return;
		setDevicesDefaultView(currentUser.id, $page.url.search);
		refreshStoredDefault();
		showToast({ message: t('devices.filters.defaultSaved'), type: 'success' });
	}

	function handleClearDefault() {
		if (!currentUser?.id) return;
		clearDevicesDefaultView(currentUser.id);
		refreshStoredDefault();
		showToast({ message: t('devices.filters.defaultCleared'), type: 'success' });
	}

	// F023: group devices client-side when a groupBy dimension is active.
	// Pulls reference data for human-readable labels (Category/Owner names).
	const refData = $derived($referenceDataStore);
	const groupedView = $derived.by(() => {
		if (!urlFilters.groupBy || !query.data?.items) return undefined;
		return groupDevices(
			query.data.items,
			urlFilters.groupBy,
			{ categories: refData.categories, owners: refData.owners },
			t('devices.groups.unknown')
		);
	});

	// F024 — multi-select bulk actions state.
	// Selection is ephemeral (not URL-persisted); reset whenever filters/sort/grouping
	// or pagination change so users never act on a stale, off-screen selection.
	type BulkField = 'category' | 'owner' | 'brand' | 'location' | 'status';
	const STATUS_OPTIONS: Array<{ id: string; name: string }> = [
		{ id: 'Active', name: 'Active' },
		{ id: 'Retired', name: 'Retired' },
		{ id: 'Disposed', name: 'Disposed' },
		{ id: 'InRepair', name: 'In Repair' },
		{ id: 'Lent', name: 'Lent' }
	];

	let selectedIds = $state(new Set<string>());
	let bulkUpdateField = $state<BulkField | null>(null);
	let bulkDeleteOpen = $state(false);

	// Clear selection on any meaningful query-key change.
	$effect(() => {
		// Touch each filter key so the effect re-runs when any of them changes.
		void urlFilters.search;
		void urlFilters.brandId;
		void urlFilters.categoryId;
		void urlFilters.ownerId;
		void urlFilters.locationId;
		void urlFilters.networkId;
		void urlFilters.status;
		void urlFilters.purchaseYearMin;
		void urlFilters.purchaseYearMax;
		void urlFilters.sort;
		void urlFilters.sortDir;
		void urlFilters.groupBy;
		void urlFilters.page;
		void urlFilters.pageSize;
		selectedIds = new Set<string>();
	});

	const visibleDeviceIds = $derived(query.data?.items?.map((d) => d.id) ?? []);
	const allVisibleSelected = $derived(
		visibleDeviceIds.length > 0 && visibleDeviceIds.every((id) => selectedIds.has(id))
	);
	const someVisibleSelected = $derived(
		!allVisibleSelected && visibleDeviceIds.some((id) => selectedIds.has(id))
	);

	function toggleSelect(id: string) {
		const next = new Set(selectedIds);
		if (next.has(id)) next.delete(id);
		else next.add(id);
		selectedIds = next;
	}
	function toggleSelectAllVisible() {
		const next = new Set(selectedIds);
		if (allVisibleSelected) {
			visibleDeviceIds.forEach((id) => next.delete(id));
		} else {
			visibleDeviceIds.forEach((id) => next.add(id));
		}
		selectedIds = next;
	}
	function clearSelection() {
		selectedIds = new Set<string>();
	}

	const bulkUpdateOptions = $derived.by(() => {
		switch (bulkUpdateField) {
			case 'category':
				return refData.categories.map((c) => ({ id: c.id, name: c.name }));
			case 'owner':
				return refData.owners.map((o) => ({ id: o.id, name: o.name }));
			case 'brand':
				return refData.brands.map((b) => ({ id: b.id, name: b.name }));
			case 'location':
				return refData.locations.map((l) => ({ id: l.id, name: l.name }));
			case 'status':
				return STATUS_OPTIONS;
			default:
				return [];
		}
	});

	async function handleBulkUpdate(value: string) {
		if (!bulkUpdateField) return;
		const ids = Array.from(selectedIds);
		const changes: Record<string, string> = {};
		switch (bulkUpdateField) {
			case 'category':
				changes.categoryId = value;
				break;
			case 'owner':
				changes.ownerId = value;
				break;
			case 'brand':
				changes.brandId = value;
				break;
			case 'location':
				changes.locationId = value;
				break;
			case 'status':
				changes.status = value;
				break;
		}
		try {
			const result = await devicesApi.bulkUpdate({ deviceIds: ids, changes });
			showToast({
				message: t('devices.bulk.successUpdate', { count: result?.affectedCount ?? ids.length }),
				type: 'success'
			});
			bulkUpdateField = null;
			clearSelection();
			await query.refetch();
		} catch (err) {
			console.error('[devices] bulkUpdate failed:', err);
			showToast({ message: t('devices.bulk.errorPartial'), type: 'error' });
		}
	}

	async function handleBulkDelete(reason: string) {
		const ids = Array.from(selectedIds);
		try {
			const result = await devicesApi.bulkDelete({ deviceIds: ids, reason });
			showToast({
				message: t('devices.bulk.successDelete', { count: result?.affectedCount ?? ids.length }),
				type: 'success'
			});
			bulkDeleteOpen = false;
			clearSelection();
			await query.refetch();
		} catch (err) {
			console.error('[devices] bulkDelete failed:', err);
			showToast({ message: t('devices.bulk.errorPartial'), type: 'error' });
		}
	}

	const canBulkDelete = $derived(currentUser?.role === 'Admin');
</script>

<div class="flex min-h-screen">
	<!-- Filters sidebar (desktop: sticky, mobile: drawer) -->
	<DeviceFilters
		filters={urlFilters}
		onFiltersChange={updateFilters}
		isOpen={filtersOpen}
		onClose={() => (filtersOpen = false)}
		onSaveDefault={handleSaveDefault}
		onClearDefault={handleClearDefault}
		hasStoredDefault={hasStoredDefault}
		canSaveDefault={canSaveDefault}
	/>

	<!-- Main content -->
	<div class="flex-1 p-6">
		<!-- Header -->
		<div class="mb-6 flex items-center justify-between border-b border-neutral-200 pb-4 dark:border-neutral-800">
			<div>
				<h1 class="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
					{t('devices.list.title')}
				</h1>
				{#if currentUser}
					<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
						{t('auth.welcomeMessage', { name: currentUser.displayName })}
					</p>
				{/if}
			</div>

			<!-- Right-side actions -->
			<div class="flex items-center gap-2">
				<!-- Mobile filter button -->
				<button
					type="button"
					onclick={() => (filtersOpen = !filtersOpen)}
					class="md:hidden inline-flex items-center gap-2 rounded-lg bg-neutral-100 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-200 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-300 dark:hover:bg-neutral-700"
					aria-expanded={filtersOpen}
				>
					<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
						/>
					</svg>
					{t('devices.filters.filterButton')}
				</button>

				<!-- Add Device CTA -->
				<button
					type="button"
					onclick={() => (createModalOpen = true)}
					class="inline-flex min-h-11 items-center gap-2 rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
				>
					<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
					</svg>
					{t('devices.list.addButton')}
				</button>
			</div>
		</div>

		<!-- Content: loading / error / empty / success -->
		{#if query.isLoading}
			<LoadingSkeleton rows={7} />
		{:else if query.error}
			<ErrorState error={query.error} onRetry={query.refetch} />
		{:else if !query.data || !query.data.items || query.data.items.length === 0}
			<EmptyState filtered={hasActiveFilters} onAdd={() => (createModalOpen = true)} />
		{:else}
			<!-- Table -->
			<DeviceTable
				devices={query.data.items}
				groups={groupedView}
				currentSort={urlFilters.sort}
				sortDir={urlFilters.sortDir}
				onSort={handleSort}
				selectable={true}
				selectedIds={selectedIds}
				onToggleSelect={toggleSelect}
				onToggleSelectAll={toggleSelectAllVisible}
				allVisibleSelected={allVisibleSelected}
				someVisibleSelected={someVisibleSelected}
			/>

			<!-- Pagination (hidden while grouping is active; groups span the full page) -->
			{#if !urlFilters.groupBy}
				<PaginationControls
					currentPage={query.data.page}
					pageSize={query.data.pageSize}
					totalCount={query.data.totalCount}
					onPageChange={handlePageChange}
				/>
			{/if}
		{/if}
	</div>
</div>

{#if createModalOpen}
	<AddDeviceModal
		onClose={() => (createModalOpen = false)}
		onCreated={() => query.refetch()}
	/>
{/if}

<BulkActionBar
	count={selectedIds.size}
	onClear={clearSelection}
	onChangeField={(field) => (bulkUpdateField = field)}
	onDelete={canBulkDelete ? () => (bulkDeleteOpen = true) : undefined}
/>

{#if bulkUpdateField}
	<BulkUpdateModal
		field={bulkUpdateField}
		count={selectedIds.size}
		options={bulkUpdateOptions}
		onConfirm={handleBulkUpdate}
		onCancel={() => (bulkUpdateField = null)}
	/>
{/if}

{#if bulkDeleteOpen}
	<BulkDeleteModal
		count={selectedIds.size}
		onConfirm={handleBulkDelete}
		onCancel={() => (bulkDeleteOpen = false)}
	/>
{/if}


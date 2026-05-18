<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { useDevices, type DeviceFilters as DeviceFiltersType } from '$lib/queries/devices';
	import DeviceTable from '$lib/components/DeviceTable.svelte';
	import DeviceFilters from '$lib/components/DeviceFilters.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';

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
			sortDir: (params.get('sortDir') as 'asc' | 'desc') || 'asc'
		};
		return filters;
	});

	// Devices query (reactive via $derived hook — eslint warning is false positive)
	// eslint-disable-next-line svelte/valid-compile
	const query = useDevices(urlFilters);

	// Mobile drawer state
	let filtersOpen = $state(false);

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
</script>

<div class="flex min-h-screen">
	<!-- Filters sidebar (desktop: sticky, mobile: drawer) -->
	<DeviceFilters
		filters={urlFilters}
		onFiltersChange={updateFilters}
		isOpen={filtersOpen}
		onClose={() => (filtersOpen = false)}
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
		</div>

		<!-- Content: loading / error / empty / success -->
		{#if query.isLoading}
			<LoadingSkeleton rows={7} />
		{:else if query.error}
			<ErrorState error={query.error} onRetry={query.refetch} />
		{:else if !query.data || !query.data.items || query.data.items.length === 0}
			<EmptyState filtered={hasActiveFilters} />
		{:else}
			<!-- Table -->
			<DeviceTable
				devices={query.data.items}
				currentSort={urlFilters.sort}
				sortDir={urlFilters.sortDir}
				onSort={handleSort}
			/>

			<!-- Pagination -->
			<PaginationControls
				currentPage={query.data.page}
				pageSize={query.data.pageSize}
				totalCount={query.data.totalCount}
				onPageChange={handlePageChange}
			/>
		{/if}
	</div>
</div>


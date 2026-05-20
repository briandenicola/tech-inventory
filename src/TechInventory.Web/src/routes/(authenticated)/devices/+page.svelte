<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import {
		fetchDevicesPage,
		invalidateDevicesCache,
		serializeDeviceFilters,
		useDevices,
		type DeviceFilters as DeviceFiltersType,
		type DeviceResponse
	} from '$lib/queries/devices.svelte';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import BackToTopFab from '$lib/components/BackToTopFab.svelte';
	import DeviceListAddActions from '$lib/components/DeviceListAddActions.svelte';
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
	import DeviceDetailModal from '$lib/components/DeviceDetailModal.svelte';
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
	const canCreateDevice = $derived(
		currentUser?.role === 'Admin' || currentUser?.role === 'Member'
	);
	const initialReducedMotion =
		typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

	let prefersReducedMotion = $state(initialReducedMotion);
	let showBackToTop = $state(false);
	let infiniteItems = $state<DeviceResponse[]>([]);
	let infinitePage = $state(0);
	let totalCount = $state(0);
	let isLoadingMore = $state(false);
	let loadMoreError = $state<string | null>(null);
	let activeInfiniteKey = $state('');
	let sentinel = $state<HTMLDivElement | null>(null);

	// F026: Active-default status filter.
	//
	// Behaviour:
	//   - no `status` URL param        → status = ['Active']  (the new default)
	//   - `status=all`                 → status = undefined   (show everything)
	//   - `status=<DeviceStatus>`      → status = [that one]  (explicit pick)
	//
	// The `all` sentinel is required so we can distinguish "user explicitly
	// asked for every status" from "user has not chosen anything yet"; without
	// it the bare URL would always be re-coerced to Active and there would be
	// no way to opt back out.
	const STATUS_ALL_SENTINEL = 'all';
	type DeviceStatus = NonNullable<DeviceFiltersType['status']>[number];
	const KNOWN_STATUSES: DeviceStatus[] = ['Active', 'Retired', 'Disposed', 'InRepair', 'Lent'];
	function isDeviceStatus(value: string): value is DeviceStatus {
		return (KNOWN_STATUSES as string[]).includes(value);
	}

	// Parse filters from URL searchParams (reactive)
	const urlFilters = $derived.by(() => {
		const params = $page.url.searchParams;
		const rawStatus = params.get('status');
		let status: DeviceStatus[] | undefined;
		if (rawStatus === STATUS_ALL_SENTINEL) {
			status = undefined;
		} else if (rawStatus && isDeviceStatus(rawStatus)) {
			status = [rawStatus];
		} else {
			status = ['Active'];
		}
		const filters: DeviceFiltersType = {
			page: parseInt(params.get('page') || '1', 10),
			pageSize: parseInt(params.get('pageSize') || '25', 10),
			search: params.get('search') || undefined,
			brandId: params.get('brandId') || undefined,
			categoryId: params.get('categoryId') || undefined,
			ownerId: params.get('ownerId') || undefined,
			locationId: params.get('locationId') || undefined,
			networkId: params.get('networkId') || undefined,
			status,
			purchaseYearMin: params.get('yearMin') ? parseInt(params.get('yearMin')!, 10) : undefined,
			purchaseYearMax: params.get('yearMax') ? parseInt(params.get('yearMax')!, 10) : undefined,
			sort: (params.get('sort') as 'name' | 'purchaseDate' | 'createdAt') || undefined,
			sortDir: (params.get('sortDir') as 'asc' | 'desc') || 'asc',
			groupBy: (params.get('groupBy') as 'category' | 'owner' | 'year' | null) || undefined
		};
		return filters;
	});

	// F026: did the user explicitly opt out of the Active default this session?
	// Stays true only inside the bare-URL → Active-default branch.
	const statusIsImplicitActive = $derived($page.url.searchParams.get('status') === null);
	const showingAllStatuses = $derived($page.url.searchParams.get('status') === STATUS_ALL_SENTINEL);

	// Devices query (reactive — pass a getter so filter changes propagate).
	// Infinite scroll keeps the API on page 1 and progressively appends pages on
	// the client unless grouping is active or reduced motion requests the classic
	// pagination fallback.
	const queryFilters = $derived.by(() => {
		if (urlFilters.groupBy) {
			return { ...urlFilters, page: 1, pageSize: 200 };
		}

		return prefersReducedMotion ? urlFilters : { ...urlFilters, page: 1 };
	});
	const query = useDevices(() => queryFilters);
	const infiniteBaseFilters = $derived.by(() => ({ ...urlFilters, page: 1 }));
	const infiniteFiltersKey = $derived(serializeDeviceFilters(infiniteBaseFilters));
	const displayedDevices = $derived.by(() => {
		if (urlFilters.groupBy || prefersReducedMotion) {
			return query.data?.items ?? [];
		}

		return infiniteItems.length > 0 ? infiniteItems : (query.data?.items ?? []);
	});
	const hasMorePages = $derived(
		!urlFilters.groupBy &&
			!prefersReducedMotion &&
			displayedDevices.length > 0 &&
			displayedDevices.length < totalCount
	);

	// Mobile drawer state
	let filtersOpen = $state(false);

	// Search debounce timeout
	let searchTimeout: ReturnType<typeof setTimeout> | null = $state(null);

	// Device detail modal state (selected via ?device= URL param)
	const selectedDeviceId = $derived($page.url.searchParams.get('device'));

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
		// F026: status round-trip rules.
		//   undefined or []           → user cleared status → STATUS_ALL_SENTINEL
		//   ['Active']                → matches implicit default → omit
		//   any other single value    → write as-is
		if (!newFilters.status || newFilters.status.length === 0) {
			params.set('status', STATUS_ALL_SENTINEL);
		} else if (newFilters.status.length === 1 && newFilters.status[0] === 'Active') {
			// omit — implicit default
		} else {
			params.set('status', newFilters.status[0]);
		}
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
		const newDir = urlFilters.sort === column && urlFilters.sortDir === 'asc' ? 'desc' : 'asc';
		updateFilters({ ...urlFilters, sort: column, sortDir: newDir });
	}

	// Handle pagination
	function handlePageChange(page: number, pageSize: number) {
		updateFilters({ ...urlFilters, page, pageSize });
	}

	function openDeviceDetail(deviceId: string) {
		const params = new URLSearchParams($page.url.searchParams);
		params.set('device', deviceId);
		const query = params.toString();
		void goto(query ? `${$page.url.pathname}?${query}` : $page.url.pathname, {
			keepFocus: true,
			noScroll: true
		});
	}

	function closeDeviceDetail() {
		const params = new URLSearchParams($page.url.searchParams);
		params.delete('device');
		const query = params.toString();
		void goto(query ? `${$page.url.pathname}?${query}` : $page.url.pathname, {
			keepFocus: true,
			noScroll: true,
			replaceState: true
		});
	}

	// Determine if filters are active (for empty state logic).
	// F026: the implicit Active default is *not* a user-set filter, so it
	// doesn't count toward "filters are active" — otherwise the empty state
	// would always say "no devices match your filters" even on a clean URL.
	const hasActiveFilters = $derived(
		!!(
			urlFilters.search ||
			urlFilters.brandId ||
			urlFilters.categoryId ||
			urlFilters.ownerId ||
			urlFilters.locationId ||
			urlFilters.networkId ||
			(!statusIsImplicitActive && urlFilters.status && urlFilters.status.length > 0) ||
			showingAllStatuses ||
			urlFilters.purchaseYearMin ||
			urlFilters.purchaseYearMax
		)
	);

	// F026: toggle between the implicit-Active default and the explicit
	// show-all sentinel from the header chip.
	function showAllStatuses() {
		updateFilters({ ...urlFilters, status: undefined, page: 1 });
	}
	function restoreActiveDefault() {
		updateFilters({ ...urlFilters, status: ['Active'], page: 1 });
	}

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


		const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
		const updateMotionPreference = () => {
			prefersReducedMotion = mediaQuery.matches;
		};
		const updateBackToTopVisibility = () => {
			showBackToTop = window.scrollY > window.innerHeight;
		};

		updateMotionPreference();
		updateBackToTopVisibility();
		mediaQuery.addEventListener('change', updateMotionPreference);
		window.addEventListener('scroll', updateBackToTopVisibility, { passive: true });

		return () => {
			mediaQuery.removeEventListener('change', updateMotionPreference);
			window.removeEventListener('scroll', updateBackToTopVisibility);
		};
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

	async function loadNextPage() {
		if (prefersReducedMotion || urlFilters.groupBy || isLoadingMore || !hasMorePages) {
			return;
		}

		isLoadingMore = true;
		loadMoreError = null;

		try {
			const nextPage = await fetchDevicesPage({
				...infiniteBaseFilters,
				page: infinitePage + 1
			});
			const nextItems = nextPage.items ?? [];

			infiniteItems = [...infiniteItems, ...nextItems];
			infinitePage = nextPage.page;
			totalCount = nextPage.totalCount;
		} catch (err) {
			loadMoreError = err instanceof Error ? err.message : t('devices.infiniteScroll.loadError');
		} finally {
			isLoadingMore = false;
		}
	}

	async function refreshDevicesList() {
		invalidateDevicesCache();
		loadMoreError = null;

		if (!prefersReducedMotion && !urlFilters.groupBy) {
			activeInfiniteKey = '';
			infiniteItems = [];
			infinitePage = 0;
			totalCount = 0;
		}

		await query.refetch();
	}

	function scrollToTop() {
		if (typeof window === 'undefined') {
			return;
		}

		window.scrollTo({
			top: 0,
			behavior: prefersReducedMotion ? 'auto' : 'smooth'
		});
	}

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, refreshDevicesList);
		return unregister;
	});

	$effect(() => {
		if (prefersReducedMotion || urlFilters.groupBy) {
			return;
		}

		const key = infiniteFiltersKey;
		if (key === activeInfiniteKey) {
			return;
		}

		activeInfiniteKey = key;
		infiniteItems = [];
		infinitePage = 0;
		totalCount = 0;
		loadMoreError = null;
	});

	$effect(() => {
		if (prefersReducedMotion || urlFilters.groupBy || !query.data) {
			return;
		}

		const key = infiniteFiltersKey;
		if (activeInfiniteKey !== key || query.data.page !== 1) {
			return;
		}

		infiniteItems = query.data.items ?? [];
		infinitePage = query.data.page;
		totalCount = query.data.totalCount;
		loadMoreError = null;
	});

	$effect(() => {
		if (prefersReducedMotion || urlFilters.groupBy || !sentinel || !hasMorePages || loadMoreError) {
			return;
		}

		const observer = new IntersectionObserver(
			(entries) => {
				if (entries.some((entry) => entry.isIntersecting)) {
					void loadNextPage();
				}
			},
			{ rootMargin: '200px 0px' }
		);

		observer.observe(sentinel);
		return () => {
			observer.disconnect();
		};
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

	const visibleDeviceIds = $derived(displayedDevices.map((device) => device.id));
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
			await refreshDevicesList();
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
			await refreshDevicesList();
		} catch (err) {
			console.error('[devices] bulkDelete failed:', err);
			showToast({ message: t('devices.bulk.errorPartial'), type: 'error' });
		}
	}

	const canBulkDelete = $derived(currentUser?.role === 'Admin');
</script>

<!-- Filters sidebar (floats over as drawer on all breakpoints) -->
<DeviceFilters
	filters={urlFilters}
	onFiltersChange={updateFilters}
	isOpen={filtersOpen}
	onClose={() => (filtersOpen = false)}
	onSaveDefault={handleSaveDefault}
	onClearDefault={handleClearDefault}
	{hasStoredDefault}
	{canSaveDefault}
/>

<!-- Main content -->
<div class="p-6">
	<!-- Header -->
	<div
		class="mb-6 flex items-start justify-between gap-4 border-b border-neutral-200 pb-4 dark:border-neutral-800"
	>
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
				class="inline-flex items-center gap-2 rounded-lg bg-neutral-100 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-200 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-300 dark:hover:bg-neutral-700"
				aria-expanded={filtersOpen}
				aria-controls="device-filters-panel"
				aria-haspopup="dialog"
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

			<!-- Add Device CTA (desktop only; mobile uses the FAB below) -->
			<DeviceListAddActions selectedCount={selectedIds.size} detailOpen={!!selectedDeviceId} />
		</div>
	</div>

	<!-- Search input -->
	<div class="mb-4 w-full md:max-w-lg">
		<div class="relative">
			<div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4">
				<svg
					class="h-5 w-5 text-neutral-400 dark:text-neutral-500"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
					/>
				</svg>
			</div>
			<input
				type="search"
				value={urlFilters.search || ''}
				oninput={(e) => {
					const target = e.target as HTMLInputElement;
					const value = target.value;
					if (searchTimeout) clearTimeout(searchTimeout);
					searchTimeout = setTimeout(() => {
						updateFilters({ ...urlFilters, search: value || undefined, page: 1 });
					}, 300);
				}}
				placeholder={t('devices.filters.searchPlaceholder')}
				aria-label={t('devices.filters.searchPlaceholder')}
				class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 pl-11 pr-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
			/>
		</div>
	</div>

	<!--
			F026: status-filter chip. The implicit Active default is invisible by
			itself, so we surface it as a chip with a one-tap escape hatch. When the
			user has opted into "all", the chip flips to offer the reverse action.
		-->
	{#if statusIsImplicitActive}
		<div class="mb-4 flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
			<span
				class="inline-flex items-center rounded-full bg-success-100 px-3 py-1 text-xs font-medium text-success-800 dark:bg-success-900 dark:text-success-100"
			>
				{t('devices.statusChip.showingActive')}
			</span>
			<button
				type="button"
				onclick={showAllStatuses}
				class="rounded-full px-2 py-1 text-sm font-medium text-primary-700 underline-offset-2 hover:underline focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-primary-300"
			>
				{t('devices.statusChip.showAll')}
			</button>
		</div>
	{:else if showingAllStatuses}
		<div class="mb-4 flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
			<span
				class="inline-flex items-center rounded-full bg-neutral-200 px-3 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200"
			>
				{t('devices.statusChip.showingAll')}
			</span>
			<button
				type="button"
				onclick={restoreActiveDefault}
				class="rounded-full px-2 py-1 text-sm font-medium text-primary-700 underline-offset-2 hover:underline focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-primary-300"
			>
				{t('devices.statusChip.activeOnly')}
			</button>
		</div>
	{/if}

	<!-- Content: loading / error / empty / success -->
	{#if query.isLoading}
		<LoadingSkeleton rows={7} />
	{:else if query.error}
		<ErrorState error={query.error} onRetry={refreshDevicesList} />
	{:else if displayedDevices.length === 0}
		<EmptyState filtered={hasActiveFilters} showAddAction={canCreateDevice} />
	{:else}
		<DeviceTable
			devices={displayedDevices}
			groups={groupedView}
			currentSort={urlFilters.sort}
			sortDir={urlFilters.sortDir}
			onSort={handleSort}
			selectable={true}
			{selectedIds}
			onToggleSelect={toggleSelect}
			onToggleSelectAll={toggleSelectAllVisible}
			{allVisibleSelected}
			{someVisibleSelected}
			onOpenDevice={openDeviceDetail}
		/>

		{#if urlFilters.groupBy}
			<!-- Grouped mode renders a single expanded page. -->
		{:else if prefersReducedMotion}
			<PaginationControls
				currentPage={query.data?.page ?? urlFilters.page ?? 1}
				pageSize={query.data?.pageSize ?? urlFilters.pageSize ?? 25}
				totalCount={query.data?.totalCount ?? totalCount}
				onPageChange={handlePageChange}
			/>
		{:else}
			<div class="mt-6" aria-live="polite" aria-atomic="true">
				{#if hasMorePages}
					<div bind:this={sentinel} class="h-px w-full" aria-hidden="true"></div>
				{/if}

				{#if isLoadingMore}
					<div
						class="flex items-center justify-center gap-3 rounded-lg border border-neutral-200 bg-white px-4 py-3 text-sm text-neutral-700 dark:border-neutral-800 dark:bg-neutral-950 dark:text-neutral-300"
					>
						<svg
							class="h-5 w-5 animate-spin text-primary-600 dark:text-primary-400"
							viewBox="0 0 24 24"
							fill="none"
							aria-hidden="true"
						>
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
							></path>
						</svg>
						<span>{t('devices.infiniteScroll.loadingMore')}</span>
					</div>
				{:else if loadMoreError}
					<div
						class="flex flex-col items-start gap-3 rounded-lg border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700 dark:border-danger-900 dark:bg-danger-900/30 dark:text-danger-100 sm:flex-row sm:items-center sm:justify-between"
					>
						<span>{loadMoreError}</span>
						<button
							type="button"
							onclick={() => void loadNextPage()}
							class="rounded-lg border border-danger-300 px-3 py-2 font-medium transition-colors hover:bg-danger-100 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-danger-700 dark:hover:bg-danger-900/60"
						>
							{t('common.actions.retry')}
						</button>
					</div>
				{:else if !hasMorePages}
					<p class="text-center text-sm text-neutral-600 dark:text-neutral-400">
						{t('devices.infiniteScroll.complete')}
					</p>
				{/if}
			</div>
		{/if}
	{/if}
</div>

{#if selectedDeviceId}
	<DeviceDetailModal
		deviceId={selectedDeviceId}
		onClose={closeDeviceDetail}
		onChanged={() => void refreshDevicesList()}
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

<BackToTopFab
	visible={showBackToTop}
	label={t('devices.infiniteScroll.backToTop')}
	onClick={scrollToTop}
/>

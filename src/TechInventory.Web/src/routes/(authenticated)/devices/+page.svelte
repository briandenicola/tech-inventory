<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import {
		fetchDevicesPage,
		fetchAllDevicesForGrouping,
		invalidateDevicesCache,
		serializeDeviceFilters,
		useDevices,
		MAX_GROUPED_DEVICES,
		type DeviceFilters as DeviceFiltersType,
		type DeviceResponse
	} from '$lib/queries/devices.svelte';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { registerDeviceCreateHandler } from '$lib/stores/deviceCreate';
	import { displayMode } from '$lib/stores/displayMode.svelte';
	import { buildDevicesUrlParams, STATUS_ALL_SENTINEL, GROUP_BY_NONE_SENTINEL } from '$lib/utils/deviceFilterUrl';
	import { memberRoles, isNavItemVisible, type AppNavItem } from '$lib/navigation/appNav';
	import AddDeviceFab from '$lib/components/AddDeviceFab.svelte';
	import BackToTopFab from '$lib/components/BackToTopFab.svelte';
	import {
		getDevicesDefaultView,
		setDevicesDefaultView,
		clearDevicesDefaultView,
		normalizeQueryString,
		getDevicesViewMode,
		setDevicesViewMode,
		getTableColumns,
		DEFAULT_TABLE_COLUMNS,
		type DevicesViewMode,
		type TableColumnId
	} from '$lib/stores/userPrefs';
	import {
		getDevicesViewState,
		setDevicesViewState,
		clearDevicesViewState
	} from '$lib/stores/viewState';
	import { showToast } from '$lib/stores/toast';
	import DeviceTable from '$lib/components/DeviceTable.svelte';
	import DeviceFilters from '$lib/components/DeviceFilters.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import AddDeviceModal from '$lib/components/AddDeviceModal.svelte';
	import DeviceDetailModal from '$lib/components/DeviceDetailModal.svelte';
	import BulkActionBar from '$lib/components/BulkActionBar.svelte';
	import BulkUpdateModal from '$lib/components/BulkUpdateModal.svelte';
	import BulkDeleteModal from '$lib/components/BulkDeleteModal.svelte';
	import { devices as devicesApi } from '$lib/api/client';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import { groupDevices } from '$lib/utils/groupDevices';
	import { getApiErrorMessage } from '$lib/utils/apiErrors';

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
	// F045 B follow-up: same role gate AppBottomNav applies to its own Add
	// button — reused here so `?add=1` (the URL the bottom-nav Add action
	// navigates to) can't bypass the Viewer restriction.
	const addGateItem: AppNavItem = { href: '', labelKey: '', activePaths: [], roles: memberRoles };
	// #78: kept as a JS matchMedia check (not superseded by the app.css global
	// override) because it gates actual behavior, not just animation timing —
	// it switches between infinite-scroll and paginated mode and chooses
	// scrollIntoView's 'auto' vs 'smooth', neither of which a CSS override can do.
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

	// Grouped mode state: loading all pages for complete grouping
	let groupedDevices = $state<DeviceResponse[]>([]);
	let groupedTotalCount = $state(0);
	let isLoadingGrouped = $state(false);
	let groupedError = $state<string | null>(null);
	let activeGroupedKey = $state('');

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
	// no way to opt back out. (STATUS_ALL_SENTINEL is imported from
	// deviceFilterUrl so the read side here and the write side in
	// `updateFilters` share one definition.)
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
		let includeAllStatuses = false;
		if (rawStatus === STATUS_ALL_SENTINEL) {
			status = undefined;
			includeAllStatuses = true;
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
			includeAllStatuses,
			purchaseYearMin: params.get('yearMin') ? parseInt(params.get('yearMin')!, 10) : undefined,
			purchaseYearMax: params.get('yearMax') ? parseInt(params.get('yearMax')!, 10) : undefined,
			sort: (params.get('sort') as 'name' | 'purchaseDate' | 'createdAt') || undefined,
			sortDir: (params.get('sortDir') as 'asc' | 'desc') || 'asc',
			groupBy: (params.get('groupBy') as 'category' | 'owner' | 'year' | 'none' | null) || undefined
		};
		return filters;
	});

	// F026: did the user explicitly opt out of the Active default this session?
	// Stays true only inside the bare-URL → Active-default branch.
	const statusIsImplicitActive = $derived($page.url.searchParams.get('status') === null);
	const showingAllStatuses = $derived($page.url.searchParams.get('status') === STATUS_ALL_SENTINEL);

	// F045 R2/R5: in installed-PWA mode only, absent an explicit `groupBy`,
	// implicitly group by category. This never round-trips to the URL and
	// never counts as an active filter — it is pure presentation, not a user
	// choice. `?groupBy=none` is a real sentinel (not just "param absent") so a
	// user who explicitly picks "None" while in app mode can override the
	// implicit default; see `buildDevicesUrlParams` (deviceFilterUrl.ts) for
	// the write side — B2's fix lives there, not here: the read side below
	// was already correct.
	const groupByExplicitlyNone = $derived(
		$page.url.searchParams.get('groupBy') === GROUP_BY_NONE_SENTINEL
	);
	// implicitGroupingActive: true only when the PWA's implicit category
	// default is in effect with no explicit user choice at all — distinct
	// from groupByExplicitlyNone (explicit opt-out) and from an explicit
	// dimension pick. Drives the presentation-only note in DeviceFilters
	// (never written to the URL or counted in activeFilterCount).
	const implicitGroupingActive = $derived(
		displayMode.isPwa && !urlFilters.groupBy && !groupByExplicitlyNone
	);
	const effectiveGroupBy = $derived.by(() => {
		if (groupByExplicitlyNone) return undefined;
		if (urlFilters.groupBy && urlFilters.groupBy !== 'none') return urlFilters.groupBy;
		if (displayMode.isPwa) return 'category';
		return undefined;
	});

	// Devices query (reactive — pass a getter so filter changes propagate).
	// Grouped mode uses a separate loading path via fetchAllDevicesForGrouping,
	// so the standard query stays on page 1 with a small page size for the initial
	// load only. Infinite scroll keeps the API on page 1 and progressively appends
	// pages on the client unless reduced motion requests the classic pagination fallback.
	const queryFilters = $derived.by(() => {
		if (effectiveGroupBy) {
			// Grouped mode: query is only for the loading state, actual data comes from groupedDevices
			return { ...urlFilters, page: 1, pageSize: 25 };
		}

		return prefersReducedMotion ? urlFilters : { ...urlFilters, page: 1 };
	});
	const query = useDevices(() => queryFilters);
	const infiniteBaseFilters = $derived.by(() => ({ ...urlFilters, page: 1 }));
	const infiniteFiltersKey = $derived(serializeDeviceFilters(infiniteBaseFilters));
	const groupedFiltersKey = $derived(
		serializeDeviceFilters({ ...urlFilters, groupBy: effectiveGroupBy, page: 1 })
	);
	const displayedDevices = $derived.by(() => {
		if (effectiveGroupBy) {
			return groupedDevices;
		}
		
		if (prefersReducedMotion) {
			return query.data?.items ?? [];
		}

		return infiniteItems.length > 0 ? infiniteItems : (query.data?.items ?? []);
	});
	const hasMorePages = $derived(
		!effectiveGroupBy &&
			!prefersReducedMotion &&
			displayedDevices.length > 0 &&
			displayedDevices.length < totalCount
	);

	// Mobile drawer state
	let filtersOpen = $state(false);

	// Search debounce timeout
	let searchTimeout: ReturnType<typeof setTimeout> | null = $state(null);

	// Mobile view mode (cards or table) — desktop/mobile-web only. Ignored in PWA (#142).
	let mobileViewMode = $state<DevicesViewMode>('cards');

	// Table column visibility/order preference
	let tableColumns = $state<TableColumnId[]>([...DEFAULT_TABLE_COLUMNS]);

	// #145: PWA selection mode — row checkboxes are hidden by default in the
	// installed PWA. The user opts in via the filter panel's "Enable Selection Mode"
	// command. Exiting clears the current selection.
	let pwaSelectionMode = $state(false);

	function enablePwaSelection() {
		pwaSelectionMode = true;
	}
	function disablePwaSelection() {
		pwaSelectionMode = false;
		clearSelection();
	}

	// Add Device modal state (D-137 — Apple-elegant modal replaces /devices/new flow)
	let createModalOpen = $state(false);
	const selectedDeviceId = $derived($page.url.searchParams.get('device'));

	// Update URL when filters change
	function updateFilters(newFilters: DeviceFiltersType) {
		const params = buildDevicesUrlParams(newFilters, { isPwa: displayMode.isPwa });

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

	// Count of active filters for badge display on the Filter button
	const activeFilterCount = $derived.by(() => {
		let count = 0;
		if (urlFilters.search) count++;
		if (urlFilters.brandId) count++;
		if (urlFilters.categoryId) count++;
		if (urlFilters.ownerId) count++;
		if (urlFilters.locationId) count++;
		if (urlFilters.networkId) count++;
		if (!statusIsImplicitActive && urlFilters.status && urlFilters.status.length > 0) count++;
		if (showingAllStatuses) count++;
		if (urlFilters.purchaseYearMin) count++;
		if (urlFilters.purchaseYearMax) count++;
		return count;
	});

	// F022: per-user default view persistence (localStorage).
	// Apply a stored default exactly once on first mount when the URL is bare;
	// explicit deep-links always win.
	let storedDefault = $state<string | null>(null);
	function refreshStoredDefault() {
		storedDefault = getDevicesDefaultView(currentUser?.id);
	}
	onMount(() => {
		refreshStoredDefault();
		
		// Check for session view state first (navigation continuity)
		const sessionState = getDevicesViewState();
		const bareEntry = $page.url.search === '' || $page.url.search === '?';
		
		if (sessionState && bareEntry) {
			// Restore session state and clear it (one-time restore)
			clearDevicesViewState();
			void goto(`?${sessionState}`, { replaceState: true, keepFocus: true, noScroll: true });
		} else if (bareEntry && storedDefault) {
			// Fall back to saved default view if no session state
			void goto(`?${storedDefault}`, { replaceState: true, keepFocus: true, noScroll: true });
		}

		// F045 R2/B follow-up: `?add=1` is how the bottom-nav Add action reaches
		// this page when the user was elsewhere (see deviceCreate.ts). Gated the
		// same way AppBottomNav gates its own Add button — a Viewer navigating
		// straight to `?add=1` must not get a create modal via URL bypass.
		// Strip the param unconditionally (so it never reopens on
		// refresh/back-navigation) but only open the modal when permitted.
		if ($page.url.searchParams.get('add') === '1') {
			if (isNavItemVisible(addGateItem, currentUser?.role ?? null)) {
				createModalOpen = true;
			}
			const strippedParams = new URLSearchParams($page.url.searchParams);
			strippedParams.delete('add');
			const strippedUrl = strippedParams.toString()
				? `?${strippedParams.toString()}`
				: $page.url.pathname;
			void goto(strippedUrl, { replaceState: true, keepFocus: true, noScroll: true });
		}

		const savedViewMode = getDevicesViewMode(currentUser?.id);
		if (savedViewMode) {
			mobileViewMode = savedViewMode;
		}

		const savedColumns = getTableColumns(currentUser?.id);
		if (savedColumns && savedColumns.length > 0) {
			tableColumns = savedColumns;
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
	
	// Persist current view state to session storage whenever URL changes (for navigation continuity)
	$effect(() => {
		setDevicesViewState($page.url.search);
	});

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

	// F031: mobile view mode toggle handler.
	function setViewMode(mode: DevicesViewMode) {
		mobileViewMode = mode;
		if (currentUser?.id) {
			setDevicesViewMode(currentUser.id, mode);
		}
	}

	// F023: group devices client-side when a groupBy dimension is active.
	// Pulls reference data for human-readable labels (Category/Owner names).
	const refData = $derived($referenceDataStore);
	const groupedView = $derived.by(() => {
		if (!effectiveGroupBy || groupedDevices.length === 0) return undefined;
		return groupDevices(
			groupedDevices,
			effectiveGroupBy,
			{ categories: refData.categories, owners: refData.owners },
			t('devices.groups.unknown')
		);
	});

	async function loadNextPage() {
		if (prefersReducedMotion || effectiveGroupBy || isLoadingMore || !hasMorePages) {
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
			loadMoreError = getApiErrorMessage(err, t('devices.infiniteScroll.loadError'));
		} finally {
			isLoadingMore = false;
		}
	}

	async function refreshDevicesList() {
		invalidateDevicesCache();
		loadMoreError = null;

		if (!prefersReducedMotion && !effectiveGroupBy) {
			activeInfiniteKey = '';
			infiniteItems = [];
			infinitePage = 0;
			totalCount = 0;
		}

		if (effectiveGroupBy) {
			activeGroupedKey = '';
			groupedDevices = [];
			groupedTotalCount = 0;
			groupedError = null;
		}

		await query.refetch();
	}

	async function loadGroupedDevices() {
		if (!effectiveGroupBy || isLoadingGrouped) {
			return;
		}

		isLoadingGrouped = true;
		groupedError = null;

		try {
			// F045 B4/D-182: the row cap is scoped to the standalone-PWA caller
			// only — desktop and mobile-web grouped fetches stay unbounded,
			// matching their pre-F045 behavior, which F045 must not change.
			const result = await fetchAllDevicesForGrouping(
				{ ...urlFilters, groupBy: effectiveGroupBy },
				displayMode.isPwa ? MAX_GROUPED_DEVICES : undefined
			);
			groupedDevices = result.items ?? [];
			groupedTotalCount = result.totalCount;
		} catch (err) {
			groupedError = getApiErrorMessage(err, t('devices.grouped.loadError'));
			groupedDevices = [];
			groupedTotalCount = 0;
		} finally {
			isLoadingGrouped = false;
		}
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

	// F045 R2: bridges the bottom-nav Add action (owned by the root layout) to
	// this page's create-modal state, mirroring the pullToRefresh registration
	// above. Only registered while this page is mounted.
	$effect(() => {
		const unregister = registerDeviceCreateHandler('/devices', () => {
			createModalOpen = true;
		});
		return unregister;
	});

	// Effect: Load all devices when grouped mode is active
	$effect(() => {
		if (!effectiveGroupBy) {
			return;
		}

		const key = groupedFiltersKey;
		if (key === activeGroupedKey) {
			return;
		}

		activeGroupedKey = key;
		groupedDevices = [];
		groupedTotalCount = 0;
		groupedError = null;
		void loadGroupedDevices();
	});

	$effect(() => {
		if (prefersReducedMotion || effectiveGroupBy) {
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
		if (prefersReducedMotion || effectiveGroupBy || !query.data) {
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
		if (prefersReducedMotion || effectiveGroupBy || !sentinel || !hasMorePages || loadMoreError) {
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
		void effectiveGroupBy;
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

<svelte:head>
	<title>{t('devices.list.title')} — {t('app.title')}</title>
</svelte:head>

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
	{implicitGroupingActive}
	isPwa={displayMode.isPwa}
	{pwaSelectionMode}
	onEnablePwaSelection={enablePwaSelection}
	onDisablePwaSelection={disablePwaSelection}
/>

<!-- Main content -->
<div class="-mt-8 flex flex-col">
	<!-- Sticky page header (below app header) -->
	<div
		class="sticky top-[73px] z-20 -mx-4 border-b border-neutral-200 bg-white dark:border-neutral-800 dark:bg-neutral-900 sm:-mx-6 lg:-mx-8"
	>
		<div class="px-4 pt-4 pb-4 sm:px-6 lg:px-8">
			<!-- Header row -->
			<div class="flex items-center justify-between gap-4">
				<h1 class="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
					{t('devices.list.title')}
				</h1>

				<!-- Right-side actions -->
				<div class="flex items-center gap-2">
					<!-- Cards/Table view-mode toggle — shown only on mobile-web; hidden in
							 installed PWA (#142: PWA has a single list presentation). -->
						{#if !displayMode.isPwa}
						<div
							class="md:hidden inline-flex items-center rounded-full bg-neutral-100 p-1 dark:bg-neutral-800"
							role="group"
							aria-label={t('devices.viewMode.toggleLabel')}
						>
							<button
								type="button"
								onclick={() => setViewMode('cards')}
								aria-pressed={mobileViewMode === 'cards'}
								class="min-h-11 px-3 rounded-full transition-colors {mobileViewMode === 'cards'
									? 'bg-white shadow-sm text-neutral-900 dark:bg-neutral-700 dark:text-neutral-50'
									: 'text-neutral-600 dark:text-neutral-400'}"
							>
								<svg
									class="h-5 w-5"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
									aria-hidden="true"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M4 6h6v6H4zM14 6h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z"
									/>
								</svg>
								<span class="sr-only">{t('devices.viewMode.cards')}</span>
							</button>
							<button
								type="button"
								onclick={() => setViewMode('table')}
								aria-pressed={mobileViewMode === 'table'}
								class="min-h-11 px-3 rounded-full transition-colors {mobileViewMode === 'table'
									? 'bg-white shadow-sm text-neutral-900 dark:bg-neutral-700 dark:text-neutral-50'
									: 'text-neutral-600 dark:text-neutral-400'}"
							>
								<svg
									class="h-5 w-5"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
									aria-hidden="true"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M3 5h18M3 12h18M3 19h18"
									/>
								</svg>
								<span class="sr-only">{t('devices.viewMode.table')}</span>
							</button>
						</div>
						{/if}

						<!-- Filter button with active count badge -->
						<button
							type="button"
							onclick={() => (filtersOpen = !filtersOpen)}
						class="relative inline-flex items-center gap-2 rounded-lg bg-neutral-100 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-300 dark:hover:bg-neutral-700"
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
						{#if activeFilterCount > 0}
							<span class="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary-600 text-xs font-bold text-white dark:bg-primary-500">
								{activeFilterCount}
							</span>
						{/if}
					</button>

					<!-- Add Device CTA (desktop-browser only; mobile-web uses the FAB below,
						 app mode uses the bottom-nav pill's Add action instead). -->
					{#if !displayMode.isPwa}
					<button
						type="button"
						onclick={() => (createModalOpen = true)}
						class="hidden md:inline-flex min-h-11 items-center gap-2 rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
					>
						<svg
							class="h-5 w-5"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
							aria-hidden="true"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 4v16m8-8H4"
							/>
						</svg>
						{t('devices.list.addButton')}
					</button>
					{/if}
				</div>
			</div>

			<!-- Search input (F045: full-width in app mode, capped on desktop browser) -->
			<div class="mt-4 w-full {displayMode.isPwa ? '' : 'md:max-w-lg'}">
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
						class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 pl-11 pr-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
					/>
				</div>
			</div>
		</div>
	</div>

	<!-- Scrollable content area (F045: extra bottom padding in app mode clears
		 the fixed bottom-nav pill/bubble; safe-area-aware). -->
	<div class="pt-4" style={displayMode.isPwa ? 'padding-bottom: calc(env(safe-area-inset-bottom, 0px) + 5.5rem);' : ''}>

	<!-- Content: loading / error / empty / success -->
	{#if query.isLoading || (effectiveGroupBy && isLoadingGrouped)}
		<LoadingSkeleton rows={7} />
	{:else if query.error || (effectiveGroupBy && groupedError)}
		<ErrorState error={query.error || groupedError || 'Unknown error'} onRetry={refreshDevicesList} />
	{:else if displayedDevices.length === 0}
		<!--
			showAddAction=false: the persistent header "Add Device" CTA (desktop)
			and AddDeviceFab (mobile) already cover every viewport, so the
			empty-state's own CTA would just be a second identically-labeled
			button on screen at the same time.
		-->
		<EmptyState filtered={hasActiveFilters} showAddAction={false} />
	{:else}
		<DeviceTable
			devices={displayedDevices}
			groups={groupedView}
			currentSort={urlFilters.sort}
			sortDir={urlFilters.sortDir}
			onSort={handleSort}
			selectable={displayMode.isPwa ? pwaSelectionMode : true}
			{selectedIds}
			onToggleSelect={toggleSelect}
			onToggleSelectAll={toggleSelectAllVisible}
			{allVisibleSelected}
			{someVisibleSelected}
			{mobileViewMode}
			onOpenDevice={openDeviceDetail}
			visibleColumns={tableColumns}
			presentation={displayMode.isPwa ? 'pwa' : 'auto'}
			{currentUser}
			onChanged={refreshDevicesList}
		/>

		{#if effectiveGroupBy}
			<!-- Grouped mode renders a single expanded page with all matching devices,
				 capped at MAX_GROUPED_DEVICES for standalone-PWA callers only
				 (F045 R5/B4/D-182) — desktop/mobile-web grouped fetches are
				 unbounded, so the truncation note must never render for them. -->
			<div class="mt-6 text-center text-sm text-neutral-600 dark:text-neutral-400">
				{#if displayMode.isPwa && groupedTotalCount > MAX_GROUPED_DEVICES}
					{t('devices.list.showingFirst', { count: MAX_GROUPED_DEVICES, total: groupedTotalCount })}
				{:else if groupedTotalCount > 0}
					{t('devices.grouped.showingAll', { count: groupedTotalCount })}
				{/if}
			</div>
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
							class="rounded-lg border border-danger-300 px-3 py-2 font-medium transition-colors hover:bg-danger-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-500 dark:border-danger-700 dark:hover:bg-danger-900/60"
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
</div>

{#if createModalOpen}
	<AddDeviceModal
		onClose={() => (createModalOpen = false)}
		onCreated={() => void refreshDevicesList()}
	/>
{/if}

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

<AddDeviceFab
	visible={selectedIds.size === 0 && !createModalOpen && !selectedDeviceId && !displayMode.isPwa}
	label={t('devices.list.addFab')}
	onClick={() => (createModalOpen = true)}
/>

<BackToTopFab
	visible={showBackToTop}
	label={t('devices.infiniteScroll.backToTop')}
	onClick={scrollToTop}
/>

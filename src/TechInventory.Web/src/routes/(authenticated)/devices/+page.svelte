<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import {
		clampDevicePage,
		clampDevicePageSize,
		fetchDevicesPage,
		invalidateDevicesCache,
		serializeDeviceFilters,
		useDevices,
		type DeviceFilters as DeviceFiltersType,
		type DeviceResponse
	} from '$lib/queries/devices.svelte';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import BackToTopFab from '$lib/components/BackToTopFab.svelte';
	import DeviceTable from '$lib/components/DeviceTable.svelte';
	import DeviceFilters from '$lib/components/DeviceFilters.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';

	const currentUser = $derived($authStore.currentUser);
	const initialReducedMotion =
		typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

	let filtersOpen = $state(false);
	let prefersReducedMotion = $state(initialReducedMotion);
	let showBackToTop = $state(false);
	let infiniteItems = $state<DeviceResponse[]>([]);
	let infinitePage = $state(0);
	let totalCount = $state(0);
	let isLoadingMore = $state(false);
	let loadMoreError = $state<string | null>(null);
	let activeInfiniteKey = $state('');
	let sentinel = $state<HTMLDivElement | null>(null);

	const urlFilters = $derived.by(() => {
		const params = $page.url.searchParams;
		const pageParam = Number.parseInt(params.get('page') ?? '', 10);
		const pageSizeParam = Number.parseInt(params.get('pageSize') ?? '', 10);
		const purchaseYearMinParam = Number.parseInt(params.get('yearMin') ?? '', 10);
		const purchaseYearMaxParam = Number.parseInt(params.get('yearMax') ?? '', 10);
		const status = params.get('status');

		const filters: DeviceFiltersType = {
			page: clampDevicePage(Number.isNaN(pageParam) ? undefined : pageParam),
			pageSize: clampDevicePageSize(Number.isNaN(pageSizeParam) ? undefined : pageSizeParam),
			search: params.get('search') || undefined,
			brandId: params.get('brandId') || undefined,
			categoryId: params.get('categoryId') || undefined,
			ownerId: params.get('ownerId') || undefined,
			locationId: params.get('locationId') || undefined,
			networkId: params.get('networkId') || undefined,
			status: status
				? [status as 'Active' | 'Retired' | 'Disposed' | 'InRepair' | 'Lent']
				: undefined,
			purchaseYearMin: Number.isNaN(purchaseYearMinParam) ? undefined : purchaseYearMinParam,
			purchaseYearMax: Number.isNaN(purchaseYearMaxParam) ? undefined : purchaseYearMaxParam,
			sort: (params.get('sort') as 'name' | 'purchaseDate' | 'createdAt') || undefined,
			sortDir: (params.get('sortDir') as 'asc' | 'desc') || 'asc'
		};

		return filters;
	});

	const infiniteBaseFilters = $derived.by(() => ({ ...urlFilters, page: 1 }));
	const activeQueryFilters = $derived.by(() =>
		prefersReducedMotion ? urlFilters : infiniteBaseFilters
	);
	const query = useDevices(() => activeQueryFilters);
	const infiniteFiltersKey = $derived(serializeDeviceFilters(infiniteBaseFilters));
	const displayedDevices = $derived(
		prefersReducedMotion ? (query.data?.items ?? []) : infiniteItems
	);
	const hasMorePages = $derived(
		!prefersReducedMotion && displayedDevices.length > 0 && displayedDevices.length < totalCount
	);
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

	function updateFilters(newFilters: DeviceFiltersType) {
		const normalizedFilters: DeviceFiltersType = {
			...newFilters,
			page: clampDevicePage(newFilters.page),
			pageSize: clampDevicePageSize(newFilters.pageSize)
		};
		const params = new URLSearchParams();

		if (normalizedFilters.page && normalizedFilters.page !== 1) {
			params.set('page', normalizedFilters.page.toString());
		}
		if (normalizedFilters.pageSize && normalizedFilters.pageSize !== 25) {
			params.set('pageSize', normalizedFilters.pageSize.toString());
		}
		if (normalizedFilters.search) params.set('search', normalizedFilters.search);
		if (normalizedFilters.brandId) params.set('brandId', normalizedFilters.brandId);
		if (normalizedFilters.categoryId) params.set('categoryId', normalizedFilters.categoryId);
		if (normalizedFilters.ownerId) params.set('ownerId', normalizedFilters.ownerId);
		if (normalizedFilters.locationId) params.set('locationId', normalizedFilters.locationId);
		if (normalizedFilters.networkId) params.set('networkId', normalizedFilters.networkId);
		if (normalizedFilters.status && normalizedFilters.status.length > 0) {
			params.set('status', normalizedFilters.status[0]);
		}
		if (normalizedFilters.purchaseYearMin) {
			params.set('yearMin', normalizedFilters.purchaseYearMin.toString());
		}
		if (normalizedFilters.purchaseYearMax) {
			params.set('yearMax', normalizedFilters.purchaseYearMax.toString());
		}
		if (normalizedFilters.sort) params.set('sort', normalizedFilters.sort);
		if (normalizedFilters.sortDir && normalizedFilters.sortDir !== 'asc') {
			params.set('sortDir', normalizedFilters.sortDir);
		}

		const url = params.toString() ? `?${params.toString()}` : $page.url.pathname;
		goto(url, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function handleSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		const newDir = urlFilters.sort === column && urlFilters.sortDir === 'asc' ? 'desc' : 'asc';
		updateFilters({ ...urlFilters, sort: column, sortDir: newDir, page: 1 });
	}

	function handlePageChange(pageNumber: number, pageSize: number) {
		updateFilters({ ...urlFilters, page: pageNumber, pageSize });
	}

	async function loadNextPage() {
		if (prefersReducedMotion || isLoadingMore || !hasMorePages) {
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
		} catch (error) {
			loadMoreError =
				error instanceof Error ? error.message : t('devices.infiniteScroll.loadError');
		} finally {
			isLoadingMore = false;
		}
	}

	async function refreshDevicesList() {
		invalidateDevicesCache();
		loadMoreError = null;

		if (!prefersReducedMotion) {
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

	onMount(() => {
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

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, refreshDevicesList);
		return unregister;
	});

	$effect(() => {
		if (prefersReducedMotion) {
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
		if (prefersReducedMotion || !query.data) {
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
		if (prefersReducedMotion || !sentinel || !hasMorePages || loadMoreError) {
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
</script>

<div class="flex min-h-screen">
	<DeviceFilters
		filters={urlFilters}
		onFiltersChange={updateFilters}
		isOpen={filtersOpen}
		onClose={() => (filtersOpen = false)}
	/>

	<div class="flex-1 p-6">
		<div
			class="mb-6 flex items-center justify-between border-b border-neutral-200 pb-4 dark:border-neutral-800"
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

			<div class="flex items-center gap-2">
				<button
					type="button"
					onclick={() => (filtersOpen = !filtersOpen)}
					class="md:hidden inline-flex items-center gap-2 rounded-lg bg-neutral-100 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-200 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-300 dark:hover:bg-neutral-700"
					aria-expanded={filtersOpen}
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
							d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
						/>
					</svg>
					{t('devices.filters.filterButton')}
				</button>

				<a
					href="/devices/new"
					class="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
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
				</a>
			</div>
		</div>

		{#if query.isLoading}
			<LoadingSkeleton rows={7} />
		{:else if query.error}
			<ErrorState error={query.error} onRetry={query.refetch} />
		{:else if displayedDevices.length === 0}
			<EmptyState filtered={hasActiveFilters} />
		{:else}
			<DeviceTable
				devices={displayedDevices}
				currentSort={urlFilters.sort}
				sortDir={urlFilters.sortDir}
				onSort={handleSort}
			/>

			<div class="reduce-motion-only mt-6">
				<PaginationControls
					currentPage={query.data?.page ?? urlFilters.page ?? 1}
					pageSize={query.data?.pageSize ?? urlFilters.pageSize ?? 25}
					totalCount={query.data?.totalCount ?? totalCount}
					onPageChange={handlePageChange}
				/>
			</div>

			<div class="motion-safe-only mt-6" aria-live="polite" aria-atomic="true">
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
	</div>
</div>

<BackToTopFab
	visible={showBackToTop}
	label={t('devices.infiniteScroll.backToTop')}
	onClick={scrollToTop}
/>

<style>
	.reduce-motion-only {
		display: none;
	}

	@media (prefers-reduced-motion: reduce) {
		.motion-safe-only {
			display: none;
		}

		.reduce-motion-only {
			display: block;
		}
	}
</style>

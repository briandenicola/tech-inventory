<!--
	DeviceFilters.svelte — T16 Filters sidebar (search + facets)
	
	Layout: Sidebar on desktop (~280px left of table), collapsible drawer on mobile.
	Controls: search (debounced 300ms), brand/category/owner/location/network dropdowns,
	status multi-select, purchase year range, "Clear all" button.
	URL-backed via $page.url.searchParams.
	Reference data cached in referenceDataStore (fetch once on mount).
	
	Related: specs/002-frontend-mvp/spec.md §5, T16
-->
<script lang="ts">
	import { onMount } from 'svelte';
	import { t } from '$lib/i18n';
	import { referenceDataStore, fetchReferenceData } from '$lib/stores/referenceData';
	import type { DeviceFilters, DeviceStatus } from '$lib/queries/devices';

	interface Props {
		filters: DeviceFilters;
		onFiltersChange: (filters: DeviceFilters) => void;
		isOpen?: boolean;
		onClose?: () => void;
	}

	let { filters, onFiltersChange, isOpen = true, onClose }: Props = $props();

	const refData = $derived($referenceDataStore);

	// Define status options with proper typing
	const statusOptions: DeviceStatus[] = ['Active', 'Retired', 'Disposed', 'InRepair', 'Lent'];

	// Debounced search
	let searchTimeout: ReturnType<typeof setTimeout> | null = null;
	function handleSearchChange(e: Event) {
		const target = e.target as HTMLInputElement;
		const value = target.value;

		if (searchTimeout) clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			onFiltersChange({ ...filters, search: value || undefined, page: 1 });
		}, 300);
	}

	// Filter change handlers
	function handleFilterChange(
		key: keyof DeviceFilters,
		value: string | number | DeviceStatus[] | undefined
	) {
		onFiltersChange({ ...filters, [key]: value || undefined, page: 1 });
	}

	// Status multi-select
	let statusValues = $derived(filters.status || []);
	function toggleStatus(status: DeviceStatus) {
		const current = [...statusValues];
		const index = current.indexOf(status);
		if (index > -1) {
			current.splice(index, 1);
		} else {
			current.push(status);
		}
		onFiltersChange({ ...filters, status: current.length > 0 ? current : undefined, page: 1 });
	}

	// Clear all filters
	function clearAll() {
		onFiltersChange({ page: 1, pageSize: filters.pageSize || 25 });
	}

	// Fetch reference data on mount
	onMount(() => {
		if (!refData.brands.length && !refData.isLoading) {
			void fetchReferenceData();
		}
	});

	// Current year for year range max
	const currentYear = new Date().getFullYear();
</script>

<!-- Mobile drawer backdrop -->
{#if !isOpen && onClose}
	<div
		class="md:hidden fixed inset-0 z-40 bg-neutral-900/50"
		onclick={onClose}
		role="presentation"
	></div>
{/if}

<!-- Filters sidebar/drawer -->
<aside
	class="fixed inset-y-0 left-0 z-50 w-80 transform overflow-y-auto border-r border-neutral-200 bg-white p-6 transition-transform dark:border-neutral-800 dark:bg-neutral-950 md:sticky md:top-0 md:z-auto md:h-screen md:w-72 md:translate-x-0"
	class:translate-x-0={isOpen}
	class:-translate-x-full={!isOpen}
	aria-label={t('devices.filters.title')}
>
	<!-- Header -->
	<div class="mb-6 flex items-center justify-between">
		<h2 class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
			{t('devices.filters.title')}
		</h2>
		{#if onClose}
			<button
				type="button"
				onclick={onClose}
				class="md:hidden rounded-lg p-1 hover:bg-neutral-100 dark:hover:bg-neutral-900"
				aria-label={t('devices.filters.closeFilters')}
			>
				<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M6 18L18 6M6 6l12 12"
					/>
				</svg>
			</button>
		{/if}
	</div>

	<!-- Search -->
	<div class="mb-4">
		<label for="search" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.searchPlaceholder')}
		</label>
		<input
			type="search"
			id="search"
			value={filters.search || ''}
			oninput={handleSearchChange}
			placeholder={t('devices.filters.searchPlaceholder')}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		/>
	</div>

	<!-- Brand -->
	<div class="mb-4">
		<label for="brand" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.brandLabel')}
		</label>
		<select
			id="brand"
			value={filters.brandId || ''}
			onchange={(e) => handleFilterChange('brandId', (e.target as HTMLSelectElement).value)}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.brands as brand (brand.id)}
				<option value={brand.id}>{brand.name}</option>
			{/each}
		</select>
	</div>

	<!-- Category -->
	<div class="mb-4">
		<label for="category" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.categoryLabel')}
		</label>
		<select
			id="category"
			value={filters.categoryId || ''}
			onchange={(e) => handleFilterChange('categoryId', (e.target as HTMLSelectElement).value)}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.categories as category (category.id)}
				<option value={category.id}>{category.name}</option>
			{/each}
		</select>
	</div>

	<!-- Owner -->
	<div class="mb-4">
		<label for="owner" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.ownerLabel')}
		</label>
		<select
			id="owner"
			value={filters.ownerId || ''}
			onchange={(e) => handleFilterChange('ownerId', (e.target as HTMLSelectElement).value)}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.owners as owner (owner.id)}
				<option value={owner.id}>{owner.name}</option>
			{/each}
		</select>
	</div>

	<!-- Location -->
	<div class="mb-4">
		<label for="location" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.locationLabel')}
		</label>
		<select
			id="location"
			value={filters.locationId || ''}
			onchange={(e) => handleFilterChange('locationId', (e.target as HTMLSelectElement).value)}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.locations as location (location.id)}
				<option value={location.id}>{location.name}</option>
			{/each}
		</select>
	</div>

	<!-- Network -->
	<div class="mb-4">
		<label for="network" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.networkLabel')}
		</label>
		<select
			id="network"
			value={filters.networkId || ''}
			onchange={(e) => handleFilterChange('networkId', (e.target as HTMLSelectElement).value)}
			class="mt-1 w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.networks as network (network.id)}
				<option value={network.id}>{network.name}</option>
			{/each}
		</select>
	</div>

	<!-- Status multi-select -->
	<fieldset class="mb-4">
		<legend class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.statusLabel')}
		</legend>
		<div class="mt-2 space-y-2">
			{#each statusOptions as status}
				<label class="flex items-center gap-2">
					<input
						type="checkbox"
						checked={statusValues.includes(status)}
						onchange={() => toggleStatus(status)}
						class="rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900"
					/>
					<span class="text-sm text-neutral-700 dark:text-neutral-300">
						{t(`devices.filters.status${status}`)}
					</span>
				</label>
			{/each}
		</div>
	</fieldset>

	<!-- Purchase Year Range -->
	<fieldset class="mb-6">
		<legend class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
			{t('devices.filters.yearRangeLabel')}
		</legend>
		<div class="mt-2 flex gap-2">
			<div class="flex-1">
				<label for="yearMin" class="sr-only">{t('devices.filters.yearMinLabel')}</label>
				<input
					type="number"
					id="yearMin"
					min="1990"
					max={currentYear}
					value={filters.purchaseYearMin || ''}
					oninput={(e) =>
						handleFilterChange(
							'purchaseYearMin',
							parseInt((e.target as HTMLInputElement).value, 10) || undefined
						)}
					placeholder="1990"
					class="w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</div>
			<div class="flex-1">
				<label for="yearMax" class="sr-only">{t('devices.filters.yearMaxLabel')}</label>
				<input
					type="number"
					id="yearMax"
					min="1990"
					max={currentYear}
					value={filters.purchaseYearMax || ''}
					oninput={(e) =>
						handleFilterChange(
							'purchaseYearMax',
							parseInt((e.target as HTMLInputElement).value, 10) || undefined
						)}
					placeholder={currentYear.toString()}
					class="w-full rounded-md border-neutral-300 text-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</div>
		</div>
	</fieldset>

	<!-- Clear all button -->
	<button
		type="button"
		onclick={clearAll}
		class="w-full rounded-lg border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-900"
	>
		{t('devices.filters.clearAll')}
	</button>

	<!-- ARIA live region for results announcement -->
	<div class="sr-only" role="status" aria-live="polite" aria-atomic="true">
		<!-- Will be populated by parent component with result count -->
	</div>
</aside>

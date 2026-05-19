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
	import type { DeviceFilters, DeviceStatus } from '$lib/queries/devices.svelte';

	interface Props {
		filters: DeviceFilters;
		onFiltersChange: (filters: DeviceFilters) => void;
		isOpen?: boolean;
		onClose?: () => void;
		onSaveDefault?: () => void;
		onClearDefault?: () => void;
		hasStoredDefault?: boolean;
		canSaveDefault?: boolean;
	}

	let {
		filters,
		onFiltersChange,
		isOpen = true,
		onClose,
		onSaveDefault,
		onClearDefault,
		hasStoredDefault = false,
		canSaveDefault = true
	}: Props = $props();

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
	class="fixed inset-y-0 left-0 z-50 w-[22rem] transform overflow-y-auto border-r border-neutral-200/70 bg-white p-7 transition-transform dark:border-neutral-800/70 dark:bg-neutral-950 md:sticky md:top-0 md:z-auto md:h-screen md:w-80 md:translate-x-0"
	class:translate-x-0={isOpen}
	class:-translate-x-full={!isOpen}
	aria-label={t('devices.filters.title')}
>
	<!-- Header -->
	<div class="mb-7 flex items-center justify-between">
		<h2 class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
			{t('devices.filters.title')}
		</h2>
		{#if onClose}
			<button
				type="button"
				onclick={onClose}
				class="md:hidden inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-600 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
				aria-label={t('devices.filters.closeFilters')}
			>
				<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
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
	<div class="mb-6">
		<label for="search" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.searchPlaceholder')}
		</label>
		<input
			type="search"
			id="search"
			value={filters.search || ''}
			oninput={handleSearchChange}
			placeholder={t('devices.filters.searchPlaceholder')}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
		/>
	</div>

	<!-- Brand -->
	<div class="mb-6">
		<label for="brand" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.brandLabel')}
		</label>
		<select
			id="brand"
			value={filters.brandId || ''}
			onchange={(e) => handleFilterChange('brandId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.brands as brand (brand.id)}
				<option value={brand.id}>{brand.name}</option>
			{/each}
		</select>
	</div>

	<!-- Category -->
	<div class="mb-6">
		<label for="category" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.categoryLabel')}
		</label>
		<select
			id="category"
			value={filters.categoryId || ''}
			onchange={(e) => handleFilterChange('categoryId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.categories as category (category.id)}
				<option value={category.id}>{category.name}</option>
			{/each}
		</select>
	</div>

	<!-- Owner -->
	<div class="mb-6">
		<label for="owner" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.ownerLabel')}
		</label>
		<select
			id="owner"
			value={filters.ownerId || ''}
			onchange={(e) => handleFilterChange('ownerId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.owners as owner (owner.id)}
				<option value={owner.id}>{owner.name}</option>
			{/each}
		</select>
	</div>

	<!-- Location -->
	<div class="mb-6">
		<label for="location" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.locationLabel')}
		</label>
		<select
			id="location"
			value={filters.locationId || ''}
			onchange={(e) => handleFilterChange('locationId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.locations as location (location.id)}
				<option value={location.id}>{location.name}</option>
			{/each}
		</select>
	</div>

	<!-- Network -->
	<div class="mb-6">
		<label for="network" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.networkLabel')}
		</label>
		<select
			id="network"
			value={filters.networkId || ''}
			onchange={(e) => handleFilterChange('networkId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.networks as network (network.id)}
				<option value={network.id}>{network.name}</option>
			{/each}
		</select>
	</div>

	<!-- Status multi-select -->
	<fieldset class="mb-6">
		<legend class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.statusLabel')}
		</legend>
		<div class="space-y-2.5">
			{#each statusOptions as status}
				<label class="flex min-h-10 cursor-pointer items-center gap-3 rounded-lg px-2 py-1.5 transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800">
					<input
						type="checkbox"
						checked={statusValues.includes(status)}
						onchange={() => toggleStatus(status)}
						class="h-5 w-5 rounded-md border-neutral-300 text-primary-600 focus:ring-primary-500 focus:ring-offset-0 dark:border-neutral-600 dark:bg-neutral-800"
					/>
					<span class="text-base text-neutral-800 dark:text-neutral-200">
						{t(`devices.filters.status${status}`)}
					</span>
				</label>
			{/each}
		</div>
	</fieldset>

	<!-- Purchase Year Range -->
	<fieldset class="mb-7">
		<legend class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.yearRangeLabel')}
		</legend>
		<div class="flex gap-3">
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
					class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
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
					class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
				/>
			</div>
		</div>
	</fieldset>

	<!-- Clear all button -->
	<button
		type="button"
		onclick={clearAll}
		class="inline-flex min-h-11 w-full items-center justify-center rounded-full border border-neutral-300 px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
	>
		{t('devices.filters.clearAll')}
	</button>

	{#if onSaveDefault || onClearDefault}
		<div class="mt-3 flex flex-col gap-2">
			{#if onSaveDefault}
				<button
					type="button"
					onclick={onSaveDefault}
					disabled={!canSaveDefault}
					class="inline-flex min-h-11 w-full items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-500 dark:hover:bg-primary-600"
				>
					{t('devices.filters.saveDefault')}
				</button>
			{/if}
			{#if onClearDefault && hasStoredDefault}
				<button
					type="button"
					onclick={onClearDefault}
					class="inline-flex min-h-11 w-full items-center justify-center rounded-full px-5 py-2.5 text-sm font-medium text-neutral-600 underline-offset-4 transition-colors hover:underline focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:text-neutral-400"
				>
					{t('devices.filters.clearDefault')}
				</button>
			{/if}
		</div>
	{/if}

	<!-- ARIA live region for results announcement -->
	<div class="sr-only" role="status" aria-live="polite" aria-atomic="true">
		<!-- Will be populated by parent component with result count -->
	</div>
</aside>

<!--
	/admin/export — Devices export (T38)

	Format radio (CSV/JSON) + filter panel mirroring /devices list filters.
	Calls GET /api/v1/exports/devices?... which returns a binary blob; we save
	with a timestamped filename like `devices-2026-05-19T08-30-00Z.csv`.

	Refs: PRD F4, spec.md J11.
-->
<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { exports as exportsApi } from '$lib/api/client';
	import { addToast } from '$lib/stores/toast';
	import { referenceDataStore, fetchReferenceData } from '$lib/stores/referenceData';
	import type { DeviceStatus, ExportFormat } from '$lib/api/types';

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

	const refData = $derived($referenceDataStore);

	onMount(() => {
		void fetchReferenceData();
	});

	const STATUS_OPTIONS: DeviceStatus[] = ['Active', 'Retired', 'Disposed', 'InRepair', 'Lent'];

	let format = $state<ExportFormat>('Csv');
	let search = $state('');
	let brandId = $state<string>('');
	let categoryId = $state<string>('');
	let ownerId = $state<string>('');
	let locationId = $state<string>('');
	let networkId = $state<string>('');
	let status = $state<DeviceStatus | ''>('');
	let purchaseYearFrom = $state<string>('');
	let purchaseYearTo = $state<string>('');

	let isDownloading = $state(false);
	let errorMessage = $state<string | null>(null);

	function timestampedFilename(): string {
		const stamp = new Date()
			.toISOString()
			.replace(/[:.]/g, '-')
			.replace(/T/, 'T')
			.slice(0, 19);
		const ext = format === 'Csv' ? 'csv' : 'json';
		return `devices-${stamp}Z.${ext}`;
	}

	function clearFilters() {
		search = '';
		brandId = '';
		categoryId = '';
		ownerId = '';
		locationId = '';
		networkId = '';
		status = '';
		purchaseYearFrom = '';
		purchaseYearTo = '';
	}

	function buildParams() {
		const params: Record<string, string | number | undefined> = { Format: format };
		if (search.trim()) params.Search = search.trim();
		if (brandId) params.BrandId = brandId;
		if (categoryId) params.CategoryId = categoryId;
		if (ownerId) params.OwnerId = ownerId;
		if (locationId) params.LocationId = locationId;
		if (networkId) params.NetworkId = networkId;
		if (status) params.Status = status;
		if (purchaseYearFrom) params.PurchaseYearFrom = Number(purchaseYearFrom);
		if (purchaseYearTo) params.PurchaseYearTo = Number(purchaseYearTo);
		return params;
	}

	async function handleDownload() {
		if (isDownloading) return;
		isDownloading = true;
		errorMessage = null;
		try {
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			const blob = await exportsApi.devices(buildParams() as any);
			const url = URL.createObjectURL(blob);
			const link = document.createElement('a');
			link.href = url;
			link.download = timestampedFilename();
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);
			URL.revokeObjectURL(url);
			addToast({ type: 'success', message: t('export.success') });
		} catch (err) {
			console.error('[admin/export] Download failed:', err);
			const fallback = 'Failed to download export';
			errorMessage =
				err instanceof Error
					? ('detail' in err ? (err as unknown as { detail?: string }).detail : null) ??
						err.message ??
						fallback
					: fallback;
			addToast({ type: 'error', message: errorMessage });
		} finally {
			isDownloading = false;
		}
	}

	const currentYear = new Date().getFullYear();
</script>

<svelte:head>
	<title>{t('export.title')} — {t('app.title')}</title>
</svelte:head>

<div class="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
	<header class="mb-8">
		<h1 class="text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
			{t('export.title')}
		</h1>
		<p class="mt-2 text-neutral-600 dark:text-neutral-400">
			Apply optional filters, then download.
		</p>
	</header>

	<section
		class="space-y-6 rounded-2xl border border-neutral-200 bg-white p-7 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
	>
		<!-- Format -->
		<fieldset>
			<legend class="text-base font-semibold text-neutral-900 dark:text-neutral-50">
				{t('export.formatLabel')}
			</legend>
			<div class="mt-3 grid grid-cols-2 gap-3">
				{#each [{ value: 'Csv' as ExportFormat, label: t('export.csvFormat') }, { value: 'Json' as ExportFormat, label: t('export.jsonFormat') }] as opt (opt.value)}
					<label
						class="flex min-h-11 cursor-pointer items-center gap-3 rounded-xl border bg-neutral-50 px-4 py-3 transition-colors {format ===
						opt.value
							? 'border-primary-500 bg-primary-50 dark:bg-primary-950/30'
							: 'border-transparent hover:bg-neutral-100 dark:bg-neutral-800 dark:hover:bg-neutral-700'}"
					>
						<input
							type="radio"
							name="format"
							value={opt.value}
							bind:group={format}
							class="h-4 w-4 text-primary-600 focus:ring-primary-500"
						/>
						<span class="text-base font-medium text-neutral-900 dark:text-neutral-100">
							{opt.label}
						</span>
					</label>
				{/each}
			</div>
		</fieldset>

		<!-- Filters -->
		<fieldset class="space-y-4">
			<legend class="text-base font-semibold text-neutral-900 dark:text-neutral-50">
				{t('export.filterTitle')}
			</legend>

			<div>
				<label for="export-search" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('common.search')}
				</label>
				<input
					id="export-search"
					type="search"
					bind:value={search}
					placeholder={t('devices.filters.searchPlaceholder')}
					class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
				/>
			</div>

			<div class="grid gap-4 sm:grid-cols-2">
				<div>
					<label for="export-brand" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.brandLabel')}
					</label>
					<select
						id="export-brand"
						bind:value={brandId}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All brands</option>
						{#each refData.brands as b (b.id)}
							<option value={b.id}>{b.name}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-category" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.categoryLabel')}
					</label>
					<select
						id="export-category"
						bind:value={categoryId}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All categories</option>
						{#each refData.categories as c (c.id)}
							<option value={c.id}>{c.name}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-owner" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.ownerLabel')}
					</label>
					<select
						id="export-owner"
						bind:value={ownerId}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All owners</option>
						{#each refData.owners as o (o.id)}
							<option value={o.id}>{o.name}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-location" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.locationLabel')}
					</label>
					<select
						id="export-location"
						bind:value={locationId}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All locations</option>
						{#each refData.locations as l (l.id)}
							<option value={l.id}>{l.name}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-network" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.networkLabel')}
					</label>
					<select
						id="export-network"
						bind:value={networkId}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All networks</option>
						{#each refData.networks as n (n.id)}
							<option value={n.id}>{n.name}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-status" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.statusLabel')}
					</label>
					<select
						id="export-status"
						bind:value={status}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">All statuses</option>
						{#each STATUS_OPTIONS as s (s)}
							<option value={s}>{s}</option>
						{/each}
					</select>
				</div>

				<div>
					<label for="export-year-from" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.yearMinLabel')}
					</label>
					<input
						id="export-year-from"
						type="number"
						min="1990"
						max={currentYear}
						bind:value={purchaseYearFrom}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					/>
				</div>

				<div>
					<label for="export-year-to" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
						{t('devices.filters.yearMaxLabel')}
					</label>
					<input
						id="export-year-to"
						type="number"
						min="1990"
						max={currentYear}
						bind:value={purchaseYearTo}
						class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					/>
				</div>
			</div>
		</fieldset>

		<!-- Actions -->
		<div class="flex flex-col-reverse items-stretch gap-3 border-t border-neutral-200 pt-6 sm:flex-row sm:items-center sm:justify-end dark:border-neutral-800">
			<button
				type="button"
				onclick={clearFilters}
				class="inline-flex min-h-11 items-center justify-center rounded-full px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('devices.filters.clearAll')}
			</button>
			<button
				type="button"
				onclick={handleDownload}
				disabled={isDownloading}
				class="inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-primary-600 px-6 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				{#if isDownloading}
					<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
						<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
						<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
					</svg>
					{t('common.loading')}
				{:else}
					<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
					</svg>
					{t('export.downloadButton')}
				{/if}
			</button>
		</div>

		{#if errorMessage}
			<div
				class="rounded-xl border border-danger-300 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-800 dark:bg-danger-950/40 dark:text-danger-200"
				role="alert"
			>
				{errorMessage}
			</div>
		{/if}
	</section>
</div>

<script lang="ts">
	import { onMount } from 'svelte';
	import api from '$lib/api/client';
	import EraReportDecade from '$lib/components/EraReportDecade.svelte';
	import { t } from '$lib/i18n';
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import { formatDateOnly, normalizeEraReport, type EraReportViewModel } from '$lib/utils/reports';

	const palettes = [
		'from-info-500 via-primary-500 to-primary-300',
		'from-success-500 via-info-400 to-primary-300',
		'from-warning-500 via-warning-400 to-danger-400',
		'from-primary-600 via-info-500 to-success-400',
		'from-danger-500 via-warning-500 to-warning-300'
	] as const;

	let selectedCategoryId = $state('');
	let report = $state<EraReportViewModel>({ decades: [], asOfDate: null, appliedCategoryId: null });
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let latestRequest = 0;

	const categories = $derived($referenceDataStore.categories);
	const maxDeviceCount = $derived(
		report.decades.reduce((largest, item) => Math.max(largest, item.deviceCount), 0)
	);

	onMount(() => {
		if ($referenceDataStore.categories.length === 0 && !$referenceDataStore.isLoading) {
			void fetchReferenceData();
		}
	});

	$effect(() => {
		void selectedCategoryId;
		void loadEraReport(selectedCategoryId || null);
	});

	async function loadEraReport(categoryId: string | null): Promise<void> {
		const requestId = ++latestRequest;
		isLoading = true;
		error = null;

		try {
			const response = await api.reports.eras(categoryId);
			if (requestId !== latestRequest) return;
			report = normalizeEraReport(response);
		} catch (loadError: unknown) {
			console.error('[EraReportCard] Era report load failed:', loadError);
			if (requestId !== latestRequest) return;
			error = t('reports.errors.eras');
		} finally {
			if (requestId === latestRequest) {
				isLoading = false;
			}
		}
	}

	function getDecadeLabel(decade: string): string {
		const key = `reports.eras.decades.${decade}`;
		const translated = t(key);
		return translated === key ? decade : translated;
	}

	function getPalette(index: number): string {
		return palettes[index % palettes.length] ?? palettes[0];
	}

	function getBarWidth(deviceCount: number): string {
		if (maxDeviceCount === 0 || deviceCount === 0) {
			return '0%';
		}

		const relativeWidth = (deviceCount / maxDeviceCount) * 100;
		return `${Math.max(relativeWidth, 12)}%`;
	}
</script>

<section class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
		<div class="space-y-2">
			<h2 class="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('reports.eras.title')}
			</h2>
			<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('reports.eras.description')}</p>
			<p class="text-xs text-neutral-500 dark:text-neutral-400">{t('reports.eras.legend')}</p>
			{#if report.asOfDate && !isLoading && !error}
				<p class="text-xs text-neutral-500 dark:text-neutral-400">
					{t('reports.eras.asOf', { date: formatDateOnly(report.asOfDate) })}
				</p>
			{/if}
		</div>

		<div class="flex flex-col gap-3 sm:min-w-80">
			<div class="flex flex-wrap items-center gap-2" role="group" aria-label={t('reports.eras.filterLabel')}>
				<button
					type="button"
					onclick={() => (selectedCategoryId = '')}
					aria-pressed={selectedCategoryId.length === 0}
					class="inline-flex min-h-11 items-center rounded-full px-4 py-2 text-sm font-medium transition-colors duration-150"
					class:bg-primary-600={selectedCategoryId.length === 0}
					class:text-white={selectedCategoryId.length === 0}
					class:hover:bg-primary-700={selectedCategoryId.length === 0}
					class:bg-neutral-100={selectedCategoryId.length > 0}
					class:text-neutral-700={selectedCategoryId.length > 0}
					class:hover:bg-neutral-200={selectedCategoryId.length > 0}
					class:dark:bg-primary-700={selectedCategoryId.length === 0}
					class:dark:bg-neutral-800={selectedCategoryId.length > 0}
					class:dark:text-neutral-200={selectedCategoryId.length > 0}
				>
					{t('reports.eras.allCategories')}
				</button>

				<div class="min-w-0 flex-1">
					<label for="era-category-filter" class="sr-only">{t('reports.eras.filterLabel')}</label>
					<select
						id="era-category-filter"
						bind:value={selectedCategoryId}
						class="block min-h-11 w-full rounded-full border-0 bg-neutral-100 px-4 py-2.5 text-sm text-neutral-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100"
					>
						<option value="">{t('reports.eras.allCategories')}</option>
						{#each categories as category (category.id)}
							<option value={category.id}>{category.name}</option>
						{/each}
					</select>
				</div>
			</div>
		</div>
	</div>

	{#if isLoading}
		<div class="mt-6 space-y-4" role="status" aria-live="polite">
			<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('common.states.loading')}</p>
			{#each [0, 1, 2] as index (index)}
				<div class="space-y-3 rounded-2xl border border-neutral-200 p-4 dark:border-neutral-800">
					<div class="h-4 w-24 animate-pulse rounded-full bg-neutral-200 dark:bg-neutral-800"></div>
					<div class="h-3 w-full animate-pulse rounded-full bg-neutral-100 dark:bg-neutral-900"></div>
					<div class="h-4 w-40 animate-pulse rounded-full bg-neutral-100 dark:bg-neutral-900"></div>
				</div>
			{/each}
		</div>
	{:else if error}
		<div class="mt-6 rounded-2xl border border-danger-200 bg-danger-50 p-4 dark:border-danger-900 dark:bg-danger-950">
			<p class="text-sm text-danger-800 dark:text-danger-200">{error}</p>
			<button
				type="button"
				onclick={() => void loadEraReport(selectedCategoryId || null)}
				class="mt-4 inline-flex min-h-11 items-center rounded-full bg-danger-600 px-4 py-2 text-sm font-medium text-white hover:bg-danger-700 dark:bg-danger-500 dark:hover:bg-danger-600"
			>
				{t('common.actions.retry')}
			</button>
		</div>
	{:else if report.decades.length === 0}
		<p class="mt-6 text-sm text-neutral-600 dark:text-neutral-300">{t('reports.eras.empty')}</p>
	{:else}
		<div class="mt-6 grid gap-4 md:hidden" role="list" aria-label={t('reports.eras.cardsLabel')}>
			{#each report.decades as decade, index (`${decade.decade}-${decade.startYear}`)}
				<EraReportDecade
					item={decade}
					mode="mobile"
					label={getDecadeLabel(decade.decade)}
					paletteClass={getPalette(index)}
					barWidth={getBarWidth(decade.deviceCount)}
				/>
			{/each}
		</div>

		<div class="mt-6 hidden overflow-hidden rounded-2xl border border-neutral-200 dark:border-neutral-800 md:block">
			<div class="overflow-x-auto">
				<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
					<caption class="sr-only">{t('reports.eras.tableLabel')}</caption>
					<thead class="bg-neutral-50 dark:bg-neutral-900">
						<tr class="text-left text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
							<th scope="col" class="px-4 py-3">{t('reports.eras.columns.decade')}</th>
							<th scope="col" class="px-4 py-3">{t('reports.eras.columns.devices')}</th>
							<th scope="col" class="px-4 py-3">{t('reports.eras.columns.value')}</th>
							<th scope="col" class="px-4 py-3">{t('reports.eras.columns.sampleDevices')}</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
						{#each report.decades as decade, index (`table-${decade.decade}-${decade.startYear}`)}
							<EraReportDecade
								item={decade}
								mode="desktop"
								label={getDecadeLabel(decade.decade)}
								paletteClass={getPalette(index)}
								barWidth={getBarWidth(decade.deviceCount)}
							/>
						{/each}
					</tbody>
				</table>
			</div>
		</div>
	{/if}
</section>

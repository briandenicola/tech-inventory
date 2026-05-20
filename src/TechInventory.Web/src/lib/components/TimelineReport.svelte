<script lang="ts">
	import { onMount } from 'svelte';
	import api from '$lib/api/client';
	import TimelineBar from '$lib/components/TimelineBar.svelte';
	import { t } from '$lib/i18n';
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import {
		formatDateOnly,
		normalizeTimelineReport,
		type TimelineReportViewModel
	} from '$lib/utils/reports';
	import type { TimelineReportGroupBy } from '$lib/api/types';

	const emptyReport: TimelineReportViewModel = {
		entries: [],
		groups: [],
		axisTicks: [],
		minDate: null,
		maxDate: null,
		totalSpanDays: 0,
		asOfDate: null,
		groupBy: 'Category',
		appliedCategoryId: null
	};

	let selectedCategoryId = $state('');
	let selectedGroupBy = $state<TimelineReportGroupBy>('Category');
	let report = $state<TimelineReportViewModel>(emptyReport);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let latestRequest = 0;

	const categories = $derived($referenceDataStore.categories);

	onMount(() => {
		if ($referenceDataStore.categories.length === 0 && !$referenceDataStore.isLoading) {
			void fetchReferenceData();
		}
	});

	$effect(() => {
		void selectedCategoryId;
		void selectedGroupBy;
		void loadTimelineReport();
	});

	async function loadTimelineReport(): Promise<void> {
		const requestId = ++latestRequest;
		isLoading = true;
		error = null;

		try {
			const response = await api.reports.timeline({
				categoryId: selectedCategoryId || null,
				groupBy: selectedGroupBy
			});

			if (requestId !== latestRequest) return;
			report = normalizeTimelineReport(response, new Date(), t('reports.timeline.unknownGroup'));
		} catch (loadError: unknown) {
			console.error('[TimelineReport] Timeline load failed:', loadError);
			if (requestId !== latestRequest) return;
			error = t('reports.timeline.error');
		} finally {
			if (requestId === latestRequest) {
				isLoading = false;
			}
		}
	}
</script>

<section class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
		<div class="space-y-2">
			<h2 class="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">{t('reports.timeline.title')}</h2>
			<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('reports.timeline.description')}</p>
			{#if report.asOfDate && !isLoading && !error}
				<p class="text-xs text-neutral-500 dark:text-neutral-400">{t('reports.timeline.asOf', { date: formatDateOnly(report.asOfDate) })}</p>
			{/if}
		</div>

		<div class="grid gap-3 sm:min-w-80 sm:grid-cols-2">
			<div>
				<label for="timeline-category-filter" class="sr-only">{t('reports.timeline.filterLabel')}</label>
				<select id="timeline-category-filter" bind:value={selectedCategoryId} class="block min-h-11 w-full rounded-full border-0 bg-neutral-100 px-4 py-2.5 text-sm text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100">
					<option value="">{t('reports.timeline.allCategories')}</option>
					{#each categories as category (category.id)}
						<option value={category.id}>{category.name}</option>
					{/each}
				</select>
			</div>

			<div class="inline-flex min-h-11 items-center rounded-full bg-neutral-100 p-1 dark:bg-neutral-800" role="group" aria-label={t('reports.timeline.groupByLabel')}>
				<button type="button" onclick={() => (selectedGroupBy = 'Category')} aria-pressed={selectedGroupBy === 'Category'} class="flex-1 rounded-full px-4 py-2 text-sm font-medium transition-colors duration-150" class:bg-white={selectedGroupBy === 'Category'} class:text-neutral-900={selectedGroupBy === 'Category'} class:shadow-sm={selectedGroupBy === 'Category'} class:text-neutral-600={selectedGroupBy !== 'Category'} class:dark:bg-neutral-700={selectedGroupBy === 'Category'} class:dark:text-neutral-50={selectedGroupBy === 'Category'} class:dark:text-neutral-200={selectedGroupBy !== 'Category'}>{t('reports.timeline.groupBy.category')}</button>
				<button type="button" onclick={() => (selectedGroupBy = 'Owner')} aria-pressed={selectedGroupBy === 'Owner'} class="flex-1 rounded-full px-4 py-2 text-sm font-medium transition-colors duration-150" class:bg-white={selectedGroupBy === 'Owner'} class:text-neutral-900={selectedGroupBy === 'Owner'} class:shadow-sm={selectedGroupBy === 'Owner'} class:text-neutral-600={selectedGroupBy !== 'Owner'} class:dark:bg-neutral-700={selectedGroupBy === 'Owner'} class:dark:text-neutral-50={selectedGroupBy === 'Owner'} class:dark:text-neutral-200={selectedGroupBy !== 'Owner'}>{t('reports.timeline.groupBy.owner')}</button>
			</div>
		</div>
	</div>

	<div class="mt-4 flex flex-wrap items-center gap-4 text-xs text-neutral-500 dark:text-neutral-400">
		<span class="font-medium text-neutral-700 dark:text-neutral-200">{t('reports.timeline.legend')}</span>
		<span class="inline-flex items-center gap-2"><span class="h-3 w-3 rounded-full bg-gradient-to-r from-primary-600 via-info-500 to-success-400"></span>{t('reports.timeline.active')}</span>
		<span class="inline-flex items-center gap-2"><span class="h-3 w-3 rounded-full bg-gradient-to-r from-neutral-400 to-neutral-500"></span>{t('reports.timeline.disposed')}</span>
	</div>

	{#if isLoading}
		<div class="mt-6 space-y-4" role="status" aria-live="polite">
			<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('reports.timeline.loading')}</p>
			{#each [0, 1] as index (index)}
				<div class="rounded-2xl border border-neutral-200 p-4 dark:border-neutral-800">
					<div class="h-4 w-32 animate-pulse rounded-full bg-neutral-200 dark:bg-neutral-800"></div>
					<div class="mt-4 h-3 w-full animate-pulse rounded-full bg-neutral-100 dark:bg-neutral-900"></div>
					<div class="mt-3 h-4 w-48 animate-pulse rounded-full bg-neutral-100 dark:bg-neutral-900"></div>
				</div>
			{/each}
		</div>
	{:else if error}
		<div class="mt-6 rounded-2xl border border-danger-200 bg-danger-50 p-4 dark:border-danger-900 dark:bg-danger-950">
			<p class="text-sm text-danger-800 dark:text-danger-200">{error}</p>
			<button type="button" onclick={() => void loadTimelineReport()} class="mt-4 inline-flex min-h-11 items-center rounded-full bg-danger-600 px-4 py-2 text-sm font-medium text-white hover:bg-danger-700 dark:bg-danger-500 dark:hover:bg-danger-600">{t('common.actions.retry')}</button>
		</div>
	{:else if report.entries.length === 0}
		<p class="mt-6 text-sm text-neutral-600 dark:text-neutral-300">{t('reports.timeline.empty')}</p>
	{:else}
		<div class="mt-6 hidden md:block">
			<div class="overflow-x-auto">
				<div class="w-max min-w-full" aria-label={t('reports.timeline.axisLabel')}>
					<div class="flex items-center justify-between gap-8 text-xs text-neutral-500 dark:text-neutral-400">
						{#each report.axisTicks as tick (tick.year)}<span>{tick.label}</span>{/each}
					</div>
					<div class="mt-2 h-px bg-neutral-200 dark:bg-neutral-800"></div>
				</div>
			</div>
		</div>

		<ul class="mt-6 space-y-4" aria-label={t('reports.timeline.groupsLabel')}>
			{#each report.groups as group (group.label)}
				<li class="rounded-2xl border border-neutral-200 p-4 dark:border-neutral-800">
					<h3 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">{group.label}</h3>
					<p class="mt-1 text-xs text-neutral-500 dark:text-neutral-400">{t('reports.timeline.deviceCount', { count: group.entries.length })}</p>
					<div class="mt-4 space-y-4">
						{#each group.entries as entry (`${group.label}-${entry.deviceName}-${entry.purchaseDate}`)}
							<TimelineBar {entry} />
						{/each}
					</div>
				</li>
			{/each}
		</ul>

		<div class="mt-6 hidden md:block">
			<div class="overflow-x-auto">
				<div class="w-max min-w-full">
					<div class="h-px bg-neutral-200 dark:bg-neutral-800"></div>
					<div class="mt-2 flex items-center justify-between gap-8 text-xs text-neutral-500 dark:text-neutral-400">
						{#each report.axisTicks as tick (tick.year)}<span>{tick.label}</span>{/each}
					</div>
				</div>
			</div>
		</div>
	{/if}
</section>

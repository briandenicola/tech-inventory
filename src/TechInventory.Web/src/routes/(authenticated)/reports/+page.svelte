<script lang="ts">
	import { page } from '$app/stores';
	import api from '$lib/api/client';
	import EraReportCard from '$lib/components/EraReportCard.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ReportBreakdownCard from '$lib/components/ReportBreakdownCard.svelte';
	import ReportMetricCard from '$lib/components/ReportMetricCard.svelte';
	import TimelineReport from '$lib/components/TimelineReport.svelte';
	import WarrantyExpiryPanel from '$lib/components/WarrantyExpiryPanel.svelte';
	import { t } from '$lib/i18n';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import {
		formatReportCurrency,
		normalizeSummaryReport,
		normalizeWarrantyReport,
		sortWarrantyItems,
		type SummaryReportViewModel,
		type WarrantyReportViewModel,
		type WarrantySortDirection
	} from '$lib/utils/reports';

	type WarrantyWindow = 30 | 60 | 90;

	let summary = $state<SummaryReportViewModel | null>(null);
	let warranties = $state<WarrantyReportViewModel>({ totalCount: 0, items: [] });
	let summaryLoading = $state(true);
	let warrantyLoading = $state(true);
	let summaryError = $state<string | null>(null);
	let warrantyError = $state<string | null>(null);
	let selectedWarrantyWindow = $state<WarrantyWindow>(90);
	let warrantySortDirection = $state<WarrantySortDirection>('asc');

	const sortedWarrantyItems = $derived(sortWarrantyItems(warranties.items, warrantySortDirection));

	$effect(() => {
		void loadSummary();
	});

	$effect(() => {
		void selectedWarrantyWindow;
		void loadWarranties();
	});

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, refreshReports);
		return unregister;
	});

	async function loadSummary(): Promise<void> {
		summaryLoading = true;
		summaryError = null;

		try {
			summary = normalizeSummaryReport(await api.reports.summary(), t('reports.summary.unknownLabel'));
		} catch (error: unknown) {
			console.error('[ReportsPage] Summary load failed:', error);
			summaryError = error instanceof Error ? error.message : t('reports.errors.summary');
		} finally {
			summaryLoading = false;
		}
	}

	async function loadWarranties(): Promise<void> {
		warrantyLoading = true;
		warrantyError = null;

		try {
			warranties = normalizeWarrantyReport(await api.reports.warranties(selectedWarrantyWindow));
		} catch (error: unknown) {
			console.error('[ReportsPage] Warranty load failed:', error);
			warrantyError = error instanceof Error ? error.message : t('reports.errors.warranties');
		} finally {
			warrantyLoading = false;
		}
	}

	async function refreshReports(): Promise<void> {
		await Promise.all([loadSummary(), loadWarranties()]);
	}

	function handleWarrantyWindowChange(days: number): void {
		if (days === 30 || days === 60 || days === 90) {
			selectedWarrantyWindow = days;
		}
	}

	function toggleWarrantySort(): void {
		warrantySortDirection = warrantySortDirection === 'asc' ? 'desc' : 'asc';
	}
</script>

<svelte:head>
	<title>{t('reports.title')} — {t('app.title')}</title>
</svelte:head>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-8 space-y-2">
		<h1 class="text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">{t('reports.title')}</h1>
		<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('reports.subtitle')}</p>
	</div>

	{#if summaryLoading}
		<div class="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-4">
			<div class="col-span-1 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"><LoadingSkeleton rows={2} /></div>
			<div class="col-span-1 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"><LoadingSkeleton rows={2} /></div>
			<div class="col-span-2 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"><LoadingSkeleton rows={3} /></div>
			<div class="col-span-2 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"><LoadingSkeleton rows={3} /></div>
			<div class="col-span-2 rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950 xl:col-span-2"><LoadingSkeleton rows={3} /></div>
		</div>
	{:else if summaryError}
		<ErrorState error={summaryError} onRetry={loadSummary} />
	{:else if summary}
		<div class="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-4">
			<div class="col-span-1">
				<ReportMetricCard title={t('reports.summary.totalDevices')} value={summary.totalDevices.toString()} />
			</div>
			<div class="col-span-1">
				<ReportMetricCard title={t('reports.summary.totalValue')} value={formatReportCurrency(summary.totalValue)} />
			</div>
			<div class="col-span-2 lg:col-span-1">
				<ReportBreakdownCard title={t('reports.summary.byCategory')} items={summary.byCategory} emptyText={t('reports.summary.empty')} />
			</div>
			<div class="col-span-2 lg:col-span-1">
				<ReportBreakdownCard title={t('reports.summary.byLocation')} items={summary.byLocation} emptyText={t('reports.summary.empty')} />
			</div>
			<div class="col-span-2 lg:col-span-1 xl:col-span-2">
				<ReportBreakdownCard title={t('reports.summary.byStatus')} items={summary.byStatus.map((item) => ({ label: item.status, count: item.count }))} emptyText={t('reports.summary.empty')} />
			</div>
		</div>
	{/if}

	<div class="mt-8">
		<EraReportCard />
	</div>

	<div class="mt-8">
		<TimelineReport />
	</div>

	<div class="mt-8">
		<WarrantyExpiryPanel
			items={sortedWarrantyItems}
			totalCount={warranties.totalCount}
			selectedWindow={selectedWarrantyWindow}
			isLoading={warrantyLoading}
			error={warrantyError}
			sortDirection={warrantySortDirection}
			onWindowChange={handleWarrantyWindowChange}
			onRetry={loadWarranties}
			onSortToggle={toggleWarrantySort}
		/>
	</div>
</div>

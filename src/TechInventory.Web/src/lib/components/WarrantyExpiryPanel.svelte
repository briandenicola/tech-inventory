<script lang="ts">
	import { t } from '$lib/i18n';
	import type {
		WarrantyReportItemViewModel,
		WarrantySortDirection,
		WarrantyStatusTone
	} from '$lib/utils/reports';
	import { formatDateOnly } from '$lib/utils/reports';

	interface Props {
		items: WarrantyReportItemViewModel[];
		totalCount: number;
		selectedWindow: number;
		isLoading: boolean;
		error: string | null;
		sortDirection: WarrantySortDirection;
		onWindowChange: (days: number) => void;
		onRetry: () => void;
		onSortToggle: () => void;
	}

	let {
		items,
		totalCount,
		selectedWindow,
		isLoading,
		error,
		sortDirection,
		onWindowChange,
		onRetry,
		onSortToggle
	}: Props = $props();

	const filterOptions = [30, 60, 90] as const;

	function getBadgeClasses(tone: WarrantyStatusTone): string {
		if (tone === 'urgent') {
			return 'bg-danger-100 text-danger-700 dark:bg-danger-950 dark:text-danger-200';
		}

		if (tone === 'warning') {
			return 'bg-warning-100 text-warning-700 dark:bg-warning-950 dark:text-warning-200';
		}

		return 'bg-primary-100 text-primary-700 dark:bg-primary-950 dark:text-primary-200';
	}

	function getBadgeLabel(tone: WarrantyStatusTone): string {
		return t(`reports.warranties.priority.${tone}`);
	}
</script>

<section class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
		<div>
			<h2 class="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('reports.warranties.title')}
			</h2>
			<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-300">
				{t('reports.warranties.subtitle')}
			</p>
		</div>

		<div class="flex flex-wrap items-center gap-2" aria-label={t('reports.warranties.filterLabel')}>
			{#each filterOptions as option (option)}
				<button
					type="button"
					onclick={() => onWindowChange(option)}
					class="inline-flex min-h-11 items-center rounded-full px-4 py-2 text-sm font-medium transition-colors duration-150"
					class:bg-primary-600={selectedWindow === option}
					class:text-white={selectedWindow === option}
					class:hover:bg-primary-700={selectedWindow === option}
					class:bg-neutral-100={selectedWindow !== option}
					class:text-neutral-700={selectedWindow !== option}
					class:hover:bg-neutral-200={selectedWindow !== option}
					class:dark:bg-primary-700={selectedWindow === option}
					class:dark:hover:bg-primary-800={selectedWindow === option}
					class:dark:bg-neutral-800={selectedWindow !== option}
					class:dark:text-neutral-200={selectedWindow !== option}
					class:dark:hover:bg-neutral-700={selectedWindow !== option}
				>
					{t('reports.warranties.filterDays', { days: option })}
				</button>
			{/each}
		</div>
	</div>

	{#if isLoading}
		<div class="mt-6 space-y-3">
			{#each [0, 1, 2, 3, 4] as index (index)}
				<div class="h-12 animate-pulse rounded-xl bg-neutral-100 dark:bg-neutral-900"></div>
			{/each}
		</div>
	{:else if error}
		<div class="mt-6 rounded-2xl border border-danger-200 bg-danger-50 p-4 dark:border-danger-900 dark:bg-danger-950">
			<p class="text-sm text-danger-800 dark:text-danger-200">{error}</p>
			<button
				type="button"
				onclick={onRetry}
				class="mt-4 inline-flex min-h-11 items-center rounded-full bg-danger-600 px-4 py-2 text-sm font-medium text-white hover:bg-danger-700 dark:bg-danger-500 dark:hover:bg-danger-600"
			>
				{t('common.actions.retry')}
			</button>
		</div>
	{:else if items.length === 0}
		<p class="mt-6 text-sm text-neutral-600 dark:text-neutral-300">
			{t('reports.warranties.empty', { days: selectedWindow })}
		</p>
	{:else}
		<div class="mt-6 flex items-center justify-between gap-3 text-sm text-neutral-500 dark:text-neutral-400">
			<span>{totalCount}</span>
			<button
				type="button"
				onclick={onSortToggle}
				class="inline-flex min-h-11 items-center gap-2 rounded-full px-4 py-2 font-medium text-primary-700 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-950"
			>
				{t('reports.warranties.sortExpiry')}
				<span>{sortDirection === 'asc' ? t('reports.warranties.sortAsc') : t('reports.warranties.sortDesc')}</span>
			</button>
		</div>

		<div class="mt-4 overflow-x-auto">
			<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
				<thead>
					<tr class="text-left text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
						<th scope="col" class="px-3 py-3">{t('reports.warranties.columns.device')}</th>
						<th scope="col" class="px-3 py-3">{t('reports.warranties.columns.brand')}</th>
						<th scope="col" class="px-3 py-3">{t('reports.warranties.columns.expiry')}</th>
						<th scope="col" class="px-3 py-3">{t('reports.warranties.columns.days')}</th>
						<th scope="col" class="px-3 py-3">{t('reports.warranties.columns.status')}</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
					{#each items as item (`${item.deviceId ?? item.deviceName}-${item.warrantyExpiry}`)}
						<tr class="text-sm text-neutral-700 dark:text-neutral-300">
							<td class="px-3 py-3 font-medium text-neutral-900 dark:text-neutral-50">
								{#if item.deviceId}
									<a href={`/devices/${item.deviceId}`} class="text-primary-700 hover:underline dark:text-primary-300">
										{item.deviceName}
									</a>
								{:else}
									<span title={t('reports.warranties.missingDeviceLink')}>{item.deviceName}</span>
								{/if}
							</td>
							<td class="px-3 py-3">{item.brandName ?? '—'}</td>
							<td class="px-3 py-3">
								<time datetime={item.warrantyExpiry}>{formatDateOnly(item.warrantyExpiry)}</time>
							</td>
							<td class="px-3 py-3">{item.daysUntilExpiry}</td>
							<td class="px-3 py-3">
								<span class={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${getBadgeClasses(item.tone)}`}>
									{getBadgeLabel(item.tone)}
								</span>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</section>

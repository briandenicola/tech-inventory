<script lang="ts">
	import { t } from '$lib/i18n';
	import {
		formatReportCurrency,
		type EraReportDecadeViewModel
	} from '$lib/utils/reports';

	interface Props {
		item: EraReportDecadeViewModel;
		mode: 'mobile' | 'desktop';
		label: string;
		paletteClass: string;
		barWidth: string;
	}

	let { item, mode, label, paletteClass, barWidth }: Props = $props();
</script>

{#if mode === 'mobile'}
	<div role="listitem" class="rounded-2xl border border-neutral-200 p-4 dark:border-neutral-800">
		<div class="flex items-start justify-between gap-3">
			<div>
				<h3 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">{label}</h3>
				<p class="mt-1 text-sm text-neutral-500 dark:text-neutral-400">
					{t('reports.eras.deviceCount', { count: item.deviceCount })}
				</p>
			</div>
			<p class="text-sm font-medium text-neutral-700 dark:text-neutral-200">{formatReportCurrency(item.totalValue)}</p>
		</div>
		<div class="mt-4 h-3 overflow-hidden rounded-full bg-neutral-100 dark:bg-neutral-900">
			<div class={`h-full rounded-full bg-gradient-to-r ${paletteClass}`} style={`width: ${barWidth}`}></div>
		</div>
		<p class="mt-3 text-xs text-neutral-500 dark:text-neutral-400">
			{t('reports.eras.years', { start: item.startYear, end: item.endYear })}
		</p>
		<div class="mt-4 flex flex-wrap gap-2" aria-label={t('reports.eras.sampleDevices')}>
			{#if item.sampleDevices.length > 0}
				{#each item.sampleDevices as sampleDevice (sampleDevice)}
					<span class="rounded-full bg-neutral-100 px-3 py-1 text-xs text-neutral-700 dark:bg-neutral-900 dark:text-neutral-200">{sampleDevice}</span>
				{/each}
			{:else}
				<span class="text-xs text-neutral-500 dark:text-neutral-400">{t('reports.eras.noSamples')}</span>
			{/if}
		</div>
	</div>
{:else}
	<tr class="align-top text-sm text-neutral-700 dark:text-neutral-300">
		<th scope="row" class="px-4 py-4 font-semibold text-neutral-900 dark:text-neutral-50">
			{label}
			<p class="mt-1 text-xs font-normal text-neutral-500 dark:text-neutral-400">
				{t('reports.eras.years', { start: item.startYear, end: item.endYear })}
			</p>
		</th>
		<td class="px-4 py-4">
			<div class="flex min-w-64 items-center gap-3">
				<div class="h-3 flex-1 overflow-hidden rounded-full bg-neutral-100 dark:bg-neutral-900">
					<div class={`h-full rounded-full bg-gradient-to-r ${paletteClass}`} style={`width: ${barWidth}`}></div>
				</div>
				<span class="font-medium text-neutral-900 dark:text-neutral-50">{item.deviceCount}</span>
			</div>
		</td>
		<td class="px-4 py-4 font-medium text-neutral-900 dark:text-neutral-50">{formatReportCurrency(item.totalValue)}</td>
		<td class="px-4 py-4">
			<div class="flex flex-wrap gap-2">
				{#if item.sampleDevices.length > 0}
					{#each item.sampleDevices as sampleDevice (sampleDevice)}
						<span class="rounded-full bg-neutral-100 px-3 py-1 text-xs text-neutral-700 dark:bg-neutral-900 dark:text-neutral-200">{sampleDevice}</span>
					{/each}
				{:else}
					<span class="text-xs text-neutral-500 dark:text-neutral-400">{t('reports.eras.noSamples')}</span>
				{/if}
			</div>
		</td>
	</tr>
{/if}

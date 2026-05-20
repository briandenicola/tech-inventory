<script lang="ts">
	import { t } from '$lib/i18n';
	import { formatDateOnly, type TimelineEntryViewModel } from '$lib/utils/reports';

	interface Props {
		entry: TimelineEntryViewModel;
	}

	let { entry }: Props = $props();

	const durationLabel = $derived(
		t('reports.timeline.duration', {
			years: entry.durationYears < 10 ? entry.durationYears.toFixed(1) : Math.round(entry.durationYears)
		})
	);
	const barToneClass = $derived(
		entry.isActive
			? 'bg-gradient-to-r from-primary-600 via-info-500 to-success-400'
			: 'bg-gradient-to-r from-neutral-400 to-neutral-500'
	);
</script>

<div class="space-y-3 md:flex md:items-center md:gap-4 md:space-y-0">
	<div class="min-w-0 md:w-56 md:shrink-0">
		<p class="truncate text-sm font-semibold text-neutral-900 dark:text-neutral-50">{entry.deviceName}</p>
		<p class="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
			{#if entry.brand}{entry.brand} · {/if}{durationLabel} ·
			{entry.isActive ? t('reports.timeline.active') : t('reports.timeline.disposed')}
		</p>
	</div>

	<div class="min-w-0 flex-1 space-y-2">
		<div class="relative h-4 overflow-hidden rounded-full bg-neutral-100 dark:bg-neutral-900">
			<div
				class={`absolute inset-y-0 rounded-full ${barToneClass}`}
				data-state={entry.isActive ? 'active' : 'disposed'}
				style={`left: ${entry.startPercent}%; width: ${Math.max(entry.widthPercent, 1.5)}%;`}
			>
				<span class="sr-only">
					{entry.deviceName}: {formatDateOnly(entry.purchaseDate)} –
					{entry.isActive ? t('reports.timeline.active') : formatDateOnly(entry.disposalDate)}
				</span>
			</div>
		</div>

		<div class="flex items-center justify-between gap-3 text-xs text-neutral-500 dark:text-neutral-400">
			<span>{formatDateOnly(entry.purchaseDate)}</span>
			<span>{entry.isActive ? t('reports.timeline.active') : formatDateOnly(entry.disposalDate)}</span>
		</div>
	</div>
</div>

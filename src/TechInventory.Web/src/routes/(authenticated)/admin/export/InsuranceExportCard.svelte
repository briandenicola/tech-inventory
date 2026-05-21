<script lang="ts">
	import api from '$lib/api/client';
	import { t } from '$lib/i18n';
	import { triggerBlobDownload } from '$lib/utils/blobDownload';

	type LocationOption = {
		id: string;
		name: string;
	};

	let {
		locations = [],
		locationsLoading = false
	}: {
		locations?: LocationOption[];
		locationsLoading?: boolean;
	} = $props();

	let selectedLocationId = $state('');
	let isDownloading = $state(false);
	let status: 'idle' | 'success' | 'error' = $state('idle');
	let statusMessage = $state(t('export.insurance.idle'));

	function resetStatus(): void {
		status = 'idle';
		statusMessage = t('export.insurance.idle');
	}

	function getStatusClasses(): string {
		if (status === 'error') {
			return 'border-danger-200 bg-danger-50 text-danger-800 dark:border-danger-900 dark:bg-danger-950 dark:text-danger-200';
		}

		if (status === 'success') {
			return 'border-success-200 bg-success-50 text-success-800 dark:border-success-900 dark:bg-success-950 dark:text-success-200';
		}

		return 'border-neutral-200 bg-neutral-50 text-neutral-700 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-200';
	}

	async function handleDownload(): Promise<void> {
		if (isDownloading) {
			return;
		}

		isDownloading = true;
		status = 'idle';
		statusMessage = t('export.insurance.downloading');

		try {
			const download = await api.reports.insurance(
				selectedLocationId ? { LocationId: selectedLocationId } : undefined
			);
			const fileName = download.fileName ?? t('export.insurance.defaultFilename');
			triggerBlobDownload(download.blob, fileName);
			status = 'success';
			statusMessage = t('export.insurance.success', { fileName });
		} catch (error: unknown) {
			console.error('[InsuranceExportCard] Insurance export failed:', error);
			status = 'error';
			statusMessage = error instanceof Error && error.message ? error.message : t('export.insurance.error');
		} finally {
			isDownloading = false;
		}
	}
</script>

<section class="space-y-6 rounded-2xl border border-neutral-200 bg-white p-7 shadow-sm dark:border-neutral-800 dark:bg-neutral-900">
	<div class="space-y-2">
		<h2 class="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
			{t('export.insurance.title')}
		</h2>
		<p class="text-sm text-neutral-600 dark:text-neutral-300">{t('export.insurance.description')}</p>
		<p class="text-xs text-neutral-500 dark:text-neutral-400">{t('export.insurance.note')}</p>
	</div>

	<div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto]">
		<div>
			<label for="insurance-location-filter" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
				{t('export.insurance.filterLabel')}
			</label>
			<select
				id="insurance-location-filter"
				bind:value={selectedLocationId}
				disabled={locationsLoading || isDownloading}
				onchange={resetStatus}
				class="mt-1 block min-h-11 w-full rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-neutral-800 dark:text-neutral-100"
			>
				<option value="">{t('export.insurance.allLocations')}</option>
				{#if locationsLoading}
					<option value="" disabled>{t('export.insurance.loadingLocations')}</option>
				{/if}
				{#each locations as location (location.id)}
					<option value={location.id}>{location.name}</option>
				{/each}
			</select>
		</div>

		<div class="flex items-end">
			<button
				type="button"
				onclick={() => void handleDownload()}
				disabled={isDownloading}
				class="inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-primary-600 px-6 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				{#if isDownloading}
					<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
						<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
						<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
					</svg>
					{t('export.insurance.downloadingButton')}
				{:else}
					{t('export.insurance.downloadButton')}
				{/if}
			</button>
		</div>
	</div>

	<div class={`rounded-xl border p-4 text-sm ${getStatusClasses()}`} role="status" aria-live="polite">
		<p>{statusMessage}</p>
	</div>
</section>

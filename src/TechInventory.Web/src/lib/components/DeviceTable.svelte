<!--
	DeviceTable.svelte — Devices list table with sortable columns (T15 + T17)
	
	Per T15: Paginated table with columns: Name, Brand, Category, Owner, Status, Purchase Date.
	Per T17: Sortable columns (Name, Purchase Date, Created At) with aria-sort + URL-backed.
	
	Mobile (360px+): Stacks as cards. Desktop: Full table.
	Semantic <table> for accessibility: <thead>, <tbody>, <th scope="col">, <caption>.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import type { DeviceResponse } from '$lib/queries/devices';

	interface Props {
		devices: DeviceResponse[];
		currentSort?: 'name' | 'purchaseDate' | 'createdAt';
		sortDir?: 'asc' | 'desc';
		onSort: (column: 'name' | 'purchaseDate' | 'createdAt') => void;
	}

	let { devices, currentSort, sortDir, onSort }: Props = $props();

	// Toggle sort: current asc → desc → asc (2-state cycle per D-052 choice)
	function handleSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		onSort(column);
	}

	// Aria-sort helper
	function getAriaSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		if (currentSort !== column) return 'none';
		return sortDir === 'asc' ? 'ascending' : 'descending';
	}

	// Format date helper (nullable dates)
	function formatDate(dateStr: string | null | undefined): string {
		if (!dateStr) return '—';
		try {
			const date = new Date(dateStr);
			return new Intl.DateTimeFormat('en-US', {
				year: 'numeric',
				month: 'short',
				day: 'numeric'
			}).format(date);
		} catch {
			return dateStr;
		}
	}
</script>

<!-- Desktop table (hidden on mobile) -->
<div class="hidden md:block overflow-x-auto">
	<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
		<caption class="sr-only">{t('devices.list.title')}</caption>
		<thead class="bg-neutral-50 dark:bg-neutral-900">
			<tr>
				<!-- Name (sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
					aria-sort={getAriaSort('name')}
				>
					<button
						type="button"
						onclick={() => handleSort('name')}
						class="group inline-flex items-center gap-1 hover:text-neutral-900 dark:hover:text-neutral-100"
					>
						{t('devices.columns.name')}
						{#if currentSort === 'name'}
							<svg
								class="h-4 w-4"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								aria-hidden="true"
							>
								{#if sortDir === 'asc'}
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M5 15l7-7 7 7"
									/>
								{:else}
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 9l-7 7-7-7"
									/>
								{/if}
							</svg>
						{:else}
							<svg
								class="h-4 w-4 opacity-0 group-hover:opacity-50"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								aria-hidden="true"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
								/>
							</svg>
						{/if}
					</button>
				</th>

				<!-- Brand (not sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
				>
					{t('devices.columns.brand')}
				</th>

				<!-- Category (not sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
				>
					{t('devices.columns.category')}
				</th>

				<!-- Owner (not sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
				>
					{t('devices.columns.owner')}
				</th>

				<!-- Status (not sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
				>
					{t('devices.columns.status')}
				</th>

				<!-- Purchase Date (sortable) -->
				<th
					scope="col"
					class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
					aria-sort={getAriaSort('purchaseDate')}
				>
					<button
						type="button"
						onclick={() => handleSort('purchaseDate')}
						class="group inline-flex items-center gap-1 hover:text-neutral-900 dark:hover:text-neutral-100"
					>
						{t('devices.columns.purchaseDate')}
						{#if currentSort === 'purchaseDate'}
							<svg
								class="h-4 w-4"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								aria-hidden="true"
							>
								{#if sortDir === 'asc'}
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M5 15l7-7 7 7"
									/>
								{:else}
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 9l-7 7-7-7"
									/>
								{/if}
							</svg>
						{:else}
							<svg
								class="h-4 w-4 opacity-0 group-hover:opacity-50"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								aria-hidden="true"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
								/>
							</svg>
						{/if}
					</button>
				</th>
			</tr>
		</thead>
		<tbody class="divide-y divide-neutral-200 bg-white dark:divide-neutral-800 dark:bg-neutral-950">
			{#each devices as device (device.id)}
				<tr
					class="cursor-pointer transition-colors hover:bg-neutral-50 dark:hover:bg-neutral-900"
					onclick={() => (window.location.href = `/devices/${device.id}`)}
				>
					<td class="px-4 py-4 text-sm font-medium text-neutral-900 dark:text-neutral-50">
						{device.name || '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{device.brandId ? 'Brand' : '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{device.categoryId ? 'Category' : '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{device.ownerId ? 'Owner' : '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{device.status || '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{formatDate(device.purchaseDate)}
					</td>
				</tr>
			{/each}
		</tbody>
	</table>
</div>

<!-- Mobile card layout -->
<div class="md:hidden space-y-4">
	{#each devices as device (device.id)}
		<a
			href="/devices/{device.id}"
			class="block rounded-lg border border-neutral-200 bg-white p-4 transition-colors hover:bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-950 dark:hover:bg-neutral-900"
		>
			<div class="flex items-start justify-between">
				<div class="flex-1">
					<h3 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">
						{device.name || '—'}
					</h3>
					<dl class="mt-2 space-y-1 text-sm text-neutral-700 dark:text-neutral-300">
						<div class="flex gap-2">
							<dt class="font-medium">{t('devices.columns.brand')}:</dt>
							<dd>{device.brandId ? 'Brand' : '—'}</dd>
						</div>
						<div class="flex gap-2">
							<dt class="font-medium">{t('devices.columns.category')}:</dt>
							<dd>{device.categoryId ? 'Category' : '—'}</dd>
						</div>
						<div class="flex gap-2">
							<dt class="font-medium">{t('devices.columns.owner')}:</dt>
							<dd>{device.ownerId ? 'Owner' : '—'}</dd>
						</div>
						<div class="flex gap-2">
							<dt class="font-medium">{t('devices.columns.status')}:</dt>
							<dd>{device.status || '—'}</dd>
						</div>
						<div class="flex gap-2">
							<dt class="font-medium">{t('devices.columns.purchaseDate')}:</dt>
							<dd>{formatDate(device.purchaseDate)}</dd>
						</div>
					</dl>
				</div>
				<svg
					class="h-5 w-5 flex-shrink-0 text-neutral-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M9 5l7 7-7 7"
					/>
				</svg>
			</div>
		</a>
	{/each}
</div>

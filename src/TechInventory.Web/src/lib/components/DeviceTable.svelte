<!--
	DeviceTable.svelte — Devices list table with sortable columns (T15 + T17)
	and optional client-side grouping (F023).

	Per T15: Paginated table with columns: Name, Brand, Category, Owner, Status, Purchase Date.
	Per T17: Sortable columns (Name, Purchase Date, Created At) with aria-sort + URL-backed.
	Per F023: When `groups` is provided, rows are partitioned under sticky/collapsible group
	headers; current sort still applies inside each group.

	Mobile (360px+): Stacks as cards. Desktop: Full table.
	Semantic <table> for accessibility: <thead>, <tbody>, <th scope="col">, <caption>.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceGroup } from '$lib/utils/groupDevices';

	interface Props {
		devices: DeviceResponse[];
		/** F023: when set, ignored `devices` is partitioned into these groups for rendering. */
		groups?: DeviceGroup[];
		currentSort?: 'name' | 'purchaseDate' | 'createdAt';
		sortDir?: 'asc' | 'desc';
		onSort: (column: 'name' | 'purchaseDate' | 'createdAt') => void;
		/** F024: when true, renders selection checkboxes (column on desktop, leading on mobile cards). */
		selectable?: boolean;
		selectedIds?: Set<string>;
		onToggleSelect?: (id: string) => void;
		onToggleSelectAll?: () => void;
		allVisibleSelected?: boolean;
		someVisibleSelected?: boolean;
	}

	let {
		devices,
		groups,
		currentSort,
		sortDir,
		onSort,
		selectable = false,
		selectedIds,
		onToggleSelect,
		onToggleSelectAll,
		allVisibleSelected = false,
		someVisibleSelected = false
	}: Props = $props();

	const refData = $derived($referenceDataStore);
	const isGrouped = $derived(Array.isArray(groups) && groups.length > 0);
	const groupColspan = $derived(selectable ? 7 : 6);

	function isSelected(id: string): boolean {
		return selectedIds?.has(id) ?? false;
	}
	function handleSelectToggle(event: Event, id: string) {
		event.stopPropagation();
		onToggleSelect?.(id);
	}
	function setIndeterminate(node: HTMLInputElement, value: boolean) {
		node.indeterminate = value;
		return {
			update(next: boolean) {
				node.indeterminate = next;
			}
		};
	}

	// Ephemeral collapse state — not URL-persisted per spec.
	let collapsedKeys = $state(new Set<string>());
	function toggleGroup(key: string) {
		const next = new Set(collapsedKeys);
		if (next.has(key)) next.delete(key);
		else next.add(key);
		collapsedKeys = next;
	}
	function isCollapsed(key: string): boolean {
		return collapsedKeys.has(key);
	}

	function lookupName(
		collection: { id: string; name: string }[],
		id: string | null | undefined
	): string {
		if (!id) return '—';
		return collection.find((entity) => entity.id === id)?.name ?? '—';
	}

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
				{#if selectable}
					<th
						scope="col"
						class="w-12 px-4 py-3 text-left"
					>
						<input
							type="checkbox"
							class="h-4 w-4 cursor-pointer rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
							checked={allVisibleSelected}
							use:setIndeterminate={!allVisibleSelected && someVisibleSelected}
							onchange={() => onToggleSelectAll?.()}
							aria-label={t('devices.bulk.selectAllVisible')}
						/>
					</th>
				{/if}
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
			{#snippet desktopRow(device: DeviceResponse)}
				<tr
					class="cursor-pointer transition-colors hover:bg-neutral-50 dark:hover:bg-neutral-900"
					class:bg-primary-50={selectable && isSelected(device.id)}
					class:dark:bg-primary-950={selectable && isSelected(device.id)}
					onclick={() => (window.location.href = `/devices/${device.id}`)}
				>
					{#if selectable}
						<td class="w-12 px-4 py-4" onclick={(e) => e.stopPropagation()}>
							<input
								type="checkbox"
								class="h-4 w-4 cursor-pointer rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
								checked={isSelected(device.id)}
								onchange={(e) => handleSelectToggle(e, device.id)}
								aria-label={t('devices.bulk.selectRow', { name: device.name || device.id })}
							/>
						</td>
					{/if}
					<td class="px-4 py-4 text-sm font-medium text-neutral-900 dark:text-neutral-50">
						{device.name || '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{lookupName(refData.brands, device.brandId)}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{lookupName(refData.categories, device.categoryId)}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{lookupName(refData.owners, device.ownerId)}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{device.status || '—'}
					</td>
					<td class="px-4 py-4 text-sm text-neutral-700 dark:text-neutral-300">
						{formatDate(device.purchaseDate)}
					</td>
				</tr>
			{/snippet}

			{#if isGrouped && groups}
				{#each groups as group (group.key)}
					{@const collapsed = isCollapsed(group.key)}
					<tr
						class="sticky top-0 z-10 border-y border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900"
						data-testid="device-group-header"
					>
						<th
							colspan={groupColspan}
							scope="colgroup"
							class="px-4 py-2 text-left"
						>
							<button
								type="button"
								onclick={() => toggleGroup(group.key)}
								aria-expanded={!collapsed}
								aria-controls={`group-${group.key}`}
								aria-label={collapsed
									? t('devices.groups.expandGroup', { label: group.label })
									: t('devices.groups.collapseGroup', { label: group.label })}
								class="inline-flex w-full items-center gap-2 text-sm font-semibold text-neutral-900 hover:text-primary-600 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-1 dark:text-neutral-50 dark:hover:text-primary-400"
							>
								<svg
									class="h-4 w-4 transition-transform"
									class:rotate-[-90deg]={collapsed}
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
									aria-hidden="true"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 9l-7 7-7-7"
									/>
								</svg>
								<span>{group.label}</span>
								<span
									class="ml-1 inline-flex items-center rounded-full bg-neutral-200 px-2 py-0.5 text-xs font-medium text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300"
								>
									{group.count}
								</span>
							</button>
						</th>
					</tr>
					{#if !collapsed}
						{#each group.devices as device (device.id)}
							{@render desktopRow(device)}
						{/each}
					{/if}
				{/each}
			{:else}
				{#each devices as device (device.id)}
					{@render desktopRow(device)}
				{/each}
			{/if}
		</tbody>
	</table>
</div>

<!-- Mobile card layout -->
<div class="md:hidden space-y-4">
	{#snippet mobileCard(device: DeviceResponse)}
		<div
			class="relative rounded-lg border border-neutral-200 bg-white transition-colors hover:bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-950 dark:hover:bg-neutral-900"
			class:ring-2={selectable && isSelected(device.id)}
			class:ring-primary-500={selectable && isSelected(device.id)}
		>
			{#if selectable}
				<label class="absolute left-3 top-3 z-10 flex h-8 w-8 cursor-pointer items-center justify-center">
					<input
						type="checkbox"
						class="h-5 w-5 cursor-pointer rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
						checked={isSelected(device.id)}
						onchange={(e) => handleSelectToggle(e, device.id)}
						aria-label={t('devices.bulk.selectRow', { name: device.name || device.id })}
					/>
				</label>
			{/if}
			<a
				href="/devices/{device.id}"
				class="block p-4"
				class:pl-12={selectable}
			>
				<div class="flex items-start justify-between">
					<div class="flex-1">
						<h3 class="text-base font-semibold text-neutral-900 dark:text-neutral-50">
							{device.name || '—'}
						</h3>
						<dl class="mt-2 space-y-1 text-sm text-neutral-700 dark:text-neutral-300">
							<div class="flex gap-2">
								<dt class="font-medium">{t('devices.columns.brand')}:</dt>
								<dd>{lookupName(refData.brands, device.brandId)}</dd>
							</div>
							<div class="flex gap-2">
								<dt class="font-medium">{t('devices.columns.category')}:</dt>
								<dd>{lookupName(refData.categories, device.categoryId)}</dd>
							</div>
							<div class="flex gap-2">
								<dt class="font-medium">{t('devices.columns.owner')}:</dt>
								<dd>{lookupName(refData.owners, device.ownerId)}</dd>
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
		</div>
	{/snippet}

	{#if isGrouped && groups}
		{#each groups as group (group.key)}
			{@const collapsed = isCollapsed(group.key)}
			<section aria-label={group.label} data-testid="device-group-section-mobile">
				<button
					type="button"
					onclick={() => toggleGroup(group.key)}
					aria-expanded={!collapsed}
					class="mb-2 flex w-full items-center gap-2 rounded-lg bg-neutral-100 px-3 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-200 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:bg-neutral-900 dark:text-neutral-50 dark:hover:bg-neutral-800"
				>
					<svg
						class="h-4 w-4 transition-transform"
						class:rotate-[-90deg]={collapsed}
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						aria-hidden="true"
					>
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
					</svg>
					<span class="flex-1 text-left">{group.label}</span>
					<span class="rounded-full bg-neutral-200 px-2 py-0.5 text-xs font-medium text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300">
						{group.count}
					</span>
				</button>
				{#if !collapsed}
					<div class="space-y-3">
						{#each group.devices as device (device.id)}
							{@render mobileCard(device)}
						{/each}
					</div>
				{/if}
			</section>
		{/each}
	{:else}
		{#each devices as device (device.id)}
			{@render mobileCard(device)}
		{/each}
	{/if}
</div>

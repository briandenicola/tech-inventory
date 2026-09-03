<!--
	DeviceTableCards.svelte — F045 D-178 extraction.

	The 2-up mobile card grid, split out of the former monolithic
	DeviceTable.svelte. Mobile-web mode only — `presentation='pwa'` swaps this
	out for `DevicePwaList` instead (see `DeviceTable.svelte`).

	F026: 2-up grid at mobile widths to roughly double the device density per
	screen. Cards drop secondary metadata (owner, purchase date) and show only
	name + brand + category so two cards fit comfortably at the 360 px
	breakpoint we target. The full record is still one tap away in the detail
	modal. Status badge removed per #141 — status remains in the detail surface.

	`visibleColumns` never reaches this renderer — it is a desktop-table-only
	preference (D-178).
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import { lookupName } from '$lib/utils/deviceDisplay';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceGroup } from '$lib/utils/groupDevices';

	interface Props {
		devices: DeviceResponse[];
		groups?: DeviceGroup[];
		selectable?: boolean;
		selectedIds?: Set<string>;
		onToggleSelect?: (id: string) => void;
		onOpenDevice?: (deviceId: string) => void;
	}

	let { devices, groups, selectable = false, selectedIds, onToggleSelect, onOpenDevice }: Props =
		$props();

	const refData = $derived($referenceDataStore);
	const isGrouped = $derived(Array.isArray(groups) && groups.length > 0);

	function isSelected(id: string): boolean {
		return selectedIds?.has(id) ?? false;
	}
	function handleSelectToggle(event: Event, id: string) {
		event.stopPropagation();
		onToggleSelect?.(id);
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

	function openDevice(deviceId: string): void {
		if (onOpenDevice) {
			onOpenDevice(deviceId);
			return;
		}
		window.location.href = `/devices/${deviceId}`;
	}
</script>

<div class="grid grid-cols-2 gap-3">
	{#snippet mobileCard(device: DeviceResponse)}
		{@const selected = selectable && isSelected(device.id)}
		<div
			class="relative rounded-lg border border-neutral-200 bg-white transition-colors hover:bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-950 dark:hover:bg-neutral-900 {selected
				? 'ring-2 ring-primary-500 bg-primary-500/5'
				: ''}"
		>
			{#if selectable}
				<label
					class="absolute right-2 top-2 z-10 flex h-8 w-8 cursor-pointer items-center justify-center"
				>
					<input
						type="checkbox"
						class="h-5 w-5 cursor-pointer rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
						checked={isSelected(device.id)}
						onchange={(e) => handleSelectToggle(e, device.id)}
						aria-label={t('devices.bulk.selectRow', { name: device.name || device.id })}
					/>
				</label>
			{/if}
			{#if onOpenDevice}
				<button
					type="button"
					onclick={() => openDevice(device.id)}
					class="block w-full p-3 text-left"
					class:pr-10={selectable}
				>
					<h3 class="text-sm font-semibold leading-tight text-neutral-900 dark:text-neutral-50">
						{device.name || '—'}
					</h3>
					{#if device.model}
						<p class="mt-0.5 truncate text-xs text-neutral-500 dark:text-neutral-400">
							{device.model}
						</p>
					{/if}
					<dl class="mt-2 space-y-0.5 text-xs text-neutral-700 dark:text-neutral-300">
						<div class="flex gap-1 truncate">
							<dt class="font-medium text-neutral-500 dark:text-neutral-400">
								{t('devices.columns.brand')}:
							</dt>
							<dd class="truncate">{lookupName(refData.brands, device.brandId)}</dd>
						</div>
						<div class="flex gap-1 truncate">
							<dt class="font-medium text-neutral-500 dark:text-neutral-400">
								{t('devices.columns.category')}:
							</dt>
							<dd class="truncate">{lookupName(refData.categories, device.categoryId)}</dd>
						</div>
					</dl>
				</button>
			{:else}
				<a href={`/devices/${device.id}`} class="block p-3" class:pr-10={selectable}>
					<h3 class="text-sm font-semibold leading-tight text-neutral-900 dark:text-neutral-50">
						{device.name || '—'}
					</h3>
					{#if device.model}
						<p class="mt-0.5 truncate text-xs text-neutral-500 dark:text-neutral-400">
							{device.model}
						</p>
					{/if}
					<dl class="mt-2 space-y-0.5 text-xs text-neutral-700 dark:text-neutral-300">
						<div class="flex gap-1 truncate">
							<dt class="font-medium text-neutral-500 dark:text-neutral-400">
								{t('devices.columns.brand')}:
							</dt>
							<dd class="truncate">{lookupName(refData.brands, device.brandId)}</dd>
						</div>
						<div class="flex gap-1 truncate">
							<dt class="font-medium text-neutral-500 dark:text-neutral-400">
								{t('devices.columns.category')}:
							</dt>
							<dd class="truncate">{lookupName(refData.categories, device.categoryId)}</dd>
						</div>
					</dl>
				</a>
			{/if}
		</div>
	{/snippet}

	{#if isGrouped && groups}
		{#each groups as group (group.key)}
			{@const collapsed = isCollapsed(group.key)}
			<section
				aria-label={group.label}
				data-testid="device-group-section-mobile"
				class="col-span-2"
			>
				<button
					type="button"
					onclick={() => toggleGroup(group.key)}
					aria-expanded={!collapsed}
					class="mb-2 flex w-full items-center gap-2 rounded-lg bg-neutral-100 px-3 py-2 text-sm font-semibold text-neutral-900 hover:bg-neutral-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-900 dark:text-neutral-50 dark:hover:bg-neutral-800"
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
					<span class="flex-1 text-left">{group.label}</span>
					<span
						class="rounded-full bg-neutral-200 px-2 py-0.5 text-xs font-medium text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300"
					>
						{group.count}
					</span>
				</button>
				{#if !collapsed}
					<div class="grid grid-cols-2 gap-3">
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

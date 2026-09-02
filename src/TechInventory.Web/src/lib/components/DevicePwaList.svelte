<!--
	DevicePwaList.svelte — F045 §5.5 / Drake D-F.

	Installed-PWA device list renderer: an inset-grouped list of
	`DevicePwaRow` rows instead of the 2-up mobile card grid. Group headers
	(chevron + label + count badge) are retained verbatim from
	`DeviceTableCards` per spec — only the row renderer changes.
-->
<script lang="ts">
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceGroup } from '$lib/utils/groupDevices';
	import type { CurrentUser } from '$lib/stores/auth';
	import DevicePwaRow from '$lib/components/DevicePwaRow.svelte';

	interface Props {
		devices: DeviceResponse[];
		groups?: DeviceGroup[];
		selectable?: boolean;
		selectedIds?: Set<string>;
		onToggleSelect?: (id: string) => void;
		onOpenDevice?: (deviceId: string) => void;
		currentUser: CurrentUser | null;
		onChanged?: () => void;
	}

	let {
		devices,
		groups,
		selectable = false,
		selectedIds,
		onToggleSelect,
		onOpenDevice,
		currentUser,
		onChanged
	}: Props = $props();

	const isGrouped = $derived(Array.isArray(groups) && groups.length > 0);

	function isSelected(id: string): boolean {
		return selectedIds?.has(id) ?? false;
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
</script>

{#snippet rowList(items: DeviceResponse[])}
	<div
		class="overflow-hidden rounded-2xl border border-neutral-200 bg-white dark:border-neutral-800 dark:bg-neutral-950"
	>
		{#each items as device (device.id)}
			<DevicePwaRow
				{device}
				{selectable}
				selected={isSelected(device.id)}
				{onToggleSelect}
				{onOpenDevice}
				{currentUser}
				{onChanged}
			/>
		{/each}
	</div>
{/snippet}

<div class="flex flex-col gap-6">
	{#if isGrouped && groups}
		{#each groups as group (group.key)}
			{@const collapsed = isCollapsed(group.key)}
			<section aria-label={group.label} data-testid="device-group-section-mobile">
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
					{@render rowList(group.devices)}
				{/if}
			</section>
		{/each}
	{:else}
		{@render rowList(devices)}
	{/if}
</div>

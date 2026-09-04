<!--
	DeviceTable.svelte — thin renderer selector (F045 D-178).

	Desktop table, mobile 2-up cards, and mobile table view are always
	available. `presentation='pwa'` swaps the cards renderer for the
	installed-PWA row list (`DevicePwaList`) when the caller's mobile view
	mode is 'cards' (the PWA default), and for the same scrollable
	`DeviceTableDesktop` used elsewhere when the caller's mobile view mode is
	'table' — so the app-mode "Table" view toggle (F045 B3) has somewhere to
	render. `visibleColumns` is threaded only to `DeviceTableDesktop` — PWA
	row-list and mobile cards never receive it (D-178 invariant).
-->
<script lang="ts">
	import { DEFAULT_TABLE_COLUMNS, type TableColumnId } from '$lib/stores/userPrefs';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceGroup } from '$lib/utils/groupDevices';
	import type { CurrentUser } from '$lib/stores/auth';
	import DeviceTableDesktop from '$lib/components/DeviceTableDesktop.svelte';
	import DeviceTableCards from '$lib/components/DeviceTableCards.svelte';
	import DevicePwaList from '$lib/components/DevicePwaList.svelte';

	interface Props {
		devices: DeviceResponse[];
		/** F023: when set, `devices` is partitioned into these groups for rendering. */
		groups?: DeviceGroup[];
		currentSort?: 'name' | 'purchaseDate' | 'createdAt';
		sortDir?: 'asc' | 'desc';
		onSort: (column: 'name' | 'purchaseDate' | 'createdAt') => void;
		/** F024: when true, renders selection checkboxes. */
		selectable?: boolean;
		selectedIds?: Set<string>;
		onToggleSelect?: (id: string) => void;
		onToggleSelectAll?: () => void;
		allVisibleSelected?: boolean;
		someVisibleSelected?: boolean;
		/** F031: mobile view mode — cards (default) or table (horizontally scrollable). */
		mobileViewMode?: 'cards' | 'table';
		onOpenDevice?: (deviceId: string) => void;
		/** Visible columns in display order. Desktop-table-only (D-178). */
		visibleColumns?: TableColumnId[];
		/** Effective per-column widths. Desktop instance only. */
		columnWidths?: Record<TableColumnId, number>;
		onResizeColumn?: (column: TableColumnId, width: number) => void;
		onResetColumnWidth?: (column: TableColumnId) => void;
		/** F045: 'pwa' renders DevicePwaList instead of the 2-up card grid. */
		presentation?: 'auto' | 'pwa';
		/** F045: required by DevicePwaList's DeviceActionsMenu permission checks. */
		currentUser?: CurrentUser | null;
		/** Starting state for PWA groups, from the user's saved preference. */
		defaultCollapsed?: boolean;
		/** Counter that forces every PWA group open or closed. */
		bulkToggleSignal?: number;
		bulkToggleTarget?: 'expand' | 'collapse';
		/** F045: refresh hook forwarded to DevicePwaList row actions. */
		onChanged?: () => void;
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
		someVisibleSelected = false,
		mobileViewMode = 'cards',
		onOpenDevice,
		visibleColumns = DEFAULT_TABLE_COLUMNS,
		columnWidths,
		onResizeColumn,
		onResetColumnWidth,
		presentation = 'auto',
		defaultCollapsed = false,
		bulkToggleSignal = 0,
		bulkToggleTarget = 'expand',
		currentUser = null,
		onChanged
	}: Props = $props();
</script>

<!-- F045 D-175: presentation='pwa' is gated by standalone-display-mode, not
	 viewport — it must render at every width, never fall back to the
	 viewport-based desktop/mobile split below.
	 #142: installed PWA has one list presentation (DevicePwaList); the
	 mobileViewMode='table' path is removed from the PWA branch so the
	 view-mode toggle has no effect in app mode. -->
{#if presentation === 'pwa'}
	<DevicePwaList
		{devices}
		{groups}
		{defaultCollapsed}
		{bulkToggleSignal}
		{bulkToggleTarget}
		{selectable}
		{selectedIds}
		{onToggleSelect}
		{onOpenDevice}
		{currentUser}
		{onChanged}
	/>
{:else}
	<!-- Desktop table (hidden on mobile) -->
	<div class="hidden md:block overflow-x-auto">
		<DeviceTableDesktop
			{devices}
			{groups}
			{currentSort}
			{sortDir}
			{onSort}
			{selectable}
			{selectedIds}
			{onToggleSelect}
			{onToggleSelectAll}
			{allVisibleSelected}
			{someVisibleSelected}
			{onOpenDevice}
			{visibleColumns}
			resizable
			{columnWidths}
			{onResizeColumn}
			{onResetColumnWidth}
		/>
	</div>

	<!-- Mobile table mode (horizontally scrollable, only when mobileViewMode === 'table') -->
	{#if mobileViewMode === 'table'}
		<div class="md:hidden overflow-x-auto">
			<DeviceTableDesktop
				{devices}
				{groups}
				{currentSort}
				{sortDir}
				{onSort}
				{selectable}
				{selectedIds}
				{onToggleSelect}
				{onToggleSelectAll}
				{allVisibleSelected}
				{someVisibleSelected}
				{onOpenDevice}
				{visibleColumns}
			/>
		</div>
	{/if}

	<!-- Mobile cards-mode: the 2-up card grid. -->
	{#if mobileViewMode === 'cards'}
		<div class="md:hidden">
			<DeviceTableCards {devices} {groups} {selectable} {selectedIds} {onToggleSelect} {onOpenDevice} />
		</div>
	{/if}
{/if}

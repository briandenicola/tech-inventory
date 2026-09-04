<!--
	DeviceTableDesktop.svelte — F045 D-178 extraction.

	The semantic <table> renderer, split out of the former monolithic
	DeviceTable.svelte so `visibleColumns` stays provably scoped to this
	renderer only (desktop browser table + the mobile horizontally-scrollable
	table view). PWA rows and mobile cards never see this component.

	Per T15: Paginated table with columns: Name, Brand, Category, Owner, Status, Purchase Date.
	Per T17: Sortable columns (Name, Purchase Date, Created At) with aria-sort + URL-backed.
	Per F023: When `groups` is provided, rows are partitioned under collapsible group headers.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import {
		DEFAULT_TABLE_COLUMNS,
		DEFAULT_TABLE_COLUMN_WIDTHS,
		MAX_TABLE_COLUMN_WIDTH,
		MIN_TABLE_COLUMN_WIDTH,
		type TableColumnId
	} from '$lib/stores/userPrefs';
	import { columnResize } from '$lib/actions/columnResize';
	import { lookupName } from '$lib/utils/deviceDisplay';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceGroup } from '$lib/utils/groupDevices';

	interface Props {
		devices: DeviceResponse[];
		groups?: DeviceGroup[];
		currentSort?: 'name' | 'purchaseDate' | 'createdAt';
		sortDir?: 'asc' | 'desc';
		onSort: (column: 'name' | 'purchaseDate' | 'createdAt') => void;
		selectable?: boolean;
		selectedIds?: Set<string>;
		onToggleSelect?: (id: string) => void;
		onToggleSelectAll?: () => void;
		allVisibleSelected?: boolean;
		someVisibleSelected?: boolean;
		onOpenDevice?: (deviceId: string) => void;
		/** Visible columns in display order. Desktop-table-only per D-178. */
		visibleColumns?: TableColumnId[];
		/**
		 * Enables drag/keyboard column resizing. Off by default because this same
		 * component also renders the mobile horizontally-scrolling table view, where
		 * the table is already wider than the viewport and a resize handle would just
		 * be a hard-to-hit target competing with the scroll gesture.
		 */
		resizable?: boolean;
		/** Effective widths in CSS pixels, keyed by column. */
		columnWidths?: Record<TableColumnId, number>;
		/** Fired when a resize gesture ends. The caller persists. */
		onResizeColumn?: (column: TableColumnId, width: number) => void;
		/** Fired when the user asks for one column's default back. */
		onResetColumnWidth?: (column: TableColumnId) => void;
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
		onOpenDevice,
		visibleColumns = DEFAULT_TABLE_COLUMNS,
		resizable = false,
		columnWidths,
		onResizeColumn,
		onResetColumnWidth
	}: Props = $props();

	/**
	 * Widths shown right now. Seeded from the persisted values and updated live
	 * during a drag so the column tracks the cursor; the caller only hears about it
	 * when the gesture ends, so a drag is one write rather than dozens.
	 */
	let draftWidths = $state<Record<string, number>>({ ...DEFAULT_TABLE_COLUMN_WIDTHS });

	$effect(() => {
		draftWidths = { ...DEFAULT_TABLE_COLUMN_WIDTHS, ...(columnWidths ?? {}) };
	});

	function widthOf(column: TableColumnId): number {
		return draftWidths[column] ?? DEFAULT_TABLE_COLUMN_WIDTHS[column];
	}

	/** Select checkbox (48) + every visible column + the sticky Actions column (120). */
	const totalWidth = $derived(
		(selectable ? 48 : 0) +
			visibleColumns.reduce((sum, col) => sum + widthOf(col), 0) +
			120
	);

	function commitWidth(column: TableColumnId, width: number) {
		draftWidths = { ...draftWidths, [column]: width };
		onResizeColumn?.(column, width);
	}

	function previewWidth(column: TableColumnId, width: number) {
		draftWidths = { ...draftWidths, [column]: width };
	}

	function resetWidth(column: TableColumnId) {
		draftWidths = { ...draftWidths, [column]: DEFAULT_TABLE_COLUMN_WIDTHS[column] };
		onResetColumnWidth?.(column);
	}

	const refData = $derived($referenceDataStore);
	const isGrouped = $derived(Array.isArray(groups) && groups.length > 0);
	// +1 for actions column, +1 if selectable
	const groupColspan = $derived((selectable ? 1 : 0) + visibleColumns.length + 1);

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

	// Toggle sort: current asc → desc → asc (2-state cycle per D-052 choice)
	function handleSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		onSort(column);
	}

	// Aria-sort helper
	function getAriaSort(column: 'name' | 'purchaseDate' | 'createdAt') {
		if (currentSort !== column) return 'none';
		return sortDir === 'asc' ? 'ascending' : 'descending';
	}

	// Sortable columns
	type SortableColumn = 'name' | 'purchaseDate' | 'createdAt';
	const SORTABLE_COLUMNS: SortableColumn[] = ['name', 'purchaseDate'];

	function isSortable(col: TableColumnId): boolean {
		return (SORTABLE_COLUMNS as string[]).includes(col);
	}

	function getCellValue(device: DeviceResponse, col: TableColumnId): string {
		switch (col) {
			case 'name':
				return device.name || '—';
			case 'model':
				return device.model || '—';
			case 'brand':
				return lookupName(refData.brands, device.brandId);
			case 'category':
				return lookupName(refData.categories, device.categoryId);
			case 'owner':
				return lookupName(refData.owners, device.ownerId);
			case 'status':
				return device.status || '—';
			case 'purchaseDate':
				return formatDate(device.purchaseDate);
			default:
				return '—';
		}
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

	function openDevice(deviceId: string): void {
		if (onOpenDevice) {
			onOpenDevice(deviceId);
			return;
		}

		window.location.href = `/devices/${deviceId}`;
	}
</script>

<!--
	`table-fixed` + <colgroup> only in the resizable (desktop) case. Auto layout
	ignores width hints once the table is cramped — which is exactly when a width
	matters — so honouring a user's chosen width requires fixed layout. The mobile
	table view keeps auto layout, where letting content size the columns is the
	better default.
-->
<table
	class="divide-y divide-neutral-200 dark:divide-neutral-800 {resizable ? 'table-fixed' : 'min-w-full'}"
	style={resizable ? `width:${totalWidth}px;min-width:100%` : undefined}
>
	<caption class="sr-only">{t('devices.list.title')}</caption>
	{#if resizable}
		<colgroup>
			{#if selectable}
				<col style="width:48px" />
			{/if}
			{#each visibleColumns as col (col)}
				<col style={`width:${widthOf(col)}px`} />
			{/each}
			<col style="width:120px" />
		</colgroup>
	{/if}
	<thead class="bg-neutral-50 dark:bg-neutral-900">
		<tr>
			{#if selectable}
				<th scope="col" class="w-12 px-4 py-3 text-left">
					<input
						type="checkbox"
						class="h-4 w-4 cursor-pointer rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
						checked={allVisibleSelected}
						use:setIndeterminate={!allVisibleSelected && someVisibleSelected}
						onchange={() => onToggleSelectAll?.()}
						aria-label={t('devices.bulk.selectAllVisible')}
					/>
				</th>
			{/if}
			{#each visibleColumns as col, colIdx (col)}
				{@const isFirst = colIdx === 0}
				{@const sortable = isSortable(col)}
				<th
					scope="col"
					class="relative {isFirst ? 'sticky left-0 z-10 bg-neutral-50 dark:bg-neutral-900 border-r border-neutral-200 dark:border-neutral-800 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.1)]' : ''} px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300 {resizable ? 'truncate' : ''}"
					aria-sort={sortable ? getAriaSort(col as SortableColumn) : undefined}
				>
					{#if sortable}
						<button
							type="button"
							onclick={() => handleSort(col as SortableColumn)}
							class="group inline-flex items-center gap-1 hover:text-neutral-900 dark:hover:text-neutral-100"
						>
							{t(`devices.columns.${col}`)}
							{#if currentSort === col}
								<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
									{#if sortDir === 'asc'}
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
									{:else}
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
									{/if}
								</svg>
							{:else}
								<svg class="h-4 w-4 opacity-0 group-hover:opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4" />
								</svg>
							{/if}
						</button>
					{:else}
						{t(`devices.columns.${col}`)}
					{/if}

					{#if resizable}
						<!--
							role="separator" with aria-valuenow is the ARIA pattern for a
							resize grip: it announces the current width and makes the arrow-key
							behaviour discoverable, rather than presenting a bare div that a
							screen reader would skip entirely.

							Placed inside the <th>, which is position:relative, so the grip sits
							on the cell's own right edge whatever the column width is.
						-->
						<!--
							svelte-ignore a11y_no_noninteractive_tabindex
							A focusable separator IS interactive per WAI-ARIA — this is the
							Window Splitter pattern (role="separator" + tabindex + aria-valuenow),
							which is precisely what a resize grip is. The rule does not model
							that case. Downgrading to a non-focusable element would make the
							column keyboard-unreachable, which is the actual accessibility
							failure this suppression avoids.
						-->
						<span
							role="separator"
							tabindex="0"
							aria-orientation="vertical"
							aria-label={t('devices.table.resizeColumn', {
								column: t(`devices.columns.${col}`)
							})}
							aria-valuenow={widthOf(col)}
							aria-valuemin={MIN_TABLE_COLUMN_WIDTH}
							aria-valuemax={MAX_TABLE_COLUMN_WIDTH}
							data-testid={`resize-${col}`}
							class="absolute inset-y-0 right-0 z-20 w-2 cursor-col-resize touch-none select-none opacity-0 transition-opacity hover:opacity-100 focus-visible:opacity-100 focus-visible:outline-none"
							use:columnResize={{
								width: widthOf(col),
								min: MIN_TABLE_COLUMN_WIDTH,
								max: MAX_TABLE_COLUMN_WIDTH,
								onResize: (width) => previewWidth(col, width),
								onCommit: (width) => commitWidth(col, width),
								onReset: () => resetWidth(col)
							}}
						>
							<!-- The visible grip: a hairline that thickens on hover/focus. -->
							<span
								class="pointer-events-none absolute inset-y-1 right-0 w-0.5 rounded bg-primary-500"
								aria-hidden="true"
							></span>
						</span>
					{/if}
				</th>
			{/each}

			<!-- Actions (non-sortable) -->
			<th
				scope="col"
				class="sticky right-0 z-10 border-l border-neutral-200 bg-neutral-50 px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-neutral-700 shadow-[-2px_0_4px_-2px_rgba(0,0,0,0.1)] dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-300"
			>
				{t('common.labels.actions')}
			</th>
		</tr>
	</thead>
	<tbody class="divide-y divide-neutral-200 bg-white dark:divide-neutral-800 dark:bg-neutral-950">
		{#snippet desktopRow(device: DeviceResponse)}
			{@const selected = selectable && isSelected(device.id)}
			<tr
				class="group/row cursor-pointer transition-colors hover:bg-neutral-50 dark:hover:bg-neutral-900 {selected
					? 'bg-primary-500/10'
					: ''}"
				onclick={() => openDevice(device.id)}
			>
				{#if selectable}
					<td
						class="w-12 px-4 py-4 {selected ? 'border-l-4 border-primary-500' : ''}"
						onclick={(e) => e.stopPropagation()}
					>
						<input
							type="checkbox"
							class="h-4 w-4 cursor-pointer rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
							checked={isSelected(device.id)}
							onchange={(e) => handleSelectToggle(e, device.id)}
							aria-label={t('devices.bulk.selectRow', { name: device.name || device.id })}
						/>
					</td>
				{/if}
				{#each visibleColumns as col, colIdx (col)}
					{@const isFirst = colIdx === 0}
					<td class="{isFirst ? `sticky left-0 z-10 border-r border-neutral-200 dark:border-neutral-800 shadow-[2px_0_4px_-2px_rgba(0,0,0,0.1)] ${selected ? 'bg-primary-500/10' : 'bg-white dark:bg-neutral-950'} group-hover/row:bg-neutral-50 dark:group-hover/row:bg-neutral-900` : 'truncate'} px-4 py-4 text-sm {isFirst ? 'font-medium text-neutral-900 dark:text-neutral-50' : 'text-neutral-700 dark:text-neutral-300'}">
						{getCellValue(device, col)}
					</td>
				{/each}
				<td class="sticky right-0 z-10 border-l border-neutral-200 px-4 py-4 text-right shadow-[-2px_0_4px_-2px_rgba(0,0,0,0.1)] dark:border-neutral-800 {selected ? 'bg-primary-500/10' : 'bg-white dark:bg-neutral-950'} group-hover/row:bg-neutral-50 dark:group-hover/row:bg-neutral-900">
					<button
						type="button"
						onclick={(e) => {
							e.stopPropagation();
							openDevice(device.id);
						}}
						class="inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 hover:text-neutral-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
						aria-label={t('common.actions.viewDetails', { name: device.name || device.id })}
					>
						{t('common.actions.view')}
						<svg
							class="h-4 w-4"
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
					</button>
				</td>
			</tr>
		{/snippet}

		{#if isGrouped && groups}
			{#each groups as group (group.key)}
				{@const collapsed = isCollapsed(group.key)}
				<tr
					class="border-y border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900"
					data-testid="device-group-header"
				>
					<th colspan={groupColspan} scope="colgroup" class="px-4 py-2 text-left">
						<button
							type="button"
							onclick={() => toggleGroup(group.key)}
							aria-expanded={!collapsed}
							aria-controls={`group-${group.key}`}
							aria-label={collapsed
								? t('devices.groups.expandGroup', { label: group.label })
								: t('devices.groups.collapseGroup', { label: group.label })}
							class="inline-flex w-full items-center gap-2 text-sm font-semibold text-neutral-900 hover:text-primary-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-1 dark:text-neutral-50 dark:hover:text-primary-400"
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

<!--
	DeviceFilters.svelte — T16 "View" panel (search + facets + view options)

	Named DeviceFilters for its history; the panel now also carries view options
	(Group by, bulk expand/collapse, PWA selection mode), so its heading and the
	button that opens it read "View" rather than "Filters".
	
	Layout: Sidebar on desktop (~280px left of table), collapsible drawer on mobile.
	Controls: search (debounced 300ms), brand/category/owner/location/network dropdowns,
	status multi-select, purchase year range, "Clear all" button,
	Group by + Expand/Collapse all groups.
	URL-backed via $page.url.searchParams.
	Reference data cached in referenceDataStore (fetch once on mount).
	
	Related: specs/002-frontend-mvp/spec.md §5, T16
-->
<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { t } from '$lib/i18n';
	import { referenceDataStore, fetchReferenceData } from '$lib/stores/referenceData';
	import type { DeviceFilters, DeviceStatus } from '$lib/queries/devices.svelte';

	interface Props {
		filters: DeviceFilters;
		onFiltersChange: (filters: DeviceFilters) => void;
		isOpen?: boolean;
		onClose?: () => void;
		onSaveDefault?: () => void;
		onClearDefault?: () => void;
		hasStoredDefault?: boolean;
		canSaveDefault?: boolean;
		/** F045 B2 follow-up: true when the installed PWA's implicit
		 *  category-grouping default is in effect (no explicit user choice).
		 *  Presentation-only — never written to the URL or counted toward an
		 *  active-filter badge. */
		implicitGroupingActive?: boolean;
		/** #145: true when running as an installed PWA — reveals the SELECTION
		 *  section so the user can opt in to row checkboxes. */
		isPwa?: boolean;
		/** #145: current PWA selection-mode state. */
		pwaSelectionMode?: boolean;
		/** #145: called when the user taps "Enable Selection Mode". */
		onEnablePwaSelection?: () => void;
		/** #145: called when the user taps "Exit Selection Mode" (clears selection). */
		onDisablePwaSelection?: () => void;
		/** True when a grouping dimension is actually in effect for the rendered
		 *  list — the explicit `groupBy`, or the PWA's implicit category default.
		 *  Gates the bulk expand/collapse actions, which have nothing to act on
		 *  when the list is flat. Deliberately the *applied* grouping, not
		 *  `pending.groupBy`: the buttons act on what is on screen right now. */
		groupingActive?: boolean;
		/** Expand every group in the current list. */
		onExpandAllGroups?: () => void;
		/** Collapse every group in the current list. */
		onCollapseAllGroups?: () => void;
	}

	let {
		filters,
		onFiltersChange,
		isOpen = true,
		onClose,
		onSaveDefault,
		onClearDefault,
		hasStoredDefault = false,
		canSaveDefault = true,
		implicitGroupingActive = false,
		isPwa = false,
		pwaSelectionMode = false,
		onEnablePwaSelection,
		onDisablePwaSelection,
		groupingActive = false,
		onExpandAllGroups,
		onCollapseAllGroups
	}: Props = $props();

	const refData = $derived($referenceDataStore);
	const panelOpen = $derived(isOpen && Boolean(onClose));
	const headerPaddingTop = 'calc(env(safe-area-inset-top, 0px) + var(--space-4))';
	const footerPaddingBottom = 'calc(env(safe-area-inset-bottom, 0px) + var(--space-4))';

	let dialogElement = $state<HTMLElement | null>(null);
	let previousFocusedElement: HTMLElement | null = null;

	// Define status options with proper typing
	const statusOptions: DeviceStatus[] = ['Active', 'Retired', 'Disposed', 'InRepair', 'Lent'];

	// #128 explicit Apply model: every control in this panel edits a local
	// `pending` copy of the filters, never the applied `filters` prop
	// directly. Nothing reaches the parent (and therefore the URL, the
	// query, the rendered rows, or the applied-filter badge) until the user
	// presses Apply. This is what fixes "changing filter values only
	// sometimes updates the device list" — previously every control called
	// `onFiltersChange` immediately, so partial/interrupted interactions
	// (e.g. two checkboxes ticked in quick succession) raced each other.
	// The top-of-page search box is intentionally NOT part of this model —
	// it lives outside this component entirely (in the devices page header)
	// and keeps its existing live-debounced behavior.
	// `untrack` makes the one-time-only read explicit to both the compiler
	// (silencing the "state_referenced_locally" warning) and future readers
	// — this line intentionally does NOT react to `filters` changing; the
	// `$effect` below is what re-baselines `pending` afterward.
	let pending = $state<DeviceFilters>(untrack(() => ({ ...filters })));

	// Re-baseline `pending` from the applied `filters` every time the panel
	// transitions to open. This guarantees a freshly opened panel always
	// reflects reality (fixing stale-panel bugs) while deliberately
	// discarding any never-applied edits from a previous open/close cycle —
	// closing the panel without pressing Apply abandons those edits, which
	// mirrors how the Escape/backdrop-close already behaved for focus.
	$effect(() => {
		if (panelOpen) {
			pending = { ...filters };
		}
	});

	function statusSetsEqual(a: DeviceStatus[] | undefined, b: DeviceStatus[] | undefined): boolean {
		const left = [...(a || [])].sort();
		const right = [...(b || [])].sort();
		return left.length === right.length && left.every((value, index) => value === right[index]);
	}

	// True whenever the panel's local edits differ from the last-applied
	// filters — drives the visually-distinct pending state (banner + Apply
	// button emphasis) required by #128's acceptance criteria.
	const hasPendingChanges = $derived.by(() => {
		return (
			(pending.groupBy || undefined) !== (filters.groupBy || undefined) ||
			(pending.brandId || undefined) !== (filters.brandId || undefined) ||
			(pending.categoryId || undefined) !== (filters.categoryId || undefined) ||
			(pending.ownerId || undefined) !== (filters.ownerId || undefined) ||
			(pending.locationId || undefined) !== (filters.locationId || undefined) ||
			(pending.networkId || undefined) !== (filters.networkId || undefined) ||
			(pending.purchaseYearMin || undefined) !== (filters.purchaseYearMin || undefined) ||
			(pending.purchaseYearMax || undefined) !== (filters.purchaseYearMax || undefined) ||
			!statusSetsEqual(pending.status, filters.status)
		);
	});

	// Filter change handlers — write to `pending` only (see above).
	function handleFilterChange(
		key: keyof DeviceFilters,
		value: string | number | DeviceStatus[] | undefined
	) {
		pending = { ...pending, [key]: value || undefined, page: 1 };
	}

	// Status multi-select
	let statusValues = $derived(pending.status || []);
	function toggleStatus(status: DeviceStatus) {
		const current = [...statusValues];
		const index = current.indexOf(status);
		if (index > -1) {
			current.splice(index, 1);
		} else {
			current.push(status);
		}
		pending = { ...pending, status: current.length > 0 ? current : undefined, page: 1 };
	}

	// Apply pending changes — the only place (besides Clear All) that calls
	// onFiltersChange, so the URL/query/rows/badge only ever move in one
	// deliberate step.
	function applyFilters() {
		onFiltersChange({ ...pending });
	}

	// Clear all filters. Unlike other panel edits this applies immediately:
	// it is an unambiguous, explicit action rather than an in-progress edit,
	// so there is nothing to stage. pageSize and groupBy are preserved from
	// the current panel state (groupBy is a view preference, not a filter
	// criterion — see F045 B2) rather than silently reverted.
	function clearAll() {
		const cleared: DeviceFilters = {
			page: 1,
			pageSize: pending.pageSize || 25,
			groupBy: pending.groupBy
		};
		pending = cleared;
		onFiltersChange(cleared);
	}

	function trapFocus(event: KeyboardEvent) {
		if (event.key !== 'Tab' || !dialogElement) {
			return;
		}

		const focusableElements = Array.from(
			dialogElement.querySelectorAll<HTMLElement>(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			)
		).filter((element) => !element.hasAttribute('disabled'));

		const firstElement = focusableElements[0];
		const lastElement = focusableElements[focusableElements.length - 1];

		if (!firstElement || !lastElement) {
			return;
		}

		if (event.shiftKey && document.activeElement === firstElement) {
			event.preventDefault();
			lastElement.focus();
		} else if (!event.shiftKey && document.activeElement === lastElement) {
			event.preventDefault();
			firstElement.focus();
		}
	}

	function focusInitialControl() {
		dialogElement
			?.querySelector<HTMLElement>(
				'[data-dialog-initial-focus], button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			)
			?.focus();
	}

	// Fetch reference data on mount
	onMount(() => {
		if (!refData.brands.length && !refData.isLoading) {
			void fetchReferenceData();
		}
	});

	$effect(() => {
		if (!panelOpen || !dialogElement) {
			return;
		}

		const previousOverflow = document.body.style.overflow;
		previousFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
		document.body.style.overflow = 'hidden';

		function handleKeydown(event: KeyboardEvent) {
			if (event.key === 'Escape') {
				event.preventDefault();
				onClose?.();
				return;
			}

			trapFocus(event);
		}

		dialogElement.addEventListener('keydown', handleKeydown);
		focusInitialControl();

		return () => {
			document.body.style.overflow = previousOverflow;
			dialogElement?.removeEventListener('keydown', handleKeydown);
			previousFocusedElement?.focus();
			previousFocusedElement = null;
		};
	});

	// Current year for year range max
	const currentYear = new Date().getFullYear();
</script>

<!--
	Mobile drawer backdrop.

	F026: the previous condition was inverted (`!isOpen && onClose`), which
	rendered a full-screen z-40 overlay *whenever the drawer was closed* — that
	intercepted every tap on the devices list and gave the "transparent element
	in front of devices" symptom Brian reported during field testing. Backdrop
	must only render when the drawer is actually open.
-->
{#if panelOpen}
	<div
		class="fixed inset-0 bg-neutral-900/50"
		style="z-index: var(--z-modal-backdrop);"
		onclick={onClose}
		role="presentation"
	></div>
{/if}

<!-- Filters sidebar/drawer -->
<div
	bind:this={dialogElement}
	id="device-filters-panel"
	class="fixed left-0 top-0 flex h-dvh w-[22rem] max-w-full transform flex-col overflow-hidden border-r border-neutral-200/70 bg-white transition-transform dark:border-neutral-800/70 dark:bg-neutral-950 md:w-96"
	class:translate-x-0={isOpen}
	class:-translate-x-full={!isOpen}
	role={panelOpen ? 'dialog' : undefined}
	aria-modal={panelOpen ? 'true' : undefined}
	aria-labelledby={panelOpen ? 'device-filters-title' : undefined}
	aria-hidden={!panelOpen ? 'true' : undefined}
	style="z-index: var(--z-modal);"
>
	<!-- Header -->
	<div class="sticky top-0 z-10 shrink-0 border-b border-neutral-200/70 bg-white/95 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95">
		<div class="flex items-center justify-between gap-3 px-7 pb-4" style={`padding-top: ${headerPaddingTop};`}>
			<h2 id="device-filters-title" class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('devices.filters.title')}
			</h2>
			{#if onClose}
				<button
					type="button"
					onclick={onClose}
					class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-600 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800"
					aria-label={t('devices.filters.closeFilters')}
					data-dialog-initial-focus
				>
					<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					</svg>
				</button>
			{/if}
		</div>
	</div>

	<div class="min-h-0 flex-1 overflow-y-auto overscroll-y-contain px-7 py-6">
		<!-- #128: pending-changes banner. Visually + programmatically (aria-live)
			 distinguishes unapplied edits from the applied state without gating
			 focus/keyboard reachability of the controls below. -->
		{#if hasPendingChanges}
			<div
				role="status"
				aria-live="polite"
				class="mb-6 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm font-medium text-amber-800 dark:border-amber-700/60 dark:bg-amber-900/30 dark:text-amber-200"
			>
				{t('devices.filters.pendingChanges')}
			</div>
		{/if}

		<!-- #145: SELECTION section — visible only in installed-PWA mode.
			 Lets the user opt in to per-row checkboxes without the checkboxes
			 always consuming horizontal space. Grouped under a clear section
			 heading per the reference pattern in issue #145. -->
		{#if isPwa}
			<section aria-label={t('devices.filters.selectionSection')} class="mb-6">
				<p class="mb-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
					{t('devices.filters.selectionSection')}
				</p>
				{#if pwaSelectionMode}
					<button
						type="button"
						onclick={() => { onDisablePwaSelection?.(); onClose?.(); }}
						class="inline-flex min-h-11 w-full items-center justify-center rounded-full border border-neutral-300 px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('devices.filters.exitSelectionMode')}
					</button>
				{:else}
					<button
						type="button"
						onclick={() => { onEnablePwaSelection?.(); onClose?.(); }}
						class="inline-flex min-h-11 w-full items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:bg-primary-500 dark:hover:bg-primary-600"
					>
						{t('devices.filters.enableSelectionMode')}
					</button>
				{/if}
			</section>
		{/if}

		<!-- Group By -->
		<div class="mb-6">
		<label for="groupBy" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.groupByLabel')}
		</label>
		<select
			id="groupBy"
			value={pending.groupBy || (implicitGroupingActive ? 'category' : 'none')}
			onchange={(e) => {
				const value = (e.target as HTMLSelectElement).value;
				pending = {
					...pending,
					groupBy: value as 'none' | 'category' | 'owner' | 'year',
					page: 1
				};
			}}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="none">{t('devices.filters.groupByNone')}</option>
			<option value="category">{t('devices.filters.groupByCategory')}</option>
			<option value="owner">{t('devices.filters.groupByOwner')}</option>
			<option value="year">{t('devices.filters.groupByYear')}</option>
		</select>
		{#if implicitGroupingActive}
			<!--
				F045 follow-up: devices.groups.pwaDefaultNote existed in en.json
				but was never rendered anywhere (a dead key). It belongs here: the
				<select> above shows "Category" (the effective value) even though
				no explicit choice was ever made, so a one-line note clarifies that
				this is the app's default, not a persisted user selection. Purely
				presentational — never written to the URL or counted in
				activeFilterCount.
			-->
			<p class="mt-2 text-sm text-neutral-500 dark:text-neutral-400">
				{t('devices.groups.pwaDefaultNote')}
			</p>
		{/if}

		<!--
			Bulk expand/collapse. Immediate actions rather than Apply-gated edits:
			they change nothing about the query, only how the already-loaded list is
			displayed, so there is nothing to stage (same reasoning as "Clear All
			Filters" above). Closing the panel is the point — the result is behind it.
			Rendered only while grouping is actually in effect, since a flat list has
			no groups to act on.
		-->
		{#if groupingActive}
			<!--
				One word each. "Expand all groups" wrapped to two lines in these
				half-width pills and read as a paragraph rather than a button; the
				surrounding "Grouped by …" note and the Group by control directly
				above already say what is being expanded.

				The fuller phrase stays as the accessible name, so a screen-reader
				user still hears "Expand all groups" out of context. WCAG 2.5.3 is
				satisfied because the accessible name starts with the visible label.

				`whitespace-nowrap` is the actual fix for the wrap; `truncate` is the
				fallback for a translation that still cannot fit, so it ellipsises
				inside its own pill rather than growing the row (the footer pair
				above carries the same guard).
			-->
			<div class="mt-3 flex gap-2" role="group" aria-label={t('devices.filters.groupActionsLabel')}>
				<button
					type="button"
					data-testid="expand-all-groups"
					aria-label={t('devices.filters.expandGroupsAria')}
					onclick={() => { onExpandAllGroups?.(); onClose?.(); }}
					class="inline-flex min-h-11 min-w-0 flex-1 items-center justify-center whitespace-nowrap rounded-full border border-neutral-300 px-4 py-2.5 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					<span class="truncate">{t('devices.filters.expandGroups')}</span>
				</button>
				<button
					type="button"
					data-testid="collapse-all-groups"
					aria-label={t('devices.filters.collapseGroupsAria')}
					onclick={() => { onCollapseAllGroups?.(); onClose?.(); }}
					class="inline-flex min-h-11 min-w-0 flex-1 items-center justify-center whitespace-nowrap rounded-full border border-neutral-300 px-4 py-2.5 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					<span class="truncate">{t('devices.filters.collapseGroups')}</span>
				</button>
			</div>
		{/if}
	</div>

	<!-- Brand -->
	<div class="mb-6">
		<label for="brand" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.brandLabel')}
		</label>
		<select
			id="brand"
			value={pending.brandId || ''}
			onchange={(e) => handleFilterChange('brandId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.brands as brand (brand.id)}
				<option value={brand.id}>{brand.name}</option>
			{/each}
		</select>
	</div>

	<!-- Category -->
	<div class="mb-6">
		<label for="category" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.categoryLabel')}
		</label>
		<select
			id="category"
			value={pending.categoryId || ''}
			onchange={(e) => handleFilterChange('categoryId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.categories as category (category.id)}
				<option value={category.id}>{category.name}</option>
			{/each}
		</select>
	</div>

	<!-- Owner -->
	<div class="mb-6">
		<label for="owner" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.ownerLabel')}
		</label>
		<select
			id="owner"
			value={pending.ownerId || ''}
			onchange={(e) => handleFilterChange('ownerId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.owners as owner (owner.id)}
				<option value={owner.id}>{owner.name}</option>
			{/each}
		</select>
	</div>

	<!-- Location -->
	<div class="mb-6">
		<label for="location" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.locationLabel')}
		</label>
		<select
			id="location"
			value={pending.locationId || ''}
			onchange={(e) => handleFilterChange('locationId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.locations as location (location.id)}
				<option value={location.id}>{location.name}</option>
			{/each}
		</select>
	</div>

	<!-- Network -->
	<div class="mb-6">
		<label for="network" class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.networkLabel')}
		</label>
		<select
			id="network"
			value={pending.networkId || ''}
			onchange={(e) => handleFilterChange('networkId', (e.target as HTMLSelectElement).value)}
			class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:focus:bg-neutral-900"
		>
			<option value="">{t('devices.filters.allOption')}</option>
			{#each refData.networks as network (network.id)}
				<option value={network.id}>{network.name}</option>
			{/each}
		</select>
	</div>

	<!-- Status multi-select -->
	<fieldset class="mb-6">
		<legend class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.statusLabel')}
		</legend>
		<!--
			Two per row: five stacked full-width rows spent 255px on five short
			words. Paired, the same five options cost 161.5px and the row grows from
			min-h-10 to min-h-11 (42.5 -> 46.75px), so the tap target improves while
			the panel gets 93.5px shorter.

			Measured in Chromium against the built stylesheet at 320/360/375/390/430
			CSS px — no overlap, no truncation, both columns of a row aligned at
			every width. At the narrowest (320px, where the w-[22rem] panel is
			capped by max-w-full) each column is 124.9px, leaving 82.4px of text
			room after the checkbox, gap and padding; the widest label ("Disposed")
			needs 69px. That headroom is why the in-label gap is gap-2 and the
			padding px-1.5 rather than the old gap-3/px-2, which left "In Repair"
			wrapping to two lines at 320px — exactly the misalignment this layout
			must not introduce.

			`truncate` is the belt-and-braces guard: a longer translation or a wider
			system font ellipsises inside its own column rather than wrapping and
			pushing its row taller than its neighbour's.
		-->
		<div class="grid grid-cols-2 gap-2.5">
			{#each statusOptions as status}
				<label class="flex min-h-11 min-w-0 cursor-pointer items-center gap-2 rounded-lg px-1.5 py-1.5 transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800">
					<input
						type="checkbox"
						checked={statusValues.includes(status)}
						onchange={() => toggleStatus(status)}
						class="h-5 w-5 shrink-0 rounded-md border-neutral-300 text-primary-600 focus-visible:ring-primary-500 focus-visible:ring-offset-0 dark:border-neutral-600 dark:bg-neutral-800"
					/>
					<span class="min-w-0 truncate text-base text-neutral-800 dark:text-neutral-200">
						{t(`devices.filters.status${status}`)}
					</span>
				</label>
			{/each}
		</div>
	</fieldset>

	<!-- Purchase Year Range -->
	<fieldset class="mb-7">
		<legend class="mb-2 block text-base font-medium text-neutral-800 dark:text-neutral-200">
			{t('devices.filters.yearRangeLabel')}
		</legend>
		<div class="flex gap-3">
			<div class="flex-1">
				<label for="yearMin" class="sr-only">{t('devices.filters.yearMinLabel')}</label>
				<input
					type="number"
					id="yearMin"
					min="1990"
					max={currentYear}
					value={pending.purchaseYearMin || ''}
					oninput={(e) =>
						handleFilterChange(
							'purchaseYearMin',
							parseInt((e.target as HTMLInputElement).value, 10) || undefined
						)}
					placeholder="1990"
					class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
				/>
			</div>
			<div class="flex-1">
				<label for="yearMax" class="sr-only">{t('devices.filters.yearMaxLabel')}</label>
				<input
					type="number"
					id="yearMax"
					min="1990"
					max={currentYear}
					value={pending.purchaseYearMax || ''}
					oninput={(e) =>
						handleFilterChange(
							'purchaseYearMax',
							parseInt((e.target as HTMLInputElement).value, 10) || undefined
						)}
					placeholder={currentYear.toString()}
					class="w-full min-h-11 rounded-xl border-0 bg-neutral-100 px-4 py-2.5 text-base text-neutral-900 placeholder:text-neutral-500 focus:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:bg-neutral-800 dark:text-neutral-100 dark:placeholder:text-neutral-400 dark:focus:bg-neutral-900"
				/>
			</div>
		</div>
	</fieldset>

		<!--
			Save/Clear default view live in the scroll area, not the sticky footer.
			They are occasional actions — set once, rarely revisited — and pinning
			them cost ~110px of permanent chrome on every phone viewport, hiding the
			options the panel exists to show. Apply and Clear all stay pinned
			because they are what you reach for on the way out.
		-->
		{#if onSaveDefault || onClearDefault}
			<section aria-label={t('devices.filters.defaultViewSection')} class="mb-6">
				<p class="mb-2 text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
					{t('devices.filters.defaultViewSection')}
				</p>
				<div class="flex flex-col gap-2">
					{#if onSaveDefault}
						<button
							type="button"
							onclick={onSaveDefault}
							disabled={!canSaveDefault || hasPendingChanges}
							class="inline-flex min-h-11 w-full items-center justify-center rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-500 dark:hover:bg-primary-600"
						>
							{t('devices.filters.saveDefault')}
						</button>
						{#if hasPendingChanges}
							<!--
								#128: Save as default view must save the applied filter
								set (D-188), not in-progress panel edits — otherwise the
								user thinks they saved what they see, but the stored
								default silently diverges. Disabling and explaining is
								cheaper and clearer than re-defining what "default" means.
							-->
							<p class="text-sm text-neutral-500 dark:text-neutral-400">
								{t('devices.filters.saveDefaultPendingHint')}
							</p>
						{/if}
					{/if}
					{#if onClearDefault && hasStoredDefault}
						<button
							type="button"
							onclick={onClearDefault}
							class="inline-flex min-h-11 w-full items-center justify-center rounded-full px-5 py-2.5 text-sm font-medium text-neutral-600 underline-offset-4 transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:text-neutral-400"
						>
							{t('devices.filters.clearDefault')}
						</button>
					{/if}
				</div>
			</section>
		{/if}

		<!-- ARIA live region for results announcement -->
		<div class="sr-only" role="status" aria-live="polite" aria-atomic="true">
			<!-- Will be populated by parent component with result count -->
		</div>
	</div>

	<div class="sticky bottom-0 z-10 shrink-0 border-t border-neutral-200/70 bg-white/95 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95">
		<!--
			One row, not a stack: two 46.75px pills side by side cost one row of
			height instead of two, and the panel's whole job is showing the options
			above them. Clear sits left and Apply right so the destructive-ish
			action is not under the thumb that just reached for Apply.
			`min-w-0` on both so a long localized label truncates its own button
			rather than forcing the row wider than the panel at 320px.
		-->
		<div class="grid grid-cols-2 gap-2 px-7 pt-3" style={`padding-bottom: ${footerPaddingBottom};`}>
			<button
				type="button"
				onclick={clearAll}
				class="inline-flex min-h-11 w-full min-w-0 items-center justify-center rounded-full border border-neutral-300 px-4 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				<span class="truncate">{t('devices.filters.clearAll')}</span>
			</button>

			<!--
				#128: Apply is the explicit gate that turns `pending` into the
				applied filters — the ONLY place besides Clear All that calls
				onFiltersChange. Always enabled (never `disabled`) so it stays in
				the natural Tab order regardless of pending state; the emphasis
				classes below are the visual (not focus-gating) distinction
				between "pending edits waiting" and "nothing to apply".
			-->
			<button
				type="button"
				onclick={applyFilters}
				class="inline-flex min-h-11 w-full min-w-0 items-center justify-center rounded-full px-4 py-2.5 text-base font-medium text-white transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 {hasPendingChanges
					? 'bg-primary-600 hover:bg-primary-700 dark:bg-primary-500 dark:hover:bg-primary-600'
					: 'bg-neutral-400 hover:bg-neutral-500 dark:bg-neutral-600 dark:hover:bg-neutral-500'}"
			>
				<span class="truncate">{t('devices.filters.apply')}</span>
			</button>
		</div>
	</div>
</div>


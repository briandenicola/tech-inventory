<!--
	TableColumnSettings.svelte — Column visibility + reorder config for the devices table.
	Used in the Settings page. Persists to localStorage via userPrefs.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { addToast } from '$lib/stores/toast';
	import {
		ALL_TABLE_COLUMNS,
		DEFAULT_TABLE_COLUMNS,
		getTableColumns,
		setTableColumns,
		type TableColumnId
	} from '$lib/stores/userPrefs';

	const currentUser = $derived($authStore.currentUser);

	// Map column IDs to their i18n labels
	function columnLabel(id: TableColumnId): string {
		return t(`devices.columns.${id}`);
	}

	// Load saved columns or defaults
	let columns = $state<TableColumnId[]>([...DEFAULT_TABLE_COLUMNS]);
	let enabledSet = $state<Set<TableColumnId>>(new Set(DEFAULT_TABLE_COLUMNS));

	$effect(() => {
		if (!currentUser?.id) return;
		const saved = getTableColumns(currentUser.id);
		if (saved && saved.length > 0) {
			columns = [...saved];
			enabledSet = new Set(saved);
		} else {
			columns = [...DEFAULT_TABLE_COLUMNS];
			enabledSet = new Set(DEFAULT_TABLE_COLUMNS);
		}
	});

	// The full ordered list: enabled columns in their order, then disabled ones
	const orderedColumns = $derived.by(() => {
		const disabled = ALL_TABLE_COLUMNS.filter((c) => !enabledSet.has(c));
		return [...columns, ...disabled];
	});

	function isEnabled(id: TableColumnId): boolean {
		return enabledSet.has(id);
	}

	function toggleColumn(id: TableColumnId) {
		const next = new Set(enabledSet);
		if (next.has(id)) {
			// Don't allow disabling the last column
			if (next.size <= 1) return;
			next.delete(id);
			columns = columns.filter((c) => c !== id);
		} else {
			next.add(id);
			columns = [...columns, id];
		}
		enabledSet = next;
		save();
	}

	function moveUp(id: TableColumnId) {
		const idx = columns.indexOf(id);
		if (idx <= 0) return;
		const next = [...columns];
		[next[idx - 1], next[idx]] = [next[idx], next[idx - 1]];
		columns = next;
		save();
	}

	function moveDown(id: TableColumnId) {
		const idx = columns.indexOf(id);
		if (idx < 0 || idx >= columns.length - 1) return;
		const next = [...columns];
		[next[idx], next[idx + 1]] = [next[idx + 1], next[idx]];
		columns = next;
		save();
	}

	function resetToDefault() {
		columns = [...DEFAULT_TABLE_COLUMNS];
		enabledSet = new Set(DEFAULT_TABLE_COLUMNS);
		setTableColumns(currentUser?.id, null);
		addToast({ type: 'success', message: t('settings.tableColumns.toast.reset') });
	}

	function save() {
		if (!currentUser?.id) return;
		setTableColumns(currentUser.id, columns);
		addToast({ type: 'success', message: t('settings.tableColumns.toast.saved') });
	}
</script>

<section
	class="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	aria-labelledby="table-columns-heading"
>
	<div class="flex items-center justify-between">
		<div>
			<h2
				id="table-columns-heading"
				class="text-lg font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{t('settings.tableColumns.heading')}
			</h2>
			<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
				{t('settings.tableColumns.subheading')}
			</p>
		</div>
		<button
			type="button"
			onclick={resetToDefault}
			class="rounded-md border border-neutral-300 bg-white px-3 py-1.5 text-sm font-medium text-neutral-700 hover:bg-neutral-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
		>
			{t('settings.tableColumns.resetToDefault')}
		</button>
	</div>

	<ul class="mt-4 divide-y divide-neutral-200 dark:divide-neutral-800" role="list">
		{#each orderedColumns as col (col)}
			{@const enabled = isEnabled(col)}
			{@const idx = columns.indexOf(col)}
			<li
				class="flex items-center gap-3 py-3 {enabled ? '' : 'opacity-50'}"
				data-testid="table-column-item"
			>
				<input
					type="checkbox"
					checked={enabled}
					onchange={() => toggleColumn(col)}
					class="h-4 w-4 cursor-pointer rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
					aria-label={columnLabel(col)}
				/>
				<span class="flex-1 text-sm text-neutral-900 dark:text-neutral-100">
					{columnLabel(col)}
				</span>
				{#if enabled}
					<button
						type="button"
						onclick={() => moveUp(col)}
						disabled={idx === 0}
						class="rounded p-1 text-neutral-500 hover:text-neutral-700 disabled:opacity-30 dark:text-neutral-400 dark:hover:text-neutral-200"
						aria-label={t('settings.tableColumns.moveUp')}
					>
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
						</svg>
					</button>
					<button
						type="button"
						onclick={() => moveDown(col)}
						disabled={idx === columns.length - 1}
						class="rounded p-1 text-neutral-500 hover:text-neutral-700 disabled:opacity-30 dark:text-neutral-400 dark:hover:text-neutral-200"
						aria-label={t('settings.tableColumns.moveDown')}
					>
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</button>
				{/if}
			</li>
		{/each}
	</ul>
</section>

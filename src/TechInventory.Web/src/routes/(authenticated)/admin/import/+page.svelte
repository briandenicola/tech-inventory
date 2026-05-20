<!--
	/admin/import — CSV import wizard (T34/T35/T36)

	Three-step state machine: upload -> preview -> committed.

	Step 1 (upload): drag-drop or click-to-select a CSV file. Client-side
	validation (size, type), then client-side preview of the first 10 rows so
	the user can sanity-check column ordering before sending bytes to the
	server.

	Step 2 (preview): POST the file to /api/v1/imports/preview. The server
	returns totalRows / validRows / invalidRows / lookupsToCreate. We render
	a summary card plus tables of any row-level errors and any new lookup
	entities (brands, categories, owners, locations, networks) the commit
	will create.

	Step 3 (committed): POST the same File to /api/v1/imports/commit. Render
	an ImportBatch summary card with a link back to /devices. Toast on
	success.

	The File is held in $state across the wizard so the user does not need
	to re-upload between preview and commit.

	Refs: PRD F1, spec.md J10, D-137 (Apple-elegant), constitution §4.4.
-->
<script lang="ts">
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import { imports } from '$lib/api/client';
	import { addToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import type {
		PreviewImportResult,
		CommitImportResult,
		ImportRowError,
		MissingLookup
	} from '$lib/api/types';

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	$effect(() => {
		if (!isAdmin && currentUser !== null) {
			goto('/devices');
		}
	});

	type Step = 'upload' | 'preview' | 'committed';

	let step = $state<Step>('upload');
	let selectedFile = $state<File | null>(null);
	let clientHeaders = $state<string[]>([]);
	let clientRows = $state<string[][]>([]);
	let previewResult = $state<PreviewImportResult | null>(null);
	let commitResult = $state<CommitImportResult | null>(null);
	let isLoading = $state(false);
	let errorMessage = $state<string | null>(null);
	let isDragging = $state(false);
	let fileInputRef = $state<HTMLInputElement | undefined>(undefined);

	const MAX_FILE_SIZE = 10 * 1024 * 1024;

	function formatBytes(bytes: number): string {
		if (bytes < 1024) return `${bytes} B`;
		if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
		return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
	}

	// Minimal CSV parser — handles quoted fields with embedded commas + escaped quotes
	function parseCsvLine(line: string): string[] {
		const out: string[] = [];
		let cell = '';
		let inQuotes = false;
		for (let i = 0; i < line.length; i++) {
			const ch = line[i];
			if (ch === '"') {
				if (inQuotes && line[i + 1] === '"') {
					cell += '"';
					i++;
				} else {
					inQuotes = !inQuotes;
				}
			} else if (ch === ',' && !inQuotes) {
				out.push(cell);
				cell = '';
			} else {
				cell += ch;
			}
		}
		out.push(cell);
		return out;
	}

	async function handleFileSelected(file: File) {
		errorMessage = null;
		previewResult = null;
		commitResult = null;
		clientHeaders = [];
		clientRows = [];

		if (file.size > MAX_FILE_SIZE) {
			errorMessage = t('import.upload.fileSizeExceeded');
			selectedFile = null;
			return;
		}

		const isCsv =
			file.name.toLowerCase().endsWith('.csv') ||
			file.type === 'text/csv' ||
			file.type === 'application/vnd.ms-excel';
		if (!isCsv) {
			errorMessage = t('import.upload.invalidFileType');
			selectedFile = null;
			return;
		}

		selectedFile = file;

		try {
			const text = await file.text();
			const lines = text.split(/\r?\n/).filter((l) => l.trim().length > 0);
			const parsed = lines.slice(0, 11).map(parseCsvLine);
			if (parsed.length > 0) {
				clientHeaders = parsed[0];
				clientRows = parsed.slice(1);
			}
		} catch (err) {
			console.warn('[admin/import] Client preview failed:', err);
		}
	}

	function handleFileInputChange(e: Event) {
		const target = e.target as HTMLInputElement;
		const file = target.files?.[0];
		if (file) {
			void handleFileSelected(file);
		}
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		isDragging = false;
		const file = e.dataTransfer?.files?.[0];
		if (file) {
			void handleFileSelected(file);
		}
	}

	function handleDragOver(e: DragEvent) {
		e.preventDefault();
		isDragging = true;
	}

	function handleDragLeave(e: DragEvent) {
		e.preventDefault();
		isDragging = false;
	}

	function openFilePicker() {
		fileInputRef?.click();
	}

	function extractErrorMessage(err: unknown, fallback: string): string {
		if (err instanceof Error) {
			const detail =
				'detail' in err
					? (err as unknown as { detail?: string }).detail
					: undefined;
			return detail ?? err.message ?? fallback;
		}
		return fallback;
	}

	async function handlePreview() {
		if (!selectedFile || isLoading) return;
		isLoading = true;
		errorMessage = null;
		try {
			const formData = new FormData();
			formData.append('File', selectedFile);
			previewResult = await imports.preview(formData);
			step = 'preview';
		} catch (err) {
			console.error('[admin/import] Preview failed:', err);
			errorMessage = extractErrorMessage(err, 'Failed to preview import');
			addToast({ type: 'error', message: errorMessage });
		} finally {
			isLoading = false;
		}
	}

	async function handleCommit() {
		if (!selectedFile || isLoading) return;
		isLoading = true;
		errorMessage = null;
		try {
			const formData = new FormData();
			formData.append('File', selectedFile);
			commitResult = await imports.commit(formData);
			invalidateDevicesCache();
			addToast({ type: 'success', message: t('import.commit.success') });
			step = 'committed';
		} catch (err) {
			console.error('[admin/import] Commit failed:', err);
			errorMessage = extractErrorMessage(err, 'Failed to commit import');
			addToast({ type: 'error', message: errorMessage });
		} finally {
			isLoading = false;
		}
	}

	function handleStartOver() {
		selectedFile = null;
		clientHeaders = [];
		clientRows = [];
		previewResult = null;
		commitResult = null;
		errorMessage = null;
		step = 'upload';
		if (fileInputRef) {
			fileInputRef.value = '';
		}
	}

	function formatRowErrors(row: ImportRowError): string {
		if (!row.errors || row.errors.length === 0) return '';
		return row.errors
			.map((e) => `${e.field ?? 'row'}: ${e.message ?? ''}`)
			.join('; ');
	}

	function groupLookups(lookups: MissingLookup[] | null | undefined): Record<string, string[]> {
		const groups: Record<string, string[]> = {};
		if (!lookups) return groups;
		for (const lk of lookups) {
			const key = lk.entityType ?? 'Other';
			(groups[key] ??= []).push(lk.name ?? '');
		}
		return groups;
	}

	const lookupGroups = $derived(groupLookups(previewResult?.lookupsToCreate));
</script>

<svelte:head>
	<title>{t('import.title')} — Tech Inventory</title>
</svelte:head>

<div class="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
	<header class="mb-8">
		<h1 class="text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
			{t('import.title')}
		</h1>
		<p class="mt-2 text-neutral-600 dark:text-neutral-400">
			{t('import.upload.title')} → {t('import.preview.title')} → {t('import.commit.title')}
		</p>
	</header>

	<!-- Step indicator -->
	<ol class="mb-10 flex items-center gap-3" aria-label="Import progress">
		{#each [
			{ key: 'upload', label: t('import.upload.title'), index: 1 },
			{ key: 'preview', label: t('import.preview.title'), index: 2 },
			{ key: 'committed', label: t('import.commit.title'), index: 3 }
		] as s (s.key)}
			{@const isActive = step === s.key}
			{@const isComplete =
				(s.key === 'upload' && step !== 'upload') ||
				(s.key === 'preview' && step === 'committed')}
			<li class="flex flex-1 items-center gap-3">
				<span
					class="inline-flex h-9 w-9 items-center justify-center rounded-full text-sm font-semibold transition-colors {isActive
						? 'bg-primary-600 text-white'
						: isComplete
							? 'bg-success-600 text-white'
							: 'bg-neutral-200 text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400'}"
					aria-current={isActive ? 'step' : undefined}
				>
					{isComplete ? '✓' : s.index}
				</span>
				<span
					class="text-sm font-medium {isActive
						? 'text-neutral-900 dark:text-neutral-50'
						: 'text-neutral-600 dark:text-neutral-400'}"
				>
					{s.label}
				</span>
				{#if s.index < 3}
					<span class="h-px flex-1 bg-neutral-200 dark:bg-neutral-800" aria-hidden="true"></span>
				{/if}
			</li>
		{/each}
	</ol>

	{#if step === 'upload'}
		<!-- Step 1: Upload -->
		<section
			class="rounded-2xl border border-neutral-200 bg-white p-8 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
		>
			{#if !selectedFile}
				<button
					type="button"
					ondrop={handleDrop}
					ondragover={handleDragOver}
					ondragleave={handleDragLeave}
					onclick={openFilePicker}
					class="flex min-h-64 w-full flex-col items-center justify-center rounded-xl border-2 border-dashed p-8 text-center transition-colors {isDragging
						? 'border-primary-500 bg-primary-50 dark:bg-primary-950/30'
						: 'border-neutral-300 hover:border-primary-400 dark:border-neutral-700 dark:hover:border-primary-500'}"
				>
					<svg
						class="mb-4 h-12 w-12 text-neutral-400"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						aria-hidden="true"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="1.5"
							d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3"
						/>
					</svg>
					<p class="text-lg font-medium text-neutral-900 dark:text-neutral-100">
						{t('import.upload.dragDropPrompt')}
					</p>
					<p class="mt-2 text-sm text-neutral-500 dark:text-neutral-400">
						CSV files only · max 10 MB
					</p>
				</button>
				<input
					bind:this={fileInputRef}
					type="file"
					accept=".csv,text/csv,application/vnd.ms-excel"
					class="sr-only"
					onchange={handleFileInputChange}
				/>
			{:else}
				<!-- Selected file + client-side preview -->
				<div class="flex items-start justify-between gap-4">
					<div>
						<p class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
							{selectedFile.name}
						</p>
						<p class="text-sm text-neutral-600 dark:text-neutral-400">
							{formatBytes(selectedFile.size)}
							{#if clientRows.length > 0}· {clientRows.length} row{clientRows.length === 1
									? ''
									: 's'} previewed{/if}
						</p>
					</div>
					<button
						type="button"
						onclick={handleStartOver}
						class="inline-flex min-h-11 items-center rounded-full px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
					>
						{t('common.actions.cancel')}
					</button>
				</div>

				{#if clientRows.length > 0}
					<div class="mt-6 overflow-x-auto rounded-xl border border-neutral-200 dark:border-neutral-800">
						<table class="w-full text-left text-sm">
							<thead
								class="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:bg-neutral-950 dark:text-neutral-400"
							>
								<tr>
									{#each clientHeaders as header, i (i)}
										<th class="px-3 py-2">{header}</th>
									{/each}
								</tr>
							</thead>
							<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
								{#each clientRows as row, ri (ri)}
									<tr>
										{#each row as cell, ci (ci)}
											<td class="px-3 py-2 text-neutral-700 dark:text-neutral-300">{cell}</td>
										{/each}
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}

				<div class="mt-6 flex justify-end gap-3 border-t border-neutral-200 pt-6 dark:border-neutral-800">
					<button
						type="button"
						onclick={handlePreview}
						disabled={isLoading}
						class="inline-flex min-h-11 items-center gap-2 rounded-full bg-primary-600 px-6 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-primary-500 dark:hover:bg-primary-600"
					>
						{#if isLoading}
							<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
								<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
								<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
							</svg>
							{t('common.loading')}
						{:else}
							{t('common.actions.next')}
						{/if}
					</button>
				</div>
			{/if}

			{#if errorMessage}
				<div
					class="mt-4 rounded-xl border border-danger-300 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-800 dark:bg-danger-950/40 dark:text-danger-200"
					role="alert"
				>
					{errorMessage}
				</div>
			{/if}
		</section>
	{:else if step === 'preview' && previewResult}
		<!-- Step 2: Preview validation result -->
		<section class="space-y-6">
			<!-- Summary cards -->
			<div class="grid gap-4 sm:grid-cols-3">
				<div
					class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
				>
					<p class="text-sm text-neutral-600 dark:text-neutral-400">Total rows</p>
					<p class="mt-1 text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
						{previewResult.totalRows}
					</p>
				</div>
				<div
					class="rounded-2xl border border-success-200 bg-success-50/40 p-5 shadow-sm dark:border-success-800 dark:bg-success-950/30"
				>
					<p class="text-sm text-success-800 dark:text-success-300">
						{t('import.preview.validRows', { count: String(previewResult.validRows?.length ?? 0) })}
					</p>
					<p class="mt-1 text-3xl font-semibold tracking-tight text-success-900 dark:text-success-100">
						{previewResult.validRows?.length ?? 0}
					</p>
				</div>
				<div
					class="rounded-2xl border border-danger-200 bg-danger-50/40 p-5 shadow-sm dark:border-danger-800 dark:bg-danger-950/30"
				>
					<p class="text-sm text-danger-800 dark:text-danger-300">
						{t('import.preview.invalidRows', { count: String(previewResult.invalidRows?.length ?? 0) })}
					</p>
					<p class="mt-1 text-3xl font-semibold tracking-tight text-danger-900 dark:text-danger-100">
						{previewResult.invalidRows?.length ?? 0}
					</p>
				</div>
			</div>

			<!--
				F034: per-row preview table so admins can confirm Model / Purpose /
				Notes (and every other field) landed in the right column BEFORE
				committing. Collapsed by default since most imports don't need
				row-level inspection; expanded reveals up to the first 10 rows.
			-->
			{#if previewResult.validRows && previewResult.validRows.length > 0}
				<details
					class="rounded-2xl border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
					data-testid="import-preview-rows"
				>
					<summary
						class="cursor-pointer rounded-2xl px-6 py-4 text-base font-semibold text-neutral-900 hover:bg-neutral-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:text-neutral-50 dark:hover:bg-neutral-800/40"
					>
						{t('import.preview.previewRowsTitle')}
					</summary>
					<div class="px-6 pb-6">
						<p class="mb-3 text-sm text-neutral-600 dark:text-neutral-400">
							{t('import.preview.previewRowsHint', {
								count: String(Math.min(previewResult.validRows.length, 10))
							})}
						</p>
						<div class="overflow-x-auto">
							<table class="min-w-full divide-y divide-neutral-200 text-sm dark:divide-neutral-800">
								<thead class="bg-neutral-50 text-left text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:bg-neutral-900 dark:text-neutral-400">
									<tr>
										<th scope="col" class="px-3 py-2">{t('devices.columns.name')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.brand')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.category')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.model')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.purpose')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.notes')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.owner')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.location')}</th>
										<th scope="col" class="px-3 py-2">{t('devices.columns.status')}</th>
									</tr>
								</thead>
								<tbody class="divide-y divide-neutral-100 dark:divide-neutral-800">
									{#each previewResult.validRows.slice(0, 10) as row, idx (row.rowNumber ?? idx)}
										{@const dev = row.device}
										<tr class="text-neutral-800 dark:text-neutral-200">
											<td class="px-3 py-2 font-medium">{dev?.name ?? '—'}</td>
											<td class="px-3 py-2">{dev?.brand ?? '—'}</td>
											<td class="px-3 py-2">{dev?.category ?? '—'}</td>
											<td class="px-3 py-2">{dev?.model ?? '—'}</td>
											<td class="px-3 py-2 max-w-xs truncate" title={dev?.purpose ?? ''}>
												{dev?.purpose ?? '—'}
											</td>
											<td class="px-3 py-2 max-w-xs truncate" title={dev?.notes ?? ''}>
												{dev?.notes ?? '—'}
											</td>
											<td class="px-3 py-2">{dev?.owner ?? '—'}</td>
											<td class="px-3 py-2">{dev?.location ?? '—'}</td>
											<td class="px-3 py-2">{dev?.status ?? '—'}</td>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					</div>
				</details>
			{/if}

			<!-- Missing lookups -->
			{#if previewResult.lookupsToCreate && previewResult.lookupsToCreate.length > 0}
				<div
					class="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
				>
					<h2 class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
						{t('import.preview.missingLookups')}
					</h2>
					<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
						These reference entities will be created when you commit.
					</p>
					<div class="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
						{#each Object.entries(lookupGroups) as [entityType, names] (entityType)}
							<div>
								<h3 class="text-sm font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
									{entityType}
								</h3>
								<ul class="mt-2 space-y-1">
									{#each names as name (name)}
										<li class="rounded-lg bg-neutral-50 px-3 py-1.5 text-sm text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200">
											{name}
										</li>
									{/each}
								</ul>
							</div>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Invalid rows -->
			{#if previewResult.invalidRows && previewResult.invalidRows.length > 0}
				<div
					class="rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
				>
					<h2 class="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
						{t('import.preview.validationErrors')}
					</h2>
					<div class="mt-4 overflow-x-auto rounded-xl border border-neutral-200 dark:border-neutral-800">
						<table class="w-full text-left text-sm">
							<thead
								class="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:bg-neutral-950 dark:text-neutral-400"
							>
								<tr>
									<th class="px-3 py-2">Row</th>
									<th class="px-3 py-2">Errors</th>
								</tr>
							</thead>
							<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
								{#each previewResult.invalidRows as row (row.rowNumber)}
									<tr>
										<td class="px-3 py-2 font-mono text-neutral-700 dark:text-neutral-300">
											{row.rowNumber}
										</td>
										<td class="px-3 py-2 text-danger-700 dark:text-danger-300">
											{formatRowErrors(row)}
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>
			{/if}

			<!-- Actions -->
			<div class="flex flex-col-reverse justify-end gap-3 sm:flex-row">
				<button
					type="button"
					onclick={handleStartOver}
					class="inline-flex min-h-11 items-center justify-center rounded-full px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					{t('common.actions.cancel')}
				</button>
				<button
					type="button"
					onclick={handleCommit}
					disabled={isLoading || (previewResult.validRows?.length ?? 0) === 0}
					class="inline-flex min-h-11 items-center gap-2 rounded-full bg-primary-600 px-6 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-primary-500 dark:hover:bg-primary-600"
				>
					{#if isLoading}
						<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
						</svg>
						{t('common.loading')}
					{:else}
						{t('import.commit.title')}
					{/if}
				</button>
			</div>

			{#if errorMessage}
				<div
					class="rounded-xl border border-danger-300 bg-danger-50 p-4 text-sm text-danger-800 dark:border-danger-800 dark:bg-danger-950/40 dark:text-danger-200"
					role="alert"
				>
					{errorMessage}
				</div>
			{/if}
		</section>
	{:else if step === 'committed' && commitResult}
		<!-- Step 3: Summary -->
		<section
			class="rounded-2xl border border-success-200 bg-white p-8 text-center shadow-sm dark:border-success-800 dark:bg-neutral-900"
		>
			<div class="mx-auto mb-4 inline-flex h-16 w-16 items-center justify-center rounded-full bg-success-100 text-success-700 dark:bg-success-900/40 dark:text-success-300">
				<svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
				</svg>
			</div>
			<h2 class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
				{t('import.commit.success')}
			</h2>
			<p class="mt-2 text-neutral-600 dark:text-neutral-400">
				{t('import.commit.summary', {
					processed: String(commitResult.importedRows ?? 0),
					errors: String(commitResult.invalidRows ?? 0)
				})}
			</p>

			<dl class="mx-auto mt-6 grid max-w-md grid-cols-3 gap-4 text-sm">
				<div>
					<dt class="text-neutral-500 dark:text-neutral-400">Total</dt>
					<dd class="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">
						{commitResult.totalRows ?? 0}
					</dd>
				</div>
				<div>
					<dt class="text-success-700 dark:text-success-400">Imported</dt>
					<dd class="text-2xl font-semibold text-success-700 dark:text-success-400">
						{commitResult.importedRows ?? 0}
					</dd>
				</div>
				<div>
					<dt class="text-danger-700 dark:text-danger-400">Invalid</dt>
					<dd class="text-2xl font-semibold text-danger-700 dark:text-danger-400">
						{commitResult.invalidRows ?? 0}
					</dd>
				</div>
			</dl>

			<div class="mt-8 flex flex-col-reverse justify-center gap-3 sm:flex-row">
				<button
					type="button"
					onclick={handleStartOver}
					class="inline-flex min-h-11 items-center justify-center rounded-full px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
				>
					Import another file
				</button>
				<a
					href="/devices"
					class="inline-flex min-h-11 items-center justify-center rounded-full bg-primary-600 px-6 py-2.5 text-base font-medium text-white transition-colors hover:bg-primary-700 dark:bg-primary-500 dark:hover:bg-primary-600"
				>
					View devices
				</a>
			</div>
		</section>
	{/if}
</div>

<script lang="ts">
	/**
	 * F021 v1 — Admin Audit Log viewer.
	 *
	 * Mirrors the URL-backed filter pattern from /admin/brands. All filter
	 * state lives in the querystring so reload / share / deep-link from the
	 * device modal ("View change history") all just work.
	 *
	 * Filters:
	 *  - entityType (string, free text — matches backend enum strings)
	 *  - entityId   (guid, free text)
	 *  - action     (Created | Updated | Deleted)
	 *  - actor      (string, free text)
	 *  - from / to  (date inputs)
	 *  - page / pageSize
	 *
	 * Backend enforces Admin via [Authorize(Policy = Admin)] — this page
	 * mirrors with a client-side redirect for non-admins (belt-and-suspenders,
	 * same pattern as /admin/brands).
	 */
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { authStore } from '$lib/stores/auth';
	import api from '$lib/api/client';
	import type { components } from '$lib/api/generated/types';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import AuditDiffDrawer from '$lib/components/admin/AuditDiffDrawer.svelte';

	type AuditEvent = components['schemas']['AuditEventResponse'];
	type AuditAction = components['schemas']['AuditAction'];

	const currentUser = $derived($authStore.currentUser);
	const isAdmin = $derived(currentUser?.role === 'Admin');

	// Belt-and-suspenders: backend enforces Admin policy, but redirect any
	// non-admin who reaches this route directly.
	$effect(() => {
		if (currentUser !== null && !isAdmin) {
			goto('/devices');
		}
	});

	const urlFilters = $derived.by(() => {
		const params = $page.url.searchParams;
		const actionParam = params.get('action');
		const isAction = (s: string | null): s is AuditAction =>
			s === 'Created' || s === 'Updated' || s === 'Deleted';
		return {
			page: parseInt(params.get('page') ?? '1', 10),
			pageSize: parseInt(params.get('pageSize') ?? '25', 10),
			entityType: params.get('entityType') ?? '',
			entityId: params.get('entityId') ?? '',
			action: isAction(actionParam) ? actionParam : ('' as '' | AuditAction),
			actor: params.get('actor') ?? '',
			from: params.get('from') ?? '',
			to: params.get('to') ?? ''
		};
	});

	let events = $state<AuditEvent[]>([]);
	let totalCount = $state(0);
	let loading = $state(true);
	let error = $state<string | null>(null);

	// Local filter form state — separate from URL so the user can edit
	// freely before clicking Apply.
	let filterForm = $state({
		entityType: '',
		entityId: '',
		action: '' as '' | AuditAction,
		actor: '',
		from: '',
		to: ''
	});

	// Sync form ← URL whenever it changes (mount, back/forward, deep link).
	$effect(() => {
		filterForm.entityType = urlFilters.entityType;
		filterForm.entityId = urlFilters.entityId;
		filterForm.action = urlFilters.action;
		filterForm.actor = urlFilters.actor;
		filterForm.from = urlFilters.from;
		filterForm.to = urlFilters.to;
	});

	let selectedEvent = $state<AuditEvent | null>(null);

	// Brian's PWA feedback: the inline filter form pushed the actual log rows
	// off-screen and made the page feel "filter-first". Hide the form behind
	// a toggle so the focus is the audit table; surface the active-filter
	// count on the toggle so users still know whether a filter is on.
	let filtersOpen = $state(false);
	const activeFilterCount = $derived(
		(urlFilters.entityType ? 1 : 0) +
			(urlFilters.entityId ? 1 : 0) +
			(urlFilters.action ? 1 : 0) +
			(urlFilters.actor ? 1 : 0) +
			(urlFilters.from ? 1 : 0) +
			(urlFilters.to ? 1 : 0)
	);

	$effect(() => {
		void urlFilters;
		void loadEvents();
	});

	async function loadEvents() {
		loading = true;
		error = null;
		try {
			const response = await api.auditEvents.list({
				page: urlFilters.page,
				pageSize: urlFilters.pageSize,
				entityType: urlFilters.entityType || undefined,
				entityId: urlFilters.entityId || undefined,
				action: urlFilters.action || undefined,
				actor: urlFilters.actor || undefined,
				from: urlFilters.from || undefined,
				to: urlFilters.to || undefined
			});
			events = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[AuditLog] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load audit events';
		} finally {
			loading = false;
		}
	}

	function applyFilters(e: Event) {
		e.preventDefault();
		const params = new URLSearchParams();
		if (filterForm.entityType) params.set('entityType', filterForm.entityType);
		if (filterForm.entityId) params.set('entityId', filterForm.entityId);
		if (filterForm.action) params.set('action', filterForm.action);
		if (filterForm.actor) params.set('actor', filterForm.actor);
		if (filterForm.from) params.set('from', filterForm.from);
		if (filterForm.to) params.set('to', filterForm.to);
		// Reset to page 1 on any filter change
		const qs = params.toString();
		goto(qs ? `?${qs}` : '?', { replaceState: true, keepFocus: true, noScroll: true });
		// Auto-collapse after apply so the table comes back into focus.
		filtersOpen = false;
	}

	function clearFilters() {
		filterForm = {
			entityType: '',
			entityId: '',
			action: '',
			actor: '',
			from: '',
			to: ''
		};
		goto('?', { replaceState: true, keepFocus: true, noScroll: true });
		filtersOpen = false;
	}

	function handlePageChange(newPage: number, newPageSize: number) {
		const params = new URLSearchParams($page.url.searchParams);
		if (newPage !== 1) params.set('page', newPage.toString());
		else params.delete('page');
		if (newPageSize !== 25) params.set('pageSize', newPageSize.toString());
		else params.delete('pageSize');
		goto(`?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
	}

	function formatTimestamp(ts: string | null | undefined): string {
		if (!ts) return '—';
		return new Date(ts).toLocaleString();
	}

	function summarize(event: AuditEvent): string {
		// Tiny inline summary for the table — full diff lives in the drawer.
		if (!event.beforePayload && event.afterPayload) {
			return t('admin.audit.summary.created');
		}
		if (event.beforePayload && !event.afterPayload) {
			return t('admin.audit.summary.deleted');
		}
		return t('admin.audit.summary.updated');
	}

	function openDetail(event: AuditEvent) {
		selectedEvent = event;
	}

	function closeDetail() {
		selectedEvent = null;
	}

	function actionClass(action: string | null | undefined): string {
		switch (action) {
			case 'Created':
				return 'bg-success-100 text-success-800 dark:bg-success-900 dark:text-success-100';
			case 'Updated':
				return 'bg-warning-100 text-warning-800 dark:bg-warning-900 dark:text-warning-100';
			case 'Deleted':
				return 'bg-danger-100 text-danger-800 dark:bg-danger-900 dark:text-danger-100';
			default:
				return 'bg-neutral-100 text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300';
		}
	}
</script>

<svelte:head>
	<title>{t('admin.audit.list.title')} — {t('app.title')}</title>
</svelte:head>

<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
	<div class="mb-6 flex items-start justify-between gap-3">
		<div class="min-w-0">
			<h1 class="text-2xl font-bold text-neutral-900 dark:text-neutral-50">
				{t('admin.audit.list.title')}
			</h1>
			<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
				{t('admin.audit.list.subtitle')}
			</p>
		</div>
		<!--
			Filters toggle. Default-collapsed so the audit rows are the first
			thing the user sees on mobile. Active-count badge gives a quick
			read on whether the table is filtered.
		-->
		<button
			type="button"
			onclick={() => (filtersOpen = !filtersOpen)}
			aria-expanded={filtersOpen}
			aria-controls="audit-filters-panel"
			class="inline-flex min-h-11 shrink-0 items-center gap-2 rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200 dark:hover:bg-neutral-800"
		>
			<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
				/>
			</svg>
			{t('admin.audit.filters.toggle')}
			{#if activeFilterCount > 0}
				<span
					class="inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-primary-600 px-1.5 text-xs font-semibold text-white"
					aria-label={t('admin.audit.filters.activeCount', { count: activeFilterCount })}
				>
					{activeFilterCount}
				</span>
			{/if}
		</button>
	</div>

	<!-- Filters (collapsed by default; toggled by the chip above) -->
	{#if filtersOpen}
		<form
			id="audit-filters-panel"
			onsubmit={applyFilters}
			class="mb-6 rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
		>
		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.entityType')}
				</span>
				<input
					type="text"
					bind:value={filterForm.entityType}
					placeholder="Device, Brand, Category..."
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</label>
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.entityId')}
				</span>
				<input
					type="text"
					bind:value={filterForm.entityId}
					placeholder="00000000-…"
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 font-mono text-xs shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</label>
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.action')}
				</span>
				<select
					bind:value={filterForm.action}
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				>
					<option value="">{t('admin.audit.filters.actionAny')}</option>
					<option value="Created">Created</option>
					<option value="Updated">Updated</option>
					<option value="Deleted">Deleted</option>
				</select>
			</label>
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.actor')}
				</span>
				<input
					type="text"
					bind:value={filterForm.actor}
					placeholder="User oid or display name"
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</label>
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.from')}
				</span>
				<input
					type="datetime-local"
					bind:value={filterForm.from}
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</label>
			<label class="block">
				<span class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
					{t('admin.audit.filters.to')}
				</span>
				<input
					type="datetime-local"
					bind:value={filterForm.to}
					class="mt-1 block w-full rounded-md border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100"
				/>
			</label>
		</div>
		<div class="mt-4 flex items-center justify-end gap-3">
			<button
				type="button"
				onclick={clearFilters}
				class="rounded-md border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 hover:bg-neutral-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('common.actions.clear')}
			</button>
			<button
				type="submit"
				class="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
			>
				{t('admin.audit.filters.apply')}
			</button>
		</div>
	</form>
	{/if}

	{#if loading}
		<LoadingSkeleton />
	{:else if error}
		<ErrorState {error} onRetry={loadEvents} />
	{:else if events.length === 0}
		<div
			class="flex min-h-[300px] flex-col items-center justify-center rounded-lg border border-neutral-200 bg-white p-12 text-center dark:border-neutral-800 dark:bg-neutral-950"
		>
			<p class="mt-4 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
				{t('admin.audit.list.emptyState')}
			</p>
		</div>
	{:else}
		<div
			class="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow dark:border-neutral-800 dark:bg-neutral-950"
		>
			<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
				<thead class="bg-neutral-50 dark:bg-neutral-900">
					<tr>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('admin.audit.columns.timestamp')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('admin.audit.columns.actor')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('admin.audit.columns.entityType')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('admin.audit.columns.action')}
						</th>
						<th
							scope="col"
							class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300"
						>
							{t('admin.audit.columns.summary')}
						</th>
					</tr>
				</thead>
				<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
					{#each events as event (event.id)}
						<tr
							class="cursor-pointer transition-colors hover:bg-neutral-50 dark:hover:bg-neutral-900"
							onclick={() => openDetail(event)}
						>
							<td
								class="whitespace-nowrap px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100"
							>
								{formatTimestamp(event.timestamp)}
							</td>
							<td
								class="px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100"
							>
								{event.actor ?? '—'}
							</td>
							<td
								class="whitespace-nowrap px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100"
							>
								{event.entityType ?? '—'}
							</td>
							<td class="whitespace-nowrap px-4 py-3 text-sm">
								<span
									class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {actionClass(
										event.action
									)}"
								>
									{event.action ?? '—'}
								</span>
							</td>
							<td class="px-4 py-3 text-sm text-neutral-600 dark:text-neutral-400">
								{summarize(event)}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>

			<PaginationControls
				currentPage={urlFilters.page}
				pageSize={urlFilters.pageSize}
				{totalCount}
				onPageChange={handlePageChange}
			/>
		</div>
	{/if}
</div>

<AuditDiffDrawer event={selectedEvent} onClose={closeDetail} />

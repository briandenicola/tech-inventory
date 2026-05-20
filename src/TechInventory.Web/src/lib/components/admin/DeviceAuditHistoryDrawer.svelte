<!--
	DeviceAuditHistoryDrawer.svelte — F026.

	Right-side drawer that lists every AuditEvent scoped to a single device
	(entityType=Device, entityId={deviceId}) and lets the user expand any row
	into the existing AuditDiffDrawer for a full before/after diff.

	Brian's PWA field test (2026-05-19) flagged that "View change history" on
	the device detail navigated away to /admin/audit and lost the device
	context entirely. This component keeps the user in the device detail modal
	and only offers a focused, per-device view; the global /admin/audit page
	is still available for cross-entity browsing.

	Props:
	  - deviceId: string — entity to filter audit events by (loaded on mount).
	  - onClose:  () => void — Escape, X, backdrop click.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import api from '$lib/api/client';
	import type { components } from '$lib/api/generated/types';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import AuditDiffDrawer from '$lib/components/admin/AuditDiffDrawer.svelte';

	type AuditEvent = components['schemas']['AuditEventResponse'];

	interface Props {
		deviceId: string;
		onClose: () => void;
	}

	let { deviceId, onClose }: Props = $props();

	let events = $state<AuditEvent[]>([]);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let selectedEvent = $state<AuditEvent | null>(null);

	let dialogElement = $state<HTMLDivElement | undefined>(undefined);

	async function loadEvents() {
		loading = true;
		error = null;
		try {
			const response = await api.auditEvents.list({
				entityType: 'Device',
				entityId: deviceId,
				page: 1,
				pageSize: 50
			});
			events = response.items ?? [];
		} catch (err: unknown) {
			console.error('[DeviceAuditHistoryDrawer] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load audit events';
		} finally {
			loading = false;
		}
	}

	$effect(() => {
		void deviceId;
		void loadEvents();
	});

	$effect(() => {
		const previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';
		if (dialogElement) dialogElement.focus();
		return () => {
			document.body.style.overflow = previousOverflow;
		};
	});

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && !selectedEvent) onClose();
	}

	function formatTimestamp(ts: string | null | undefined): string {
		if (!ts) return '—';
		return new Date(ts).toLocaleString();
	}

	function summarize(event: AuditEvent): string {
		if (!event.beforePayload && event.afterPayload) return t('admin.audit.summary.created');
		if (event.beforePayload && !event.afterPayload) return t('admin.audit.summary.deleted');
		return t('admin.audit.summary.updated');
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

<svelte:window onkeydown={handleKeydown} />

<!-- Backdrop -->
<div
	class="fixed inset-0 z-40 bg-neutral-900/50 backdrop-blur-sm"
	onclick={onClose}
	role="presentation"
></div>

<!-- Drawer -->
<div
	bind:this={dialogElement}
	role="dialog"
	aria-modal="true"
	aria-labelledby="device-audit-history-title"
	tabindex="-1"
	class="fixed inset-y-0 right-0 z-50 flex w-full max-w-xl flex-col overflow-hidden border-l border-neutral-200 bg-white shadow-xl outline-none dark:border-neutral-800 dark:bg-neutral-950"
>
	<div class="flex items-start justify-between gap-3 border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
		<div>
			<h2
				id="device-audit-history-title"
				class="text-lg font-semibold text-neutral-900 dark:text-neutral-50"
			>
				{t('devices.history.title')}
			</h2>
			<p class="mt-0.5 text-sm text-neutral-600 dark:text-neutral-400">
				{t('devices.history.subtitle')}
			</p>
		</div>
		<button
			type="button"
			onclick={onClose}
			class="inline-flex h-10 w-10 items-center justify-center rounded-full text-neutral-600 hover:bg-neutral-100 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-300 dark:hover:bg-neutral-800"
			aria-label={t('common.actions.close')}
		>
			<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
			</svg>
		</button>
	</div>

	<div class="flex-1 overflow-y-auto px-6 py-4">
		{#if loading}
			<LoadingSkeleton rows={5} />
		{:else if error}
			<ErrorState {error} onRetry={loadEvents} />
		{:else if events.length === 0}
			<div class="rounded-lg border border-neutral-200 bg-neutral-50 p-6 text-center text-sm text-neutral-600 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-400">
				{t('devices.history.empty')}
			</div>
		{:else}
			<ol class="space-y-2">
				{#each events as event (event.id)}
					<li>
						<button
							type="button"
							onclick={() => (selectedEvent = event)}
							class="flex w-full flex-col gap-1 rounded-lg border border-neutral-200 bg-white px-4 py-3 text-left transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-800 dark:bg-neutral-950 dark:hover:bg-neutral-900"
						>
							<div class="flex items-center justify-between gap-2">
								<time
									datetime={event.timestamp ?? ''}
									class="text-sm font-medium text-neutral-900 dark:text-neutral-100"
								>
									{formatTimestamp(event.timestamp)}
								</time>
								<span
									class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {actionClass(event.action)}"
								>
									{event.action ?? '—'}
								</span>
							</div>
							<div class="text-sm text-neutral-600 dark:text-neutral-400">
								{summarize(event)}
							</div>
							{#if event.actor}
								<div class="text-xs text-neutral-500 dark:text-neutral-500">
									{t('admin.audit.columns.actor')}: {event.actor}
								</div>
							{/if}
						</button>
					</li>
				{/each}
			</ol>
		{/if}
	</div>
</div>

<AuditDiffDrawer event={selectedEvent} onClose={() => (selectedEvent = null)} />

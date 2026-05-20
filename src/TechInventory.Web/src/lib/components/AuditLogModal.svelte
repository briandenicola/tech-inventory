<script lang="ts">
	import api from '$lib/api/client';
	import type { components } from '$lib/api/generated/types';
	import { t } from '$lib/i18n';
	import DeviceAuditTrail from '$lib/components/DeviceAuditTrail.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import PaginationControls from '$lib/components/PaginationControls.svelte';
	import AuditDiffDrawer from '$lib/components/admin/AuditDiffDrawer.svelte';

	type AuditEvent = components['schemas']['AuditEventResponse'];

	interface AuditSummary {
		createdAt: string | null;
		createdBy?: string | null;
		modifiedAt: string | null;
		modifiedBy?: string | null;
	}

	interface Props {
		isOpen: boolean;
		onClose: () => void;
		entityId?: string;
		entityType?: string;
		auditSummary?: AuditSummary;
	}

	let {
		isOpen,
		onClose,
		entityId = undefined,
		entityType = undefined,
		auditSummary = undefined
	}: Props = $props();

	const entityTypeOptions = ['Device', 'Brand', 'Category', 'Owner', 'Location', 'Network', 'Tag'];

	let dialogElement = $state<HTMLDialogElement | undefined>(undefined);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let events = $state<AuditEvent[]>([]);
	let totalCount = $state(0);
	let selectedEvent = $state<AuditEvent | null>(null);
	let currentPage = $state(1);
	let pageSize = $state(25);
	let appliedEntityType = $state('');
	let entityTypeDraft = $state('');

	const isDeviceScoped = $derived(entityType === 'Device' && !!entityId);
	const showEntityTypeFilter = $derived(!entityId);
	const effectiveEntityType = $derived(entityId ? entityType : appliedEntityType || undefined);
	const modalTitle = $derived(
		isDeviceScoped ? t('devices.history.title') : t('admin.audit.list.title')
	);
	const modalSubtitle = $derived(
		isDeviceScoped ? t('devices.history.subtitle') : t('admin.audit.modal.subtitle')
	);
	const emptyMessage = $derived(
		isDeviceScoped ? t('devices.history.empty') : t('admin.audit.list.emptyState')
	);

	$effect(() => {
		const nextEntityType = entityType ?? '';
		if (nextEntityType) {
			appliedEntityType = nextEntityType;
			entityTypeDraft = nextEntityType;
		}
	});

	$effect(() => {
		if (!dialogElement) {
			return;
		}

		if (isOpen) {
			if (!dialogElement.open) {
				dialogElement.showModal();
			}
		} else if (dialogElement.open) {
			dialogElement.close();
		}
	});

	$effect(() => {
		if (!isOpen) {
			return;
		}

		void currentPage;
		void pageSize;
		void effectiveEntityType;
		void entityId;
		void loadEvents();
	});

	$effect(() => {
		if (!isOpen || !dialogElement) {
			return;
		}

		const focusableElements = Array.from(
			dialogElement.querySelectorAll<HTMLElement>(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			)
		).filter((element) => !element.hasAttribute('disabled'));

		const firstElement = focusableElements[0];
		const lastElement = focusableElements[focusableElements.length - 1];

		function trapFocus(event: KeyboardEvent) {
			if (event.key !== 'Tab' || !firstElement || !lastElement) {
				return;
			}

			if (event.shiftKey && document.activeElement === firstElement) {
				event.preventDefault();
				lastElement.focus();
				return;
			}

			if (!event.shiftKey && document.activeElement === lastElement) {
				event.preventDefault();
				firstElement.focus();
			}
		}

		dialogElement.addEventListener('keydown', trapFocus);
		firstElement?.focus();

		return () => {
			dialogElement?.removeEventListener('keydown', trapFocus);
		};
	});

	async function loadEvents() {
		loading = true;
		error = null;

		try {
			const response = await api.auditEvents.list({
				page: currentPage,
				pageSize,
				entityType: effectiveEntityType,
				entityId
			});
			events = response.items ?? [];
			totalCount = response.totalCount ?? 0;
		} catch (err: unknown) {
			console.error('[AuditLogModal] Load failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load audit events';
		} finally {
			loading = false;
		}
	}

	function handleCancel(event: Event) {
		event.preventDefault();
		closeModal();
	}

	function handleDialogClick(event: MouseEvent) {
		if (event.target === dialogElement) {
			closeModal();
		}
	}

	function closeModal() {
		if (selectedEvent) {
			selectedEvent = null;
			return;
		}

		onClose();
	}

	function openDetail(event: AuditEvent) {
		selectedEvent = event;
	}

	function applyEntityTypeFilter(submitEvent: Event) {
		submitEvent.preventDefault();
		appliedEntityType = entityTypeDraft.trim();
		currentPage = 1;
	}

	function clearEntityTypeFilter() {
		entityTypeDraft = '';
		appliedEntityType = '';
		currentPage = 1;
	}

	function handlePageChange(nextPage: number, nextPageSize: number) {
		currentPage = nextPage;
		pageSize = nextPageSize;
	}

	function formatTimestamp(timestamp: string | null | undefined): string {
		if (!timestamp) {
			return '—';
		}

		return new Date(timestamp).toLocaleString();
	}

	function summarize(event: AuditEvent): string {
		if (!event.beforePayload && event.afterPayload) {
			return t('admin.audit.summary.created');
		}

		if (event.beforePayload && !event.afterPayload) {
			return t('admin.audit.summary.deleted');
		}

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

<dialog
	bind:this={dialogElement}
	class="audit-log-modal w-full max-w-5xl overflow-hidden rounded-2xl border border-neutral-200 bg-white p-0 text-left shadow-2xl dark:border-neutral-800 dark:bg-neutral-950"
	oncancel={handleCancel}
	onclick={handleDialogClick}
	aria-labelledby="audit-log-modal-title"
	aria-modal="true"
>
	<div class="flex max-h-[85vh] flex-col">
		<div class="flex items-start justify-between gap-4 border-b border-neutral-200 px-6 py-5 dark:border-neutral-800">
			<div class="min-w-0">
				<h2 id="audit-log-modal-title" class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
					{modalTitle}
				</h2>
				<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
					{modalSubtitle}
				</p>
			</div>
			<button
				type="button"
				onclick={closeModal}
				class="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
				aria-label={t('common.actions.close')}
			>
				<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
				</svg>
			</button>
		</div>

		<div class="flex-1 overflow-y-auto px-6 py-5">
			{#if showEntityTypeFilter}
				<form onsubmit={applyEntityTypeFilter} class="mb-5 flex flex-col gap-3 rounded-2xl border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-800 dark:bg-neutral-900/60 sm:flex-row sm:items-end">
					<div class="min-w-0 flex-1">
						<label for="audit-entity-type-filter" class="block text-sm font-medium text-neutral-700 dark:text-neutral-300">
							{t('admin.audit.filters.entityType')}
						</label>
						<input
							id="audit-entity-type-filter"
							type="text"
							list="audit-entity-type-options"
							bind:value={entityTypeDraft}
							placeholder={t('admin.audit.filters.entityTypePlaceholder')}
							class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm transition-colors focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100"
						/>
						<datalist id="audit-entity-type-options">
							{#each entityTypeOptions as option}
								<option value={option}></option>
							{/each}
						</datalist>
					</div>
					<div class="flex items-center gap-2 sm:justify-end">
						<button
							type="button"
							onclick={clearEntityTypeFilter}
							class="inline-flex min-h-11 items-center rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-300 dark:hover:bg-neutral-900"
						>
							{t('common.actions.clear')}
						</button>
						<button
							type="submit"
							class="inline-flex min-h-11 items-center rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
						>
							{t('admin.audit.filters.apply')}
						</button>
					</div>
				</form>
			{/if}

			{#if auditSummary}
				<div class="mb-5">
					<DeviceAuditTrail
						createdAt={auditSummary.createdAt}
						createdBy={auditSummary.createdBy}
						modifiedAt={auditSummary.modifiedAt}
						modifiedBy={auditSummary.modifiedBy}
					/>
				</div>
			{/if}

			{#if loading}
				<LoadingSkeleton rows={5} />
			{:else if error}
				<ErrorState {error} onRetry={loadEvents} />
			{:else if events.length === 0}
				<div class="rounded-2xl border border-neutral-200 bg-neutral-50 p-8 text-center text-sm text-neutral-600 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-400">
					{emptyMessage}
				</div>
			{:else}
				<div class="overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
					<div class="overflow-x-auto">
						<table class="min-w-full divide-y divide-neutral-200 dark:divide-neutral-800">
							<thead class="bg-neutral-50 dark:bg-neutral-900">
								<tr>
									<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
										{t('admin.audit.columns.timestamp')}
									</th>
									<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
										{t('admin.audit.columns.actor')}
									</th>
									<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
										{t('admin.audit.columns.entityType')}
									</th>
									<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
										{t('admin.audit.columns.action')}
									</th>
									<th scope="col" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300">
										{t('admin.audit.columns.summary')}
									</th>
								</tr>
							</thead>
							<tbody class="divide-y divide-neutral-200 dark:divide-neutral-800">
								{#each events as event (event.id)}
									<tr class="transition-colors hover:bg-neutral-50 dark:hover:bg-neutral-900">
										<td class="px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100">
											<button type="button" onclick={() => openDetail(event)} class="w-full text-left focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 rounded-sm">
												{formatTimestamp(event.timestamp)}
											</button>
										</td>
										<td class="px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100">{event.actor ?? '—'}</td>
										<td class="px-4 py-3 text-sm text-neutral-900 dark:text-neutral-100">{event.entityType ?? '—'}</td>
										<td class="px-4 py-3 text-sm">
											<span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {actionClass(event.action)}">
												{event.action ?? '—'}
											</span>
										</td>
										<td class="px-4 py-3 text-sm text-neutral-600 dark:text-neutral-400">{summarize(event)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
					<PaginationControls
						currentPage={currentPage}
						{pageSize}
						{totalCount}
						onPageChange={handlePageChange}
					/>
				</div>
			{/if}
		</div>
	</div>
</dialog>

<AuditDiffDrawer event={selectedEvent} onClose={() => (selectedEvent = null)} />

<style>
	.audit-log-modal {
		margin: auto;
	}

	.audit-log-modal::backdrop {
		background: rgb(10 10 10 / 0.6);
		backdrop-filter: blur(6px);
	}
</style>

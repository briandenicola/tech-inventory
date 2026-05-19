<!--
	AuditDiffDrawer.svelte — Right-side drawer that renders a BEFORE/AFTER diff
	for one AuditEvent row. F021 v1.

	Color tokens (matches existing palette):
	- added    → success-* (green)
	- removed  → danger-* (red)
	- changed  → warning-* (amber)
	- unchanged → neutral (rendered collapsed-by-default)

	Props:
	- event: the AuditEventResponse to render (null hides drawer)
	- onClose: invoked on Escape / backdrop / X click
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import type { components } from '$lib/api/generated/types';
	import {
		computeDiff,
		formatValueForDisplay,
		parseAuditPayload,
		type DiffEntry,
		type DiffKind
	} from '$lib/utils/jsonDiff';

	type AuditEvent = components['schemas']['AuditEventResponse'];

	interface Props {
		event: AuditEvent | null;
		onClose: () => void;
	}

	let { event, onClose }: Props = $props();

	let showUnchanged = $state(false);
	let dialogElement = $state<HTMLDivElement | undefined>(undefined);

	const diff = $derived.by<DiffEntry[]>(() => {
		if (!event) return [];
		const before = parseAuditPayload(event.beforePayload ?? null);
		const after = parseAuditPayload(event.afterPayload ?? null);
		return computeDiff(before, after);
	});

	const visibleDiff = $derived(showUnchanged ? diff : diff.filter((d) => d.kind !== 'unchanged'));

	const unchangedCount = $derived(diff.filter((d) => d.kind === 'unchanged').length);

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') onClose();
	}

	$effect(() => {
		if (!event) return;
		const previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';
		if (dialogElement) dialogElement.focus();
		return () => {
			document.body.style.overflow = previousOverflow;
		};
	});

	function kindClass(kind: DiffKind): string {
		switch (kind) {
			case 'added':
				return 'bg-success-50 text-success-900 dark:bg-success-950 dark:text-success-100 border-l-4 border-success-500';
			case 'removed':
				return 'bg-danger-50 text-danger-900 dark:bg-danger-950 dark:text-danger-100 border-l-4 border-danger-500';
			case 'changed':
				return 'bg-warning-50 text-warning-900 dark:bg-warning-950 dark:text-warning-100 border-l-4 border-warning-500';
			case 'unchanged':
				return 'bg-neutral-50 text-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 border-l-4 border-neutral-300 dark:border-neutral-700';
		}
	}

	function kindLabel(kind: DiffKind): string {
		switch (kind) {
			case 'added':
				return t('admin.audit.diff.added');
			case 'removed':
				return t('admin.audit.diff.removed');
			case 'changed':
				return t('admin.audit.diff.changed');
			case 'unchanged':
				return t('admin.audit.diff.unchanged');
		}
	}

	function formatTimestamp(ts: string | null | undefined): string {
		if (!ts) return '—';
		return new Date(ts).toLocaleString();
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if event}
	<div class="fixed inset-0 z-50">
		<button
			type="button"
			aria-label={t('common.actions.close')}
			class="absolute inset-0 h-full w-full cursor-default bg-black/60 backdrop-blur-sm"
			onclick={onClose}
		></button>

		<div
			bind:this={dialogElement}
			class="absolute right-0 top-0 flex h-full w-full max-w-2xl flex-col overflow-hidden bg-white shadow-2xl dark:bg-neutral-900"
			role="dialog"
			aria-modal="true"
			aria-labelledby="audit-diff-title"
			tabindex="-1"
		>
			<!-- Header -->
			<div
				class="flex items-start justify-between gap-4 border-b border-neutral-200/70 bg-white/95 px-6 py-5 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-900/95"
			>
				<div class="min-w-0 flex-1">
					<h2
						id="audit-diff-title"
						class="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
					>
						{t('admin.audit.diff.title')}
					</h2>
					<dl class="mt-3 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
						<div>
							<dt class="text-neutral-500 dark:text-neutral-400">
								{t('admin.audit.columns.timestamp')}
							</dt>
							<dd class="font-medium text-neutral-900 dark:text-neutral-100">
								{formatTimestamp(event.timestamp)}
							</dd>
						</div>
						<div>
							<dt class="text-neutral-500 dark:text-neutral-400">
								{t('admin.audit.columns.actor')}
							</dt>
							<dd class="truncate font-medium text-neutral-900 dark:text-neutral-100">
								{event.actor ?? '—'}
							</dd>
						</div>
						<div>
							<dt class="text-neutral-500 dark:text-neutral-400">
								{t('admin.audit.columns.entityType')}
							</dt>
							<dd class="font-medium text-neutral-900 dark:text-neutral-100">
								{event.entityType ?? '—'}
							</dd>
						</div>
						<div>
							<dt class="text-neutral-500 dark:text-neutral-400">
								{t('admin.audit.columns.action')}
							</dt>
							<dd class="font-medium text-neutral-900 dark:text-neutral-100">
								{event.action ?? '—'}
							</dd>
						</div>
						<div class="col-span-2">
							<dt class="text-neutral-500 dark:text-neutral-400">
								{t('admin.audit.columns.entityId')}
							</dt>
							<dd
								class="break-all font-mono text-xs text-neutral-700 dark:text-neutral-300"
							>
								{event.entityId ?? '—'}
							</dd>
						</div>
					</dl>
				</div>
				<button
					type="button"
					onclick={onClose}
					class="inline-flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-full text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
					aria-label={t('common.actions.close')}
				>
					<svg
						class="h-5 w-5"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						aria-hidden="true"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					</svg>
				</button>
			</div>

			<!-- Body (scrollable) -->
			<div class="flex-1 overflow-y-auto px-6 py-6">
				{#if diff.length === 0}
					<p class="text-sm text-neutral-600 dark:text-neutral-400">
						{t('admin.audit.diff.empty')}
					</p>
				{:else}
					<div class="mb-4 flex items-center justify-between">
						<h3 class="text-sm font-semibold uppercase tracking-wider text-neutral-600 dark:text-neutral-400">
							{t('admin.audit.diff.changes')}
						</h3>
						{#if unchangedCount > 0}
							<label class="flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
								<input
									type="checkbox"
									bind:checked={showUnchanged}
									class="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
								/>
								{t('admin.audit.diff.showUnchanged')} ({unchangedCount})
							</label>
						{/if}
					</div>

					<ul class="space-y-2">
						{#each visibleDiff as entry (entry.path + entry.kind)}
							<li class="rounded-r-lg px-4 py-3 {kindClass(entry.kind)}">
								<div class="flex items-center justify-between gap-2">
									<code class="font-mono text-sm font-semibold">
										{entry.path || '(value)'}
									</code>
									<span class="text-xs font-semibold uppercase tracking-wider opacity-75">
										{kindLabel(entry.kind)}
									</span>
								</div>
								{#if entry.kind === 'changed'}
									<div class="mt-2 space-y-1 text-sm">
										<div>
											<span class="text-xs font-medium opacity-75">
												{t('admin.audit.diff.before')}:
											</span>
											<span class="ml-2 break-all font-mono">
												{formatValueForDisplay(entry.before)}
											</span>
										</div>
										<div>
											<span class="text-xs font-medium opacity-75">
												{t('admin.audit.diff.after')}:
											</span>
											<span class="ml-2 break-all font-mono">
												{formatValueForDisplay(entry.after)}
											</span>
										</div>
									</div>
								{:else if entry.kind === 'added'}
									<div class="mt-2 break-all font-mono text-sm">
										{formatValueForDisplay(entry.after)}
									</div>
								{:else if entry.kind === 'removed'}
									<div class="mt-2 break-all font-mono text-sm">
										{formatValueForDisplay(entry.before)}
									</div>
								{:else}
									<div class="mt-2 break-all font-mono text-sm opacity-75">
										{formatValueForDisplay(entry.after)}
									</div>
								{/if}
							</li>
						{/each}
					</ul>
				{/if}
			</div>
		</div>
	</div>
{/if}

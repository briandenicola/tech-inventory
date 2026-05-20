<!--
	BulkDeleteModal.svelte — F024 destructive confirmation for bulk delete.

	"Type {count} to confirm" + reason textarea (≥10 chars). Mirrors single-delete UX
	(DeleteDeviceModal) but type-confirm value is the selection count rather than a name.
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		count: number;
		onConfirm: (reason: string) => Promise<void>;
		onCancel: () => void;
	}

	let { count, onConfirm, onCancel }: Props = $props();

	let confirmText = $state('');
	let reason = $state('');
	let isSubmitting = $state(false);
	let modalElement: HTMLDivElement | undefined;

	const canSubmit = $derived(
		confirmText.trim() === String(count) && reason.trim().length >= 10 && !isSubmitting
	);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!canSubmit) return;

		isSubmitting = true;
		try {
			await onConfirm(reason);
		} catch (err) {
			console.error('[BulkDeleteModal] Submit failed:', err);
		} finally {
			isSubmitting = false;
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && !isSubmitting) onCancel();
	}

	$effect(() => {
		if (modalElement) {
			const focusables = modalElement.querySelectorAll<HTMLElement>(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			);
			const first = focusables[0];
			const last = focusables[focusables.length - 1];

			function trap(e: KeyboardEvent) {
				if (e.key !== 'Tab') return;
				if (e.shiftKey && document.activeElement === first) {
					e.preventDefault();
					last?.focus();
				} else if (!e.shiftKey && document.activeElement === last) {
					e.preventDefault();
					first?.focus();
				}
			}
			modalElement.addEventListener('keydown', trap);
			first?.focus();
			return () => modalElement?.removeEventListener('keydown', trap);
		}
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="ti-modal-backdrop fixed inset-0 z-40" aria-hidden="true"></div>

<div
	bind:this={modalElement}
	class="fixed inset-0 z-50 flex items-center justify-center p-4"
	role="dialog"
	aria-labelledby="bulk-delete-title"
	aria-modal="true"
>
	<div class="ti-modal-surface w-full max-w-md overflow-hidden rounded-lg bg-white shadow-xl dark:bg-neutral-900">
		<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
			<h2 id="bulk-delete-title" class="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
				{t('devices.bulk.deleteModal.title', { count })}
			</h2>
		</div>

		<form onsubmit={handleSubmit} class="space-y-4 px-6 py-4">
			<div class="flex gap-3 rounded-lg bg-danger-50 p-3 dark:bg-danger-950">
				<svg class="h-5 w-5 flex-shrink-0 text-danger-600 dark:text-danger-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
				</svg>
				<p class="text-sm text-danger-900 dark:text-danger-100">
					{t('devices.bulk.deleteModal.warning', { count })}
				</p>
			</div>

			<div>
				<label for="confirmCount" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t('devices.bulk.deleteModal.confirmPrompt', { count })}
				</label>
				<input
					id="confirmCount"
					type="text"
					inputmode="numeric"
					bind:value={confirmText}
					placeholder={String(count)}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus:border-danger-500 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus:border-danger-500"
				/>
			</div>

			<div>
				<label for="bulkReason" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t('devices.bulk.deleteModal.reasonLabel')} <span class="text-danger-600">*</span>
				</label>
				<textarea
					id="bulkReason"
					bind:value={reason}
					rows="3"
					minlength="10"
					placeholder={t('devices.bulk.deleteModal.reasonPlaceholder')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus:border-danger-500 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus:border-danger-500"
				></textarea>
				<p class="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
					Minimum 10 characters ({reason.trim().length}/10)
				</p>
			</div>

			<div class="flex justify-end gap-3 border-t border-neutral-200 pt-4 dark:border-neutral-800">
				<button
					type="button"
					onclick={onCancel}
					disabled={isSubmitting}
					class="rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus:outline-none focus:ring-2 focus:ring-neutral-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:focus:ring-neutral-600"
				>
					{t('common.actions.cancel')}
				</button>
				<button
					type="submit"
					disabled={!canSubmit}
					class="inline-flex items-center gap-2 rounded-lg bg-danger-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-danger-700 focus:outline-none focus:ring-2 focus:ring-danger-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-danger-500 dark:hover:bg-danger-600"
				>
					{#if isSubmitting}
						<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						{t('common.states.deleting')}
					{:else}
						{t('common.actions.delete')}
					{/if}
				</button>
			</div>
		</form>
	</div>
</div>

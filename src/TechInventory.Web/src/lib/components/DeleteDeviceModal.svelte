<!--
	DeleteDeviceModal.svelte — Confirmation modal for device deletion (T22)
	
	"Type device name to confirm" pattern + reason textarea (≥10 chars).
	Focus trap (roll-your-own with tab cycling). Backdrop click disabled.
	Only Cancel or Escape dismisses. Submit disabled until name matches exactly.
	
	Props:
	- deviceName: string (for confirmation input)
	- onConfirm: (reason: string) => Promise<void>
	- onCancel: () => void
	
	Related: specs/002-frontend-mvp/spec.md J8, Constitution §4.3
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		deviceName: string;
		onConfirm: (reason: string) => Promise<void>;
		onCancel: () => void;
	}

	let { deviceName, onConfirm, onCancel }: Props = $props();

	let confirmName = $state('');
	let reason = $state('');
	let isSubmitting = $state(false);
	let modalElement: HTMLDivElement | undefined;

	const canSubmit = $derived(
		confirmName.trim() === deviceName.trim() && reason.trim().length >= 10 && !isSubmitting
	);

	// Handle submit
	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!canSubmit) return;

		isSubmitting = true;
		try {
			await onConfirm(reason);
		} catch (err) {
			console.error('[DeleteDeviceModal] Submit failed:', err);
		} finally {
			isSubmitting = false;
		}
	}

	// Escape key handler
	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && !isSubmitting) {
			onCancel();
		}
	}

	// Focus trap: cycle focus between first and last focusable elements
	// Simple implementation without external library per D-071 (to be dropped)
	$effect(() => {
		if (modalElement) {
			const focusableElements = modalElement.querySelectorAll(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			);
			const firstElement = focusableElements[0] as HTMLElement;
			const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

			function trapFocus(e: KeyboardEvent) {
				if (e.key !== 'Tab') return;

				if (e.shiftKey) {
					// Shift+Tab: if at first element, jump to last
					if (document.activeElement === firstElement) {
						e.preventDefault();
						lastElement.focus();
					}
				} else {
					// Tab: if at last element, jump to first
					if (document.activeElement === lastElement) {
						e.preventDefault();
						firstElement.focus();
					}
				}
			}

			modalElement.addEventListener('keydown', trapFocus);
			firstElement?.focus();

			return () => {
				modalElement?.removeEventListener('keydown', trapFocus);
			};
		}
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<!-- Modal backdrop -->
<div
	class="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm"
	aria-hidden="true"
></div>

<!-- Modal dialog -->
<div
	bind:this={modalElement}
	class="fixed inset-0 z-50 flex items-center justify-center p-4"
	role="dialog"
	aria-labelledby="delete-modal-title"
	aria-modal="true"
>
	<div
		class="w-full max-w-md overflow-hidden rounded-lg bg-white shadow-xl dark:bg-neutral-900"
	>
		<!-- Header -->
		<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
			<h2 id="delete-modal-title" class="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
				{t('devices.delete.title')}
			</h2>
		</div>

		<!-- Body -->
		<form onsubmit={handleSubmit} class="space-y-4 px-6 py-4">
			<!-- Warning message -->
			<div class="flex gap-3 rounded-lg bg-danger-50 p-3 dark:bg-danger-950">
				<svg
					class="h-5 w-5 flex-shrink-0 text-danger-600 dark:text-danger-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
					/>
				</svg>
				<p class="text-sm text-danger-900 dark:text-danger-100">
					This action will soft-delete the device. Type the device name to confirm.
				</p>
			</div>

			<!-- Confirmation input -->
			<div>
				<label for="confirmName" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t('devices.delete.confirmPrompt')}
				</label>
				<input
					id="confirmName"
					type="text"
					bind:value={confirmName}
					placeholder={deviceName}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus:border-danger-500 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus:border-danger-500"
				/>
			</div>

			<!-- Reason textarea -->
			<div>
				<label for="reason" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t('devices.delete.reasonLabel')} <span class="text-danger-600">*</span>
				</label>
				<textarea
					id="reason"
					bind:value={reason}
					rows="3"
					minlength="10"
					placeholder={t('devices.delete.reasonPlaceholder')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus:border-danger-500 focus:outline-none focus:ring-2 focus:ring-danger-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus:border-danger-500"
				></textarea>
				<p class="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
					Minimum 10 characters ({reason.trim().length}/10)
				</p>
			</div>

			<!-- Action buttons -->
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
						<svg
							class="h-4 w-4 animate-spin"
							fill="none"
							viewBox="0 0 24 24"
							aria-hidden="true"
						>
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							></path>
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

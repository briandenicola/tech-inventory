<!--
	RetireDeviceModal.svelte — Device retirement confirmation modal
	
	Simple confirmation modal for retiring active devices.
	Yes/Cancel confirmation.
	Focus trap (roll-your-own, same pattern as ReleaseOwnershipModal).
	Backdrop click DOES close (less destructive than delete).
	
	Props:
	- deviceName: string (for display)
	- onConfirm: () => Promise<void>
	- onCancel: () => void
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		deviceName: string;
		onConfirm: () => Promise<void>;
		onCancel: () => void;
	}

	let { deviceName, onConfirm, onCancel }: Props = $props();

	let isSubmitting = $state(false);
	let modalElement: HTMLDivElement | undefined;

	// Handle submit
	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (isSubmitting) return;

		isSubmitting = true;
		try {
			await onConfirm();
		} catch (err) {
			console.error('[RetireDeviceModal] Submit failed:', err);
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

	// Backdrop click handler (close modal)
	function handleBackdropClick(e: MouseEvent) {
		if (e.target === e.currentTarget && !isSubmitting) {
			onCancel();
		}
	}

	// Focus trap: cycle focus between first and last focusable elements
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

<!-- Modal backdrop (clickable to close) -->
<div
	class="ti-modal-backdrop fixed inset-0 z-40"
	onclick={handleBackdropClick}
	aria-hidden="true"
></div>

<!-- Modal dialog -->
<div
	bind:this={modalElement}
	class="fixed inset-0 z-50 flex items-center justify-center p-4"
	role="dialog"
	aria-labelledby="retire-modal-title"
	aria-modal="true"
>
	<div
		class="ti-modal-surface w-full max-w-md overflow-hidden rounded-lg bg-white shadow-xl dark:bg-neutral-900"
	>
		<!-- Header -->
		<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
			<h2 id="retire-modal-title" class="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
				{t('devices.retire.modal.title')}
			</h2>
		</div>

		<!-- Body -->
		<form onsubmit={handleSubmit} class="space-y-4 px-6 py-4">
			<!-- Warning message -->
			<div class="flex gap-3 rounded-lg bg-warning-50 p-3 dark:bg-warning-950">
				<svg
					class="h-5 w-5 flex-shrink-0 text-warning-600 dark:text-warning-400"
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
				<div class="text-sm text-warning-900 dark:text-warning-100">
					<p class="font-medium">{deviceName}</p>
					<p class="mt-1">{t('devices.retire.modal.body')}</p>
				</div>
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
					disabled={isSubmitting}
					class="inline-flex items-center gap-2 rounded-lg bg-warning-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-warning-700 focus:outline-none focus:ring-2 focus:ring-warning-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-warning-500 dark:hover:bg-warning-600"
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
						{t('common.states.processing')}
					{:else}
						{t('devices.retire.modal.confirm')}
					{/if}
				</button>
			</div>
		</form>
	</div>
</div>

<!--
	DeactivateConfirmModal.svelte — Lightweight confirmation for deactivation (T27, T30, T31, T32)
	
	Simpler than DeleteDeviceModal (no type-name-to-confirm, no reason field).
	Deactivation is reversible via "Show Inactive" toggle, so lighter UX is acceptable.
	
	Props:
	- entityName: string (display name for confirmation message)
	- entityType: 'brand' | 'category' | 'location' | 'network' | 'owner' | 'tag' (for i18n lookup)
	- onConfirm: () => Promise<void>
	- onCancel: () => void
	
	Decision: D-090 (Deactivate confirm UX — simple Yes/Cancel, no type-to-confirm)
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		entityName: string;
		entityType: 'brand' | 'category' | 'location' | 'network' | 'owner' | 'tag';
		onConfirm: () => Promise<void>;
		onCancel: () => void;
	}

	let { entityName, entityType, onConfirm, onCancel }: Props = $props();

	let isSubmitting = $state(false);
	let modalElement: HTMLDivElement | undefined;

	// Handle confirm
	async function handleConfirm() {
		if (isSubmitting) return;

		isSubmitting = true;
		try {
			await onConfirm();
		} catch (err) {
			console.error('[DeactivateConfirmModal] Confirm failed:', err);
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

	// Auto-focus first button on mount
	$effect(() => {
		if (modalElement) {
			const firstButton = modalElement.querySelector('button');
			firstButton?.focus();
		}
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<!-- Backdrop (fixed overlay) -->
<div class="ti-modal-backdrop fixed inset-0 z-40" aria-hidden="true"></div>

<div
	class="fixed inset-0 z-50 flex items-center justify-center"
	role="dialog"
	aria-modal="true"
	aria-labelledby="deactivate-modal-title"
	bind:this={modalElement}
>
	<!-- Modal Card -->
	<div
		class="ti-modal-surface relative mx-4 w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl dark:bg-neutral-900"
	>
		<!-- Title -->
		<h2
			id="deactivate-modal-title"
			class="mb-4 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
		>
			{t(`${entityType}s.deactivate.title`)}
		</h2>

		<!-- Confirm Prompt -->
		<p class="mb-6 text-base text-neutral-700 dark:text-neutral-300">
			{t(`${entityType}s.deactivate.confirmPrompt`)}
		</p>

		<!-- Entity Name (for reference) -->
		<div class="mb-6 rounded-xl bg-neutral-100 p-4 dark:bg-neutral-800">
			<p class="text-base font-medium text-neutral-900 dark:text-neutral-50">
				{entityName}
			</p>
		</div>

		<!-- Actions -->
		<div class="flex justify-end gap-3">
			<button
				type="button"
				onclick={onCancel}
				disabled={isSubmitting}
				class="inline-flex min-h-11 items-center rounded-full border border-neutral-300 px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors duration-150 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('common.actions.cancel')}
			</button>
			<button
				type="button"
				onclick={handleConfirm}
				disabled={isSubmitting}
				class="inline-flex min-h-11 items-center rounded-full bg-warning-600 px-5 py-2.5 text-base font-medium text-white transition-colors duration-150 hover:bg-warning-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-warning-700 dark:hover:bg-warning-800"
			>
				{isSubmitting ? t('common.states.loading') : t('common.actions.confirm')}
			</button>
		</div>
	</div>
</div>

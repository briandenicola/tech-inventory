<!--
	BulkUpdateModal.svelte — F024 reusable modal for changing a single field across N devices.

	Spec: specs/_backlog/F024-multi-select-bulk-actions.md. Accepts a field type +
	the selected option list. On confirm, calls onConfirm(value) with the chosen value.
	The caller wires that into devices.bulkUpdate(...).
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	type BulkField = 'category' | 'owner' | 'brand' | 'location' | 'status';

	interface Option {
		id: string;
		name: string;
	}

	interface Props {
		field: BulkField;
		count: number;
		options: Option[];
		onConfirm: (value: string) => Promise<void>;
		onCancel: () => void;
	}

	let { field, count, options, onConfirm, onCancel }: Props = $props();

	let selectedValue = $state('');
	let isSubmitting = $state(false);
	let modalElement: HTMLDivElement | undefined;

	const fieldLabelKey = $derived(`devices.bulk.change${field.charAt(0).toUpperCase() + field.slice(1)}` as const);
	const canSubmit = $derived(selectedValue !== '' && !isSubmitting);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!canSubmit) return;

		isSubmitting = true;
		try {
			await onConfirm(selectedValue);
		} catch (err) {
			console.error('[BulkUpdateModal] Submit failed:', err);
		} finally {
			isSubmitting = false;
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && !isSubmitting) {
			onCancel();
		}
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

<div class="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm" aria-hidden="true"></div>

<div
	bind:this={modalElement}
	class="fixed inset-0 z-50 flex items-center justify-center p-4"
	role="dialog"
	aria-labelledby="bulk-update-title"
	aria-modal="true"
>
	<div class="w-full max-w-md overflow-hidden rounded-lg bg-white shadow-xl dark:bg-neutral-900">
		<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
			<h2 id="bulk-update-title" class="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
				{t('devices.bulk.update.title', { count })}
			</h2>
		</div>

		<form onsubmit={handleSubmit} class="space-y-4 px-6 py-4">
			<div>
				<label for="bulk-value" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t(fieldLabelKey)}
				</label>
				<select
					id="bulk-value"
					bind:value={selectedValue}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600"
				>
					<option value="" disabled>{t('common.actions.select')}</option>
					{#each options as option (option.id)}
						<option value={option.id}>{option.name}</option>
					{/each}
				</select>
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
					class="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-500 dark:hover:bg-primary-600"
				>
					{#if isSubmitting}
						<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						{t('common.states.saving')}
					{:else}
						{t('devices.bulk.update.confirm')}
					{/if}
				</button>
			</div>
		</form>
	</div>
</div>

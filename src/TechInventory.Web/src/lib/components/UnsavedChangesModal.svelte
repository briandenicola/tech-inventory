<!--
	UnsavedChangesModal.svelte — Confirm-before-discard guard

	Shown whenever the user tries to leave a dirty form (Cancel, Escape,
	backdrop click, browser Back/forward, or an in-app link) without saving.
	Modeled on DeactivateConfirmModal's structure/visual language.

	Props:
	- onDiscard:     () => void — proceed with leaving, discarding the edit
	- onKeepEditing: () => void — dismiss this modal, stay on the form
-->
<script lang="ts">
	import { t } from '$lib/i18n';

	interface Props {
		onDiscard: () => void;
		onKeepEditing: () => void;
	}

	let { onDiscard, onKeepEditing }: Props = $props();

	let modalElement: HTMLDivElement | undefined;

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			onKeepEditing();
		}
	}

	$effect(() => {
		if (modalElement) {
			const firstButton = modalElement.querySelector('button');
			firstButton?.focus();
		}
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="ti-modal-backdrop fixed inset-0" style="z-index: var(--z-modal-backdrop);" aria-hidden="true"></div>

<div
	class="fixed inset-0 flex items-center justify-center"
	style="z-index: var(--z-modal);"
	role="dialog"
	aria-modal="true"
	aria-labelledby="unsaved-changes-title"
	bind:this={modalElement}
>
	<div
		class="ti-modal-surface relative mx-4 w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl dark:bg-neutral-900"
	>
		<h2
			id="unsaved-changes-title"
			class="mb-2 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
		>
			{t('common.unsavedChanges.title')}
		</h2>
		<p class="mb-6 text-base text-neutral-700 dark:text-neutral-300">
			{t('common.unsavedChanges.description')}
		</p>

		<div class="flex justify-end gap-3">
			<button
				type="button"
				onclick={onDiscard}
				class="inline-flex min-h-11 items-center rounded-full border border-neutral-300 px-5 py-2.5 text-base font-medium text-neutral-700 transition-colors duration-150 hover:bg-neutral-50 dark:border-neutral-600 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('common.unsavedChanges.discard')}
			</button>
			<button
				type="button"
				onclick={onKeepEditing}
				class="inline-flex min-h-11 items-center rounded-full bg-primary-600 px-5 py-2.5 text-base font-medium text-white transition-colors duration-150 hover:bg-primary-700 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				{t('common.unsavedChanges.keepEditing')}
			</button>
		</div>
	</div>
</div>

<!--
	AddDeviceModal.svelte — Modal wrapper around DeviceForm for the
	primary Add Device CTA on /devices.

	D-137: Apple-elegant — rounded-2xl card, shadow-2xl, backdrop-blur
	backdrop, sticky header with close X, scrollable form body capped at
	90vh, Escape + backdrop click to close.

	Props:
	- onClose:   () => void — called on Cancel, Escape, or backdrop click
	- onCreated: (id: string) => void — called after a successful create
	             (caller typically refetches the devices list)
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { devices } from '$lib/api/client';
	import { showToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import DeviceForm from '$lib/components/DeviceForm.svelte';
	import TagPicker from '$lib/components/TagPicker.svelte';
	import type { DeviceCreateInput } from '$lib/schemas/device';

	interface Props {
		onClose: () => void;
		onCreated?: (id: string) => void;
	}

	let { onClose, onCreated }: Props = $props();

	let modalElement = $state<HTMLDivElement | undefined>(undefined);
	let dialogElement = $state<HTMLDivElement | undefined>(undefined);

	// F030: device tagging happens via separate POST /devices/{id}/tags
	// endpoints, so the picker manages its own state and we apply the
	// selection after the device has been created.
	let selectedTagIds = $state<string[]>([]);
	const availableTags = $derived($referenceDataStore.tags);

	async function handleSubmit(data: DeviceCreateInput) {
		// Transform empty strings to undefined for optional UUID fields
		// (matches /devices/new page logic)
		const payload = {
			...data,
			model: data.model || undefined,
			ownerId: data.ownerId || undefined,
			locationId: data.locationId || undefined,
			networkId: data.networkId || undefined,
			serialNumber: data.serialNumber || undefined,
			purchaseDate: data.purchaseDate || undefined,
			purchasePrice: data.purchasePrice ?? undefined,
			currencyCode: data.currencyCode || undefined,
			notes: data.notes || undefined
		};

		try {
			const result = await devices.create(payload);

			// F030: tag application is best-effort and runs only after the
			// device exists. A tag failure should not block the create-success
			// toast — surface partial failures with a separate warning toast.
			if (result.id && selectedTagIds.length > 0) {
				const tagResults = await Promise.allSettled(
					selectedTagIds.map((tagId) => devices.addTag(result.id!, tagId))
				);
				const failures = tagResults.filter((r) => r.status === 'rejected').length;
				if (failures > 0) {
					showToast({
						type: 'error',
						message: t('devices.tags.applyErrorSome', { count: failures })
					});
				}
			}

			invalidateDevicesCache();

			showToast({
				type: 'success',
				message: `Device "${data.name}" created successfully`
			});

			if (result.id) {
				onCreated?.(result.id);
			}
			onClose();
		} catch (err) {
			console.error('[AddDeviceModal] Submit failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to create device';
			showToast({ type: 'error', message: errorMsg });
			throw err; // keep DeviceForm in submitting state false via finally
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			onClose();
		}
	}

	$effect(() => {
		// Lock body scroll while the modal is open
		const previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';

		// Focus the first input inside the form on mount for keyboard users
		if (dialogElement) {
			const firstInput = dialogElement.querySelector<HTMLElement>(
				'input, select, textarea'
			);
			firstInput?.focus();
		}

		return () => {
			document.body.style.overflow = previousOverflow;
		};
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<!--
	Modal layering:
	- Backdrop is a real <button> so click-to-close has built-in keyboard
	  semantics (Enter/Space) and screen-reader labelling.
	- Dialog wrapper is pointer-events-none so click-through hits the
	  backdrop; the dialog card re-enables pointer-events for itself.
	- Escape is handled at the window level above.
-->
<div class="fixed inset-0" style="z-index: var(--z-modal-backdrop);" bind:this={modalElement}>
	<button
		type="button"
		aria-label={t('common.actions.cancel')}
		class="ti-modal-backdrop absolute inset-0 h-full w-full cursor-default"
		onclick={onClose}
	></button>

	<div class="pointer-events-none relative flex h-full items-center justify-center px-4 pt-[calc(env(safe-area-inset-top,0px)+4.5rem)] pb-4 sm:py-10">
		<div
			bind:this={dialogElement}
			class="ti-modal-surface pointer-events-auto relative flex max-h-[85dvh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl dark:bg-neutral-900"
			style="z-index: var(--z-modal);"
			role="dialog"
			aria-modal="true"
			aria-labelledby="add-device-modal-title"
			tabindex="-1"
		>
				<!-- Fixed header (outside scroll area) -->
				<div
					class="flex shrink-0 items-center justify-between border-b border-neutral-200/70 bg-white px-6 py-5 dark:border-neutral-800/70 dark:bg-neutral-900"
				>
					<div>
						<h2
							id="add-device-modal-title"
							class="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
						>
							{t('devices.create.title')}
						</h2>
						<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
							{t('devices.create.subtitle')}
						</p>
					</div>
					<button
						type="button"
						onclick={onClose}
						class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
						aria-label={t('common.actions.cancel')}
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

				<!-- Form body (scrollable) -->
				<div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-6 py-6">
					<!--
						F030 tag picker. Lives above DeviceForm so it's visible without
						scrolling and so the user can curate tags before saving. Tags
						persist via separate POST /devices/{id}/tags calls inside
						handleSubmit after the device record exists.
					-->
					<div class="mb-6">
						<label
							for="add-device-tag-picker"
							class="mb-1.5 block text-sm font-medium text-neutral-900 dark:text-neutral-100"
						>
							{t('devices.tags.sectionLabel')}
						</label>
						<TagPicker
							id="add-device-tag-picker"
							selectedIds={selectedTagIds}
							{availableTags}
							onChange={(ids) => (selectedTagIds = ids)}
						/>
					</div>

					<DeviceForm mode="create" onSubmit={handleSubmit} onCancel={onClose} />
				</div>
		</div>
	</div>
</div>
<!--
	QuickCreateReferenceModal.svelte — Lightweight inline create dialog for Category, Brand, or Location.

	#136: Allows creating a missing reference entity directly from the device add/edit form
	without navigating away or losing in-progress form data.

	Accessibility (skill: modal-rendering, mobile-sheet-pattern):
	- role="dialog" aria-modal="true" aria-labelledby
	- Tab/Shift+Tab focus trapped inside the dialog while open
	- Escape closes without creating anything
	- On close/cancel, focus returns to the originating control (triggerElement)

	API errors:
	- HTTP 409 Conflict → inline duplicate-name field error (user-legible)
	- Other failures → generic submit-error banner

	Props:
	- type: 'brand' | 'category' | 'location'
	- triggerElement: HTMLElement | null — receives focus when dialog closes
	- onCreated: (id: string, name: string) => void
	- onCancel: () => void

	Decision: Owner and Network are explicitly out of scope per #136 design review.

	Related: issue #136, specs/002-frontend-mvp/spec.md
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { brands, categories, locations, ApiError } from '$lib/api/client';
	import { brandSchema } from '$lib/schemas/brand';
	import { categorySchema } from '$lib/schemas/category';
	import { locationSchema } from '$lib/schemas/location';

	type ReferenceType = 'brand' | 'category' | 'location';

	interface Props {
		type: ReferenceType;
		triggerElement: HTMLElement | null;
		onCreated: (id: string, name: string) => void;
		onCancel: () => void;
	}

	let { type, triggerElement, onCreated, onCancel }: Props = $props();

	let dialogElement: HTMLDivElement | undefined;
	let name = $state('');
	let locationType = $state<'Home' | 'Storage' | 'External'>('Home');
	let nameError = $state('');
	let locationTypeError = $state('');
	let submitError = $state('');
	let isSubmitting = $state(false);

	// Focus the name input and wire the focus trap on mount.
	$effect(() => {
		if (!dialogElement) return;

		const nameInput = dialogElement.querySelector<HTMLElement>('#qc-name');
		nameInput?.focus();

		function trapFocus(e: KeyboardEvent) {
			if (e.key !== 'Tab') return;
			const focusable = Array.from(
				dialogElement!.querySelectorAll<HTMLElement>(
					'button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
				)
			);
			if (focusable.length === 0) return;

			const first = focusable[0];
			const last = focusable[focusable.length - 1];

			if (e.shiftKey) {
				if (document.activeElement === first) {
					e.preventDefault();
					last.focus();
				}
			} else {
				if (document.activeElement === last) {
					e.preventDefault();
					first.focus();
				}
			}
		}

		dialogElement.addEventListener('keydown', trapFocus);
		return () => dialogElement?.removeEventListener('keydown', trapFocus);
	});

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && !isSubmitting) {
			handleCancel();
		}
	}

	function handleCancel() {
		triggerElement?.focus();
		onCancel();
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		nameError = '';
		locationTypeError = '';
		submitError = '';

		// Client-side Zod validation mirrors the admin screens' FluentValidation.
		if (type === 'brand') {
			const result = brandSchema.pick({ name: true }).safeParse({ name });
			if (!result.success) {
				nameError = result.error.issues[0]?.message ?? t('common.validation.required');
				return;
			}
		} else if (type === 'category') {
			const result = categorySchema.pick({ name: true }).safeParse({ name });
			if (!result.success) {
				nameError = result.error.issues[0]?.message ?? t('common.validation.required');
				return;
			}
		} else {
			const result = locationSchema.safeParse({ name, type: locationType, notes: '' });
			if (!result.success) {
				result.error.issues.forEach((issue) => {
					if (issue.path[0] === 'name') nameError = issue.message;
					else if (issue.path[0] === 'type') locationTypeError = issue.message;
				});
				return;
			}
		}

		isSubmitting = true;
		try {
			let id: string;
			let createdName: string;

			if (type === 'brand') {
				const res = await brands.create({ name: name.trim() });
				id = res.id ?? '';
				createdName = res.name ?? name.trim();
			} else if (type === 'category') {
				// Creates as a root category (no parentId). Auditing is identical
				// to admin-screen creation — same API endpoint.
				const res = await categories.create({ name: name.trim() });
				id = res.id ?? '';
				createdName = res.name ?? name.trim();
			} else {
				const res = await locations.create({ name: name.trim(), type: locationType });
				id = res.id ?? '';
				createdName = res.name ?? name.trim();
			}

			triggerElement?.focus();
			onCreated(id, createdName);
		} catch (err) {
			if (err instanceof ApiError && err.status === 409) {
				nameError = t(`referenceCreate.${type}.duplicateError`);
			} else {
				submitError = t(`referenceCreate.${type}.createError`);
			}
		} finally {
			isSubmitting = false;
		}
	}

	const inputClass =
		'mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500';
	const inputErrorClass = ' border-danger-500 dark:border-danger-500';
</script>

<svelte:window onkeydown={handleKeydown} />

<!-- Backdrop -->
<div class="ti-modal-backdrop fixed inset-0 z-40" aria-hidden="true"></div>

<!-- Dialog — two root elements; Svelte 5 supports this natively -->
<div
	bind:this={dialogElement}
	class="fixed inset-0 z-50 flex items-center justify-center p-4"
	role="dialog"
	aria-modal="true"
	aria-labelledby="qc-dialog-title"
>
	<div class="ti-modal-surface w-full max-w-sm rounded-lg bg-white shadow-xl dark:bg-neutral-900">
		<!-- Header -->
		<div class="border-b border-neutral-200 px-6 py-4 dark:border-neutral-800">
			<h2 id="qc-dialog-title" class="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
				{t(`referenceCreate.${type}.dialogTitle`)}
			</h2>
		</div>

		<!-- Body -->
		<div class="space-y-4 px-6 py-4">
			<!-- Name field -->
			<div>
				<label for="qc-name" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
					{t('referenceCreate.nameLabel')}
					<span class="text-danger-600" aria-hidden="true">*</span>
				</label>
				<input
					id="qc-name"
					type="text"
					bind:value={name}
					placeholder={t(`referenceCreate.${type}.namePlaceholder`)}
					autocomplete="off"
					disabled={isSubmitting}
					aria-required="true"
					aria-invalid={nameError ? 'true' : undefined}
					aria-describedby={nameError ? 'qc-name-error' : undefined}
					class="{inputClass}{nameError ? inputErrorClass : ''}"
				/>
				{#if nameError}
					<p id="qc-name-error" role="alert" class="mt-1 text-sm text-danger-600 dark:text-danger-400">
						{nameError}
					</p>
				{/if}
			</div>

			<!-- Location type selector (location type only) -->
			{#if type === 'location'}
				<div>
					<label
						for="qc-location-type"
						class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
					>
						{t('referenceCreate.location.typeLabel')}
						<span class="text-danger-600" aria-hidden="true">*</span>
					</label>
					<select
						id="qc-location-type"
						bind:value={locationType}
						disabled={isSubmitting}
						aria-required="true"
						class="{inputClass}{locationTypeError ? inputErrorClass : ''}"
					>
						<option value="Home">{t('referenceCreate.location.typeHome')}</option>
						<option value="Storage">{t('referenceCreate.location.typeStorage')}</option>
						<option value="External">{t('referenceCreate.location.typeExternal')}</option>
					</select>
					{#if locationTypeError}
						<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{locationTypeError}</p>
					{/if}
				</div>
			{/if}

			<!-- Submit-level error (non-duplicate failures) -->
			{#if submitError}
				<p role="alert" class="text-sm text-danger-600 dark:text-danger-400">{submitError}</p>
			{/if}
		</div>

		<!-- Footer / action buttons -->
		<div
			class="flex justify-end gap-3 border-t border-neutral-200 px-6 py-4 dark:border-neutral-800"
		>
			<button
				type="button"
				onclick={handleCancel}
				disabled={isSubmitting}
				class="rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-neutral-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800"
			>
				{t('referenceCreate.cancel')}
			</button>

			<button
				type="button"
				onclick={handleSubmit}
				disabled={isSubmitting}
				class="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-500 dark:hover:bg-primary-600"
			>
				{#if isSubmitting}
					<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
						<circle
							class="opacity-25"
							cx="12"
							cy="12"
							r="10"
							stroke="currentColor"
							stroke-width="4"
						></circle>
						<path
							class="opacity-75"
							fill="currentColor"
							d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
						></path>
					</svg>
					{t('referenceCreate.submitting')}
				{:else}
					{t('referenceCreate.submit')}
				{/if}
			</button>
		</div>
	</div>
</div>

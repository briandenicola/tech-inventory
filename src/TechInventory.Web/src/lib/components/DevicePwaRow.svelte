<!--
	DevicePwaRow.svelte — F045 §5.4 / Drake D-F.

	One device per full-width row inside the installed-PWA device list
	(inset-grouped list, not the 2-up card grid). Line 1 is the device name;
	line 2 is `brand · model`. Status is intentionally absent from the row
	(#141) — it remains discoverable in the device detail surface and filters.
	The trailing ellipsis reuses `DeviceActionsMenu` verbatim — same props
	`DeviceDetailModal` passes it — via the shared `createDeviceRowActions`
	controller so permission rules and API calls aren't forked between the
	modal and the row.
-->
<script lang="ts">
	import { t } from '$lib/i18n';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import { lookupName } from '$lib/utils/deviceDisplay';
	import { createDeviceRowActions } from '$lib/utils/deviceRowActions.svelte';
	import { deviceStatusOptions } from '$lib/utils/deviceRetirement';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { CurrentUser } from '$lib/stores/auth';
	import DeviceActionsMenu from '$lib/components/DeviceActionsMenu.svelte';
	import ClaimOwnershipModal from '$lib/components/ClaimOwnershipModal.svelte';
	import ReleaseOwnershipModal from '$lib/components/ReleaseOwnershipModal.svelte';
	import BulkUpdateModal from '$lib/components/BulkUpdateModal.svelte';
	import DeleteDeviceModal from '$lib/components/DeleteDeviceModal.svelte';
	import AuditLogModal from '$lib/components/AuditLogModal.svelte';

	interface Props {
		device: DeviceResponse;
		selectable?: boolean;
		selected?: boolean;
		onToggleSelect?: (id: string) => void;
		onOpenDevice?: (deviceId: string) => void;
		currentUser: CurrentUser | null;
		/** Refresh hook — called after any row action mutates the device. */
		onChanged?: () => void;
	}

	let {
		device,
		selectable = false,
		selected = false,
		onToggleSelect,
		onOpenDevice,
		currentUser,
		onChanged
	}: Props = $props();

	const refData = $derived($referenceDataStore);
	const brandName = $derived(lookupName(refData.brands, device.brandId));
	const ownerName = $derived(lookupName(refData.owners, device.ownerId));

	// Omit the separator when either half is missing; render '—' only when both are.
	const lineTwoText = $derived.by(() => {
		const brand = brandName !== '—' ? brandName : '';
		const model = device.model?.trim() || '';
		if (brand && model) return `${brand} · ${model}`;
		return brand || model || '—';
	});

	const rowActions = createDeviceRowActions(
		() => device,
		() => currentUser,
		{ onChanged: () => onChanged?.() }
	);
	const statusOptions = $derived(deviceStatusOptions(t));

	function handleSelectToggle(event: Event) {
		event.stopPropagation();
		onToggleSelect?.(device.id);
	}

	function openDevice() {
		onOpenDevice?.(device.id);
	}
</script>

<div class="pwa-row w-full" class:pwa-row--selectable={selectable}>
	{#if selectable}
		<label class="flex h-11 w-11 shrink-0 cursor-pointer items-center justify-center">
			<input
				type="checkbox"
				class="h-5 w-5 cursor-pointer rounded border-neutral-300 text-primary-600 focus-visible:ring-primary-500 dark:border-neutral-600 dark:bg-neutral-800"
				checked={selected}
				onchange={handleSelectToggle}
				aria-label={t('devices.bulk.selectRow', { name: device.name || device.id })}
			/>
		</label>
	{/if}

	{#snippet rowBody()}
		<span
			class="w-full truncate text-base font-semibold leading-tight text-neutral-900 dark:text-neutral-50"
		>
			{device.name || '—'}
		</span>
		<span
			class="flex w-full min-w-0 items-center gap-2 text-sm leading-snug"
		>
			<span class="min-w-0 truncate text-neutral-600 dark:text-neutral-400">{lineTwoText}</span>
		</span>
	{/snippet}

	<!--
		F045 follow-up: when no onOpenDevice callback is supplied, this must be
		a real <a href> (as DeviceTableCards' equivalent fallback already is),
		not a <button> faking navigation via window.location.href — that lost
		middle-click/ctrl-click "open in new tab", right-click "copy link", and
		link semantics for assistive tech.
	-->
	{#if onOpenDevice}
		<button type="button" class="flex min-w-0 flex-col items-start gap-0.5 text-left" onclick={openDevice}>
			{@render rowBody()}
		</button>
	{:else}
		<a href={`/devices/${device.id}`} class="flex min-w-0 flex-col items-start gap-0.5 text-left">
			{@render rowBody()}
		</a>
	{/if}

	<div class="self-center">
		<DeviceActionsMenu
			compact
			editHref={rowActions.canEdit ? `/devices/${device.id}/edit` : undefined}
			cloneHref={rowActions.canEdit ? `/devices/new?cloneFrom=${device.id}` : undefined}
			onClaim={rowActions.canClaim ? () => (rowActions.openModal = 'claim') : undefined}
			onRelease={rowActions.canRelease ? () => (rowActions.openModal = 'release') : undefined}
			onChangeStatus={rowActions.canChangeStatus ? () => (rowActions.openModal = 'changeStatus') : undefined}
			onViewHistory={rowActions.canViewHistory ? () => (rowActions.openModal = 'history') : undefined}
			onDelete={rowActions.canDelete ? () => (rowActions.openModal = 'delete') : undefined}
		/>
	</div>
</div>

{#if rowActions.openModal === 'claim'}
	<ClaimOwnershipModal
		deviceName={device.name ?? 'Device'}
		currentOwnerName={ownerName !== '—' ? ownerName : null}
		onConfirm={rowActions.handleClaim}
		onCancel={() => (rowActions.openModal = null)}
	/>
{:else if rowActions.openModal === 'release'}
	<ReleaseOwnershipModal
		deviceName={device.name ?? 'Device'}
		onConfirm={rowActions.handleRelease}
		onCancel={() => (rowActions.openModal = null)}
	/>
{:else if rowActions.openModal === 'changeStatus'}
	<BulkUpdateModal
		field="status"
		count={1}
		options={statusOptions}
		initialValue={device.status ?? ''}
		onConfirm={(value) => rowActions.handleChangeStatus(value)}
		onCancel={() => (rowActions.openModal = null)}
	/>
{:else if rowActions.openModal === 'delete'}
	<DeleteDeviceModal
		deviceName={device.name ?? 'Device'}
		onConfirm={rowActions.handleDelete}
		onCancel={() => (rowActions.openModal = null)}
	/>
{:else if rowActions.openModal === 'history'}
	<AuditLogModal
		isOpen={true}
		onClose={() => (rowActions.openModal = null)}
		entityId={device.id}
		entityType="Device"
		auditSummary={{
			createdAt: device.createdAt,
			createdBy: device.createdBy,
			modifiedAt: device.modifiedAt,
			modifiedBy: device.modifiedBy
		}}
	/>
{/if}

<style>
	.pwa-row {
		position: relative;
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto; /* [text] [kebab] */
		align-items: center;
		column-gap: var(--space-3);
		min-height: 4rem; /* 68px */
		padding: var(--space-3) var(--space-4);
	}

	/* When selection mode is active, prepend the fixed-width checkbox column. */
	.pwa-row--selectable {
		grid-template-columns: auto minmax(0, 1fr) auto; /* [checkbox] [text] [kebab] */
	}

	/*
		Round the first and last rows to match the parent card's border-radius.
		The container uses rounded-2xl without overflow-hidden (overflow-hidden
		was removed so the actions dropdown can escape the card boundaries).
		These selectors recreate the clipping that overflow-hidden provided.
		rounded-2xl = 1rem = var(--radius-lg) in this project's token scale.
	*/
	:global(.pwa-row:first-child) {
		border-radius: var(--radius-lg) var(--radius-lg) 0 0;
	}
	:global(.pwa-row:last-child) {
		border-radius: 0 0 var(--radius-lg) var(--radius-lg);
	}
	:global(.pwa-row:only-child) {
		border-radius: var(--radius-lg);
	}

	/*
		Inset hairline — aligns with the text column, not the container edge.
		Each row is its own component instance, so the sibling relationship
		spans two DevicePwaRow instances rather than two elements declared in
		this file; :global() keeps the selector correctly matching at runtime
		while telling svelte-check not to flag it as unused within this file.
	*/
	:global(.pwa-row + .pwa-row)::before {
		content: '';
		position: absolute;
		inset-inline-start: var(--space-4);
		inset-inline-end: 0;
		top: 0;
		height: 1px;
		background: var(--color-border);
	}
</style>

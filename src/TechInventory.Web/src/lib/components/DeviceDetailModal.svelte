<script lang="ts">
	import { fly } from 'svelte/transition';
	import { t } from '$lib/i18n';
	import { devices } from '$lib/api/client';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import { authStore } from '$lib/stores/auth';
	import { showToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import DeleteDeviceModal from '$lib/components/DeleteDeviceModal.svelte';
	import ClaimOwnershipModal from '$lib/components/ClaimOwnershipModal.svelte';
	import ReleaseOwnershipModal from '$lib/components/ReleaseOwnershipModal.svelte';
	import RetireDeviceModal from '$lib/components/RetireDeviceModal.svelte';
	import AuditLogModal from '$lib/components/AuditLogModal.svelte';
	import DeviceActionsMenu from '$lib/components/DeviceActionsMenu.svelte';
	import DeviceDetailFields from '$lib/components/DeviceDetailFields.svelte';
	import {
		buildRetireDeviceRequest,
		buildUnretireDeviceRequest,
		canRetireDevice,
		canUnretireDevice
	} from '$lib/utils/deviceRetirement';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { components } from '$lib/api/generated/types';

	type TagResponse = components['schemas']['TagResponse'];

	interface Props {
		deviceId: string;
		onClose: () => void;
		onChanged?: () => void;
	}

	let { deviceId, onClose, onChanged }: Props = $props();

	const currentUser = $derived($authStore.currentUser);
	const refData = $derived($referenceDataStore);

	let device = $state<DeviceResponse | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let notFound = $state(false);
	let deviceTags = $state<TagResponse[]>([]);

	let showDeleteModal = $state(false);
	let showClaimModal = $state(false);
	let showReleaseModal = $state(false);
	let showRetireModal = $state(false);
	let showHistoryDrawer = $state(false);

	let dialogElement = $state<HTMLDivElement | null>(null);

	const canEdit = $derived(currentUser?.role === 'Admin' || currentUser?.role === 'Member');
	const canDelete = $derived(currentUser?.role === 'Admin');
	const canViewHistory = $derived(currentUser?.role === 'Admin');
	const canClaim = $derived(device && currentUser && device.ownerId !== currentUser.id);
	const canRelease = $derived(device && currentUser && device.ownerId === currentUser.id);
	const canRetire = $derived(canRetireDevice(device, currentUser));
	const canUnretire = $derived(canUnretireDevice(device, currentUser));

	async function fetchDevice() {
		isLoading = true;
		error = null;
		notFound = false;
		deviceTags = [];

		try {
			const [deviceResult, tagsResult] = await Promise.all([
				devices.get(deviceId),
				devices.listTags(deviceId).catch((tagErr) => {
					console.error('[DeviceDetailModal] Tag fetch failed:', tagErr);
					return [] as TagResponse[];
				})
			]);

			device = deviceResult as DeviceResponse;
			deviceTags = tagsResult ?? [];
		} catch (err) {
			console.error('[DeviceDetailModal] Fetch failed:', err);
			if (
				err instanceof Error &&
				'status' in err &&
				(err as unknown as { status: number }).status === 404
			) {
				notFound = true;
			} else {
				error = err instanceof Error ? err.message : 'Failed to load device';
			}
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		void deviceId;
		void fetchDevice();
	});

	const brandName = $derived.by(() => {
		const brandId = device?.brandId;
		return brandId
			? (refData.brands.find((brand) => brand.id === brandId)?.name ?? 'Unknown')
			: '—';
	});
	const categoryName = $derived.by(() => {
		const categoryId = device?.categoryId;
		return categoryId
			? (refData.categories.find((category) => category.id === categoryId)?.name ?? 'Unknown')
			: '—';
	});
	const ownerName = $derived.by(() => {
		const ownerId = device?.ownerId;
		return ownerId
			? (refData.owners.find((owner) => owner.id === ownerId)?.name ?? 'Unknown')
			: '—';
	});
	const locationName = $derived.by(() => {
		const locationId = device?.locationId;
		return locationId
			? (refData.locations.find((location) => location.id === locationId)?.name ?? 'Unknown')
			: '—';
	});
	const networkName = $derived.by(() => {
		const networkId = device?.networkId;
		return networkId
			? (refData.networks.find((network) => network.id === networkId)?.name ?? 'Unknown')
			: '—';
	});

	function getStatusColor(status: string | null): string {
		switch (status) {
			case 'Active':
				return 'bg-success-100 text-success-800 dark:bg-success-900 dark:text-success-100';
			case 'Retired':
				return 'bg-warning-100 text-warning-800 dark:bg-warning-900 dark:text-warning-100';
			case 'Disposed':
				return 'bg-neutral-200 text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200';
			default:
				return 'bg-neutral-100 text-neutral-700 dark:bg-neutral-900 dark:text-neutral-300';
		}
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}

	async function handleDelete(reason: string) {
		if (!device) return;

		try {
			await devices.delete(device.id, reason);
			invalidateDevicesCache();
			showToast({ type: 'success', message: t('devices.delete.success') });
			onChanged?.();
			onClose();
		} catch (err) {
			console.error('[DeviceDetailModal] Delete failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to delete device';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showDeleteModal = false;
		}
	}

	async function handleClaimOwnership() {
		if (!device || !currentUser) return;

		try {
			await devices.updateOwner(device.id, currentUser.id);
			invalidateDevicesCache();
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.claim.toast.success').replace('{name}', device.name ?? 'Device')
			});
			onChanged?.();
		} catch (err) {
			console.error('[DeviceDetailModal] Claim failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to claim ownership';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showClaimModal = false;
		}
	}

	async function handleReleaseOwnership() {
		if (!device) return;

		try {
			await devices.updateOwner(device.id, null);
			invalidateDevicesCache();
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.release.toast.success').replace('{name}', device.name ?? 'Device')
			});
			onChanged?.();
		} catch (err) {
			console.error('[DeviceDetailModal] Release failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to release ownership';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showReleaseModal = false;
		}
	}

	async function handleRetire() {
		if (!device) return;

		try {
			await devices.update(device.id, buildRetireDeviceRequest(device, new Date().toISOString()));
			invalidateDevicesCache();
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.retire.toast.success').replace('{name}', device.name ?? 'Device')
			});
			onChanged?.();
		} catch (err) {
			console.error('[DeviceDetailModal] Retire failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to retire device';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showRetireModal = false;
		}
	}

	async function handleUnretire() {
		if (!device) return;

		try {
			await devices.update(device.id, buildUnretireDeviceRequest(device));
			invalidateDevicesCache();
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.unretire.toast.success').replace('{name}', device.name ?? 'Device')
			});
			onChanged?.();
		} catch (err) {
			console.error('[DeviceDetailModal] Unretire failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to unretire device';
			showToast({ type: 'error', message: errorMsg });
		}
	}

	function handleViewHistory() {
		if (device) {
			showHistoryDrawer = true;
		}
	}

	function trapFocus(event: KeyboardEvent) {
		if (event.key !== 'Tab' || !dialogElement) {
			return;
		}

		const focusableElements = Array.from(
			dialogElement.querySelectorAll<HTMLElement>(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			)
		).filter((element) => !element.hasAttribute('disabled'));

		const firstElement = focusableElements[0];
		const lastElement = focusableElements[focusableElements.length - 1];

		if (!firstElement || !lastElement) {
			return;
		}

		if (event.shiftKey && document.activeElement === firstElement) {
			event.preventDefault();
			lastElement.focus();
		} else if (!event.shiftKey && document.activeElement === lastElement) {
			event.preventDefault();
			firstElement.focus();
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (
			event.key === 'Escape' &&
			!showDeleteModal &&
			!showClaimModal &&
			!showReleaseModal &&
			!showRetireModal &&
			!showHistoryDrawer
		) {
			onClose();
		}
	}

	$effect(() => {
		const previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';

		dialogElement?.addEventListener('keydown', trapFocus);
		dialogElement
			?.querySelector<HTMLElement>(
				'[data-dialog-initial-focus], button, [href], input, select, textarea'
			)
			?.focus();

		return () => {
			document.body.style.overflow = previousOverflow;
			dialogElement?.removeEventListener('keydown', trapFocus);
		};
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="fixed inset-0" style="z-index: var(--z-modal-backdrop);">
	<button
		type="button"
		aria-label={t('common.actions.close')}
		class="ti-modal-backdrop absolute inset-0 h-full w-full cursor-default"
		onclick={onClose}
	></button>

	<div
		class="pointer-events-none relative flex min-h-full items-start justify-center pt-[env(safe-area-inset-top,0px)] sm:items-center sm:px-4 sm:py-10"
	>
		<div
			bind:this={dialogElement}
			transition:fly|local={{ y: 32, duration: 180 }}
			class="ti-modal-surface pointer-events-auto relative flex h-[calc(100dvh-env(safe-area-inset-top,0px))] w-full flex-col overflow-hidden rounded-t-3xl bg-white shadow-2xl dark:bg-neutral-950 sm:h-auto sm:max-h-[85dvh] sm:max-w-4xl sm:rounded-2xl"
			role="dialog"
			aria-modal="true"
			aria-labelledby="device-detail-modal-title"
			tabindex="-1"
			style="z-index: var(--z-modal);"
		>
			<div
				class="shrink-0 sticky top-0 z-10 flex items-start justify-between gap-3 border-b border-neutral-200/70 bg-white/95 px-5 py-4 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-950/95 sm:px-6 sm:py-5"
			>
				<div class="min-w-0 flex-1">
					<p
						class="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400"
					>
						{t('devices.detail.title')}
					</p>
					<h2
						id="device-detail-modal-title"
						class="mt-1 truncate text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50 sm:text-2xl"
					>
						{device?.name ?? t('devices.detail.title')}
					</h2>
					{#if device && brandName !== '—'}
						<p class="mt-1 text-sm text-neutral-600 dark:text-neutral-400">{brandName}</p>
					{/if}
				</div>
				<div class="flex items-center gap-2">
					{#if device && !isLoading}
						<DeviceActionsMenu
							editHref={canEdit ? `/devices/${device.id}/edit` : undefined}
							onClaim={canClaim ? () => (showClaimModal = true) : undefined}
							onRelease={canRelease ? () => (showReleaseModal = true) : undefined}
							onRetire={canRetire ? () => (showRetireModal = true) : undefined}
							onUnretire={canUnretire ? handleUnretire : undefined}
							onViewHistory={canViewHistory ? handleViewHistory : undefined}
							onDelete={canDelete ? () => (showDeleteModal = true) : undefined}
						/>
					{/if}
					<button
						type="button"
						onclick={onClose}
						class="inline-flex h-11 w-11 items-center justify-center rounded-full text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
						aria-label={t('common.actions.close')}
						data-dialog-initial-focus
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
			</div>

			<div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 py-5 sm:px-6 sm:py-6">
				{#if isLoading}
					<LoadingSkeleton rows={6} />
				{:else if notFound}
					<div
						class="rounded-2xl border border-warning-200 bg-warning-50 p-8 text-center dark:border-warning-900 dark:bg-warning-950"
					>
						<h3 class="text-base font-semibold text-warning-900 dark:text-warning-100">
							{t('devices.detail.notFoundTitle')}
						</h3>
						<p class="mt-2 text-sm text-warning-700 dark:text-warning-300">
							{t('devices.detail.notFoundDescription')}
						</p>
					</div>
				{:else if error}
					<ErrorState {error} onRetry={fetchDevice} />
				{:else if device}
					<DeviceDetailFields
						{device}
						{brandName}
						{categoryName}
						{ownerName}
						{locationName}
						{networkName}
						{deviceTags}
						statusClass={getStatusColor(device.status)}
						{formatDate}
						{formatDateTime}
					/>
				{/if}
			</div>
		</div>
	</div>
</div>

{#if showDeleteModal && device}
	<DeleteDeviceModal
		deviceName={device.name ?? 'Device'}
		onConfirm={handleDelete}
		onCancel={() => (showDeleteModal = false)}
	/>
{/if}

{#if showClaimModal && device}
	<ClaimOwnershipModal
		deviceName={device.name ?? 'Device'}
		currentOwnerName={ownerName !== '—' ? ownerName : null}
		onConfirm={handleClaimOwnership}
		onCancel={() => (showClaimModal = false)}
	/>
{/if}

{#if showReleaseModal && device}
	<ReleaseOwnershipModal
		deviceName={device.name ?? 'Device'}
		onConfirm={handleReleaseOwnership}
		onCancel={() => (showReleaseModal = false)}
	/>
{/if}

{#if showRetireModal && device}
	<RetireDeviceModal
		deviceName={device.name ?? 'Device'}
		onConfirm={handleRetire}
		onCancel={() => (showRetireModal = false)}
	/>
{/if}

{#if showHistoryDrawer && device}
	<AuditLogModal
		isOpen={showHistoryDrawer}
		onClose={() => (showHistoryDrawer = false)}
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

<!--
	DeviceDetailModal.svelte — Modal wrapper around the device detail view.

	Mirrors the Apple-elegant chrome from AddDeviceModal (rounded-2xl card,
	shadow-2xl, backdrop-blur, sticky header with close X, scrollable body
	capped at viewport height, Escape + backdrop click to close).

	Owns its own fetch + Claim/Release/Delete state. Edit still navigates to
	/devices/{id}/edit because that's a multi-step form better suited to a
	full page; cancelling there comes back to /devices?device={id} so the
	detail modal re-opens automatically.

	Props:
	- deviceId: string — which device to load
	- onClose:  () => void — Cancel, Escape, X, backdrop click
	- onChanged?: () => void — fired after a successful mutation
	                          (delete / claim / release) so the parent list
	                          can refetch.
-->
<script lang="ts">
	import { goto } from '$app/navigation';
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
	import DeviceAuditHistoryDrawer from '$lib/components/admin/DeviceAuditHistoryDrawer.svelte';
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

	// F030: tags assigned to this device, loaded alongside the device record.
	let deviceTags = $state<TagResponse[]>([]);

	let showDeleteModal = $state(false);
	let showClaimModal = $state(false);
	let showReleaseModal = $state(false);
	let showHistoryDrawer = $state(false);

	let dialogElement = $state<HTMLDivElement | undefined>(undefined);

	const canEdit = $derived(currentUser?.role === 'Admin' || currentUser?.role === 'Member');
	const canDelete = $derived(currentUser?.role === 'Admin');
	const canViewHistory = $derived(currentUser?.role === 'Admin');
	const canClaim = $derived(device && currentUser && device.ownerId !== currentUser.id);
	const canRelease = $derived(device && currentUser && device.ownerId === currentUser.id);

	async function fetchDevice() {
		isLoading = true;
		error = null;
		notFound = false;
		deviceTags = [];

		try {
			const [deviceResult, tagsResult] = await Promise.all([
				devices.get(deviceId),
				devices.listTags(deviceId).catch((tagErr) => {
					// F030: tag fetch failure is non-fatal; log and show empty.
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
		// Re-fetch whenever the modal is opened with a different deviceId.
		void deviceId;
		void fetchDevice();
	});

	const brandName = $derived(
		device?.brandId
			? (refData.brands.find((b) => b.id === device!.brandId)?.name ?? 'Unknown')
			: '—'
	);
	const categoryName = $derived(
		device?.categoryId
			? (refData.categories.find((c) => c.id === device!.categoryId)?.name ?? 'Unknown')
			: '—'
	);
	const ownerName = $derived(
		device?.ownerId
			? (refData.owners.find((o) => o.id === device!.ownerId)?.name ?? 'Unknown')
			: '—'
	);
	const locationName = $derived(
		device?.locationId
			? (refData.locations.find((l) => l.id === device!.locationId)?.name ?? 'Unknown')
			: '—'
	);
	const networkName = $derived(
		device?.networkId
			? (refData.networks.find((n) => n.id === device!.networkId)?.name ?? 'Unknown')
			: '—'
	);

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
			showToast({ type: 'success', message: `${device.name ?? 'Device'} deleted successfully` });
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

	function handleEdit() {
		if (device) {
			goto(`/devices/${device.id}/edit`);
		}
	}

	function handleViewHistory() {
		// F026: keep the audit history inside the device context. Brian's PWA
		// field test (2026-05-19) flagged the old "navigate to /admin/audit"
		// behaviour as a context-loss bug; the standalone admin route is still
		// available for cross-entity browsing.
		if (device) {
			showHistoryDrawer = true;
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		// Don't swallow Escape when a nested modal is open — let them close first.
		if (
			e.key === 'Escape' &&
			!showDeleteModal &&
			!showClaimModal &&
			!showReleaseModal &&
			!showHistoryDrawer
		) {
			onClose();
		}
	}

	$effect(() => {
		const previousOverflow = document.body.style.overflow;
		document.body.style.overflow = 'hidden';

		if (dialogElement) {
			dialogElement.focus();
		}

		return () => {
			document.body.style.overflow = previousOverflow;
		};
	});
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="fixed inset-0 z-50">
	<button
		type="button"
		aria-label={t('common.actions.close')}
		class="absolute inset-0 h-full w-full cursor-default bg-black/60 backdrop-blur-sm"
		onclick={onClose}
	></button>

	<div class="pointer-events-none relative h-full overflow-y-auto">
		<div class="flex min-h-full items-start justify-center px-4 py-10 sm:py-16">
			<div
				bind:this={dialogElement}
				class="pointer-events-auto relative w-full max-w-3xl overflow-hidden rounded-2xl bg-white shadow-2xl dark:bg-neutral-900"
				role="dialog"
				aria-modal="true"
				aria-labelledby="device-detail-modal-title"
				tabindex="-1"
			>
				<!-- Sticky header -->
				<div
					class="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-neutral-200/70 bg-white/95 px-6 py-5 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-900/95"
				>
					<div class="min-w-0 flex-1">
						<h2
							id="device-detail-modal-title"
							class="truncate text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
						>
							{device?.name ?? t('devices.detail.title')}
						</h2>
						{#if device}
							<div class="mt-2 flex flex-wrap items-center gap-2">
								<span
									class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {getStatusColor(
										device.status
									)}"
								>
									{device.status ?? 'Unknown'}
								</span>
								{#if brandName !== '—'}
									<span class="text-sm text-neutral-600 dark:text-neutral-400">{brandName}</span>
								{/if}
							</div>
						{/if}
					</div>
					<button
						type="button"
						onclick={onClose}
						class="inline-flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-full text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-100"
						aria-label={t('common.actions.close')}
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

				<!-- Body -->
				<div class="px-6 py-6">
					{#if isLoading}
						<LoadingSkeleton rows={5} />
					{:else if notFound}
						<div
							class="rounded-xl border border-warning-200 bg-warning-50 p-8 text-center dark:border-warning-900 dark:bg-warning-950"
						>
							<h3 class="text-base font-semibold text-warning-900 dark:text-warning-100">
								Device Not Found
							</h3>
							<p class="mt-2 text-sm text-warning-700 dark:text-warning-300">
								This device does not exist or has been deleted.
							</p>
						</div>
					{:else if error}
						<ErrorState {error} onRetry={fetchDevice} />
					{:else if device}
						<dl class="grid gap-x-6 gap-y-5 sm:grid-cols-2">
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.serial')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">
									{device.serialNumber ?? '—'}
								</dd>
							</div>
							{#if device.model}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.model')}
									</dt>
									<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{device.model}</dd>
								</div>
							{/if}
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.brand')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{brandName}</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.category')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{categoryName}</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.owner')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{ownerName}</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.location')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{locationName}</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.network')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{networkName}</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.purchaseDate')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">
									{formatDate(device.purchaseDate)}
								</dd>
							</div>
							<div>
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.purchasePrice')}
								</dt>
								<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">
									{#if device.purchasePrice !== null && device.currencyCode}
										{device.currencyCode} {device.purchasePrice.toFixed(2)}
									{:else}
										—
									{/if}
								</dd>
							</div>
							<!--
								F034: mirror the detail page so the modal view (deep-linked
								via /devices?device=:id) exposes the same imported fields.
								Each row is truthy-gated so devices without OS/IP/etc. stay
								scannable.
							-->
							{#if device.operatingSystem}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.operatingSystem')}
									</dt>
									<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{device.operatingSystem}</dd>
								</div>
							{/if}
							{#if device.version}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.version')}
									</dt>
									<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{device.version}</dd>
								</div>
							{/if}
							{#if device.ipAddress}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.ipAddress')}
									</dt>
									<dd class="mt-1 font-mono text-base text-neutral-900 dark:text-neutral-100">{device.ipAddress}</dd>
								</div>
							{/if}
							{#if device.macAddress}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.macAddress')}
									</dt>
									<dd class="mt-1 font-mono text-base text-neutral-900 dark:text-neutral-100">{device.macAddress}</dd>
								</div>
							{/if}
							{#if device.productUrl}
								<div class="sm:col-span-2">
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.productUrl')}
									</dt>
									<dd class="mt-1 truncate text-base">
										<a
											href={device.productUrl}
											target="_blank"
											rel="noopener noreferrer"
											class="text-primary-600 hover:text-primary-500 hover:underline dark:text-primary-400 dark:hover:text-primary-300"
										>
											{device.productUrl}
										</a>
									</dd>
								</div>
							{/if}
							{#if device.retiredDate}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.retiredDate')}
									</dt>
									<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">
										{formatDate(device.retiredDate)}
									</dd>
								</div>
							{/if}
							{#if device.disposalMethod}
								<div>
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.disposalMethod')}
									</dt>
									<dd class="mt-1 text-base text-neutral-900 dark:text-neutral-100">{device.disposalMethod}</dd>
								</div>
							{/if}
							{#if device.purpose}
								<div class="sm:col-span-2">
									<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
										{t('devices.columns.purpose')}
									</dt>
									<dd class="mt-1 whitespace-pre-wrap text-base text-neutral-900 dark:text-neutral-100">
										{device.purpose}
									</dd>
								</div>
							{/if}
						</dl>

						<!--
							F030: per-device tag chips. Rendered between the property
							grid and the notes block so they read as a property of the
							device. Empty state is intentionally muted, not hidden,
							so users notice the affordance and click Edit to add tags.
						-->
						<div class="mt-6">
							<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
								{t('devices.tags.label')}
							</dt>
							<dd class="mt-2">
								{#if deviceTags.length > 0}
									<ul class="flex flex-wrap gap-2" aria-label={t('devices.tags.label')}>
										{#each deviceTags as tag (tag.id)}
											<li
												class="inline-flex items-center gap-1.5 rounded-full bg-neutral-100 px-3 py-1 text-sm font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200"
											>
												{#if tag.color}
													<span
														class="inline-block h-2 w-2 rounded-full"
														style="background-color: {tag.color};"
														aria-hidden="true"
													></span>
												{/if}
												{tag.name}
											</li>
										{/each}
									</ul>
								{:else}
									<p class="text-sm text-neutral-500 dark:text-neutral-400">
										{t('devices.tags.empty')}
									</p>
								{/if}
							</dd>
						</div>

						{#if device.notes}
							<div class="mt-6">
								<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
									{t('devices.columns.notes')}
								</dt>
								<dd
									class="mt-1 whitespace-pre-wrap text-base text-neutral-900 dark:text-neutral-100"
								>
									{device.notes}
								</dd>
							</div>
						{/if}

						<div class="mt-8 border-t border-neutral-200 pt-5 dark:border-neutral-800">
							<h3 class="text-sm font-semibold text-neutral-600 dark:text-neutral-400">
								Audit Trail
							</h3>
							<dl class="mt-2 space-y-1 text-sm text-neutral-600 dark:text-neutral-400">
								<div>
									<span class="font-medium">Created:</span>
									<time datetime={device.createdAt} title={formatDateTime(device.createdAt)}>
										{formatDateTime(device.createdAt)}
									</time>
									{#if device.createdBy}<span> by {device.createdBy}</span>{/if}
								</div>
								<div>
									<span class="font-medium">Last Modified:</span>
									<time datetime={device.modifiedAt} title={formatDateTime(device.modifiedAt)}>
										{formatDateTime(device.modifiedAt)}
									</time>
									{#if device.modifiedBy}<span> by {device.modifiedBy}</span>{/if}
								</div>
							</dl>
						</div>
					{/if}
				</div>

				{#if device && !isLoading}
					<!-- Sticky footer with role-aware actions -->
					<div
						class="sticky bottom-0 z-10 flex flex-wrap items-center justify-end gap-2 border-t border-neutral-200/70 bg-white/95 px-6 py-4 backdrop-blur-md dark:border-neutral-800/70 dark:bg-neutral-900/95"
					>
						{#if canClaim}
							<button
								type="button"
								onclick={() => (showClaimModal = true)}
								class="inline-flex min-h-11 items-center gap-2 rounded-full border border-primary-600 px-4 py-2 text-sm font-medium text-primary-600 transition-colors hover:bg-primary-50 dark:border-primary-500 dark:text-primary-400 dark:hover:bg-neutral-800"
							>
								{t('devices.detail.claimButton')}
							</button>
						{/if}
						{#if canRelease}
							<button
								type="button"
								onclick={() => (showReleaseModal = true)}
								class="inline-flex min-h-11 items-center gap-2 rounded-full border border-warning-600 px-4 py-2 text-sm font-medium text-warning-600 transition-colors hover:bg-warning-50 dark:border-warning-500 dark:text-warning-400 dark:hover:bg-neutral-800"
							>
								{t('devices.detail.releaseButton')}
							</button>
						{/if}
						{#if canViewHistory}
							<button
								type="button"
								onclick={handleViewHistory}
								class="inline-flex min-h-11 items-center gap-2 rounded-full border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 dark:border-neutral-700 dark:text-neutral-300 dark:hover:bg-neutral-800"
							>
								{t('admin.audit.link.viewHistory')}
							</button>
						{/if}
						{#if canEdit}
							<button
								type="button"
								onclick={handleEdit}
								class="inline-flex min-h-11 items-center gap-2 rounded-full border border-primary-600 px-4 py-2 text-sm font-medium text-primary-600 transition-colors hover:bg-primary-50 dark:border-primary-500 dark:text-primary-400 dark:hover:bg-neutral-800"
							>
								{t('common.actions.edit')}
							</button>
						{/if}
						{#if canDelete}
							<button
								type="button"
								onclick={() => (showDeleteModal = true)}
								class="inline-flex min-h-11 items-center gap-2 rounded-full border border-danger-600 px-4 py-2 text-sm font-medium text-danger-600 transition-colors hover:bg-danger-50 dark:border-danger-500 dark:text-danger-400 dark:hover:bg-neutral-800"
							>
								{t('common.actions.delete')}
							</button>
						{/if}
					</div>
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

{#if showHistoryDrawer && device}
	<DeviceAuditHistoryDrawer
		deviceId={device.id}
		onClose={() => (showHistoryDrawer = false)}
	/>
{/if}

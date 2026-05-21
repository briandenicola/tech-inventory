<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { devices } from '$lib/api/client';
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import { authStore } from '$lib/stores/auth';
	import { showToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import DeleteDeviceModal from '$lib/components/DeleteDeviceModal.svelte';
	import ClaimOwnershipModal from '$lib/components/ClaimOwnershipModal.svelte';
	import ReleaseOwnershipModal from '$lib/components/ReleaseOwnershipModal.svelte';
	import AuditLogModal from '$lib/components/AuditLogModal.svelte';
	import DeviceActionsMenu from '$lib/components/DeviceActionsMenu.svelte';
	import DeviceDetailFields from '$lib/components/DeviceDetailFields.svelte';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';

	/**
	 * T19: Device detail page — all fields, resolved references, breadcrumbs, role-aware Edit/Delete buttons
	 * T24: Claim Ownership button + modal (visible if device unowned or owned by another)
	 * T25: Release Ownership button + modal (visible if current user IS owner)
	 *
	 * States: loading → success/error/notFound
	 * Breadcrumbs: Home > Devices > {Device Name}
	 * Edit button: visible to Admin + Member
	 * Delete button: visible to Admin only
	 * Claim button: visible when device.ownerId !== currentUser.id
	 * Release button: visible when device.ownerId === currentUser.id
	 *
	 * Related: specs/002-frontend-mvp/spec.md J5, J9
	 */

	const deviceId = $derived($page.params.id);
	const currentUser = $derived($authStore.currentUser);
	const refData = $derived($referenceDataStore);

	// Device state
	let device = $state<DeviceResponse | null>(null);
	let deviceTags = $state<Array<{ id: string; name: string; color: string | null }>>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let notFound = $state(false);

	// Modal state
	let showDeleteModal = $state(false);
	let showClaimModal = $state(false);
	let showReleaseModal = $state(false);
	let showAuditLogModal = $state(false);

	// Role checks
	const canEdit = $derived(currentUser?.role === 'Admin' || currentUser?.role === 'Member');
	const canDelete = $derived(currentUser?.role === 'Admin');
	const canViewHistory = $derived(currentUser?.role === 'Admin');

	// Ownership checks (T24, T25)
	// Claim: visible when device unowned OR owned by another user
	const canClaim = $derived(device && currentUser && device.ownerId !== currentUser.id);
	// Release: visible when current user IS the owner
	const canRelease = $derived(device && currentUser && device.ownerId === currentUser.id);

	// Fetch device
	async function fetchDevice() {
		if (!deviceId) return;

		isLoading = true;
		error = null;
		notFound = false;

		try {
			const [deviceResult, tagResults] = await Promise.all([
				devices.get(deviceId),
				devices.listTags(deviceId)
			]);
			device = deviceResult as DeviceResponse;
			deviceTags = tagResults
				.filter(
					(tag): tag is { id: string; name: string; color?: string | null } =>
						!!tag.id && !!tag.name
				)
				.map((tag) => ({
					id: tag.id,
					name: tag.name,
					color: tag.color ?? null
				}));
		} catch (err) {
			console.error('[device-detail] Fetch failed:', err);
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

	onMount(() => {
		void fetchReferenceData();
	});

	$effect(() => {
		void fetchDevice();
	});

	// Resolve reference data (brand, category, owner, location, network names)
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

	// Status badge color
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

	// Format date (relative + absolute tooltip)
	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		const date = new Date(dateStr);
		return date.toLocaleDateString('en-US', {
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

	// Handle delete
	async function handleDelete(reason: string) {
		if (!device) return;

		try {
			await devices.delete(device.id, reason);
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: `${device.name ?? 'Device'} deleted successfully`
			});
			goto('/devices');
		} catch (err) {
			console.error('[device-detail] Delete failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to delete device';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showDeleteModal = false;
		}
	}

	// Handle claim ownership (T24)
	async function handleClaimOwnership() {
		if (!device || !currentUser) return;

		try {
			await devices.updateOwner(device.id, currentUser.id);
			invalidateDevicesCache();
			// Refetch device detail to show updated owner
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.claim.toast.success').replace('{name}', device.name ?? 'Device')
			});
		} catch (err) {
			console.error('[device-detail] Claim ownership failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to claim ownership';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showClaimModal = false;
		}
	}

	// Handle release ownership (T25)
	async function handleReleaseOwnership() {
		if (!device) return;

		try {
			await devices.updateOwner(device.id, null);
			invalidateDevicesCache();
			// Refetch device detail to show updated owner (now null)
			await fetchDevice();
			showToast({
				type: 'success',
				message: t('devices.release.toast.success').replace('{name}', device.name ?? 'Device')
			});
		} catch (err) {
			console.error('[device-detail] Release ownership failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to release ownership';
			showToast({ type: 'error', message: errorMsg });
		} finally {
			showReleaseModal = false;
		}
	}
</script>

<svelte:head>
	<title>{t('devices.detail.title')} — {t('app.title')}</title>
</svelte:head>

<!-- Breadcrumbs -->
<div class="pb-24">
<nav class="mb-4 flex text-sm text-neutral-600 dark:text-neutral-400" aria-label="Breadcrumb">
	<ol class="flex items-center space-x-2">
		<li>
			<a href="/" class="hover:text-primary-600 dark:hover:text-primary-400">
				{t('navigation.home')}
			</a>
		</li>
		<li>
			<svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
				<path
					fill-rule="evenodd"
					d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
					clip-rule="evenodd"
				/>
			</svg>
		</li>
		<li>
			<a href="/devices" class="hover:text-primary-600 dark:hover:text-primary-400">
				{t('common.nouns.devices')}
			</a>
		</li>
		<li>
			<svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
				<path
					fill-rule="evenodd"
					d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
					clip-rule="evenodd"
				/>
			</svg>
		</li>
		<li aria-current="page" class="font-medium text-neutral-900 dark:text-neutral-100">
			{device?.name ?? 'Device'}
		</li>
	</ol>
</nav>

<!-- Page header -->
<div class="mb-6 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
	<div class="min-w-0">
		<!--
			Mobile: large "Device Details" wrapped under the action row and felt
			cramped. Use the device name as the title (truncating gracefully) with
			a small "Device details" eyebrow, so the page identifies itself even
			before the device loads.
		-->
		<p class="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
			{t('devices.detail.title')}
		</p>
		<h1
			class="mt-1 text-xl font-bold tracking-tight text-neutral-900 dark:text-neutral-100 sm:text-2xl"
		>
			{device?.name ?? '—'}
		</h1>
	</div>

	{#if device && !isLoading}
		<div class="flex justify-end">
			<DeviceActionsMenu
				editHref={canEdit ? `/devices/${device.id}/edit` : undefined}
				onClaim={canClaim ? () => (showClaimModal = true) : undefined}
				onRelease={canRelease ? () => (showReleaseModal = true) : undefined}
				onViewHistory={canViewHistory ? () => (showAuditLogModal = true) : undefined}
				onDelete={canDelete ? () => (showDeleteModal = true) : undefined}
			/>
		</div>
	{/if}
</div>

<!-- Content -->
{#if isLoading}
	<LoadingSkeleton rows={5} />
{:else if notFound}
	<div
		class="rounded-lg border border-warning-200 bg-warning-50 p-12 text-center dark:border-warning-900 dark:bg-warning-950"
	>
		<svg
			class="mx-auto h-16 w-16 text-warning-600 dark:text-warning-400"
			fill="none"
			viewBox="0 0 24 24"
			stroke="currentColor"
			aria-hidden="true"
		>
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="1.5"
				d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
			/>
		</svg>
		<h2 class="mt-4 text-lg font-semibold text-warning-900 dark:text-warning-100">
			{t('devices.detail.notFoundTitle')}
		</h2>
		<p class="mt-2 text-sm text-warning-700 dark:text-warning-300">
			{t('devices.detail.notFoundDescription')}
		</p>
		<a
			href="/devices"
			class="mt-6 inline-flex items-center gap-2 rounded-lg bg-warning-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-warning-700 focus:outline-none focus:ring-2 focus:ring-warning-500 focus:ring-offset-2 dark:bg-warning-500 dark:hover:bg-warning-600"
		>
			{t('common.actions.back')} to Devices
		</a>
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

<!-- Delete confirmation modal -->
{#if showDeleteModal && device}
	<DeleteDeviceModal
		deviceName={device.name ?? 'Device'}
		onConfirm={handleDelete}
		onCancel={() => (showDeleteModal = false)}
	/>
{/if}

<!-- Claim ownership modal (T24) -->
{#if showClaimModal && device}
	<ClaimOwnershipModal
		deviceName={device.name ?? 'Device'}
		currentOwnerName={ownerName !== '—' ? ownerName : null}
		onConfirm={handleClaimOwnership}
		onCancel={() => (showClaimModal = false)}
	/>
{/if}

<!-- Release ownership modal (T25) -->
{#if showReleaseModal && device}
	<ReleaseOwnershipModal
		deviceName={device.name ?? 'Device'}
		onConfirm={handleReleaseOwnership}
		onCancel={() => (showReleaseModal = false)}
	/>
{/if}

{#if showAuditLogModal && device}
	<AuditLogModal
		isOpen={showAuditLogModal}
		onClose={() => (showAuditLogModal = false)}
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
</div>

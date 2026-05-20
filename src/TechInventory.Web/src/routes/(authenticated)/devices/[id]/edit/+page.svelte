<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { devices } from '$lib/api/client';
	import { showToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import DeviceForm from '$lib/components/DeviceForm.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceFormInput } from '$lib/schemas/device';

	/**
	 * T21: Device edit page — /devices/[id]/edit
	 * 
	 * Pre-populate form with existing device data. Retired devices show badge;
	 * only notes editable when retired. Submit → PUT /api/v1/devices/{id} → toast → redirect.
	 * 
	 * Related: specs/002-frontend-mvp/spec.md J7
	 */

	const deviceId = $derived($page.params.id);

	// Device state
	let device = $state<DeviceResponse | null>(null);
	let deviceTagIds = $state<string[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Fetch device
	async function fetchDevice() {
		if (!deviceId) return;

		isLoading = true;
		error = null;

		try {
			const [deviceResult, tagResults] = await Promise.all([
				devices.get(deviceId),
				devices.listTags(deviceId)
			]);
			device = deviceResult as DeviceResponse;
			deviceTagIds = tagResults
				.map((tag) => tag.id)
				.filter((tagId): tagId is string => typeof tagId === 'string' && tagId.length > 0);
		} catch (err) {
			console.error('[device-edit] Fetch failed:', err);
			error = err instanceof Error ? err.message : 'Failed to load device';
		} finally {
			isLoading = false;
		}
	}

	// Load device on mount
	$effect(() => {
		void fetchDevice();
	});


	// Retired device logic: disable all fields except notes
	const isRetired = $derived(device?.status === 'Retired');
	const disabledFields = $derived(
		isRetired
			? [
					'name',
					'serialNumber',
					'brandId',
					'categoryId',
					'ownerId',
					'locationId',
					'networkId',
					'tagIds',
					'purchaseDate',
					'purchasePrice',
					'currencyCode'
				]
			: []
	);

	// Handle submit
	async function handleSubmit(data: DeviceFormInput) {
		if (!device) return;

		try {
			const { tagIds, ...deviceData } = data;
			const payload = {
				...deviceData,
				ownerId: data.ownerId || undefined,
				locationId: data.locationId || undefined,
				networkId: data.networkId || undefined,
				serialNumber: data.serialNumber || undefined,
				purchaseDate: data.purchaseDate || undefined,
				purchasePrice: data.purchasePrice ?? undefined,
				currencyCode: data.currencyCode || undefined,
				notes: data.notes || undefined
			};

			await devices.update(device.id, payload);
			await devices.syncTags(device.id, tagIds);
			invalidateDevicesCache();

			showToast({
				type: 'success',
				message: `Device "${data.name}" updated successfully`
			});

			// Navigate back to detail page
			goto(`/devices/${device.id}`);
		} catch (err) {
			console.error('[device-edit] Submit failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to update device';
			showToast({ type: 'error', message: errorMsg });
			throw err; // Re-throw to keep form in submitting state
		}
	}

	function handleCancel() {
		if (device) {
			goto(`/devices/${device.id}`);
		} else {
			goto('/devices');
		}
	}
</script>

<!-- Breadcrumbs -->
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
		<li>
			<a
				href={`/devices/${deviceId}`}
				class="hover:text-primary-600 dark:hover:text-primary-400"
			>
				{device?.name ?? 'Device'}
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
			{t('common.actions.edit')}
		</li>
	</ol>
</nav>

<!-- Page header -->
<div class="mb-6">
	<h1 class="text-3xl font-bold text-neutral-900 dark:text-neutral-100">
		{t('devices.edit.title')}
	</h1>
	<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
		{t('devices.edit.subtitle')}
	</p>

	<!-- Retired device notice -->
	{#if isRetired}
		<div class="mt-4 flex items-start gap-3 rounded-lg bg-warning-50 p-3 dark:bg-warning-950">
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
					d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
				/>
			</svg>
			<p class="text-sm font-medium text-warning-900 dark:text-warning-100">
				{t('devices.edit.retiredNotice')}
			</p>
		</div>
	{/if}
</div>

<!-- Content -->
{#if isLoading}
	<LoadingSkeleton rows={5} />
{:else if error}
	<ErrorState {error} onRetry={fetchDevice} />
{:else if device}
	<!-- Form -->
	<div class="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
		<DeviceForm
			mode="edit"
			initialData={{
				name: device.name ?? '',
				serialNumber: device.serialNumber ?? '',
				brandId: device.brandId ?? '',
				categoryId: device.categoryId,
				ownerId: device.ownerId ?? '',
				locationId: device.locationId ?? '',
				networkId: device.networkId ?? '',
				tagIds: deviceTagIds,
				purchaseDate: device.purchaseDate ?? '',
				purchasePrice: device.purchasePrice ?? null,
				currencyCode: device.currencyCode ?? 'USD',
				notes: device.notes ?? '',
				purpose: device.purpose ?? '',
				operatingSystem: device.operatingSystem ?? '',
				ipAddress: device.ipAddress ?? '',
				macAddress: device.macAddress ?? '',
				productUrl: device.productUrl ?? '',
				version: device.version ?? ''
			}}
			{disabledFields}
			onSubmit={handleSubmit}
			onCancel={handleCancel}
		/>
	</div>
{/if}

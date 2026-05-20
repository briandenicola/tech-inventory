<script lang="ts">
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { devices } from '$lib/api/client';
	import { showToast } from '$lib/stores/toast';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import DeviceForm from '$lib/components/DeviceForm.svelte';
	import type { DeviceFormInput } from '$lib/schemas/device';

	/**
	 * T20: Device create page — /devices/new
	 * 
	 * Form with all device fields, Zod validation, household default currency pre-filled.
	 * Submit → POST /api/v1/devices → toast → redirect to detail page.
	 * 
	 * Related: specs/002-frontend-mvp/spec.md J6
	 */

	async function handleSubmit(data: DeviceFormInput) {
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

			const result = await devices.create(payload);
			await devices.syncTags(result.id, tagIds);
			invalidateDevicesCache();

			showToast({
				type: 'success',
				message: `Device "${data.name}" created successfully`
			});

			// Navigate to device detail page
			goto(`/devices/${result.id}`);
		} catch (err) {
			console.error('[device-create] Submit failed:', err);
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to create device';
			showToast({ type: 'error', message: errorMsg });
			throw err; // Re-throw to keep form in submitting state
		}
	}

	function handleCancel() {
		goto('/devices');
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
		<li aria-current="page" class="font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.create.title')}
		</li>
	</ol>
</nav>

<!-- Page header -->
<div class="mb-6">
	<h1 class="text-3xl font-bold text-neutral-900 dark:text-neutral-100">
		{t('devices.create.title')}
	</h1>
	<p class="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
		{t('devices.create.subtitle')}
	</p>
</div>

<!-- Form -->
<div class="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
	<DeviceForm mode="create" onSubmit={handleSubmit} onCancel={handleCancel} />
</div>

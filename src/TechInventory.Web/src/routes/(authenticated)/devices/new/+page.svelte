<script lang="ts">
	import { goto, beforeNavigate } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { devices, ApiError } from '$lib/api/client';
	import DeviceForm from '$lib/components/DeviceForm.svelte';
	import UnsavedChangesModal from '$lib/components/UnsavedChangesModal.svelte';
	import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
	import type { DeviceFormInput } from '$lib/schemas/device';
	import { fetchReferenceData } from '$lib/stores/referenceData';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { showToast } from '$lib/stores/toast';
	import { mapApiFieldErrors } from '$lib/utils/apiErrors';

	/**
	 * T20: Device create page — /devices/new
	 *
	 * Form with all device fields, Zod validation, household default currency pre-filled.
	 * Submit → POST /api/v1/devices → toast → redirect to detail page.
	 *
	 * No LoadingSkeleton — the form renders immediately with no data fetch
	 * gating it; DeviceForm fills its own reference-data selects as they load.
	 *
	 * Related: specs/002-frontend-mvp/spec.md J6
	 */

	let isDirty = $state(false);
	let confirmingDiscard = $state(false);
	let discarding = false;
	let serverErrors = $state<Record<string, string>>({});

	async function handleSubmit(data: DeviceFormInput) {
		serverErrors = {};
		try {
			const { tagIds, ...deviceData } = data;
			const payload = {
				...deviceData,
				model: data.model || undefined,
				brandId: data.brandId || undefined,
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
			if (!result.id) {
				throw new Error('Created device did not return an id');
			}

			// F030: Apply tags after device exists
			if (tagIds.length > 0) {
				await devices.syncTags(result.id, tagIds);
			}
			invalidateDevicesCache();

			showToast({
				type: 'success',
				message: `Device ${data.name} created successfully`
			});

			discarding = true;
			goto(`/devices/${result.id}`);
		} catch (err) {
			console.error('[device-create] Submit failed:', err);
			if (err instanceof ApiError && err.errors) {
				serverErrors = mapApiFieldErrors(err.errors);
			}
			const errorMsg =
				err instanceof Error && 'detail' in err
					? (err as unknown as { detail: string }).detail
					: 'Failed to create device';
			showToast({ type: 'error', message: errorMsg });
			throw err;
		}
	}

	function handleCancel() {
		goto('/devices');
	}

	let pendingUrl = $state<URL | null>(null);

	beforeNavigate(({ cancel, to }) => {
		if (!isDirty || discarding) return;
		cancel();
		pendingUrl = to?.url ?? null;
		confirmingDiscard = true;
	});

	function discardAndLeave() {
		confirmingDiscard = false;
		discarding = true;
		if (pendingUrl) {
			goto(pendingUrl);
		} else {
			goto('/devices');
		}
	}

	$effect(() => {
		const unregister = registerPullToRefresh($page.url.pathname, fetchReferenceData);
		return unregister;
	});
</script>

<svelte:head>
	<title>{t('devices.create.title')} — {t('app.title')}</title>
</svelte:head>

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
<div
	class="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
>
	<DeviceForm
		mode="create"
		onSubmit={handleSubmit}
		onCancel={handleCancel}
		bind:isDirty
		{serverErrors}
	/>
</div>

{#if confirmingDiscard}
	<UnsavedChangesModal
		onDiscard={discardAndLeave}
		onKeepEditing={() => {
			confirmingDiscard = false;
			pendingUrl = null;
		}}
	/>
{/if}

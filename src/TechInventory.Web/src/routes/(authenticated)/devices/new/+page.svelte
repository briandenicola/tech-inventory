<script lang="ts">
	import { goto, beforeNavigate } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from '$lib/i18n';
	import { devices, ApiError } from '$lib/api/client';
	import Breadcrumbs from '$lib/components/Breadcrumbs.svelte';
	import DeviceForm from '$lib/components/DeviceForm.svelte';
	import LoadingSkeleton from '$lib/components/LoadingSkeleton.svelte';
	import ErrorState from '$lib/components/ErrorState.svelte';
	import UnsavedChangesModal from '$lib/components/UnsavedChangesModal.svelte';
	import { invalidateDevicesCache, type DeviceResponse } from '$lib/queries/devices.svelte';
	import type { DeviceFormInput } from '$lib/schemas/device';
	import { fetchReferenceData } from '$lib/stores/referenceData';
	import { registerPullToRefresh } from '$lib/stores/pullToRefresh';
	import { showToast } from '$lib/stores/toast';
	import { getApiErrorMessage, mapApiFieldErrors } from '$lib/utils/apiErrors';

	/**
	 * T20: Device create page — /devices/new
	 *
	 * Form with all device fields, Zod validation, household default currency pre-filled.
	 * Submit → POST /api/v1/devices → toast → redirect to detail page.
	 *
	 * No LoadingSkeleton — the form renders immediately with no data fetch
	 * gating it; DeviceForm fills its own reference-data selects as they load.
	 *
	 * #131: Clone Device — when a `?cloneFrom=<id>` query param is present
	 * (set by DeviceActionsMenu's cloneHref, wired from the device detail
	 * route/modal/row), this page fetches the *source* device fresh from the
	 * API (never trusts a serialized copy in the URL/session) and pre-fills
	 * DeviceForm's `initialData` with only the reusable/descriptive fields.
	 * This still goes through the normal create path — the same
	 * `handleSubmit`/`devices.create()` call as every other add — so the
	 * clone is a brand-new record with its own id/audit trail, never an
	 * update of the source. Identity/unique/history fields are intentionally
	 * left out of `buildCloneInitialData` below (see the field-copy matrix in
	 * `page.clone.test.ts`):
	 *   - name, serialNumber: unique per-unit identifiers — left blank so the
	 *     required/editable Name field forces the user to give this device
	 *     its own identity before saving (satisfies the issue's "name is
	 *     copied with a disambiguating suffix OR left editable and
	 *     required" acceptance criterion via the simpler, safer option).
	 *   - ipAddress, macAddress: per-unit network/hardware identifiers, not
	 *     reusable across a batch of otherwise-identical devices.
	 *   - purchaseDate, purchasePrice, notes: instance-specific transaction/
	 *     history detail, not descriptive/reference data.
	 *   - status, retiredDate, disposalMethod, id, createdAt/By,
	 *     modifiedAt/By: never part of DeviceFormInput; mode="create" also
	 *     hides the Status control entirely (server defaults it to Active).
	 *
	 * Related: specs/002-frontend-mvp/spec.md J6, issue #131
	 */

	let isDirty = $state(false);
	let confirmingDiscard = $state(false);
	let discarding = false;
	let serverErrors = $state<Record<string, string>>({});

	const cloneFromId = $derived($page.url.searchParams.get('cloneFrom'));

	let cloneInitialData = $state<Partial<DeviceFormInput> | undefined>(undefined);
	let cloneSourceName = $state<string | null>(null);
	// Initialized from the URL synchronously (not inside the $effect below)
	// so a clone deep-link never flashes the blank create form before the
	// fetch starts — mirrors the edit page's `isLoading = $state(true)`.
	let isCloneLoading = $state(Boolean($page.url.searchParams.get('cloneFrom')));
	let cloneLoadError = $state<string | null>(null);

	/** Field-copy matrix for #131 — see script-block comment above for rationale. */
	function buildCloneInitialData(
		source: DeviceResponse,
		tagIds: string[]
	): Partial<DeviceFormInput> {
		return {
			model: source.model ?? '',
			brandId: source.brandId ?? '',
			categoryId: source.categoryId ?? '',
			ownerId: source.ownerId ?? '',
			locationId: source.locationId ?? '',
			networkId: source.networkId ?? '',
			tagIds,
			currencyCode: source.currencyCode ?? 'USD',
			purpose: source.purpose ?? '',
			operatingSystem: source.operatingSystem ?? '',
			productUrl: source.productUrl ?? '',
			version: source.version ?? ''
			// Deliberately omitted: name, serialNumber, ipAddress, macAddress,
			// purchaseDate, purchasePrice, notes, status.
		};
	}

	async function loadCloneSource(sourceId: string) {
		isCloneLoading = true;
		cloneLoadError = null;
		try {
			const [sourceDevice, tags] = await Promise.all([
				devices.get(sourceId),
				devices.listTags(sourceId)
			]);
			const device = sourceDevice as DeviceResponse;
			const tagIds = tags
				.map((tag) => tag.id)
				.filter((tagId): tagId is string => typeof tagId === 'string' && tagId.length > 0);

			cloneSourceName = device.name ?? null;
			cloneInitialData = buildCloneInitialData(device, tagIds);
		} catch (err) {
			console.error('[device-create] Clone source fetch failed:', err);
			cloneLoadError = getApiErrorMessage(err, t('devices.clone.loadError'));
		} finally {
			isCloneLoading = false;
		}
	}

	$effect(() => {
		const sourceId = cloneFromId;
		if (sourceId) {
			void loadCloneSource(sourceId);
		}
	});

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
			const errorMsg = getApiErrorMessage(err, 'Failed to create device');
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
<Breadcrumbs
	items={[
		{ label: t('navigation.home'), href: '/' },
		{ label: t('common.nouns.devices'), href: '/devices' },
		{ label: t('devices.create.title') }
	]}
/>

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
{#if cloneFromId && isCloneLoading}
	<LoadingSkeleton rows={5} />
{:else if cloneFromId && cloneLoadError}
	<ErrorState error={cloneLoadError} onRetry={() => loadCloneSource(cloneFromId)} />
{:else}
	<div
		class="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	>
		{#if cloneSourceName}
			<div
				class="mb-4 rounded-lg border border-primary-200 bg-primary-50 p-3 text-sm text-primary-800 dark:border-primary-900 dark:bg-primary-950 dark:text-primary-200"
			>
				{t('devices.clone.banner', { name: cloneSourceName })}
			</div>
		{/if}
		<DeviceForm
			mode="create"
			initialData={cloneInitialData}
			onSubmit={handleSubmit}
			onCancel={handleCancel}
			bind:isDirty
			{serverErrors}
		/>
	</div>
{/if}

{#if confirmingDiscard}
	<UnsavedChangesModal
		onDiscard={discardAndLeave}
		onKeepEditing={() => {
			confirmingDiscard = false;
			pendingUrl = null;
		}}
	/>
{/if}

<!--
	DeviceForm.svelte — Shared device create/edit form component
	
	T20+T21: Single-purpose form with Zod validation, inline error display,
	design tokens only, role-aware disabled fields for retired devices.
	
	Props:
	- mode: 'create' | 'edit'
	- initialData?: Partial<DeviceFormData>
	- disabledFields?: string[]
	- onSubmit: (data) => Promise<void>
	- onCancel: () => void
	
	Related: specs/002-frontend-mvp/spec.md J5-J7, Constitution §4.3
-->
<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { t } from '$lib/i18n';
	import { fetchReferenceData, referenceDataStore } from '$lib/stores/referenceData';
	import DeviceTagSelector from '$lib/components/DeviceTagSelector.svelte';
	import { deviceFormSchema, type DeviceFormInput } from '$lib/schemas/device';
	import type { ZodError } from 'zod';

	interface Props {
		mode: 'create' | 'edit';
		initialData?: Partial<DeviceFormInput>;
		disabledFields?: string[];
		onSubmit: (data: DeviceFormInput) => Promise<void>;
		onCancel: () => void;
	}

	let { mode, initialData = {}, disabledFields = [], onSubmit, onCancel }: Props = $props();

	const refData = $derived($referenceDataStore);

	onMount(() => {
		void fetchReferenceData();
	});

	let formData = $state<DeviceFormInput>(
		untrack(() => ({
			name: initialData.name ?? '',
			model: initialData.model ?? '',
			serialNumber: initialData.serialNumber ?? '',
			brandId: initialData.brandId ?? '',
			categoryId: initialData.categoryId ?? '',
			ownerId: initialData.ownerId ?? '',
			locationId: initialData.locationId ?? '',
			networkId: initialData.networkId ?? '',
			tagIds: initialData.tagIds ?? [],
			purchaseDate: initialData.purchaseDate ?? '',
			purchasePrice: initialData.purchasePrice ?? null,
			currencyCode: initialData.currencyCode ?? 'USD',
			notes: initialData.notes ?? '',
			purpose: initialData.purpose ?? '',
			operatingSystem: initialData.operatingSystem ?? '',
			ipAddress: initialData.ipAddress ?? '',
			macAddress: initialData.macAddress ?? '',
			productUrl: initialData.productUrl ?? '',
			version: initialData.version ?? ''
		}))
	);

	let errors = $state<Record<string, string>>({});
	let touched = $state<Record<string, boolean>>({});
	let isSubmitting = $state(false);

	function areValuesEqual(initial: unknown, current: unknown): boolean {
		if (Array.isArray(initial) && Array.isArray(current)) {
			return (
				initial.length === current.length &&
				initial.every((value, index) => value === current[index])
			);
		}

		return initial === current;
	}

	function getInitialValue(formKey: keyof DeviceFormInput): unknown {
		const initialValue = initialData[formKey];
		if (initialValue !== undefined) {
			return initialValue;
		}

		if (formKey === 'tagIds') {
			return [];
		}

		if (formKey === 'purchasePrice') {
			return null;
		}

		return '';
	}

	const isDirty = $derived(
		Object.keys(formData).some((key) => {
			const formKey = key as keyof DeviceFormInput;
			const initial = getInitialValue(formKey);
			const current = formData[formKey];
			return !areValuesEqual(initial, current);
		})
	);

	function validateField(name: string) {
		try {
			deviceFormSchema.parse(formData);
			errors = { ...errors, [name]: '' };
		} catch (err) {
			if (err instanceof Error && 'issues' in err) {
				const zodError = err as ZodError;
				const fieldError = zodError.issues.find((issue) => issue.path[0] === name);
				errors = { ...errors, [name]: fieldError?.message ?? '' };
			}
		}
	}

	function handleBlur(name: string) {
		touched = { ...touched, [name]: true };
		validateField(name);
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isSubmitting = true;
		touched = Object.keys(formData).reduce(
			(acc, key) => ({ ...acc, [key]: true }),
			{} as Record<string, boolean>
		);

		try {
			const validatedData = deviceFormSchema.parse(formData);
			await onSubmit(validatedData);
		} catch (err) {
			if (err instanceof Error && 'issues' in err) {
				const zodError = err as ZodError;
				const newErrors: Record<string, string> = {};
				zodError.issues.forEach((issue) => {
					const field = issue.path[0] as string;
					newErrors[field] ??= issue.message;
				});
				errors = newErrors;
			} else {
				console.error('[DeviceForm] Submit failed:', err);
			}
		} finally {
			isSubmitting = false;
		}
	}

	function isDisabled(field: string): boolean {
		return disabledFields.includes(field);
	}

	const currencyOptions = [
		{ code: 'USD', name: 'USD' },
		{ code: 'EUR', name: 'EUR' },
		{ code: 'GBP', name: 'GBP' },
		{ code: 'JPY', name: 'JPY' },
		{ code: 'CAD', name: 'CAD' },
		{ code: 'AUD', name: 'AUD' }
	];
</script>

<form onsubmit={handleSubmit} class="space-y-6">
	<!-- Name -->
	<div>
		<label for="name" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.name')} <span class="text-danger-600">*</span>
		</label>
		<input
			id="name"
			type="text"
			bind:value={formData.name}
			onblur={() => handleBlur('name')}
			disabled={isDisabled('name')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
			placeholder="iPhone 15 Pro"
		/>
		{#if touched.name && errors.name}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.name}</p>
		{/if}
	</div>

	<!-- Serial Number -->
	<div>
		<label
			for="serialNumber"
			class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
		>
			{t('devices.columns.serial')}
		</label>
		<input
			id="serialNumber"
			type="text"
			bind:value={formData.serialNumber}
			onblur={() => handleBlur('serialNumber')}
			disabled={isDisabled('serialNumber')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
			placeholder="G6AZH2X3F7"
		/>
		{#if touched.serialNumber && errors.serialNumber}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.serialNumber}</p>
		{/if}
	</div>

	<!--
		Model (F034) — sits between Serial Number and Brand because the
		spec-sheet ordering "Serial → Model → Brand" matches how users read
		device labels. The field is optional and gated on truthy values
		everywhere it displays so hand-entered devices without a model
		don't show a blank row.
	-->
	<div>
		<label for="model" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.model')}
		</label>
		<input
			id="model"
			type="text"
			bind:value={formData.model}
			onblur={() => handleBlur('model')}
			disabled={isDisabled('model')}
			maxlength="200"
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
			placeholder={t('devices.form.modelPlaceholder')}
		/>
		{#if touched.model && errors.model}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.model}</p>
		{/if}
	</div>

	<!-- Brand -->
	<div>
		<label for="brandId" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.brand')} <span class="text-danger-600">*</span>
		</label>
		<select
			id="brandId"
			bind:value={formData.brandId}
			onblur={() => handleBlur('brandId')}
			disabled={isDisabled('brandId')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		>
			<option value="">-- Select Brand --</option>
			{#each refData.brands as brand (brand.id)}
				<option value={brand.id}>{brand.name}</option>
			{/each}
		</select>
		{#if touched.brandId && errors.brandId}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.brandId}</p>
		{/if}
	</div>

	<!-- Category -->
	<div>
		<label
			for="categoryId"
			class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
		>
			{t('devices.columns.category')} <span class="text-danger-600">*</span>
		</label>
		<select
			id="categoryId"
			bind:value={formData.categoryId}
			onblur={() => handleBlur('categoryId')}
			disabled={isDisabled('categoryId')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		>
			<option value="">-- Select Category --</option>
			{#each refData.categories as category (category.id)}
				<option value={category.id}>{category.name}</option>
			{/each}
		</select>
		{#if touched.categoryId && errors.categoryId}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.categoryId}</p>
		{/if}
	</div>

	<!-- Owner -->
	<div>
		<label for="ownerId" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.owner')}
		</label>
		<select
			id="ownerId"
			bind:value={formData.ownerId}
			onblur={() => handleBlur('ownerId')}
			disabled={isDisabled('ownerId')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		>
			<option value="">-- No Owner --</option>
			{#each refData.owners as owner (owner.id)}
				<option value={owner.id}>{owner.name}</option>
			{/each}
		</select>
		{#if touched.ownerId && errors.ownerId}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.ownerId}</p>
		{/if}
	</div>

	<!-- Location -->
	<div>
		<label
			for="locationId"
			class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
		>
			{t('devices.columns.location')}
		</label>
		<select
			id="locationId"
			bind:value={formData.locationId}
			onblur={() => handleBlur('locationId')}
			disabled={isDisabled('locationId')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		>
			<option value="">-- No Location --</option>
			{#each refData.locations as location (location.id)}
				<option value={location.id}>{location.name}</option>
			{/each}
		</select>
		{#if touched.locationId && errors.locationId}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.locationId}</p>
		{/if}
	</div>

	<!-- Network -->
	<div>
		<label for="networkId" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.network')}
		</label>
		<select
			id="networkId"
			bind:value={formData.networkId}
			onblur={() => handleBlur('networkId')}
			disabled={isDisabled('networkId')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		>
			<option value="">-- No Network --</option>
			{#each refData.networks as network (network.id)}
				<option value={network.id}>{network.name}</option>
			{/each}
		</select>
		{#if touched.networkId && errors.networkId}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.networkId}</p>
		{/if}
	</div>

	<!-- Tags -->
	<DeviceTagSelector
		options={refData.tags}
		bind:selectedTagIds={formData.tagIds}
		disabled={isDisabled('tagIds')}
	/>

	<!-- Purchase Date -->
	<div>
		<label
			for="purchaseDate"
			class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
		>
			{t('devices.columns.purchaseDate')}
		</label>
		<input
			id="purchaseDate"
			type="date"
			bind:value={formData.purchaseDate}
			onblur={() => handleBlur('purchaseDate')}
			disabled={isDisabled('purchaseDate')}
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
		/>
		{#if touched.purchaseDate && errors.purchaseDate}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.purchaseDate}</p>
		{/if}
	</div>

	<!-- Purchase Price + Currency (two-column on desktop) -->
	<div class="grid gap-4 sm:grid-cols-2">
		<div>
			<label
				for="purchasePrice"
				class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
			>
				{t('devices.columns.purchasePrice')}
			</label>
			<input
				id="purchasePrice"
				type="number"
				step="0.01"
				min="0"
				bind:value={formData.purchasePrice}
				onblur={() => handleBlur('purchasePrice')}
				disabled={isDisabled('purchasePrice')}
				class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
				placeholder="999.99"
			/>
			{#if touched.purchasePrice && errors.purchasePrice}
				<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.purchasePrice}</p>
			{/if}
		</div>

		<div>
			<label
				for="currencyCode"
				class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
			>
				{t('devices.columns.currency')}
			</label>
			<select
				id="currencyCode"
				bind:value={formData.currencyCode}
				onblur={() => handleBlur('currencyCode')}
				disabled={isDisabled('currencyCode')}
				class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
			>
				{#each currencyOptions as currency (currency.code)}
					<option value={currency.code}>{currency.name}</option>
				{/each}
			</select>
			{#if touched.currencyCode && errors.currencyCode}
				<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.currencyCode}</p>
			{/if}
		</div>
	</div>

	<!-- Notes -->
	<div>
		<label for="notes" class="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
			{t('devices.columns.notes')}
		</label>
		<textarea
			id="notes"
			bind:value={formData.notes}
			onblur={() => handleBlur('notes')}
			disabled={isDisabled('notes')}
			rows="4"
			class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
			placeholder="Additional notes or comments…"
		></textarea>
		{#if touched.notes && errors.notes}
			<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.notes}</p>
		{/if}
	</div>

	<!-- Additional Details (collapsible) -->
	<details class="group">
		<summary
			class="cursor-pointer list-none text-sm font-medium text-neutral-900 dark:text-neutral-100"
		>
			<span class="inline-flex items-center gap-2">
				<svg
					class="h-4 w-4 transition-transform group-open:rotate-90"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					aria-hidden="true"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
				</svg>
				{t('devices.form.additionalDetails')}
			</span>
		</summary>

		<div class="mt-4 space-y-6 border-l-2 border-neutral-200 pl-4 dark:border-neutral-800">
			<!-- Purpose -->
			<div>
				<label
					for="purpose"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.purpose')}
				</label>
				<textarea
					id="purpose"
					bind:value={formData.purpose}
					onblur={() => handleBlur('purpose')}
					disabled={isDisabled('purpose')}
					rows="3"
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.purposePlaceholder')}
				></textarea>
				{#if touched.purpose && errors.purpose}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.purpose}</p>
				{/if}
			</div>

			<!-- Operating System -->
			<div>
				<label
					for="operatingSystem"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.operatingSystem')}
				</label>
				<input
					id="operatingSystem"
					type="text"
					bind:value={formData.operatingSystem}
					onblur={() => handleBlur('operatingSystem')}
					disabled={isDisabled('operatingSystem')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.operatingSystemPlaceholder')}
				/>
				{#if touched.operatingSystem && errors.operatingSystem}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.operatingSystem}</p>
				{/if}
			</div>

			<!-- IP Address -->
			<div>
				<label
					for="ipAddress"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.ipAddress')}
				</label>
				<input
					id="ipAddress"
					type="text"
					bind:value={formData.ipAddress}
					onblur={() => handleBlur('ipAddress')}
					disabled={isDisabled('ipAddress')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.ipAddressPlaceholder')}
				/>
				{#if touched.ipAddress && errors.ipAddress}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.ipAddress}</p>
				{/if}
			</div>

			<!-- MAC Address -->
			<div>
				<label
					for="macAddress"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.macAddress')}
				</label>
				<input
					id="macAddress"
					type="text"
					bind:value={formData.macAddress}
					onblur={() => handleBlur('macAddress')}
					disabled={isDisabled('macAddress')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.macAddressPlaceholder')}
				/>
				{#if touched.macAddress && errors.macAddress}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.macAddress}</p>
				{/if}
			</div>

			<!-- Product URL -->
			<div>
				<label
					for="productUrl"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.productUrl')}
				</label>
				<input
					id="productUrl"
					type="url"
					bind:value={formData.productUrl}
					onblur={() => handleBlur('productUrl')}
					disabled={isDisabled('productUrl')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.productUrlPlaceholder')}
				/>
				{#if touched.productUrl && errors.productUrl}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.productUrl}</p>
				{/if}
			</div>

			<!-- Version -->
			<div>
				<label
					for="version"
					class="block text-sm font-medium text-neutral-900 dark:text-neutral-100"
				>
					{t('devices.form.version')}
				</label>
				<input
					id="version"
					type="text"
					bind:value={formData.version}
					onblur={() => handleBlur('version')}
					disabled={isDisabled('version')}
					class="mt-1 block w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 transition-colors placeholder:text-neutral-500 hover:border-neutral-400 focus-visible:border-primary-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-500 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-600 dark:hover:border-neutral-600 dark:focus-visible:border-primary-500 dark:disabled:bg-neutral-900"
					placeholder={t('devices.form.versionPlaceholder')}
				/>
				{#if touched.version && errors.version}
					<p class="mt-1 text-sm text-danger-600 dark:text-danger-400">{errors.version}</p>
				{/if}
			</div>
		</div>
	</details>

	<!-- Action buttons -->
	<div class="flex justify-end gap-3 border-t border-neutral-200 pt-6 dark:border-neutral-800">
		<button
			type="button"
			onclick={onCancel}
			disabled={isSubmitting}
			class="rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-neutral-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:bg-neutral-800 dark:focus-visible:ring-neutral-600"
		>
			{t('common.actions.cancel')}
		</button>

		<button
			type="submit"
			disabled={isSubmitting || (mode === 'edit' && !isDirty)}
			class="inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-primary-500 dark:hover:bg-primary-600"
		>
			{#if isSubmitting}
				<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
					<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
					></circle>
					<path
						class="opacity-75"
						fill="currentColor"
						d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
					></path>
				</svg>
				{t('common.states.saving')}
			{:else}
				{t('common.actions.save')}
			{/if}
		</button>
	</div>
</form>

<script lang="ts">
	import { t } from '$lib/i18n';
	import type { components } from '$lib/api/generated/types';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';

	type TagResponse = components['schemas']['TagResponse'];
	type DetailField = {
		key: string;
		label: string;
		value: string;
		href?: string;
		mono?: boolean;
		multiline?: boolean;
	};

	interface Props {
		device: DeviceResponse;
		brandName: string;
		categoryName: string;
		ownerName: string;
		locationName: string;
		networkName: string;
		deviceTags?: TagResponse[];
		statusClass: string;
		formatDate: (value: string | null) => string;
		formatDateTime: (value: string | null) => string;
	}

	let {
		device,
		brandName,
		categoryName,
		ownerName,
		locationName,
		networkName,
		deviceTags = [],
		statusClass,
		formatDate,
		formatDateTime
	}: Props = $props();

	const detailFields = $derived.by(() => {
		const fields: DetailField[] = [
			{ key: 'serial', label: t('devices.columns.serial'), value: device.serialNumber ?? '—' },
			{ key: 'model', label: t('devices.columns.model'), value: device.model ?? '—' },
			{ key: 'brand', label: t('devices.columns.brand'), value: brandName },
			{ key: 'category', label: t('devices.columns.category'), value: categoryName },
			{ key: 'owner', label: t('devices.columns.owner'), value: ownerName },
			{ key: 'location', label: t('devices.columns.location'), value: locationName },
			{ key: 'network', label: t('devices.columns.network'), value: networkName },
			{
				key: 'purchaseDate',
				label: t('devices.columns.purchaseDate'),
				value: formatDate(device.purchaseDate)
			},
			{
				key: 'purchasePrice',
				label: t('devices.columns.purchasePrice'),
				value:
					device.purchasePrice !== null && device.currencyCode
						? `${device.currencyCode} ${device.purchasePrice.toFixed(2)}`
						: '—'
			}
		];

		if (device.operatingSystem) {
			fields.push({
				key: 'operatingSystem',
				label: t('devices.columns.operatingSystem'),
				value: device.operatingSystem
			});
		}
		if (device.version) {
			fields.push({ key: 'version', label: t('devices.columns.version'), value: device.version });
		}
		if (device.ipAddress) {
			fields.push({
				key: 'ipAddress',
				label: t('devices.columns.ipAddress'),
				value: device.ipAddress,
				mono: true
			});
		}
		if (device.macAddress) {
			fields.push({
				key: 'macAddress',
				label: t('devices.columns.macAddress'),
				value: device.macAddress,
				mono: true
			});
		}
		if (device.productUrl) {
			fields.push({
				key: 'productUrl',
				label: t('devices.columns.productUrl'),
				value: device.productUrl,
				href: device.productUrl
			});
		}
		if (device.retiredDate) {
			fields.push({
				key: 'retiredDate',
				label: t('devices.columns.retiredDate'),
				value: formatDate(device.retiredDate)
			});
		}
		if (device.disposalMethod) {
			fields.push({
				key: 'disposalMethod',
				label: t('devices.columns.disposalMethod'),
				value: device.disposalMethod
			});
		}
		if (device.purpose) {
			fields.push({
				key: 'purpose',
				label: t('devices.columns.purpose'),
				value: device.purpose,
				multiline: true
			});
		}
		if (device.notes) {
			fields.push({
				key: 'notes',
				label: t('devices.columns.notes'),
				value: device.notes,
				multiline: true
			});
		}

		return fields;
	});
</script>

<div class="space-y-6">
	<div>
		<span
			class={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${statusClass}`}
		>
			{device.status ?? 'Unknown'}
		</span>
	</div>

	<!-- Main attribute grid: single-column mobile, two-column desktop -->
	<div
		class="rounded-2xl border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950 sm:p-6"
	>
		<dl class="grid grid-cols-1 gap-x-8 gap-y-5 sm:grid-cols-2">
			{#each detailFields as field (field.key)}
				<div class={field.multiline ? 'col-span-full' : ''}>
					<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
						{field.label}
					</dt>
					<dd class="mt-1 text-sm text-neutral-900 dark:text-neutral-100">
						{#if field.href}
							<a
								href={field.href}
								target="_blank"
								rel="noopener noreferrer"
								class="break-all text-primary-600 hover:text-primary-500 hover:underline dark:text-primary-400 dark:hover:text-primary-300"
							>
								{field.value}
							</a>
						{:else if field.mono}
							<span class="break-all font-mono">{field.value}</span>
						{:else}
							<span class:whitespace-pre-wrap={field.multiline} class="break-words"
								>{field.value}</span
							>
						{/if}
					</dd>
				</div>
			{/each}

			<!-- Tags always span the full width to accommodate the chip list -->
			<div class="col-span-full">
				<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
					{t('devices.tags.label')}
				</dt>
				<dd class="mt-1 text-sm text-neutral-900 dark:text-neutral-100">
					{#if deviceTags.length > 0}
						<ul class="flex flex-wrap gap-2" aria-label={t('devices.tags.label')}>
							{#each deviceTags as tag (tag.id)}
								<li
									class="inline-flex items-center gap-1.5 rounded-full bg-neutral-100 px-3 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200"
								>
									{#if tag.color}
										<span
											class="inline-block h-2 w-2 rounded-full"
											style={`background-color: ${tag.color};`}
											aria-hidden="true"
										></span>
									{/if}
									{tag.name}
								</li>
							{/each}
						</ul>
					{:else}
						<span class="text-neutral-500 dark:text-neutral-400"
							>{t('devices.detail.tagsEmpty')}</span
						>
					{/if}
				</dd>
			</div>
		</dl>
	</div>

	<!-- Audit trail: same definition-list grid system as main fields -->
	<div
		class="rounded-2xl border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950 sm:p-6"
	>
		<h2 class="text-sm font-semibold text-neutral-600 dark:text-neutral-400">
			{t('devices.detail.audit.title')}
		</h2>
		<dl class="mt-4 grid grid-cols-1 gap-x-8 gap-y-5 sm:grid-cols-2">
			<div>
				<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
					{t('devices.detail.audit.created')}
				</dt>
				<dd class="mt-1 text-sm text-neutral-900 dark:text-neutral-100">
					<time datetime={device.createdAt} title={formatDateTime(device.createdAt)}>
						{formatDateTime(device.createdAt)}
					</time>
					{#if device.createdBy}
						<span class="text-neutral-500 dark:text-neutral-400">
							{t('devices.detail.audit.by', { actor: device.createdBy })}
						</span>
					{/if}
				</dd>
			</div>
			<div>
				<dt class="text-sm font-medium text-neutral-600 dark:text-neutral-400">
					{t('devices.detail.audit.lastModified')}
				</dt>
				<dd class="mt-1 text-sm text-neutral-900 dark:text-neutral-100">
					<time datetime={device.modifiedAt} title={formatDateTime(device.modifiedAt)}>
						{formatDateTime(device.modifiedAt)}
					</time>
					{#if device.modifiedBy}
						<span class="text-neutral-500 dark:text-neutral-400">
							{t('devices.detail.audit.by', { actor: device.modifiedBy })}
						</span>
					{/if}
				</dd>
			</div>
		</dl>
	</div>
</div>

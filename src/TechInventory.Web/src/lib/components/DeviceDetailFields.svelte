<script lang="ts">
	import { t } from '$lib/i18n';
	import type { components } from '$lib/api/generated/types';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';

	type TagResponse = components['schemas']['TagResponse'];
	type DetailRow = {
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

	const detailRows = $derived.by(() => {
		const rows: DetailRow[] = [
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
			rows.push({
				key: 'operatingSystem',
				label: t('devices.columns.operatingSystem'),
				value: device.operatingSystem
			});
		}
		if (device.version) {
			rows.push({ key: 'version', label: t('devices.columns.version'), value: device.version });
		}
		if (device.ipAddress) {
			rows.push({
				key: 'ipAddress',
				label: t('devices.columns.ipAddress'),
				value: device.ipAddress,
				mono: true
			});
		}
		if (device.macAddress) {
			rows.push({
				key: 'macAddress',
				label: t('devices.columns.macAddress'),
				value: device.macAddress,
				mono: true
			});
		}
		if (device.productUrl) {
			rows.push({
				key: 'productUrl',
				label: t('devices.columns.productUrl'),
				value: device.productUrl,
				href: device.productUrl
			});
		}
		if (device.retiredDate) {
			rows.push({
				key: 'retiredDate',
				label: t('devices.columns.retiredDate'),
				value: formatDate(device.retiredDate)
			});
		}
		if (device.disposalMethod) {
			rows.push({
				key: 'disposalMethod',
				label: t('devices.columns.disposalMethod'),
				value: device.disposalMethod
			});
		}
		if (device.purpose) {
			rows.push({
				key: 'purpose',
				label: t('devices.columns.purpose'),
				value: device.purpose,
				multiline: true
			});
		}
		if (device.notes) {
			rows.push({
				key: 'notes',
				label: t('devices.columns.notes'),
				value: device.notes,
				multiline: true
			});
		}

		return rows;
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

	<div
		class="overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	>
		<table class="w-full table-fixed border-collapse">
			<tbody>
				{#each detailRows as row, index (row.key)}
					<tr class:border-t={index > 0} class="border-neutral-200 dark:border-neutral-800">
						<th
							scope="row"
							class="w-32 px-4 py-3 text-left align-top text-sm font-medium text-neutral-600 dark:text-neutral-400 sm:w-40"
						>
							{row.label}
						</th>
						<td class="px-4 py-3 align-top text-sm text-neutral-900 dark:text-neutral-100">
							{#if row.href}
								<a
									href={row.href}
									target="_blank"
									rel="noopener noreferrer"
									class="break-all text-primary-600 hover:text-primary-500 hover:underline dark:text-primary-400 dark:hover:text-primary-300"
								>
									{row.value}
								</a>
							{:else if row.mono}
								<span class="font-mono break-all">{row.value}</span>
							{:else}
								<span class:whitespace-pre-wrap={row.multiline} class="break-words"
									>{row.value}</span
								>
							{/if}
						</td>
					</tr>
				{/each}
				<tr class="border-t border-neutral-200 dark:border-neutral-800">
					<th
						scope="row"
						class="w-32 px-4 py-3 text-left align-top text-sm font-medium text-neutral-600 dark:text-neutral-400 sm:w-40"
					>
						{t('devices.tags.label')}
					</th>
					<td class="px-4 py-3 align-top text-sm text-neutral-900 dark:text-neutral-100">
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
					</td>
				</tr>
			</tbody>
		</table>
	</div>

	<div
		class="rounded-2xl border border-neutral-200 bg-white px-4 py-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950"
	>
		<h3 class="text-sm font-semibold text-neutral-600 dark:text-neutral-400">
			{t('devices.detail.audit.title')}
		</h3>
		<table class="mt-3 w-full table-fixed border-collapse">
			<tbody>
				<tr>
					<th
						scope="row"
						class="w-32 text-left align-top text-sm font-medium text-neutral-600 dark:text-neutral-400 sm:w-40"
					>
						{t('devices.detail.audit.created')}
					</th>
					<td class="text-sm text-neutral-900 dark:text-neutral-100">
						<time datetime={device.createdAt} title={formatDateTime(device.createdAt)}>
							{formatDateTime(device.createdAt)}
						</time>
						{#if device.createdBy}
							<span class="text-neutral-500 dark:text-neutral-400">
								{t('devices.detail.audit.by', { actor: device.createdBy })}
							</span>
						{/if}
					</td>
				</tr>
				<tr class="border-t border-neutral-200 dark:border-neutral-800">
					<th
						scope="row"
						class="w-32 py-3 text-left align-top text-sm font-medium text-neutral-600 dark:text-neutral-400 sm:w-40"
					>
						{t('devices.detail.audit.lastModified')}
					</th>
					<td class="py-3 text-sm text-neutral-900 dark:text-neutral-100">
						<time datetime={device.modifiedAt} title={formatDateTime(device.modifiedAt)}>
							{formatDateTime(device.modifiedAt)}
						</time>
						{#if device.modifiedBy}
							<span class="text-neutral-500 dark:text-neutral-400">
								{t('devices.detail.audit.by', { actor: device.modifiedBy })}
							</span>
						{/if}
					</td>
				</tr>
			</tbody>
		</table>
	</div>
</div>

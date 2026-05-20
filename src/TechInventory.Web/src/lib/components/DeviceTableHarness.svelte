<script lang="ts">
	import DeviceTable from './DeviceTable.svelte';
	import { referenceDataStore } from '$lib/stores/referenceData';
	import type { DeviceResponse } from '$lib/queries/devices.svelte';

	const devices: DeviceResponse[] = [
		{
			id: 'device-1',
			name: 'Kitchen Hub',
			model: 'Nest Hub Max',
			serialNumber: 'SN0001',
			brandId: 'brand-1',
			categoryId: 'category-1',
			ownerId: 'owner-1',
			locationId: 'location-1',
			networkId: 'network-1',
			purchaseDate: '2024-01-15',
			purchasePrice: 199.99,
			currencyCode: 'USD',
			status: 'Active',
			notes: null,
			retiredDate: null,
			disposalMethod: null,
			purpose: null,
			operatingSystem: null,
			ipAddress: null,
			macAddress: null,
			productUrl: null,
			version: null,
			createdAt: '2024-01-15T10:30:00Z',
			createdBy: 'test-user@example.com',
			modifiedAt: '2024-01-15T10:30:00Z',
			modifiedBy: 'test-user@example.com'
		}
	];

	referenceDataStore.set({
		brands: [{ id: 'brand-1', name: 'Google' }],
		categories: [{ id: 'category-1', name: 'Smart Display' }],
		owners: [{ id: 'owner-1', name: 'Brian' }],
		locations: [{ id: 'location-1', name: 'Kitchen' }],
		networks: [{ id: 'network-1', name: 'Main Wi-Fi' }],
		tags: [],
		isLoading: false,
		error: null
	});

	let selectedIds = $state(new Set<string>());

	function toggleSelect(id: string) {
		const next = new Set(selectedIds);
		if (next.has(id)) next.delete(id);
		else next.add(id);
		selectedIds = next;
	}

	function toggleSelectAllVisible() {
		if (selectedIds.has('device-1')) {
			selectedIds = new Set<string>();
			return;
		}

		selectedIds = new Set<string>(['device-1']);
	}
</script>

<DeviceTable
	{devices}
	currentSort={undefined}
	sortDir="asc"
	onSort={() => undefined}
	selectable={true}
	{selectedIds}
	onToggleSelect={toggleSelect}
	onToggleSelectAll={toggleSelectAllVisible}
	allVisibleSelected={selectedIds.has('device-1')}
	someVisibleSelected={false}
	onOpenDevice={() => undefined}
/>

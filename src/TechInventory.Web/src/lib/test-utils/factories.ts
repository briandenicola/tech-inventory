/**
 * Test data factories for components tests
 * 
 * Per Constitution §3.5: Tests own their data; no shared fixtures.
 * These factories produce deterministic test data with unique IDs per call.
 */

import type { DeviceResponse } from '$lib/queries/devices';

let deviceIdCounter = 0;

/**
 * Create a DeviceResponse fixture for tests
 */
export function createDeviceResponse(overrides?: Partial<DeviceResponse>): DeviceResponse {
	const id = deviceIdCounter++;
	return {
		id: `device-${id.toString().padStart(8, '0')}-0000-0000-000000000000`,
		name: `Test Device ${id}`,
		model: `Model ${id}`,
		serialNumber: `SN${id.toString().padStart(8, '0')}`,
		brandId: 'brand-00000000-0000-0000-0000-000000000000',
		categoryId: 'category-0000-0000-0000-000000000000',
		ownerId: 'owner-000000-0000-0000-0000-000000000000',
		locationId: 'location-00-0000-0000-0000-000000000000',
		networkId: 'network-000-0000-0000-0000-000000000000',
		purchaseDate: '2024-01-15',
		purchasePrice: 499.99,
		currencyCode: 'USD',
		status: 'Active',
		notes: null,
		retiredDate: null,
		disposalMethod: null,
		createdAt: '2024-01-15T10:30:00Z',
		createdBy: 'test-user@example.com',
		modifiedAt: '2024-01-15T10:30:00Z',
		modifiedBy: 'test-user@example.com',
		...overrides
	};
}

/**
 * Create multiple device fixtures
 */
export function createDeviceList(count: number): DeviceResponse[] {
	return Array.from({ length: count }, () => createDeviceResponse());
}

/**
 * Reset factory counters (call in beforeEach if needed)
 */
export function resetFactories() {
	deviceIdCounter = 0;
}

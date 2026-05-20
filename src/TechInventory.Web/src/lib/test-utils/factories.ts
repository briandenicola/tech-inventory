/**
 * Test data factories for components tests
 * 
 * Per Constitution §3.5: Tests own their data; no shared fixtures.
 * These factories produce deterministic test data with unique IDs per call.
 */

import type { DeviceResponse } from '$lib/queries/devices.svelte';
import type { ReferenceEntity, ReferenceTag } from '$lib/stores/referenceData';
import type { DeviceFormInput } from '$lib/schemas/device';

let deviceIdCounter = 0;
let brandIdCounter = 0;
let categoryIdCounter = 0;
let ownerIdCounter = 0;
let locationIdCounter = 0;
let networkIdCounter = 0;
let tagIdCounter = 0;

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
		brandId: 'brand-00000000-4000-8000-000000000000',
		categoryId: 'category-4000-8000-000000000000',
		ownerId: 'owner-000000-4000-8000-000000000000',
		locationId: 'location-00-4000-8000-000000000000',
		networkId: 'network-000-4000-8000-000000000000',
		purchaseDate: '2024-01-15',
		purchasePrice: 499.99,
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
 * Create a Brand reference entity for tests
 */
export function createBrand(overrides?: Partial<ReferenceEntity>): ReferenceEntity {
	const id = brandIdCounter++;
	return {
		id: `brand-${id.toString().padStart(8, '0')}-0000-0000-000000000000`,
		name: `Brand ${id}`,
		...overrides
	};
}

/**
 * Create a Category reference entity for tests
 */
export function createCategory(overrides?: Partial<ReferenceEntity>): ReferenceEntity {
	const id = categoryIdCounter++;
	return {
		id: `category-${id.toString().padStart(6, '0')}-0000-0000-000000000000`,
		name: `Category ${id}`,
		...overrides
	};
}

/**
 * Create an Owner reference entity for tests
 */
export function createOwner(overrides?: Partial<ReferenceEntity>): ReferenceEntity {
	const id = ownerIdCounter++;
	return {
		id: `owner-${id.toString().padStart(8, '0')}-0000-0000-000000000000`,
		name: `Owner ${id}`,
		...overrides
	};
}

/**
 * Create a Location reference entity for tests
 */
export function createLocation(overrides?: Partial<ReferenceEntity>): ReferenceEntity {
	const id = locationIdCounter++;
	return {
		id: `location-${id.toString().padStart(6, '0')}-0000-0000-000000000000`,
		name: `Location ${id}`,
		...overrides
	};
}

/**
 * Create a Network reference entity for tests
 */
export function createNetwork(overrides?: Partial<ReferenceEntity>): ReferenceEntity {
	const id = networkIdCounter++;
	return {
		id: `network-${id.toString().padStart(7, '0')}-0000-0000-000000000000`,
		name: `Network ${id}`,
		...overrides
	};
}

/**
 * Create a tag reference entity for tests
 */
export function createTag(overrides?: Partial<ReferenceTag>): ReferenceTag {
	const id = tagIdCounter++;
	return {
		id: `00000000-0000-4000-8000-${id.toString().padStart(12, '0')}`,
		name: `Tag ${id}`,
		color: '#64748b',
		...overrides
	};
}

/**
 * Create a DeviceFormInput payload for tests
 */
export function createDeviceCreateInput(
	overrides?: Partial<DeviceFormInput>
): DeviceFormInput {
	return {
		name: 'Test Device',
		serialNumber: 'SN12345',
		brandId: 'brand-00000000-4000-8000-000000000000',
		categoryId: 'category-4000-8000-000000000000',
		ownerId: '',
		locationId: '',
		networkId: '',
		tagIds: [],
		purchaseDate: '',
		purchasePrice: null,
		currencyCode: 'USD',
		notes: '',
		purpose: '',
		operatingSystem: '',
		ipAddress: '',
		macAddress: '',
		productUrl: '',
		version: '',
		...overrides
	};
}

/**
 * Reset factory counters (call in beforeEach if needed)
 */
export function resetFactories() {
	deviceIdCounter = 0;
	brandIdCounter = 0;
	categoryIdCounter = 0;
	ownerIdCounter = 0;
	locationIdCounter = 0;
	networkIdCounter = 0;
	tagIdCounter = 0;
}

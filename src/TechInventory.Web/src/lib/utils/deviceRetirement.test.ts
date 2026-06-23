import { describe, expect, it } from 'vitest';
import type { DeviceResponse } from '$lib/queries/devices.svelte';
import { buildRetireDeviceRequest, canRetireDevice } from './deviceRetirement';

const activeDevice = {
	id: 'device-1',
	name: 'Aqua Flosser',
	model: 'Aqua Flosser PRO',
	serialNumber: 'SN-123',
	brandId: 'brand-1',
	categoryId: 'category-1',
	ownerId: 'member-1',
	locationId: 'location-1',
	networkId: null,
	purchaseDate: '2025-10-20',
	purchasePrice: 99,
	currencyCode: 'USD',
	status: 'Active',
	notes: 'Countertop',
	retiredDate: null,
	disposalMethod: null,
	purpose: 'Flossing',
	operatingSystem: null,
	ipAddress: null,
	macAddress: null,
	productUrl: null,
	version: 'abc',
	createdAt: '2025-10-20T00:00:00Z',
	createdBy: 'tester',
	modifiedAt: '2025-10-21T00:00:00Z',
	modifiedBy: 'tester'
} satisfies DeviceResponse;

describe('deviceRetirement', () => {
	it('allows admins and owning members to retire active devices', () => {
		expect(canRetireDevice(activeDevice, { id: 'admin-1', role: 'Admin' })).toBe(true);
		expect(canRetireDevice(activeDevice, { id: 'member-1', role: 'Member' })).toBe(true);
	});

	it('blocks viewers, non-owning members, and inactive devices from retiring', () => {
		expect(canRetireDevice(activeDevice, { id: 'viewer-1', role: 'Viewer' })).toBe(false);
		expect(canRetireDevice(activeDevice, { id: 'member-2', role: 'Member' })).toBe(false);
		expect(canRetireDevice({ ...activeDevice, status: 'Retired' }, { id: 'admin-1', role: 'Admin' })).toBe(
			false
		);
	});

	it('builds an update payload that preserves existing fields while setting retired status', () => {
		const payload = buildRetireDeviceRequest(activeDevice, '2026-06-23');

		expect(payload).toMatchObject({
			name: 'Aqua Flosser',
			brandId: 'brand-1',
			categoryId: 'category-1',
			status: 'Retired',
			retiredDate: '2026-06-23',
			version: 'abc'
		});
	});
});

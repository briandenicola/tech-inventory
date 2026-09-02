import { describe, expect, it } from 'vitest';
import type { DeviceResponse } from '$lib/queries/devices.svelte';
import {
	buildRetireDeviceRequest,
	buildUnretireDeviceRequest,
	toDateOnly,
	canRetireDevice,
	canUnretireDevice
} from './deviceRetirement';

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

	it('allows admins and owning members to unretire retired devices', () => {
		const retiredDevice = { ...activeDevice, status: 'Retired' };

		expect(canUnretireDevice(retiredDevice, { id: 'admin-1', role: 'Admin' })).toBe(true);
		expect(canUnretireDevice(retiredDevice, { id: 'member-1', role: 'Member' })).toBe(true);
	});

	it('blocks viewers, non-owning members, and active devices from unretiring', () => {
		const retiredDevice = { ...activeDevice, status: 'Retired' };

		expect(canUnretireDevice(retiredDevice, { id: 'viewer-1', role: 'Viewer' })).toBe(false);
		expect(canUnretireDevice(retiredDevice, { id: 'member-2', role: 'Member' })).toBe(false);
		expect(canUnretireDevice(activeDevice, { id: 'admin-1', role: 'Admin' })).toBe(false);
	});

	it('builds an update payload that preserves existing fields while setting retired status', () => {
		const payload = buildRetireDeviceRequest(activeDevice, new Date(2026, 5, 23, 14, 30));

		expect(payload).toMatchObject({
			name: 'Aqua Flosser',
			brandId: 'brand-1',
			categoryId: 'category-1',
			status: 'Retired',
			retiredDate: '2026-06-23',
			version: 'abc'
		});
	});

	// Regression: this previously took a pre-formatted string and the test fed
	// it '2026-06-23', while both call sites passed `new Date().toISOString()`.
	// The API binds retiredDate to a C# DateOnly?, which rejects a timestamp, so
	// every retire came back 400 while this test stayed green. Assert the wire
	// format itself, not a value no caller produces.
	it('always emits a date-only retiredDate the API can bind', () => {
		const payload = buildRetireDeviceRequest(activeDevice, new Date(2026, 8, 1, 19, 55));

		expect(payload.retiredDate).toBe('2026-09-01');
		expect(payload.retiredDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
		expect(payload.retiredDate).not.toContain('T');
	});

	it('records the local calendar date, not the UTC one, for an evening retire', () => {
		// 19:55 on Sep 1 in any timezone behind UTC is already Sep 2 in UTC.
		// Slicing toISOString() would file the device under the wrong day.
		expect(toDateOnly(new Date(2026, 8, 1, 19, 55))).toBe('2026-09-01');
		expect(toDateOnly(new Date(2026, 0, 5, 0, 30))).toBe('2026-01-05');
	});

	it('zero-pads single-digit months and days', () => {
		expect(toDateOnly(new Date(2026, 0, 9))).toBe('2026-01-09');
	});

	it('builds an update payload that clears retirement metadata and sets active status', () => {
		const payload = buildUnretireDeviceRequest({
			...activeDevice,
			status: 'Retired',
			retiredDate: '2026-06-23',
			disposalMethod: 'Stored'
		});

		expect(payload).toMatchObject({
			name: 'Aqua Flosser',
			brandId: 'brand-1',
			categoryId: 'category-1',
			status: 'Active',
			retiredDate: null,
			disposalMethod: null,
			version: 'abc'
		});
	});
});

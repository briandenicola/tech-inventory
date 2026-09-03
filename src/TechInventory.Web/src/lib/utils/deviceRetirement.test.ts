import { describe, expect, it } from 'vitest';
import type { DeviceResponse } from '$lib/queries/devices.svelte';
import { canChangeDeviceStatus, deviceStatusOptions } from './deviceRetirement';

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

describe('canChangeDeviceStatus', () => {
	it('allows an Admin to change status on an Active device', () => {
		expect(canChangeDeviceStatus(activeDevice, { id: 'admin-1', role: 'Admin' })).toBe(true);
	});

	it('allows an Admin to change status regardless of the current non-terminal status', () => {
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'Retired' }, { id: 'admin-1', role: 'Admin' })
		).toBe(true);
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'InRepair' }, { id: 'admin-1', role: 'Admin' })
		).toBe(true);
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'Lent' }, { id: 'admin-1', role: 'Admin' })
		).toBe(true);
	});

	it('allows an owning Member to change status', () => {
		expect(canChangeDeviceStatus(activeDevice, { id: 'member-1', role: 'Member' })).toBe(true);
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'Retired' }, { id: 'member-1', role: 'Member' })
		).toBe(true);
	});

	it('blocks a non-owning Member', () => {
		expect(canChangeDeviceStatus(activeDevice, { id: 'member-2', role: 'Member' })).toBe(false);
	});

	it('blocks a Viewer even when they own the device', () => {
		expect(canChangeDeviceStatus(activeDevice, { id: 'member-1', role: 'Viewer' })).toBe(false);
	});

	it('blocks changing status on an already-Disposed device — a real terminal state the bulk-update API 409s on', () => {
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'Disposed' }, { id: 'admin-1', role: 'Admin' })
		).toBe(false);
		expect(
			canChangeDeviceStatus({ ...activeDevice, status: 'Disposed' }, { id: 'member-1', role: 'Member' })
		).toBe(false);
	});

	it('returns false for a null device or a signed-out user', () => {
		expect(canChangeDeviceStatus(null, { id: 'admin-1', role: 'Admin' })).toBe(false);
		expect(canChangeDeviceStatus(activeDevice, null)).toBe(false);
		expect(canChangeDeviceStatus(activeDevice, undefined)).toBe(false);
	});
});

describe('deviceStatusOptions', () => {
	it('returns one translated { id, name } option per DeviceStatus value, in enum order', () => {
		const translate = (key: string) => `translated:${key}`;

		expect(deviceStatusOptions(translate)).toEqual([
			{ id: 'Active', name: 'translated:devices.filters.statusActive' },
			{ id: 'Retired', name: 'translated:devices.filters.statusRetired' },
			{ id: 'Disposed', name: 'translated:devices.filters.statusDisposed' },
			{ id: 'InRepair', name: 'translated:devices.filters.statusInRepair' },
			{ id: 'Lent', name: 'translated:devices.filters.statusLent' }
		]);
	});
});

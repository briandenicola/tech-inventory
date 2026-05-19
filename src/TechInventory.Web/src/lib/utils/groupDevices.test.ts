/**
 * groupDevices helper tests (F023).
 *
 * Covers partitioning, label lookup, Unknown bucket handling, and group sort
 * order across all three dimensions.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { groupDevices } from './groupDevices';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const refData = {
	categories: [
		{ id: 'cat-laptops', name: 'Laptops' },
		{ id: 'cat-networking', name: 'Networking' },
		{ id: 'cat-appliances', name: 'Appliances' }
	],
	owners: [
		{ id: 'owner-brian', name: 'Brian' },
		{ id: 'owner-alex', name: 'Alex' }
	]
};

describe('groupDevices', () => {
	beforeEach(() => {
		resetFactories();
	});

	describe('groupBy: category', () => {
		it('partitions devices by categoryId and labels with reference data', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', categoryId: 'cat-laptops' }),
				createDeviceResponse({ id: 'd2', categoryId: 'cat-networking' }),
				createDeviceResponse({ id: 'd3', categoryId: 'cat-laptops' })
			];

			const groups = groupDevices(devices, 'category', refData);

			expect(groups).toHaveLength(2);
			const laptops = groups.find((g) => g.key === 'cat-laptops')!;
			expect(laptops.label).toBe('Laptops');
			expect(laptops.count).toBe(2);
			expect(laptops.devices.map((d) => d.id)).toEqual(['d1', 'd3']);
		});

		it('sorts groups alphabetically', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', categoryId: 'cat-networking' }),
				createDeviceResponse({ id: 'd2', categoryId: 'cat-appliances' }),
				createDeviceResponse({ id: 'd3', categoryId: 'cat-laptops' })
			];

			const groups = groupDevices(devices, 'category', refData);

			expect(groups.map((g) => g.label)).toEqual(['Appliances', 'Laptops', 'Networking']);
		});
	});

	describe('groupBy: owner', () => {
		it('partitions by ownerId and labels with owner name', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', ownerId: 'owner-brian' }),
				createDeviceResponse({ id: 'd2', ownerId: 'owner-alex' }),
				createDeviceResponse({ id: 'd3', ownerId: 'owner-brian' })
			];

			const groups = groupDevices(devices, 'owner', refData);

			expect(groups.map((g) => g.label)).toEqual(['Alex', 'Brian']);
			expect(groups.find((g) => g.key === 'owner-brian')!.count).toBe(2);
		});
	});

	describe('groupBy: year', () => {
		it('partitions by purchase-date year and sorts most recent first', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', purchaseDate: '2024-03-15' }),
				createDeviceResponse({ id: 'd2', purchaseDate: '2022-07-01' }),
				createDeviceResponse({ id: 'd3', purchaseDate: '2024-11-20' }),
				createDeviceResponse({ id: 'd4', purchaseDate: '2023-05-10' })
			];

			const groups = groupDevices(devices, 'year', refData);

			expect(groups.map((g) => g.key)).toEqual(['2024', '2023', '2022']);
			expect(groups[0].count).toBe(2);
		});

		it('sends devices with no purchaseDate to the Unknown bucket', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', purchaseDate: '2024-01-01' }),
				createDeviceResponse({ id: 'd2', purchaseDate: null })
			];

			const groups = groupDevices(devices, 'year', refData);

			expect(groups).toHaveLength(2);
			expect(groups[groups.length - 1].isUnknown).toBe(true);
			expect(groups[groups.length - 1].devices[0].id).toBe('d2');
		});

		it('preserves input device order within each year bucket', () => {
			const devices = [
				createDeviceResponse({ id: 'd1', name: 'Alpha', purchaseDate: '2024-01-01' }),
				createDeviceResponse({ id: 'd2', name: 'Beta', purchaseDate: '2024-06-01' }),
				createDeviceResponse({ id: 'd3', name: 'Gamma', purchaseDate: '2024-12-01' })
			];

			const groups = groupDevices(devices, 'year', refData);

			expect(groups[0].devices.map((d) => d.name)).toEqual(['Alpha', 'Beta', 'Gamma']);
		});
	});

	it('returns an empty array when given an empty device list', () => {
		expect(groupDevices([], 'category', refData)).toEqual([]);
		expect(groupDevices([], 'owner', refData)).toEqual([]);
		expect(groupDevices([], 'year', refData)).toEqual([]);
	});
});

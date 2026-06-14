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

	describe('Large dataset scenarios (regression for >200 device bug)', () => {
		it('partitions 250 devices correctly across groups without truncation', () => {
			// Regression test: grouped view must not silently truncate at 200
			const devices = [];
			for (let i = 1; i <= 250; i++) {
				const categoryId = i <= 100 ? 'cat-laptops' : i <= 200 ? 'cat-networking' : 'cat-appliances';
				devices.push(createDeviceResponse({ id: `d${i}`, categoryId }));
			}

			const groups = groupDevices(devices, 'category', refData);

			expect(groups).toHaveLength(3);
			expect(groups.find((g) => g.key === 'cat-laptops')!.count).toBe(100);
			expect(groups.find((g) => g.key === 'cat-networking')!.count).toBe(100);
			expect(groups.find((g) => g.key === 'cat-appliances')!.count).toBe(50);

			// Verify device on "page 3" (index 201) is included
			const appliancesGroup = groups.find((g) => g.key === 'cat-appliances')!;
			expect(appliancesGroup.devices).toContainEqual(
				expect.objectContaining({ id: 'd201' })
			);
		});

		it('handles filter-then-group scenario with 150 devices', () => {
			// Simulates: user filters by brand, then groups by category
			// All 150 filtered devices should be grouped, not just first 200
			const devices = [];
			for (let i = 1; i <= 150; i++) {
				const categoryId = i <= 75 ? 'cat-laptops' : 'cat-networking';
				devices.push(
					createDeviceResponse({
						id: `dell-${i}`,
						brandId: 'brand-dell',
						categoryId
					})
				);
			}

			const groups = groupDevices(devices, 'category', refData);

			expect(groups).toHaveLength(2);
			expect(groups.find((g) => g.key === 'cat-laptops')!.count).toBe(75);
			expect(groups.find((g) => g.key === 'cat-networking')!.count).toBe(75);

			// Verify no devices are orphaned
			const totalGroupedDevices = groups.reduce((sum, g) => sum + g.count, 0);
			expect(totalGroupedDevices).toBe(150);
		});

		it('preserves all devices when grouping by owner with 300 devices', () => {
			const devices = [];
			for (let i = 1; i <= 300; i++) {
				const ownerId = i <= 150 ? 'owner-brian' : 'owner-alex';
				devices.push(createDeviceResponse({ id: `d${i}`, ownerId }));
			}

			const groups = groupDevices(devices, 'owner', refData);

			expect(groups).toHaveLength(2);
			expect(groups.find((g) => g.key === 'owner-brian')!.count).toBe(150);
			expect(groups.find((g) => g.key === 'owner-alex')!.count).toBe(150);

			// Verify device 300 (last device) is included
			const alexGroup = groups.find((g) => g.key === 'owner-alex')!;
			expect(alexGroup.devices).toContainEqual(expect.objectContaining({ id: 'd300' }));
		});

		it('handles year grouping with 400 devices across multiple years', () => {
			const devices = [];
			for (let i = 1; i <= 400; i++) {
				const year = 2020 + Math.floor((i - 1) / 100); // 100 devices per year: 2020, 2021, 2022, 2023
				devices.push(
					createDeviceResponse({
						id: `d${i}`,
						purchaseDate: `${year}-01-01`
					})
				);
			}

			const groups = groupDevices(devices, 'year', refData);

			expect(groups).toHaveLength(4);
			expect(groups.map((g) => g.key)).toEqual(['2023', '2022', '2021', '2020']); // Most recent first
			groups.forEach((g) => {
				expect(g.count).toBe(100);
			});

			// Verify device 400 (last device, year 2023) is included
			const year2023Group = groups.find((g) => g.key === '2023')!;
			expect(year2023Group.devices).toContainEqual(expect.objectContaining({ id: 'd400' }));
		});

		it('does not orphan devices when input is exactly 200', () => {
			// Edge case: exactly at the problematic threshold
			const devices = [];
			for (let i = 1; i <= 200; i++) {
				devices.push(createDeviceResponse({ id: `d${i}`, categoryId: 'cat-laptops' }));
			}

			const groups = groupDevices(devices, 'category', refData);

			expect(groups).toHaveLength(1);
			expect(groups[0].count).toBe(200);
			expect(groups[0].devices).toHaveLength(200);
		});
	});
});

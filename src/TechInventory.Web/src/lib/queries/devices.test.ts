import { beforeEach, describe, expect, it, vi } from 'vitest';
import { devices as devicesApi } from '$lib/api/client';
import {
	DEFAULT_DEVICE_PAGE_SIZE,
	MAX_DEVICE_PAGE_SIZE,
	clampDevicePage,
	clampDevicePageSize,
	fetchDevicesPage,
	serializeDeviceFilters
} from './devices.svelte';

vi.mock('$lib/api/client', () => ({
	devices: {
		list: vi.fn()
	}
}));

const mockedList = vi.mocked(devicesApi.list);

const sampleResponse = {
	items: [
		{
			id: '11111111-1111-4111-8111-111111111111',
			name: 'Router',
			model: 'AX6000',
			serialNumber: 'SN-001',
			brandId: '22222222-2222-4222-8222-222222222222',
			categoryId: '33333333-3333-4333-8333-333333333333',
			ownerId: '44444444-4444-4444-8444-444444444444',
			locationId: '55555555-5555-4555-8555-555555555555',
			networkId: '66666666-6666-4666-8666-666666666666',
			purchaseDate: '2024-01-15',
			purchasePrice: 199.99,
			currencyCode: 'USD',
			status: 'Active',
			notes: null,
			retiredDate: null,
			disposalMethod: null,
			createdAt: '2026-05-20T12:00:00Z',
			createdBy: null,
			modifiedAt: '2026-05-20T12:00:00Z',
			modifiedBy: null
		}
	],
	totalCount: 1,
	page: 1,
	pageSize: MAX_DEVICE_PAGE_SIZE
};

describe('devices query helpers', () => {
	beforeEach(() => {
		mockedList.mockReset();
	});

	it('clamps page numbers to a positive integer', () => {
		expect(clampDevicePage(undefined)).toBe(1);
		expect(clampDevicePage(0)).toBe(1);
		expect(clampDevicePage(3.8)).toBe(3);
	});

	it('clamps page size to the API maximum', () => {
		expect(clampDevicePageSize(undefined)).toBe(DEFAULT_DEVICE_PAGE_SIZE);
		expect(clampDevicePageSize(0)).toBe(1);
		expect(clampDevicePageSize(500)).toBe(MAX_DEVICE_PAGE_SIZE);
	});

	it('serializes filters stably for cache keys', () => {
		const first = serializeDeviceFilters({ search: 'router', page: 1, pageSize: 25 });
		const second = serializeDeviceFilters({ pageSize: 25, page: 1, search: 'router' });

		expect(first).toBe(second);
	});

	it('maps filters to API params and validates the response', async () => {
		mockedList.mockResolvedValue(sampleResponse);

		const response = await fetchDevicesPage({
			page: 0,
			pageSize: 500,
			search: 'router',
			status: ['Active'],
			sort: 'name',
			sortDir: 'desc'
		});

		expect(mockedList).toHaveBeenCalledWith({
			Page: 1,
			PageSize: MAX_DEVICE_PAGE_SIZE,
			Search: 'router',
			Status: 'Active',
			SortBy: 'name',
			SortDescending: true
		});
		expect(response.pageSize).toBe(MAX_DEVICE_PAGE_SIZE);
		expect(response.items?.[0]?.name).toBe('Router');
	});
});

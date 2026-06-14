import { beforeEach, describe, expect, it, vi } from 'vitest';
import { devices as devicesApi } from '$lib/api/client';
import {
	DEFAULT_DEVICE_PAGE_SIZE,
	MAX_DEVICE_PAGE_SIZE,
	clampDevicePage,
	clampDevicePageSize,
	fetchDevicesPage,
	fetchAllDevicesForGrouping,
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

	it('omits Status and IncludeAllStatuses when status filter is undefined', async () => {
		mockedList.mockResolvedValue(sampleResponse);

		await fetchDevicesPage({
			page: 1,
			pageSize: 25,
			status: undefined
		});

		const call = mockedList.mock.calls[0][0];
		expect(call).not.toHaveProperty('Status');
		expect(call).not.toHaveProperty('IncludeAllStatuses');
	});

	it('omits Status and IncludeAllStatuses when status filter is empty array', async () => {
		mockedList.mockResolvedValue(sampleResponse);

		await fetchDevicesPage({
			page: 1,
			pageSize: 25,
			status: []
		});

		const call = mockedList.mock.calls[0][0];
		expect(call).not.toHaveProperty('Status');
		expect(call).not.toHaveProperty('IncludeAllStatuses');
	});

	it('sends IncludeAllStatuses for explicit all-status requests', async () => {
		mockedList.mockResolvedValue(sampleResponse);

		await fetchDevicesPage({
			page: 1,
			pageSize: 25,
			includeAllStatuses: true
		});

		expect(mockedList).toHaveBeenCalledWith({
			Page: 1,
			PageSize: 25,
			IncludeAllStatuses: true
		});
	});

	describe('fetchAllDevicesForGrouping', () => {
		it('fetches only first page when total count is under 200', async () => {
			mockedList.mockResolvedValue({
				...sampleResponse,
				totalCount: 150,
				pageSize: MAX_DEVICE_PAGE_SIZE
			});

			const result = await fetchAllDevicesForGrouping({
				page: 1,
				pageSize: 25,
				status: ['Active']
			});

			expect(mockedList).toHaveBeenCalledTimes(1);
			expect(result.totalCount).toBe(150);
			expect(result.items?.length).toBe(1);
		});

		it('fetches multiple pages when total count exceeds 200', async () => {
			// First page response - using single valid device repeated
			const firstPageItems = Array.from({ length: 200 }, () => ({
				...sampleResponse.items[0]
			}));

			// Second page response
			const secondPageItems = Array.from({ length: 150 }, () => ({
				...sampleResponse.items[0]
			}));

			mockedList
				.mockResolvedValueOnce({
					items: firstPageItems,
					totalCount: 350,
					page: 1,
					pageSize: MAX_DEVICE_PAGE_SIZE
				})
				.mockResolvedValueOnce({
					items: secondPageItems,
					totalCount: 350,
					page: 2,
					pageSize: MAX_DEVICE_PAGE_SIZE
				});

			const result = await fetchAllDevicesForGrouping({
				page: 1,
				pageSize: 25,
				status: ['Active']
			});

			expect(mockedList).toHaveBeenCalledTimes(2);
			expect(mockedList).toHaveBeenNthCalledWith(1, {
				Page: 1,
				PageSize: MAX_DEVICE_PAGE_SIZE,
				Status: 'Active'
			});
			expect(mockedList).toHaveBeenNthCalledWith(2, {
				Page: 2,
				PageSize: MAX_DEVICE_PAGE_SIZE,
				Status: 'Active'
			});
			expect(result.totalCount).toBe(350);
			expect(result.items?.length).toBe(350);
			expect(result.page).toBe(1);
			expect(result.pageSize).toBe(350);
		});

		it('fetches three pages when total count exceeds 400', async () => {
			const page1 = Array.from({ length: 200 }, () => ({
				...sampleResponse.items[0]
			}));

			const page2 = Array.from({ length: 200 }, () => ({
				...sampleResponse.items[0]
			}));

			const page3 = Array.from({ length: 100 }, () => ({
				...sampleResponse.items[0]
			}));

			mockedList
				.mockResolvedValueOnce({
					items: page1,
					totalCount: 500,
					page: 1,
					pageSize: MAX_DEVICE_PAGE_SIZE
				})
				.mockResolvedValueOnce({
					items: page2,
					totalCount: 500,
					page: 2,
					pageSize: MAX_DEVICE_PAGE_SIZE
				})
				.mockResolvedValueOnce({
					items: page3,
					totalCount: 500,
					page: 3,
					pageSize: MAX_DEVICE_PAGE_SIZE
				});

			const result = await fetchAllDevicesForGrouping({
				page: 1,
				pageSize: 25,
				brandId: 'brand-123'
			});

			expect(mockedList).toHaveBeenCalledTimes(3);
			expect(result.totalCount).toBe(500);
			expect(result.items?.length).toBe(500);
		});

		it('preserves filters when fetching multiple pages', async () => {
			mockedList
				.mockResolvedValueOnce({
					items: Array.from({ length: 200 }, () => ({ ...sampleResponse.items[0] })),
					totalCount: 250,
					page: 1,
					pageSize: MAX_DEVICE_PAGE_SIZE
				})
				.mockResolvedValueOnce({
					items: Array.from({ length: 50 }, () => ({ ...sampleResponse.items[0] })),
					totalCount: 250,
					page: 2,
					pageSize: MAX_DEVICE_PAGE_SIZE
				});

			await fetchAllDevicesForGrouping({
				page: 1,
				pageSize: 25,
				search: 'laptop',
				brandId: 'brand-123',
				categoryId: 'cat-456',
				includeAllStatuses: true
			});

			expect(mockedList).toHaveBeenNthCalledWith(1, {
				Page: 1,
				PageSize: MAX_DEVICE_PAGE_SIZE,
				Search: 'laptop',
				BrandId: 'brand-123',
				CategoryId: 'cat-456',
				IncludeAllStatuses: true
			});
			expect(mockedList).toHaveBeenNthCalledWith(2, {
				Page: 2,
				PageSize: MAX_DEVICE_PAGE_SIZE,
				Search: 'laptop',
				BrandId: 'brand-123',
				CategoryId: 'cat-456',
				IncludeAllStatuses: true
			});
		});
	});
});

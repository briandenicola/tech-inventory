/**
 * T14: Devices List Query Hook — Svelte 5 Runes Pattern
 *
 * useDevices() provides reactive server state for device list filters,
 * sorting, pagination, and infinite-scroll page fetches.
 *
 * Related: specs/002-frontend-mvp/spec.md §5 (Devices list), J4
 */

import { devices } from '$lib/api/client';
import { z } from 'zod';

export const DEFAULT_DEVICE_PAGE_SIZE = 25;
export const MAX_DEVICE_PAGE_SIZE = 200;

/**
 * DeviceStatus enum (mirror OpenAPI spec)
 */
export const DeviceStatus = z.enum(['Active', 'Retired', 'Disposed', 'InRepair', 'Lent']);
export type DeviceStatus = z.infer<typeof DeviceStatus>;
export type DeviceSort = 'name' | 'purchaseDate' | 'createdAt';
export type DeviceSortDirection = 'asc' | 'desc';

/**
 * DeviceResponse schema (runtime validation mirror of OpenAPI)
 */
export const DeviceResponseSchema = z.object({
	id: z.string().uuid(),
	name: z.string().nullable(),
	model: z.string().nullable(),
	serialNumber: z.string().nullable(),
	brandId: z.string().uuid().nullable(),
	categoryId: z.string().uuid(),
	ownerId: z.string().uuid().nullable(),
	locationId: z.string().uuid().nullable(),
	networkId: z.string().uuid().nullable(),
	purchaseDate: z.string().nullable(),
	purchasePrice: z.number().nullable(),
	currencyCode: z.string().nullable(),
	status: z.string().nullable(),
	notes: z.string().nullable(),
	retiredDate: z.string().nullable(),
	disposalMethod: z.string().nullable(),
	purpose: z.string().nullish(),
	operatingSystem: z.string().nullish(),
	ipAddress: z.string().nullish(),
	macAddress: z.string().nullish(),
	productUrl: z.string().nullish(),
	version: z.string().nullish(),
	createdAt: z.string(),
	createdBy: z.string().nullable(),
	modifiedAt: z.string(),
	modifiedBy: z.string().nullable()
});

export type DeviceResponse = z.infer<typeof DeviceResponseSchema>;

/**
 * PaginatedResponse generic (mirror server-side PagedResponse<T>)
 */
export const PaginatedResponseSchema = <T extends z.ZodTypeAny>(itemSchema: T) =>
	z.object({
		items: z.array(itemSchema).nullable(),
		totalCount: z.number().int(),
		page: z.number().int(),
		pageSize: z.number().int()
	});

export type PaginatedResponse<T> = {
	items: T[] | null;
	totalCount: number;
	page: number;
	pageSize: number;
};

/**
 * Device filters shape (maps to /api/v1/devices query params)
 */
export interface DeviceFilters {
	search?: string;
	brandId?: string;
	categoryId?: string;
	ownerId?: string;
	locationId?: string;
	networkId?: string;
	status?: DeviceStatus[];
	purchaseYearMin?: number;
	purchaseYearMax?: number;
	sort?: DeviceSort;
	sortDir?: DeviceSortDirection;
	page?: number;
	pageSize?: number;
}

interface DeviceFiltersParams {
	Page?: number;
	PageSize?: number;
	Search?: string;
	BrandId?: string;
	CategoryId?: string;
	OwnerId?: string;
	LocationId?: string;
	NetworkId?: string;
	Status?: DeviceStatus;
	PurchaseYearFrom?: number;
	PurchaseYearTo?: number;
	SortBy?: DeviceSort;
	SortDescending?: boolean;
}

/**
 * Query result shape
 */
export interface DevicesQueryResult {
	data: PaginatedResponse<DeviceResponse> | null;
	isLoading: boolean;
	error: string | null;
	refetch: () => Promise<void>;
}

type DeviceFiltersSource = DeviceFilters | (() => DeviceFilters);

const cache = new Map<string, PaginatedResponse<DeviceResponse>>();

export function clampDevicePage(page: number | undefined): number {
	if (typeof page !== 'number' || !Number.isFinite(page)) {
		return 1;
	}

	return Math.max(1, Math.floor(page));
}

export function clampDevicePageSize(pageSize: number | undefined): number {
	if (typeof pageSize !== 'number' || !Number.isFinite(pageSize)) {
		return DEFAULT_DEVICE_PAGE_SIZE;
	}

	return Math.min(MAX_DEVICE_PAGE_SIZE, Math.max(1, Math.floor(pageSize)));
}

export function normalizeDeviceFilters(filters: DeviceFilters): DeviceFilters {
	return {
		...filters,
		page: clampDevicePage(filters.page),
		pageSize: clampDevicePageSize(filters.pageSize),
		sortDir: filters.sort ? (filters.sortDir ?? 'asc') : filters.sortDir,
		status: filters.status ? [...filters.status].sort() : undefined
	};
}

export function serializeDeviceFilters(filters: DeviceFilters): string {
	const normalized = normalizeDeviceFilters(filters);
	const sorted = Object.keys(normalized)
		.sort()
		.reduce((acc, key) => {
			const value = normalized[key as keyof DeviceFilters];
			if (value !== undefined && value !== null) {
				acc[key] = value;
			}
			return acc;
		}, {} as Record<string, unknown>);

	return JSON.stringify(sorted);
}

function resolveFilters(filters: DeviceFiltersSource): DeviceFilters {
	return typeof filters === 'function' ? filters() : filters;
}

function buildDeviceQueryParams(filters: DeviceFilters): DeviceFiltersParams {
	const params: DeviceFiltersParams = {
		Page: filters.page,
		PageSize: filters.pageSize
	};

	if (filters.search) params.Search = filters.search;
	if (filters.brandId) params.BrandId = filters.brandId;
	if (filters.categoryId) params.CategoryId = filters.categoryId;
	if (filters.ownerId) params.OwnerId = filters.ownerId;
	if (filters.locationId) params.LocationId = filters.locationId;
	if (filters.networkId) params.NetworkId = filters.networkId;
	if (filters.status && filters.status.length > 0) {
		params.Status = filters.status[0];
	}
	if (filters.purchaseYearMin !== undefined) {
		params.PurchaseYearFrom = filters.purchaseYearMin;
	}
	if (filters.purchaseYearMax !== undefined) {
		params.PurchaseYearTo = filters.purchaseYearMax;
	}
	if (filters.sort) {
		params.SortBy = filters.sort;
		params.SortDescending = filters.sortDir === 'desc';
	}

	return params;
}

export async function fetchDevicesPage(
	filters: DeviceFilters
): Promise<PaginatedResponse<DeviceResponse>> {
	const normalizedFilters = normalizeDeviceFilters(filters);
	const response = await devices.list(buildDeviceQueryParams(normalizedFilters));

	return PaginatedResponseSchema(DeviceResponseSchema).parse(response);
}

/**
 * useDevices() — Svelte 5 runes-based reactive query hook
 *
 * Pass a closure when the filter source is itself reactive to avoid capturing
 * only the initial object value.
 */
export function useDevices(filters: DeviceFiltersSource): DevicesQueryResult {
	let data = $state<PaginatedResponse<DeviceResponse> | null>(null);
	let isLoading = $state<boolean>(true);
	let error = $state<string | null>(null);

	const normalizedFilters = $derived(normalizeDeviceFilters(resolveFilters(filters)));
	const cacheKey = $derived(serializeDeviceFilters(normalizedFilters));

	async function fetchDevices() {
		isLoading = true;
		error = null;
		data = null;

		const cached = cache.get(cacheKey);
		if (cached) {
			data = cached;
			isLoading = false;
			return;
		}

		try {
			const validated = await fetchDevicesPage(normalizedFilters);
			cache.set(cacheKey, validated);
			data = validated;
		} catch (err) {
			console.error('[devices] Fetch error:', err);
			error = err instanceof Error ? err.message : 'Failed to fetch devices';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		void cacheKey;
		void normalizedFilters;
		void fetchDevices();
	});

	const refetch = async () => {
		cache.delete(cacheKey);
		await fetchDevices();
	};

	return {
		get data() {
			return data;
		},
		get isLoading() {
			return isLoading;
		},
		get error() {
			return error;
		},
		refetch
	};
}

export function invalidateDevicesCache(): void {
	cache.clear();
}

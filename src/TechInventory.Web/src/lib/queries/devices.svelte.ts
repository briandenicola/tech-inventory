/**
 * T14: Devices List Query Hook — Svelte 5 Runes Pattern
 *
 * useDevices() hook provides reactive server state for device list with
 * filter/sort/pagination. Auto-refetches when filters change ($derived + $effect).
 * Simple cache by serialized filter key; invalidation wired in R4 (CRUD mutations).
 *
 * Related: specs/002-frontend-mvp/spec.md §5 (Devices list), J4
 */

import { devices } from '$lib/api/client';
import { z } from 'zod';

export const DEFAULT_DEVICE_PAGE_SIZE = 25;
export const MAX_DEVICE_PAGE_SIZE = 200;
export type DeviceSort = 'name' | 'purchaseDate' | 'createdAt';
export type DeviceSortDirection = 'asc' | 'desc';

/**
 * DeviceStatus enum (mirror OpenAPI spec)
 */
export const DeviceStatus = z.enum(['Active', 'Retired', 'Disposed', 'InRepair', 'Lent']);
export type DeviceStatus = z.infer<typeof DeviceStatus>;

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
	purchaseDate: z.string().nullable(), // ISO date string
	purchasePrice: z.number().nullable(),
	currencyCode: z.string().nullable(),
	status: z.string().nullable(),
	notes: z.string().nullable(),
	retiredDate: z.string().nullable(),
	disposalMethod: z.string().nullable(),
	// F034: server has always returned these fields; the Zod mirror was missing
	// them, so the UI silently dropped Purpose/OS/IP/MAC/URL/Version even when
	// imports populated them. Adding them lets the detail page render them.
	purpose: z.string().nullable().optional(),
	operatingSystem: z.string().nullable().optional(),
	ipAddress: z.string().nullable().optional(),
	macAddress: z.string().nullable().optional(),
	productUrl: z.string().nullable().optional(),
	version: z.string().nullable().optional(),
	createdAt: z.string(), // ISO datetime
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
 *
 * Note: OpenAPI uses PascalCase query params (Page, PageSize, Search, etc.)
 * but API client wrapper (client.ts) accepts camelCase and passes raw params object.
 * Our hook uses camelCase internally; wrapper maps to API shape.
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
	/** F023: client-side grouping dimension. Undefined => flat list. */
	groupBy?: 'category' | 'owner' | 'year';
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

/**
 * Simple in-memory cache by serialized filter key.
 *
 * Entries carry a fetch timestamp and are treated as stale after CACHE_TTL_MS.
 * Without a TTL, a transiently-empty result (e.g. before the user added
 * matching devices) would stick for the entire SPA session — Brian hit this
 * with the F026 Active-default filter showing "No devices yet" indefinitely
 * after a state where no Active devices existed.
 *
 * Mutations (create/update/delete/bulk) also clear the cache via
 * `invalidateDevicesCache()`.
 */
const CACHE_TTL_MS = 30_000;
interface CacheEntry {
	value: PaginatedResponse<DeviceResponse>;
	fetchedAt: number;
}
const cache = new Map<string, CacheEntry>();

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
 * Usage:
 * ```
 * let filters = $state({ page: 1, pageSize: 25, search: 'iPhone' });
 * const query = useDevices(() => filters);
 *
 * $effect(() => {
 *   // query.data, query.isLoading, query.error are reactive
 * });
 * ```
 *
 * Auto-refetches when filters change via $derived + $effect internally.
 */
/**
 * Devices query hook (Svelte 5 runes)
 *
 * Accepts a *getter* so updates to the caller's reactive filters propagate
 * into the hook. Passing a plain object snapshot would freeze the hook on
 * the first render and silently swallow every subsequent filter change —
 * the exact bug that hid `?search=` typing from the backend prior to this
 * refactor.
 *
 * Usage:
 * ```
 * const urlFilters = $derived.by(() => buildFiltersFromUrl());
 * const query = useDevices(() => urlFilters);
 * ```
 *
 * Auto-refetches when the caller's reactive filters change.
 */
export function useDevices(getFilters: () => DeviceFilters): DevicesQueryResult {
	// Internal reactive state
	let data = $state<PaginatedResponse<DeviceResponse> | null>(null);
	let isLoading = $state<boolean>(true);
	let error = $state<string | null>(null);

	const normalizedFilters = $derived(normalizeDeviceFilters(getFilters()));
	const cacheKey = $derived(serializeDeviceFilters(normalizedFilters));

	// Fetch function (called on mount + filter change)
	async function fetchDevices() {
		isLoading = true;
		error = null;

		const filters = normalizedFilters;
		const cached = cache.get(cacheKey);
		if (cached && Date.now() - cached.fetchedAt < CACHE_TTL_MS) {
			data = cached.value;
			isLoading = false;
			return;
		}

		try {
			const validated = await fetchDevicesPage(filters);
			cache.set(cacheKey, { value: validated, fetchedAt: Date.now() });
			data = validated;
		} catch (err) {
			console.error('[devices] Fetch error:', err);
			error = err instanceof Error ? err.message : 'Failed to fetch devices';
		} finally {
			isLoading = false;
		}
	}

	// $effect: refetch when cacheKey changes (i.e., filters change)
	$effect(() => {
		void normalizedFilters;
		void cacheKey;
		void fetchDevices();
	});

	// Expose refetch function for manual refresh
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

/**
 * Clear entire devices cache (e.g., after device CRUD mutation in Round 4)
 */
export function invalidateDevicesCache(): void {
	cache.clear();
}

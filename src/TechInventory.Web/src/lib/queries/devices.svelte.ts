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
	ownerId: z.string().uuid(),
	locationId: z.string().uuid(),
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
	sort?: 'name' | 'purchaseDate' | 'createdAt';
	sortDir?: 'asc' | 'desc';
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

/**
 * Serialize filters to cache key (stable sort keys)
 */
function serializeFilters(filters: DeviceFilters): string {
	// Sort keys for stable serialization
	const sorted = Object.keys(filters)
		.sort()
		.reduce((acc, key) => {
			const val = filters[key as keyof DeviceFilters];
			if (val !== undefined && val !== null) {
				acc[key] = val;
			}
			return acc;
		}, {} as Record<string, unknown>);

	return JSON.stringify(sorted);
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

	// Serialize filters for cache key — recomputes whenever the caller's
	// reactive filters change because we invoke the getter inside $derived.
	const cacheKey = $derived(serializeFilters(getFilters()));

	// Fetch function (called on mount + filter change)
	async function fetchDevices() {
		isLoading = true;
		error = null;

		// Snapshot filters at fetch time so the request body matches the
		// cacheKey that triggered it.
		const filters = getFilters();

		// Check cache first; honour TTL so transient empty results don't pin
		// the UI to "No devices yet" for the rest of the session.
		const cached = cache.get(cacheKey);
		if (cached && Date.now() - cached.fetchedAt < CACHE_TTL_MS) {
			data = cached.value;
			isLoading = false;
			return;
		}

		try {
			// Map filters to API query params shape (PascalCase)
			// OpenAPI spec uses: Page, PageSize, Search, BrandId, etc.
			const params: Record<string, unknown> = {};
			if (filters.page !== undefined) params.Page = filters.page;
			if (filters.pageSize !== undefined) params.PageSize = filters.pageSize;
			if (filters.search) params.Search = filters.search;
			if (filters.brandId) params.BrandId = filters.brandId;
			if (filters.categoryId) params.CategoryId = filters.categoryId;
			if (filters.ownerId) params.OwnerId = filters.ownerId;
			if (filters.locationId) params.LocationId = filters.locationId;
			if (filters.networkId) params.NetworkId = filters.networkId;
			if (filters.status && filters.status.length > 0) {
				// API expects single Status enum, not array — take first or join?
				// From spec: Status is single enum, not multi-select. We store as array for UI;
				// for now, only send first status. Round 4 may need backend support for multi-status.
				params.Status = filters.status[0];
			}
			if (filters.purchaseYearMin !== undefined)
				params.PurchaseYearFrom = filters.purchaseYearMin;
			if (filters.purchaseYearMax !== undefined) params.PurchaseYearTo = filters.purchaseYearMax;

			// Sort params: SortBy (string), SortDescending (bool)
			if (filters.sort) {
				params.SortBy = filters.sort;
				params.SortDescending = filters.sortDir === 'desc';
			}

			// Call API (client.ts devices.list)
			const response = await devices.list(params);

			// Validate response schema
			const validated = PaginatedResponseSchema(DeviceResponseSchema).parse(response);

			// Cache result
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
		// Read cacheKey to establish reactive dependency
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

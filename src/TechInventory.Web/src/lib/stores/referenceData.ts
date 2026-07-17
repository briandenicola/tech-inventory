/**
 * Reference Data Store — Brands, Categories, Owners, Locations, Networks
 * 
 * Per T16: Reference entities (brands, categories, etc.) are slow-changing.
 * Fetch once on component mount; cache in module-level store. Don't refetch
 * on every filter change.
 * 
 * Related: specs/002-frontend-mvp/spec.md §5 (Devices list filters), D-046+
 */

import { writable } from 'svelte/store';
import { brands, categories, owners, locations, networks, tags } from '$lib/api/client';
import type { components } from '$lib/api/generated/types';

type TagResponse = components['schemas']['TagResponse'];

/**
 * Simple reference entity shape (name + id)
 */
export type ReferenceEntity = {
	id: string;
	name: string;
};

export type ReferenceTag = TagResponse & {
	id: string;
	name: string;
	color: string | null;
};

/**
 * Reference data state
 */
export interface ReferenceDataState {
	brands: ReferenceEntity[];
	categories: ReferenceEntity[];
	owners: ReferenceEntity[];
	locations: ReferenceEntity[];
	networks: ReferenceEntity[];
	tags: ReferenceTag[];
	isLoading: boolean;
	error: string | null;
}

const initialState: ReferenceDataState = {
	brands: [],
	categories: [],
	owners: [],
	locations: [],
	networks: [],
	tags: [],
	isLoading: false,
	error: null
};

/**
 * Reference data store (writable)
 */
export const referenceDataStore = writable<ReferenceDataState>(initialState);

let inFlightFetch: Promise<boolean> | null = null;
let hasLoadedOnce = false;

/**
 * Fetch all reference data (call on component mount).
 *
 * Several independent components (the authenticated layout, DeviceForm,
 * DeviceFilters, report cards, ...) each call this on their own mount —
 * that's by design, since any of them could be the first thing to render.
 * Without de-duplication here, mounting several of them in the same tick
 * (e.g. opening the Add Device modal, which mounts DeviceForm on top of a
 * page the layout + DeviceFilters already populated) fires the same six
 * GETs redundantly. A plain `isLoading` check on the store isn't enough to
 * prevent that, since the store only updates asynchronously — two mounts
 * in the same synchronous tick both see `isLoading: false` before either
 * call has had a chance to flip it. Track the in-flight promise directly
 * instead, and skip re-fetching once we already have data.
 *
 * Pass `{ force: true }` after a mutation (creating/merging/deleting a
 * reference entity) to bypass the cache and get fresh data.
 */
export async function fetchReferenceData(options?: { force?: boolean }): Promise<void> {
	if (!options?.force) {
		if (inFlightFetch) {
			await inFlightFetch;
			return;
		}
		if (hasLoadedOnce) return;
	}

	const fetchPromise = performFetch();
	inFlightFetch = fetchPromise;
	try {
		hasLoadedOnce = await fetchPromise;
	} finally {
		if (inFlightFetch === fetchPromise) {
			inFlightFetch = null;
		}
	}
}

/** Returns true on success so callers can tell a real load from a swallowed error. */
async function performFetch(): Promise<boolean> {
	referenceDataStore.update((state) => ({ ...state, isLoading: true, error: null }));

	try {
		// Fetch all in parallel.
		// pageSize is capped at 200 by Application validators (see Brands/Categories/
		// Owners/Locations/Networks/Tags ListQueryValidator) — asking for more would
		// 400 the whole reference-data fetch and leave every dropdown empty.
		const [brandsRes, categoriesRes, ownersRes, locationsRes, networksRes, tagsRes] = await Promise.all([
			brands.list({ pageSize: 200, includeInactive: false }),
			categories.list({ pageSize: 200, includeInactive: false }),
			owners.list({ pageSize: 200, includeInactive: false }),
			locations.list({ pageSize: 200, includeInactive: false }),
			networks.list({ pageSize: 200, includeInactive: false }),
			tags.list({ pageSize: 200, includeInactive: false })
		]);

		// Extract items (each response shape: { items: [], totalCount, page, pageSize })
		// Use type guards and nullish checks for safe property access
		const brandsData = brandsRes.items
			? brandsRes.items
					.filter((b): b is { id: string; name: string } => !!b.id && !!b.name)
					.map((b) => ({ id: b.id, name: b.name }))
			: [];
		const categoriesData = categoriesRes.items
			? categoriesRes.items
					.filter((c): c is { id: string; name: string } => !!c.id && !!c.name)
					.map((c) => ({ id: c.id, name: c.name }))
			: [];
		const ownersData = ownersRes.items
			? ownersRes.items
					.filter(
						(o): o is { id: string; displayName: string } => !!o.id && !!o.displayName
					)
					.map((o) => ({
						id: o.id,
						name: o.displayName
					}))
			: [];
		const locationsData = locationsRes.items
			? locationsRes.items
					.filter((l): l is { id: string; name: string } => !!l.id && !!l.name)
					.map((l) => ({ id: l.id, name: l.name }))
			: [];
		const networksData = networksRes.items
			? networksRes.items
					.filter((n): n is { id: string; name: string } => !!n.id && !!n.name)
					.map((n) => ({ id: n.id, name: n.name }))
			: [];
		// Tags keep the generated TagResponse fields while narrowing id/name/color
		// for components that require non-null swatches and labels.
		const tagsData: ReferenceTag[] = (tagsRes.items ?? [])
			.filter((tag): tag is TagResponse & { id: string; name: string } => !!tag.id && !!tag.name)
			.map((tag) => ({
				...tag,
				id: tag.id,
				name: tag.name,
				color: tag.color ?? null
			}));

		referenceDataStore.set({
			brands: brandsData,
			categories: categoriesData,
			owners: ownersData,
			locations: locationsData,
			networks: networksData,
			tags: tagsData,
			isLoading: false,
			error: null
		});
		return true;
	} catch (err) {
		console.error('[referenceData] Fetch error:', err);
		referenceDataStore.update((state) => ({
			...state,
			isLoading: false,
			error: err instanceof Error ? err.message : 'Failed to fetch reference data'
		}));
		return false;
	}
}

/**
 * Clear reference data (for logout or cache invalidation)
 */
export function clearReferenceData(): void {
	referenceDataStore.set(initialState);
	hasLoadedOnce = false;
	inFlightFetch = null;
}

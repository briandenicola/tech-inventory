/**
 * Reference Data Store — Brands, Categories, Owners, Locations, Networks, Tags
 * 
 * Per T16: Reference entities (brands, categories, etc.) are slow-changing.
 * Fetch once on component mount; cache in module-level store. Don't refetch
 * on every filter change.
 * 
 * Related: specs/002-frontend-mvp/spec.md §5 (Devices list filters), D-046+
 */

import { writable } from 'svelte/store';
import { brands, categories, owners, locations, networks, tags } from '$lib/api/client';

/**
 * Simple reference entity shape (name + id)
 */
export type ReferenceEntity = {
	id: string;
	name: string;
};

export type ReferenceTag = ReferenceEntity & {
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

/**
 * Fetch all reference data (call on component mount)
 */
export async function fetchReferenceData(): Promise<void> {
	referenceDataStore.update((state) => ({ ...state, isLoading: true, error: null }));

	try {
		const [brandsRes, categoriesRes, ownersRes, locationsRes, networksRes, tagsRes] = await Promise.all([
			brands.list({ pageSize: 1000, includeInactive: false }),
			categories.list({ pageSize: 1000, includeInactive: false }),
			owners.list({ pageSize: 1000, includeInactive: false }),
			locations.list({ pageSize: 1000, includeInactive: false }),
			networks.list({ pageSize: 1000, includeInactive: false }),
			tags.list({ pageSize: 1000, includeInactive: false })
		]);

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
		const tagsData = tagsRes.items
			? tagsRes.items
					.filter((tag): tag is { id: string; name: string; color?: string | null } => !!tag.id && !!tag.name)
					.map((tag) => ({
						id: tag.id,
						name: tag.name,
						color: tag.color ?? null
					}))
			: [];

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
	} catch (err) {
		console.error('[referenceData] Fetch error:', err);
		referenceDataStore.update((state) => ({
			...state,
			isLoading: false,
			error: err instanceof Error ? err.message : 'Failed to fetch reference data'
		}));
	}
}

/**
 * Clear reference data (for logout or cache invalidation)
 */
export function clearReferenceData(): void {
	referenceDataStore.set(initialState);
}

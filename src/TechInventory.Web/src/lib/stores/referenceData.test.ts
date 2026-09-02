/**
 * C-11 — creating a reference entity invalidates the reference query so
 * `DeviceForm` (or any other consumer) sees the new option immediately.
 *
 * `DeviceForm` / `DeviceFilters` / report cards don't call the reference
 * endpoints themselves — they all subscribe to the single shared
 * `referenceDataStore`. The cross-cutting bug this guards against is the
 * store silently keeping stale data after an admin page creates/merges/
 * deactivates a reference entity. Production call sites (e.g.
 * `admin/locations/+page.svelte`) invalidate by calling
 * `fetchReferenceData({ force: true })` after a mutation — this test
 * exercises that exact contract against the store module directly, which
 * is the cheapest deterministic place to prove it (no need to compose a
 * full admin page + DeviceForm just to observe the same store).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { get } from 'svelte/store';

const listMocks = vi.hoisted(() => ({
	brandsList: vi.fn(),
	categoriesList: vi.fn(),
	ownersList: vi.fn(),
	locationsList: vi.fn(),
	networksList: vi.fn(),
	tagsList: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	brands: { list: listMocks.brandsList },
	categories: { list: listMocks.categoriesList },
	owners: { list: listMocks.ownersList },
	locations: { list: listMocks.locationsList },
	networks: { list: listMocks.networksList },
	tags: { list: listMocks.tagsList }
}));

import { referenceDataStore, fetchReferenceData, clearReferenceData } from './referenceData';

function emptyList() {
	return { items: [], totalCount: 0, page: 1, pageSize: 200 };
}

const homeOffice = { id: 'loc-1', name: 'Home Office' };
const garage = { id: 'loc-2', name: 'Garage' };

describe('referenceData store (C-11 cache invalidation)', () => {
	beforeEach(() => {
		clearReferenceData();
		listMocks.brandsList.mockReset().mockResolvedValue(emptyList());
		listMocks.categoriesList.mockReset().mockResolvedValue(emptyList());
		listMocks.ownersList.mockReset().mockResolvedValue(emptyList());
		listMocks.locationsList.mockReset().mockResolvedValue({ ...emptyList(), items: [homeOffice] });
		listMocks.networksList.mockReset().mockResolvedValue(emptyList());
		listMocks.tagsList.mockReset().mockResolvedValue(emptyList());
	});

	it('caches after the first load — a plain re-fetch does not re-hit the API', async () => {
		await fetchReferenceData();
		await fetchReferenceData();

		expect(listMocks.locationsList).toHaveBeenCalledTimes(1);
		expect(get(referenceDataStore).locations).toEqual([homeOffice]);
	});

	it('force:true bypasses the cache so a newly-created reference entity appears immediately', async () => {
		await fetchReferenceData();
		expect(get(referenceDataStore).locations).toEqual([homeOffice]);

		// Model an admin creating "Garage": the server now returns both.
		listMocks.locationsList.mockResolvedValue({ ...emptyList(), items: [homeOffice, garage] });

		await fetchReferenceData({ force: true });

		expect(listMocks.locationsList).toHaveBeenCalledTimes(2);
		expect(get(referenceDataStore).locations).toEqual([homeOffice, garage]);
	});
});

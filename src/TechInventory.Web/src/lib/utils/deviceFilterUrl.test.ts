/**
 * buildDevicesUrlParams — F045 B2 regression guard.
 *
 * The QC-rejected version wrote `groupBy=none` to the URL whenever
 * `newFilters.groupBy` was falsy AND the caller was in installed-PWA mode —
 * which fires on *every* search/sort/page/unrelated-filter-edit call, not
 * just an explicit "None" selection, permanently killing the implicit
 * category-grouping default on the very first keystroke. These tests prove
 * the sentinel now writes only from an explicit `groupBy: 'none'` input,
 * and that ordinary filter/search/sort/page changes leave the URL clean
 * while implicit grouping stays active (i.e., nothing forces `groupBy=none`).
 */
import { describe, it, expect } from 'vitest';
import { buildDevicesUrlParams, STATUS_ALL_SENTINEL, GROUP_BY_NONE_SENTINEL } from './deviceFilterUrl';
import type { DeviceFilters } from '$lib/queries/devices.svelte';

const baseFilters: DeviceFilters = {
	page: 1,
	pageSize: 25,
	status: ['Active'],
	sortDir: 'asc'
};

describe('buildDevicesUrlParams (F045 B2)', () => {
	it('omits groupBy entirely when untouched (undefined), in both PWA and non-PWA mode', () => {
		const pwaParams = buildDevicesUrlParams({ ...baseFilters }, { isPwa: true });
		const desktopParams = buildDevicesUrlParams({ ...baseFilters }, { isPwa: false });

		expect(pwaParams.has('groupBy')).toBe(false);
		expect(desktopParams.has('groupBy')).toBe(false);
	});

	it('does not write groupBy=none when a search edit is applied while groupBy stays untouched (PWA mode)', () => {
		const params = buildDevicesUrlParams({ ...baseFilters, search: 'router' }, { isPwa: true });

		expect(params.get('search')).toBe('router');
		expect(params.has('groupBy')).toBe(false);
	});

	it('does not write groupBy=none when a sort change is applied while groupBy stays untouched (PWA mode)', () => {
		const params = buildDevicesUrlParams(
			{ ...baseFilters, sort: 'name', sortDir: 'desc' },
			{ isPwa: true }
		);

		expect(params.get('sort')).toBe('name');
		expect(params.get('sortDir')).toBe('desc');
		expect(params.has('groupBy')).toBe(false);
	});

	it('does not write groupBy=none when a page change is applied while groupBy stays untouched (PWA mode)', () => {
		const params = buildDevicesUrlParams({ ...baseFilters, page: 3 }, { isPwa: true });

		expect(params.get('page')).toBe('3');
		expect(params.has('groupBy')).toBe(false);
	});

	it('does not write groupBy=none when an unrelated filter (brandId) is applied while groupBy stays untouched (PWA mode)', () => {
		const params = buildDevicesUrlParams(
			{ ...baseFilters, brandId: 'brand-123' },
			{ isPwa: true }
		);

		expect(params.get('brandId')).toBe('brand-123');
		expect(params.has('groupBy')).toBe(false);
	});

	it('writes the explicit groupBy dimension as-is, in both PWA and non-PWA mode', () => {
		const pwaParams = buildDevicesUrlParams({ ...baseFilters, groupBy: 'owner' }, { isPwa: true });
		const desktopParams = buildDevicesUrlParams(
			{ ...baseFilters, groupBy: 'owner' },
			{ isPwa: false }
		);

		expect(pwaParams.get('groupBy')).toBe('owner');
		expect(desktopParams.get('groupBy')).toBe('owner');
	});

	it('writes the groupBy=none sentinel only for an explicit None selection, and only in PWA mode', () => {
		const pwaParams = buildDevicesUrlParams(
			{ ...baseFilters, groupBy: 'none' },
			{ isPwa: true }
		);
		const desktopParams = buildDevicesUrlParams(
			{ ...baseFilters, groupBy: 'none' },
			{ isPwa: false }
		);

		expect(pwaParams.get('groupBy')).toBe(GROUP_BY_NONE_SENTINEL);
		// Desktop/mobile-web has no implicit default to defeat — the sentinel
		// would be meaningless noise in the URL, so it must be omitted.
		expect(desktopParams.has('groupBy')).toBe(false);
	});

	it('preserves the existing status/page/search round-trip rules unchanged', () => {
		const clearedStatus = buildDevicesUrlParams({ ...baseFilters, status: undefined }, { isPwa: false });
		expect(clearedStatus.get('status')).toBe(STATUS_ALL_SENTINEL);

		const implicitActive = buildDevicesUrlParams({ ...baseFilters, status: ['Active'] }, { isPwa: false });
		expect(implicitActive.has('status')).toBe(false);

		const explicitOther = buildDevicesUrlParams({ ...baseFilters, status: ['Retired'] }, { isPwa: false });
		expect(explicitOther.get('status')).toBe('Retired');

		const firstPage = buildDevicesUrlParams({ ...baseFilters, page: 1 }, { isPwa: false });
		expect(firstPage.has('page')).toBe(false);
	});

	describe('Clear All output (C-06)', () => {
		// DeviceFilters.svelte's clearAll() emits exactly
		// { page: 1, pageSize, groupBy } — every other dimension is dropped
		// from the filters object entirely (not just falsy). This proves what
		// that object serializes to: no search/brandId/categoryId/ownerId/
		// locationId/networkId/year params, and — per the F026 sentinel rule
		// above — an *implicit* `status=all` because status is now undefined,
		// not because the URL omits status altogether.
		it('serializes a groupBy-preserving Clear All result with no other filter keys', () => {
			const clearAllResult: DeviceFilters = { page: 1, pageSize: 50, groupBy: 'owner' };

			const params = buildDevicesUrlParams(clearAllResult, { isPwa: false });

			expect(params.get('status')).toBe(STATUS_ALL_SENTINEL);
			expect(params.get('groupBy')).toBe('owner');
			expect(params.get('pageSize')).toBe('50');
			for (const key of [
				'search',
				'brandId',
				'categoryId',
				'ownerId',
				'locationId',
				'networkId',
				'yearMin',
				'yearMax',
				'sort',
				'sortDir',
				'page'
			]) {
				expect(params.has(key)).toBe(false);
			}
		});

		it('serializes a Clear All result with no prior groupBy as a bare reset (no groupBy key at all)', () => {
			const clearAllResult: DeviceFilters = { page: 1, pageSize: 25 };

			const params = buildDevicesUrlParams(clearAllResult, { isPwa: true });

			expect(params.get('status')).toBe(STATUS_ALL_SENTINEL);
			expect(params.has('groupBy')).toBe(false);
			expect(params.has('pageSize')).toBe(false);
		});
	});
});

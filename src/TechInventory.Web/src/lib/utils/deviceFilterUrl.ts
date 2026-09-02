import type { DeviceFilters } from '$lib/queries/devices.svelte';

/**
 * F026: `status=all` sentinel. Distinguishes "user explicitly asked for
 * every status" from "user has not chosen anything yet" (bare URL) — without
 * it, a bare URL is indistinguishable from an explicit opt-out, so there
 * would be no way to persist "show everything" across a reload.
 */
export const STATUS_ALL_SENTINEL = 'all';

/**
 * F045 R2/R5 (B2 fix): `groupBy=none` sentinel. Distinguishes "user
 * explicitly picked None to defeat the installed-PWA's implicit category
 * default" from "groupBy is simply untouched" — both would otherwise read as
 * `groupBy: undefined`. Conflating these two was the QC-rejected bug: it
 * caused every search/sort/page/unrelated-filter change to also (re-)write
 * this sentinel, permanently killing the implicit default on first keystroke.
 */
export const GROUP_BY_NONE_SENTINEL = 'none';

export interface BuildDevicesUrlParamsOptions {
	/** Only the standalone-PWA caller may persist the None sentinel — desktop
	 *  and mobile-web have no implicit grouping default for it to defeat. */
	isPwa: boolean;
}

/**
 * Pure URL-param builder for the devices list page (F045 B2).
 *
 * Extracted from `devices/+page.svelte`'s `updateFilters` so the contract
 * "search/sort/page/unrelated-filter changes must never write
 * `groupBy=none`; only an explicit user None selection may" has a fast,
 * framework-free unit test instead of requiring a full page-component mount.
 */
export function buildDevicesUrlParams(
	filters: DeviceFilters,
	{ isPwa }: BuildDevicesUrlParamsOptions
): URLSearchParams {
	const params = new URLSearchParams();

	if (filters.page && filters.page !== 1) params.set('page', filters.page.toString());
	if (filters.pageSize && filters.pageSize !== 25) {
		params.set('pageSize', filters.pageSize.toString());
	}
	if (filters.search) params.set('search', filters.search);
	if (filters.brandId) params.set('brandId', filters.brandId);
	if (filters.categoryId) params.set('categoryId', filters.categoryId);
	if (filters.ownerId) params.set('ownerId', filters.ownerId);
	if (filters.locationId) params.set('locationId', filters.locationId);
	if (filters.networkId) params.set('networkId', filters.networkId);

	// F026: status round-trip rules.
	//   undefined or []           → user cleared status → STATUS_ALL_SENTINEL
	//   ['Active']                → matches implicit default → omit
	//   any other single value    → write as-is
	if (!filters.status || filters.status.length === 0) {
		params.set('status', STATUS_ALL_SENTINEL);
	} else if (filters.status.length === 1 && filters.status[0] === 'Active') {
		// omit — implicit default
	} else {
		params.set('status', filters.status[0]);
	}

	if (filters.purchaseYearMin) params.set('yearMin', filters.purchaseYearMin.toString());
	if (filters.purchaseYearMax) params.set('yearMax', filters.purchaseYearMax.toString());
	if (filters.sort) params.set('sort', filters.sort);
	if (filters.sortDir && filters.sortDir !== 'asc') params.set('sortDir', filters.sortDir);

	// F045 B2: the sentinel is written only from an *explicit* user "None"
	// choice — filters.groupBy === GROUP_BY_NONE_SENTINEL, set solely by
	// DeviceFilters.svelte's <select> — never inferred from "groupBy happens
	// to be falsy", which is indistinguishable from "search box edited",
	// "sort clicked", "page turned", etc.
	if (filters.groupBy === GROUP_BY_NONE_SENTINEL) {
		if (isPwa) {
			params.set('groupBy', GROUP_BY_NONE_SENTINEL);
		}
		// else: desktop/mobile-web has no implicit default to defeat — omit.
	} else if (filters.groupBy) {
		params.set('groupBy', filters.groupBy);
	}
	// else: groupBy is untouched — omit entirely. This is the case the
	// QC-rejected version got wrong (it wrote the sentinel here instead).

	return params;
}

/**
 * groupDevices — F023 client-side grouping helper.
 *
 * Partition a flat device list by one of three dimensions (Category, Owner,
 * Purchase year) into ordered, labeled buckets for grouped rendering in
 * DeviceTable. Missing values fall into a single "Unknown" bucket that is
 * always rendered last.
 *
 * Sort order:
 *   - 'category' / 'owner': alphabetical by label (en, case-insensitive)
 *   - 'year':               numeric descending (most recent first)
 *   - Unknown bucket:       always last regardless of dimension
 *
 * Within each group the input device order is preserved, so the parent
 * page's existing sort (name / purchaseDate / createdAt) still applies.
 */

import type { DeviceResponse } from '$lib/queries/devices.svelte';

export type GroupByDimension = 'category' | 'owner' | 'year';

export interface DeviceGroup {
	/** Stable group identity: category id, owner id, year string, or '__unknown__'. */
	key: string;
	/** User-facing group label. */
	label: string;
	count: number;
	devices: DeviceResponse[];
	isUnknown: boolean;
}

interface NamedRef {
	id: string;
	name: string;
}

interface RefLookups {
	categories: NamedRef[];
	owners: NamedRef[];
}

const UNKNOWN_KEY = '__unknown__';

export function groupDevices(
	devices: DeviceResponse[],
	groupBy: GroupByDimension,
	refData: RefLookups,
	unknownLabel = 'Unknown'
): DeviceGroup[] {
	const buckets = new Map<
		string,
		{ label: string; devices: DeviceResponse[]; isUnknown: boolean }
	>();

	for (const device of devices) {
		const { key, label, isUnknown } = bucketFor(device, groupBy, refData, unknownLabel);
		const existing = buckets.get(key);
		if (existing) {
			existing.devices.push(device);
		} else {
			buckets.set(key, { label, devices: [device], isUnknown });
		}
	}

	const groups: DeviceGroup[] = Array.from(buckets.entries()).map(([key, b]) => ({
		key,
		label: b.label,
		count: b.devices.length,
		devices: b.devices,
		isUnknown: b.isUnknown
	}));

	groups.sort((a, b) => {
		if (a.isUnknown !== b.isUnknown) return a.isUnknown ? 1 : -1;
		if (groupBy === 'year') {
			return Number(b.key) - Number(a.key);
		}
		return a.label.localeCompare(b.label, 'en', { sensitivity: 'base' });
	});

	return groups;
}

function bucketFor(
	device: DeviceResponse,
	groupBy: GroupByDimension,
	refData: RefLookups,
	unknownLabel: string
): { key: string; label: string; isUnknown: boolean } {
	if (groupBy === 'category') {
		if (device.categoryId) {
			const name =
				refData.categories.find((c) => c.id === device.categoryId)?.name ?? unknownLabel;
			return { key: device.categoryId, label: name, isUnknown: false };
		}
		return { key: UNKNOWN_KEY, label: unknownLabel, isUnknown: true };
	}

	if (groupBy === 'owner') {
		if (device.ownerId) {
			const name = refData.owners.find((o) => o.id === device.ownerId)?.name ?? unknownLabel;
			return { key: device.ownerId, label: name, isUnknown: false };
		}
		return { key: UNKNOWN_KEY, label: unknownLabel, isUnknown: true };
	}

	// year
	if (device.purchaseDate) {
		// purchaseDate is an ISO date string ("YYYY-MM-DD" or full ISO). Parse the
		// calendar year from the leading 4 characters to avoid timezone drift
		// (new Date('2024-01-01').getFullYear() returns 2023 in negative-UTC zones).
		const yearMatch = /^(\d{4})/.exec(device.purchaseDate);
		if (yearMatch) {
			const yearStr = yearMatch[1];
			return { key: yearStr, label: yearStr, isUnknown: false };
		}
	}
	return { key: UNKNOWN_KEY, label: unknownLabel, isUnknown: true };
}

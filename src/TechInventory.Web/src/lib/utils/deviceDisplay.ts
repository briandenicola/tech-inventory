/**
 * deviceDisplay.ts — shared device presentation helpers.
 *
 * Hoisted out of `DeviceTable.svelte` (F045 D-178) so the desktop table, the
 * mobile card grid, and the new PWA row renderer all resolve reference-data
 * names and status colors the same way instead of forking the lookup logic.
 */

export interface NamedReference {
	id: string;
	name: string;
}

/** Resolves a reference-data id (brand/category/owner/…) to its display name, or '—'. */
export function lookupName(
	collection: NamedReference[],
	id: string | null | undefined
): string {
	if (!id) return '—';
	return collection.find((entity) => entity.id === id)?.name ?? '—';
}

/**
 * F026: status pill colors, shared across every device list/card/row
 * renderer so the visual language stays consistent.
 */
export function statusBadgeClass(status: string | null | undefined): string {
	switch (status) {
		case 'Active':
			return 'bg-success-100 text-success-800 dark:bg-success-900 dark:text-success-100';
		case 'Retired':
			return 'bg-neutral-200 text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300';
		case 'Disposed':
			return 'bg-danger-100 text-danger-800 dark:bg-danger-900 dark:text-danger-100';
		case 'InRepair':
			return 'bg-warning-100 text-warning-800 dark:bg-warning-900 dark:text-warning-100';
		case 'Lent':
			return 'bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-200';
		default:
			return 'bg-neutral-100 text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300';
	}
}

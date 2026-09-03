/**
 * deviceDisplay.ts — shared device presentation helpers.
 *
 * Hoisted out of `DeviceTable.svelte` (F045 D-178) so the desktop table, the
 * mobile card grid, and the new PWA row renderer all resolve reference-data
 * names the same way instead of forking the lookup logic.
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

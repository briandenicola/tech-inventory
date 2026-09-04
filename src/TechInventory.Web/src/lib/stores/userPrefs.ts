/**
 * User Preferences (client-side, localStorage)
 *
 * F022: Persist per-user UI preferences (currently the default `/devices`
 * view) so that revisits and bare-URL navigations honor the user's preferred
 * filter + sort combination.
 *
 * Storage shape lives in localStorage under a per-user key. Cross-device sync
 * is out of scope; F020 will introduce a server-side `UserPreference` table
 * and this module will gain a server adapter.
 *
 * SSR-safe: every reader returns `null` when `window` is undefined so the
 * SvelteKit prerender + Vitest jsdom-less paths don't blow up.
 */

import { z } from 'zod';

const STORAGE_VERSION = 1;
const KEY_PREFIX = 'ti.userPrefs.v1';

/**
 * All possible table columns that the user can show/hide/reorder.
 */
export const ALL_TABLE_COLUMNS = [
	'name',
	'model',
	'brand',
	'category',
	'owner',
	'status',
	'purchaseDate'
] as const;

export type TableColumnId = (typeof ALL_TABLE_COLUMNS)[number];

/** Default column order and visibility — all columns, original order. */
export const DEFAULT_TABLE_COLUMNS: TableColumnId[] = [...ALL_TABLE_COLUMNS];

const TableColumnSchema = z.enum(ALL_TABLE_COLUMNS);

/**
 * Per-column widths in CSS pixels, for the resizable desktop table.
 *
 * Defaults are hand-set rather than uniform: Name carries the longest values and
 * is the sticky identity column, while Status holds a short badge. Splitting the
 * width evenly is what squeezed "Purchase Date" into "Pur D" in the first place.
 */
export const DEFAULT_TABLE_COLUMN_WIDTHS: Record<TableColumnId, number> = {
	name: 260,
	model: 200,
	brand: 160,
	category: 160,
	owner: 160,
	status: 120,
	purchaseDate: 150
};

/** Narrow enough to stay useful, wide enough that a header is never clipped to nonsense. */
export const MIN_TABLE_COLUMN_WIDTH = 80;

/** Beyond this a single column pushes everything else off-screen with no benefit. */
export const MAX_TABLE_COLUMN_WIDTH = 640;

/**
 * Stored widths are intentionally sparse — only columns the user actually resized
 * are written, so a default that changes later still reaches everyone who never
 * touched that column. Keys are validated against ALL_TABLE_COLUMNS on read rather
 * than in the schema, so a stale key from a removed column is ignored instead of
 * failing the whole preferences blob.
 */
const TableColumnWidthsSchema = z.record(z.string(), z.number());

const UserPrefsSchema = z.object({
	version: z.literal(STORAGE_VERSION),
	devicesDefaultView: z.string().nullable().optional(),
	devicesViewMode: z.enum(['cards', 'table']).nullable().optional(),
	themePreference: z.enum(['light', 'dark', 'system']).nullable().optional(),
	tableColumns: z.array(TableColumnSchema).nullable().optional(),
	tableColumnWidths: TableColumnWidthsSchema.nullable().optional()
});

export type UserPrefs = z.infer<typeof UserPrefsSchema>;
export type DevicesViewMode = 'cards' | 'table';
export type ThemePreference = 'light' | 'dark' | 'system';

const EMPTY_PREFS: UserPrefs = { version: STORAGE_VERSION };

function isBrowser(): boolean {
	return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
}

function storageKey(userId: string): string {
	return `${KEY_PREFIX}.${userId}`;
}

function readPrefs(userId: string): UserPrefs {
	if (!isBrowser() || !userId) return { ...EMPTY_PREFS };
	const raw = window.localStorage.getItem(storageKey(userId));
	if (!raw) return { ...EMPTY_PREFS };
	try {
		const parsed = UserPrefsSchema.safeParse(JSON.parse(raw));
		if (!parsed.success) {
			// Forward-compatibility: drop unrecognized payloads silently.
			window.localStorage.removeItem(storageKey(userId));
			return { ...EMPTY_PREFS };
		}
		return parsed.data;
	} catch {
		window.localStorage.removeItem(storageKey(userId));
		return { ...EMPTY_PREFS };
	}
}

function writePrefs(userId: string, prefs: UserPrefs): void {
	if (!isBrowser() || !userId) return;
	window.localStorage.setItem(storageKey(userId), JSON.stringify(prefs));
}

/**
 * Normalize a querystring so equality checks are stable regardless of the
 * order the URLSearchParams iterated keys in. Empty strings → null.
 */
export function normalizeQueryString(query: string | null | undefined): string | null {
	if (!query) return null;
	const trimmed = query.startsWith('?') ? query.slice(1) : query;
	if (trimmed.length === 0) return null;
	const params = new URLSearchParams(trimmed);
	const entries = Array.from(params.entries()).sort(([a], [b]) => a.localeCompare(b));
	if (entries.length === 0) return null;
	const next = new URLSearchParams();
	for (const [k, v] of entries) next.append(k, v);
	return next.toString();
}

export function getDevicesDefaultView(userId: string | null | undefined): string | null {
	if (!userId) return null;
	const prefs = readPrefs(userId);
	return prefs.devicesDefaultView ?? null;
}

export function setDevicesDefaultView(
	userId: string | null | undefined,
	query: string | null | undefined
): void {
	if (!userId) return;
	const normalized = normalizeQueryString(query);
	const prefs = readPrefs(userId);
	prefs.devicesDefaultView = normalized;
	writePrefs(userId, prefs);
}

export function clearDevicesDefaultView(userId: string | null | undefined): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	if (prefs.devicesDefaultView == null) return;
	delete prefs.devicesDefaultView;
	writePrefs(userId, prefs);
}

export function getDevicesViewMode(userId: string | null | undefined): DevicesViewMode | null {
	if (!userId) return null;
	const prefs = readPrefs(userId);
	return prefs.devicesViewMode ?? null;
}

export function setDevicesViewMode(
	userId: string | null | undefined,
	mode: DevicesViewMode
): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	prefs.devicesViewMode = mode;
	writePrefs(userId, prefs);
}

export function getThemePreference(userId: string | null | undefined): ThemePreference | null {
	if (!userId) return null;
	const prefs = readPrefs(userId);
	return prefs.themePreference ?? null;
}

export function setThemePreference(
	userId: string | null | undefined,
	theme: ThemePreference
): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	prefs.themePreference = theme;
	writePrefs(userId, prefs);
}

/**
 * Get the user's configured table columns (order determines display order;
 * only included columns are visible). Returns null if unset (use defaults).
 */
export function getTableColumns(userId: string | null | undefined): TableColumnId[] | null {
	if (!userId) return null;
	const prefs = readPrefs(userId);
	return prefs.tableColumns ?? null;
}

/**
 * Persist the user's chosen table columns (order + visibility).
 * Pass null to reset to defaults.
 */
export function setTableColumns(
	userId: string | null | undefined,
	columns: TableColumnId[] | null
): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	prefs.tableColumns = columns;
	writePrefs(userId, prefs);
}

/**
 * Clamp a width into the supported range.
 *
 * Applied on read as well as on write: a stored value can predate a change to these
 * bounds, or have been hand-edited in localStorage, and a 4-pixel or 20000-pixel
 * column would leave the table unusable with no obvious way back.
 */
export function clampTableColumnWidth(width: number): number {
	if (!Number.isFinite(width)) return MIN_TABLE_COLUMN_WIDTH;
	return Math.min(MAX_TABLE_COLUMN_WIDTH, Math.max(MIN_TABLE_COLUMN_WIDTH, Math.round(width)));
}

/**
 * Effective widths for every column: the user's stored values where present,
 * defaults elsewhere. Always returns a width for every column, so callers never
 * have to handle a partially-configured table.
 */
export function getTableColumnWidths(
	userId: string | null | undefined
): Record<TableColumnId, number> {
	const stored: Record<string, number> | null | undefined = userId
		? readPrefs(userId).tableColumnWidths
		: null;
	const widths = { ...DEFAULT_TABLE_COLUMN_WIDTHS };

	if (!stored) return widths;

	for (const column of ALL_TABLE_COLUMNS) {
		const value = stored[column];
		if (typeof value === 'number') {
			widths[column] = clampTableColumnWidth(value);
		}
	}

	return widths;
}

/**
 * Persist one column's width. Pass null to drop the override and fall back to the
 * default for that column.
 */
export function setTableColumnWidth(
	userId: string | null | undefined,
	column: TableColumnId,
	width: number | null
): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	const widths = { ...(prefs.tableColumnWidths ?? {}) };

	if (width === null) {
		delete widths[column];
	} else {
		widths[column] = clampTableColumnWidth(width);
	}

	prefs.tableColumnWidths = Object.keys(widths).length > 0 ? widths : null;
	writePrefs(userId, prefs);
}

/** Drop every width override, returning the table to the defaults above. */
export function resetTableColumnWidths(userId: string | null | undefined): void {
	if (!userId) return;
	const prefs = readPrefs(userId);
	prefs.tableColumnWidths = null;
	writePrefs(userId, prefs);
}

/**
 * Clear every persisted preference for a user (sign-out hook).
 */
export function clearAllUserPrefs(userId: string | null | undefined): void {
	if (!isBrowser() || !userId) return;
	window.localStorage.removeItem(storageKey(userId));
}

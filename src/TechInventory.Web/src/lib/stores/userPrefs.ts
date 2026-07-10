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

const TableColumnSchema = z.enum(['name', 'model', 'brand', 'category', 'owner', 'status', 'purchaseDate']);

const UserPrefsSchema = z.object({
	version: z.literal(STORAGE_VERSION),
	devicesDefaultView: z.string().nullable().optional(),
	devicesViewMode: z.enum(['cards', 'table']).nullable().optional(),
	themePreference: z.enum(['light', 'dark', 'system']).nullable().optional(),
	tableColumns: z.array(TableColumnSchema).nullable().optional()
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
 * Clear every persisted preference for a user (sign-out hook).
 */
export function clearAllUserPrefs(userId: string | null | undefined): void {
	if (!isBrowser() || !userId) return;
	window.localStorage.removeItem(storageKey(userId));
}

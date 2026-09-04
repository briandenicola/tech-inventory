/**
 * Table column configuration tests
 *
 * Covers getTableColumns / setTableColumns round-trip and defaults.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import {
	getTableColumns,
	setTableColumns,
	DEFAULT_TABLE_COLUMNS,
	DEFAULT_TABLE_COLUMN_WIDTHS,
	MIN_TABLE_COLUMN_WIDTH,
	MAX_TABLE_COLUMN_WIDTH,
	getTableColumnWidths,
	setTableColumnWidth,
	resetTableColumnWidths,
	ALL_TABLE_COLUMNS,
	clearAllUserPrefs
} from './userPrefs';

const USER = 'test-user-columns';

describe('tableColumns preferences', () => {
	beforeEach(() => {
		window.localStorage.clear();
	});

	it('returns null when nothing is stored', () => {
		expect(getTableColumns(USER)).toBeNull();
	});

	it('returns null for null/undefined userId', () => {
		expect(getTableColumns(null)).toBeNull();
		expect(getTableColumns(undefined)).toBeNull();
	});

	it('round-trips a custom column order', () => {
		const custom = ['brand', 'name', 'status'] as const;
		setTableColumns(USER, [...custom]);
		expect(getTableColumns(USER)).toEqual([...custom]);
	});

	it('persists a subset of columns (visibility)', () => {
		setTableColumns(USER, ['name', 'category']);
		expect(getTableColumns(USER)).toEqual(['name', 'category']);
	});

	it('resets to null when passed null', () => {
		setTableColumns(USER, ['name', 'brand']);
		setTableColumns(USER, null);
		expect(getTableColumns(USER)).toBeNull();
	});

	it('does not write when userId is null', () => {
		setTableColumns(null, ['name']);
		// No crash, nothing persisted
		expect(getTableColumns(null)).toBeNull();
	});

	it('clearAllUserPrefs removes column preferences too', () => {
		setTableColumns(USER, ['status', 'owner']);
		clearAllUserPrefs(USER);
		expect(getTableColumns(USER)).toBeNull();
	});

	it('DEFAULT_TABLE_COLUMNS contains all columns', () => {
		expect(DEFAULT_TABLE_COLUMNS).toEqual([...ALL_TABLE_COLUMNS]);
	});
});

describe('table column widths', () => {
	// Widths back the resizable desktop table. The invariants that matter: a stored
	// value can never render the table unusable, and a column the user never touched
	// keeps following the default.
	const USER = 'width-user';

	beforeEach(() => {
		window.localStorage.clear();
	});

	it('returns defaults for a user who has never resized anything', () => {
		expect(getTableColumnWidths(USER)).toEqual(DEFAULT_TABLE_COLUMN_WIDTHS);
	});

	it('returns defaults when there is no user (signed out / SSR)', () => {
		expect(getTableColumnWidths(null)).toEqual(DEFAULT_TABLE_COLUMN_WIDTHS);
	});

	it('persists one column and leaves the rest on their defaults', () => {
		setTableColumnWidth(USER, 'name', 320);

		const widths = getTableColumnWidths(USER);
		expect(widths.name).toBe(320);
		expect(widths.model).toBe(DEFAULT_TABLE_COLUMN_WIDTHS.model);
	});

	it('clamps a width below the minimum on write', () => {
		setTableColumnWidth(USER, 'status', 10);

		expect(getTableColumnWidths(USER).status).toBe(MIN_TABLE_COLUMN_WIDTH);
	});

	it('clamps a width above the maximum on write', () => {
		setTableColumnWidth(USER, 'status', 99999);

		expect(getTableColumnWidths(USER).status).toBe(MAX_TABLE_COLUMN_WIDTH);
	});

	it('clamps a hand-edited out-of-range value on read', () => {
		// localStorage is user-writable and bounds can change between releases, so a
		// stored 4px column must not be able to render the table unusable.
		window.localStorage.setItem(
			`ti.userPrefs.v1.${USER}`,
			JSON.stringify({ version: 1, tableColumnWidths: { name: 4, model: 100000 } })
		);

		const widths = getTableColumnWidths(USER);
		expect(widths.name).toBe(MIN_TABLE_COLUMN_WIDTH);
		expect(widths.model).toBe(MAX_TABLE_COLUMN_WIDTH);
	});

	it('ignores a stored key that is not a known column', () => {
		// A column removed in a later release leaves a stale key behind; that must not
		// discard the whole preferences blob.
		window.localStorage.setItem(
			`ti.userPrefs.v1.${USER}`,
			JSON.stringify({ version: 1, tableColumnWidths: { name: 300, removedColumn: 200 } })
		);

		const widths = getTableColumnWidths(USER);
		expect(widths.name).toBe(300);
		expect(widths).not.toHaveProperty('removedColumn');
	});

	it('rounds fractional widths, since sub-pixel columns help nobody', () => {
		setTableColumnWidth(USER, 'brand', 173.6);

		expect(getTableColumnWidths(USER).brand).toBe(174);
	});

	it('drops a single override when passed null, restoring that default', () => {
		setTableColumnWidth(USER, 'owner', 300);
		setTableColumnWidth(USER, 'owner', null);

		expect(getTableColumnWidths(USER).owner).toBe(DEFAULT_TABLE_COLUMN_WIDTHS.owner);
	});

	it('resets every override at once', () => {
		setTableColumnWidth(USER, 'name', 300);
		setTableColumnWidth(USER, 'model', 300);

		resetTableColumnWidths(USER);

		expect(getTableColumnWidths(USER)).toEqual(DEFAULT_TABLE_COLUMN_WIDTHS);
	});

	it('keeps widths separate per user', () => {
		setTableColumnWidth(USER, 'name', 300);

		expect(getTableColumnWidths('someone-else').name).toBe(DEFAULT_TABLE_COLUMN_WIDTHS.name);
	});

	it('does not disturb column visibility/order when widths change', () => {
		setTableColumns(USER, ['name', 'status']);
		setTableColumnWidth(USER, 'name', 300);

		expect(getTableColumns(USER)).toEqual(['name', 'status']);
		expect(getTableColumnWidths(USER).name).toBe(300);
	});
});

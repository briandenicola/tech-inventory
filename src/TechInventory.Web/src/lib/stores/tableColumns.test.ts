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

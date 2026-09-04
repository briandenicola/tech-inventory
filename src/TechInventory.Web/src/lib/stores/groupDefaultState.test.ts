/**
 * Group default-state preference tests
 *
 * Covers getGroupDefaultState / setGroupDefaultState round-trip, the fallback
 * used before a choice is made, and isolation from the other preferences that
 * share the same localStorage entry.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import {
	GROUP_DEFAULT_STATES,
	DEFAULT_GROUP_STATE,
	getGroupDefaultState,
	setGroupDefaultState,
	getTableColumns,
	setTableColumns,
	clearAllUserPrefs
} from './userPrefs';

const USER = 'test-user-groups';
const OTHER = 'test-user-groups-other';

describe('group default-state preference', () => {
	beforeEach(() => {
		window.localStorage.clear();
	});

	it('offers exactly the two states the UI renders', () => {
		expect([...GROUP_DEFAULT_STATES]).toEqual(['expanded', 'collapsed']);
	});

	it('defaults to expanded so a new user sees devices, not a wall of headers', () => {
		expect(DEFAULT_GROUP_STATE).toBe('expanded');
		expect(getGroupDefaultState(USER)).toBe('expanded');
	});

	it('falls back to the default for null/undefined userId', () => {
		expect(getGroupDefaultState(null)).toBe(DEFAULT_GROUP_STATE);
		expect(getGroupDefaultState(undefined)).toBe(DEFAULT_GROUP_STATE);
	});

	it('round-trips a stored choice', () => {
		setGroupDefaultState(USER, 'collapsed');
		expect(getGroupDefaultState(USER)).toBe('collapsed');

		setGroupDefaultState(USER, 'expanded');
		expect(getGroupDefaultState(USER)).toBe('expanded');
	});

	it('returns to the default when cleared with null', () => {
		setGroupDefaultState(USER, 'collapsed');
		setGroupDefaultState(USER, null);
		expect(getGroupDefaultState(USER)).toBe(DEFAULT_GROUP_STATE);
	});

	it('is a no-op for a null userId', () => {
		expect(() => setGroupDefaultState(null, 'collapsed')).not.toThrow();
		expect(getGroupDefaultState(null)).toBe(DEFAULT_GROUP_STATE);
	});

	it('keeps the choice separate per user', () => {
		setGroupDefaultState(USER, 'collapsed');
		expect(getGroupDefaultState(OTHER)).toBe(DEFAULT_GROUP_STATE);
	});

	it('does not disturb neighbouring preferences in the same entry', () => {
		setTableColumns(USER, ['name', 'status']);
		setGroupDefaultState(USER, 'collapsed');

		expect(getTableColumns(USER)).toEqual(['name', 'status']);
		expect(getGroupDefaultState(USER)).toBe('collapsed');
	});

	it('is dropped by clearAllUserPrefs', () => {
		setGroupDefaultState(USER, 'collapsed');
		clearAllUserPrefs(USER);
		expect(getGroupDefaultState(USER)).toBe(DEFAULT_GROUP_STATE);
	});
});

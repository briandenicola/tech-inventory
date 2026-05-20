/**
 * userPrefs store tests (F022)
 *
 * Covers the localStorage round-trip helpers and querystring normalization
 * used to back the "Save as default" affordance on /devices.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import {
	getDevicesDefaultView,
	setDevicesDefaultView,
	clearDevicesDefaultView,
	clearAllUserPrefs,
	normalizeQueryString
} from './userPrefs';

const USER_A = 'user-a-oid';
const USER_B = 'user-b-oid';

describe('userPrefs', () => {
	beforeEach(() => {
		window.localStorage.clear();
	});

	describe('normalizeQueryString', () => {
		it('returns null for empty / nullish / "?"', () => {
			expect(normalizeQueryString(null)).toBeNull();
			expect(normalizeQueryString(undefined)).toBeNull();
			expect(normalizeQueryString('')).toBeNull();
			expect(normalizeQueryString('?')).toBeNull();
		});

		it('strips leading "?"', () => {
			expect(normalizeQueryString('?status=Active')).toBe('status=Active');
		});

		it('sorts keys for stable equality', () => {
			expect(normalizeQueryString('sort=name&status=Active')).toBe(
				normalizeQueryString('status=Active&sort=name')
			);
		});

		it('preserves duplicate keys in the original order within their key group', () => {
			expect(normalizeQueryString('tag=a&tag=b&sort=name')).toBe('sort=name&tag=a&tag=b');
		});
	});

	describe('getDevicesDefaultView', () => {
		it('returns null when nothing is stored', () => {
			expect(getDevicesDefaultView(USER_A)).toBeNull();
		});

		it('returns null for null / empty user id', () => {
			expect(getDevicesDefaultView(null)).toBeNull();
			expect(getDevicesDefaultView('')).toBeNull();
		});

		it('returns null when localStorage payload is malformed', () => {
			window.localStorage.setItem('ti.userPrefs.v1.' + USER_A, '{not-json');
			expect(getDevicesDefaultView(USER_A)).toBeNull();
		});

		it('returns null when payload fails schema validation', () => {
			window.localStorage.setItem(
				'ti.userPrefs.v1.' + USER_A,
				JSON.stringify({ version: 99, devicesDefaultView: 'x' })
			);
			expect(getDevicesDefaultView(USER_A)).toBeNull();
		});
	});

	describe('setDevicesDefaultView', () => {
		it('persists the normalized querystring', () => {
			setDevicesDefaultView(USER_A, '?status=Active&sort=name');
			expect(getDevicesDefaultView(USER_A)).toBe('sort=name&status=Active');
		});

		it('stores null when given an empty querystring (i.e., "use system defaults")', () => {
			setDevicesDefaultView(USER_A, '?status=Active');
			expect(getDevicesDefaultView(USER_A)).not.toBeNull();

			setDevicesDefaultView(USER_A, '');
			expect(getDevicesDefaultView(USER_A)).toBeNull();
		});

		it('is keyed per-user — User A and User B see independent values', () => {
			setDevicesDefaultView(USER_A, '?sort=name');
			setDevicesDefaultView(USER_B, '?sort=purchaseDate');
			expect(getDevicesDefaultView(USER_A)).toBe('sort=name');
			expect(getDevicesDefaultView(USER_B)).toBe('sort=purchaseDate');
		});

		it('is a no-op for falsy user id', () => {
			setDevicesDefaultView(null, '?sort=name');
			setDevicesDefaultView('', '?sort=name');
			expect(window.localStorage.length).toBe(0);
		});
	});

	describe('clearDevicesDefaultView', () => {
		it('removes a previously-stored default', () => {
			setDevicesDefaultView(USER_A, '?sort=name');
			clearDevicesDefaultView(USER_A);
			expect(getDevicesDefaultView(USER_A)).toBeNull();
		});

		it('is a no-op when nothing is stored', () => {
			expect(() => clearDevicesDefaultView(USER_A)).not.toThrow();
		});

		it('does not affect other users', () => {
			setDevicesDefaultView(USER_A, '?sort=name');
			setDevicesDefaultView(USER_B, '?sort=purchaseDate');
			clearDevicesDefaultView(USER_A);
			expect(getDevicesDefaultView(USER_B)).toBe('sort=purchaseDate');
		});
	});

	describe('clearAllUserPrefs', () => {
		it('removes the entire user-prefs entry', () => {
			setDevicesDefaultView(USER_A, '?sort=name');
			clearAllUserPrefs(USER_A);
			expect(window.localStorage.getItem('ti.userPrefs.v1.' + USER_A)).toBeNull();
		});
	});
});

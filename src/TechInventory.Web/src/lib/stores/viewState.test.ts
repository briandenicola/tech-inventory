import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import {
	getDevicesViewState,
	setDevicesViewState,
	clearDevicesViewState,
	normalizeDevicesViewState
} from './viewState';

describe('viewState', () => {
	beforeEach(() => {
		// Clear sessionStorage before each test
		window.sessionStorage.clear();
	});

	afterEach(() => {
		window.sessionStorage.clear();
	});

	describe('getDevicesViewState', () => {
		it('returns null when no state is set', () => {
			expect(getDevicesViewState()).toBeNull();
		});

		it('returns the stored view state', () => {
			window.sessionStorage.setItem('ti.viewState.devices', 'categoryId=abc&search=test');
			expect(getDevicesViewState()).toBe('categoryId=abc&search=test');
		});
	});

	describe('setDevicesViewState', () => {
		it('stores the view state in sessionStorage', () => {
			setDevicesViewState('?categoryId=xyz&page=2');
			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBe('categoryId=xyz&page=2');
		});

		it('removes the state when null is passed', () => {
			setDevicesViewState('?categoryId=xyz');
			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBe('categoryId=xyz');

			setDevicesViewState(null);
			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBeNull();
		});

		it('stores list filters without the transient detail modal device param', () => {
			setDevicesViewState('?categoryId=category-1&sort=name&device=device-1');

			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBe('categoryId=category-1&sort=name');
		});
	});

	describe('clearDevicesViewState', () => {
		it('removes the view state from sessionStorage', () => {
			window.sessionStorage.setItem('ti.viewState.devices', '?search=test');
			clearDevicesViewState();
			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBeNull();
		});

		it('does nothing when no state is set', () => {
			clearDevicesViewState();
			expect(window.sessionStorage.getItem('ti.viewState.devices')).toBeNull();
		});
	});

	describe('session scoped persistence', () => {
		it('navigation state persists across page loads in same session', () => {
			// Simulate user filters devices
			setDevicesViewState('?categoryId=xyz&search=laptop');
			expect(getDevicesViewState()).toBe('categoryId=xyz&search=laptop');

			// Simulate navigation away and back (sessionStorage persists)
			const stored = getDevicesViewState();
			expect(stored).toBe('categoryId=xyz&search=laptop');

			// Verify clear works
			clearDevicesViewState();
			expect(getDevicesViewState()).toBeNull();
		});

		it('preserves complex filter state including sort and pagination', () => {
			const complexState = '?categoryId=abc-123&ownerId=def-456&search=keyboard&sort=name&sortDir=desc&page=3&pageSize=50';
			setDevicesViewState(complexState);
			expect(getDevicesViewState()).toBe(complexState.slice(1));
		});
	});

	describe('normalizeDevicesViewState', () => {
		it('represents category filter to detail edit save back-to-list without reopening the detail modal', () => {
			const filteredListDetailUrl = '?categoryId=appliance&search=flosser&device=device-1';
			const saved = normalizeDevicesViewState(filteredListDetailUrl);

			expect(saved).toBe('categoryId=appliance&search=flosser');
			expect(`/devices?${saved}`).toBe('/devices?categoryId=appliance&search=flosser');
		});

		it('returns null when only transient detail state is present', () => {
			expect(normalizeDevicesViewState('?device=device-1')).toBeNull();
		});
	});
});

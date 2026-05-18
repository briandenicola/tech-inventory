/**
 * Toast Store Tests — Toast notification behavior (T23)
 * 
 * Test showToast, dismissToast, auto-dismiss with fake timers.
 * Constitution §3.5: Tests own their data
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { get } from 'svelte/store';
import { toasts, showToast, dismissToast, clearToasts } from './toast';

describe('toast store', () => {
	beforeEach(() => {
		// Clear store before each test
		clearToasts();
		// Use fake timers for auto-dismiss tests
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	describe('showToast', () => {
		it('adds a new toast to the store', () => {
			showToast({ type: 'success', message: 'Device created!' });

			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(1);
			expect(activeToasts[0].type).toBe('success');
			expect(activeToasts[0].message).toBe('Device created!');
		});

		it('generates unique ID for each toast', () => {
			const id1 = showToast({ type: 'success', message: 'First' });
			const id2 = showToast({ type: 'info', message: 'Second' });

			expect(id1).not.toBe(id2);
			
			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(2);
			expect(activeToasts[0].id).toBe(id1);
			expect(activeToasts[1].id).toBe(id2);
		});

		it('uses default timeout for success (4000ms)', () => {
			showToast({ type: 'success', message: 'Success!' });

			const activeToasts = get(toasts);
			expect(activeToasts[0].timeout).toBe(4000);
		});

		it('uses default timeout for error (8000ms)', () => {
			showToast({ type: 'error', message: 'Error!' });

			const activeToasts = get(toasts);
			expect(activeToasts[0].timeout).toBe(8000);
		});

		it('uses default timeout for info (4000ms)', () => {
			showToast({ type: 'info', message: 'Info!' });

			const activeToasts = get(toasts);
			expect(activeToasts[0].timeout).toBe(4000);
		});

		it('accepts custom timeout', () => {
			showToast({ type: 'success', message: 'Custom', timeout: 10000 });

			const activeToasts = get(toasts);
			expect(activeToasts[0].timeout).toBe(10000);
		});

		it('allows multiple toasts to coexist', () => {
			showToast({ type: 'success', message: 'First' });
			showToast({ type: 'error', message: 'Second' });
			showToast({ type: 'info', message: 'Third' });

			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(3);
			expect(activeToasts[0].message).toBe('First');
			expect(activeToasts[1].message).toBe('Second');
			expect(activeToasts[2].message).toBe('Third');
		});
	});

	describe('dismissToast', () => {
		it('removes toast by ID', () => {
			const id1 = showToast({ type: 'success', message: 'First' });
			const id2 = showToast({ type: 'success', message: 'Second' });

			dismissToast(id1);

			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(1);
			expect(activeToasts[0].id).toBe(id2);
		});

		it('does not error if dismissing non-existent ID', () => {
			showToast({ type: 'success', message: 'Test' });

			expect(() => dismissToast('non-existent-id')).not.toThrow();

			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(1);
		});

		it('can dismiss middle toast from multiple toasts', () => {
			const id1 = showToast({ type: 'success', message: 'First' });
			const id2 = showToast({ type: 'success', message: 'Second' });
			const id3 = showToast({ type: 'success', message: 'Third' });

			dismissToast(id2);

			const activeToasts = get(toasts);
			expect(activeToasts).toHaveLength(2);
			expect(activeToasts[0].id).toBe(id1);
			expect(activeToasts[1].id).toBe(id3);
		});
	});

	describe('auto-dismiss behavior', () => {
		it('auto-dismisses success toast after 4000ms', () => {
			showToast({ type: 'success', message: 'Success!' });

			// Should still be present before timeout
			expect(get(toasts)).toHaveLength(1);

			// Advance time by 3999ms (just before timeout)
			vi.advanceTimersByTime(3999);
			expect(get(toasts)).toHaveLength(1);

			// Advance past timeout
			vi.advanceTimersByTime(1);
			expect(get(toasts)).toHaveLength(0);
		});

		it('auto-dismisses error toast after 8000ms', () => {
			showToast({ type: 'error', message: 'Error!' });

			expect(get(toasts)).toHaveLength(1);

			// Advance to just before timeout
			vi.advanceTimersByTime(7999);
			expect(get(toasts)).toHaveLength(1);

			// Advance past timeout
			vi.advanceTimersByTime(1);
			expect(get(toasts)).toHaveLength(0);
		});

		it('auto-dismisses with custom timeout', () => {
			showToast({ type: 'info', message: 'Custom', timeout: 2000 });

			vi.advanceTimersByTime(1999);
			expect(get(toasts)).toHaveLength(1);

			vi.advanceTimersByTime(1);
			expect(get(toasts)).toHaveLength(0);
		});

		it('auto-dismisses toasts independently', () => {
			showToast({ type: 'success', message: 'First', timeout: 2000 });
			showToast({ type: 'error', message: 'Second', timeout: 4000 });

			// First toast should dismiss at 2000ms
			vi.advanceTimersByTime(2000);
			const afterFirst = get(toasts);
			expect(afterFirst).toHaveLength(1);
			expect(afterFirst[0].message).toBe('Second');

			// Second toast should dismiss at 4000ms
			vi.advanceTimersByTime(2000);
			expect(get(toasts)).toHaveLength(0);
		});

		it('does not leak timeouts after manual dismiss', () => {
			const id = showToast({ type: 'success', message: 'Test', timeout: 5000 });

			// Manually dismiss before auto-dismiss
			dismissToast(id);
			expect(get(toasts)).toHaveLength(0);

			// Advance past auto-dismiss timeout — should not re-add or error
			vi.advanceTimersByTime(5000);
			expect(get(toasts)).toHaveLength(0);
		});
	});

	describe('clearToasts', () => {
		it('removes all toasts at once', () => {
			showToast({ type: 'success', message: 'First' });
			showToast({ type: 'error', message: 'Second' });
			showToast({ type: 'info', message: 'Third' });

			expect(get(toasts)).toHaveLength(3);

			clearToasts();

			expect(get(toasts)).toHaveLength(0);
		});

		it('works on empty store', () => {
			expect(() => clearToasts()).not.toThrow();
			expect(get(toasts)).toHaveLength(0);
		});
	});
});

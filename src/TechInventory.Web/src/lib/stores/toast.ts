/**
 * Toast Notification Store — Simple toast system for device CRUD feedback
 * 
 * T19-T22 infra: showToast() helper, auto-dismiss, ARIA live region support.
 * 
 * Usage:
 * ```ts
 * import { showToast } from '$lib/stores/toast';
 * showToast({ type: 'success', message: 'Device created!' });
 * ```
 * 
 * Related: specs/002-frontend-mvp/spec.md J5-J8 (device CRUD)
 */

import { writable } from 'svelte/store';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
	id: string;
	type: ToastType;
	message: string;
	timeout: number;
}

export interface ToastOptions {
	type: ToastType;
	message: string;
	timeout?: number;
}

const DEFAULT_TIMEOUT = {
	success: 4000,
	info: 4000,
	error: 8000
};

/**
 * Active toasts (up to 3 visible at a time, stacked top-right)
 */
export const toasts = writable<Toast[]>([]);

let idCounter = 0;

/**
 * Show toast notification
 */
export function showToast(options: ToastOptions): string {
	const id = `toast-${++idCounter}`;
	const timeout = options.timeout ?? DEFAULT_TIMEOUT[options.type];

	const toast: Toast = {
		id,
		type: options.type,
		message: options.message,
		timeout
	};

	toasts.update((all) => [...all, toast]);

	// Auto-dismiss after timeout
	setTimeout(() => {
		dismissToast(id);
	}, timeout);

	return id;
}

export const addToast = showToast;

/**
 * Dismiss toast by ID
 */
export function dismissToast(id: string): void {
	toasts.update((all) => all.filter((t) => t.id !== id));
}

/**
 * Clear all toasts (for logout or route change)
 */
export function clearToasts(): void {
	toasts.set([]);
}

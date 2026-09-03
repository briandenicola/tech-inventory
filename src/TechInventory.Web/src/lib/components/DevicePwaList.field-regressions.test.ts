/**
 * DevicePwaList — field regression guards (#165).
 *
 * Issue: The inset-grouped list container carried `overflow-hidden` which
 * clipped absolutely-positioned children — specifically the DeviceActionsMenu
 * dropdown that renders `absolute right-0 top-full` inside DevicePwaRow.
 * CSS overflow:hidden clips absolutely-positioned children even when their
 * z-index is high; the fix removes `overflow-hidden` from the row list
 * container so the dropdown can escape the card boundary.
 *
 * TAMPER-TESTED: Both guards are currently met by the implementation.
 *   Re-adding overflow-hidden to the row list container div causes the first
 *   test to fail; restoring it makes it pass again.
 *
 * jsdom LIMITATION: jsdom cannot verify that removing overflow-hidden actually
 * un-clips the menu at runtime. The source contract test guards against the
 * class being re-introduced. Real-device verification lives in
 * docs/testing/manual-pwa-validation.md (M-22–M-24).
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/svelte';
import DevicePwaList from './DevicePwaList.svelte';
import { createDeviceList, resetFactories } from '$lib/test-utils/factories';

vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	return {
		referenceDataStore: writable({
			brands: [],
			categories: [],
			owners: [],
			locations: [],
			networks: [],
			tags: [],
			isLoading: false,
			error: null
		})
	};
});

vi.mock('$lib/api/client', () => ({
	devices: { bulkUpdate: vi.fn() }
}));

vi.mock('$lib/stores/toast', () => ({
	showToast: vi.fn()
}));

vi.mock('$lib/utils/deviceRowActions.svelte', () => ({
	createDeviceRowActions: () => ({
		canEdit: false,
		canClaim: false,
		canRelease: false,
		canChangeStatus: false,
		canViewHistory: false,
		canDelete: false,
		openModal: null,
		handleClaim: vi.fn(),
		handleRelease: vi.fn(),
		handleChangeStatus: vi.fn(),
		handleDelete: vi.fn()
	})
}));

describe('DevicePwaList — #165 row list container overflow contract', () => {
	beforeEach(() => {
		resetFactories();
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// The div that wraps <DevicePwaRow> items must NOT carry `overflow-hidden`.
	// That class clips the absolutely-positioned DeviceActionsMenu dropdown
	// regardless of the dropdown's z-index, causing the per-device action menu
	// to be visually trapped inside the list container boundary.
	// Re-adding overflow-hidden to the row list container div causes this test
	// to fail.
	it('row list container does not carry overflow-hidden', () => {
		const devices = createDeviceList(2);
		const { container } = render(DevicePwaList, {
			props: { devices, currentUser: null }
		});

		// The row list container is the div wrapping DevicePwaRow instances.
		// It currently has: overflow-hidden rounded-2xl border ...
		// After the fix it should have the border/rounding but NOT overflow-hidden.
		const rowContainer = container.querySelector('[class*="rounded-2xl"][class*="border"]');
		expect(rowContainer, 'row list container must exist').toBeTruthy();
		expect(
			rowContainer!.className,
			'row list container must not carry overflow-hidden — that clips the DeviceActionsMenu dropdown'
		).not.toMatch(/\boverflow-hidden\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The outer DevicePwaList wrapper (flex column gap-6) must not carry
	// overflow-hidden either; only the inner row container is being changed.
	it('outer flex column wrapper does not carry overflow-hidden', () => {
		const devices = createDeviceList(1);
		const { container } = render(DevicePwaList, {
			props: { devices, currentUser: null }
		});

		const flexWrapper = container.querySelector('[class*="flex"][class*="flex-col"]');
		expect(flexWrapper, 'outer flex-col wrapper must exist').toBeTruthy();
		expect(
			flexWrapper!.className,
			'outer flex wrapper must not carry overflow-hidden'
		).not.toMatch(/\boverflow-hidden\b/);
	});
});

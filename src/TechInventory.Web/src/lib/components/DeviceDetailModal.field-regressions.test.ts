/**
 * DeviceDetailModal — field regression guards (#148 modal ancestor chain).
 *
 * Issue: If the modal surface or its scroll region lack the correct
 * overflow containment, an overflowing DeviceForm child (e.g. a native
 * date or number input with a wide iOS intrinsic minimum width) will push
 * the viewport instead of being clipped by the modal.
 *
 * Required ancestor chain contracts:
 *   • Modal surface `div[role=dialog]` carries `overflow-hidden`
 *     (clips content that escapes the rounded-corner boundary).
 *   • Scroll region div carries `overflow-y-auto` and `overscroll-contain`
 *     (enables interior scroll without propagating to the viewport).
 *
 * TAMPER-TESTED: Both classes are currently present. Removing either causes
 * the respective test to fail; restoring it makes it pass.
 *
 * jsdom LIMITATION: jsdom cannot compute overflow clipping or scroll
 * containment.  These tests assert the structural classes that enable
 * correct layout; real-device verification lives in
 * docs/testing/manual-pwa-validation.md (M-20 and M-22–M-24).
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import type { ReferenceDataState } from '$lib/stores/referenceData';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';

const { getMock, listTagsMock } = vi.hoisted(() => ({
	getMock: vi.fn(),
	listTagsMock: vi.fn()
}));

vi.mock('$lib/api/client', async () => {
	const actual = await vi.importActual<typeof import('$lib/api/client')>('$lib/api/client');
	return {
		...actual,
		devices: { ...actual.devices, get: getMock, listTags: listTagsMock }
	};
});

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await import('svelte/store');
	return {
		authStore: writable({
			currentUser: { id: 'u1', role: 'Viewer', displayName: 'T', entraObjectId: null },
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		})
	};
});

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	const initialState: ReferenceDataState = {
		brands: [],
		categories: [],
		owners: [],
		locations: [],
		networks: [],
		tags: [],
		isLoading: false,
		error: null
	};
	return { referenceDataStore: writable(initialState) };
});

import DeviceDetailModal from './DeviceDetailModal.svelte';

describe('DeviceDetailModal — #148 modal ancestor chain overflow contract', () => {
	beforeEach(() => {
		resetFactories();
		getMock.mockReset();
		listTagsMock.mockReset().mockResolvedValue([]);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The modal surface div (role=dialog) must carry overflow-hidden.
	// Without it, a wide native input child (iOS date/number) can push the
	// modal wider than the viewport instead of being clipped at the rounded edge.
	it('modal surface [role=dialog] carries overflow-hidden (tamper-tested)', async () => {
		const device = createDeviceResponse();
		getMock.mockResolvedValue(device);

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });
		// Wait for device to load
		await screen.findByRole('heading', { level: 2 });

		const dialog = document.querySelector('[role="dialog"]');
		expect(dialog, 'modal dialog element must exist').toBeTruthy();
		expect(
			dialog!.className,
			'modal surface must carry overflow-hidden to clip overflowing form inputs at the modal boundary'
		).toMatch(/\boverflow-hidden\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The scroll region div (inside the dialog, wrapping the device detail
	// content) must carry overflow-y-auto so the interior can scroll without
	// leaking scroll to the viewport.
	it('scroll region inside the modal carries overflow-y-auto (tamper-tested)', async () => {
		const device = createDeviceResponse();
		getMock.mockResolvedValue(device);

		render(DeviceDetailModal, { props: { deviceId: device.id, onClose: vi.fn() } });
		await screen.findByRole('heading', { level: 2 });

		const dialog = document.querySelector('[role="dialog"]');
		expect(dialog, 'modal dialog element must exist').toBeTruthy();

		// The scroll region is the flex-1 child div that carries overflow-y-auto.
		const scrollRegion = dialog!.querySelector('[class*="overflow-y-auto"]');
		expect(
			scrollRegion,
			'scroll region div must exist inside the dialog with overflow-y-auto'
		).toBeTruthy();
		expect(scrollRegion!.className, 'scroll region must carry overscroll-contain').toMatch(
			/\boverscroll-contain\b/
		);
	});
});

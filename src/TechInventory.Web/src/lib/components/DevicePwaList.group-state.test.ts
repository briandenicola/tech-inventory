/**
 * DevicePwaList — group expand/collapse state.
 *
 * Covers the three ways group collapse state is set:
 *   1. `defaultCollapsed` seeds it when the group set first appears, and when
 *      the user switches Group By (a genuinely different set of groups).
 *   2. `bulkToggleSignal` / `bulkToggleTarget` force every group open or closed
 *      on demand, driven from the View panel's "Expand all" / "Collapse all".
 *   3. Neither may stomp a hand-toggled group when the parent merely re-renders
 *      with an equivalent `groups` array — the devices page rebuilds that array
 *      on every refetch, so identity-based reseeding would silently close
 *      whatever the user had just opened.
 *
 * TAMPER-TESTED: each behaviour was confirmed to fail when its guard is removed
 *   — dropping the signature check in the seeding effect fails
 *   "keeps a hand-expanded group open across an equivalent groups rebuild";
 *   comparing bulkToggleSignal as a boolean fails "applies a repeated collapse
 *   signal after the user reopens a group".
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import DevicePwaList from './DevicePwaList.svelte';
import { createDeviceList, resetFactories } from '$lib/test-utils/factories';
import type { DeviceGroup } from '$lib/utils/groupDevices';
import type { DeviceResponse } from '$lib/queries/devices.svelte';

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
		canDelete: false
	})
}));

function makeGroups(devices: DeviceResponse[]): DeviceGroup[] {
	return [
		{
			key: 'category-laptops',
			label: 'Laptops',
			count: 1,
			devices: [devices[0]],
			isUnknown: false
		},
		{
			key: 'category-phones',
			label: 'Phones',
			count: 1,
			devices: [devices[1]],
			isUnknown: false
		}
	];
}

/** aria-expanded on each group header, in render order. */
function expandedStates(): string[] {
	return screen
		.getAllByTestId('device-group-section-mobile')
		.map((section) => section.querySelector('button')!.getAttribute('aria-expanded')!);
}

describe('DevicePwaList group expand/collapse state', () => {
	beforeEach(() => {
		resetFactories();
	});

	it('starts every group expanded by default', () => {
		const devices = createDeviceList(2);
		render(DevicePwaList, {
			props: { devices, groups: makeGroups(devices), currentUser: null }
		});

		expect(expandedStates()).toEqual(['true', 'true']);
	});

	it('starts every group collapsed when defaultCollapsed is set', () => {
		const devices = createDeviceList(2);
		render(DevicePwaList, {
			props: {
				devices,
				groups: makeGroups(devices),
				currentUser: null,
				defaultCollapsed: true
			}
		});

		expect(expandedStates()).toEqual(['false', 'false']);
	});

	it('lets the user open a single group when the default is collapsed', async () => {
		const devices = createDeviceList(2);
		render(DevicePwaList, {
			props: {
				devices,
				groups: makeGroups(devices),
				currentUser: null,
				defaultCollapsed: true
			}
		});

		await fireEvent.click(
			screen.getAllByTestId('device-group-section-mobile')[0].querySelector('button')!
		);

		expect(expandedStates()).toEqual(['true', 'false']);
	});

	it('keeps a hand-expanded group open across an equivalent groups rebuild', async () => {
		const devices = createDeviceList(2);
		const { rerender } = render(DevicePwaList, {
			props: {
				devices,
				groups: makeGroups(devices),
				currentUser: null,
				defaultCollapsed: true
			}
		});

		await fireEvent.click(
			screen.getAllByTestId('device-group-section-mobile')[0].querySelector('button')!
		);
		expect(expandedStates()).toEqual(['true', 'false']);

		// A refetch hands down a brand-new array describing the same groups.
		await rerender({
			devices,
			groups: makeGroups(devices),
			currentUser: null,
			defaultCollapsed: true
		});

		expect(expandedStates()).toEqual(['true', 'false']);
	});

	it('re-seeds from defaultCollapsed when the group set itself changes', async () => {
		const devices = createDeviceList(2);
		const { rerender } = render(DevicePwaList, {
			props: {
				devices,
				groups: makeGroups(devices),
				currentUser: null,
				defaultCollapsed: true
			}
		});

		await fireEvent.click(
			screen.getAllByTestId('device-group-section-mobile')[0].querySelector('button')!
		);
		expect(expandedStates()).toEqual(['true', 'false']);

		// Switching Group By produces different keys — a fresh seed is correct here.
		await rerender({
			devices,
			groups: [
				{ key: 'owner-ada', label: 'Ada', count: 2, devices, isUnknown: false }
			] satisfies DeviceGroup[],
			currentUser: null,
			defaultCollapsed: true
		});

		expect(expandedStates()).toEqual(['false']);
	});

	it('collapses every group when the bulk signal fires with target "collapse"', async () => {
		const devices = createDeviceList(2);
		const { rerender } = render(DevicePwaList, {
			props: { devices, groups: makeGroups(devices), currentUser: null, bulkToggleSignal: 0 }
		});

		expect(expandedStates()).toEqual(['true', 'true']);

		await rerender({
			devices,
			groups: makeGroups(devices),
			currentUser: null,
			bulkToggleSignal: 1,
			bulkToggleTarget: 'collapse' as const
		});

		expect(expandedStates()).toEqual(['false', 'false']);
	});

	it('expands every group when the bulk signal fires with target "expand"', async () => {
		const devices = createDeviceList(2);
		const { rerender } = render(DevicePwaList, {
			props: {
				devices,
				groups: makeGroups(devices),
				currentUser: null,
				defaultCollapsed: true,
				bulkToggleSignal: 0
			}
		});

		expect(expandedStates()).toEqual(['false', 'false']);

		await rerender({
			devices,
			groups: makeGroups(devices),
			currentUser: null,
			defaultCollapsed: true,
			bulkToggleSignal: 1,
			bulkToggleTarget: 'expand' as const
		});

		expect(expandedStates()).toEqual(['true', 'true']);
	});

	it('applies a repeated collapse signal after the user reopens a group', async () => {
		const devices = createDeviceList(2);
		const base = { devices, groups: makeGroups(devices), currentUser: null };
		const { rerender } = render(DevicePwaList, {
			props: { ...base, bulkToggleSignal: 0 }
		});

		await rerender({ ...base, bulkToggleSignal: 1, bulkToggleTarget: 'collapse' as const });
		expect(expandedStates()).toEqual(['false', 'false']);

		await fireEvent.click(
			screen.getAllByTestId('device-group-section-mobile')[0].querySelector('button')!
		);
		expect(expandedStates()).toEqual(['true', 'false']);

		// Same target, next tick of the counter — must take effect again.
		await rerender({ ...base, bulkToggleSignal: 2, bulkToggleTarget: 'collapse' as const });
		expect(expandedStates()).toEqual(['false', 'false']);
	});

	it('renders an ungrouped list unchanged when defaultCollapsed is set', () => {
		const devices = createDeviceList(3);
		render(DevicePwaList, {
			props: { devices, currentUser: null, defaultCollapsed: true }
		});

		expect(screen.queryAllByTestId('device-group-section-mobile')).toHaveLength(0);
		expect(screen.getByText(devices[0].name!)).toBeInTheDocument();
	});
});

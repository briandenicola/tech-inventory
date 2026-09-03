import type { DeviceResponse } from '$lib/queries/devices.svelte';
import { deviceStatusValues } from '$lib/schemas/device';

/**
 * Role/ownership gate for the single-device "Change Status" row/detail
 * action (#127). Replaces the old status-specific `canRetireDevice`
 * (Active-only) / `canUnretireDevice` (Retired-only) pair — the underlying
 * mutation now goes through `POST /api/v1/devices/bulk/update`
 * (`BulkUpdateDevicesCommandHandler`), which already enforces every
 * domain-valid transition (Retired → Active via `Device.Reactivate()`,
 * Retired/Active → Disposed, etc.) and rejects a Disposed device outright
 * ("Device is already disposed."). So the only client-side gate left is:
 * same Admin-any / Member-if-owner rule the old functions used, and "not
 * already Disposed" (a real terminal state, not just a UI restriction — the
 * bulk endpoint 409s on it). This is an affordance gate, not the security
 * boundary; the API has no per-device ownership check of its own (see
 * `.squad/decisions/vasquez-130-133-client-status.md`).
 */
export function canChangeDeviceStatus(
	device: Pick<DeviceResponse, 'status' | 'ownerId'> | null,
	currentUser: { id: string; role: string } | null | undefined
): boolean {
	if (!device || device.status === 'Disposed' || !currentUser) {
		return false;
	}

	return currentUser.role === 'Admin' || (currentUser.role === 'Member' && device.ownerId === currentUser.id);
}

/**
 * Builds the `{ id, name }` option list the shared `BulkUpdateModal` status
 * select expects, from the same `DeviceStatus` enum + `devices.filters.status*`
 * i18n keys `DeviceForm`'s status control already uses — so the single-device
 * dialog and the full-edit-form dropdown can never drift on which statuses
 * exist or how they're labeled.
 */
export function deviceStatusOptions(
	translate: (key: string) => string
): Array<{ id: string; name: string }> {
	return deviceStatusValues.map((status) => ({
		id: status,
		name: translate(`devices.filters.status${status}`)
	}));
}

/**
 * deviceRowActions.svelte.ts — F045 §5.4 shared controller for the PWA row's
 * `DeviceActionsMenu`.
 *
 * Mirrors the permission derivations and API handlers already wired in
 * `DeviceDetailModal.svelte` (edit / claim / release / change status /
 * view history / delete) so `DevicePwaRow` reuses the same rules instead of
 * forking them. `DeviceDetailModal` keeps its own inline copy because it also
 * owns the fetch/refresh lifecycle for a single open device — this composable
 * is for a list of rows, each needing only the mutation + permission slice.
 *
 * `canClaim`/`canRelease` are role-gated (Admin/Member only) **and**
 * ownership-gated — constitution §5.2 / `docs/prd.md` define Viewer as
 * read-only, so a Viewer must never see a mutation affordance regardless of
 * device ownership. The API's `PATCH /devices/{id}/owner` endpoint already
 * enforces this via `AuthorizationPolicies.AdminOrMember`
 * (`ViewerRoleAuthorizationTests.ClaimDeviceOwnership_WhenCallerIsViewer_ReturnsForbidden` /
 * `.ReleaseDeviceOwnership_WhenCallerIsViewer_ReturnsForbidden`); this is the
 * matching client-side affordance gate, not the security boundary.
 *
 * `handleChangeStatus` (#127) intentionally calls the same
 * `devices.bulkUpdate` the N-device bulk toolbar uses, scoped to this one
 * device id, instead of re-deriving a full `devices.update` payload the way
 * the old per-status Retire/Unretire handlers did — the bulk endpoint's
 * handler already implements every domain-valid transition (including
 * Retired → Active via `Device.Reactivate()`) so there is exactly one status
 * transition implementation on the client, not two.
 */
import { devices } from '$lib/api/client';
import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
import type { DeviceResponse } from '$lib/queries/devices.svelte';
import { showToast } from '$lib/stores/toast';
import type { CurrentUser } from '$lib/stores/auth';
import type { DeviceStatus } from '$lib/api/types';
import { t } from '$lib/i18n';
import { getApiErrorMessage } from '$lib/utils/apiErrors';
import { canChangeDeviceStatus } from '$lib/utils/deviceRetirement';

export type DeviceRowModal = 'claim' | 'release' | 'changeStatus' | 'delete' | 'history' | null;

export interface DeviceRowActionsOptions {
	onChanged?: () => void;
}

export interface DeviceRowActionsController {
	openModal: DeviceRowModal;
	readonly canEdit: boolean;
	readonly canDelete: boolean;
	readonly canViewHistory: boolean;
	readonly canClaim: boolean;
	readonly canRelease: boolean;
	readonly canChangeStatus: boolean;
	handleClaim(): Promise<void>;
	handleRelease(): Promise<void>;
	handleChangeStatus(status: string): Promise<void>;
	handleDelete(reason: string): Promise<void>;
}

export function createDeviceRowActions(
	getDevice: () => DeviceResponse | null,
	getCurrentUser: () => CurrentUser | null,
	options: DeviceRowActionsOptions = {}
): DeviceRowActionsController {
	let openModal = $state<DeviceRowModal>(null);

	const canEdit = $derived.by(() => {
		const role = getCurrentUser()?.role;
		return role === 'Admin' || role === 'Member';
	});
	const canDelete = $derived(getCurrentUser()?.role === 'Admin');
	const canViewHistory = $derived(getCurrentUser()?.role === 'Admin');
	const canClaim = $derived.by(() => {
		const device = getDevice();
		const user = getCurrentUser();
		const role = user?.role;
		if (role !== 'Admin' && role !== 'Member') return false;
		return Boolean(device && user && device.ownerId !== user.id);
	});
	const canRelease = $derived.by(() => {
		const device = getDevice();
		const user = getCurrentUser();
		const role = user?.role;
		if (role !== 'Admin' && role !== 'Member') return false;
		return Boolean(device && user && device.ownerId === user.id);
	});
	const canChangeStatus = $derived(canChangeDeviceStatus(getDevice(), getCurrentUser()));

	async function handleClaim(): Promise<void> {
		const device = getDevice();
		const user = getCurrentUser();
		if (!device || !user) return;

		try {
			await devices.updateOwner(device.id, user.id);
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: t('devices.claim.toast.success').replace('{name}', device.name ?? 'Device')
			});
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Claim failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to claim ownership') });
		} finally {
			openModal = null;
		}
	}

	async function handleRelease(): Promise<void> {
		const device = getDevice();
		if (!device) return;

		try {
			await devices.updateOwner(device.id, null);
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: t('devices.release.toast.success').replace('{name}', device.name ?? 'Device')
			});
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Release failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to release ownership') });
		} finally {
			openModal = null;
		}
	}

	async function handleChangeStatus(status: string): Promise<void> {
		const device = getDevice();
		if (!device) return;

		try {
			await devices.bulkUpdate({ deviceIds: [device.id], changes: { status: status as DeviceStatus } });
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: t('devices.changeStatus.toast.success', { name: device.name ?? 'Device' })
			});
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Change status failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to change status') });
		} finally {
			openModal = null;
		}
	}

	async function handleDelete(reason: string): Promise<void> {
		const device = getDevice();
		if (!device) return;

		try {
			await devices.delete(device.id, reason);
			invalidateDevicesCache();
			showToast({ type: 'success', message: t('devices.delete.success') });
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Delete failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to delete device') });
		} finally {
			openModal = null;
		}
	}

	return {
		get openModal() {
			return openModal;
		},
		set openModal(value: DeviceRowModal) {
			openModal = value;
		},
		get canEdit() {
			return canEdit;
		},
		get canDelete() {
			return canDelete;
		},
		get canViewHistory() {
			return canViewHistory;
		},
		get canClaim() {
			return canClaim;
		},
		get canRelease() {
			return canRelease;
		},
		get canChangeStatus() {
			return canChangeStatus;
		},
		handleClaim,
		handleRelease,
		handleChangeStatus,
		handleDelete
	};
}

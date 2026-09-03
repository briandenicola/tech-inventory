/**
 * deviceRowActions.svelte.ts — F045 §5.4 shared controller for the PWA row's
 * `DeviceActionsMenu`.
 *
 * Mirrors the permission derivations and API handlers already wired in
 * `DeviceDetailModal.svelte` (edit / claim / release / retire / unretire /
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
 */
import { devices } from '$lib/api/client';
import { invalidateDevicesCache } from '$lib/queries/devices.svelte';
import type { DeviceResponse } from '$lib/queries/devices.svelte';
import { showToast } from '$lib/stores/toast';
import type { CurrentUser } from '$lib/stores/auth';
import { t } from '$lib/i18n';
import { getApiErrorMessage } from '$lib/utils/apiErrors';
import {
	buildRetireDeviceRequest,
	buildUnretireDeviceRequest,
	canRetireDevice,
	canUnretireDevice
} from '$lib/utils/deviceRetirement';

export type DeviceRowModal = 'claim' | 'release' | 'retire' | 'delete' | 'history' | null;

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
	readonly canRetire: boolean;
	readonly canUnretire: boolean;
	handleClaim(): Promise<void>;
	handleRelease(): Promise<void>;
	handleRetire(): Promise<void>;
	handleUnretire(): Promise<void>;
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
	const canRetire = $derived(canRetireDevice(getDevice(), getCurrentUser()));
	const canUnretire = $derived(canUnretireDevice(getDevice(), getCurrentUser()));

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

	async function handleRetire(): Promise<void> {
		const device = getDevice();
		if (!device) return;

		try {
			await devices.update(device.id, buildRetireDeviceRequest(device, new Date()));
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: t('devices.retire.toast.success').replace('{name}', device.name ?? 'Device')
			});
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Retire failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to retire device') });
		} finally {
			openModal = null;
		}
	}

	async function handleUnretire(): Promise<void> {
		const device = getDevice();
		if (!device) return;

		try {
			await devices.update(device.id, buildUnretireDeviceRequest(device));
			invalidateDevicesCache();
			showToast({
				type: 'success',
				message: t('devices.unretire.toast.success').replace('{name}', device.name ?? 'Device')
			});
			options.onChanged?.();
		} catch (err) {
			console.error('[DevicePwaRow] Unretire failed:', err);
			showToast({ type: 'error', message: getApiErrorMessage(err, 'Failed to unretire device') });
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
		get canRetire() {
			return canRetire;
		},
		get canUnretire() {
			return canUnretire;
		},
		handleClaim,
		handleRelease,
		handleRetire,
		handleUnretire,
		handleDelete
	};
}

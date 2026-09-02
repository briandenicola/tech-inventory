import type { components } from '$lib/api/generated/types';
import type { DeviceResponse } from '$lib/queries/devices.svelte';

type UpdateDeviceRequest = components['schemas']['UpdateDeviceRequest'];

export function canRetireDevice(
	device: Pick<DeviceResponse, 'status' | 'ownerId'> | null,
	currentUser: { id: string; role: string } | null | undefined
): boolean {
	if (!device || device.status !== 'Active' || !currentUser) {
		return false;
	}

	return currentUser.role === 'Admin' || (currentUser.role === 'Member' && device.ownerId === currentUser.id);
}

export function canUnretireDevice(
	device: Pick<DeviceResponse, 'status' | 'ownerId'> | null,
	currentUser: { id: string; role: string } | null | undefined
): boolean {
	if (!device || device.status !== 'Retired' || !currentUser) {
		return false;
	}

	return currentUser.role === 'Admin' || (currentUser.role === 'Member' && device.ownerId === currentUser.id);
}

/**
 * Formats a Date as the `yyyy-MM-dd` string an OpenAPI `format: date` field
 * requires.
 *
 * The API binds `retiredDate` to a C# `DateOnly?`, and System.Text.Json's
 * built-in converter accepts only the ISO full-date form. Passing a full
 * timestamp — which is what `Date.prototype.toISOString()` returns — fails to
 * bind, so the request never reaches the handler and comes back as a 400
 * carrying no ProblemDetails `detail`.
 *
 * Built from local date parts on purpose: retiring a device at 7pm Central
 * should record today's date, not tomorrow's, which is what slicing the UTC
 * timestamp would give for anyone west of Greenwich in the evening.
 */
export function toDateOnly(value: Date): string {
	const year = value.getFullYear();
	const month = String(value.getMonth() + 1).padStart(2, '0');
	const day = String(value.getDate()).padStart(2, '0');
	return `${year}-${month}-${day}`;
}

export function buildRetireDeviceRequest(
	device: DeviceResponse,
	retiredOn: Date
): UpdateDeviceRequest {
	return {
		name: device.name,
		model: device.model,
		serialNumber: device.serialNumber,
		brandId: device.brandId,
		categoryId: device.categoryId,
		ownerId: device.ownerId ?? undefined,
		locationId: device.locationId ?? undefined,
		networkId: device.networkId,
		purchaseDate: device.purchaseDate,
		purchasePrice: device.purchasePrice,
		currencyCode: device.currencyCode,
		status: 'Retired',
		retiredDate: toDateOnly(retiredOn),
		disposalMethod: device.disposalMethod,
		notes: device.notes,
		purpose: device.purpose,
		operatingSystem: device.operatingSystem,
		ipAddress: device.ipAddress,
		macAddress: device.macAddress,
		productUrl: device.productUrl,
		version: device.version
	};
}

export function buildUnretireDeviceRequest(device: DeviceResponse): UpdateDeviceRequest {
	return {
		name: device.name,
		model: device.model,
		serialNumber: device.serialNumber,
		brandId: device.brandId,
		categoryId: device.categoryId,
		ownerId: device.ownerId ?? undefined,
		locationId: device.locationId ?? undefined,
		networkId: device.networkId,
		purchaseDate: device.purchaseDate,
		purchasePrice: device.purchasePrice,
		currencyCode: device.currencyCode,
		status: 'Active',
		retiredDate: null,
		disposalMethod: null,
		notes: device.notes,
		purpose: device.purpose,
		operatingSystem: device.operatingSystem,
		ipAddress: device.ipAddress,
		macAddress: device.macAddress,
		productUrl: device.productUrl,
		version: device.version
	};
}

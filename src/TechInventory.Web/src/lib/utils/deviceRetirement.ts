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

export function buildRetireDeviceRequest(
	device: DeviceResponse,
	retiredDate: string
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
		retiredDate,
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

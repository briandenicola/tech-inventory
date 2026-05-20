import api from '$lib/api/client';
import type { MergeEntityRequest, MergeEntityResponse } from '$lib/api/types';
import type { ReferenceEntity } from '$lib/stores/referenceData';

export type MergeEntityType = 'brand' | 'category' | 'location' | 'network';

export interface MergeEntityOption extends ReferenceEntity {
	deviceCount?: number | null;
}

type DeviceCountQuery = {
	Page: number;
	PageSize: number;
	BrandId?: string;
	CategoryId?: string;
	LocationId?: string;
	NetworkId?: string;
};

export function buildMergeTargetOptions(
	entities: ReferenceEntity[],
	sourceId: string
): MergeEntityOption[] {
	return entities
		.filter((entity) => entity.id !== sourceId)
		.map((entity) => ({
			id: entity.id,
			name: entity.name
		}))
		.sort((left, right) => left.name.localeCompare(right.name));
}

export function sortMergeEntityOptions(entities: ReferenceEntity[]): MergeEntityOption[] {
	return entities
		.map((entity) => ({
			id: entity.id,
			name: entity.name
		}))
		.sort((left, right) => left.name.localeCompare(right.name));
}

export async function fetchReferenceDeviceCount(
	entityType: MergeEntityType,
	sourceId: string
): Promise<number> {
	const params: DeviceCountQuery = {
		Page: 1,
		PageSize: 1
	};

	if (entityType === 'brand') {
		params.BrandId = sourceId;
	}

	if (entityType === 'category') {
		params.CategoryId = sourceId;
	}

	if (entityType === 'location') {
		params.LocationId = sourceId;
	}

	if (entityType === 'network') {
		params.NetworkId = sourceId;
	}

	const response = await api.devices.list(params);
	return response.totalCount ?? 0;
}

export async function fetchMergeDeviceCount(
	entityType: MergeEntityType,
	sourceId: string
): Promise<number> {
	return fetchReferenceDeviceCount(entityType, sourceId);
}

export async function mergeReferenceEntities(
	entityType: MergeEntityType,
	request: MergeEntityRequest
): Promise<MergeEntityResponse> {
	switch (entityType) {
		case 'brand':
			return api.brands.merge(request);
		case 'category':
			return api.categories.merge(request);
		case 'location':
			return api.locations.merge(request);
		case 'network':
			return api.networks.merge(request);
	}
}

export async function mergeReferenceEntitySelection(
	entityType: MergeEntityType,
	sourceIds: string[],
	targetId: string
): Promise<number> {
	let mergedCount = 0;

	for (const sourceId of sourceIds) {
		if (sourceId === targetId) {
			continue;
		}

		const response = await mergeReferenceEntities(entityType, { sourceId, targetId });
		mergedCount += response.mergedCount;
	}

	return mergedCount;
}

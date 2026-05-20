import api from '$lib/api/client';
import type {
	BrandResponse,
	CategoryResponse,
	LocationResponse,
	MergeEntityRequest,
	MergeEntityResponse
} from '$lib/api/types';
import type { ReferenceEntity } from '$lib/stores/referenceData';

export type MergeEntityType = 'brand' | 'category' | 'location';

export interface MergeEntityOption extends ReferenceEntity {
	deviceCount?: number | null;
}

type MergeSourceResponse = BrandResponse | CategoryResponse | LocationResponse;

type DeviceCountQuery = {
	Page: number;
	PageSize: number;
	BrandId?: string;
	CategoryId?: string;
	LocationId?: string;
};

export function toMergeEntityOption(entity: MergeSourceResponse): MergeEntityOption | null {
	if (!entity.id || !entity.name) {
		return null;
	}

	return {
		id: entity.id,
		name: entity.name
	};
}

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

export async function fetchMergeDeviceCount(
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

	const response = await api.devices.list(params);
	return response.totalCount ?? 0;
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
	}
}

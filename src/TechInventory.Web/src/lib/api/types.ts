/**
 * Re-exports of generated OpenAPI response/request schemas as named types.
 *
 * Hand-rolling these aliases keeps consumer code clean (`BrandResponse`)
 * instead of leaking the generated `components['schemas'][...]` indirection.
 * Constitution §6.5.2: API client is generated from OpenAPI — these names
 * mirror the backend DTOs verbatim.
 */
import type { components } from './generated/types';

type Schemas = components['schemas'];

export type BrandResponse = Schemas['BrandResponse'];
export type CategoryResponse = Schemas['CategoryResponse'];
export type DeviceResponse = Schemas['DeviceResponse'];
export type LocationResponse = Schemas['LocationResponse'];
export type NetworkResponse = Schemas['NetworkResponse'];
export type OwnerResponse = Schemas['OwnerResponse'];
export type TagResponse = Schemas['TagResponse'];

export type OwnerRole = Schemas['OwnerRole'];
export type LocationType = Schemas['LocationType'];
export type DeviceStatus = Schemas['DeviceStatus'];

// Import / Export DTOs
export type PreviewImportResult = Schemas['PreviewImportResult'];
export type CommitImportResult = Schemas['CommitImportResult'];
export type ImportRowError = Schemas['ImportRowError'];
export type ImportRowPreview = Schemas['ImportRowPreview'];
export type ImportFieldError = Schemas['ImportFieldError'];
export type MissingLookup = Schemas['MissingLookup'];
export type ExportFormat = Schemas['ExportFormat'];

export interface MergeEntityRequest {
	sourceId: string;
	targetId: string;
}

export interface MergeEntityResponse {
	mergedCount: number;
	sourceId: string;
	targetId: string;
}

export interface ReportBreakdownResponseItem {
	label?: string | null;
	name?: string | null;
	status?: string | null;
	count?: number | null;
}

export interface SummaryReportResponse {
	totalDevices?: number;
	totalActiveDeviceCount?: number;
	totalValue?: number;
	totalEstimatedValue?: number;
	byCategory?: ReportBreakdownResponseItem[] | null;
	devicesByCategory?: ReportBreakdownResponseItem[] | null;
	byLocation?: ReportBreakdownResponseItem[] | null;
	devicesByLocation?: ReportBreakdownResponseItem[] | null;
	byStatus?: ReportBreakdownResponseItem[] | null;
	devicesByStatus?: ReportBreakdownResponseItem[] | null;
}

export interface WarrantyReportItemResponse {
	deviceId?: string | null;
	deviceName?: string | null;
	brandName?: string | null;
	brand?: string | null;
	purchaseDate?: string | null;
	warrantyExpiry?: string | null;
	daysUntilExpiry?: number | null;
	daysRemaining?: number | null;
}

export interface WarrantyReportResponse {
	totalCount?: number;
	items?: WarrantyReportItemResponse[] | null;
	devices?: WarrantyReportItemResponse[] | null;
	asOfDate?: string | null;
	expiringWithinDays?: number;
}

export interface EraReportDecadeResponse {
	decade?: string | null;
	startYear?: number | null;
	endYear?: number | null;
	deviceCount?: number | null;
	totalValue?: number | null;
	sampleDevices?: string[] | null;
}

export interface EraReportResponse {
	decades?: EraReportDecadeResponse[] | null;
	asOfDate?: string | null;
	appliedCategoryId?: string | null;
}

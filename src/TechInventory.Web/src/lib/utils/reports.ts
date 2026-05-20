import type {
	SummaryReportResponse,
	WarrantyReportItemResponse,
	WarrantyReportResponse
} from '$lib/api/types';

export interface ReportBreakdownItemViewModel {
	label: string;
	count: number;
}

export interface StatusBreakdownItemViewModel {
	status: string;
	count: number;
}

export interface SummaryReportViewModel {
	totalDevices: number;
	totalValue: number;
	byCategory: ReportBreakdownItemViewModel[];
	byLocation: ReportBreakdownItemViewModel[];
	byStatus: StatusBreakdownItemViewModel[];
}

export type WarrantyStatusTone = 'urgent' | 'warning' | 'info';
export type WarrantySortDirection = 'asc' | 'desc';

export interface WarrantyReportItemViewModel {
	deviceId: string | null;
	deviceName: string;
	brandName: string | null;
	purchaseDate: string | null;
	warrantyExpiry: string;
	daysUntilExpiry: number;
	tone: WarrantyStatusTone;
}

export interface WarrantyReportViewModel {
	totalCount: number;
	items: WarrantyReportItemViewModel[];
}

function isFiniteNumber(value: unknown): value is number {
	return typeof value === 'number' && Number.isFinite(value);
}

function normalizeLabel(value: unknown, fallback: string): string {
	return typeof value === 'string' && value.trim().length > 0 ? value.trim() : fallback;
}

function normalizeCount(value: unknown): number {
	return isFiniteNumber(value) ? value : 0;
}

function normalizeBreakdownItems(
	items: SummaryReportResponse['byCategory'] | SummaryReportResponse['devicesByCategory'],
	fallbackLabel: string
): ReportBreakdownItemViewModel[] {
	return (items ?? []).map((item) => ({
		label: normalizeLabel(item?.name ?? item?.label, fallbackLabel),
		count: normalizeCount(item?.count)
	}));
}

function normalizeStatusItems(
	items: SummaryReportResponse['byStatus'] | SummaryReportResponse['devicesByStatus'],
	fallbackLabel: string
): StatusBreakdownItemViewModel[] {
	return (items ?? []).map((item) => ({
		status: normalizeLabel(item?.status ?? item?.name ?? item?.label, fallbackLabel),
		count: normalizeCount(item?.count)
	}));
}

function getWarrantyItems(
	response: WarrantyReportResponse
): WarrantyReportItemResponse[] {
	return response.items ?? response.devices ?? [];
}

export function normalizeSummaryReport(
	response: SummaryReportResponse,
	fallbackLabel = 'Unknown'
): SummaryReportViewModel {
	return {
		totalDevices: normalizeCount(response.totalDevices ?? response.totalActiveDeviceCount),
		totalValue: normalizeCount(response.totalValue ?? response.totalEstimatedValue),
		byCategory: normalizeBreakdownItems(response.byCategory ?? response.devicesByCategory, fallbackLabel),
		byLocation: normalizeBreakdownItems(response.byLocation ?? response.devicesByLocation, fallbackLabel),
		byStatus: normalizeStatusItems(response.byStatus ?? response.devicesByStatus, fallbackLabel)
	};
}

export function getWarrantyStatusTone(daysUntilExpiry: number): WarrantyStatusTone {
	if (daysUntilExpiry < 30) {
		return 'urgent';
	}

	if (daysUntilExpiry < 60) {
		return 'warning';
	}

	return 'info';
}

export function normalizeWarrantyReport(response: WarrantyReportResponse): WarrantyReportViewModel {
	const items = getWarrantyItems(response)
		.map((item) => ({
			deviceId: typeof item.deviceId === 'string' && item.deviceId.length > 0 ? item.deviceId : null,
			deviceName: normalizeLabel(item.deviceName, '—'),
			brandName: typeof item.brandName === 'string' ? item.brandName : item.brand ?? null,
			purchaseDate: typeof item.purchaseDate === 'string' ? item.purchaseDate : null,
			warrantyExpiry: normalizeLabel(item.warrantyExpiry, ''),
			daysUntilExpiry: normalizeCount(item.daysUntilExpiry ?? item.daysRemaining),
			tone: getWarrantyStatusTone(normalizeCount(item.daysUntilExpiry ?? item.daysRemaining))
		}))
		.filter((item) => item.warrantyExpiry.length > 0);

	return {
		totalCount: normalizeCount(response.totalCount) || items.length,
		items
	};
}

function getDateOnlySortValue(date: string): number {
	const parts = date.split('-').map((part) => Number.parseInt(part, 10));
	if (parts.length !== 3 || parts.some((part) => Number.isNaN(part))) {
		return Number.POSITIVE_INFINITY;
	}

	return Date.UTC(parts[0] ?? 0, (parts[1] ?? 1) - 1, parts[2] ?? 1);
}

export function sortWarrantyItems(
	items: WarrantyReportItemViewModel[],
	direction: WarrantySortDirection = 'asc'
): WarrantyReportItemViewModel[] {
	const modifier = direction === 'asc' ? 1 : -1;

	return [...items].sort((left, right) => {
		const dateDifference = getDateOnlySortValue(left.warrantyExpiry) - getDateOnlySortValue(right.warrantyExpiry);
		if (dateDifference !== 0) {
			return dateDifference * modifier;
		}

		return left.deviceName.localeCompare(right.deviceName) * modifier;
	});
}

export function formatReportCurrency(value: number, currency = 'USD'): string {
	return new Intl.NumberFormat('en-US', {
		style: 'currency',
		currency,
		maximumFractionDigits: 2,
		minimumFractionDigits: 2
	}).format(value);
}

export function formatDateOnly(date: string | null): string {
	if (!date) {
		return '—';
	}

	const parts = date.split('-').map((part) => Number.parseInt(part, 10));
	if (parts.length !== 3 || parts.some((part) => Number.isNaN(part))) {
		return date;
	}

	const parsedDate = new Date(Date.UTC(parts[0] ?? 0, (parts[1] ?? 1) - 1, parts[2] ?? 1));
	return parsedDate.toLocaleDateString('en-US', {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
		timeZone: 'UTC'
	});
}

import type {
	EraReportResponse,
	SummaryReportResponse,
	TimelineReportGroupBy,
	TimelineReportResponse,
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

export interface EraReportDecadeViewModel {
	decade: string;
	startYear: number;
	endYear: number;
	deviceCount: number;
	totalValue: number;
	sampleDevices: string[];
}

export interface EraReportViewModel {
	decades: EraReportDecadeViewModel[];
	asOfDate: string | null;
	appliedCategoryId: string | null;
}

export interface TimelineEntryViewModel {
	deviceName: string;
	brand: string | null;
	purchaseDate: string;
	disposalDate: string | null;
	groupLabel: string;
	estimatedValue: number | null;
	endDate: string;
	isActive: boolean;
	durationYears: number;
	durationDays: number;
	startOffsetDays: number;
	startPercent: number;
	widthPercent: number;
}

export interface TimelineGroupViewModel {
	label: string;
	entries: TimelineEntryViewModel[];
}

export interface TimelineAxisTickViewModel {
	year: number;
	label: string;
	offsetPercent: number;
}

export interface TimelineReportViewModel {
	entries: TimelineEntryViewModel[];
	groups: TimelineGroupViewModel[];
	axisTicks: TimelineAxisTickViewModel[];
	minDate: string | null;
	maxDate: string | null;
	totalSpanDays: number;
	asOfDate: string | null;
	groupBy: TimelineReportGroupBy;
	appliedCategoryId: string | null;
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

export function normalizeEraReport(response: EraReportResponse): EraReportViewModel {
	const decades = (response.decades ?? [])
		.map((item) => ({
			decade: normalizeLabel(item?.decade, 'Unknown'),
			startYear: normalizeCount(item?.startYear),
			endYear: normalizeCount(item?.endYear),
			deviceCount: normalizeCount(item?.deviceCount),
			totalValue: isFiniteNumber(item?.totalValue) ? item.totalValue : 0,
			sampleDevices: (item?.sampleDevices ?? []).filter(
				(device): device is string => typeof device === 'string' && device.trim().length > 0
			)
		}))
		.sort((left, right) => left.startYear - right.startYear);

	return {
		decades,
		asOfDate: typeof response.asOfDate === 'string' ? response.asOfDate : null,
		appliedCategoryId:
			typeof response.appliedCategoryId === 'string' && response.appliedCategoryId.length > 0
				? response.appliedCategoryId
				: null
	};
}

function parseDateOnly(date: unknown): Date | null {
	if (typeof date !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
		return null;
	}

	const [year, month, day] = date.split('-').map((part) => Number.parseInt(part, 10));
	if ([year, month, day].some((part) => Number.isNaN(part))) {
		return null;
	}

	const value = new Date(Date.UTC(year ?? 0, (month ?? 1) - 1, day ?? 1));
	return value.getUTCFullYear() === year && value.getUTCMonth() === (month ?? 1) - 1 && value.getUTCDate() === day ? value : null;
}

function toDateOnly(date: Date): string {
	return date.toISOString().slice(0, 10);
}

function toUtcDate(date: Date): Date {
	return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
}

function getDayDifference(start: Date, end: Date): number {
	return Math.floor((end.getTime() - start.getTime()) / 86_400_000);
}

function buildTimelineAxisTicks(
	minDate: Date,
	maxDate: Date,
	totalSpanDays: number
): TimelineAxisTickViewModel[] {
	const ticks: TimelineAxisTickViewModel[] = [];

	for (let year = minDate.getUTCFullYear(); year <= maxDate.getUTCFullYear(); year += 1) {
		const tickDate = new Date(Date.UTC(year, 0, 1));
		const clampedDate = tickDate < minDate ? minDate : tickDate > maxDate ? maxDate : tickDate;
		const offsetDays = getDayDifference(minDate, clampedDate);
		ticks.push({
			year,
			label: `${year}`,
			offsetPercent: totalSpanDays <= 1 ? 0 : (offsetDays / Math.max(totalSpanDays - 1, 1)) * 100
		});
	}

	return ticks;
}

export function normalizeTimelineReport(
	response: TimelineReportResponse,
	today = new Date(),
	fallbackGroupLabel = 'Unknown'
): TimelineReportViewModel {
	const asOfDateValue = parseDateOnly(response.asOfDate) ?? toUtcDate(today);
	const normalizedAsOfDate = toDateOnly(asOfDateValue);
	const rawEntries = (response.entries ?? [])
		.map((item) => {
			const purchaseDateValue = parseDateOnly(item?.purchaseDate);
			if (!purchaseDateValue) {
				return null;
			}

			const disposalDateValue = parseDateOnly(item?.disposalDate);
			const resolvedEndDate =
				disposalDateValue && disposalDateValue >= purchaseDateValue
					? disposalDateValue
					: asOfDateValue >= purchaseDateValue
						? asOfDateValue
						: purchaseDateValue;
			const durationDays = getDayDifference(purchaseDateValue, resolvedEndDate) + 1;

			return {
				deviceName: normalizeLabel(item?.deviceName, '—'),
				brand: typeof item?.brand === 'string' && item.brand.trim().length > 0 ? item.brand.trim() : null,
				purchaseDate: toDateOnly(purchaseDateValue),
				disposalDate: disposalDateValue ? toDateOnly(disposalDateValue) : null,
				groupLabel: normalizeLabel(item?.groupLabel, fallbackGroupLabel),
				estimatedValue: isFiniteNumber(item?.estimatedValue) ? item.estimatedValue : null,
				endDate: toDateOnly(resolvedEndDate),
				isActive: disposalDateValue === null,
				durationYears: durationDays / 365.25,
				durationDays,
				purchaseDateValue,
				endDateValue: resolvedEndDate
			};
		})
		.filter((item): item is NonNullable<typeof item> => item !== null);

	if (rawEntries.length === 0) {
		return {
			entries: [],
			groups: [],
			axisTicks: [],
			minDate: null,
			maxDate: null,
			totalSpanDays: 0,
			asOfDate: normalizedAsOfDate,
			groupBy: response.groupBy === 'Owner' ? 'Owner' : 'Category',
			appliedCategoryId:
				typeof response.appliedCategoryId === 'string' && response.appliedCategoryId.length > 0
					? response.appliedCategoryId
					: null
		};
	}

	const minDateValue = rawEntries.reduce(
		(earliest, item) => (item.purchaseDateValue < earliest ? item.purchaseDateValue : earliest),
		rawEntries[0].purchaseDateValue
	);
	const maxDateValue = rawEntries.reduce(
		(latest, item) => (item.endDateValue > latest ? item.endDateValue : latest),
		rawEntries[0].endDateValue
	);
	const totalSpanDays = getDayDifference(minDateValue, maxDateValue) + 1;
	const entries = rawEntries
		.map((item) => ({
			deviceName: item.deviceName,
			brand: item.brand,
			purchaseDate: item.purchaseDate,
			disposalDate: item.disposalDate,
			groupLabel: item.groupLabel,
			estimatedValue: item.estimatedValue,
			endDate: item.endDate,
			isActive: item.isActive,
			durationYears: item.durationYears,
			durationDays: item.durationDays,
			startOffsetDays: getDayDifference(minDateValue, item.purchaseDateValue),
			startPercent: (getDayDifference(minDateValue, item.purchaseDateValue) / totalSpanDays) * 100,
			widthPercent: (item.durationDays / totalSpanDays) * 100
		}))
		.sort((left, right) => left.purchaseDate.localeCompare(right.purchaseDate) || left.deviceName.localeCompare(right.deviceName));
	const groupedEntries = entries.reduce<Map<string, TimelineEntryViewModel[]>>((groups, entry) => {
		const current = groups.get(entry.groupLabel) ?? [];
		current.push(entry);
		groups.set(entry.groupLabel, current);
		return groups;
	}, new Map());

	return {
		entries,
		groups: Array.from(groupedEntries.entries())
			.sort(([left], [right]) => left.localeCompare(right))
			.map(([label, groupEntries]) => ({ label, entries: groupEntries })),
		axisTicks: buildTimelineAxisTicks(minDateValue, maxDateValue, totalSpanDays),
		minDate: toDateOnly(minDateValue),
		maxDate: toDateOnly(maxDateValue),
		totalSpanDays,
		asOfDate: normalizedAsOfDate,
		groupBy: response.groupBy === 'Owner' ? 'Owner' : 'Category',
		appliedCategoryId:
			typeof response.appliedCategoryId === 'string' && response.appliedCategoryId.length > 0
				? response.appliedCategoryId
				: null
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

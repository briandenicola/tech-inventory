import { describe, expect, it } from 'vitest';
import {
	formatDateOnly,
	normalizeSummaryReport,
	normalizeWarrantyReport,
	sortWarrantyItems
} from './reports';

describe('reports utils', () => {
	it('normalizes both current and planned summary payload shapes', () => {
		const legacy = normalizeSummaryReport({
			totalActiveDeviceCount: 42,
			totalEstimatedValue: 15750,
			devicesByCategory: [{ label: 'Laptop', count: 12 }],
			devicesByLocation: [{ label: 'Office', count: 8 }],
			devicesByStatus: [{ label: 'Active', count: 38 }]
		});
		const planned = normalizeSummaryReport({
			totalDevices: 42,
			totalValue: 15750,
			byCategory: [{ name: 'Laptop', count: 12 }],
			byLocation: [{ name: 'Office', count: 8 }],
			byStatus: [{ status: 'Active', count: 38 }]
		});

		expect(legacy).toEqual(planned);
	});

	it('normalizes both current and planned warranty payload shapes', () => {
		const legacy = normalizeWarrantyReport({
			devices: [{
				deviceName: 'MacBook Pro',
				brand: 'Apple',
				purchaseDate: '2024-01-15',
				warrantyExpiry: '2026-07-15',
				daysRemaining: 55
			}],
			expiringWithinDays: 90
		});
		const planned = normalizeWarrantyReport({
			totalCount: 1,
			items: [{
				deviceId: '00000000-0000-4000-8000-000000000001',
				deviceName: 'MacBook Pro',
				brandName: 'Apple',
				warrantyExpiry: '2026-07-15',
				daysUntilExpiry: 55
			}]
		});

		expect(legacy.totalCount).toBe(1);
		expect(legacy.items[0]?.daysUntilExpiry).toBe(55);
		expect(planned.items[0]?.deviceId).toBe('00000000-0000-4000-8000-000000000001');
	});

	it('sorts warranty items by expiry date ascending by default', () => {
		const items = [
			{ deviceId: null, deviceName: 'Later', brandName: null, purchaseDate: null, warrantyExpiry: '2026-08-01', daysUntilExpiry: 70, tone: 'info' as const },
			{ deviceId: null, deviceName: 'Sooner', brandName: null, purchaseDate: null, warrantyExpiry: '2026-06-01', daysUntilExpiry: 9, tone: 'urgent' as const }
		];

		expect(sortWarrantyItems(items).map((item) => item.deviceName)).toEqual(['Sooner', 'Later']);
		expect(sortWarrantyItems(items, 'desc').map((item) => item.deviceName)).toEqual(['Later', 'Sooner']);
	});

	it('formats date-only strings without timezone drift', () => {
		expect(formatDateOnly('2026-07-15')).toBe('Jul 15, 2026');
	});
});

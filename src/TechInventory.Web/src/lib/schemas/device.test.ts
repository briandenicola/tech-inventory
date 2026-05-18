/**
 * Device Zod Schema Tests — Pure validation unit tests (T23)
 * 
 * Fast feedback layer: test schema validation without rendering components.
 * Mirrors backend FluentValidation constraints.
 * 
 * Constitution §3.5: Tests own their data
 */

import { describe, it, expect } from 'vitest';
import { deviceCreateSchema, deviceUpdateSchema } from './device';

describe('deviceCreateSchema', () => {
	describe('valid payloads', () => {
		it('accepts minimal valid payload (required fields only)', () => {
			const payload = {
				name: 'iPhone 15 Pro',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts full payload with all optional fields', () => {
			const payload = {
				name: 'MacBook Pro 16"',
				serialNumber: 'C02XJ0FMJGH5',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				ownerId: 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee',
				locationId: 'bbbbbbbb-cccc-4ddd-8eee-ffffffffffff',
				networkId: 'cccccccc-dddd-4eee-8fff-000000000000',
				purchaseDate: '2024-03-15',
				purchasePrice: 2499.99,
				currencyCode: 'USD',
				notes: 'Company laptop, issued 2024'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts empty strings for optional fields (form clearance)', () => {
			const payload = {
				name: 'Test Device',
				serialNumber: '',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				ownerId: '',
				locationId: '',
				networkId: '',
				purchaseDate: '',
				currencyCode: '',
				notes: ''
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts zero purchase price (free device)', () => {
			const payload = {
				name: 'Promotional Item',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchasePrice: 0
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts null for purchasePrice', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchasePrice: null
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('name field validation', () => {
		it('rejects empty name', () => {
			const payload = {
				name: '',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects name exceeding 200 characters', () => {
			const payload = {
				name: 'A'.repeat(201),
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('200 characters');
			}
		});

		it('accepts name at exactly 200 characters', () => {
			const payload = {
				name: 'A'.repeat(200),
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('serialNumber field validation', () => {
		it('rejects serial number exceeding 100 characters', () => {
			const payload = {
				name: 'Device',
				serialNumber: 'S'.repeat(101),
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('100 characters');
			}
		});

		it('accepts serial number at exactly 100 characters', () => {
			const payload = {
				name: 'Device',
				serialNumber: 'S'.repeat(100),
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('brandId and categoryId validation', () => {
		it('rejects missing brandId', () => {
			const payload = {
				name: 'Device',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects missing categoryId', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects non-UUID brandId', () => {
			const payload = {
				name: 'Device',
				brandId: 'not-a-uuid',
				categoryId: '87654321-4321-4321-8321-cba987654321'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});

		it('rejects non-UUID categoryId', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: 'not-a-uuid'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});
	});

	describe('optional UUID fields (ownerId, locationId, networkId)', () => {
		it('rejects non-UUID ownerId when provided', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				ownerId: 'not-a-uuid'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('accepts valid UUID ownerId', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				ownerId: 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('purchaseDate validation', () => {
		it('accepts valid YYYY-MM-DD date', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchaseDate: '2024-03-15'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects date in wrong format (MM/DD/YYYY)', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchaseDate: '03/15/2024'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('YYYY-MM-DD');
			}
		});

		it('rejects date in wrong format (DD-MM-YYYY)', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchaseDate: '15-03-2024'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects malformed date string', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchaseDate: 'not-a-date'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});
	});

	describe('purchasePrice validation', () => {
		it('rejects negative purchase price', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchasePrice: -10.50
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('0 or greater');
			}
		});

		it('accepts decimal purchase price', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				purchasePrice: 1299.99
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('currencyCode validation', () => {
		it('accepts 3-character currency code', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				currencyCode: 'EUR'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects currency code with wrong length', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				currencyCode: 'US'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('3 characters');
			}
		});
	});

	describe('notes validation', () => {
		it('accepts notes under 2000 characters', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				notes: 'A'.repeat(1999)
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts notes at exactly 2000 characters', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				notes: 'A'.repeat(2000)
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects notes exceeding 2000 characters', () => {
			const payload = {
				name: 'Device',
				brandId: '12345678-1234-4234-8234-123456789abc',
				categoryId: '87654321-4321-4321-8321-cba987654321',
				notes: 'A'.repeat(2001)
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				const notesError = result.error.issues.find((issue) => issue.path[0] === 'notes');
				expect(notesError).toBeDefined();
				expect(notesError?.message).toContain('2000 characters');
			}
		});
	});
});

describe('deviceUpdateSchema', () => {
	it('accepts same payload as create schema (for now, update = create)', () => {
		const payload = {
			name: 'Updated Device',
			brandId: '12345678-1234-4234-8234-123456789abc',
			categoryId: '87654321-4321-4321-8321-cba987654321',
			purchasePrice: 500,
			serialNumber: '',
			ownerId: '',
			locationId: '',
			networkId: '',
			purchaseDate: '',
			currencyCode: '',
			notes: ''
		};

		const result = deviceUpdateSchema.safeParse(payload);
		expect(result.success).toBe(true);
	});

	it('has same validation rules as create schema', () => {
		// Update schema should reject same invalid data as create
		const payload = {
			name: '',
			brandId: '12345678-1234-4234-8234-123456789abc',
			categoryId: '87654321-4321-4321-8321-cba987654321',
			serialNumber: '',
			ownerId: '',
			locationId: '',
			networkId: '',
			purchaseDate: '',
			currencyCode: '',
			notes: ''
		};

		const result = deviceUpdateSchema.safeParse(payload);
		expect(result.success).toBe(false);
	});
});

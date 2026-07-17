/**
 * Device Zod Schema Tests — Pure validation unit tests (T23)
 *
 * Fast feedback layer: test schema validation without rendering components.
 * Mirrors backend FluentValidation constraints (verified directly against
 * DeviceValidationRules.ApplyOptionalBrandRule / ApplyRequiredReferenceRules /
 * ApplyOptionalNetworkRule — brandId and networkId are optional, categoryId/
 * ownerId/locationId are required).
 *
 * Constitution §3.5: Tests own their data
 */

import { describe, it, expect } from 'vitest';
import { deviceCreateSchema, deviceUpdateSchema } from './device';

const validOwnerId = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
const validLocationId = 'bbbbbbbb-cccc-4ddd-8eee-ffffffffffff';
const validCategoryId = '87654321-4321-4321-8321-cba987654321';
const validBrandId = '12345678-1234-4234-8234-123456789abc';

describe('deviceCreateSchema', () => {
	describe('valid payloads', () => {
		it('accepts minimal valid payload (required fields only)', () => {
			const payload = {
				name: 'iPhone 15 Pro',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts full payload with all optional fields', () => {
			const payload = {
				name: 'MacBook Pro 16"',
				serialNumber: 'C02XJ0FMJGH5',
				brandId: validBrandId,
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				networkId: 'cccccccc-dddd-4eee-8fff-000000000000',
				purchaseDate: '2024-03-15',
				purchasePrice: 2499.99,
				currencyCode: 'USD',
				notes: 'Company laptop, issued 2024'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts empty strings for truly optional fields (form clearance)', () => {
			const payload = {
				name: 'Test Device',
				serialNumber: '',
				brandId: '',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				purchasePrice: 0
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts null for purchasePrice', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('brandId validation (optional per backend ApplyOptionalBrandRule)', () => {
		it('accepts missing brandId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts empty string brandId', () => {
			const payload = {
				name: 'Device',
				brandId: '',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects non-UUID brandId', () => {
			const payload = {
				name: 'Device',
				brandId: 'not-a-uuid',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('Invalid brand');
			}
		});
	});

	describe('categoryId validation (required)', () => {
		it('rejects missing categoryId', () => {
			const payload = {
				name: 'Device',
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects non-UUID categoryId', () => {
			const payload = {
				name: 'Device',
				categoryId: 'not-a-uuid',
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues[0].message).toContain('required');
			}
		});
	});

	describe('ownerId / locationId validation (required per backend ApplyRequiredReferenceRules)', () => {
		it('rejects missing ownerId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues.some((issue) => issue.path[0] === 'ownerId')).toBe(true);
			}
		});

		it('rejects empty string ownerId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: '',
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects non-UUID ownerId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: 'not-a-uuid',
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('accepts valid UUID ownerId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects missing locationId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
			if (!result.success) {
				expect(result.error.issues.some((issue) => issue.path[0] === 'locationId')).toBe(true);
			}
		});

		it('rejects empty string locationId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: ''
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});
	});

	describe('networkId validation (optional)', () => {
		it('rejects non-UUID networkId when provided', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				networkId: 'not-a-uuid'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('accepts missing networkId', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});
	});

	describe('purchaseDate validation', () => {
		it('accepts valid YYYY-MM-DD date', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				purchaseDate: '2024-03-15'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects date in wrong format (MM/DD/YYYY)', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				purchaseDate: '15-03-2024'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(false);
		});

		it('rejects malformed date string', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				currencyCode: 'EUR'
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects currency code with wrong length', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				notes: 'A'.repeat(1999)
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('accepts notes at exactly 2000 characters', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
				notes: 'A'.repeat(2000)
			};

			const result = deviceCreateSchema.safeParse(payload);
			expect(result.success).toBe(true);
		});

		it('rejects notes exceeding 2000 characters', () => {
			const payload = {
				name: 'Device',
				categoryId: validCategoryId,
				ownerId: validOwnerId,
				locationId: validLocationId,
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
			categoryId: validCategoryId,
			ownerId: validOwnerId,
			locationId: validLocationId,
			purchasePrice: 500,
			serialNumber: '',
			brandId: '',
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
			categoryId: validCategoryId,
			ownerId: validOwnerId,
			locationId: validLocationId,
			serialNumber: '',
			brandId: '',
			networkId: '',
			purchaseDate: '',
			currencyCode: '',
			notes: ''
		};

		const result = deviceUpdateSchema.safeParse(payload);
		expect(result.success).toBe(false);
	});
});

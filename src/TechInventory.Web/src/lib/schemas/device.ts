/**
 * Device Zod Schemas — Client-side validation mirroring backend FluentValidation
 * 
 * T20+T21: Form validation for create/edit. Zod errors mapped to per-field UI errors.
 * 
 * Backend constraints (from CreateDeviceCommand + UpdateDeviceCommand validators):
 * - name: required, max 200
 * - serial: optional, max 100
 * - brandId: required UUID
 * - categoryId: required UUID
 * - ownerId: optional UUID
 * - locationId: optional UUID
 * - networkId: optional UUID
 * - purchaseDate: optional ISO 8601 date
 * - purchasePrice: optional, >= 0
 * - currency: optional, 3-char ISO code (USD, EUR, etc.)
 * - notes: optional, max 2000
 * 
 * Related: specs/002-frontend-mvp/spec.md J5-J8, Constitution §4.3
 */

import { z } from 'zod';

/**
 * Device create schema (all fields except ID)
 */
export const deviceCreateSchema = z.object({
	name: z
		.string()
		.min(1, 'Device name is required')
		.max(200, 'Device name must be 200 characters or less'),
	serialNumber: z
		.string()
		.max(100, 'Serial number must be 100 characters or less')
		.optional()
		.or(z.literal('')),
	brandId: z.string().uuid('Brand is required'),
	categoryId: z.string().uuid('Category is required'),
	ownerId: z.string().uuid().optional().or(z.literal('')),
	locationId: z.string().uuid().optional().or(z.literal('')),
	networkId: z.string().uuid().optional().or(z.literal('')),
	purchaseDate: z
		.string()
		.regex(/^\d{4}-\d{2}-\d{2}$/, 'Purchase date must be in YYYY-MM-DD format')
		.optional()
		.or(z.literal('')),
	purchasePrice: z
		.number()
		.nonnegative('Purchase price must be 0 or greater')
		.optional()
		.or(z.literal(null)),
	currencyCode: z.string().length(3, 'Currency code must be 3 characters (e.g., USD)').optional().or(z.literal('')),
	notes: z.string().max(2000, 'Notes must be 2000 characters or less').optional().or(z.literal(''))
});

export type DeviceCreateInput = z.infer<typeof deviceCreateSchema>;

/**
 * Device update schema (same as create for now; retired-device logic is in UI, not schema)
 */
export const deviceUpdateSchema = deviceCreateSchema;

export type DeviceUpdateInput = z.infer<typeof deviceUpdateSchema>;

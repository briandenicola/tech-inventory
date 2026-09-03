/**
 * Device Zod Schemas — Client-side validation mirroring backend FluentValidation
 * 
 * T20+T21: Form validation for create/edit. Zod errors mapped to per-field UI errors.
 * 
 * Backend constraints (from CreateDeviceCommand + UpdateDeviceCommand validators,
 * via DeviceValidationRules.ApplyOptionalBrandRule / ApplyRequiredReferenceRules /
 * ApplyOptionalNetworkRule — verified against the live validators, not assumed):
 * - name: required, max 200
 * - model: optional, max 200 (F034)
 * - serial: optional, max 100
 * - brandId: optional UUID
 * - categoryId: required UUID
 * - ownerId: required UUID
 * - locationId: required UUID
 * - networkId: optional UUID
 * - purchaseDate: optional ISO 8601 date
 * - purchasePrice: optional, >= 0
 * - currency: optional, 3-char ISO code (USD, EUR, etc.)
 * - notes: optional, max 2000
 * - purpose: optional, max 500
 * - operatingSystem: optional, max 100
 * - ipAddress: optional, max 45
 * - macAddress: optional, max 17
 * - productUrl: optional, max 500
 * - version: optional, max 50
 * - status: DeviceStatus enum, defaults to Active (F-D133: edit form must
 *   always send the device's current status — the API's UpdateDeviceRequest
 *   defaults an omitted status to Active, which silently reactivated
 *   InRepair/Lent/Retired devices when the field was missing from payloads)
 * 
 * Related: specs/002-frontend-mvp/spec.md J5-J8, Constitution §4.3
 */

import { z } from 'zod';

/** Mirrors the generated `DeviceStatus` OpenAPI enum (components['schemas']['DeviceStatus']). */
export const deviceStatusValues = ['Active', 'Retired', 'Disposed', 'InRepair', 'Lent'] as const;

const deviceBaseSchema = z.object({
	name: z
		.string()
		.min(1, 'Device name is required')
		.max(200, 'Device name must be 200 characters or less'),
	model: z
		.string()
		.max(200, 'Model must be 200 characters or less')
		.optional()
		.or(z.literal('')),
	serialNumber: z
		.string()
		.max(100, 'Serial number must be 100 characters or less')
		.optional()
		.or(z.literal('')),
	brandId: z.string().uuid('Invalid brand ID').optional().or(z.literal('')),
	categoryId: z.string().uuid('Category is required'),
	ownerId: z.string().min(1, 'Owner is required').uuid('Invalid owner ID'),
	locationId: z.string().min(1, 'Location is required').uuid('Invalid location ID'),
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
	currencyCode: z
		.string()
		.length(3, 'Currency code must be 3 characters (e.g., USD)')
		.optional()
		.or(z.literal('')),
	notes: z.string().max(2000, 'Notes must be 2000 characters or less').optional().or(z.literal('')),
	purpose: z.string().max(500, 'Purpose must be 500 characters or less').optional().or(z.literal('')),
	operatingSystem: z
		.string()
		.max(100, 'Operating system must be 100 characters or less')
		.optional()
		.or(z.literal('')),
	ipAddress: z.string().max(45, 'IP address must be 45 characters or less').optional().or(z.literal('')),
	macAddress: z.string().max(17, 'MAC address must be 17 characters or less').optional().or(z.literal('')),
	productUrl: z.string().max(500, 'Product URL must be 500 characters or less').optional().or(z.literal('')),
	version: z.string().max(50, 'Version must be 50 characters or less').optional().or(z.literal('')),
	status: z.enum(deviceStatusValues).default('Active')
});

/**
 * Device create schema (matches API payload)
 */
export const deviceCreateSchema = deviceBaseSchema;
export type DeviceCreateInput = z.infer<typeof deviceCreateSchema>;

/**
 * Device form schema (includes client-side-only tag selection state)
 */
export const deviceFormSchema = deviceBaseSchema.extend({
	tagIds: z.array(z.string()).default([])
});
export type DeviceFormInput = z.infer<typeof deviceFormSchema>;

/**
 * Device update schema (same as create for now; retired-device logic is in UI, not schema)
 */
export const deviceUpdateSchema = deviceCreateSchema;
export type DeviceUpdateInput = z.infer<typeof deviceUpdateSchema>;

/**
 * Narrows a loosely-typed device status (e.g. `DeviceResponse.status`, which
 * is a plain nullable `string` mirror of the API's `DeviceStatus` enum) down
 * to a known `DeviceFormInput['status']` value, defaulting to `'Active'` for
 * `null`/unrecognized values. Used when pre-populating the edit form so an
 * unexpected value can't silently widen `formData.status` to `string`.
 */
export function toDeviceFormStatus(status: string | null | undefined): DeviceFormInput['status'] {
	return (deviceStatusValues as readonly string[]).includes(status ?? '')
		? (status as DeviceFormInput['status'])
		: 'Active';
}

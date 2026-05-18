/**
 * Zod Validation Schemas — Tech Inventory
 * 
 * Constitution §6.5.3: "Zod schemas for client validation (mirror server-side FluentValidation)"
 * Mirrors backend FluentValidation rules for runtime client-side validation
 */

import { z } from 'zod';

/**
 * Common validation rules
 */
const requiredString = z.string().trim().min(1, 'Required');
const requiredUrl = z.string().url('Invalid URL');

/**
 * Device schema
 */
export const deviceSchema = z.object({
	name: requiredString.max(200, 'Name must be 200 characters or fewer'),
	brandId: requiredString.uuid('Invalid brand'),
	serialNumber: z
		.string()
		.trim()
		.max(100, 'Serial number must be 100 characters or fewer')
		.optional(),
	assetTag: z
		.string()
		.trim()
		.max(50, 'Asset tag must be 50 characters or fewer')
		.optional(),
	categoryId: requiredString.uuid('Invalid category'),
	ownerId: z.string().uuid('Invalid owner').optional(),
	locationId: z.string().uuid('Invalid location').optional(),
	acquiredOn: z.coerce.date().optional(),
	purchasePrice: z
		.number()
		.positive('Purchase price must be positive')
		.optional(),
	currency: z
		.string()
		.length(3, 'Currency must be 3-character ISO code')
		.regex(/^[A-Z]{3}$/, 'Currency must be uppercase ISO 4217 code')
		.optional(),
	notes: z
		.string()
		.trim()
		.max(2000, 'Notes must be 2000 characters or fewer')
		.optional()
});

export type DeviceFormData = z.infer<typeof deviceSchema>;

/**
 * Brand schema
 */
export const brandSchema = z.object({
	name: requiredString.max(100, 'Name must be 100 characters or fewer'),
	website: requiredUrl.optional(),
	notes: z
		.string()
		.trim()
		.max(1000, 'Notes must be 1000 characters or fewer')
		.optional()
});

export type BrandFormData = z.infer<typeof brandSchema>;

/**
 * Category schema
 */
export const categorySchema = z.object({
	name: requiredString.max(100, 'Name must be 100 characters or fewer'),
	parentId: z.string().uuid('Invalid parent category').optional(),
	icon: z
		.string()
		.trim()
		.max(50, 'Icon must be 50 characters or fewer')
		.optional(),
	description: z
		.string()
		.trim()
		.max(500, 'Description must be 500 characters or fewer')
		.optional()
});

export type CategoryFormData = z.infer<typeof categorySchema>;

/**
 * Owner schema
 */
export const ownerSchema = z.object({
	name: requiredString.max(100, 'Name must be 100 characters or fewer'),
	email: z.string().email('Invalid email').optional(),
	role: z.enum(['Admin', 'Member', 'Viewer']),
	entraObjectId: z.string().uuid('Invalid Entra Object ID').optional()
});

export type OwnerFormData = z.infer<typeof ownerSchema>;

/**
 * Location schema
 */
export const locationSchema = z.object({
	name: requiredString.max(100, 'Name must be 100 characters or fewer'),
	type: z.enum(['Home', 'Storage', 'External']),
	description: z
		.string()
		.trim()
		.max(500, 'Description must be 500 characters or fewer')
		.optional()
});

export type LocationFormData = z.infer<typeof locationSchema>;

/**
 * Delete device schema (confirmation)
 */
export const deleteDeviceSchema = z.object({
	reason: requiredString.max(
		500,
		'Reason must be 500 characters or fewer'
	),
	confirmName: requiredString
});

export type DeleteDeviceFormData = z.infer<typeof deleteDeviceSchema>;

/**
 * Import preview schema
 */
export const importPreviewSchema = z.object({
	file: z
		.instanceof(File)
		.refine((f) => f.size <= 10 * 1024 * 1024, {
			message: 'Files must be 10MB or smaller'
		})
		.refine(
			(f) => f.type === 'text/csv' || f.name.endsWith('.csv'),
			{
				message: 'File must be a CSV'
			}
		)
});

export type ImportPreviewFormData = z.infer<typeof importPreviewSchema>;

/**
 * TODO: Additional schemas for:
 * - Models
 * - Networks
 * - Tags
 * - Manufacturers
 * These will be filled in during Round 2+ as per-page forms are built.
 */

/**
 * Brand validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - name: required, 1-100 chars
 * - website: optional URL
 * - notes: optional, 0-500 chars
 * 
 * Related: src/TechInventory.Application/Brands/Commands/CreateBrandCommand.cs
 */

import { z } from 'zod';

export const brandSchema = z.object({
	name: z.string().trim().min(1, 'Brand name is required').max(100, 'Brand name must be 100 characters or less'),
	website: z.string().trim().url('Please enter a valid URL').optional().or(z.literal('')),
	notes: z.string().trim().max(500, 'Notes must be 500 characters or less').optional().or(z.literal(''))
});

export type BrandFormData = z.infer<typeof brandSchema>;

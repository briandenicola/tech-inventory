/**
 * Category validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - name: required, 1-200 chars
 * - parentId: optional UUID (null = root category)
 * - icon: optional emoji string, 0-100 chars
 * - depth: required, 1-3 (computed from parent relationship)
 * 
 * Related: src/TechInventory.Application/Categories/Commands/CreateCategoryCommand.cs
 */

import { z } from 'zod';

export const categorySchema = z.object({
	name: z.string().trim().min(1, 'Category name is required').max(200, 'Category name must be 200 characters or less'),
	parentId: z.string().uuid('Invalid parent category ID').optional().or(z.literal('')),
	icon: z.string().trim().max(100, 'Icon must be 100 characters or less').optional().or(z.literal('')),
	depth: z.number().int().min(1).max(3)
});

export type CategoryFormData = z.infer<typeof categorySchema>;

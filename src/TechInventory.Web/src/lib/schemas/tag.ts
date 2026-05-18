/**
 * Tag validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - name: required, 1-50 chars
 * - color: hex color string (6-char hex)
 * 
 * Related: src/TechInventory.Application/Tags/Commands/CreateTagCommand.cs
 */

import { z } from 'zod';

export const tagSchema = z.object({
	name: z.string().trim().min(1, 'Tag name is required').max(50, 'Tag name must be 50 characters or less'),
	color: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Please select a valid color')
});

export type TagFormData = z.infer<typeof tagSchema>;

/**
 * Preset colors for tag picker (D-089: Tag color preset approach)
 * 8 brand-friendly colors with good contrast across light/dark modes
 */
export const TAG_PRESET_COLORS = [
	'#EF4444', // red
	'#F59E0B', // amber
	'#10B981', // emerald
	'#06B6D4', // cyan
	'#3B82F6', // blue
	'#8B5CF6', // violet
	'#EC4899', // pink
	'#6B7280'  // gray
] as const;

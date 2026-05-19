/**
 * Location validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - name: required, 1-100 chars
 * - type: enum (Home | Storage | External)
 * - notes: optional, 0-500 chars
 * 
 * Related: src/TechInventory.Application/Locations/Commands/CreateLocationCommand.cs
 */

import { z } from 'zod';

export const locationSchema = z.object({
	name: z.string().trim().min(1, 'Location name is required').max(100, 'Location name must be 100 characters or less'),
	type: z.enum(['Home', 'Storage', 'External']),
	notes: z.string().trim().max(500, 'Notes must be 500 characters or less').optional().or(z.literal(''))
});

export type LocationFormData = z.infer<typeof locationSchema>;

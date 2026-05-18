/**
 * Network validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - name: required, 1-100 chars
 * - description: optional, 0-500 chars
 * 
 * Related: src/TechInventory.Application/Networks/Commands/CreateNetworkCommand.cs
 */

import { z } from 'zod';

export const networkSchema = z.object({
	name: z.string().trim().min(1, 'Network name is required').max(100, 'Network name must be 100 characters or less'),
	description: z.string().trim().max(500, 'Description must be 500 characters or less').optional().or(z.literal(''))
});

export type NetworkFormData = z.infer<typeof networkSchema>;

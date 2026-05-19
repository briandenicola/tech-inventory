/**
 * Owner validation schema — mirrors FluentValidation rules from backend
 * 
 * Fields:
 * - displayName: required, 1-200 chars
 * - role: required, one of Admin/Member/Viewer
 * - entraObjectId: optional UUID (links to Microsoft Entra ID identity)
 * 
 * Related: src/TechInventory.Application/Owners/Commands/CreateOwnerCommand.cs
 */

import { z } from 'zod';

export const ownerSchema = z.object({
	displayName: z.string().trim().min(1, 'Display name is required').max(200, 'Display name must be 200 characters or less'),
	role: z.enum(['Admin', 'Member', 'Viewer'], { errorMap: () => ({ message: 'Role is required' }) }),
	entraObjectId: z.string().uuid('Invalid Entra Object ID').optional().or(z.literal(''))
});

export type OwnerFormData = z.infer<typeof ownerSchema>;

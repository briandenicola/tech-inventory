/**
 * Auth Store — Current User Context
 * 
 * Per T10: Svelte writable store containing authenticated user context.
 * Populated from /api/v1/owners/me after MSAL sign-in.
 * 
 * Related: specs/002-frontend-mvp/spec.md §4.3, §5, J1, J13
 */

import { writable } from 'svelte/store';
import { z } from 'zod';

/**
 * OwnerResponse schema (runtime validation mirror of OpenAPI)
 * Mirrors src/TechInventory.Application/Owners/OwnerResponse.cs.
 *
 * Notes:
 * - The API does not surface `email` (Owner domain entity has no email field).
 * - `entraObjectId` is `Guid?` server-side; .NET Guids are not RFC-strict UUID v4,
 *   so we validate as a plain string and let the backend remain authoritative on shape.
 */
const OwnerResponseSchema = z.object({
	id: z.string(),
	entraObjectId: z.string().nullable().optional(),
	displayName: z.string(),
	role: z.enum(['Admin', 'Member', 'Viewer']),
	isActive: z.boolean()
});

/**
 * Current user shape (extracted from OwnerResponse)
 */
export interface CurrentUser {
	id: string;
	entraObjectId: string | null;
	displayName: string;
	role: 'Admin' | 'Member' | 'Viewer';
}

/**
 * Auth state shape
 */
export interface AuthState {
	currentUser: CurrentUser | null;
	isAuthenticated: boolean;
	isLoading: boolean;
	error: string | null;
}

/**
 * Initial auth state (loading until we check MSAL + fetch /owners/me)
 */
const initialState: AuthState = {
	currentUser: null,
	isAuthenticated: false,
	isLoading: true,
	error: null
};

/**
 * Auth store (writable)
 */
export const authStore = writable<AuthState>(initialState);

/**
 * Fetch current user from /api/v1/owners/me
 * Call after MSAL sign-in succeeds; populates auth store
 * 
 * Per T10 DoD: If Bishop's T11 hasn't landed yet, gracefully handle 404
 */
export async function fetchCurrentUser(): Promise<void> {
	authStore.update((state) => ({ ...state, isLoading: true, error: null }));

	try {
		// Dynamic import to avoid circular dependency (client.ts imports auth helpers)
		const { owners } = await import('$lib/api/client');
		
		// T11 endpoint (Bishop's task — may 404 if not yet deployed)
		// Shape: GET /api/v1/owners/me → OwnerResponse
		const response = await owners.me();

		// Runtime validation (Zod schema)
		const parsed = OwnerResponseSchema.parse(response);

		// Extract CurrentUser shape
		const currentUser: CurrentUser = {
			id: parsed.id,
			entraObjectId: parsed.entraObjectId ?? null,
			displayName: parsed.displayName,
			role: parsed.role
		};

		authStore.set({
			currentUser,
			isAuthenticated: true,
			isLoading: false,
			error: null
		});
	} catch (error) {
		console.error('[auth] Failed to fetch current user:', error);

		// If 404 (Bishop's T11 not landed), set error but don't crash
		const errorMessage =
			error instanceof Error && error.message.includes('404')
				? 'User endpoint not available yet (T11 pending)'
				: 'Failed to load user profile';

		authStore.set({
			currentUser: null,
			isAuthenticated: false,
			isLoading: false,
			error: errorMessage
		});
	}
}

/**
 * Clear auth state (on logout)
 */
export function clearAuth(): void {
	authStore.set({
		currentUser: null,
		isAuthenticated: false,
		isLoading: false,
		error: null
	});
}

/**
 * F020 v1 — update the displayName on the current user in the store so the
 * header chip + any other live consumers reflect a self-service rename
 * without a hard reload. Call this AFTER a successful
 * `api.owners.updateMyProfile(...)` round-trip.
 */
export function updateCurrentUserDisplayName(displayName: string): void {
	authStore.update((state) =>
		state.currentUser
			? { ...state, currentUser: { ...state.currentUser, displayName } }
			: state
	);
}

/**
 * Auth Helper Module — MSAL.js wrappers for token acquisition + account management
 * 
 * Per T05: Infrastructure only — no login/logout UI (that's T09 in Round 2).
 * These helpers provide typed wrappers around MSAL.js for use in API client + route guards.
 * 
 * Related: specs/002-frontend-mvp/spec.md §4.1, docs/auth-design.md §3
 */

import { msalInstance, apiTokenRequest } from './msal';
import type { AccountInfo, AuthenticationResult, InteractionRequiredAuthError } from '@azure/msal-browser';

/**
 * Initialize MSAL instance (required in v3+ before any other MSAL call)
 * Call once at app bootstrap (e.g., in +layout.svelte onMount or +layout.ts load)
 */
export async function initializeMsal(): Promise<void> {
	await msalInstance.initialize();
}

/**
 * Handle redirect callback after Entra sign-in redirect
 * Call once at app bootstrap (after initializeMsal) to process auth code from redirect
 * Returns AuthenticationResult if redirect contained auth response, null otherwise
 */
export async function handleRedirectPromise(): Promise<AuthenticationResult | null> {
	return await msalInstance.handleRedirectPromise();
}

/**
 * Get the currently signed-in account (if any)
 * Returns null if no user is authenticated
 */
export function getActiveAccount(): AccountInfo | null {
	const accounts = msalInstance.getAllAccounts();
	if (accounts.length === 0) {
		return null;
	}
	
	// Return the first account (single-user household app; no account picker needed)
	// If MSAL has a previously set active account, prefer that
	return msalInstance.getActiveAccount() ?? accounts[0];
}

/**
 * Acquire access token for API calls
 * 
 * Flow:
 * 1. Try silent acquisition (uses cached token or refresh token)
 * 2. If InteractionRequiredAuthError, fall back to redirect (user will be redirected to Entra)
 * 
 * Returns access token string, or null if no active account (unauthenticated)
 * 
 * Per Constitution: Uses acquireTokenRedirect (NOT popup) — popups are blocked / poor UX
 */
export async function acquireApiToken(): Promise<string | null> {
	const account = getActiveAccount();
	
	if (!account) {
		// No active account — user not signed in
		// Round 2 route guard will redirect unauthenticated users to login before API calls
		console.warn('[auth] acquireApiToken called with no active account — user not authenticated');
		return null;
	}
	
	const request = {
		...apiTokenRequest,
		account
	};
	
	try {
		// Try silent token acquisition (cached or refresh token)
		const result = await msalInstance.acquireTokenSilent(request);
		return result.accessToken;
	} catch (error) {
		// If interaction required (token expired, consent needed, etc.), redirect to Entra
		if (isInteractionRequiredError(error)) {
			console.info('[auth] Silent token acquisition failed; redirecting to Entra for interactive auth');
			// This will redirect the user to Entra; they'll return via handleRedirectPromise
			// The calling code won't receive a response — redirect is a full navigation
			await msalInstance.acquireTokenRedirect(request);
			// Unreachable after redirect, but TypeScript needs a return
			return null;
		}
		
		// Other error (network failure, etc.) — rethrow
		console.error('[auth] Token acquisition failed:', error);
		throw error;
	}
}

/**
 * Type guard: check if error is InteractionRequiredAuthError
 * MSAL.js throws this when silent token acquisition requires user interaction
 */
function isInteractionRequiredError(error: unknown): error is InteractionRequiredAuthError {
	if (typeof error === 'object' && error !== null && 'errorCode' in error) {
		const errorCode = (error as { errorCode: string }).errorCode;
		// MSAL v3 error codes that indicate interaction required
		return (
			errorCode === 'interaction_required' ||
			errorCode === 'consent_required' ||
			errorCode === 'login_required'
		);
	}
	return false;
}

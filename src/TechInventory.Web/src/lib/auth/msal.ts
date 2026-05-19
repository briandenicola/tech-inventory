/**
 * MSAL.js Configuration for Microsoft Entra ID (Workforce Tenant)
 * 
 * Per D-039 (Decision 5): Tenant/Client IDs are committed inline (public values, not secrets).
 * Cache location: sessionStorage per Constitution §7 (NEVER localStorage).
 * 
 * Related: specs/002-frontend-mvp/spec.md §4.1, docs/auth-design.md §2
 */

import { PublicClientApplication, BrowserCacheLocation } from '@azure/msal-browser';
import type { Configuration, RedirectRequest } from '@azure/msal-browser';

/**
 * Entra App Registration values (household Workforce tenant)
 * Per D-039: these are public values visible in OAuth redirects/JWTs — NOT secrets
 */
const ENTRA_TENANT_ID = 'b2108b29-ea40-4fee-b229-e3100835667e';
const ENTRA_CLIENT_ID = '60341158-b5af-4216-8140-a4c321f1e79c';
const ENTRA_AUTHORITY = `https://login.microsoftonline.com/${ENTRA_TENANT_ID}`;
const API_SCOPE = 'api://60341158-b5af-4216-8140-a4c321f1e79c/access_as_user';

/**
 * MSAL configuration (PublicClientApplication)
 * - redirectUri: derived from window.location.origin (dev=http://localhost:5173, prod=deployed URL)
 * - Cache: sessionStorage only (Constitution §7; ESLint gate enforces)
 * - PKCE: handled automatically by MSAL.js v3+ on redirect flows
 */
export const msalConfig: Configuration = {
	auth: {
		clientId: ENTRA_CLIENT_ID,
		authority: ENTRA_AUTHORITY,
		redirectUri: typeof window !== 'undefined' ? window.location.origin : undefined,
		postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : undefined,
		knownAuthorities: ['login.microsoftonline.com']
	},
	cache: {
		cacheLocation: BrowserCacheLocation.SessionStorage, // NEVER localStorage
		storeAuthStateInCookie: false // Modern browsers handle PKCE redirects; no cookie fallback needed
	}
};

/**
 * Login request scopes (openid, profile, API scope)
 * Used for full sign-in flow (loginRedirect)
 */
export const loginRequest: RedirectRequest = {
	scopes: [API_SCOPE, 'openid', 'profile']
};

/**
 * API token request scopes (API scope only)
 * Used for silent token re-acquisition before API calls
 */
export const apiTokenRequest = {
	scopes: [API_SCOPE]
};

/**
 * MSAL PublicClientApplication instance
 * Singleton — call ensureMsalInitialized() before any other MSAL API call.
 */
export const msalInstance = new PublicClientApplication(msalConfig);

/**
 * Idempotent initialization gate.
 *
 * MSAL v3+ throws `uninitialized_public_client_application` if any other API is
 * called before `initialize()` completes. Several call sites (root +layout
 * onMount, the API client's auth-token provider invoked from child $effect
 * blocks, etc.) can race the first MSAL call. This helper caches the
 * init promise so every caller awaits the same single initialization.
 */
let initPromise: Promise<void> | null = null;
export function ensureMsalInitialized(): Promise<void> {
	if (initPromise === null) {
		initPromise = msalInstance.initialize();
	}
	return initPromise;
}

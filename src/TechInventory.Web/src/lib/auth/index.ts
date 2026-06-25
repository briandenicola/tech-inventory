/**
 * Auth Helper Module — MSAL.js wrappers for token acquisition + account management
 *
 * Per T05: Infrastructure only — no login/logout UI (that's T09 in Round 2).
 * These helpers provide typed wrappers around MSAL.js for use in API client + route guards.
 *
 * Related: specs/002-frontend-mvp/spec.md §4.1, docs/auth-design.md §3
 */

import { msalInstance, apiTokenRequest, ensureMsalInitialized, loginRequest } from './msal';
import type {
	AccountInfo,
	AuthenticationResult,
	InteractionRequiredAuthError
} from '@azure/msal-browser';

const SILENT_SSO_SUPPRESS_KEY = 'ti_silent_sso_suppressed';

export interface TryAcquireApiTokenSilentOptions {
	timeoutMs?: number;
}

export function suppressSilentSso(): void {
	if (typeof sessionStorage === 'undefined') return;
	sessionStorage.setItem(SILENT_SSO_SUPPRESS_KEY, 'true');
}

export function clearSilentSsoSuppression(): void {
	if (typeof sessionStorage === 'undefined') return;
	sessionStorage.removeItem(SILENT_SSO_SUPPRESS_KEY);
}

function isSilentSsoSuppressed(): boolean {
	if (typeof sessionStorage === 'undefined') return false;
	return sessionStorage.getItem(SILENT_SSO_SUPPRESS_KEY) === 'true';
}

export function shouldAutoStartInteractiveSignIn(): boolean {
	return !isSilentSsoSuppressed() && msalInstance.getAllAccounts().length > 0;
}

async function withTimeout<T>(promise: Promise<T>, timeoutMs?: number): Promise<T> {
	if (!timeoutMs || timeoutMs <= 0) {
		return await promise;
	}

	return await new Promise<T>((resolve, reject) => {
		const timeoutId = setTimeout(() => {
			reject(new Error('Silent auth attempt timed out'));
		}, timeoutMs);

		void promise.then(
			(value) => {
				clearTimeout(timeoutId);
				resolve(value);
			},
			(error: unknown) => {
				clearTimeout(timeoutId);
				reject(error);
			}
		);
	});
}

/**
 * Initialize MSAL instance (required in v3+ before any other MSAL call).
 *
 * Delegates to the shared idempotent init promise so concurrent callers all
 * await the same initialization. Safe to call from root +layout onMount even
 * if a child component's $effect has already triggered an API call that
 * awaits the same promise via acquireApiToken().
 */
export async function initializeMsal(): Promise<void> {
	await ensureMsalInitialized();
}

/**
 * Handle redirect callback after Entra sign-in redirect
 * Call once at app bootstrap (after initializeMsal) to process auth code from redirect
 * Returns AuthenticationResult if redirect contained auth response, null otherwise
 */
export async function handleRedirectPromise(): Promise<AuthenticationResult | null> {
	await ensureMsalInitialized();
	const result = await msalInstance.handleRedirectPromise();
	if (result?.account) {
		msalInstance.setActiveAccount(result.account);
		clearSilentSsoSuppression();
	}
	return result;
}

/**
 * Get the currently signed-in account (if any)
 * Returns null if no user is authenticated
 *
 * Caller must ensure MSAL is initialized first (see ensureMsalInitialized).
 */
export function getActiveAccount(): AccountInfo | null {
	const accounts = msalInstance.getAllAccounts();
	if (accounts.length === 0) {
		return null;
	}

	const activeAccount = msalInstance.getActiveAccount();
	if (activeAccount) {
		return activeAccount;
	}

	const fallbackAccount = accounts[0];
	msalInstance.setActiveAccount(fallbackAccount);
	return fallbackAccount;
}

/**
 * Acquire access token for API calls
 *
 * Flow:
 * 1. Await MSAL initialization (idempotent) so callers from $effect blocks
 *    racing the root layout's bootstrap don't hit
 *    uninitialized_public_client_application.
 * 2. Try silent acquisition (uses cached token or refresh token)
 * 3. If InteractionRequiredAuthError, fall back to redirect (user will be redirected to Entra)
 *
 * Returns access token string, or null if no active account (unauthenticated)
 *
 * Per Constitution: Uses acquireTokenRedirect (NOT popup) — popups are blocked / poor UX
 */
export async function acquireApiToken(): Promise<string | null> {
	await ensureMsalInitialized();
	const result = await tryAcquireApiTokenSilent();

	if (result) {
		return result.accessToken;
	}

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

	console.info('[auth] Silent token acquisition failed; redirecting to Entra for interactive auth');
	clearSilentSsoSuppression();
	// This will redirect the user to Entra; they'll return via handleRedirectPromise
	// The calling code won't receive a response — redirect is a full navigation
	await msalInstance.acquireTokenRedirect(request);
	// Unreachable after redirect, but TypeScript needs a return
	return null;
}

export async function tryAcquireApiTokenSilent(
	options: TryAcquireApiTokenSilentOptions = {}
): Promise<AuthenticationResult | null> {
	await ensureMsalInitialized();
	if (isSilentSsoSuppressed()) {
		return null;
	}

	const account = getActiveAccount();

	try {
		const result = account
			? await withTimeout(
					msalInstance.acquireTokenSilent({
						...apiTokenRequest,
						account
					}),
					options.timeoutMs
			  )
			: await withTimeout(msalInstance.ssoSilent(loginRequest), options.timeoutMs);
		const resolvedAccount = result.account ?? account;
		if (resolvedAccount) {
			msalInstance.setActiveAccount(resolvedAccount);
		}
		clearSilentSsoSuppression();
		return result;
	} catch (error) {
		if (isInteractionRequiredError(error)) {
			return null;
		}

		console.error('[auth] Token acquisition failed:', error);
		throw error;
	}
}

/**
 * Type guard: check if error is InteractionRequiredAuthError
 * MSAL.js throws this when silent token acquisition requires user interaction
 */
export function isInteractionRequiredError(error: unknown): error is InteractionRequiredAuthError {
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

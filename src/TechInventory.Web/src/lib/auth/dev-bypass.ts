/**
 * Dev-Bypass Auth Mode (mirrors backend Auth:DevBypass)
 *
 * Activated by build-time env `VITE_AUTH_DEV_BYPASS=true`. As an additional
 * safety guard the runtime check refuses to activate unless the document is
 * being served from a loopback host (localhost / 127.0.0.1 / [::1]). This
 * makes accidental enablement on a deployed origin a no-op.
 *
 * When bypass is on:
 * - MSAL is never initialized.
 * - `getActiveAccount()` returns a fake AccountInfo with the same subject
 *   (`11111111-1111-1111-1111-111111111111`) the backend's
 *   DevBypassAuthenticationHandler emits.
 * - `acquireApiToken()` returns null (no Authorization header). The backend
 *   DevBypass handler authenticates the request as Admin.
 *
 * E2E + local dev consume this. Production builds without the env var act
 * exactly as before.
 */

import type { AccountInfo } from '@azure/msal-browser';

const LOOPBACK_HOSTS = new Set(['localhost', '127.0.0.1', '::1']);

const DEV_BYPASS_SUBJECT = '11111111-1111-1111-1111-111111111111';

let warned = false;

export function isAuthDevBypass(): boolean {
	if (import.meta.env.VITE_AUTH_DEV_BYPASS !== 'true') return false;
	if (typeof window === 'undefined') return false;
	if (!LOOPBACK_HOSTS.has(window.location.hostname)) return false;

	if (!warned) {
		warned = true;
		console.warn(
			'[auth] DEV BYPASS ACTIVE — MSAL is disabled; backend DevBypass authenticates as Admin.'
		);
	}
	return true;
}

export function devBypassAccount(): AccountInfo {
	return {
		homeAccountId: `${DEV_BYPASS_SUBJECT}.dev-bypass`,
		environment: 'dev-bypass',
		tenantId: 'dev-bypass',
		username: 'dev-bypass@localhost',
		localAccountId: DEV_BYPASS_SUBJECT,
		name: 'Dev Bypass Admin'
	};
}

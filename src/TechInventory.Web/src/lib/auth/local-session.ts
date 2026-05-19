/**
 * F025 — local-account fallback authentication helpers.
 *
 * Companion to MSAL-based Entra sign-in. Local sessions live in
 * `sessionStorage` (per Constitution §6: never localStorage) and carry a
 * short-lived HS256 JWT issued by the API.
 *
 * The API client's `getAuthToken` callback prefers a local token when one is
 * present; otherwise it falls back to MSAL.
 */

const STORAGE_KEY_TOKEN = 'ti_local_token';
const STORAGE_KEY_META = 'ti_local_meta';

export interface LocalSessionMeta {
	subjectId: string;
	displayName: string;
	role: 'Admin' | 'Member' | 'Viewer';
	mustChangePassword: boolean;
	expiresAtUtc: number;
}

interface JwtPayload {
	sub?: string;
	name?: string;
	role?: string;
	auth_method?: string;
	must_change_password?: string;
	exp?: number;
}

export function getLocalToken(): string | null {
	if (typeof sessionStorage === 'undefined') return null;
	const raw = sessionStorage.getItem(STORAGE_KEY_TOKEN);
	if (!raw) return null;

	// If the token is past its lifetime, drop it so the next call falls back
	// to MSAL (if applicable) or surfaces the unauthenticated state.
	const meta = getLocalSessionMeta();
	if (meta && meta.expiresAtUtc * 1000 < Date.now()) {
		clearLocalSession();
		return null;
	}

	return raw;
}

export function getLocalSessionMeta(): LocalSessionMeta | null {
	if (typeof sessionStorage === 'undefined') return null;
	const raw = sessionStorage.getItem(STORAGE_KEY_META);
	if (!raw) return null;
	try {
		return JSON.parse(raw) as LocalSessionMeta;
	} catch {
		return null;
	}
}

export function setLocalSession(token: string): LocalSessionMeta {
	const payload = decodeJwtPayload(token);
	const meta: LocalSessionMeta = {
		subjectId: payload.sub ?? '',
		displayName: payload.name ?? 'Local user',
		role: (payload.role as LocalSessionMeta['role']) ?? 'Viewer',
		mustChangePassword: payload.must_change_password === 'true',
		expiresAtUtc: payload.exp ?? Math.floor(Date.now() / 1000) + 3600
	};

	sessionStorage.setItem(STORAGE_KEY_TOKEN, token);
	sessionStorage.setItem(STORAGE_KEY_META, JSON.stringify(meta));
	return meta;
}

export function clearLocalSession(): void {
	if (typeof sessionStorage === 'undefined') return;
	sessionStorage.removeItem(STORAGE_KEY_TOKEN);
	sessionStorage.removeItem(STORAGE_KEY_META);
}

export function isLocalAuth(): boolean {
	return getLocalToken() !== null;
}

function decodeJwtPayload(token: string): JwtPayload {
	const parts = token.split('.');
	if (parts.length !== 3) {
		return {};
	}
	try {
		// JWT payloads are base64url-encoded — pad + swap chars before atob.
		// Browser-only path: we never need a Buffer fallback (the auth store is
		// SSR-skipped in +layout.svelte) and avoiding it keeps Node @types off
		// the web bundle.
		const padded = parts[1].replace(/-/g, '+').replace(/_/g, '/');
		const padding = padded.length % 4 === 0 ? '' : '='.repeat(4 - (padded.length % 4));
		const json = atob(padded + padding);
		return JSON.parse(json) as JwtPayload;
	} catch {
		return {};
	}
}

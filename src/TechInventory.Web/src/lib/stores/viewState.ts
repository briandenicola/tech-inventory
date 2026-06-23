/**
 * View State Persistence (session-scoped, sessionStorage)
 *
 * Persists transient navigation state within the current browser session.
 * Use for preserving filters/sorts/search when navigating to detail/edit/create
 * and returning to the list. Different from userPrefs.devicesDefaultView which
 * is a long-term saved preference across sessions.
 *
 * Session storage is cleared when the tab closes, ensuring this is truly
 * navigation continuity rather than a persistent preference.
 *
 * SSR-safe: returns null when `window` is undefined.
 */

const SESSION_KEY_DEVICES_VIEW = 'ti.viewState.devices';
const TRANSIENT_DEVICES_PARAMS = new Set(['device']);

function isBrowser(): boolean {
	return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined';
}

/**
 * Get the persisted devices list view state from the current session.
 * Returns null if not set or on SSR.
 */
export function normalizeDevicesViewState(searchString: string | null | undefined): string | null {
	if (!searchString) return null;

	const trimmed = searchString.startsWith('?') ? searchString.slice(1) : searchString;
	if (trimmed.length === 0) return null;

	const params = new URLSearchParams(trimmed);
	for (const key of TRANSIENT_DEVICES_PARAMS) {
		params.delete(key);
	}

	const entries = Array.from(params.entries());
	if (entries.length === 0) return null;

	const normalized = new URLSearchParams();
	for (const [key, value] of entries) {
		normalized.append(key, value);
	}

	return normalized.toString();
}

export function getDevicesViewState(): string | null {
	if (!isBrowser()) return null;
	return normalizeDevicesViewState(window.sessionStorage.getItem(SESSION_KEY_DEVICES_VIEW));
}

/**
 * Set the devices list view state in session storage.
 * Pass the full search string from the /devices URL (e.g., "?category=xyz&search=foo").
 */
export function setDevicesViewState(searchString: string | null): void {
	if (!isBrowser()) return;
	const normalized = normalizeDevicesViewState(searchString);
	if (normalized) {
		window.sessionStorage.setItem(SESSION_KEY_DEVICES_VIEW, normalized);
	} else {
		window.sessionStorage.removeItem(SESSION_KEY_DEVICES_VIEW);
	}
}

/**
 * Clear the devices list view state from session storage.
 */
export function clearDevicesViewState(): void {
	if (!isBrowser()) return;
	window.sessionStorage.removeItem(SESSION_KEY_DEVICES_VIEW);
}

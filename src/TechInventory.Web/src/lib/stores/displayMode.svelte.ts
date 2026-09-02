/**
 * displayMode.svelte.ts — F045 D-175: the three presentation modes.
 *
 * Wraps the existing `isStandalonePwa()` primitive (owned by `$lib/auth`) in a
 * reactive store so components can react to display-mode changes without
 * duplicating detection logic. Viewport width only splits `mobile` from
 * `desktop` — app-mode ("pwa") chrome is gated on `isStandalonePwa()` alone,
 * per D-175, so a narrow desktop browser window never sprouts app chrome.
 *
 * SSR / pre-mount always resolves to `desktop` with `isMounted: false` to
 * avoid a hydration mismatch (no nav-bar flash).
 */
import { isStandalonePwa } from '$lib/auth';

export type DisplayMode = 'pwa' | 'mobile' | 'desktop';

export interface DisplayModeOptions {
	/** Inject a fixed standalone-PWA value for tests instead of detecting it. */
	standalone?: boolean;
	/** Inject a fixed "viewport >= 768px" value for tests instead of detecting it. */
	wide?: boolean;
}

export interface DisplayModeHandle {
	readonly mode: DisplayMode;
	readonly isPwa: boolean;
	readonly isMounted: boolean;
	/** Removes the underlying matchMedia listeners. Call from test teardown. */
	destroy(): void;
}

const STANDALONE_QUERY = '(display-mode: standalone)';
const WIDE_QUERY = '(min-width: 768px)';

export function createDisplayMode(options: DisplayModeOptions = {}): DisplayModeHandle {
	const hasMatchMedia = typeof window !== 'undefined' && typeof window.matchMedia === 'function';

	const state = $state({
		standalone: options.standalone ?? false,
		wide: options.wide ?? true,
		mounted: false
	});

	let standaloneMedia: MediaQueryList | undefined;
	let wideMedia: MediaQueryList | undefined;
	let onStandaloneChange: ((event: MediaQueryListEvent) => void) | undefined;
	let onWideChange: ((event: MediaQueryListEvent) => void) | undefined;

	if (hasMatchMedia) {
		state.standalone = options.standalone ?? isStandalonePwa();
		state.wide = options.wide ?? window.matchMedia(WIDE_QUERY).matches;
		state.mounted = true;

		standaloneMedia = window.matchMedia(STANDALONE_QUERY);
		onStandaloneChange = (event: MediaQueryListEvent) => {
			state.standalone = event.matches;
		};
		standaloneMedia.addEventListener('change', onStandaloneChange);

		wideMedia = window.matchMedia(WIDE_QUERY);
		onWideChange = (event: MediaQueryListEvent) => {
			state.wide = event.matches;
		};
		wideMedia.addEventListener('change', onWideChange);
	}

	return {
		get mode(): DisplayMode {
			if (!state.mounted) return 'desktop';
			if (state.standalone) return 'pwa';
			return state.wide ? 'desktop' : 'mobile';
		},
		get isPwa(): boolean {
			return state.mounted && state.standalone;
		},
		get isMounted(): boolean {
			return state.mounted;
		},
		destroy(): void {
			if (standaloneMedia && onStandaloneChange) {
				standaloneMedia.removeEventListener('change', onStandaloneChange);
			}
			if (wideMedia && onWideChange) {
				wideMedia.removeEventListener('change', onWideChange);
			}
		}
	};
}

/** App-lifetime singleton — the one every component should import. */
export const displayMode = createDisplayMode();

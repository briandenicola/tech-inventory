const STORAGE_KEY = 'theme-preference';
const SYSTEM_THEME_QUERY = '(prefers-color-scheme: dark)';

export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

const VALID_PREFERENCES = ['light', 'dark', 'system'] as const satisfies readonly ThemePreference[];

const state = $state({
	preference: 'system' as ThemePreference,
	resolvedTheme: 'light' as ResolvedTheme,
	initialized: false
});

let mediaQueryList: MediaQueryList | null = null;
let mediaQueryListener: ((event: MediaQueryListEvent) => void) | null = null;

function isBrowser(): boolean {
	return typeof window !== 'undefined' && typeof document !== 'undefined';
}

function isThemePreference(value: string | null): value is ThemePreference {
	return value !== null && VALID_PREFERENCES.includes(value as ThemePreference);
}

function getSystemTheme(): ResolvedTheme {
	if (!isBrowser()) {
		return 'light';
	}

	return window.matchMedia(SYSTEM_THEME_QUERY).matches ? 'dark' : 'light';
}

function resolveTheme(preference: ThemePreference): ResolvedTheme {
	return preference === 'system' ? getSystemTheme() : preference;
}

function readStoredPreference(): ThemePreference {
	if (!isBrowser()) {
		return 'system';
	}

	const stored = window.localStorage.getItem(STORAGE_KEY);
	return isThemePreference(stored) ? stored : 'system';
}

function persistPreference(preference: ThemePreference): void {
	if (!isBrowser()) {
		return;
	}

	window.localStorage.setItem(STORAGE_KEY, preference);
}

function applyResolvedTheme(resolvedTheme: ResolvedTheme): void {
	if (!isBrowser()) {
		return;
	}

	state.resolvedTheme = resolvedTheme;
	document.documentElement.dataset.theme = resolvedTheme;
	document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
}

function removeSystemListener(): void {
	if (mediaQueryList && mediaQueryListener) {
		mediaQueryList.removeEventListener('change', mediaQueryListener);
	}

	mediaQueryList = null;
	mediaQueryListener = null;
}

function ensureSystemListener(): void {
	if (!isBrowser() || mediaQueryListener) {
		return;
	}

	mediaQueryList = window.matchMedia(SYSTEM_THEME_QUERY);
	mediaQueryListener = () => {
		if (state.preference === 'system') {
			applyResolvedTheme(resolveTheme('system'));
		}
	};
	mediaQueryList.addEventListener('change', mediaQueryListener);
}

function syncTheme(): void {
	applyResolvedTheme(resolveTheme(state.preference));
}

export const themeStore = {
	get preference(): ThemePreference {
		return state.preference;
	},
	get resolvedTheme(): ResolvedTheme {
		return state.resolvedTheme;
	}
};

export function initializeTheme(): void {
	if (!isBrowser()) {
		return;
	}

	if (!state.initialized) {
		state.preference = readStoredPreference();
		ensureSystemListener();
		state.initialized = true;
	}

	syncTheme();
}

export function setThemePreference(preference: ThemePreference): void {
	state.preference = preference;
	persistPreference(preference);
	initializeTheme();
}

export function resetThemeStoreForTests(): void {
	removeSystemListener();
	state.preference = 'system';
	state.resolvedTheme = 'light';
	state.initialized = false;

	if (isBrowser()) {
		document.documentElement.classList.remove('dark');
		delete document.documentElement.dataset.theme;
	}
}

export { STORAGE_KEY as THEME_STORAGE_KEY };

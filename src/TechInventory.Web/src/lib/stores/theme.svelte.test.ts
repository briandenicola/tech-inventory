import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
	initializeTheme,
	resetThemeStoreForTests,
	setThemePreference,
	themeStore,
	THEME_STORAGE_KEY
} from './theme.svelte';

let systemPrefersDark = false;
let changeListener: ((event: MediaQueryListEvent) => void) | null = null;

function emitSystemThemeChange(matches: boolean) {
	systemPrefersDark = matches;
	changeListener?.({ matches } as MediaQueryListEvent);
}

describe('themeStore', () => {
	beforeEach(() => {
		systemPrefersDark = false;
		changeListener = null;
		window.localStorage.clear();
		resetThemeStoreForTests();

		Object.defineProperty(window, 'matchMedia', {
			writable: true,
			value: vi.fn().mockImplementation(() => ({
				get matches() {
					return systemPrefersDark;
				},
				media: '(prefers-color-scheme: dark)',
				addEventListener: (_event: string, listener: (event: MediaQueryListEvent) => void) => {
					changeListener = listener;
				},
				removeEventListener: () => {
					changeListener = null;
				}
			}))
		});
	});

	it('defaults to the current system theme when no preference is stored', () => {
		initializeTheme();

		expect(themeStore.preference).toBe('system');
		expect(themeStore.resolvedTheme).toBe('light');
		expect(document.documentElement.dataset.theme).toBe('light');
		expect(document.documentElement.classList.contains('dark')).toBe(false);
	});

	it('persists explicit dark mode and applies the dark class', () => {
		setThemePreference('dark');

		expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
		expect(themeStore.preference).toBe('dark');
		expect(themeStore.resolvedTheme).toBe('dark');
		expect(document.documentElement.dataset.theme).toBe('dark');
		expect(document.documentElement.classList.contains('dark')).toBe(true);
	});

	it('tracks system changes while the preference is set to system', () => {
		setThemePreference('system');
		emitSystemThemeChange(true);

		expect(themeStore.preference).toBe('system');
		expect(themeStore.resolvedTheme).toBe('dark');
		expect(document.documentElement.dataset.theme).toBe('dark');
		expect(document.documentElement.classList.contains('dark')).toBe(true);
	});
});

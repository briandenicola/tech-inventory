/**
 * app.html pre-hydration theme contract — C-21.
 *
 * `stores/theme.svelte.test.ts` covers `resolvePreference`/persistence but
 * (per its own file header) is drifted from the actual inline script: it
 * seeds `ti.userPrefs.v1.<id>`, while `app.html` reads `theme-preference`.
 * This test asserts the *inline* pre-hydration script directly (by reading
 * the static file — jsdom cannot execute a real first paint) and cross-
 * checks its storage key against `THEME_STORAGE_KEY`, the constant
 * `theme.svelte.ts` uses post-hydration, so the two can never silently
 * drift apart again.
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { THEME_STORAGE_KEY } from '$lib/stores/theme.svelte';

const appHtml = readFileSync(resolve(process.cwd(), 'src/app.html'), 'utf8');

function getPreHydrationScript(): string {
	const start = appHtml.indexOf('<script>');
	const end = appHtml.indexOf('</script>', start);
	if (start === -1 || end === -1) {
		throw new Error('Could not find the pre-hydration theme <script> in app.html');
	}
	return appHtml.slice(start, end);
}

describe('app.html pre-hydration theme contract (C-21)', () => {
	it('reads the same storage key theme.svelte.ts persists to post-hydration', () => {
		const script = getPreHydrationScript();
		expect(script).toContain(`localStorage.getItem('${THEME_STORAGE_KEY}')`);
	});

	it('sets data-theme and toggles the dark class before first paint', () => {
		const script = getPreHydrationScript();
		expect(script).toContain('document.documentElement.dataset.theme = resolvedTheme');
		expect(script).toContain("document.documentElement.classList.toggle('dark', resolvedTheme === 'dark')");
	});

	it('resolves the stored preference against prefers-color-scheme, defaulting to system', () => {
		const script = getPreHydrationScript();
		// preference === 'dark' -> dark; preference === 'light' -> light;
		// anything else (including 'system'/missing) follows the media query.
		expect(script).toContain("localStorage.getItem('theme-preference') || 'system'");
		expect(script).toContain("window.matchMedia('(prefers-color-scheme: dark)').matches");
		expect(script).toContain(
			"preference === 'dark' || (preference !== 'light' && prefersDark) ? 'dark' : 'light'"
		);
	});

	it('runs inside a try/catch so a blocked localStorage/matchMedia call never breaks first paint', () => {
		const script = getPreHydrationScript();
		expect(script).toMatch(/try\s*\{[\s\S]*\}\s*catch\s*\{\}/);
	});
});

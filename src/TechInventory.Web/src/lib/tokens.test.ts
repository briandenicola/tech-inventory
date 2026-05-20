import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const tokensCss = readFileSync(resolve(process.cwd(), 'src/lib/tokens.css'), 'utf8');

function getThemeInlineBlock(): string {
	const start = tokensCss.indexOf('@theme inline {');
	const end = tokensCss.indexOf('}', start);

	if (start === -1 || end === -1) {
		throw new Error('Could not find @theme inline block in tokens.css');
	}

	return tokensCss.slice(start, end);
}

describe('Design tokens', () => {
	it('registers the dark modal alert 950 shades with Tailwind', () => {
		const themeInlineBlock = getThemeInlineBlock();

		for (const colorName of ['primary', 'success', 'warning', 'danger', 'info']) {
			expect(themeInlineBlock).toContain(
				`--color-${colorName}-950: var(--app-color-${colorName}-950);`
			);
		}
	});

	it('defines 950 shades for light and dark palettes', () => {
		for (const colorName of ['primary', 'success', 'warning', 'danger', 'info']) {
			const matches = tokensCss.match(new RegExp(`--app-color-${colorName}-950:`, 'g')) ?? [];
			expect(matches).toHaveLength(3);
		}
	});
});

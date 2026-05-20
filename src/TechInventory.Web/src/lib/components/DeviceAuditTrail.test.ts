import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import DeviceAuditTrail from './DeviceAuditTrail.svelte';

const tokensCss = readFileSync(resolve(process.cwd(), 'src/lib/tokens.css'), 'utf8');

function getTokenBlock(startMarker: string, endMarker?: string, fromIndex = 0): string {
	const startIndex = tokensCss.indexOf(startMarker, fromIndex);

	if (startIndex === -1) {
		throw new Error(`Token block start not found: ${startMarker}`);
	}

	const contentStart = startIndex + startMarker.length;
	const endIndex = endMarker ? tokensCss.indexOf(endMarker, contentStart) : tokensCss.length;

	if (endIndex === -1) {
		throw new Error(`Token block end not found: ${endMarker}`);
	}

	return tokensCss.slice(contentStart, endIndex);
}

function parseTokens(block: string): Record<string, string> {
	const tokens: Record<string, string> = {};

	for (const match of block.matchAll(/(--[\w-]+):\s*([^;]+);/g)) {
		tokens[match[1]] = match[2].trim();
	}

	return tokens;
}

function resolveToken(name: string, tokens: Record<string, string>): string {
	const value = tokens[name];

	if (!value) {
		throw new Error(`Token not found: ${name}`);
	}

	const reference = value.match(/^var\((--[\w-]+)\)$/)?.[1];
	return reference ? resolveToken(reference, tokens) : value;
}

function hexToRgb(hex: string): [number, number, number] {
	const normalized = hex.replace('#', '');
	return [0, 2, 4].map(
		(index) => Number.parseInt(normalized.slice(index, index + 2), 16) / 255
	) as [number, number, number];
}

function linearize(channel: number): number {
	return channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
}

function contrastRatio(foreground: string, background: string): number {
	const foregroundLuminance = hexToRgb(foreground)
		.map((channel) => linearize(channel))
		.reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
	const backgroundLuminance = hexToRgb(background)
		.map((channel) => linearize(channel))
		.reduce((sum, channel, index) => sum + channel * [0.2126, 0.7152, 0.0722][index], 0);
	const lighter = Math.max(foregroundLuminance, backgroundLuminance);
	const darker = Math.min(foregroundLuminance, backgroundLuminance);

	return (lighter + 0.05) / (darker + 0.05);
}

const darkModeMarker = '@media (prefers-color-scheme: dark) {';
const darkRootMarker = ':root {';
const darkModeIndex = tokensCss.indexOf(darkModeMarker);

const lightTokens = parseTokens(getTokenBlock(darkRootMarker, darkModeMarker));
const darkTokens = parseTokens(getTokenBlock(darkRootMarker, undefined, darkModeIndex));

describe('DeviceAuditTrail', () => {
	it('renders audit timestamps and actor metadata', () => {
		render(DeviceAuditTrail, {
			createdAt: '2026-05-20T12:34:00Z',
			createdBy: 'Brian',
			modifiedAt: '2026-05-20T13:45:00Z',
			modifiedBy: 'Vasquez'
		});

		expect(screen.getByRole('heading', { name: 'Audit Trail' })).toBeInTheDocument();
		expect(screen.getByText('Created')).toBeInTheDocument();
		expect(screen.getByText('Last Modified')).toBeInTheDocument();
		expect(screen.getByText('by Brian')).toBeInTheDocument();
		expect(screen.getByText('by Vasquez')).toBeInTheDocument();
		expect(screen.getAllByText(/2026/)).toHaveLength(2);
	});

	it('omits actor metadata when no actor is provided', () => {
		render(DeviceAuditTrail, {
			createdAt: '2026-05-20T12:34:00Z',
			modifiedAt: '2026-05-20T13:45:00Z'
		});

		expect(screen.queryByText(/^by /)).not.toBeInTheDocument();
	});

	it('has no axe violations', async () => {
		const { container } = render(DeviceAuditTrail, {
			createdAt: '2026-05-20T12:34:00Z',
			createdBy: 'Brian',
			modifiedAt: '2026-05-20T13:45:00Z',
			modifiedBy: 'Vasquez'
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});

	it('keeps audit text contrast at or above WCAG AA in light and dark themes', () => {
		const textPairs = [
			['--color-text', '--color-bg'],
			['--color-text-secondary', '--color-bg']
		] as const;

		for (const [foregroundToken, backgroundToken] of textPairs) {
			expect(
				contrastRatio(
					resolveToken(foregroundToken, lightTokens),
					resolveToken(backgroundToken, lightTokens)
				)
			).toBeGreaterThanOrEqual(4.5);
			expect(
				contrastRatio(
					resolveToken(foregroundToken, darkTokens),
					resolveToken(backgroundToken, darkTokens)
				)
			).toBeGreaterThanOrEqual(4.5);
		}
	});
});

/**
 * ThemeToggle iconOnly mode — #147.
 *
 * Verifies that:
 * 1. iconOnly=true hides visible text labels while keeping aria-label
 *    accessible names intact on every button.
 * 2. iconOnly=false (default) renders visible text labels.
 * 3. aria-pressed selected state is communicated correctly in both modes.
 * 4. No axe violations in either mode.
 *
 * Tamper test: the test that verifies labels are hidden in iconOnly mode
 * is validated by first confirming labels ARE visible without the prop.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import ThemeToggle from './ThemeToggle.svelte';
import { resetThemeStoreForTests } from '$lib/stores/theme.svelte';

beforeEach(() => {
	window.localStorage.clear();
	resetThemeStoreForTests();

	Object.defineProperty(window, 'matchMedia', {
		writable: true,
		value: vi.fn().mockImplementation(() => ({
			matches: false,
			media: '(prefers-color-scheme: dark)',
			addEventListener: vi.fn(),
			removeEventListener: vi.fn()
		}))
	});
});

describe('ThemeToggle — default (labels visible)', () => {
	it('renders visible text labels for Light, Dark, and System by default', () => {
		render(ThemeToggle);

		// Labels are visible in the default (non-iconOnly) mode.
		expect(screen.getByRole('button', { name: /light/i }).textContent?.trim()).toContain('Light');
		expect(screen.getByRole('button', { name: /dark/i }).textContent?.trim()).toContain('Dark');
		expect(screen.getByRole('button', { name: /system/i }).textContent?.trim()).toContain('System');
	});

	it('has no accessibility violations in default mode', async () => {
		const { container } = render(ThemeToggle);
		expect(await axe(container)).toHaveNoViolations();
	});
});

describe('ThemeToggle — iconOnly mode (#147)', () => {
	it('hides visible text labels when iconOnly is true', () => {
		render(ThemeToggle, { props: { iconOnly: true } });

		// Buttons are still accessible by aria-label, but no <span> text appears.
		const lightBtn = screen.getByRole('button', { name: /light/i });
		const darkBtn = screen.getByRole('button', { name: /dark/i });
		const systemBtn = screen.getByRole('button', { name: /system/i });

		// Icons are present (SVG children), but text label spans are absent.
		// The accessible name comes from aria-label on the button, not from
		// a visible text node — we confirm no text content from the span.
		expect(lightBtn.querySelector('span')).toBeNull();
		expect(darkBtn.querySelector('span')).toBeNull();
		expect(systemBtn.querySelector('span')).toBeNull();
	});

	it('retains aria-label accessible names on all three buttons when iconOnly is true', () => {
		render(ThemeToggle, { props: { iconOnly: true } });

		// Accessible names must survive even when visible labels are hidden.
		expect(screen.getByRole('button', { name: 'Light' })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Dark' })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'System' })).toBeInTheDocument();
	});

	it('exposes aria-pressed selected state in iconOnly mode', () => {
		render(ThemeToggle, { props: { iconOnly: true } });

		// System is the default preference.
		expect(screen.getByRole('button', { name: 'System' })).toHaveAttribute(
			'aria-pressed',
			'true'
		);
		expect(screen.getByRole('button', { name: 'Light' })).toHaveAttribute(
			'aria-pressed',
			'false'
		);
		expect(screen.getByRole('button', { name: 'Dark' })).toHaveAttribute(
			'aria-pressed',
			'false'
		);
	});

	it('renders SVG icons for all three options in iconOnly mode', () => {
		render(ThemeToggle, { props: { iconOnly: true } });

		for (const name of ['Light', 'Dark', 'System']) {
			const btn = screen.getByRole('button', { name });
			expect(btn.querySelector('svg')).not.toBeNull();
		}
	});

	it('has no accessibility violations in iconOnly mode', async () => {
		const { container } = render(ThemeToggle, { props: { iconOnly: true } });
		expect(await axe(container)).toHaveNoViolations();
	});

	it('tamper: labels ARE visible when iconOnly is false (guard for the hide test above)', () => {
		// This is the tamper-baseline: without iconOnly, spans with text exist.
		// If the hide test above passes when spans are actually present, this
		// test would still pass — meaning the hide test would be broken.
		// The combination of both tests proves the prop controls the behavior.
		render(ThemeToggle, { props: { iconOnly: false } });

		const lightBtn = screen.getByRole('button', { name: 'Light' });
		expect(lightBtn.querySelector('span')).not.toBeNull();
		expect(lightBtn.querySelector('span')?.textContent?.trim()).toBe('Light');
	});
});

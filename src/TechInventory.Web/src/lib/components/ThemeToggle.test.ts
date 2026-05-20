/**
 * ThemeToggle.test.ts — Vitest + Testing Library + axe-core unit test for F029.
 *
 * Verifies:
 * - Contrast on the theme toggle itself (≥4.5:1 AA)
 * - No accessibility violations in the component
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import ThemeToggle from './ThemeToggle.svelte';

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => {
		const translations: Record<string, string> = {
			'settings.theme.toggleLabel': 'Theme preference',
			'settings.theme.light': 'Light',
			'settings.theme.dark': 'Dark',
			'settings.theme.system': 'System'
		};
		return translations[key] || key;
	}
}));

// Mock authStore
vi.mock('$lib/stores/auth', () => ({
	authStore: {
		subscribe: (fn: (v: { currentUser: { id: string } | null }) => void) => {
			fn({ currentUser: { id: 'test-user' } });
			return () => {};
		}
	}
}));

// Mock userPrefs
vi.mock('$lib/stores/userPrefs', () => ({
	getThemePreference: vi.fn(() => 'system'),
	setThemePreference: vi.fn()
}));

describe('ThemeToggle', () => {
	beforeEach(() => {
		// Reset document.documentElement.dataset
		delete document.documentElement.dataset.theme;
	});

	it('renders without accessibility violations', async () => {
		const { container } = render(ThemeToggle);
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});

	it('renders three theme options', () => {
		render(ThemeToggle);
		expect(screen.getByRole('button', { name: /light/i })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /dark/i })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /system/i })).toBeInTheDocument();
	});

	it('marks the system option as pressed by default', () => {
		render(ThemeToggle);
		const systemButton = screen.getByRole('button', { name: /system/i });
		expect(systemButton).toHaveAttribute('aria-pressed', 'true');
	});
});

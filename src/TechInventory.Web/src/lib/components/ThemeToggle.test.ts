import { beforeEach, describe, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import ThemeToggle from './ThemeToggle.svelte';
import { resetThemeStoreForTests, THEME_STORAGE_KEY } from '$lib/stores/theme.svelte';

let systemPrefersDark = false;

describe('ThemeToggle', () => {
	beforeEach(() => {
		systemPrefersDark = false;
		window.localStorage.clear();
		resetThemeStoreForTests();

		Object.defineProperty(window, 'matchMedia', {
			writable: true,
			value: vi.fn().mockImplementation(() => ({
				get matches() {
					return systemPrefersDark;
				},
				media: '(prefers-color-scheme: dark)',
				addEventListener: vi.fn(),
				removeEventListener: vi.fn()
			}))
		});
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
		expect(screen.getByRole('button', { name: /system/i })).toHaveAttribute('aria-pressed', 'true');
	});

	it('persists a new preference when clicked', async () => {
		const user = userEvent.setup();
		render(ThemeToggle);

		await user.click(screen.getByRole('button', { name: /dark/i }));

		expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
		expect(document.documentElement.dataset.theme).toBe('dark');
		expect(document.documentElement.classList.contains('dark')).toBe(true);
	});
});

/**
 * TableColumnSettings — field regression guards (#164).
 *
 * Guards:
 *   1. Reset button text is sourced from the i18n token
 *      `settings.tableColumns.resetToDefault` — not a hard-coded copy such
 *      as "Reset to Default" or any other literal string.
 *   2. Reset button carries `whitespace-nowrap` so the label cannot break
 *      across two lines at the narrow widths found in 320–390 px installed-
 *      PWA viewports.
 *
 * TAMPER-TESTED: Both guards are currently met by the implementation.
 *   (1) Replacing `{t('settings.tableColumns.resetToDefault')}` with a
 *   hard-coded literal makes the i18n test fail.
 *   (2) Removing `whitespace-nowrap` from the button class makes the
 *   no-wrap test fail.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import TableColumnSettings from './TableColumnSettings.svelte';

vi.mock('$lib/i18n', () => ({
	// Return the key itself so tests can assert on the key, not the English
	// value — this makes the test immune to copy changes and proves the
	// component routes through t() rather than embedding literal strings.
	t: (key: string) => key
}));

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await import('svelte/store');
	return {
		authStore: writable({
			currentUser: {
				id: 'user-reset-test-0000-000000000000',
				role: 'Admin',
				displayName: 'Tester',
				entraObjectId: null
			},
			isAuthenticated: true,
			isLoading: false,
			error: null,
			authMethod: 'entra',
			mustChangePassword: false
		})
	};
});

vi.mock('$lib/stores/toast', () => ({
	addToast: vi.fn()
}));

vi.mock('$lib/stores/userPrefs', () => ({
	ALL_TABLE_COLUMNS: ['name', 'brand'] as const,
	DEFAULT_TABLE_COLUMNS: ['name', 'brand'],
	getTableColumns: vi.fn(() => null),
	setTableColumns: vi.fn()
}));

describe('TableColumnSettings — #164 Reset button i18n + no-wrap contract', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// With t(key) => key, the button accessible name equals the i18n key.
	// Replacing the template expression with a hard-coded literal breaks this.
	it('Reset button text is sourced from t("settings.tableColumns.resetToDefault"), not a hard-coded copy', () => {
		render(TableColumnSettings);
		// Exact key match proves the component calls t() rather than embedding a
		// literal.  The mock returns the key verbatim so any literal "Reset",
		// "Reset to Default", or other copy would NOT match this query.
		const btn = screen.getByRole('button', {
			name: 'settings.tableColumns.resetToDefault'
		});
		expect(btn, 'Reset button must be localized via t()').toBeInTheDocument();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// If a future change re-introduces a hard-coded copy such as "Reset to
	// Default", the component would no longer call t() for that string.
	// This guard catches that regression independently of the mock strategy.
	it('Reset button does not contain the old hard-coded copy "Reset to Default"', () => {
		render(TableColumnSettings);
		// The t() mock returns the i18n key, so any button whose text matches
		// "Reset to Default" is not using t() and is hard-coded.
		const literal = screen.queryByRole('button', { name: /reset to default/i });
		expect(literal, 'hard-coded "Reset to Default" must not appear — use t() instead').toBeNull();
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// The Reset button must carry whitespace-nowrap so that at narrow installed-
	// PWA viewports (320–390 px) the label stays on a single line.
	it('Reset button carries whitespace-nowrap to prevent label wrapping at narrow viewports', () => {
		render(TableColumnSettings);
		const btn = screen.getByRole('button', {
			name: 'settings.tableColumns.resetToDefault'
		});
		expect(
			btn.className,
			'Reset button must carry whitespace-nowrap; absent class will cause label to wrap at ≤390 px'
		).toMatch(/\bwhitespace-nowrap\b/);
	});
});

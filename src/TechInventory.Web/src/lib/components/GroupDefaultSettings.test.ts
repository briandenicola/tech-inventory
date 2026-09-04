/**
 * GroupDefaultSettings — the Settings control for how grouped lists start out.
 *
 * Uses the real i18n bundle so the copy the user actually reads is asserted;
 * `$lib/stores/auth` and `$lib/stores/toast` are stubbed because the component
 * only needs an id to key localStorage by, and a toast spy to confirm the save
 * is acknowledged.
 *
 * TAMPER-TESTED: dropping the `setGroupDefaultState` call from `choose()` fails
 *   "persists the chosen state"; seeding `selected` from DEFAULT_GROUP_STATE
 *   instead of the stored value fails "marks exactly the stored choice as
 *   checked".
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

vi.mock('$lib/stores/auth', async () => {
	const { writable } = await import('svelte/store');
	return {
		authStore: writable({
			currentUser: {
				id: 'user-group-default-0000-0000',
				role: 'Member',
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

const toastSpy = vi.hoisted(() => vi.fn());
vi.mock('$lib/stores/toast', () => ({
	showToast: toastSpy,
	addToast: toastSpy
}));

const USER_ID = 'user-group-default-0000-0000';

import GroupDefaultSettings from './GroupDefaultSettings.svelte';
import { getGroupDefaultState, setGroupDefaultState } from '$lib/stores/userPrefs';

describe('GroupDefaultSettings', () => {
	beforeEach(() => {
		window.localStorage.clear();
		toastSpy.mockClear();
	});

	it('offers both starting states', () => {
		render(GroupDefaultSettings);

		expect(screen.getByRole('radio', { name: /Expanded/ })).toBeInTheDocument();
		expect(screen.getByRole('radio', { name: /Collapsed/ })).toBeInTheDocument();
	});

	it('marks exactly the stored choice as checked', () => {
		setGroupDefaultState(USER_ID, 'collapsed');

		render(GroupDefaultSettings);

		expect(screen.getByRole('radio', { name: /Collapsed/ })).toBeChecked();
		expect(screen.getByRole('radio', { name: /Expanded/ })).not.toBeChecked();
	});

	it('defaults to Expanded before any choice is made', () => {
		render(GroupDefaultSettings);

		expect(screen.getByRole('radio', { name: /Expanded/ })).toBeChecked();
	});

	it('persists the chosen state', async () => {
		const user = userEvent.setup();
		render(GroupDefaultSettings);

		await user.click(screen.getByRole('radio', { name: /Collapsed/ }));

		expect(getGroupDefaultState(USER_ID)).toBe('collapsed');
	});

	it('acknowledges the save with a toast', async () => {
		const user = userEvent.setup();
		render(GroupDefaultSettings);

		await user.click(screen.getByRole('radio', { name: /Collapsed/ }));

		expect(toastSpy).toHaveBeenCalledWith(
			expect.objectContaining({ type: 'success', message: 'Default group state saved.' })
		);
	});

	it('does not re-save or re-toast when the current choice is re-selected', async () => {
		const user = userEvent.setup();
		render(GroupDefaultSettings);

		await user.click(screen.getByRole('radio', { name: /Expanded/ }));

		expect(toastSpy).not.toHaveBeenCalled();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(GroupDefaultSettings);

		expect(await axe(container)).toHaveNoViolations();
	});
});

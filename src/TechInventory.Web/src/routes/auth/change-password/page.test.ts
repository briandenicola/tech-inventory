/**
 * F025 — change-password page validation + happy-path coverage.
 *
 * The page coordinates auth-store mutations + the typed API client, so we
 * mock the boundary modules. JSDOM + Testing Library cover the form UX.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/svelte';

const { goto, localSignOut, changePassword } = vi.hoisted(() => ({
	goto: vi.fn(),
	localSignOut: vi.fn(),
	changePassword: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));
vi.mock('$lib/stores/auth', () => ({ localSignOut }));
vi.mock('$lib/api/client', () => ({ localAuth: { changePassword } }));

import Page from './+page.svelte';

async function fillForm(current: string, next: string, confirm: string) {
	await fireEvent.input(screen.getByLabelText(/Current password/i), {
		target: { value: current }
	});
	await fireEvent.input(screen.getByLabelText(/^New password$/i), {
		target: { value: next }
	});
	await fireEvent.input(screen.getByLabelText(/Confirm new password/i), {
		target: { value: confirm }
	});
}

describe('change-password page', () => {
	beforeEach(() => {
		goto.mockReset();
		localSignOut.mockReset();
		changePassword.mockReset();
		vi.useRealTimers();
	});

	it('blocks submission when the new password is shorter than 12 characters', async () => {
		render(Page);
		await fillForm('current-pass', 'short-pwd', 'short-pwd');
		await fireEvent.click(screen.getByRole('button', { name: /Update password/i }));

		expect(changePassword).not.toHaveBeenCalled();
		const alert = await screen.findByRole('alert');
		expect(alert.textContent).toMatch(/at least 12 characters/i);
	});

	it('blocks submission when confirmation does not match', async () => {
		render(Page);
		await fillForm('current-pass', 'a-strong-new-passw0rd!', 'mismatched-passw0rd!');
		await fireEvent.click(screen.getByRole('button', { name: /Update password/i }));

		expect(changePassword).not.toHaveBeenCalled();
		const alert = await screen.findByRole('alert');
		expect(alert.textContent).toMatch(/Passwords do not match/i);
	});

	it('blocks submission when new password equals current password', async () => {
		render(Page);
		const same = 'same-password-123!';
		await fillForm(same, same, same);
		await fireEvent.click(screen.getByRole('button', { name: /Update password/i }));

		expect(changePassword).not.toHaveBeenCalled();
		const alert = await screen.findByRole('alert');
		expect(alert.textContent).toMatch(/must differ/i);
	});

	it('clears the session and routes to /auth/login on success', async () => {
		changePassword.mockResolvedValue(undefined);
		render(Page);
		await fillForm('current-pass-1!', 'a-strong-new-passw0rd!', 'a-strong-new-passw0rd!');
		await fireEvent.click(screen.getByRole('button', { name: /Update password/i }));

		await waitFor(() =>
			expect(changePassword).toHaveBeenCalledWith({
				currentPassword: 'current-pass-1!',
				newPassword: 'a-strong-new-passw0rd!'
			})
		);
		await waitFor(() => expect(localSignOut).toHaveBeenCalled());
		await waitFor(() => expect(goto).toHaveBeenCalledWith('/auth/login'), { timeout: 3000 });
	});
});

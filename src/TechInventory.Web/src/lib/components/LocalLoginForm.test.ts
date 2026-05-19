/**
 * F025 — LocalLoginForm validation coverage. We mock the auth store's
 * `localSignIn` so the test stays isolated from `$lib/api/client` (which would
 * otherwise try to hit the network and pull in the full MSAL graph).
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/svelte';

const { goto, localSignIn } = vi.hoisted(() => ({
	goto: vi.fn(),
	localSignIn: vi.fn()
}));

vi.mock('$app/navigation', () => ({ goto }));
vi.mock('$lib/stores/auth', () => ({ localSignIn }));

import LocalLoginForm from './LocalLoginForm.svelte';

describe('LocalLoginForm', () => {
	beforeEach(() => {
		goto.mockReset();
		localSignIn.mockReset();
	});

	it('renders the username + password inputs and submit button', () => {
		render(LocalLoginForm, { props: {} });
		expect(screen.getByLabelText(/Username/i)).toBeInTheDocument();
		expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /Sign in locally/i })).toBeInTheDocument();
	});

	it('navigates to /devices when sign-in succeeds without forced rotation', async () => {
		localSignIn.mockResolvedValue({
			subjectId: 's',
			displayName: 'admin',
			role: 'Admin',
			mustChangePassword: false,
			expiresAtUtc: '2099-01-01T00:00:00Z'
		});

		render(LocalLoginForm, { props: {} });
		await fireEvent.input(screen.getByLabelText(/Username/i), { target: { value: 'admin' } });
		await fireEvent.input(screen.getByLabelText(/Password/i), {
			target: { value: 'a-strong-passw0rd!' }
		});
		await fireEvent.click(screen.getByRole('button', { name: /Sign in locally/i }));

		await waitFor(() => expect(localSignIn).toHaveBeenCalledWith('admin', 'a-strong-passw0rd!'));
		await waitFor(() => expect(goto).toHaveBeenCalledWith('/devices'));
	});

	it('redirects to /auth/change-password when the session requires rotation', async () => {
		localSignIn.mockResolvedValue({
			subjectId: 's',
			displayName: 'admin',
			role: 'Admin',
			mustChangePassword: true,
			expiresAtUtc: '2099-01-01T00:00:00Z'
		});

		render(LocalLoginForm, { props: {} });
		await fireEvent.input(screen.getByLabelText(/Username/i), { target: { value: 'admin' } });
		await fireEvent.input(screen.getByLabelText(/Password/i), {
			target: { value: 'temp-password!' }
		});
		await fireEvent.click(screen.getByRole('button', { name: /Sign in locally/i }));

		await waitFor(() => expect(goto).toHaveBeenCalledWith('/auth/change-password'));
	});

	it('shows the invalid-credentials error for 401 responses', async () => {
		localSignIn.mockRejectedValue(new Error('HTTP 401 Unauthorized'));

		render(LocalLoginForm, { props: {} });
		await fireEvent.input(screen.getByLabelText(/Username/i), { target: { value: 'admin' } });
		await fireEvent.input(screen.getByLabelText(/Password/i), { target: { value: 'wrong' } });
		await fireEvent.click(screen.getByRole('button', { name: /Sign in locally/i }));

		const alert = await screen.findByRole('alert');
		expect(alert.textContent).toMatch(/Username or password is incorrect/i);
		expect(goto).not.toHaveBeenCalled();
	});
});

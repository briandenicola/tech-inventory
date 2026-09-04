/**
 * ApiKeysSettings — #149 / spec 005 US-6.
 *
 * Covers the four required states (loading, empty, error, populated) plus the
 * behaviours that actually matter for a credential UI: the plaintext key is shown
 * once and only after a successful create, revoking is confirmed before it happens,
 * and the quota is reflected in the form.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

const mocks = vi.hoisted(() => ({
	list: vi.fn(),
	create: vi.fn(),
	revoke: vi.fn(),
	addToast: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	apiKeys: {
		list: mocks.list,
		create: mocks.create,
		revoke: mocks.revoke
	}
}));

vi.mock('$lib/stores/toast', () => ({
	addToast: mocks.addToast,
	showToast: mocks.addToast
}));

import ApiKeysSettings from './ApiKeysSettings.svelte';

const activeKey = {
	id: 'key-1',
	name: 'shortcuts',
	selector: 'c2VsZWN0b3I',
	scope: 'inventory.read',
	createdAt: '2026-03-22T10:00:00Z',
	expiresAt: '2026-06-20T10:00:00Z',
	revokedAt: null,
	isActive: true
};

function listReturns(items: unknown[]) {
	mocks.list.mockResolvedValue({ items, totalCount: items.length, page: 1, pageSize: 50 });
}

describe('ApiKeysSettings', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		listReturns([]);
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	describe('the four required states', () => {
		it('shows a loading state before the request settles', () => {
			mocks.list.mockReturnValue(new Promise(() => {}));

			render(ApiKeysSettings);

			expect(screen.getByTestId('api-keys-loading')).toBeInTheDocument();
		});

		it('shows an empty state when the user has no keys', async () => {
			listReturns([]);

			render(ApiKeysSettings);

			expect(await screen.findByTestId('api-keys-empty')).toBeInTheDocument();
		});

		it('shows an error state when the list request fails', async () => {
			mocks.list.mockRejectedValue(new Error('boom'));

			render(ApiKeysSettings);

			expect(await screen.findByRole('alert')).toBeInTheDocument();
			expect(screen.queryByTestId('api-key-row')).not.toBeInTheDocument();
		});

		it('lists the keys the user has', async () => {
			listReturns([activeKey]);

			render(ApiKeysSettings);

			expect(await screen.findByText('shortcuts')).toBeInTheDocument();
			expect(screen.getAllByTestId('api-key-row')).toHaveLength(1);
		});
	});

	describe('creating a key', () => {
		it('does not reveal a secret before one has been created', async () => {
			listReturns([activeKey]);

			render(ApiKeysSettings);
			await screen.findByText('shortcuts');

			expect(screen.queryByTestId('api-key-secret')).not.toBeInTheDocument();
		});

		it('shows the plaintext key exactly once, after a successful create', async () => {
			const user = userEvent.setup();
			listReturns([]);
			mocks.create.mockResolvedValue({ id: 'k2', name: 'ha', key: 'sel.the-only-copy' });

			render(ApiKeysSettings);
			await screen.findByTestId('api-keys-empty');

			await user.type(screen.getByLabelText(/new key name/i), 'ha');
			await user.click(screen.getByRole('button', { name: /create key/i }));

			const secret = await screen.findByTestId('api-key-secret');
			expect(secret).toHaveTextContent(/copy it now/i);
			expect(screen.getByDisplayValue('sel.the-only-copy')).toBeInTheDocument();
		});

		it('refuses to submit an empty name without calling the API', async () => {
			const user = userEvent.setup();
			render(ApiKeysSettings);
			await screen.findByTestId('api-keys-empty');

			await user.click(screen.getByRole('button', { name: /create key/i }));

			expect(mocks.create).not.toHaveBeenCalled();
			expect(await screen.findByRole('alert')).toBeInTheDocument();
		});

		it('sends the selected scope', async () => {
			const user = userEvent.setup();
			mocks.create.mockResolvedValue({ id: 'k3', name: 'w', key: 'sel.secret' });

			render(ApiKeysSettings);
			await screen.findByTestId('api-keys-empty');

			await user.type(screen.getByLabelText(/new key name/i), 'w');
			await user.click(screen.getByRole('button', { name: /read & write/i }));
			await user.click(screen.getByRole('button', { name: /create key/i }));

			await waitFor(() =>
				expect(mocks.create).toHaveBeenCalledWith({ name: 'w', scope: 'inventory.write' })
			);
		});

		it('surfaces a create failure without showing a secret', async () => {
			const user = userEvent.setup();
			mocks.create.mockRejectedValue(new Error('nope'));

			render(ApiKeysSettings);
			await screen.findByTestId('api-keys-empty');

			await user.type(screen.getByLabelText(/new key name/i), 'x');
			await user.click(screen.getByRole('button', { name: /create key/i }));

			expect(await screen.findByRole('alert')).toBeInTheDocument();
			expect(screen.queryByTestId('api-key-secret')).not.toBeInTheDocument();
		});

		it('disables creation once the five-key quota is reached', async () => {
			listReturns(
				Array.from({ length: 5 }, (_, index) => ({
					...activeKey,
					id: `key-${index}`,
					name: `key-${index}`
				}))
			);

			render(ApiKeysSettings);
			await screen.findByText('key-0');

			expect(screen.getByRole('button', { name: /create key/i })).toBeDisabled();
		});
	});

	describe('revoking a key', () => {
		it('asks for confirmation and does nothing when declined', async () => {
			const user = userEvent.setup();
			listReturns([activeKey]);
			const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

			render(ApiKeysSettings);
			await screen.findByText('shortcuts');

			await user.click(screen.getByRole('button', { name: /revoke/i }));

			expect(confirmSpy).toHaveBeenCalled();
			expect(mocks.revoke).not.toHaveBeenCalled();
		});

		it('revokes once confirmed', async () => {
			const user = userEvent.setup();
			listReturns([activeKey]);
			vi.spyOn(window, 'confirm').mockReturnValue(true);
			mocks.revoke.mockResolvedValue(undefined);

			render(ApiKeysSettings);
			await screen.findByText('shortcuts');

			await user.click(screen.getByRole('button', { name: /revoke/i }));

			await waitFor(() => expect(mocks.revoke).toHaveBeenCalledWith('key-1'));
		});

		it('offers no revoke button for an already-revoked key', async () => {
			listReturns([{ ...activeKey, revokedAt: '2026-04-01T00:00:00Z', isActive: false }]);

			render(ApiKeysSettings);
			await screen.findByText('shortcuts');

			expect(screen.queryByRole('button', { name: /revoke/i })).not.toBeInTheDocument();
		});
	});

	it('has no accessibility violations with keys listed', async () => {
		listReturns([activeKey]);

		const { container } = render(ApiKeysSettings);
		await screen.findByText('shortcuts');

		expect(await axe(container)).toHaveNoViolations();
	});

	it('has no accessibility violations while showing a secret', async () => {
		const user = userEvent.setup();
		mocks.create.mockResolvedValue({ id: 'k4', name: 'a', key: 'sel.secret' });

		const { container } = render(ApiKeysSettings);
		await screen.findByTestId('api-keys-empty');
		await user.type(screen.getByLabelText(/new key name/i), 'a');
		await user.click(screen.getByRole('button', { name: /create key/i }));
		await screen.findByTestId('api-key-secret');

		expect(await axe(container)).toHaveNoViolations();
	});
});

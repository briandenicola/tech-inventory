/**
 * ApiKeySecretDisplay — #149 / spec 005 T-016.
 *
 * The component shows a credential that cannot be recovered once dismissed, so the
 * tests focus on that: the warning is present, the value is selectable rather than
 * copy-button-only, and a clipboard failure degrades to something the user can act on
 * instead of silently appearing to succeed.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import ApiKeySecretDisplay from './ApiKeySecretDisplay.svelte';

// Deliberately low-entropy and obviously fake. A realistic random-looking key here
// trips the repo's gitleaks scan, which cannot distinguish a test fixture from a live
// credential — and it is right not to try. These tests only need *a* string in
// <selector>.<secret> shape; nothing asserts on its randomness.
const FAKE_KEY = 'test-selector.test-secret-value';

// userEvent.setup() installs its own clipboard stub, so this must be applied AFTER
// setup() and the returned spy asserted on directly rather than reading
// navigator.clipboard back.
function setClipboard(impl: () => Promise<void>) {
	const writeText = vi.fn(impl);
	Object.defineProperty(navigator, 'clipboard', {
		value: { writeText },
		configurable: true,
		writable: true
	});
	return writeText;
}

describe('ApiKeySecretDisplay', () => {
	beforeEach(() => {
		vi.clearAllMocks();
		setClipboard(() => Promise.resolve());
	});

	it('shows the secret and the warning that it will not be shown again', () => {
		render(ApiKeySecretDisplay, { props: { secret: FAKE_KEY, onDismiss: vi.fn() } });

		expect(screen.getByDisplayValue(FAKE_KEY)).toBeInTheDocument();
		expect(screen.getByText(/won't be shown again/i)).toBeInTheDocument();
	});

	it('renders the secret in a read-only field so it cannot be edited before copying', () => {
		render(ApiKeySecretDisplay, { props: { secret: FAKE_KEY, onDismiss: vi.fn() } });

		expect(screen.getByDisplayValue(FAKE_KEY)).toHaveAttribute('readonly');
	});

	it('copies the secret to the clipboard and confirms', async () => {
		const user = userEvent.setup();
		const writeText = setClipboard(() => Promise.resolve());
		render(ApiKeySecretDisplay, { props: { secret: FAKE_KEY, onDismiss: vi.fn() } });

		await user.click(screen.getByRole('button', { name: /^copy$/i }));

		expect(writeText).toHaveBeenCalledWith(FAKE_KEY);
		// Both the button label and the live region read "Copied", so target the
		// status region specifically — that is the part screen readers announce.
		await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent(/copied/i));
	});

	it('tells the user to copy manually when the clipboard API fails', async () => {
		// Insecure contexts, Safari permission prompts and older WebViews all reject
		// here. Silently doing nothing would leave the user believing they had the key.
		const user = userEvent.setup();
		setClipboard(() => Promise.reject(new Error('denied')));

		render(ApiKeySecretDisplay, { props: { secret: FAKE_KEY, onDismiss: vi.fn() } });

		await user.click(screen.getByRole('button', { name: /^copy$/i }));

		await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent(/copy failed/i));
		expect(screen.getByDisplayValue(FAKE_KEY)).toBeInTheDocument();
	});

	it('calls onDismiss when dismissed', async () => {
		const user = userEvent.setup();
		const onDismiss = vi.fn();
		render(ApiKeySecretDisplay, { props: { secret: FAKE_KEY, onDismiss } });

		await user.click(screen.getByRole('button', { name: /dismiss/i }));

		expect(onDismiss).toHaveBeenCalledTimes(1);
	});

	it('has no accessibility violations', async () => {
		const { container } = render(ApiKeySecretDisplay, {
			props: { secret: FAKE_KEY, onDismiss: vi.fn() }
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});

/**
 * PwaUpdatePrompt — C-17 (component render/dismiss only). Real service-
 * worker registration/install prompts are residual browser-only behaviour
 * (coverage-migration.md G-04 / M-05 / M-09); this test doubles the
 * `virtual:pwa-register` module `@vite-pwa/sveltekit` injects at build time
 * so the component's own reactive contract — banner appears on
 * `onNeedRefresh`, "Reload" invokes the update-and-reload callback,
 * "Dismiss" just hides the banner — is exercised deterministically.
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';

interface RegisterSWOptions {
	immediate?: boolean;
	onNeedRefresh?: () => void;
	onRegisterError?: (err: unknown) => void;
}

const { registerSWMock, updateSWMock } = vi.hoisted(() => ({
	registerSWMock: vi.fn(),
	updateSWMock: vi.fn(async () => {})
}));

vi.mock('virtual:pwa-register', () => ({
	registerSW: registerSWMock
}));

import PwaUpdatePrompt from './PwaUpdatePrompt.svelte';

function capturedOptions(): RegisterSWOptions {
	const options = registerSWMock.mock.calls.at(-1)?.[0] as RegisterSWOptions | undefined;
	if (!options) throw new Error('registerSW was not called');
	return options;
}

describe('PwaUpdatePrompt (C-17)', () => {
	beforeEach(() => {
		registerSWMock.mockReset().mockReturnValue(updateSWMock);
		updateSWMock.mockReset().mockResolvedValue(undefined);
	});

	it('renders nothing until a new service worker version is available', async () => {
		render(PwaUpdatePrompt);

		await waitFor(() => expect(registerSWMock).toHaveBeenCalled());
		expect(screen.queryByRole('status')).not.toBeInTheDocument();
	});

	it('shows the update banner when onNeedRefresh fires, and Reload activates the update', async () => {
		const user = userEvent.setup();
		render(PwaUpdatePrompt);

		await waitFor(() => expect(registerSWMock).toHaveBeenCalled());
		capturedOptions().onNeedRefresh?.();

		const banner = await screen.findByRole('status');
		expect(banner).toHaveTextContent('A new version of Tech Inventory is ready.');

		await user.click(screen.getByRole('button', { name: 'Reload' }));

		expect(updateSWMock).toHaveBeenCalledWith(true);
		expect(screen.queryByRole('status')).not.toBeInTheDocument();
	});

	it('Dismiss hides the banner without activating the update', async () => {
		const user = userEvent.setup();
		render(PwaUpdatePrompt);

		await waitFor(() => expect(registerSWMock).toHaveBeenCalled());
		capturedOptions().onNeedRefresh?.();
		await screen.findByRole('status');

		await user.click(screen.getByRole('button', { name: 'Dismiss' }));

		expect(updateSWMock).not.toHaveBeenCalled();
		expect(screen.queryByRole('status')).not.toBeInTheDocument();
	});

	it('has no accessibility violations while the banner is visible', async () => {
		const { container } = render(PwaUpdatePrompt);

		await waitFor(() => expect(registerSWMock).toHaveBeenCalled());
		capturedOptions().onNeedRefresh?.();
		await screen.findByRole('status');

		expect(await axe(container)).toHaveNoViolations();
	});
});

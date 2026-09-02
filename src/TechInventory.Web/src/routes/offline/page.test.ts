/**
 * /offline — C-14. Static PWA offline shell served by the service worker's
 * `navigateFallback` (see vite.config.ts / C-15). No API calls, no auth
 * state — this test only proves the message renders, the retry affordance
 * reloads the page, and the page is axe-clean.
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import Page from './+page.svelte';

describe('/offline', () => {
	it('renders an offline title, description, and a retry action', () => {
		render(Page);

		expect(screen.getByRole('heading', { name: "You're offline" })).toBeInTheDocument();
		expect(
			screen.getByText(
				'Tech Inventory needs an internet connection to load fresh data. Check your network and try again.'
			)
		).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument();
	});

	it('reloads the page when the retry action is clicked', async () => {
		const user = userEvent.setup();
		const reloadMock = vi.fn();
		vi.stubGlobal('location', { ...window.location, reload: reloadMock });

		render(Page);
		await user.click(screen.getByRole('button', { name: 'Try again' }));

		expect(reloadMock).toHaveBeenCalledOnce();

		vi.unstubAllGlobals();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(Page);

		expect(await axe(container)).toHaveNoViolations();
	});
});

/**
 * Proxy test for CSS containing-block / overlay-scope behavior — prior art:
 * `PullToRefresh.containing-block.test.ts`.
 *
 * F045 B6: the popover replaces a former full-screen drawer specifically so
 * it does *not* trap scroll or cover the viewport (F045 §5.3: "must not
 * scroll-lock the body and must not render a full-screen backdrop"), and so
 * it is not clipped by the sticky header's stacking context (the actual B1
 * stacking risk — see `headerStacking.test.ts` for the z-index half of that
 * fix). JSDOM cannot compute the CSS containing-block algorithm, so — as in
 * the PullToRefresh prior art — this asserts the DOM/style attributes that
 * are reliable proxies for the bug: no inline `transform` on the popover or
 * its trigger wrapper (which would create an unwanted `position: fixed`
 * containing block for the popover's own `--z-popover` panel or for any
 * sibling `position: fixed` element, e.g. `AppBottomNav`), no body scroll
 * lock, and no full-viewport backdrop element.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import AppMenuPopover from './AppMenuPopover.svelte';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

describe('AppMenuPopover — containing-block / overlay-scope safety (F045 B6)', () => {
	beforeEach(() => {
		Object.defineProperty(window, 'matchMedia', {
			writable: true,
			value: vi.fn().mockImplementation(() => ({
				matches: false,
				media: '(prefers-color-scheme: dark)',
				addEventListener: vi.fn(),
				removeEventListener: vi.fn()
			}))
		});
	});

	const defaultProps = {
		pathname: '/devices',
		currentUser: makeUser('Admin'),
		onSignOut: vi.fn()
	};

	it('at rest: neither the trigger wrapper nor the panel carry an inline transform (no unintended containing block)', () => {
		const { container } = render(AppMenuPopover, { props: defaultProps });

		const root = container.querySelector('.relative') as HTMLElement | null;
		expect(root).not.toBeNull();
		expect(root!.style.transform).toBe('');
	});

	it('while open: the panel is positioned via absolute + z-popover, not a fixed/transform escape hatch', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: defaultProps });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const panel = document.getElementById('app-menu-popover');
		expect(panel).not.toBeNull();
		expect(panel!.style.transform).toBe('');
		expect(panel!.className).toContain('absolute');
		expect(panel!.getAttribute('style')).toContain('var(--z-popover)');
	});

	it('does not lock body scroll while open', async () => {
		const user = userEvent.setup();
		const previousOverflow = document.body.style.overflow;
		render(AppMenuPopover, { props: defaultProps });

		await user.click(screen.getByRole('button', { name: 'Menu' }));
		expect(document.body.style.overflow).toBe(previousOverflow);

		await user.click(screen.getByRole('button', { name: 'Close menu' }));
		expect(document.body.style.overflow).toBe(previousOverflow);
	});

	it('renders no full-screen backdrop element while open', async () => {
		const user = userEvent.setup();
		const { container } = render(AppMenuPopover, { props: defaultProps });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		// A full-screen backdrop in this codebase is always `fixed inset-0`
		// (see DeviceFilters.svelte's mobile drawer backdrop for the pattern
		// this component deliberately does not reuse). None should exist here.
		const candidates = Array.from(container.querySelectorAll('[class*="inset-0"]'));
		expect(candidates).toHaveLength(0);
		expect(document.querySelector('[role="presentation"]')).not.toBeInTheDocument();
	});
});

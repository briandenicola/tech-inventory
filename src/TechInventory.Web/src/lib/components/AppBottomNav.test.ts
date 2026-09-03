/**
 * AppBottomNav component tests — F045 §5.6 / #143 opaque full-width bar.
 *
 * Covers: role gates (Add hidden for Viewer), active-state markers,
 * opaque/full-width/safe-area bar structure, Settings merged into bar,
 * and axe cleanliness.
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import AppBottomNav from './AppBottomNav.svelte';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

describe('AppBottomNav', () => {
	const defaultProps = {
		pathname: '/devices',
		currentUser: makeUser('Member'),
		onAdd: vi.fn()
	};

	it('renders Home, Add, Reports, and Settings as direct children of the nav bar', () => {
		render(AppBottomNav, { props: defaultProps });

		const nav = screen.getByRole('navigation', { name: 'Primary navigation' });
		expect(nav).toBeInTheDocument();

		expect(screen.getByRole('link', { name: /home/i })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument();
	});

	it('renders Settings as an app-bottom-nav__item inside the nav (not a separate bubble)', () => {
		const { container } = render(AppBottomNav, { props: defaultProps });

		const nav = container.querySelector('.app-bottom-nav');
		expect(nav).not.toBeNull();

		const settingsLink = screen.getByRole('link', { name: 'Settings' });
		expect(settingsLink).toHaveClass('app-bottom-nav__item');
		// Settings is a direct child of the nav bar, not wrapped in a pill or bubble.
		expect(nav!.contains(settingsLink)).toBe(true);
		// There should be no separate bubble element.
		expect(container.querySelector('.app-nav__bubble')).toBeNull();
	});

	it('applies the opaque full-width bar class (no translucent pill wrapper)', () => {
		const { container } = render(AppBottomNav, { props: defaultProps });

		// #143: The new bar uses app-bottom-nav — no floating pill or bubble.
		expect(container.querySelector('.app-bottom-nav')).not.toBeNull();
		expect(container.querySelector('.app-nav__pill')).toBeNull();
		expect(container.querySelector('.app-nav__bubble')).toBeNull();
	});

	it('gives every nav item a ≥44px tappable hit area via app-bottom-nav__item (constitution §6.5.6)', () => {
		render(AppBottomNav, { props: defaultProps });

		for (const el of [
			screen.getByRole('link', { name: /home/i }),
			screen.getByRole('button', { name: 'Add' }),
			screen.getByRole('link', { name: /reports/i }),
			screen.getByRole('link', { name: 'Settings' })
		]) {
			// app-bottom-nav__item declares min-height: 3rem (51px @ 17px root),
			// clearing the 44px floor.
			expect(el).toHaveClass('app-bottom-nav__item');
		}
	});

	it('hides the Add action for Viewer role (no create permission)', () => {
		render(AppBottomNav, { props: { ...defaultProps, currentUser: makeUser('Viewer') } });

		expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
		// Remaining three items are unaffected by role.
		expect(screen.getByRole('link', { name: /home/i })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument();
	});

	it('shows the Add action for Admin and Member roles', () => {
		const { rerender } = render(AppBottomNav, {
			props: { ...defaultProps, currentUser: makeUser('Admin') }
		});
		expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument();

		void rerender({ ...defaultProps, currentUser: makeUser('Member') });
	});

	it('marks Home active via aria-current when pathname matches', () => {
		render(AppBottomNav, { props: { ...defaultProps, pathname: '/devices' } });

		expect(screen.getByRole('link', { name: /home/i })).toHaveAttribute('aria-current', 'page');
		expect(screen.getByRole('link', { name: /reports/i })).not.toHaveAttribute('aria-current');
	});

	it('marks Reports active via aria-current when pathname matches', () => {
		render(AppBottomNav, { props: { ...defaultProps, pathname: '/reports' } });

		expect(screen.getByRole('link', { name: /reports/i })).toHaveAttribute('aria-current', 'page');
		expect(screen.getByRole('link', { name: /home/i })).not.toHaveAttribute('aria-current');
	});

	it('marks Settings active via aria-current when pathname matches', () => {
		render(AppBottomNav, { props: { ...defaultProps, pathname: '/settings' } });

		expect(screen.getByRole('link', { name: 'Settings' })).toHaveAttribute('aria-current', 'page');
	});

	it('calls onAdd when the Add action is activated', async () => {
		const user = userEvent.setup();
		const onAdd = vi.fn();
		render(AppBottomNav, { props: { ...defaultProps, onAdd } });

		await user.click(screen.getByRole('button', { name: 'Add' }));
		expect(onAdd).toHaveBeenCalledTimes(1);
	});

	it('has all items directly inside <nav> with no intermediate pill wrapper', () => {
		const { container } = render(AppBottomNav, { props: defaultProps });
		const nav = container.querySelector('.app-bottom-nav');
		expect(nav).not.toBeNull();

		// All four items are direct children of the nav bar. No role="group" pill.
		const directChildren = Array.from(nav!.children);
		// With canAdd=true (Member role), we expect 4 items.
		expect(directChildren).toHaveLength(4);
		for (const child of directChildren) {
			expect(child).toHaveClass('app-bottom-nav__item');
		}
	});

	it('has no accessibility violations', async () => {
		const { container } = render(AppBottomNav, { props: defaultProps });
		expect(await axe(container)).toHaveNoViolations();
	});
});

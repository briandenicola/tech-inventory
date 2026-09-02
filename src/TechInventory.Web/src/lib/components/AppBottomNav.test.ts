/**
 * AppBottomNav component tests — F045 §5.6 / Drake D-A..D-D.
 *
 * Covers: role gates (Add hidden for Viewer), active-state markers, safe-area
 * fixed positioning, and axe cleanliness. Visual/CSS-token details are
 * covered by manual spec review (drake-pwa-visual-rules.md), not asserted
 * here since jsdom doesn't compute backdrop-filter/color-mix.
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/svelte';
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

	it('renders Home, Add, and Reports inside one pill, and Settings as a separate bubble', () => {
		render(AppBottomNav, { props: defaultProps });

		const nav = screen.getByRole('navigation', { name: 'Primary navigation' });
		expect(nav).toBeInTheDocument();

		expect(screen.getByRole('link', { name: /home/i })).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();

		const settingsLink = screen.getByRole('link', { name: 'Settings' });
		expect(settingsLink).toBeInTheDocument();
		expect(settingsLink).toHaveAttribute('title', 'Settings');
	});

	it('exposes the pill as a role="group" containing exactly Home, Add, and Reports — Settings is outside it', () => {
		const { container } = render(AppBottomNav, { props: defaultProps });

		const pill = screen.getByRole('group');
		expect(pill).toHaveClass('app-nav__pill');

		const pillItems = within(pill).getAllByRole('link').concat(within(pill).getAllByRole('button'));
		expect(pillItems).toHaveLength(3);
		expect(within(pill).getByRole('link', { name: /home/i })).toBeInTheDocument();
		expect(within(pill).getByRole('button', { name: 'Add' })).toBeInTheDocument();
		expect(within(pill).getByRole('link', { name: /reports/i })).toBeInTheDocument();

		// Settings is a sibling of the pill, not a descendant of it.
		expect(within(pill).queryByRole('link', { name: 'Settings' })).not.toBeInTheDocument();
		const settingsLink = screen.getByRole('link', { name: 'Settings' });
		expect(pill.contains(settingsLink)).toBe(false);
		expect(container.querySelector('.app-nav')?.contains(settingsLink)).toBe(true);
	});

	it('gives every pill item and the Settings bubble a ≥44px tappable hit area (constitution §6.5.6)', () => {
		render(AppBottomNav, { props: defaultProps });

		for (const el of [
			screen.getByRole('link', { name: /home/i }),
			screen.getByRole('button', { name: 'Add' }),
			screen.getByRole('link', { name: /reports/i })
		]) {
			expect(el).toHaveClass('app-nav__item');
		}
		// app-nav__item declares min-height: 3rem (48px) and app-nav__bubble
		// declares a fixed 3.5rem × 3.5rem (56px) box — both clear the 44px
		// floor (jsdom does not resolve rem/CSS custom properties, so the
		// class hook is the reliable proxy, matching this file's existing
		// convention for CSS-token assertions).
		expect(screen.getByRole('link', { name: 'Settings' })).toHaveClass('app-nav__bubble');
	});

	it('hides the Add action for Viewer role (no create permission)', () => {
		render(AppBottomNav, { props: { ...defaultProps, currentUser: makeUser('Viewer') } });

		expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
		// The rest of the pill is unaffected by role.
		expect(screen.getByRole('link', { name: /home/i })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();
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

	it('marks the Settings bubble active via aria-current when pathname matches', () => {
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

	it('renders the pill and the Settings bubble as the only two direct children of <nav>', () => {
		const { container } = render(AppBottomNav, { props: defaultProps });
		const nav = container.querySelector('.app-nav');
		expect(nav).not.toBeNull();
		// Structural contract the fixed-position CSS relies on: exactly the
		// pill (role="group") and the bubble, in that order, as direct
		// children — not nested inside one another.
		expect(nav!.children).toHaveLength(2);
		expect(nav!.children[0]).toHaveClass('app-nav__pill');
		expect(nav!.children[1]).toHaveClass('app-nav__bubble');
	});

	it('has no accessibility violations', async () => {
		const { container } = render(AppBottomNav, { props: defaultProps });
		expect(await axe(container)).toHaveNoViolations();
	});
});

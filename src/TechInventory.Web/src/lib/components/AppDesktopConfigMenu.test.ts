/**
 * AppDesktopConfigMenu component tests — #134 / #144.
 *
 * Covers: trigger aria attributes, role-gated rendering, item order + active
 * state, keyboard roving focus (Arrow/Home/End), Escape closes + returns
 * focus, outside-click closes, route-change auto-close, and axe cleanliness.
 */
import { describe, it, expect } from 'vitest';
import { render, screen, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import AppDesktopConfigMenu from './AppDesktopConfigMenu.svelte';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

const adminProps = {
	pathname: '/devices',
	currentUser: makeUser('Admin')
};

describe('AppDesktopConfigMenu', () => {
	it('renders nothing for non-Admin roles (Viewer and Member have no admin items)', () => {
		const { container: viewerContainer } = render(AppDesktopConfigMenu, {
			props: { ...adminProps, currentUser: makeUser('Viewer') }
		});
		expect(viewerContainer.querySelector('button')).not.toBeInTheDocument();

		const { container: memberContainer } = render(AppDesktopConfigMenu, {
			props: { ...adminProps, currentUser: makeUser('Member') }
		});
		expect(memberContainer.querySelector('button')).not.toBeInTheDocument();
	});

	it('renders the trigger for an Admin with correct aria attributes (closed)', () => {
		render(AppDesktopConfigMenu, { props: adminProps });

		const trigger = screen.getByRole('button', { name: 'Configuration' });
		expect(trigger).toHaveAttribute('aria-expanded', 'false');
		expect(trigger).toHaveAttribute('aria-haspopup', 'menu');
		expect(trigger).toHaveAttribute('aria-controls', 'desktop-config-menu');
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('opens the panel on click, raises aria-expanded, and focuses the first item', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });

		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		const trigger = screen.getByRole('button', { name: 'Configuration' });
		expect(trigger).toHaveAttribute('aria-expanded', 'true');

		const menu = screen.getByRole('menu');
		expect(menu).toBeInTheDocument();
		const items = within(menu).getAllByRole('menuitem');
		expect(items[0]).toHaveFocus();
	});

	it('shows all six admin items for an Admin in alphabetical/configured order', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		const labels = items.map((el) => el.textContent?.trim());

		expect(labels).toEqual(['Brands', 'Categories', 'Locations', 'Networks', 'Owners', 'Tags']);
	});

	it('marks the active item with aria-current="page" when pathname matches', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, {
			props: { pathname: '/admin/brands', currentUser: makeUser('Admin') }
		});
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		const brandsLink = screen.getByRole('menuitem', { name: 'Brands' });
		expect(brandsLink).toHaveAttribute('aria-current', 'page');

		const categoriesLink = screen.getByRole('menuitem', { name: 'Categories' });
		expect(categoriesLink).not.toHaveAttribute('aria-current');
	});

	it('closes on Escape and returns focus to the trigger', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });

		const trigger = screen.getByRole('button', { name: 'Configuration' });
		await user.click(trigger);
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await user.keyboard('{Escape}');

		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Configuration' })).toHaveFocus();
	});

	it('closes on outside click', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });
		document.body.insertAdjacentHTML('beforeend', '<button id="outside-cfg">outside</button>');

		await user.click(screen.getByRole('button', { name: 'Configuration' }));
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await user.click(document.getElementById('outside-cfg')!);
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('supports ArrowDown/ArrowUp/Home/End roving focus across items', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		expect(items[0]).toHaveFocus();

		await user.keyboard('{ArrowDown}');
		expect(items[1]).toHaveFocus();

		await user.keyboard('{End}');
		expect(items[items.length - 1]).toHaveFocus();

		await user.keyboard('{Home}');
		expect(items[0]).toHaveFocus();

		await user.keyboard('{ArrowUp}');
		expect(items[items.length - 1]).toHaveFocus();
	});

	it('closes when the pathname prop changes (route navigation)', async () => {
		const user = userEvent.setup();
		const { rerender } = render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await rerender({ ...adminProps, pathname: '/admin/categories' });
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('closes the menu when a menu item is clicked', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		await user.click(screen.getByRole('menuitem', { name: 'Brands' }));
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('has no accessibility violations with the panel open', async () => {
		const user = userEvent.setup();
		const { container } = render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		expect(await axe(container)).toHaveNoViolations();
	});

	it('renders leading icon SVGs (aria-hidden) inside each menu item', async () => {
		const user = userEvent.setup();
		render(AppDesktopConfigMenu, { props: adminProps });
		await user.click(screen.getByRole('button', { name: 'Configuration' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		items.forEach((item) => {
			const svg = item.querySelector('svg');
			expect(svg).not.toBeNull();
			expect(svg).toHaveAttribute('aria-hidden', 'true');
		});
	});
});

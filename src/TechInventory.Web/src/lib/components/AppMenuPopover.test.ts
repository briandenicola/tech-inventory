/**
 * AppMenuPopover component tests — F045 §5.3 / Drake D-E.
 *
 * Covers: trigger glyph/label swap, item order + role gates (mirrors the
 * former desktop dropdown exactly), keyboard model lifted from
 * DeviceActionsMenu (Escape closes + returns focus, outside click closes),
 * the onOpenChange callback the layout uses for its header z-index fix, and
 * axe cleanliness with the panel open.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import AppMenuPopover from './AppMenuPopover.svelte';
import type { CurrentUser } from '$lib/stores/auth';

function makeUser(role: CurrentUser['role']): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Test User', role };
}

describe('AppMenuPopover', () => {
	// The panel embeds ThemeToggle, which reads window.matchMedia for the
	// system-preference listener; jsdom doesn't implement it.
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

	it('renders a closed trigger with the "Menu" label and hamburger glyph', () => {
		render(AppMenuPopover, { props: defaultProps });

		const trigger = screen.getByRole('button', { name: 'Menu' });
		expect(trigger).toHaveAttribute('aria-expanded', 'false');
		expect(trigger).toHaveAttribute('aria-haspopup', 'menu');
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('opens the panel and swaps the trigger to "Close menu" on click, focusing the first item', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: defaultProps });

		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const trigger = screen.getByRole('button', { name: 'Close menu' });
		expect(trigger).toHaveAttribute('aria-expanded', 'true');

		const menu = screen.getByRole('menu');
		expect(menu).toBeInTheDocument();
		const items = within(menu).getAllByRole('menuitem');
		expect(items[0]).toHaveFocus();
	});

	it('calls onOpenChange with true/false as the panel opens and closes', async () => {
		const user = userEvent.setup();
		const onOpenChange = vi.fn();
		render(AppMenuPopover, { props: { ...defaultProps, onOpenChange } });

		const trigger = screen.getByRole('button', { name: 'Menu' });
		await user.click(trigger);
		expect(onOpenChange).toHaveBeenLastCalledWith(true);

		await user.click(screen.getByRole('button', { name: 'Close menu' }));
		expect(onOpenChange).toHaveBeenLastCalledWith(false);
	});

	it('closes on Escape and returns focus to the trigger', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: defaultProps });

		const trigger = screen.getByRole('button', { name: 'Menu' });
		await user.click(trigger);
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await user.keyboard('{Escape}');

		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'Menu' })).toHaveFocus();
	});

	it('closes on outside click', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: defaultProps });
		document.body.insertAdjacentHTML('beforeend', '<button id="outside">outside</button>');

		await user.click(screen.getByRole('button', { name: 'Menu' }));
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await user.click(document.getElementById('outside')!);
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('renders items for an Admin in the exact order: primary → admin heading/items → Settings → Sign out', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: { ...defaultProps, currentUser: makeUser('Admin') } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		const labels = items.map((el) => el.textContent?.trim());

		expect(labels).toEqual([
			'Devices',
			'Reports',
			'Import',
			'Export',
			'Audit Log',
			'Brands',
			'Categories',
			'Locations',
			'Networks',
			'Owners',
			'Tags',
			'Settings',
			'Sign Out'
		]);
		expect(screen.getByText('Test User')).toBeInTheDocument();
	});

	it('labels the reference-data section heading "Configuration", not "ADMIN" (#139); role badge is unaffected', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: { ...defaultProps, currentUser: makeUser('Admin') } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const menu = screen.getByRole('menu');
		expect(within(menu).getByText('Configuration')).toBeInTheDocument();
		expect(within(menu).queryByText('ADMIN')).not.toBeInTheDocument();
		// The role badge next to the user's name still reads "Admin" — role
		// naming is out of scope for #139 and must remain unchanged.
		expect(screen.getByText('Test User')).toBeInTheDocument();
		expect(within(menu).getByText('Admin')).toBeInTheDocument();
	});

	it('hides Admin-only and Member-only items for a Viewer', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: { ...defaultProps, currentUser: makeUser('Viewer') } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		const labels = items.map((el) => el.textContent?.trim());

		expect(labels).toEqual(['Devices', 'Reports', 'Settings', 'Sign Out']);
	});

	it('shows Imports (Member-visible) but hides Admin-only items for a Member', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: { ...defaultProps, currentUser: makeUser('Member') } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		const menu = screen.getByRole('menu');
		const items = within(menu).getAllByRole('menuitem');
		const labels = items.map((el) => el.textContent?.trim());

		expect(labels).toEqual(['Devices', 'Reports', 'Import', 'Settings', 'Sign Out']);
	});

	it('supports ArrowDown/ArrowUp/Home/End roving focus across items', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: { ...defaultProps, currentUser: makeUser('Viewer') } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

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

	it('renders the panel with a scrollable max-height container', async () => {
		const user = userEvent.setup();
		render(AppMenuPopover, { props: defaultProps });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		// The scroll/max-height sizing lives on the outer popover panel, one
		// level above the role="menu" region itself (see AppMenuPopover.svelte:
		// the menu boundary excludes ThemeToggle, so it can't also own the
		// panel's positioning/scroll styles).
		const panel = document.getElementById('app-menu-popover');
		expect(panel?.className).toContain('max-h-[70vh]');
		expect(panel?.className).toContain('overflow-y-auto');
	});

	it('closes the menu when pathname changes (route navigation)', async () => {
		const user = userEvent.setup();
		const { rerender } = render(AppMenuPopover, { props: defaultProps });
		await user.click(screen.getByRole('button', { name: 'Menu' }));
		expect(screen.getByRole('menu')).toBeInTheDocument();

		await rerender({ ...defaultProps, pathname: '/reports' });
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('calls onSignOut and closes the menu when Sign Out is activated', async () => {
		const user = userEvent.setup();
		const onSignOut = vi.fn();
		render(AppMenuPopover, { props: { ...defaultProps, onSignOut } });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		await user.click(screen.getByRole('menuitem', { name: 'Sign Out' }));

		expect(onSignOut).toHaveBeenCalledTimes(1);
		expect(screen.queryByRole('menu')).not.toBeInTheDocument();
	});

	it('has no accessibility violations with the panel open', async () => {
		const user = userEvent.setup();
		const { container } = render(AppMenuPopover, { props: defaultProps });
		await user.click(screen.getByRole('button', { name: 'Menu' }));

		expect(await axe(container)).toHaveNoViolations();
	});
});

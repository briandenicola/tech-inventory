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
		// max-h raised from 70vh → 85vh (#144: more room for compact admin menu).
		const panel = document.getElementById('app-menu-popover');
		expect(panel?.className).toContain('max-h-[85vh]');
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

	describe('menu row geometry parity (#144 correction)', () => {
		// Reopened #144: field validation found the active-item highlight
		// visually inflating row height/whitespace relative to inactive rows.
		// This locks the fix in place with a class-contract comparison —
		// jsdom cannot compute real layout, so we assert on the Tailwind
		// utility classes that *would* produce that geometry instead.

		// Any class matching one of these prefixes is a colour-only affordance
		// (background/text/ring tint) that active vs inactive rows are allowed
		// to differ on. Everything else must be identical between the two, or
		// one state is carrying its own box geometry.
		const COLOR_ONLY_PREFIXES = [
			'bg-',
			'text-',
			'dark:bg-',
			'dark:text-',
			'hover:bg-',
			'dark:hover:bg-'
		];

		function rowClassList(el: Element): string[] {
			return (el.getAttribute('class') ?? '').split(/\s+/).filter(Boolean);
		}

		function getRow(name: string) {
			return screen.getByRole('menuitem', { name });
		}

		async function openMenu() {
			const user = userEvent.setup();
			render(AppMenuPopover, { props: defaultProps });
			await user.click(screen.getByRole('button', { name: 'Menu' }));
		}

		it('gives the active row (Devices) and an inactive row (Reports) the exact same non-colour class set', async () => {
			await openMenu();

			const activeClasses = rowClassList(getRow('Devices'));
			const inactiveClasses = rowClassList(getRow('Reports'));

			const isColorOnly = (cls: string) => COLOR_ONLY_PREFIXES.some((prefix) => cls.startsWith(prefix));

			const activeOnly = activeClasses.filter((c) => !inactiveClasses.includes(c));
			const inactiveOnly = inactiveClasses.filter((c) => !activeClasses.includes(c));

			// Every class that differs between the two rows must be a
			// colour-only utility. Anything else (padding, margin, min-height/
			// height, gap, radius, border-width, scale/transform, width, etc.)
			// means the active state is carrying its own box geometry again.
			for (const cls of [...activeOnly, ...inactiveOnly]) {
				expect(isColorOnly(cls), `unexpected non-colour class diff: "${cls}"`).toBe(true);
			}

			const activeGeometry = activeClasses.filter((c) => !isColorOnly(c)).sort();
			const inactiveGeometry = inactiveClasses.filter((c) => !isColorOnly(c)).sort();
			expect(activeGeometry).toEqual(inactiveGeometry);
		});

		it('gives every row the same shared compact geometry: >=44px min-height, standard gap, standard radius', async () => {
			await openMenu();

			for (const name of ['Devices', 'Reports', 'Settings']) {
				const classes = rowClassList(getRow(name));
				expect(classes).toContain('min-h-11'); // >= 44px touch target (WCAG 2.5.8)
				expect(classes).toContain('gap-2'); // icon/label gap
				expect(classes).toContain('rounded-lg'); // standard compact radius, not an oversized pill
				expect(classes).not.toContain('rounded-xl');
				expect(classes).not.toContain('rounded-full');
			}
		});

		it('places a single consistent token-scale gap on the role="menu" container instead of per-row/state spacing', async () => {
			await openMenu();

			const menu = screen.getByRole('menu');
			const menuClasses = rowClassList(menu);
			expect(menuClasses).toContain('flex-col');
			expect(menuClasses.some((c) => /^gap-\d/.test(c))).toBe(true);
		});

		it('distinguishes active from inactive state visually (Devices carries active tint, Reports does not)', async () => {
			await openMenu();

			expect(getRow('Devices').className).toContain('bg-primary-50');
			expect(getRow('Reports').className).not.toContain('bg-primary-50');
		});

		it('has no accessibility violations with the panel open (row geometry regression scope)', async () => {
			const user = userEvent.setup();
			const { container } = render(AppMenuPopover, { props: defaultProps });
			await user.click(screen.getByRole('button', { name: 'Menu' }));

			expect(await axe(container)).toHaveNoViolations();
		});
	});
});

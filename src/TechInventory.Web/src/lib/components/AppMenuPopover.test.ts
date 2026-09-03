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

	describe('menu row geometry parity (#144 R2 correction)', () => {
		// Reopened #144 / rejected #166 visual result: the active-item highlight
		// still visually inflated the selected row. #166 used `min-h-11 py-1.5`
		// — the internal 12px vertical padding (6px top + bottom) created blank
		// zones within the 44px coloured box, making it visually "bubble" against
		// inactive rows whose transparent background hid the same blank zones.
		// R2 fix: `h-11` (exact 44px, NO py-*) locks both states to the same
		// exact visible box. This suite locks that contract in place and MUST
		// fail if any of the following regressions appear:
		//   · active-only vertical geometry (padding, margin, height, min-height)
		//   · list gap wider than gap-0.5
		//   · return of min-h-11 or py-1.5 that can grow the box beyond 44px
		//
		// Tamper evidence (R2): added `class:h-14={active}` to the Devices <a>
		// in AppMenuPopover.svelte → test "exact same non-colour class set" failed
		// with `unexpected non-colour class diff: "h-14"`. Restored → 19/19 green.
		// Also increased container to `gap-2` → test "compact container gap"
		// failed: expected `gap-0.5` but class list had `gap-2`. Restored → green.
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

		it('gives every row the same shared compact geometry: exact 44px block height, standard gap, standard radius, no growth-inducing padding', async () => {
			await openMenu();

			for (const name of ['Devices', 'Reports', 'Settings']) {
				const classes = rowClassList(getRow(name));
				// R2: exact h-11, NOT min-h-11 — prevents growth beyond 44px via padding
				expect(classes).toContain('h-11'); // exact 44px block height (WCAG 2.5.8 touch target)
				expect(classes).not.toContain('min-h-11'); // must not regress to #166's min-h approach
				expect(classes).not.toContain('py-1.5'); // must not carry internal vertical padding that inflates the box
				expect(classes).not.toContain('py-2'); // guard against any re-added vertical padding
				expect(classes).toContain('gap-2'); // icon/label gap
				expect(classes).toContain('rounded-lg'); // standard compact radius, not an oversized pill
				expect(classes).not.toContain('rounded-xl');
				expect(classes).not.toContain('rounded-full');
			}
		});

		it('places a compact gap-0.5 (2px) on the role="menu" container — smallest token gap, tighter than #166 gap-1', async () => {
			await openMenu();

			const menu = screen.getByRole('menu');
			const menuClasses = rowClassList(menu);
			expect(menuClasses).toContain('flex-col');
			// R2: gap-0.5 (2px) — smallest useful Tailwind gap. Must not regress to gap-1 or wider.
			expect(menuClasses).toContain('gap-0.5');
			expect(menuClasses).not.toContain('gap-1');
			expect(menuClasses).not.toContain('gap-2');
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

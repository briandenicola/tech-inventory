/**
 * DevicePwaRow — #170 ellipsis right-align structural contract tests.
 *
 * Proves the two-column grid layout (minmax(0,1fr) auto) that anchors the
 * action column to the right edge regardless of text length.
 *
 * JSDOM LIMITATION: jsdom cannot measure rendered pixel positions or evaluate
 * CSS rules from stylesheets (computed style returns '' for class-based
 * properties). These tests verify the structural DOM contract — class
 * presence, child order, and touch-target classes — that guarantees
 * right-alignment in a real browser. Manual pixel verification is required;
 * see M-26 in docs/testing/manual-pwa-validation.md.
 *
 * The five devices match Brian's exact screenshot rows so that manual
 * verification (M-26) uses the same labels as this file.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import DevicePwaRow from './DevicePwaRow.svelte';
import { createDeviceResponse, resetFactories } from '$lib/test-utils/factories';
import type { CurrentUser } from '$lib/stores/auth';

const mocks = vi.hoisted(() => ({
	bulkUpdate: vi.fn(),
	showToast: vi.fn()
}));

vi.mock('$lib/api/client', () => ({
	devices: { bulkUpdate: mocks.bulkUpdate }
}));

vi.mock('$lib/stores/toast', () => ({
	showToast: mocks.showToast
}));

function makeAdmin(): CurrentUser {
	return { id: 'user-1', entraObjectId: null, displayName: 'Admin User', role: 'Admin' };
}

/** Exact devices from Brian's screenshot and M-26 manual checklist. */
const FIXTURE_DEVICES = [
	{ name: 'Mohu Leaf Stitch 60m Range', subtitle: 'Mohu · Leaf Stitch' },
	{ name: '4-Door French Door Fridge', subtitle: 'Samsung · RF28JBEDBSG/AA' },
	{ name: 'Air Fryer', subtitle: 'Ninja · DZ400 A' },
	{ name: 'Aqua Flosser', subtitle: 'AquaSonic · Aqua Flosser PRO' },
	{ name: 'Coffee Maker', subtitle: 'Keurig · K-Classic' },
] as const;

describe('DevicePwaRow — #170 ellipsis right-align structural contract', () => {
	beforeEach(() => {
		resetFactories();
		mocks.bulkUpdate.mockReset();
		mocks.showToast.mockReset();
	});

	/**
	 * Full-width wrapper: a block-level CSS grid container always stretches
	 * to its containing block width. jsdom confirms the element exists;
	 * a real browser enforces full width.
	 */
	it('renders a .pwa-row grid container for every device regardless of title length', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, { props: { device, currentUser: null } });

			expect(
				container.querySelector('.pwa-row'),
				`expected .pwa-row for "${fixture.name}"`
			).not.toBeNull();
			unmount();
		}
	});

	/**
	 * Full-width wrapper: `.pwa-row` also carries an explicit `w-full`
	 * Tailwind class (in addition to being a block-level grid container),
	 * so the wrapper contract is asserted structurally rather than relying
	 * only on default block-level sizing.
	 */
	it('wrapper carries explicit w-full class for every device regardless of title length', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, { props: { device, currentUser: null } });

			const row = container.querySelector('.pwa-row');
			expect(row?.classList.contains('w-full'), `"${fixture.name}": .pwa-row must carry w-full`).toBe(true);
			unmount();
		}
	});

	/**
	 * Two-column grid when non-selectable: the base grid-template-columns is
	 * `minmax(0, 1fr) auto`. The text column fills all available space; the
	 * action column is auto-sized (fixed width). No text, however short,
	 * can displace the action column from the right edge.
	 *
	 * Structural proof: the row must NOT carry `.pwa-row--selectable`
	 * (which would switch to the 3-column template `auto minmax(0,1fr) auto`).
	 * jsdom cannot evaluate stylesheet rules, so the class acts as a proxy
	 * for the grid-template-columns value.
	 */
	it('non-selectable rows omit .pwa-row--selectable, proving 2-col grid with fixed-right action', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, {
				props: { device, currentUser: makeAdmin(), selectable: false }
			});

			const row = container.querySelector('.pwa-row');
			expect(
				row?.classList.contains('pwa-row--selectable'),
				`"${fixture.name}": must NOT carry pwa-row--selectable when selectable=false`
			).toBe(false);
			unmount();
		}
	});

	/**
	 * Three-column grid when selectable: `.pwa-row--selectable` switches the
	 * template to `auto minmax(0,1fr) auto` for the checkbox, text, and action
	 * columns. The action remains in the rightmost auto-sized column.
	 */
	it('selectable rows carry .pwa-row--selectable for 3-col grid (checkbox + text + action)', () => {
		const device = createDeviceResponse({ name: 'Air Fryer' });
		const { container } = render(DevicePwaRow, {
			props: { device, currentUser: null, selectable: true, onToggleSelect: vi.fn() }
		});

		expect(container.querySelector('.pwa-row')?.classList.contains('pwa-row--selectable')).toBe(true);
	});

	/**
	 * min-w-0 on the text column: allows the text container to shrink below its
	 * intrinsic size and truncate with ellipsis. Without min-w-0 a flex/grid
	 * child's minimum size defaults to `auto` (its content width), preventing
	 * shrinkage and pushing the action column to the right.
	 */
	it('text column carries min-w-0 enabling truncation without displacing the action column', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, { props: { device, currentUser: null } });

			const row = container.querySelector('.pwa-row');
			expect(
				row?.querySelector('[class*="min-w-0"]'),
				`"${fixture.name}": text column must carry min-w-0`
			).not.toBeNull();
			unmount();
		}
	});

	/**
	 * Action wrapper is always the last direct child: it occupies the
	 * rightmost grid cell (the `auto` column) for every row regardless of
	 * title length. This is the DOM-observable proof of the fixed-right-edge
	 * contract.
	 */
	it('action column wrapper (.self-center) is always the last direct child of .pwa-row', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, {
				props: { device, currentUser: makeAdmin() }
			});

			const row = container.querySelector('.pwa-row');
			const children = Array.from(row?.children ?? []);
			const last = children.at(-1);
			expect(
				last?.classList.contains('self-center'),
				`"${fixture.name}": last child of .pwa-row must be the action wrapper (self-center)`
			).toBe(true);
			unmount();
		}
	});

	/**
	 * Exactly 2 direct grid children (text + action) when non-selectable.
	 * Confirms there is no phantom third column receiving the action element.
	 */
	it('non-selectable row has exactly 2 direct grid children (text, action)', () => {
		const device = createDeviceResponse({ name: 'Air Fryer' });
		const { container } = render(DevicePwaRow, {
			props: { device, currentUser: makeAdmin(), selectable: false }
		});

		const row = container.querySelector('.pwa-row');
		expect(Array.from(row?.children ?? [])).toHaveLength(2);
	});

	/**
	 * Exactly 3 direct grid children (checkbox + text + action) when selectable.
	 */
	it('selectable row has exactly 3 direct grid children (checkbox, text, action)', () => {
		const device = createDeviceResponse({ name: 'Air Fryer' });
		const { container } = render(DevicePwaRow, {
			props: { device, currentUser: makeAdmin(), selectable: true, onToggleSelect: vi.fn() }
		});

		const row = container.querySelector('.pwa-row');
		expect(Array.from(row?.children ?? [])).toHaveLength(3);
	});

	/**
	 * ≥44px touch target: the action trigger carries `h-11 w-11` (Tailwind),
	 * which resolves to 2.75 rem = 44 px at default font-size. jsdom cannot
	 * measure rendered pixels; the class presence is a structural proxy.
	 * Compact ghost style (rounded-lg, no border circle) is confirmed by the
	 * absence of `rounded-full` and `border` classes — per the compact prop.
	 */
	it('action trigger has ≥44px touch-target classes (h-11 w-11) with compact ghost style (no rounded-full)', () => {
		const device = createDeviceResponse({ name: 'Aqua Flosser' });
		const { container } = render(DevicePwaRow, { props: { device, currentUser: makeAdmin() } });

		const trigger = container.querySelector('button[aria-haspopup="menu"]');
		expect(trigger?.classList.contains('h-11'), 'h-11 (44px height)').toBe(true);
		expect(trigger?.classList.contains('w-11'), 'w-11 (44px width)').toBe(true);
		// compact=true renders rounded-lg ghost, not the rounded-full bordered circle
		expect(trigger?.classList.contains('rounded-full'), 'must NOT be rounded-full in compact mode').toBe(false);
	});

	/**
	 * Common structural right-edge contract across all five fixture rows:
	 * identical class pattern + action as last child = same CSS grid column
	 * rules applied = same computed right edge in a real browser.
	 *
	 * NOTE: jsdom cannot measure pixel positions. Manual validation is required
	 * — see M-26 in docs/testing/manual-pwa-validation.md.
	 */
	it('all five fixture rows share the same grid class contract — common structural right-edge guarantee', () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, {
				props: { device, currentUser: makeAdmin(), selectable: false }
			});

			const row = container.querySelector('.pwa-row');
			expect(row?.classList.contains('pwa-row'), `"${fixture.name}": must have .pwa-row`).toBe(true);
			expect(
				row?.classList.contains('pwa-row--selectable'),
				`"${fixture.name}": must NOT have .pwa-row--selectable`
			).toBe(false);

			const children = Array.from(row?.children ?? []);
			expect(children).toHaveLength(2);
			expect(
				children.at(-1)?.classList.contains('self-center'),
				`"${fixture.name}": last child must be the action wrapper`
			).toBe(true);
			unmount();
		}
	});

	/**
	 * Axe accessibility: all five fixture rows are clean with the action
	 * trigger rendered (Admin role shows the menu button).
	 */
	it('all five fixture rows are axe-clean with action trigger present', async () => {
		for (const fixture of FIXTURE_DEVICES) {
			const device = createDeviceResponse({ name: fixture.name });
			const { container, unmount } = render(DevicePwaRow, {
				props: { device, currentUser: makeAdmin() }
			});

			expect(await axe(container), `"${fixture.name}": axe violations`).toHaveNoViolations();
			unmount();
		}
	});
});

/**
 * DeviceTableDesktop — field regression guards (#145).
 *
 * Source/class contract tests: jsdom cannot compute sticky layout or
 * text-overflow clipping. Instead we assert that the required CSS classes
 * are present on the rendered DOM nodes. Each assertion is a structural
 * sentinel: it fails precisely when the guarded class is removed or
 * misspelled, without snapshotting the entire class string.
 *
 * TAMPER-TESTED guards (sticky right-0 on actions column — fixed by #145):
 *   Removing sticky/right-0 from the actions <th>/<td> makes those tests
 *   fail; restoring them makes them pass again.
 *
 * TAMPER-TESTED guards (truncate on content cells — fixed by #145):
 *   Removing truncate from a non-name content <td> makes that test fail.
 *
 * TAMPER-TESTED guards (sticky left-0 on name column — already fixed):
 *   Removing sticky/left-0 from the name <th> / <td> makes those tests
 *   fail; restoring them makes them pass again.
 *
 * CAUTION: Never assert a full class string — that is a snapshot, not a
 * contract. Assert only the minimal token(s) that guard the invariant.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/svelte';
import DeviceTableDesktop from './DeviceTableDesktop.svelte';
import { createDeviceList, resetFactories } from '$lib/test-utils/factories';

vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

vi.mock('$lib/stores/referenceData', async () => {
	const { writable } = await import('svelte/store');
	return {
		referenceDataStore: writable({
			brands: [],
			categories: [],
			owners: [],
			locations: [],
			networks: [],
			tags: [],
			isLoading: false,
			error: null
		})
	};
});

const noop = () => {};

describe('DeviceTableDesktop — #145 action-column pin contract', () => {
	beforeEach(() => {
		resetFactories();
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// The actions <th> must carry "sticky" and "right-0" so it remains
	// anchored during horizontal scroll of the table container. Without
	// sticky the column drifts off-screen; without right-0 it anchors to
	// the wrong edge.
	it('actions <th> carries sticky and right-0', () => {
		const devices = createDeviceList(1);
		const { container } = render(DeviceTableDesktop, {
			props: { devices, onSort: noop }
		});

		const allTh = Array.from(container.querySelectorAll('thead th'));
		const actionsTh = allTh.find((th) => th.textContent?.trim() === 'common.labels.actions');

		expect(actionsTh, 'actions <th> must exist in table header').toBeTruthy();
		expect(actionsTh!.className, 'actions <th> must be sticky').toMatch(/\bsticky\b/);
		expect(actionsTh!.className, 'actions <th> must pin to the right edge').toMatch(/\bright-0\b/);
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// The actions data <td> must also be sticky right-0 so each row's
	// action cell tracks the viewport edge during horizontal scroll.
	it('actions data <td> carries sticky and right-0', () => {
		const devices = createDeviceList(1);
		const { container } = render(DeviceTableDesktop, {
			props: { devices, onSort: noop }
		});

		const rows = container.querySelectorAll('tbody tr');
		const firstRow = rows[0];
		expect(firstRow, 'at least one data row must be rendered').toBeTruthy();

		const cells = firstRow!.querySelectorAll('td');
		const actionsTd = cells[cells.length - 1];
		expect(actionsTd?.className, 'actions <td> must be sticky').toMatch(/\bsticky\b/);
		expect(actionsTd?.className, 'actions <td> must pin to right-0').toMatch(/\bright-0\b/);
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The name column (first visible column) header is already sticky left-0
	// per the current source.  Removing "sticky" or "left-0" from the <th>
	// makes this assertion fail; restoring them makes it pass.
	it('name <th> is sticky and pinned left-0 (existing fix, tamper-tested)', () => {
		const devices = createDeviceList(1);
		const { container } = render(DeviceTableDesktop, {
			props: { devices, onSort: noop }
		});

		const allTh = Array.from(container.querySelectorAll('thead th'));
		// First column header (after optional checkbox) is the name column.
		const nameTh = allTh.find((th) =>
			th.className.includes('sticky') && th.className.includes('left-0')
		);
		expect(nameTh, 'name <th> must carry sticky left-0').toBeTruthy();
	});

	// ── TAMPER-TESTED sentinel ────────────────────────────────────────────────
	// The name data <td> is also sticky left-0.  Removing the sticky class
	// from the source makes this test fail.
	it('name data <td> is sticky left-0 (existing fix, tamper-tested)', () => {
		const devices = createDeviceList(1);
		const { container } = render(DeviceTableDesktop, {
			props: { devices, onSort: noop }
		});

		const rows = container.querySelectorAll('tbody tr');
		const cells = rows[0]!.querySelectorAll('td');
		// First td is the name column (no checkbox variant in this render).
		const nameTd = cells[0];
		expect(nameTd?.className, 'name <td> must be sticky left-0').toMatch(/\bsticky\b/);
		expect(nameTd?.className, 'name <td> must pin to left-0').toMatch(/\bleft-0\b/);
	});

	// ── TAMPER-TESTED sentinel ───────────────────────────────────────────────
	// Content cells for non-name columns must carry "truncate" (overflow:hidden
	// + text-overflow:ellipsis) so long values do not push the table wider
	// than the scroll container or occlude adjacent cells.
	it('non-name content <td> carries truncate for overflow control', () => {
		// Render with all columns visible so we get multiple content cells.
		const devices = createDeviceList(1);
		const { container } = render(DeviceTableDesktop, {
			props: { devices, onSort: noop }
		});

		const rows = container.querySelectorAll('tbody tr');
		const cells = Array.from(rows[0]!.querySelectorAll('td'));
		// Skip the first (sticky name) and last (actions) cells; the middle
		// content cells must carry "truncate".
		const contentCells = cells.slice(1, -1);
		expect(contentCells.length, 'expected at least one non-name content cell').toBeGreaterThan(0);
		for (const td of contentCells) {
			expect(td.className, `content <td> must carry truncate, got: ${td.className}`).toMatch(
				/\btruncate\b/
			);
		}
	});
});

/**
 * DeviceTableDesktop — resizable columns.
 *
 * The behaviours worth pinning: a drag changes the column and reports once (not
 * once per pointer move), the keyboard can do everything the pointer can, resizing
 * is confined to the desktop instance, and widths stay inside their bounds.
 *
 * Tamper-checked, each mutation applied and reverted in isolation (16 tests):
 *   · dropped the min/max clamp in columnResize -> "never reports a width below the
 *     minimum" failed (1 failed | 15 passed).
 *   · called onCommit from pointermove as well as pointerup -> "reports a drag once,
 *     on release" failed (1 failed | 15 passed).
 *   · forced the handle gate to always render -> "renders no resize handles unless
 *     resizable is set" failed (1 failed | 15 passed).
 * Green again after each restore.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import DeviceTableDesktop from './DeviceTableDesktop.svelte';
import {
	DEFAULT_TABLE_COLUMN_WIDTHS,
	MIN_TABLE_COLUMN_WIDTH,
	type TableColumnId
} from '$lib/stores/userPrefs';
import { createDeviceResponse } from '$lib/test-utils/factories';

vi.mock('$lib/stores/referenceData', () => ({
	referenceDataStore: { subscribe: (run: (v: unknown) => void) => { run({}); return () => {}; } }
}));

const devices = [createDeviceResponse({ id: 'd1', name: 'Fridge' })];

function baseProps(overrides: Record<string, unknown> = {}) {
	return {
		devices,
		onSort: vi.fn(),
		// Not `as const`: Props takes a mutable TableColumnId[], and a readonly tuple
		// is not assignable to it.
		visibleColumns: ['name', 'model', 'status'] as TableColumnId[],
		...overrides
	};
}

/** jsdom has no layout, so a drag is expressed as explicit pointer coordinates. */
function drag(handle: Element, fromX: number, toX: number) {
	handle.dispatchEvent(
		new PointerEvent('pointerdown', { button: 0, clientX: fromX, pointerId: 1, bubbles: true })
	);
	handle.dispatchEvent(
		new PointerEvent('pointermove', { clientX: toX, pointerId: 1, bubbles: true })
	);
	handle.dispatchEvent(
		new PointerEvent('pointerup', { clientX: toX, pointerId: 1, bubbles: true })
	);
}

describe('DeviceTableDesktop resizable columns', () => {
	beforeEach(() => vi.clearAllMocks());

	it('renders no resize handles unless resizable is set', () => {
		// The same component renders the mobile table view, which must not get grips.
		render(DeviceTableDesktop, { props: baseProps() });

		expect(screen.queryByTestId('resize-name')).not.toBeInTheDocument();
		expect(screen.queryAllByRole('separator')).toHaveLength(0);
	});

	it('renders one handle per visible column when resizable', () => {
		render(DeviceTableDesktop, { props: baseProps({ resizable: true }) });

		expect(screen.getAllByRole('separator')).toHaveLength(3);
		expect(screen.getByTestId('resize-name')).toBeInTheDocument();
		expect(screen.getByTestId('resize-status')).toBeInTheDocument();
	});

	it('exposes the current width through ARIA so the grip is announced, not silent', () => {
		render(DeviceTableDesktop, { props: baseProps({ resizable: true }) });

		const handle = screen.getByTestId('resize-name');
		expect(handle).toHaveAttribute('aria-valuenow', String(DEFAULT_TABLE_COLUMN_WIDTHS.name));
		expect(handle).toHaveAttribute('aria-orientation', 'vertical');
		expect(handle.getAttribute('aria-label')).toMatch(/name/i);
	});

	it('reports a drag once, on release, rather than on every pointer move', () => {
		// Persisting per move would write to localStorage dozens of times per drag.
		const onResizeColumn = vi.fn();
		render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

		drag(screen.getByTestId('resize-name'), 100, 160);

		expect(onResizeColumn).toHaveBeenCalledTimes(1);
		expect(onResizeColumn).toHaveBeenCalledWith('name', DEFAULT_TABLE_COLUMN_WIDTHS.name + 60);
	});

	it('shrinks the column when dragged left', () => {
		const onResizeColumn = vi.fn();
		render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

		drag(screen.getByTestId('resize-name'), 200, 150);

		expect(onResizeColumn).toHaveBeenCalledWith('name', DEFAULT_TABLE_COLUMN_WIDTHS.name - 50);
	});

	it('never reports a width below the minimum, however far left the drag goes', () => {
		const onResizeColumn = vi.fn();
		render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

		drag(screen.getByTestId('resize-status'), 500, -5000);

		expect(onResizeColumn).toHaveBeenCalledWith('status', MIN_TABLE_COLUMN_WIDTH);
	});

	it('ignores a non-primary pointer button, so right-click still opens a context menu', () => {
		const onResizeColumn = vi.fn();
		render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

		const handle = screen.getByTestId('resize-name');
		handle.dispatchEvent(
			new PointerEvent('pointerdown', { button: 2, clientX: 100, pointerId: 1, bubbles: true })
		);
		handle.dispatchEvent(new PointerEvent('pointermove', { clientX: 200, pointerId: 1, bubbles: true }));
		handle.dispatchEvent(new PointerEvent('pointerup', { clientX: 200, pointerId: 1, bubbles: true }));

		expect(onResizeColumn).not.toHaveBeenCalled();
	});

	describe('keyboard parity', () => {
		// A pointer-only resize is unusable without a mouse (WCAG 2.1.1).
		it('grows the column with ArrowRight', async () => {
			const user = userEvent.setup();
			const onResizeColumn = vi.fn();
			render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

			screen.getByTestId('resize-name').focus();
			await user.keyboard('{ArrowRight}');

			expect(onResizeColumn).toHaveBeenCalledWith('name', DEFAULT_TABLE_COLUMN_WIDTHS.name + 16);
		});

		it('shrinks the column with ArrowLeft', async () => {
			const user = userEvent.setup();
			const onResizeColumn = vi.fn();
			render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

			screen.getByTestId('resize-name').focus();
			await user.keyboard('{ArrowLeft}');

			expect(onResizeColumn).toHaveBeenCalledWith('name', DEFAULT_TABLE_COLUMN_WIDTHS.name - 16);
		});

		it('takes a bigger step with Shift held', async () => {
			const user = userEvent.setup();
			const onResizeColumn = vi.fn();
			render(DeviceTableDesktop, { props: baseProps({ resizable: true, onResizeColumn }) });

			screen.getByTestId('resize-name').focus();
			await user.keyboard('{Shift>}{ArrowRight}{/Shift}');

			expect(onResizeColumn).toHaveBeenCalledWith('name', DEFAULT_TABLE_COLUMN_WIDTHS.name + 64);
		});

		it('restores the default with Home', async () => {
			const user = userEvent.setup();
			const onResetColumnWidth = vi.fn();
			render(DeviceTableDesktop, {
				props: baseProps({
					resizable: true,
					onResetColumnWidth,
					columnWidths: { ...DEFAULT_TABLE_COLUMN_WIDTHS, name: 500 }
				})
			});

			screen.getByTestId('resize-name').focus();
			await user.keyboard('{Home}');

			expect(onResetColumnWidth).toHaveBeenCalledWith('name');
		});

		it('is reachable by Tab', async () => {
			const user = userEvent.setup();
			render(DeviceTableDesktop, { props: baseProps({ resizable: true }) });

			const handle = screen.getByTestId('resize-name');
			expect(handle).toHaveAttribute('tabindex', '0');

			handle.focus();
			await user.keyboard('{ArrowRight}');
			expect(handle).toHaveFocus();
		});
	});

	it('restores the default on double-click', async () => {
		const user = userEvent.setup();
		const onResetColumnWidth = vi.fn();
		render(DeviceTableDesktop, {
			props: baseProps({
				resizable: true,
				onResetColumnWidth,
				columnWidths: { ...DEFAULT_TABLE_COLUMN_WIDTHS, name: 500 }
			})
		});

		await user.dblClick(screen.getByTestId('resize-name'));

		expect(onResetColumnWidth).toHaveBeenCalledWith('name');
	});

	it('applies the supplied widths to the colgroup', () => {
		const { container } = render(DeviceTableDesktop, {
			props: baseProps({
				resizable: true,
				columnWidths: { ...DEFAULT_TABLE_COLUMN_WIDTHS, name: 333 }
			})
		});

		const cols = Array.from(container.querySelectorAll('colgroup col'));
		expect(cols.length).toBeGreaterThan(0);
		expect(cols.some((col) => col.getAttribute('style')?.includes('333px'))).toBe(true);
	});

	it('uses fixed layout only when resizable, since auto layout ignores widths', () => {
		const { container: fixed } = render(DeviceTableDesktop, {
			props: baseProps({ resizable: true })
		});
		expect(fixed.querySelector('table')?.className).toContain('table-fixed');

		const { container: auto } = render(DeviceTableDesktop, { props: baseProps() });
		expect(auto.querySelector('table')?.className).not.toContain('table-fixed');
		expect(auto.querySelector('colgroup')).toBeNull();
	});

	it('has no accessibility violations with handles present', async () => {
		const { container } = render(DeviceTableDesktop, { props: baseProps({ resizable: true }) });

		expect(await axe(container)).toHaveNoViolations();
	});
});

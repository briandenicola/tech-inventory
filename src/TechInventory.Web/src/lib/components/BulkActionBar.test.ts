/**
 * BulkActionBar component tests (F024).
 *
 * Verifies visibility gating (hidden when count === 0), accurate count rendering,
 * clear/change/delete callbacks, and that the Delete button is only rendered when
 * an onDelete handler is supplied (Admin-only gate is the caller's responsibility).
 */

import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import BulkActionBar from './BulkActionBar.svelte';

// F045 §5.7: the bar's bottom offset must clear AppBottomNav's pill in
// standalone-PWA mode instead of stacking flush under it. `displayMode` is a
// module-level singleton, so it's mocked here rather than injected as a prop.
let mockIsPwa = false;
vi.mock('$lib/stores/displayMode.svelte', () => ({
	get displayMode() {
		return { isPwa: mockIsPwa };
	}
}));

describe('BulkActionBar', () => {
	afterEach(() => {
		mockIsPwa = false;
	});

	const baseProps = {
		count: 0,
		onClear: vi.fn(),
		onChangeField: vi.fn(),
		onDelete: vi.fn()
	};

	it('renders nothing when count is zero', () => {
		const { container } = render(BulkActionBar, { props: { ...baseProps, count: 0 } });
		expect(container.querySelector('[role="region"]')).toBeNull();
	});

	it('shows the selection count and clear control when count > 0', () => {
		render(BulkActionBar, { props: { ...baseProps, count: 3 } });
		expect(screen.getByText(/3 selected/i)).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /clear selection/i })).toBeInTheDocument();
	});

	it('invokes onChangeField with the matching field when a change button is clicked', async () => {
		const onChangeField = vi.fn();
		const user = userEvent.setup();
		render(BulkActionBar, { props: { ...baseProps, count: 2, onChangeField } });

		await user.click(screen.getByRole('button', { name: /change category/i }));
		await user.click(screen.getByRole('button', { name: /change owner/i }));

		expect(onChangeField).toHaveBeenNthCalledWith(1, 'category');
		expect(onChangeField).toHaveBeenNthCalledWith(2, 'owner');
	});

	it('omits the Delete button when onDelete is not provided', () => {
		render(BulkActionBar, {
			props: { count: 2, onClear: vi.fn(), onChangeField: vi.fn(), onDelete: undefined }
		});
		expect(screen.queryByRole('button', { name: /delete selected/i })).toBeNull();
	});

	it('invokes onDelete when Delete is clicked', async () => {
		const onDelete = vi.fn();
		const user = userEvent.setup();
		render(BulkActionBar, { props: { ...baseProps, count: 4, onDelete } });

		await user.click(screen.getByRole('button', { name: /delete selected/i }));
		expect(onDelete).toHaveBeenCalledOnce();
	});

	it('sits flush with the viewport bottom outside standalone-PWA mode', () => {
		mockIsPwa = false;
		const { container } = render(BulkActionBar, { props: { ...baseProps, count: 1 } });
		expect(container.querySelector('[role="region"]')?.getAttribute('style')).toContain('bottom: 0px');
	});

	it('raises above AppBottomNav in standalone-PWA mode', () => {
		mockIsPwa = true;
		const { container } = render(BulkActionBar, { props: { ...baseProps, count: 1 } });
		// jsdom's CSSOM reformats/reorders multi-term calc()+env() values on
		// assignment, so assert on the distinguishing literal rather than the
		// exact serialized string.
		expect(container.querySelector('[role="region"]')?.getAttribute('style')).toContain('5.5rem');
	});
});

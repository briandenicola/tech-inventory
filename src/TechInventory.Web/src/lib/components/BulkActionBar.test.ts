/**
 * BulkActionBar component tests (F024).
 *
 * Verifies visibility gating (hidden when count === 0), accurate count rendering,
 * clear/change/delete callbacks, and that the Delete button is only rendered when
 * an onDelete handler is supplied (Admin-only gate is the caller's responsibility).
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import BulkActionBar from './BulkActionBar.svelte';

describe('BulkActionBar', () => {
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
});

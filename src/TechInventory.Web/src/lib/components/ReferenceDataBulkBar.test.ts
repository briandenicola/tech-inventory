import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import ReferenceDataBulkBar from './ReferenceDataBulkBar.svelte';

describe('ReferenceDataBulkBar', () => {
	const baseProps = {
		count: 0,
		onClear: vi.fn(),
		onDelete: vi.fn(),
		onMerge: vi.fn()
	};

	it('renders nothing when nothing is selected', () => {
		const { container } = render(ReferenceDataBulkBar, { props: baseProps });
		expect(container.querySelector('[role="region"]')).toBeNull();
	});

	it('shows the current selection count and clear button', () => {
		render(ReferenceDataBulkBar, {
			props: {
				...baseProps,
				count: 3
			}
		});

		expect(screen.getByText(/3 selected/i)).toBeInTheDocument();
		expect(screen.getByRole('button', { name: /clear/i })).toBeInTheDocument();
	});

	it('fires delete, merge, and clear callbacks', async () => {
		const user = userEvent.setup();
		const onClear = vi.fn();
		const onDelete = vi.fn();
		const onMerge = vi.fn();
		render(ReferenceDataBulkBar, {
			props: {
				count: 2,
				onClear,
				onDelete,
				onMerge
			}
		});

		await user.click(screen.getByRole('button', { name: /delete selected/i }));
		await user.click(screen.getByRole('button', { name: /merge selected/i }));
		await user.click(screen.getByRole('button', { name: /clear/i }));

		expect(onDelete).toHaveBeenCalledOnce();
		expect(onMerge).toHaveBeenCalledOnce();
		expect(onClear).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(ReferenceDataBulkBar, {
			props: {
				...baseProps,
				count: 4
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

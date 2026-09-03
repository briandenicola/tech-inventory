/**
 * BulkUpdateModal Component Tests — F024 shared "Change Status" dialog (#127).
 *
 * This modal is the one bulk-update UI reused by both the N-device bulk
 * toolbar (`devices/+page.svelte`) and every single-device "Change Status"
 * surface (`DeviceDetailModal`, `devices/[id]/+page.svelte`, `DevicePwaRow`)
 * — there is exactly one status-transition dialog implementation, not two.
 * Constitution §3.4: axe-core zero violations.
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import BulkUpdateModal from './BulkUpdateModal.svelte';

vi.mock('$lib/i18n', () => ({
	t: (key: string, params?: Record<string, string | number>) =>
		params ? `${key}:${JSON.stringify(params)}` : key
}));

const statusOptions = [
	{ id: 'Active', name: 'Active' },
	{ id: 'Retired', name: 'Retired' },
	{ id: 'Disposed', name: 'Disposed' },
	{ id: 'InRepair', name: 'In Repair' },
	{ id: 'Lent', name: 'Lent' }
];

describe('BulkUpdateModal', () => {
	const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
	const mockOnCancel = vi.fn();

	const defaultProps = {
		field: 'status' as const,
		count: 1,
		options: statusOptions,
		onConfirm: mockOnConfirm,
		onCancel: mockOnCancel
	};

	beforeEach(() => {
		mockOnConfirm.mockClear();
		mockOnCancel.mockClear();
	});

	describe('rendering', () => {
		it('renders the field label and count in the title', () => {
			render(BulkUpdateModal, { props: defaultProps });

			expect(
				screen.getByRole('heading', { name: /devices\.bulk\.update\.title/ })
			).toBeInTheDocument();
			expect(screen.getByText('devices.bulk.changeStatus')).toBeInTheDocument();
		});

		it('defaults the select to the placeholder when no initialValue is given (N-device bulk toolbar usage)', () => {
			render(BulkUpdateModal, { props: defaultProps });

			const select = screen.getByLabelText('devices.bulk.changeStatus') as HTMLSelectElement;
			expect(select.value).toBe('');
		});

		it('preselects the current status when initialValue is provided (single-device Change Status usage)', () => {
			render(BulkUpdateModal, { props: { ...defaultProps, initialValue: 'Retired' } });

			const select = screen.getByLabelText('devices.bulk.changeStatus') as HTMLSelectElement;
			expect(select.value).toBe('Retired');
		});

		it('disables the confirm button until a value is selected', () => {
			render(BulkUpdateModal, { props: defaultProps });

			expect(screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ })).toBeDisabled();
		});

		it('enables the confirm button once a value is preselected via initialValue', () => {
			render(BulkUpdateModal, { props: { ...defaultProps, initialValue: 'Active' } });

			expect(screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ })).toBeEnabled();
		});
	});

	describe('confirmation flow', () => {
		it('calls onConfirm with the selected status value on submit', async () => {
			const user = userEvent.setup();
			render(BulkUpdateModal, { props: defaultProps });

			const select = screen.getByLabelText('devices.bulk.changeStatus');
			await user.selectOptions(select, 'Retired');

			const confirmButton = screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ });
			await user.click(confirmButton);

			await waitFor(() => {
				expect(mockOnConfirm).toHaveBeenCalledWith('Retired');
			});
		});

		it('calls onCancel when the cancel button is clicked', async () => {
			const user = userEvent.setup();
			render(BulkUpdateModal, { props: { ...defaultProps, initialValue: 'Active' } });

			await user.click(screen.getByRole('button', { name: /common\.actions\.cancel/ }));

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('calls onCancel when Escape is pressed', async () => {
			const user = userEvent.setup();
			render(BulkUpdateModal, { props: defaultProps });

			await user.keyboard('{Escape}');

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('does not call onCancel on Escape while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<(value: string) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			render(BulkUpdateModal, {
				props: { ...defaultProps, initialValue: 'Retired', onConfirm }
			});

			await user.click(screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ }));
			await user.keyboard('{Escape}');

			expect(mockOnCancel).not.toHaveBeenCalled();
		});

		it('does not call onConfirm when submitting the placeholder (no value selected)', async () => {
			render(BulkUpdateModal, { props: defaultProps });

			const form = screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ }).closest('form');
			await fireEvent.submit(form as HTMLFormElement);

			expect(mockOnConfirm).not.toHaveBeenCalled();
		});
	});

	describe('loading state', () => {
		it('disables confirm and cancel and shows the saving label while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<(value: string) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(BulkUpdateModal, {
				props: { ...defaultProps, initialValue: 'Retired', onConfirm }
			});

			const confirmButton = screen.getByRole('button', { name: /devices\.bulk\.update\.confirm/ });
			await user.click(confirmButton);

			expect(screen.getByRole('button', { name: /common\.states\.saving/ })).toBeDisabled();
			expect(screen.getByRole('button', { name: /common\.actions\.cancel/ })).toBeDisabled();
		});
	});

	describe('accessibility', () => {
		it('has no axe violations in default state', async () => {
			const { container } = render(BulkUpdateModal, { props: defaultProps });

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with an initialValue preselected', async () => {
			const { container } = render(BulkUpdateModal, {
				props: { ...defaultProps, initialValue: 'Active' }
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});

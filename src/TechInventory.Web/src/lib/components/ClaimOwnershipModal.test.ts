/**
 * ClaimOwnershipModal Component Tests — T26 Ownership modals
 * 
 * Test modal rendering, confirmation flow, API call, loading state.
 * Constitution §3.4: axe-core zero violations
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import ClaimOwnershipModal from './ClaimOwnershipModal.svelte';

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

describe('ClaimOwnershipModal', () => {
	const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
	const mockOnCancel = vi.fn();

	const defaultProps = {
		deviceName: 'iPad Pro',
		currentOwnerName: 'Alice',
		onConfirm: mockOnConfirm,
		onCancel: mockOnCancel
	};

	beforeEach(() => {
		mockOnConfirm.mockClear();
		mockOnCancel.mockClear();
	});

	describe('rendering', () => {
		it('renders modal with device name', () => {
			render(ClaimOwnershipModal, { props: defaultProps });

			expect(screen.getByRole('heading', { name: /devices.claim.modal.title/i })).toBeInTheDocument();
			expect(screen.getByText('iPad Pro')).toBeInTheDocument();
		});

		it('shows current owner name when device is owned', () => {
			render(ClaimOwnershipModal, { props: defaultProps });

			expect(screen.getByText(/devices.claim.modal.body/i)).toBeInTheDocument();
		});

		it('shows unowned message when currentOwnerName is null', () => {
			render(ClaimOwnershipModal, { props: { ...defaultProps, currentOwnerName: null } });

			expect(screen.getByText(/devices.claim.modal.bodyUnowned/i)).toBeInTheDocument();
		});

		it('has cancel and claim buttons', () => {
			render(ClaimOwnershipModal, { props: defaultProps });

			expect(screen.getByRole('button', { name: /common.actions.cancel/i })).toBeInTheDocument();
			expect(screen.getByRole('button', { name: /devices.claim.modal.confirm/i })).toBeInTheDocument();
		});
	});

	describe('confirmation flow', () => {
		it('calls onConfirm when claim button clicked', async () => {
			const user = userEvent.setup();
			render(ClaimOwnershipModal, { props: defaultProps });

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			await waitFor(() => {
				expect(mockOnConfirm).toHaveBeenCalledTimes(1);
			});
		});

		it('calls onCancel when cancel button clicked', async () => {
			const user = userEvent.setup();
			render(ClaimOwnershipModal, { props: defaultProps });

			const cancelButton = screen.getByRole('button', { name: /common.actions.cancel/i });
			await user.click(cancelButton);

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('calls onCancel when Escape key pressed', async () => {
			const user = userEvent.setup();
			render(ClaimOwnershipModal, { props: defaultProps });

			await user.keyboard('{Escape}');

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('does not call onCancel on Escape when submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			render(ClaimOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			// Try to Escape during submission
			await user.keyboard('{Escape}');

			expect(mockOnCancel).not.toHaveBeenCalled();
		});
	});

	describe('loading state', () => {
		it('disables claim button while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ClaimOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			// Button should be disabled during submission
			expect(claimButton).toBeDisabled();
		});

		it('disables cancel button while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ClaimOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			const cancelButton = screen.getByRole('button', { name: /common.actions.cancel/i });
			expect(cancelButton).toBeDisabled();
		});

		it('shows processing state text while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ClaimOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			await waitFor(() => {
				expect(screen.getByText(/common.states.processing/i)).toBeInTheDocument();
			});
		});
	});

	describe('backdrop click (C-22: D-123 deferral was inaccurate — plain onclick, testable in jsdom)', () => {
		it('closes the modal when the backdrop itself is clicked', async () => {
			const onCancel = vi.fn();
			const { container } = render(ClaimOwnershipModal, {
				props: { ...defaultProps, onCancel }
			});

			const backdrop = container.querySelector('.ti-modal-backdrop');
			expect(backdrop).toBeInTheDocument();
			await fireEvent.click(backdrop as Element);

			expect(onCancel).toHaveBeenCalledTimes(1);
		});

		it('does not close when clicking inside the modal surface', async () => {
			const onCancel = vi.fn();
			render(ClaimOwnershipModal, { props: { ...defaultProps, onCancel } });

			// Clicking the dialog title (a child, not the backdrop) must not
			// close the modal — handleBackdropClick only fires when
			// event.target === event.currentTarget.
			await fireEvent.click(screen.getByText('devices.claim.modal.title'));

			expect(onCancel).not.toHaveBeenCalled();
		});
	});

	describe('accessibility', () => {
		it('has no axe violations in default state', async () => {
			const { container } = render(ClaimOwnershipModal, { props: defaultProps });

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations when device is unowned', async () => {
			const { container } = render(ClaimOwnershipModal, {
				props: { ...defaultProps, currentOwnerName: null }
			});

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			const { container } = render(ClaimOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const claimButton = screen.getByRole('button', { name: /devices.claim.modal.confirm/i });
			await user.click(claimButton);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});

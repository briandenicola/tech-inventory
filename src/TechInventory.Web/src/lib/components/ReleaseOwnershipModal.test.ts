/**
 * ReleaseOwnershipModal Component Tests — T26 Ownership modals
 * 
 * Test modal rendering, confirmation flow, API call, loading state.
 * Constitution §3.4: axe-core zero violations
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import ReleaseOwnershipModal from './ReleaseOwnershipModal.svelte';

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

describe('ReleaseOwnershipModal', () => {
	const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
	const mockOnCancel = vi.fn();

	const defaultProps = {
		deviceName: 'MacBook Air',
		onConfirm: mockOnConfirm,
		onCancel: mockOnCancel
	};

	beforeEach(() => {
		mockOnConfirm.mockClear();
		mockOnCancel.mockClear();
	});

	describe('rendering', () => {
		it('renders modal with device name', () => {
			render(ReleaseOwnershipModal, { props: defaultProps });

			expect(screen.getByRole('heading', { name: /devices.release.modal.title/i })).toBeInTheDocument();
			expect(screen.getByText('MacBook Air')).toBeInTheDocument();
		});

		it('shows warning message about releasing ownership', () => {
			render(ReleaseOwnershipModal, { props: defaultProps });

			expect(screen.getByText(/devices.release.modal.body/i)).toBeInTheDocument();
		});

		it('has cancel and release buttons', () => {
			render(ReleaseOwnershipModal, { props: defaultProps });

			expect(screen.getByRole('button', { name: /common.actions.cancel/i })).toBeInTheDocument();
			expect(screen.getByRole('button', { name: /devices.release.modal.confirm/i })).toBeInTheDocument();
		});
	});

	describe('confirmation flow', () => {
		it('calls onConfirm when release button clicked', async () => {
			const user = userEvent.setup();
			render(ReleaseOwnershipModal, { props: defaultProps });

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			await waitFor(() => {
				expect(mockOnConfirm).toHaveBeenCalledTimes(1);
			});
		});

		it('calls onCancel when cancel button clicked', async () => {
			const user = userEvent.setup();
			render(ReleaseOwnershipModal, { props: defaultProps });

			const cancelButton = screen.getByRole('button', { name: /common.actions.cancel/i });
			await user.click(cancelButton);

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('calls onCancel when Escape key pressed', async () => {
			const user = userEvent.setup();
			render(ReleaseOwnershipModal, { props: defaultProps });

			await user.keyboard('{Escape}');

			expect(mockOnCancel).toHaveBeenCalledTimes(1);
		});

		it('does not call onCancel on Escape when submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			render(ReleaseOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			// Try to Escape during submission
			await user.keyboard('{Escape}');

			expect(mockOnCancel).not.toHaveBeenCalled();
		});
	});

	describe('loading state', () => {
		it('disables release button while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ReleaseOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			// Button should be disabled during submission
			expect(releaseButton).toBeDisabled();
		});

		it('disables cancel button while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ReleaseOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			const cancelButton = screen.getByRole('button', { name: /common.actions.cancel/i });
			expect(cancelButton).toBeDisabled();
		});

		it('shows processing state text while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(ReleaseOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			await waitFor(() => {
				expect(screen.getByText(/common.states.processing/i)).toBeInTheDocument();
			});
		});
	});

	// NOTE: Backdrop click tests deferred to E2E per D-123 (jsdom limitation on backdrop click events)

	describe('accessibility', () => {
		it('has no axe violations in default state', async () => {
			const { container } = render(ReleaseOwnershipModal, { props: defaultProps });

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<() => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			const { container } = render(ReleaseOwnershipModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const releaseButton = screen.getByRole('button', { name: /devices.release.modal.confirm/i });
			await user.click(releaseButton);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});

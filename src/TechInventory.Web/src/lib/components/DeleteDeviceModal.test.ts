/**
 * DeleteDeviceModal Component Tests — Delete confirmation modal (T23)
 * 
 * Test type-name-to-confirm, reason field, submit guards, focus trap, keyboard.
 * Constitution §3.4: axe-core zero violations
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import DeleteDeviceModal from './DeleteDeviceModal.svelte';

// Mock i18n
vi.mock('$lib/i18n', () => ({
	t: (key: string) => key
}));

describe('DeleteDeviceModal', () => {
	const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
	const mockOnCancel = vi.fn();

	const defaultProps = {
		deviceName: 'iPhone 15 Pro',
		onConfirm: mockOnConfirm,
		onCancel: mockOnCancel
	};

	beforeEach(() => {
		mockOnConfirm.mockClear();
		mockOnCancel.mockClear();
	});

	describe('rendering', () => {
		it('renders modal content', () => {
			render(DeleteDeviceModal, { props: defaultProps });

			// Should have heading, confirmation input, reason textarea
			expect(screen.getByRole('heading')).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.delete.confirmPrompt/i)).toBeInTheDocument();
			expect(screen.getByLabelText(/devices.delete.reasonLabel/i)).toBeInTheDocument();
		});

		it('displays device name in instructions', () => {
			render(DeleteDeviceModal, { props: defaultProps });

			// Instructions should mention the device name (as placeholder)
			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i) as HTMLInputElement;
			expect(confirmInput.placeholder).toBe('iPhone 15 Pro');
		});

		it('has cancel and delete buttons', () => {
			render(DeleteDeviceModal, { props: defaultProps });

			expect(screen.getByRole('button', { name: /common.actions.cancel/i })).toBeInTheDocument();
			expect(screen.getByRole('button', { name: /common.actions.delete/i })).toBeInTheDocument();
		});
	});

	describe('type-name confirmation', () => {
		it('delete button disabled when confirmation name is empty', () => {
			render(DeleteDeviceModal, { props: defaultProps });

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			expect(deleteButton).toBeDisabled();
		});

		it('delete button disabled when confirmation name does not match', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'Wrong Name');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'A valid reason with at least 10 chars');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			expect(deleteButton).toBeDisabled();
		});

		it('delete button enabled when confirmation name exactly matches (case-sensitive, trimmed)', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'A valid reason with at least 10 chars');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => {
				expect(deleteButton).not.toBeDisabled();
			});
		});

		it('trims whitespace from confirmation name before matching', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, '  iPhone 15 Pro  ');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Valid reason here');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => {
				expect(deleteButton).not.toBeDisabled();
			});
		});
	});

	describe('reason field validation', () => {
		it('delete button disabled when reason is empty', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			// Reason is empty
			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			expect(deleteButton).toBeDisabled();
		});

		it('delete button disabled when reason is less than 10 characters', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Short');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			expect(deleteButton).toBeDisabled();
		});

		it('delete button enabled when reason is exactly 10 characters', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, '1234567890');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => {
				expect(deleteButton).not.toBeDisabled();
			});
		});

		it('delete button enabled when reason is longer than 10 characters', async () => {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'This device is no longer in use and will be recycled.');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => {
				expect(deleteButton).not.toBeDisabled();
			});
		});
	});

	describe('submit behavior', () => {
		it('calls onConfirm with reason on valid submission', async () => {
			const user = userEvent.setup();
			// eslint-disable-next-line @typescript-eslint/no-unused-vars
			const onConfirm = vi.fn(async (reason: string) => {});

			render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			const reason = 'Device is broken and will be recycled';
			await user.type(reasonTextarea, reason);

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => expect(deleteButton).not.toBeDisabled());

			await user.click(deleteButton);

			await waitFor(() => {
				expect(onConfirm).toHaveBeenCalledTimes(1);
				expect(onConfirm).toHaveBeenCalledWith(reason);
			});
		});

		it('disables submit button while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<(reason: string) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 100))
			);

			render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Valid reason here');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => expect(deleteButton).not.toBeDisabled());

			await user.click(deleteButton);

			// Should be disabled during submission
			expect(deleteButton).toBeDisabled();
		});
	});

	describe('cancel behavior', () => {
		it('calls onCancel when cancel button clicked', async () => {
			const user = userEvent.setup();
			const onCancel = vi.fn();

			render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onCancel
				}
			});

			const cancelButton = screen.getByRole('button', { name: /common.actions.cancel/i });
			await user.click(cancelButton);

			expect(onCancel).toHaveBeenCalledTimes(1);
		});
	});

	describe('keyboard interaction', () => {
		it('calls onCancel when Escape key pressed', async () => {
			const user = userEvent.setup();
			const onCancel = vi.fn();

			render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onCancel
				}
			});

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.click(confirmInput);
			await user.keyboard('{Escape}');

			expect(onCancel).toHaveBeenCalledTimes(1);
		});

		it('does not call onCancel on Escape when submitting', async () => {
			const user = userEvent.setup();
			const onCancel = vi.fn();
			const onConfirm = vi.fn<(reason: string) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onConfirm,
					onCancel
				}
			});

			// Fill and submit
			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Valid reason here');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => expect(deleteButton).not.toBeDisabled());

			await user.click(deleteButton);

			// Try to Escape during submission
			await user.keyboard('{Escape}');

			// onCancel should NOT be called
			expect(onCancel).not.toHaveBeenCalled();
		});
	});

	describe('focus trap (C-22: D-078 deferral was inaccurate — plain keydown handler, testable in jsdom)', () => {
		async function renderWithSubmittableState() {
			const user = userEvent.setup();
			render(DeleteDeviceModal, { props: defaultProps });

			// The trap's last focusable element is the Delete button, which is
			// disabled (and therefore unfocusable) until the confirmation text
			// and reason are valid — fill both so the wrap-around has a real,
			// focusable last element to land on.
			await user.type(screen.getByLabelText(/devices.delete.confirmPrompt/i), 'iPhone 15 Pro');
			await user.type(screen.getByLabelText(/devices.delete.reasonLabel/i), 'Valid reason here');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => expect(deleteButton).not.toBeDisabled());
			return deleteButton;
		}

		it('wraps focus from the last focusable element back to the first on Tab', async () => {
			const deleteButton = await renderWithSubmittableState();
			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);

			deleteButton.focus();
			expect(document.activeElement).toBe(deleteButton);

			fireEvent.keyDown(deleteButton, { key: 'Tab' });

			expect(document.activeElement).toBe(confirmInput);
		});

		it('wraps focus from the first focusable element back to the last on Shift+Tab', async () => {
			const deleteButton = await renderWithSubmittableState();
			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);

			confirmInput.focus();
			expect(document.activeElement).toBe(confirmInput);

			fireEvent.keyDown(confirmInput, { key: 'Tab', shiftKey: true });

			expect(document.activeElement).toBe(deleteButton);
		});

		it('does not move focus on Tab from a middle element (only wraps at the ends)', async () => {
			await renderWithSubmittableState();

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			reasonTextarea.focus();
			expect(document.activeElement).toBe(reasonTextarea);

			fireEvent.keyDown(reasonTextarea, { key: 'Tab' });

			// The trap only intercepts Tab at the last element and Shift+Tab at
			// the first — a middle element's own focus is left to native tab
			// order (jsdom does not implement it, so focus simply stays put).
			expect(document.activeElement).toBe(reasonTextarea);
		});
	});

	describe('accessibility', () => {
		it('has no axe violations in initial state', async () => {
			const { container } = render(DeleteDeviceModal, { props: defaultProps });

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with filled inputs', async () => {
			const user = userEvent.setup();
			const { container } = render(DeleteDeviceModal, { props: defaultProps });

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Valid reason here');

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations while submitting', async () => {
			const user = userEvent.setup();
			const onConfirm = vi.fn<(reason: string) => Promise<void>>(
				async () => new Promise((resolve) => setTimeout(resolve, 200))
			);

			const { container } = render(DeleteDeviceModal, {
				props: {
					...defaultProps,
					onConfirm
				}
			});

			const confirmInput = screen.getByLabelText(/devices.delete.confirmPrompt/i);
			await user.type(confirmInput, 'iPhone 15 Pro');

			const reasonTextarea = screen.getByLabelText(/devices.delete.reasonLabel/i);
			await user.type(reasonTextarea, 'Valid reason here');

			const deleteButton = screen.getByRole('button', { name: /common.actions.delete/i });
			await waitFor(() => expect(deleteButton).not.toBeDisabled());

			await user.click(deleteButton);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});


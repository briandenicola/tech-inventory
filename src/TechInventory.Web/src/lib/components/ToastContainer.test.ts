/**
 * ToastContainer Component Tests — Toast UI rendering (T23)
 * 
 * Test toast rendering, ARIA live region, manual dismiss, accessibility.
 * Constitution §3.4: axe-core zero violations
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import ToastContainer from './ToastContainer.svelte';
import { showToast, clearToasts } from '$lib/stores/toast';

describe('ToastContainer', () => {
	beforeEach(() => {
		clearToasts();
	});

	describe('rendering', () => {
		it('renders nothing when no toasts', () => {
			const { container } = render(ToastContainer);

			// Container should exist but have no toast items
			const liveRegion = container.querySelector('[aria-live="polite"]');
			expect(liveRegion).toBeInTheDocument();

			// No status roles (toasts)
			const statuses = screen.queryAllByRole('status');
			expect(statuses).toHaveLength(0);
		});

		it('renders single toast with message', () => {
			showToast({ type: 'success', message: 'Device created successfully!' });

			render(ToastContainer);

			expect(screen.getByRole('status')).toBeInTheDocument();
			expect(screen.getByText('Device created successfully!')).toBeInTheDocument();
		});

		it('renders multiple toasts', () => {
			showToast({ type: 'success', message: 'First toast' });
			showToast({ type: 'error', message: 'Second toast' });
			showToast({ type: 'info', message: 'Third toast' });

			render(ToastContainer);

			const statuses = screen.getAllByRole('status');
			expect(statuses).toHaveLength(3);
			expect(screen.getByText('First toast')).toBeInTheDocument();
			expect(screen.getByText('Second toast')).toBeInTheDocument();
			expect(screen.getByText('Third toast')).toBeInTheDocument();
		});

		it('applies success styling to success toast', () => {
			showToast({ type: 'success', message: 'Success!' });

			const { container } = render(ToastContainer);

			const toast = container.querySelector('[role="status"]');
			expect(toast).toHaveClass('bg-success-50');
		});

		it('applies error styling to error toast', () => {
			showToast({ type: 'error', message: 'Error!' });

			const { container } = render(ToastContainer);

			const toast = container.querySelector('[role="status"]');
			expect(toast).toHaveClass('bg-danger-50');
		});

		it('applies info styling to info toast', () => {
			showToast({ type: 'info', message: 'Info!' });

			const { container } = render(ToastContainer);

			const toast = container.querySelector('[role="status"]');
			expect(toast).toHaveClass('bg-info-50');
		});
	});

	describe('ARIA live region', () => {
		it('has aria-live="polite" on container', () => {
			const { container } = render(ToastContainer);

			const liveRegion = container.querySelector('[aria-live="polite"]');
			expect(liveRegion).toBeInTheDocument();
		});

		it('has aria-atomic="false" to announce only new toasts', () => {
			const { container } = render(ToastContainer);

			const liveRegion = container.querySelector('[aria-live="polite"]');
			expect(liveRegion).toHaveAttribute('aria-atomic', 'false');
		});

		it('each toast has role="status"', () => {
			showToast({ type: 'success', message: 'Test' });

			render(ToastContainer);

			const status = screen.getByRole('status');
			expect(status).toBeInTheDocument();
		});
	});

	describe('dismiss button', () => {
		it('renders dismiss button with accessible label', () => {
			showToast({ type: 'success', message: 'Test toast' });

			render(ToastContainer);

			const dismissButton = screen.getByLabelText('Dismiss notification');
			expect(dismissButton).toBeInTheDocument();
			expect(dismissButton.tagName).toBe('BUTTON');
		});

		it('removes toast when dismiss button clicked', async () => {
			const user = userEvent.setup();
			showToast({ type: 'success', message: 'Dismissible toast' });

			render(ToastContainer);

			const dismissButton = screen.getByLabelText('Dismiss notification');
			await user.click(dismissButton);

			// Toast should be gone (wait for transition)
			await waitFor(() => {
				expect(screen.queryByText('Dismissible toast')).not.toBeInTheDocument();
			});
		});

		it('dismisses correct toast when multiple present', async () => {
			const user = userEvent.setup();
			showToast({ type: 'success', message: 'First' });
			showToast({ type: 'error', message: 'Second' });

			render(ToastContainer);

			const dismissButtons = screen.getAllByLabelText('Dismiss notification');
			expect(dismissButtons).toHaveLength(2);

			// Dismiss first toast
			await user.click(dismissButtons[0]);

			// First should be gone, second should remain
			await waitFor(() => {
				expect(screen.queryByText('First')).not.toBeInTheDocument();
			});
			expect(screen.getByText('Second')).toBeInTheDocument();
		});
	});

	describe('accessibility', () => {
		it('has no axe violations with no toasts', async () => {
			const { container } = render(ToastContainer);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with success toast', async () => {
			showToast({ type: 'success', message: 'Success message' });

			const { container } = render(ToastContainer);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with error toast', async () => {
			showToast({ type: 'error', message: 'Error message' });

			const { container } = render(ToastContainer);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with info toast', async () => {
			showToast({ type: 'info', message: 'Info message' });

			const { container } = render(ToastContainer);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has no axe violations with multiple toasts', async () => {
			showToast({ type: 'success', message: 'Success' });
			showToast({ type: 'error', message: 'Error' });
			showToast({ type: 'info', message: 'Info' });

			const { container } = render(ToastContainer);

			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('icons are hidden from screen readers', () => {
			showToast({ type: 'success', message: 'Test' });

			const { container } = render(ToastContainer);

			const icons = container.querySelectorAll('svg');
			icons.forEach((icon) => {
				expect(icon).toHaveAttribute('aria-hidden', 'true');
			});
		});
	});
});

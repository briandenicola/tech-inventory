/**
 * ErrorState component tests
 * 
 * Per T18: Test error state with retry functionality.
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import ErrorState from './ErrorState.svelte';

describe('ErrorState', () => {
	it('renders error message', () => {
		const errorMessage = 'Failed to load devices';
		render(ErrorState, {
			props: {
				error: errorMessage,
				onRetry: vi.fn()
			}
		});
		
		expect(screen.getByText('Error')).toBeInTheDocument();
		expect(screen.getByText(errorMessage)).toBeInTheDocument();
	});

	it('renders retry button', () => {
		render(ErrorState, {
			props: {
				error: 'Test error',
				onRetry: vi.fn()
			}
		});
		
		const retryButton = screen.getByRole('button', { name: /Retry/i });
		expect(retryButton).toBeInTheDocument();
	});

	it('calls onRetry when retry button clicked', async () => {
		const user = userEvent.setup();
		const onRetry = vi.fn();
		
		render(ErrorState, {
			props: {
				error: 'Test error',
				onRetry
			}
		});
		
		const retryButton = screen.getByRole('button', { name: /Retry/i });
		await user.click(retryButton);
		
		expect(onRetry).toHaveBeenCalledTimes(1);
	});

	it('has role="alert" for assertive announcement', () => {
		const { container } = render(ErrorState, {
			props: {
				error: 'Test error',
				onRetry: vi.fn()
			}
		});
		
		const alert = container.querySelector('[role="alert"]');
		expect(alert).toBeInTheDocument();
		expect(alert).toHaveAttribute('aria-live', 'assertive');
	});

	it('has no accessibility violations', async () => {
		const { container } = render(ErrorState, {
			props: {
				error: 'Test error',
				onRetry: vi.fn()
			}
		});
		
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

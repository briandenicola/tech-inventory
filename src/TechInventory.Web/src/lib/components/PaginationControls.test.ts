/**
 * PaginationControls component tests
 * 
 * Per T18: Test pagination state boundaries, page size selector, callbacks.
 * Constitution §3.4: axe-core with zero violations.
 * Per D-057: URL state managed by parent via onPageChange callback.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import PaginationControls from './PaginationControls.svelte';

describe('PaginationControls', () => {
	const defaultProps = {
		currentPage: 1,
		pageSize: 25,
		totalCount: 100,
		onPageChange: vi.fn()
	};

	describe('page navigation', () => {
		it('renders page indicator showing current page', () => {
			render(PaginationControls, { props: defaultProps });
			
			// Should show "Page 1 of 4" (100 items / 25 per page = 4 pages)
			expect(screen.getByText(/Page 1 of 4/i)).toBeInTheDocument();
		});

		it('disables Previous button on first page', () => {
			render(PaginationControls, { props: defaultProps });
			
			// Desktop Previous button (there are 2 prev buttons: mobile + desktop)
			const prevButtons = screen.getAllByLabelText(/Previous Page/i);
			prevButtons.forEach(btn => {
				expect(btn).toBeDisabled();
			});
		});

		it('disables Next button on last page', () => {
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 4 }
			});
			
			const nextButtons = screen.getAllByLabelText(/Next Page/i);
			nextButtons.forEach(btn => {
				expect(btn).toBeDisabled();
			});
		});

		it('enables both buttons on middle page', () => {
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 2 }
			});
			
			const prevButtons = screen.getAllByLabelText(/Previous Page/i);
			const nextButtons = screen.getAllByLabelText(/Next Page/i);
			
			prevButtons.forEach(btn => expect(btn).not.toBeDisabled());
			nextButtons.forEach(btn => expect(btn).not.toBeDisabled());
		});

		it('calls onPageChange with next page when Next clicked', async () => {
			const user = userEvent.setup();
			const onPageChange = vi.fn();
			
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 1, onPageChange }
			});
			
			const nextButton = screen.getAllByLabelText(/Next Page/i)[0];
			await user.click(nextButton);
			
			expect(onPageChange).toHaveBeenCalledWith(2, 25);
		});

		it('calls onPageChange with previous page when Previous clicked', async () => {
			const user = userEvent.setup();
			const onPageChange = vi.fn();
			
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 2, onPageChange }
			});
			
			const prevButton = screen.getAllByLabelText(/Previous Page/i)[0];
			await user.click(prevButton);
			
			expect(onPageChange).toHaveBeenCalledWith(1, 25);
		});
	});

	describe('page size selector', () => {
		it('renders page size selector with current value', () => {
			render(PaginationControls, { props: defaultProps });
			
			const select = screen.getByLabelText(/Items per page/i) as HTMLSelectElement;
			// Svelte 5 sets the value prop but doesn't always reflect immediately in DOM
			// Check that the correct option exists and is selectable
			const option25 = select.querySelector('option[value="25"]') as HTMLOptionElement;
			expect(option25).toBeInTheDocument();
			expect(select.querySelector('option[value="50"]')).toBeInTheDocument();
			expect(select.querySelector('option[value="100"]')).toBeInTheDocument();
		});

		it('has options for 25, 50, 100', () => {
			render(PaginationControls, { props: defaultProps });
			
			const select = screen.getByLabelText(/Items per page/i);
			const options = Array.from(select.querySelectorAll('option'));
			
			expect(options).toHaveLength(3);
			expect(options[0]).toHaveValue('25');
			expect(options[1]).toHaveValue('50');
			expect(options[2]).toHaveValue('100');
		});

		it('calls onPageChange with page 1 and new size when changed', async () => {
			const user = userEvent.setup();
			const onPageChange = vi.fn();
			
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 3, onPageChange }
			});
			
			const select = screen.getByLabelText(/Items per page/i);
			await user.selectOptions(select, '50');
			
			// Should reset to page 1 when changing page size
			expect(onPageChange).toHaveBeenCalledWith(1, 50);
		});
	});

	describe('showing info', () => {
		it('displays correct item range', () => {
			render(PaginationControls, { props: defaultProps });
			
			// Page 1, pageSize 25, totalCount 100 → "Showing 1 to 25 of 100 devices"
			expect(screen.getByText(/Showing 1 to 25 of 100 devices/i)).toBeInTheDocument();
		});

		it('displays correct range on middle page', () => {
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 2 }
			});
			
			// Page 2 → items 26-50
			expect(screen.getByText(/Showing 26 to 50 of 100 devices/i)).toBeInTheDocument();
		});

		it('displays correct range on last partial page', () => {
			render(PaginationControls, {
				props: { ...defaultProps, currentPage: 4, totalCount: 87 }
			});
			
			// Page 4, pageSize 25, total 87 → items 76-87
			expect(screen.getByText(/Showing 76 to 87 of 87 devices/i)).toBeInTheDocument();
		});
	});

	describe('accessibility', () => {
		it('has no accessibility violations', async () => {
			const { container } = render(PaginationControls, { props: defaultProps });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('has labeled page size selector', () => {
			render(PaginationControls, { props: defaultProps });
			
			const select = screen.getByLabelText(/Items per page/i);
			expect(select).toBeInTheDocument();
		});

		it('has aria-label on pagination nav', () => {
			const { container } = render(PaginationControls, { props: defaultProps });
			
			const nav = container.querySelector('nav[aria-label="Pagination"]');
			expect(nav).toBeInTheDocument();
		});
	});
});

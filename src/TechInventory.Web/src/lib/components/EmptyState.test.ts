/**
 * EmptyState component tests
 * 
 * Per T18: Test empty state with and without filters.
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import EmptyState from './EmptyState.svelte';

describe('EmptyState', () => {
	describe('unfiltered (no devices)', () => {
		it('renders empty state message', () => {
			render(EmptyState, { props: { filtered: false } });
			
			expect(screen.getByText(/No devices yet/i)).toBeInTheDocument();
		});

		it('renders add device CTA button', () => {
			render(EmptyState, { props: { filtered: false } });
			
			const addButton = screen.getByRole('link', { name: /Add Device/i });
			expect(addButton).toBeInTheDocument();
			expect(addButton).toHaveAttribute('href', '/devices/new');
		});

		it('has no accessibility violations', async () => {
			const { container } = render(EmptyState, { props: { filtered: false } });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});

	describe('filtered (no matches)', () => {
		it('renders filtered empty state message', () => {
			render(EmptyState, { props: { filtered: true } });
			
			expect(screen.getByText(/No devices match your filters/i)).toBeInTheDocument();
		});

		it('does not render add device CTA when filtered', () => {
			render(EmptyState, { props: { filtered: true } });
			
			const addButton = screen.queryByRole('link', { name: /Add Device/i });
			expect(addButton).not.toBeInTheDocument();
		});

		it('has no accessibility violations', async () => {
			const { container } = render(EmptyState, { props: { filtered: true } });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});

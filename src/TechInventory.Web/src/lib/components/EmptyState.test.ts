/**
 * EmptyState component tests
 * 
 * Per T18: Test empty state with and without filters.
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import EmptyState from './EmptyState.svelte';

describe('EmptyState', () => {
	describe('unfiltered (no devices)', () => {
		it('renders empty state message', () => {
			render(EmptyState, { props: { filtered: false } });
			
			expect(screen.getByText(/No devices yet/i)).toBeInTheDocument();
		});

		it('renders add device CTA link by default (no onAdd handler)', () => {
			render(EmptyState, { props: { filtered: false } });
			
			const addButton = screen.getByRole('link', { name: /Add Device/i });
			expect(addButton).toBeInTheDocument();
			expect(addButton).toHaveAttribute('href', '/devices/new');
		});

		it('renders add device CTA as button when onAdd handler provided', async () => {
			const onAdd = vi.fn();
			render(EmptyState, { props: { filtered: false, onAdd } });

			const addButton = screen.getByRole('button', { name: /Add Device/i });
			expect(addButton).toBeInTheDocument();
			await fireEvent.click(addButton);
			expect(onAdd).toHaveBeenCalledOnce();
		});

		it('has no accessibility violations', async () => {
			const { container } = render(EmptyState, { props: { filtered: false } });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});

		it('does not render add device CTA when creation is unavailable', () => {
			render(EmptyState, { props: { filtered: false, showAddAction: false } });

			expect(screen.queryByRole('link', { name: /Add Device/i })).not.toBeInTheDocument();
			expect(screen.queryByRole('button', { name: /Add Device/i })).not.toBeInTheDocument();
		});
	});

	describe('filtered (no matches)', () => {
		it('renders filtered empty state message', () => {
			render(EmptyState, { props: { filtered: true } });
			
			expect(screen.getByText(/No devices match your filters/i)).toBeInTheDocument();
		});

		it('does not render add device CTA when filtered', () => {
			render(EmptyState, { props: { filtered: true } });
			
			const addLink = screen.queryByRole('link', { name: /Add Device/i });
			const addButton = screen.queryByRole('button', { name: /Add Device/i });
			expect(addLink).not.toBeInTheDocument();
			expect(addButton).not.toBeInTheDocument();
		});

		it('has no accessibility violations', async () => {
			const { container } = render(EmptyState, { props: { filtered: true } });
			
			const results = await axe(container);
			expect(results).toHaveNoViolations();
		});
	});
});

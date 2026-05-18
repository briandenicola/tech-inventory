/**
 * LoadingSkeleton component tests
 * 
 * Per T18: Test all four UI states (loading is one of them).
 * Constitution §3.4: axe-core with zero violations.
 */

import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import LoadingSkeleton from './LoadingSkeleton.svelte';

describe('LoadingSkeleton', () => {
	it('renders with default 7 rows', () => {
		const { container } = render(LoadingSkeleton);
		
		// Should render 7 skeleton rows (default per D-059)
		// Count the row containers, not individual cells
		const rowContainers = container.querySelectorAll('.space-y-4 > div:not(:first-child)');
		expect(rowContainers).toHaveLength(7);
	});

	it('renders with custom row count', () => {
		const { container } = render(LoadingSkeleton, { props: { rows: 3 } });
		
		const rowContainers = container.querySelectorAll('.space-y-4 > div:not(:first-child)');
		expect(rowContainers).toHaveLength(3);
	});

	it('renders header skeleton', () => {
		const { container } = render(LoadingSkeleton);
		
		// Header row should be present (6 columns per D-038)
		const headerCells = container.querySelectorAll('.flex.gap-4:first-child > div');
		expect(headerCells.length).toBeGreaterThan(0);
	});

	it('has no accessibility violations', async () => {
		const { container } = render(LoadingSkeleton);
		
		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

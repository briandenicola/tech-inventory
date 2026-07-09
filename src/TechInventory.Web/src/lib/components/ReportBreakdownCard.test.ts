import { render, screen } from '@testing-library/svelte';
import { describe, expect, it } from 'vitest';
import ReportBreakdownCard from './ReportBreakdownCard.svelte';

describe('ReportBreakdownCard', () => {
	it('caps long breakdown lists when a max height class is provided', () => {
		const { container } = render(ReportBreakdownCard, {
			props: {
				title: 'Devices by location',
				emptyText: 'No data',
				maxListHeightClass: 'max-h-96',
				items: [
					{ label: 'Kitchen', count: 12 },
					{ label: 'Garage', count: 8 }
				]
			}
		});

		expect(screen.getByText('Devices by location')).toBeInTheDocument();
		const list = container.querySelector('ul');
		expect(list).toHaveClass('max-h-96');
		expect(list).toHaveClass('overflow-y-auto');
	});
});

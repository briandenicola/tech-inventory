import { describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import ResponsiveListCard from './ResponsiveListCard.svelte';

describe('ResponsiveListCard', () => {
	it('renders the heading, definition list fields, and overflow actions', () => {
		render(ResponsiveListCard, {
			props: {
				title: 'Kitchen Router',
				titleId: 'card-kitchen-router',
				badge: {
					text: 'Inactive',
					className:
						'inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200'
				},
				fields: [
					{ key: 'network', label: 'Network', value: 'Main Wi-Fi' },
					{ key: 'notes', label: 'Notes', value: 'Mounted near the pantry.' }
				],
				actionItems: [{ id: 'edit', label: 'Edit', onSelect: vi.fn() }],
				actionMenuLabel: 'More actions',
				actionMenuTitle: 'Actions'
			}
		});

		expect(screen.getByRole('heading', { name: 'Kitchen Router' })).toBeInTheDocument();
		const definitionList = document.querySelector('dl');
		expect(definitionList).not.toBeNull();
		expect(within(definitionList as HTMLElement).getByText('Network')).toBeInTheDocument();
		expect(within(definitionList as HTMLElement).getByText('Main Wi-Fi')).toBeInTheDocument();
		expect(screen.getByRole('button', { name: 'More actions' })).toBeInTheDocument();
	});

	it('skips empty field values and has no accessibility violations', async () => {
		const { container } = render(ResponsiveListCard, {
			props: {
				title: 'Office Printer',
				titleId: 'card-office-printer',
				fields: [
					{ key: 'model', label: 'Model', value: '' },
					{ key: 'location', label: 'Location', value: 'Office' }
				],
				actionMenuLabel: 'More actions',
				actionMenuTitle: 'Actions'
			}
		});

		expect(screen.queryByText('Model')).not.toBeInTheDocument();
		expect(screen.getByText('Location')).toBeInTheDocument();

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

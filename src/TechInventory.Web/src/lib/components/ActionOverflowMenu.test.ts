import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import ActionOverflowMenu from './ActionOverflowMenu.svelte';

describe('ActionOverflowMenu', () => {
	it('opens the overflow menu and runs item actions', async () => {
		const user = userEvent.setup();
		const onEdit = vi.fn();

		render(ActionOverflowMenu, {
			props: {
				buttonLabel: 'More actions',
				menuTitle: 'Actions',
				items: [{ id: 'edit', label: 'Edit', onSelect: onEdit }]
			}
		});

		await user.click(screen.getByRole('button', { name: 'More actions' }));
		const action = screen.getAllByRole('button', { name: 'Edit' })[0];
		await user.click(action);

		expect(onEdit).toHaveBeenCalledTimes(1);
	});

	it('has no accessibility violations', async () => {
		const { container } = render(ActionOverflowMenu, {
			props: {
				buttonLabel: 'More actions',
				menuTitle: 'Actions',
				items: [{ id: 'edit', label: 'Edit', onSelect: vi.fn() }]
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

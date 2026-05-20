import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import ResponsiveAdminListHarness from './ResponsiveAdminListHarness.svelte';

describe('ResponsiveAdminList', () => {
	it('renders both semantic table and mobile card list markup', () => {
		const { container } = render(ResponsiveAdminListHarness);

		expect(screen.getByRole('table')).toBeInTheDocument();
		expect(screen.getByRole('list', { name: 'Harness admin cards' })).toBeInTheDocument();
		expect(container.querySelector('.md\\:hidden')).toBeInTheDocument();
		expect(container.querySelector('.md\\:block')).toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(ResponsiveAdminListHarness);

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

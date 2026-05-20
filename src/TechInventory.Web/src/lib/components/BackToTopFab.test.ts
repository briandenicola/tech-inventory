import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import userEvent from '@testing-library/user-event';
import BackToTopFab from './BackToTopFab.svelte';

describe('BackToTopFab', () => {
	it('does not render when hidden', () => {
		render(BackToTopFab, {
			props: {
				visible: false,
				label: 'Back to top',
				onClick: vi.fn()
			}
		});

		expect(screen.queryByRole('button', { name: /Back to top/i })).not.toBeInTheDocument();
	});

	it('calls onClick when activated', async () => {
		const user = userEvent.setup();
		const onClick = vi.fn();

		render(BackToTopFab, {
			props: {
				visible: true,
				label: 'Back to top',
				onClick
			}
		});

		await user.click(screen.getByRole('button', { name: /Back to top/i }));

		expect(onClick).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(BackToTopFab, {
			props: {
				visible: true,
				label: 'Back to top',
				onClick: vi.fn()
			}
		});

		const results = await axe(container);
		expect(results).toHaveNoViolations();
	});
});

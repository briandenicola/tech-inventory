import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import AddDeviceFab from './AddDeviceFab.svelte';

describe('AddDeviceFab', () => {
	it('renders a plus-button when visible', () => {
		render(AddDeviceFab, {
			props: {
				visible: true,
				label: 'Add device',
				onClick: vi.fn()
			}
		});

		expect(screen.getByRole('button', { name: /add device/i })).toBeInTheDocument();
	});

	it('calls onClick when tapped', async () => {
		const user = userEvent.setup();
		const onClick = vi.fn();

		render(AddDeviceFab, {
			props: {
				visible: true,
				label: 'Add device',
				onClick
			}
		});

		await user.click(screen.getByRole('button', { name: /add device/i }));

		expect(onClick).toHaveBeenCalledOnce();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(AddDeviceFab, {
			props: {
				visible: true,
				label: 'Add device',
				onClick: vi.fn()
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});

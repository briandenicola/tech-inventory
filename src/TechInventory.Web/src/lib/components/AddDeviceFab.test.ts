import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import AddDeviceFab from './AddDeviceFab.svelte';

describe('AddDeviceFab', () => {
	it('renders an add-device link when visible', () => {
		render(AddDeviceFab, {
			props: {
				visible: true,
				label: 'Add device',
				href: '/devices/new'
			}
		});

		const fab = screen.getByRole('link', { name: /add device/i });
		expect(fab).toBeInTheDocument();
		expect(fab).toHaveAttribute('href', '/devices/new');
	});

	it('does not render when hidden', () => {
		render(AddDeviceFab, {
			props: {
				visible: false,
				label: 'Add device',
				href: '/devices/new'
			}
		});

		expect(screen.queryByRole('link', { name: /add device/i })).not.toBeInTheDocument();
	});

	it('has no accessibility violations', async () => {
		const { container } = render(AddDeviceFab, {
			props: {
				visible: true,
				label: 'Add device',
				href: '/devices/new'
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});

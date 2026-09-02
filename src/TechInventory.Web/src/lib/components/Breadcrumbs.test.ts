import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import Breadcrumbs from './Breadcrumbs.svelte';

describe('Breadcrumbs (C-07)', () => {
	it('renders every item as a link except the last, which is the current page', () => {
		render(Breadcrumbs, {
			props: {
				items: [
					{ label: 'Home', href: '/' },
					{ label: 'Devices', href: '/devices' },
					{ label: 'Living Room TV' }
				]
			}
		});

		const homeLink = screen.getByRole('link', { name: 'Home' });
		const devicesLink = screen.getByRole('link', { name: 'Devices' });
		expect(homeLink).toHaveAttribute('href', '/');
		expect(devicesLink).toHaveAttribute('href', '/devices');

		// The final crumb has no href — it is not a link at all, and is marked
		// as the current page for assistive tech.
		expect(screen.queryByRole('link', { name: 'Living Room TV' })).not.toBeInTheDocument();
		const current = screen.getByText('Living Room TV');
		expect(current.closest('li')).toHaveAttribute('aria-current', 'page');
	});

	it('renders an intermediate item as a link when it carries an href (edit-page 4-level trail)', () => {
		render(Breadcrumbs, {
			props: {
				items: [
					{ label: 'Home', href: '/' },
					{ label: 'Devices', href: '/devices' },
					{ label: 'Living Room TV', href: '/devices/device-1' },
					{ label: 'Edit' }
				]
			}
		});

		expect(screen.getByRole('link', { name: 'Living Room TV' })).toHaveAttribute(
			'href',
			'/devices/device-1'
		);
		expect(screen.getByText('Edit').closest('li')).toHaveAttribute('aria-current', 'page');
	});

	it('applies a caller-supplied class override to the nav element instead of the default', () => {
		const { container } = render(Breadcrumbs, {
			props: {
				items: [{ label: 'Home', href: '/' }, { label: 'Current' }],
				class: 'mb-4 hidden text-sm sm:flex'
			}
		});

		const nav = container.querySelector('nav');
		expect(nav).toHaveClass('hidden', 'sm:flex');
		expect(nav).not.toHaveClass('flex');
	});

	it('has no accessibility violations', async () => {
		const { container } = render(Breadcrumbs, {
			props: {
				items: [
					{ label: 'Home', href: '/' },
					{ label: 'Devices', href: '/devices' },
					{ label: 'Living Room TV' }
				]
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});
});

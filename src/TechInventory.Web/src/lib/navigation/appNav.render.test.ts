import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import { axe } from 'vitest-axe';
import AppNavMenuHarness from './AppNavMenuHarness.svelte';

describe('appNav rendering', () => {
	it('shows the Audit Log top-level item only for admins and points it at the audit route', async () => {
		const { rerender } = render(AppNavMenuHarness, {
			props: {
				role: 'Admin',
				pathname: '/admin/audit'
			}
		});

		const auditLink = screen.getByRole('link', { name: 'Audit Log' });
		expect(auditLink).toHaveAttribute('href', '/admin/audit');
		expect(auditLink).toHaveAttribute('aria-current', 'page');

		await rerender({ role: 'Member', pathname: '/devices' });
		expect(screen.queryByRole('link', { name: 'Audit Log' })).not.toBeInTheDocument();

		await rerender({ role: 'Viewer', pathname: '/devices' });
		expect(screen.queryByRole('link', { name: 'Audit Log' })).not.toBeInTheDocument();
	});

	it('has no accessibility violations for the admin navigation shell', async () => {
		const { container } = render(AppNavMenuHarness, {
			props: {
				role: 'Admin',
				pathname: '/admin/audit'
			}
		});

		expect(await axe(container)).toHaveNoViolations();
	});

	it('labels the reference-data section heading "Configuration", not "ADMIN" (#139)', () => {
		render(AppNavMenuHarness, {
			props: {
				role: 'Admin',
				pathname: '/admin/brands'
			}
		});

		expect(screen.getByRole('heading', { name: 'Configuration' })).toBeInTheDocument();
		expect(screen.queryByText('ADMIN')).not.toBeInTheDocument();
		expect(screen.queryByText('Admin', { selector: 'h2' })).not.toBeInTheDocument();
		// Membership/order is unchanged — only the heading label moved.
		expect(screen.getByRole('link', { name: 'Brands' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Categories' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Locations' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Networks' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Owners' })).toBeInTheDocument();
		expect(screen.getByRole('link', { name: 'Tags' })).toBeInTheDocument();
	});

	it('hides the Configuration section for non-Admin roles (role visibility unchanged)', () => {
		render(AppNavMenuHarness, {
			props: {
				role: 'Member',
				pathname: '/devices'
			}
		});

		expect(screen.queryByRole('heading', { name: 'Configuration' })).not.toBeInTheDocument();
	});
});

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
});

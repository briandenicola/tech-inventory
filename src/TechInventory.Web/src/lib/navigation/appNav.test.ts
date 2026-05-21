import { describe, expect, it } from 'vitest';
import { adminNavItems, getVisibleNavItems, isNavItemActive, primaryNavItems } from './appNav';

describe('appNav', () => {
	it('keeps admin navigation order consistent for all admin surfaces', () => {
		expect(adminNavItems.map((item) => item.href)).toEqual([
			'/admin/brands',
			'/admin/categories',
			'/admin/locations',
			'/admin/networks',
			'/admin/owners',
			'/admin/tags'
		]);
	});

	it('shows the expected primary items for members', () => {
		expect(getVisibleNavItems(primaryNavItems, 'Member').map((item) => item.href)).toEqual([
			'/devices',
			'/reports',
			'/admin/import',
			'/admin/export'
		]);
	});

	it('shows the audit log primary item for admins', () => {
		expect(getVisibleNavItems(primaryNavItems, 'Admin').map((item) => item.href)).toEqual([
			'/devices',
			'/reports',
			'/admin/import',
			'/admin/export',
			'/admin/audit'
		]);
	});

	it('shows all admin sub-routes for admins', () => {
		expect(getVisibleNavItems(adminNavItems, 'Admin').map((item) => item.href)).toEqual([
			'/admin/brands',
			'/admin/categories',
			'/admin/locations',
			'/admin/networks',
			'/admin/owners',
			'/admin/tags'
		]);
	});

	it('marks the audit-log leaf route and admin sub-routes as active in the correct places', () => {
		const reportsItem = primaryNavItems.find((item) => item.href === '/reports');
		const auditItem = primaryNavItems.find((item) => item.href === '/admin/audit');
		const exportItem = primaryNavItems.find((item) => item.href === '/admin/export');

		expect(isNavItemActive('/devices/123/edit', primaryNavItems[0])).toBe(true);
		expect(reportsItem).toBeDefined();
		expect(auditItem).toBeDefined();
		expect(exportItem).toBeDefined();
		expect(isNavItemActive('/reports', reportsItem!)).toBe(true);
		expect(isNavItemActive('/admin/audit', auditItem!)).toBe(true);
		expect(isNavItemActive('/admin/owners', auditItem!)).toBe(false);
		expect(isNavItemActive('/admin/owners', adminNavItems[4])).toBe(true);
		expect(isNavItemActive('/admin/export', exportItem!)).toBe(true);
	});
});

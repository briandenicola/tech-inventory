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
			'/import',
			'/export'
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

	it('marks nested device and admin routes as active', () => {
		const reportsItem = primaryNavItems.find((item) => item.href === '/reports');
		const adminItem = primaryNavItems.find((item) => item.href === '/admin');
		const exportItem = primaryNavItems.find((item) => item.href === '/export');

		expect(isNavItemActive('/devices/123/edit', primaryNavItems[0])).toBe(true);
		expect(reportsItem).toBeDefined();
		expect(adminItem).toBeDefined();
		expect(exportItem).toBeDefined();
		expect(isNavItemActive('/reports', reportsItem!)).toBe(true);
		expect(isNavItemActive('/admin/owners', adminItem!)).toBe(true);
		expect(isNavItemActive('/admin/owners', adminNavItems[4])).toBe(true);
		expect(isNavItemActive('/export', exportItem!)).toBe(true);
	});
});

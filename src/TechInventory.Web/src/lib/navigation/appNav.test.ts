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
		expect(isNavItemActive('/devices/123/edit', primaryNavItems[0])).toBe(true);
		expect(isNavItemActive('/admin/owners', primaryNavItems[3])).toBe(true);
		expect(isNavItemActive('/admin/owners', adminNavItems[4])).toBe(true);
		expect(isNavItemActive('/export', primaryNavItems[2])).toBe(true);
	});
});

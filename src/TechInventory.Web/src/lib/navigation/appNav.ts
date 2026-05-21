import type { CurrentUser } from '$lib/stores/auth';

export type AppNavRole = CurrentUser['role'];

export interface AppNavItem {
	href: string;
	labelKey: string;
	activePaths: readonly string[];
	roles?: readonly AppNavRole[];
}

const adminRoles = ['Admin'] as const satisfies readonly AppNavRole[];
const memberRoles = ['Admin', 'Member'] as const satisfies readonly AppNavRole[];

export const primaryNavItems = [
	{
		href: '/devices',
		labelKey: 'navigation.devices',
		activePaths: ['/devices']
	},
	{
		href: '/reports',
		labelKey: 'navigation.reports',
		activePaths: ['/reports']
	},
	{
		href: '/admin/import',
		labelKey: 'navigation.imports',
		activePaths: ['/admin/import'],
		roles: memberRoles
	},
	{
		href: '/admin/export',
		labelKey: 'navigation.exports',
		activePaths: ['/admin/export'],
		roles: adminRoles
	},
	{
		href: '/admin/audit',
		labelKey: 'navigation.adminAudit',
		activePaths: ['/admin/audit'],
		roles: adminRoles
	}
] as const satisfies readonly AppNavItem[];

export const adminNavItems = [
	{
		href: '/admin/brands',
		labelKey: 'navigation.adminBrands',
		activePaths: ['/admin/brands'],
		roles: adminRoles
	},
	{
		href: '/admin/categories',
		labelKey: 'navigation.adminCategories',
		activePaths: ['/admin/categories'],
		roles: adminRoles
	},
	{
		href: '/admin/locations',
		labelKey: 'navigation.adminLocations',
		activePaths: ['/admin/locations'],
		roles: adminRoles
	},
	{
		href: '/admin/networks',
		labelKey: 'navigation.adminNetworks',
		activePaths: ['/admin/networks'],
		roles: adminRoles
	},
	{
		href: '/admin/owners',
		labelKey: 'navigation.adminOwners',
		activePaths: ['/admin/owners'],
		roles: adminRoles
	},
	{
		href: '/admin/tags',
		labelKey: 'navigation.adminTags',
		activePaths: ['/admin/tags'],
		roles: adminRoles
	}
] as const satisfies readonly AppNavItem[];

export function getVisibleNavItems(
	items: readonly AppNavItem[],
	role: AppNavRole | null | undefined
): AppNavItem[] {
	return items.filter((item) => isNavItemVisible(item, role));
}

export function isNavItemVisible(
	item: AppNavItem,
	role: AppNavRole | null | undefined
): boolean {
	if (!item.roles) {
		return true;
	}

	if (!role) {
		return false;
	}

	return item.roles.includes(role);
}

export function isNavItemActive(pathname: string, item: AppNavItem): boolean {
	return item.activePaths.some((activePath) => pathname === activePath || pathname.startsWith(`${activePath}/`));
}

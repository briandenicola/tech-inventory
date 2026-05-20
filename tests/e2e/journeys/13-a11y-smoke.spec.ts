/**
 * Journey 13: Accessibility smoke — axe-core on every critical route.
 *
 * PRD §7.5.4: Critical user journey #13 / Constitution §6.5.7.
 * Zero violations to merge.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { runAxe } from '../fixtures/axe';

const routes: Array<{ name: string; path: string }> = [
  { name: 'Devices list', path: '/devices' },
  { name: 'Admin audit log', path: '/admin/audit' },
  { name: 'Admin brands', path: '/admin/brands' },
  { name: 'Admin categories', path: '/admin/categories' },
  { name: 'Admin owners', path: '/admin/owners' },
  { name: 'Admin locations', path: '/admin/locations' },
  { name: 'Admin networks', path: '/admin/networks' },
  { name: 'Admin tags', path: '/admin/tags' },
  { name: 'Admin import', path: '/admin/import' },
  { name: 'Admin export', path: '/admin/export' },
  { name: 'Offline shell', path: '/offline' }
];

test.describe('Journey 13: Accessibility smoke', () => {
  for (const route of routes) {
    test(`${route.name} (${route.path}) passes axe-core`, async ({ adminPage }) => {
      await adminPage.goto(route.path);
      await adminPage.waitForLoadState('networkidle');
      const violations = await runAxe(adminPage);
      expect(violations, JSON.stringify(violations, null, 2)).toEqual([]);
    });
  }
});

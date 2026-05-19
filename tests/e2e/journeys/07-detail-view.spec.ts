/**
 * Journey 7: Detail view (Viewer opens a device, sees all fields with
 * resolved references, role-aware action buttons, and a clean breadcrumb
 * trail back to the list)
 *
 * PRD §7.5.4: Critical user journey #7
 * The dev-bypass identity is Admin, so Edit + Delete + (Claim|Release) are
 * all expected to render. Reference resolution itself is tested at the API
 * level; here we assert the page shell + role gating.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedDevice } from '../fixtures/api';

test.describe('Journey 7: Detail view', () => {
  test('detail page renders breadcrumbs back to the list', async ({ adminPage, request }) => {
    const device = await seedDevice(request);
    await adminPage.goto(`/devices/${device.id}`);

    const breadcrumb = adminPage.getByRole('navigation', { name: /breadcrumb/i });
    await expect(breadcrumb).toBeVisible();
    await expect(breadcrumb.getByRole('link', { name: /home/i })).toBeVisible();
    await expect(breadcrumb.getByRole('link', { name: /devices/i })).toBeVisible();
    // The last crumb is the device name (or "Device" placeholder while
    // loading) — it lives as the aria-current item, not as a link.
    await expect(breadcrumb.locator('[aria-current="page"]')).toBeVisible();
  });

  test('Admin sees Edit and Delete action buttons', async ({ adminPage, request }) => {
    const device = await seedDevice(request);
    await adminPage.goto(`/devices/${device.id}`);

    await expect(adminPage.getByRole('link', { name: /^edit$/i })).toBeVisible();
    await expect(adminPage.getByRole('button', { name: /^delete$/i })).toBeVisible();
  });

  test('Admin sees the Claim ownership affordance when device has no owner', async ({ adminPage, request }) => {
    // Newly seeded devices have no ownerId, so the dev-admin sees Claim.
    const device = await seedDevice(request);
    await adminPage.goto(`/devices/${device.id}`);
    await expect(adminPage.getByRole('button', { name: /claim/i })).toBeVisible();
  });

  test('breadcrumb link returns to /devices', async ({ adminPage, request }) => {
    const device = await seedDevice(request);
    await adminPage.goto(`/devices/${device.id}`);

    await adminPage
      .getByRole('navigation', { name: /breadcrumb/i })
      .getByRole('link', { name: /devices/i })
      .click();

    await expect(adminPage).toHaveURL(/\/devices(\?.*)?$/);
    await expect(adminPage.getByRole('heading', { level: 1, name: /^devices$/i })).toBeVisible();
  });
});


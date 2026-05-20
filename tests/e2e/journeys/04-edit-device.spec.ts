/**
 * Journey 4: Edit device (Admin changes fields, saves, sees changes persisted)
 *
 * PRD §7.5.4: Critical user journey #4
 * Covers the dedicated /devices/{id}/edit page (not the create modal); edits
 * round-trip through PUT /api/v1/devices/{id} and the toast + redirect.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedDevice } from '../fixtures/api';

test.describe('Journey 4: Edit device', () => {
  test('Admin navigates to edit form from the detail page', async ({ adminPage, request }) => {
    const device = await seedDevice(request);

    await adminPage.goto(`/devices/${device.id}`);
    await expect(adminPage.getByRole('heading', { name: /device details/i })).toBeVisible();

    await adminPage.getByRole('link', { name: /^edit$/i }).click();
    await expect(adminPage).toHaveURL(new RegExp(`/devices/${device.id}/edit$`));
    await expect(adminPage.getByRole('heading', { name: /edit device/i })).toBeVisible();
  });

  test('Admin renames a device and the change persists in the list', async ({ adminPage, request }) => {
    const device = await seedDevice(request);
    const newName = `${device.name} edited`;

    await adminPage.goto(`/devices/${device.id}/edit`);
    await adminPage.locator('#name').fill(newName);

    await adminPage.getByRole('button', { name: /^save|update|submit$/i }).first().click();

    // Edit form redirects to the detail page on success
    await expect(adminPage).toHaveURL(new RegExp(`/devices/${device.id}$`), { timeout: 10_000 });

    // Confirm the new name is what the list shows
    await adminPage.goto(`/devices?search=${encodeURIComponent(newName)}`);
    await expect(adminPage.getByText(newName)).toBeVisible({ timeout: 10_000 });
  });

  test('Cancel returns to the detail page without saving', async ({ adminPage, request }) => {
    const device = await seedDevice(request);
    const tampered = `${device.name} should-not-save`;

    await adminPage.goto(`/devices/${device.id}/edit`);
    await adminPage.locator('#name').fill(tampered);

    await adminPage.getByRole('button', { name: /^cancel$/i }).click();
    await expect(adminPage).toHaveURL(new RegExp(`/devices/${device.id}$`));

    // Re-load the detail and confirm the rename never happened
    await adminPage.reload();
    await expect(adminPage.getByText(tampered)).toHaveCount(0);
  });
});


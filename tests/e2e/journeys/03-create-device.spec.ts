/**
 * Journey 3: Create device (Admin opens modal, fills form, sees new device in list)
 *
 * PRD §7.5.4: Critical user journey #3
 * D-137 / round-6 modal redesign: the create flow lives in AddDeviceModal,
 * not a separate /devices/new page (that page still exists as a fallback but
 * is no longer the primary surface).
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedBrand, seedCategory } from '../fixtures/api';

test.describe('Journey 3: Create device', () => {
  test('opens the Add Device modal from the list CTA', async ({ adminPage }) => {
    await adminPage.getByRole('button', { name: /add device/i }).click();
    const dialog = adminPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('heading', { name: /add device/i })).toBeVisible();
  });

  test('modal closes on Escape, X button, and Cancel', async ({ adminPage }) => {
    const openButton = adminPage.getByRole('button', { name: /add device/i });

    await openButton.click();
    await expect(adminPage.getByRole('dialog')).toBeVisible();
    await adminPage.keyboard.press('Escape');
    await expect(adminPage.getByRole('dialog')).not.toBeVisible();

    await openButton.click();
    const dialog = adminPage.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: /cancel|close/i }).first().click();
    await expect(adminPage.getByRole('dialog')).not.toBeVisible();
  });

  test('Admin creates a device end-to-end and sees it in the list', async ({ adminPage, request }) => {
    // Seed reference data the form requires (brand + category are mandatory
    // selects). The form's own Name field is the only thing the user types.
    const brand = await seedBrand(request);
    const category = await seedCategory(request);
    const deviceName = `Test Device ${Date.now()}`;

    await adminPage.goto('/devices');
    await adminPage.getByRole('button', { name: /add device/i }).click();
    const dialog = adminPage.getByRole('dialog');
    await expect(dialog).toBeVisible();

    await dialog.locator('#name').fill(deviceName);
    await dialog.locator('#brandId').selectOption({ label: brand.name });
    await dialog.locator('#categoryId').selectOption({ label: category.name });

    await dialog.getByRole('button', { name: /^save|create|submit$/i }).first().click();

    // Modal closes on success
    await expect(dialog).not.toBeVisible({ timeout: 10_000 });

    // The seeded device shows up after the list refetches
    await adminPage.goto(`/devices?search=${encodeURIComponent(deviceName)}`);
    await expect(adminPage.getByText(deviceName)).toBeVisible({ timeout: 10_000 });
  });
});


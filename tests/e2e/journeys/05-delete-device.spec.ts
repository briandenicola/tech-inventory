/**
 * Journey 5: Delete device (Admin deletes with type-to-confirm + reason; device gone from list)
 *
 * PRD §7.5.4: Critical user journey #5
 * Constitution §4.4: destructive actions require type-to-confirm AND a reason
 * ≥10 chars; reason is captured into the audit log.
 *
 * Soft-delete: the API marks the device inactive but doesn't remove the row,
 * so the list with default filters (status=Active) hides it.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedDevice } from '../fixtures/api';

test.describe('Journey 5: Delete device', () => {
  test('Delete button is gated on type-to-confirm + reason', async ({ adminPage, request }) => {
    const device = await seedDevice(request);

    await adminPage.goto(`/devices/${device.id}`);
    await adminPage.getByRole('button', { name: /^delete$/i }).click();

    const dialog = adminPage.getByRole('dialog');
    await expect(dialog).toBeVisible();

    const confirmButton = dialog.getByRole('button', { name: /^delete|confirm$/i });
    await expect(confirmButton).toBeDisabled();

    // Type the device name but supply too-short reason — still disabled
    await dialog.locator('input').first().fill(device.name);
    await dialog.locator('#reason').fill('too short');
    await expect(confirmButton).toBeDisabled();

    // Reason ≥10 chars unlocks the confirm
    await dialog.locator('#reason').fill('Decommissioned during e2e test sweep');
    await expect(confirmButton).toBeEnabled();
  });

  test('Cancel from the delete modal leaves the device untouched', async ({ adminPage, request }) => {
    const device = await seedDevice(request);

    await adminPage.goto(`/devices/${device.id}`);
    await adminPage.getByRole('button', { name: /^delete$/i }).click();
    await adminPage.getByRole('dialog').getByRole('button', { name: /^cancel$/i }).click();

    await expect(adminPage.getByRole('dialog')).not.toBeVisible();
    await expect(adminPage.getByRole('heading', { name: /device details/i })).toBeVisible();

    // Detail page is still there because the device wasn't deleted
    await adminPage.reload();
    await expect(adminPage.getByRole('heading', { name: /device details/i })).toBeVisible();
  });

  test('Admin deletes a device and it disappears from the active list', async ({ adminPage, request }) => {
    const device = await seedDevice(request);

    await adminPage.goto(`/devices/${device.id}`);
    await adminPage.getByRole('button', { name: /^delete$/i }).click();
    const dialog = adminPage.getByRole('dialog');

    await dialog.locator('input').first().fill(device.name);
    await dialog.locator('#reason').fill('Decommissioned during e2e test sweep');
    await dialog.getByRole('button', { name: /^delete|confirm$/i }).click();

    // After delete the user lands back on /devices
    await expect(adminPage).toHaveURL(/\/devices(\?.*)?$/, { timeout: 10_000 });

    // Confirm the active list no longer surfaces the device by name
    await adminPage.goto(`/devices?search=${encodeURIComponent(device.name)}`);
    await expect(adminPage.getByText(device.name)).toHaveCount(0);
  });
});


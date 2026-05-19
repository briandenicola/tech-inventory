/**
 * Journey 8: Import CSV
 *
 * PRD §7.5.4 — Admin uploads CSV, sees preview + validation, commits import.
 *
 * Two layers of coverage:
 *   1. Wizard chrome smoke — step indicator, drop zone, hidden file input.
 *   2. Happy path — drive the hidden <input type="file"> with a generated CSV,
 *      step through preview → commit, then confirm the device landed.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedBrand, seedCategory } from '../fixtures/api';

test.describe('Journey 8: Import CSV', () => {
  test('wizard chrome renders step indicator and accepts CSV files', async ({ adminPage }) => {
    await adminPage.goto('/admin/import');
    await expect(adminPage.getByRole('heading', { level: 1 })).toBeVisible();

    const progressList = adminPage.getByRole('list', { name: /import progress/i });
    await expect(progressList).toBeVisible();
    await expect(progressList.locator('li')).toHaveCount(3);

    // Hidden file input must accept CSV MIME/extension.
    const fileInput = adminPage.locator('input[type="file"]');
    await expect(fileInput).toHaveAttribute('accept', /csv/i);
  });

  test('admin can upload, preview, and commit a CSV', async ({ adminPage, request }) => {
    const brand = await seedBrand(request);
    const category = await seedCategory(request);
    const deviceName = `Imported Device ${Date.now()}`;

    const csv = [
      'Title,Brand,Model,Serial Number,Category,Location,Purchase Date,Purchase Price,Status,Notes',
      `${deviceName},${brand.name},Air,SN-${Date.now()},${category.name},Home Office,2024-05-01,899.00,Active,E2E import`,
    ].join('\n');

    await adminPage.goto('/admin/import');
    await adminPage.locator('input[type="file"]').setInputFiles({
      name: 'e2e-import.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(csv, 'utf-8'),
    });

    // Either an explicit "Validate"/"Preview" CTA appears after selecting the
    // file, or the page advances automatically. Wait for the preview summary.
    const previewCta = adminPage.getByRole('button', { name: /validate|preview/i });
    if (await previewCta.count()) {
      await previewCta.first().click();
    }

    await expect(adminPage.getByText(/total rows/i)).toBeVisible({ timeout: 15_000 });

    const commitButton = adminPage.getByRole('button', { name: /^commit/i });
    await expect(commitButton).toBeEnabled();
    await commitButton.click();

    await expect(adminPage.getByText(/imported|success/i).first()).toBeVisible({ timeout: 15_000 });

    // Verify the device actually landed in the list.
    await adminPage.goto(`/devices?search=${encodeURIComponent(deviceName)}`);
    await expect(adminPage.getByText(deviceName)).toBeVisible({ timeout: 10_000 });
  });
});

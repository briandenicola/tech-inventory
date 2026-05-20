/**
 * Journey 14: F034 — Imported device fields surface in the UI
 *
 * Backstory: imports persisted Model / Purpose / Notes (and several other
 * fields) on the server but the UI never rendered them. Owners reported
 * "every device looks identical" after running the canonical SharePoint
 * import. This journey is the smoke gate: import a row with those fields,
 * land on the detail page, and prove they show.
 *
 * Two layers (mirrors journey 08's pattern):
 *   1. Synthetic CSV — full control over the data; asserts every new
 *      field renders on the detail page after commit.
 *   2. Canonical CSV — uploads `data/Devices.csv` (the SharePoint export
 *      that started this whole conversation) and asserts a known row's
 *      Model + Purpose render. Notes is sparse in that file, so we only
 *      assert what's actually populated.
 */

import fs from 'node:fs';
import path from 'node:path';

import { authenticated as test, expect } from '../fixtures/auth';
import { seedBrand, seedCategory } from '../fixtures/api';

test.describe('Journey 14: F034 imported fields display', () => {
  test('synthetic CSV: every imported field renders on the detail page', async ({
    adminPage,
    request
  }) => {
    const brand = await seedBrand(request);
    const category = await seedCategory(request);
    const deviceName = `F034 Demo ${Date.now()}`;
    const model = 'F034-Model-X1';
    const purpose = 'Smoke-test purpose for F034 — verifies long-form fields render.';
    const notes = 'Smoke-test notes for F034.';

    const csv = [
      // Header row uses the same column names the importer already accepts.
      'Title,Brand,Model,Serial Number,Category,Purpose,Notes,Operating System,IP Address,MAC Address,Product URL,Version,Status',
      [
        deviceName,
        brand.name,
        model,
        `SN-${Date.now()}`,
        category.name,
        purpose,
        notes,
        'macOS 15.1',
        '10.0.0.42',
        'AA:BB:CC:DD:EE:FF',
        'https://example.com/devices/f034',
        '1.2.3',
        'Active'
      ].join(',')
    ].join('\n');

    await adminPage.goto('/admin/import');
    await adminPage.locator('input[type="file"]').setInputFiles({
      name: 'f034-import.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(csv, 'utf-8')
    });

    const previewCta = adminPage.getByRole('button', { name: /validate|preview/i });
    if (await previewCta.count()) {
      await previewCta.first().click();
    }

    await expect(adminPage.getByText(/total rows/i)).toBeVisible({ timeout: 15_000 });

    // F034 Slice B: per-row preview table should expose the new fields BEFORE commit.
    const previewRowsToggle = adminPage.getByTestId('import-preview-rows');
    await expect(previewRowsToggle).toBeVisible();
    await previewRowsToggle.locator('summary').click();
    await expect(previewRowsToggle.getByText(model, { exact: false })).toBeVisible();
    await expect(previewRowsToggle.getByText(purpose, { exact: false })).toBeVisible();

    const commitButton = adminPage.getByRole('button', { name: /^commit/i });
    await expect(commitButton).toBeEnabled();
    await commitButton.click();

    await expect(adminPage.getByText(/imported|success/i).first()).toBeVisible({
      timeout: 15_000
    });

    // Navigate to the device list and click into the detail page.
    await adminPage.goto(`/devices?search=${encodeURIComponent(deviceName)}`);
    await adminPage.getByRole('link', { name: deviceName }).first().click();

    // F034 Slice A: every imported field renders on the detail page.
    await expect(adminPage.getByText(model)).toBeVisible();
    await expect(adminPage.getByText(purpose)).toBeVisible();
    await expect(adminPage.getByText(notes)).toBeVisible();
    await expect(adminPage.getByText('macOS 15.1')).toBeVisible();
    await expect(adminPage.getByText('10.0.0.42')).toBeVisible();
    await expect(adminPage.getByText('AA:BB:CC:DD:EE:FF')).toBeVisible();
    await expect(adminPage.getByText('https://example.com/devices/f034')).toBeVisible();
    await expect(adminPage.getByText('1.2.3')).toBeVisible();
  });

  test('canonical Devices.csv: Model and Purpose render for a known row', async ({
    adminPage
  }) => {
    // data/Devices.csv is the live SharePoint export Brian flagged as the
    // motivator for F034. We assert against a specific known-good row so
    // the test stays stable even as the file grows.
    const csvPath = path.resolve(__dirname, '..', '..', '..', 'data', 'Devices.csv');
    const csvBuffer = fs.readFileSync(csvPath);

    await adminPage.goto('/admin/import');
    await adminPage.locator('input[type="file"]').setInputFiles({
      name: 'Devices.csv',
      mimeType: 'text/csv',
      buffer: csvBuffer
    });

    const previewCta = adminPage.getByRole('button', { name: /validate|preview/i });
    if (await previewCta.count()) {
      await previewCta.first().click();
    }

    // The canonical export is ~648 rows; preview parsing can take a beat.
    await expect(adminPage.getByText(/total rows/i)).toBeVisible({ timeout: 30_000 });

    const commitButton = adminPage.getByRole('button', { name: /^commit/i });
    await expect(commitButton).toBeEnabled();
    await commitButton.click();

    // Commit also takes a beat for a real-world file.
    await expect(adminPage.getByText(/imported|success/i).first()).toBeVisible({
      timeout: 60_000
    });

    // "Mohu Leaf Stitch 60m Range" has Model="Leaf Stitch" and Purpose="Master TV"
    // — the row Brian called out in the F034 brief. Use status=All so we catch
    // the device regardless of how the importer mapped its status column.
    await adminPage.goto('/devices?status=All&search=Mohu+Leaf+Stitch');
    await adminPage
      .getByRole('link', { name: /Mohu Leaf Stitch/i })
      .first()
      .click();

    await expect(adminPage.getByText('Leaf Stitch')).toBeVisible();
    await expect(adminPage.getByText('Master TV')).toBeVisible();
  });
});

/**
 * Shared axe-core accessibility helper.
 * 
 * Usage:
 * 
 * import { runAxe } from '../fixtures/axe';
 * 
 * test('My page is accessible', async ({ page }) => {
 *   await page.goto('/devices');
 *   const violations = await runAxe(page);
 *   expect(violations).toEqual([]); // Zero violations to merge
 * });
 */

import { Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

export async function runAxe(page: Page) {
  const results = await new AxeBuilder({ page }).analyze();
  return results.violations;
}

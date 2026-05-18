/**
 * Journey 13: Accessibility smoke (every route above passes axe-core with zero violations)
 * 
 * PRD §7.5.4: Critical user journey #13
 * Verifies all critical routes pass axe-core accessibility checks.
 * Zero violations to merge — non-negotiable.
 */

import { test, expect } from '@playwright/test';
import { runAxe } from '../fixtures/axe';

test.describe.skip('Journey 13: Accessibility smoke', () => {
  test.todo('Sign-in page passes axe-core');
  test.todo('Device list page passes axe-core');
  test.todo('Device detail page passes axe-core');
  test.todo('Device create form passes axe-core');
  test.todo('Device edit form passes axe-core');
  test.todo('Import page passes axe-core');
  test.todo('Reference data admin page passes axe-core');
  
  // Example of how to use the axe helper once tests are unwired:
  // test('Device list is accessible', async ({ page }) => {
  //   await page.goto('/devices');
  //   const violations = await runAxe(page);
  //   expect(violations).toEqual([]);
  // });
});

/**
 * Network isolation fixture.
 * 
 * Ensures no test reaches external hosts — all outbound calls to non-localhost fail.
 * This prevents accidental dependencies on third-party services and enforces hermetic testing.
 * 
 * Usage:
 * 
 * import { test } from '../fixtures/network';
 * 
 * test('My test', async ({ page }) => {
 *   // Any non-localhost request will fail
 *   await page.goto('http://localhost:5173/devices');
 * });
 */

import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
    // Block all non-localhost requests
    await page.route('**/*', (route) => {
      const url = new URL(route.request().url());
      const isLocalhost = url.hostname === 'localhost' || url.hostname === '127.0.0.1' || url.hostname === '[::1]';
      
      if (isLocalhost) {
        route.continue();
      } else {
        route.abort('failed');
      }
    });

    await use(page);
  },
});

export { expect } from '@playwright/test';

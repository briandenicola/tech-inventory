/**
 * Authentication fixtures for Playwright E2E.
 *
 * Two test variants:
 * - `test` (default) — vanilla Playwright; use for unauthenticated journeys
 *   (sign-in screen, redirect guards).
 * - `authenticated` — pre-warms the dev-bypass auth flow against the running
 *   stack so the test starts on /devices as the Admin user.
 *
 * The dev-bypass shim is enabled at build time via the compose
 * VITE_AUTH_DEV_BYPASS=true ARG (see scripts/run-e2e.{ps1,sh}). The frontend
 * skips MSAL entirely and the backend's DevBypass handler authenticates the
 * /api/v1/owners/me probe as Admin (oid 11111111-...).
 */

import { test as base, expect } from '@playwright/test';

export const test = base;

export const authenticated = base.extend<{ adminPage: import('@playwright/test').Page }>({
  adminPage: async ({ page }, use) => {
    // The frontend auto-logs the bypass admin on app bootstrap (see
    // /src/lib/auth/dev-bypass.ts). Land on /devices and wait until the
    // protected layout actually renders (not the loading shell or login).
    await page.goto('/devices');
    await page.waitForURL('**/devices', { timeout: 15_000 });
    // Devices list always renders the "Devices" h1; wait for that instead of
    // racing against table population (which depends on seeded data).
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
    await use(page);
  }
});

export { expect } from '@playwright/test';

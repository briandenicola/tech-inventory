/**
 * Journey 1: Sign in (F025 local-account path; unauth-redirect + root render)
 *
 * PRD §7.5.4: Critical user journey #1
 *
 * The E2E stack no longer ships the Auth:DevBypass shim. Real sign-in goes
 * through `POST /api/v1/auth/local/login` (see tests/e2e/fixtures/auth.ts);
 * this spec covers the *un*authenticated bookends:
 *   1. The (authenticated) route group's guard redirects to /auth/login when
 *      the auth store finishes bootstrapping without a session.
 *   2. The root route renders without throwing on an unauthenticated boot.
 */

import { test, expect } from '../fixtures/auth';

test.describe('Journey 1: Sign in', () => {
  test('unauthenticated request to a protected route redirects to /auth/login', async ({ page }) => {
    // Clear any cached auth state so the bootstrap reruns from scratch.
    await page.context().clearCookies();

    await page.goto('/devices');
    // The (authenticated)/+layout.ts guard waits until the root layout's
    // onMount finishes its MSAL probe + reports `isLoading=false` before
    // throwing the 302. Use waitForURL so we don't race that handoff.
    await page.waitForURL('**/auth/login', { timeout: 15_000 });
    expect(new URL(page.url()).pathname).toBe('/auth/login');
  });

  test('root route renders without error', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (err) => errors.push(err.message));
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    expect(errors, errors.join('\n')).toEqual([]);
  });
});

/**
 * Authentication fixtures for Playwright E2E.
 *
 * Two test variants:
 * - `test` (default) — vanilla Playwright; use for unauthenticated journeys
 *   (sign-in screen, redirect guards).
 * - `authenticated` — pre-warms an F025 local-account sign-in against the
 *   running stack so the test starts on /devices as the seeded admin user.
 *
 * The seeded admin is created at api boot by `LocalAdminSeedHostedService`
 * (Auth:Local:Seed* config from docker-compose.e2e.yml). This fixture exchanges
 * the seed creds at POST /api/v1/auth/local/login for an HS256 JWT, decodes
 * the meta payload, injects both into sessionStorage via `page.addInitScript`
 * (so the SvelteKit root layout's `hydrateLocalSession()` picks them up on
 * first navigation), and also exposes an authenticated `APIRequestContext`
 * via the overridden `request` fixture so journey specs that seed data
 * (`seedBrand`, `seedDevice`, etc.) carry a bearer token automatically.
 *
 * Override the seed creds via E2E_SEED_USERNAME / E2E_SEED_PASSWORD if you
 * change them in docker-compose.e2e.yml.
 */

import { test as base, expect } from '@playwright/test';
import type { APIRequestContext, Page } from '@playwright/test';

export const SEED_USERNAME = process.env.E2E_SEED_USERNAME ?? 'e2e-admin';
export const SEED_PASSWORD = process.env.E2E_SEED_PASSWORD ?? 'e2e-admin-passw0rd-CHANGEME';

interface LocalSessionMeta {
  subjectId: string;
  displayName: string;
  role: 'Admin' | 'Member' | 'Viewer';
  mustChangePassword: boolean;
  expiresAtUtc: number;
}

interface SeedSession {
  token: string;
  meta: LocalSessionMeta;
}

/**
 * Exchange the seeded admin's credentials for a local-issuer JWT.
 *
 * Uses a one-shot `APIRequestContext` so the login POST doesn't carry any
 * stale headers from the test's own context. The caller is responsible for
 * disposing nothing — we tear the temp context down inline.
 */
async function seedLogin(
  playwright: typeof import('@playwright/test'),
  baseURL: string | undefined
): Promise<SeedSession> {
  const ctx = await playwright.request.newContext({ baseURL });
  try {
    const res = await ctx.post('/api/v1/auth/local/login', {
      data: { username: SEED_USERNAME, password: SEED_PASSWORD }
    });
    if (!res.ok()) {
      throw new Error(
        `Seed login failed (${res.status()}) for ${SEED_USERNAME}: ${await res.text()}`
      );
    }
    const json = (await res.json()) as { accessToken?: string };
    if (!json.accessToken) {
      throw new Error('Seed login succeeded but the response had no accessToken.');
    }
    return { token: json.accessToken, meta: decodeMeta(json.accessToken) };
  } finally {
    await ctx.dispose();
  }
}

/**
 * Decode an HS256 JWT payload (no signature verification — we trust the
 * token we just minted). Mirrors the shape `setLocalSession` writes in
 * src/TechInventory.Web/src/lib/auth/local-session.ts so the SvelteKit
 * `auth` store hydrates cleanly on first navigation.
 */
function decodeMeta(token: string): LocalSessionMeta {
  const parts = token.split('.');
  if (parts.length !== 3) {
    throw new Error(`Malformed JWT: expected 3 segments, got ${parts.length}.`);
  }
  const padded = parts[1].replace(/-/g, '+').replace(/_/g, '/');
  const padding = padded.length % 4 === 0 ? '' : '='.repeat(4 - (padded.length % 4));
  const payload = JSON.parse(Buffer.from(padded + padding, 'base64').toString('utf8')) as {
    sub?: string;
    name?: string;
    role?: string;
    must_change_password?: string;
    exp?: number;
  };
  return {
    subjectId: payload.sub ?? '',
    displayName: payload.name ?? 'Local user',
    role: (payload.role as LocalSessionMeta['role']) ?? 'Viewer',
    mustChangePassword: payload.must_change_password === 'true',
    expiresAtUtc: payload.exp ?? Math.floor(Date.now() / 1000) + 3600
  };
}

export const test = base;

export const authenticated = base.extend<{
  adminPage: Page;
  request: APIRequestContext;
}>({
  /**
   * Overrides Playwright's default `request` fixture so journey specs that
   * destructure `{ adminPage, request }` get an authenticated context — every
   * /api/v1/* route requires a bearer token, so a bare APIRequestContext (no
   * Authorization header) would 401 on every POST the seeders make.
   */
  request: async ({ playwright, baseURL }, use) => {
    const { token } = await seedLogin(playwright, baseURL);
    const ctx = await playwright.request.newContext({
      baseURL,
      extraHTTPHeaders: { Authorization: `Bearer ${token}` }
    });
    try {
      await use(ctx);
    } finally {
      await ctx.dispose();
    }
  },

  adminPage: async ({ page, playwright, baseURL }, use) => {
    const { token, meta } = await seedLogin(playwright, baseURL);

    // Inject the local-account session BEFORE any in-page script runs so the
    // root +layout.svelte's onMount calls `hydrateLocalSession()` against a
    // populated sessionStorage and lands the user as authenticated without
    // ever touching MSAL. `addInitScript` re-runs on every navigation, which
    // also covers the case where a test calls `page.reload()`.
    await page.addInitScript(
      ([t, m]) => {
        sessionStorage.setItem('ti_local_token', t);
        sessionStorage.setItem('ti_local_meta', m);
      },
      [token, JSON.stringify(meta)]
    );

    await page.goto('/devices');
    await page.waitForURL('**/devices', { timeout: 15_000 });
    // Devices list always renders the "Devices" h1; wait for that instead of
    // racing against table population (which depends on seeded data).
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
    await use(page);
  }
});

export { expect } from '@playwright/test';

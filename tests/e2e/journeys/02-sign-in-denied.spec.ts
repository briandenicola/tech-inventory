/**
 * Journey 2: Sign in denied / sign-out flow (user can leave the app cleanly)
 *
 * PRD §7.5.4: Critical user journey #2
 *
 * The original spec covered "user without role assignment is denied with a
 * helpful message", but the F025 local-account seed that powers our E2E stack
 * always authenticates as Admin — there is no in-band way to simulate a
 * role-less user without minting a separate seeded account. The bits we *can*
 * exercise are the user-pill dropdown's Sign Out affordance and that it is
 * keyboard-reachable + correctly labelled. Full sign-out-clears-session
 * coverage is now achievable in principle (handleSignOut() calls clearAuth()
 * which drops both MSAL and local-session state) — see the test.fixme below.
 */

import { authenticated as test, expect } from '../fixtures/auth';

// The user-pill button's accessible name comes from its `aria-label`, which
// the layout sets from the i18n catalog ("header.userMenu" → "User menu").
// That's stable across both the Entra and F025-local sign-in paths and is
// what we match on — the visible displayName underneath happens to be
// "Local admin (e2e-admin)" in this stack but isn't part of the button's
// accessible name.
const USER_PILL = /user menu/i;

test.describe('Journey 2: Sign out', () => {
  test('user pill dropdown exposes a Sign Out action', async ({ adminPage }) => {
    const trigger = adminPage.getByRole('button', { name: USER_PILL });
    await expect(trigger).toBeVisible();
    // Sanity-check that the F025 local-admin displayName is wired through to
    // the header chip — a regression here means the JWT `name` claim or the
    // auth store hydration is broken.
    await expect(trigger).toContainText(/local admin/i);
    await trigger.click();

    const signOut = adminPage.getByRole('menuitem', { name: /sign out/i });
    await expect(signOut).toBeVisible();
    await expect(signOut).toBeEnabled();
  });

  test('user pill dropdown is keyboard-reachable and Escape closes it', async ({ adminPage }) => {
    const trigger = adminPage.getByRole('button', { name: USER_PILL });
    await trigger.focus();
    await adminPage.keyboard.press('Enter');

    const signOut = adminPage.getByRole('menuitem', { name: /sign out/i });
    await expect(signOut).toBeVisible();

    await adminPage.keyboard.press('Escape');
    await expect(signOut).not.toBeVisible();
  });

  test.fixme(
    'clicking Sign Out clears session and redirects to /auth/login',
    async () => {
      // Now plausible under F025 — handleSignOut() calls clearAuth() which
      // drops both MSAL and the local sessionStorage keys, so a follow-up
      // navigation should hit the (authenticated) guard and bounce to
      // /auth/login. Left as fixme until someone wires up a journey that
      // doesn't fight Playwright's per-test addInitScript re-hydration
      // (the `adminPage` fixture re-injects ti_local_token on every nav).
    }
  );
});


/**
 * Journey 2: Sign in denied / sign-out flow (user can leave the app cleanly)
 *
 * PRD §7.5.4: Critical user journey #2
 *
 * The original spec covered "user without role assignment is denied with a
 * helpful message", but the dev-bypass shim that powers our E2E stack always
 * authenticates as Admin — there is no in-band way to simulate a role-less
 * user without rebuilding the web image. The bits we *can* exercise are the
 * user-pill dropdown's Sign Out affordance and that it is keyboard-reachable
 * + correctly labelled; full sign-out-clears-session coverage is deferred to
 * a follow-up that teaches the bypass shim to honour a runtime override.
 */

import { authenticated as test, expect } from '../fixtures/auth';

test.describe('Journey 2: Sign out', () => {
  test('user pill dropdown exposes a Sign Out action', async ({ adminPage }) => {
    const trigger = adminPage.getByRole('button', { name: /dev-admin/i });
    await expect(trigger).toBeVisible();
    await trigger.click();

    const signOut = adminPage.getByRole('menuitem', { name: /sign out/i });
    await expect(signOut).toBeVisible();
    await expect(signOut).toBeEnabled();
  });

  test('user pill dropdown is keyboard-reachable and Escape closes it', async ({ adminPage }) => {
    const trigger = adminPage.getByRole('button', { name: /dev-admin/i });
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
      // Blocked: dev-bypass auto-rehydrates the admin account on bootstrap, so
      // a real sign-out test needs the bypass shim to honour a runtime kill
      // switch (e.g. a session-storage flag set by handleLogout). File as a
      // small backlog item before enabling.
    }
  );
});


/**
 * Journey 15: Installed-PWA app shell (F045)
 *
 * Design authority: `specs/_backlog/F045-pwa-shell-and-device-list.md` +
 * `.squad/decisions/inbox/ripley-pwa-device-navigation.md` (D-175..D-179).
 *
 * At the time this spec was authored, Vasquez's implementation
 * (`displayMode.svelte.ts`, `AppBottomNav`, `AppMenuPopover`, `DevicePwaRow`,
 * the `DeviceTable` renderer split) had not yet landed on this branch — this
 * file is written against the F045 contract ahead of the implementation, per
 * the parallel-workstream boundary in the design doc. Every test here is
 * expected to fail red until that lands; see the selector-assumption doc
 * block in `../pages/AppShellPage.ts` for the exact contract points Vasquez
 * must satisfy for these to go green, and Apone's delivery report for the
 * same list.
 *
 * Scope note: only the seeded local Admin account exists for Playwright
 * E2E (see `fixtures/auth.ts` / `docker-compose.e2e.yml`) — Member/Viewer
 * sign-in has no fixture (Journey 11 is entirely `test.describe.skip`d for
 * the same reason). The three-role (Admin/Member/Viewer) popover
 * option-set/order regression that F045 §6.1 calls out is therefore a
 * Vitest responsibility (`AppMenuPopover.svelte` snapshot tests); this file
 * only asserts the Admin-role option set/order end-to-end.
 */

import { authenticated as test, expect } from '../fixtures/auth';
import { seedBrand, seedCategory, seedDevice } from '../fixtures/api';
import { runAxe } from '../fixtures/axe';
import { AppShellPage, emulateStandalonePwa } from '../pages/AppShellPage';

const IPHONE_VIEWPORT = { width: 390, height: 844 }; // iPhone 13-ish, per task's "iPhone-sized viewport" ask
const MOBILE_WEB_VIEWPORT = { width: 375, height: 667 };
const DESKTOP_VIEWPORT = { width: 1280, height: 800 };

test.describe('Journey 15: PWA app shell', () => {
  test.describe('Mode gating is standalone-only, never viewport-only (D-175)', () => {
    test('standalone + narrow viewport gets the bottom nav', async ({ adminPage }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa('/devices');
      await expect(shell.bottomNav).toBeVisible();
    });

    test('narrow mobile web (not standalone) does NOT get the bottom nav', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(MOBILE_WEB_VIEWPORT);
      // No emulateStandalonePwa() call — this is the un-emulated, real
      // "mobile browser tab" case the design doc says must stay unchanged.
      await adminPage.goto('/devices');
      await expect(shell.bottomNav).toHaveCount(0);
      // Existing mobile-web affordances must still be present/unaffected.
      await expect(shell.mobileAddFab).toBeVisible();
    });

    test('wide desktop browser (not standalone) does NOT get the bottom nav', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(DESKTOP_VIEWPORT);
      await adminPage.goto('/devices');
      await expect(shell.bottomNav).toHaveCount(0);
    });
  });

  test.describe('Bottom nav structure & accessibility (§5.2, F045 §7 a11y gates)', () => {
    test.beforeEach(async ({ adminPage }) => {
      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await new AppShellPage(adminPage).gotoAsPwa('/devices');
    });

    test('Home / Add / Reports share one pill; Settings is a separate bubble', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await expect(shell.navPill).toBeVisible();
      await expect(shell.homeItem).toBeVisible();
      await expect(shell.addItem).toBeVisible();
      await expect(shell.reportsItem).toBeVisible();

      // The three pill items are inside role="group"...
      for (const item of [shell.homeItem, shell.addItem, shell.reportsItem]) {
        await expect(shell.navPill).toContainText(await item.innerText());
      }
      // ...Settings must NOT be one of the group's children, whether found
      // by text (legacy check) or by its actual accessible role/name.
      await expect(
        shell.navPill.locator('a, button').filter({ hasText: /settings/i })
      ).toHaveCount(0);
      await expect(shell.navPill.getByRole('link', { name: /settings/i })).toHaveCount(0);
      await expect(shell.navPill.getByRole('button', { name: /settings/i })).toHaveCount(0);
      await expect(shell.settingsItem).toBeVisible();
    });

    test('pill items (Home / Add / Reports) each have a visible, non-empty text label', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      for (const item of [shell.homeItem, shell.addItem, shell.reportsItem]) {
        await expect(item).toBeVisible();
        const text = (await item.innerText()).trim();
        expect(
          text.length,
          'pill nav item must render a visible text label, not icon-only'
        ).toBeGreaterThan(0);
      }
    });

    // D-180: Settings is intentionally icon-only (Drake D-D geometry). It
    // only needs an accessible name (aria-label/title), not a visible text
    // node — §7.3 is narrowed to the pill items only.
    test('Settings bubble is icon-only but exposes an accessible name', async ({ adminPage }) => {
      const shell = new AppShellPage(adminPage);
      await expect(shell.settingsItem).toBeVisible();
      const accessibleName = await shell.settingsItem.evaluate(
        (el) => el.getAttribute('aria-label') || el.getAttribute('title') || ''
      );
      expect(
        accessibleName.trim().length,
        'Settings must expose an accessible name via aria-label/title'
      ).toBeGreaterThan(0);
    });

    test('nav items meet the 44x44 CSS px minimum hit target (constitution §6.5.6)', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      for (const item of [shell.homeItem, shell.addItem, shell.reportsItem, shell.settingsItem]) {
        const box = await item.boundingBox();
        expect(box, 'nav item must have a layout box').not.toBeNull();
        expect(box!.width).toBeGreaterThanOrEqual(44);
        expect(box!.height).toBeGreaterThanOrEqual(44);
      }
    });

    test('active item (Home, on /devices) carries aria-current="page"', async ({ adminPage }) => {
      const shell = new AppShellPage(adminPage);
      await expect(shell.homeItem).toHaveAttribute('aria-current', 'page');
    });

    test('bottom nav passes axe-core with zero violations', async ({ adminPage }) => {
      const violations = await runAxe(adminPage);
      expect(violations, JSON.stringify(violations, null, 2)).toEqual([]);
    });
  });

  test.describe('Title bar controls + search placement contract (§5.6)', () => {
    test('app mode: H1 + Filter stay, both Add affordances (desktop CTA + FAB) are gone', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa('/devices');

      await expect(adminPage.getByRole('heading', { level: 1 })).toBeVisible();
      await expect(shell.filterButton).toBeVisible();
      // Add now lives only in the bottom nav in app mode.
      await expect(shell.desktopAddDeviceButton).toHaveCount(0);
      await expect(shell.mobileAddFab).toHaveCount(0);
      await expect(shell.addItem).toBeVisible();
    });

    test('app mode on a wide viewport: search drops the md:max-w-lg cap (w-full)', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(DESKTOP_VIEWPORT);
      await shell.gotoAsPwa('/devices');
      const box = await shell.searchInput.boundingBox();
      expect(box).not.toBeNull();
      // md:max-w-lg is 512px; a genuinely full-width search on a 1280px
      // viewport should clear that comfortably.
      expect(box!.width).toBeGreaterThan(600);
    });

    test('regression: wide desktop browser (not standalone) keeps the capped search width', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(DESKTOP_VIEWPORT);
      await adminPage.goto('/devices');
      const box = await shell.searchInput.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.width).toBeLessThanOrEqual(560);
    });
  });

  test.describe('Implicit category grouping default (D-177)', () => {
    test('no groupBy param: app mode groups by category without writing the URL or the filter count', async ({
      adminPage,
      request
    }) => {
      const shell = new AppShellPage(adminPage);
      const categoryA = await seedCategory(request);
      const categoryB = await seedCategory(request);
      const brand = await seedBrand(request);
      await seedDevice(request, { brandId: brand.id, categoryId: categoryA.id });
      await seedDevice(request, { brandId: brand.id, categoryId: categoryB.id });

      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa('/devices');

      await expect(shell.groupSections.first()).toBeVisible({ timeout: 10_000 });
      const url = new URL(adminPage.url());
      expect(
        url.searchParams.get('groupBy'),
        'implicit default must not be written to the URL'
      ).toBeNull();

      // Implicit grouping must not count as an active filter.
      await expect(
        shell.filterButton.locator('span').filter({ hasText: /^\d+$/ })
      ).toHaveCount(0);
    });

    test('?groupBy=none opts out of the implicit default back to a flat list', async ({
      adminPage,
      request
    }) => {
      const shell = new AppShellPage(adminPage);
      const brand = await seedBrand(request);
      const category = await seedCategory(request);
      await seedDevice(request, { brandId: brand.id, categoryId: category.id });

      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa('/devices?groupBy=none');

      await expect(shell.groupSections).toHaveCount(0);
    });
  });

  test.describe('Two-line device row + ellipsis actions (§5.4)', () => {
    test('row renders name / brand·model on two lines with a separately reachable actions menu', async ({
      adminPage,
      request
    }) => {
      const shell = new AppShellPage(adminPage);
      const brand = await seedBrand(request);
      const category = await seedCategory(request);
      const device = await seedDevice(request, {
        brandId: brand.id,
        categoryId: category.id,
        model: 'Model Z',
        name: `PWA Row Device ${Date.now()}`
      });

      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa(`/devices?search=${encodeURIComponent(device.name)}`);

      const row = shell.deviceRow(device.name);
      await expect(row).toBeVisible({ timeout: 10_000 });
      await expect(row).toContainText(device.name);
      await expect(row).toContainText(brand.name);
      await expect(row).toContainText('Model Z');

      // Actions menu is reachable and distinct from the row-open control.
      const actionsTrigger = shell.actionsMenuTriggerFor(device.name);
      await expect(actionsTrigger).toBeVisible();
      await actionsTrigger.focus();
      await expect(actionsTrigger).toBeFocused();
      await adminPage.keyboard.press('Enter');
      await expect(adminPage.getByRole('menu')).toBeVisible();
      // Opening the actions menu must not also open the device detail modal.
      await expect(adminPage.getByRole('dialog')).toHaveCount(0);
    });
  });

  test.describe('Compact hamburger popover (§5.3, D-179)', () => {
    test.beforeEach(async ({ adminPage }) => {
      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await new AppShellPage(adminPage).gotoAsPwa('/devices');
    });

    test('option set + order match the legacy drawer for the Admin role', async ({ adminPage }) => {
      const shell = new AppShellPage(adminPage);
      await shell.hamburgerTrigger.click();
      await expect(shell.menuPopover).toBeVisible();

      const texts = (await shell.menuPopoverItems.allTextContents()).map((t) => t.trim());
      const expectedOrder = [
        /devices/i,
        /reports/i,
        /import/i,
        /export/i,
        /audit/i,
        /brands/i,
        /categories/i,
        /locations/i,
        /networks/i,
        /owners/i,
        /tags/i,
        /settings/i,
        /sign out/i
      ];
      expect(texts.length).toBe(expectedOrder.length);
      expectedOrder.forEach((pattern, i) => {
        expect(texts[i], `menu item ${i} ("${texts[i]}") should match ${pattern}`).toMatch(pattern);
      });
    });

    test('keyboard: Enter opens, focus moves to first item, Escape closes and restores focus', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await shell.hamburgerTrigger.focus();
      await adminPage.keyboard.press('Enter');
      await expect(shell.menuPopover).toBeVisible();
      await expect(shell.menuPopoverItems.first()).toBeFocused();

      await adminPage.keyboard.press('Escape');
      await expect(shell.menuPopover).not.toBeVisible();
      await expect(shell.hamburgerTrigger).toBeFocused();
    });

    test('outside click closes the popover without a full-screen backdrop/dialog', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await shell.hamburgerTrigger.click();
      await expect(shell.menuPopover).toBeVisible();

      // No full-screen dialog/sheet: the popover must not claim the whole
      // viewport, and there must be no role="dialog" backdrop element.
      const box = await shell.menuPopover.boundingBox();
      const viewport = adminPage.viewportSize()!;
      expect(box).not.toBeNull();
      expect(box!.width).toBeLessThan(viewport.width * 0.9);
      await expect(adminPage.getByRole('dialog')).toHaveCount(0);

      // Body must not be scroll-locked while the popover is open.
      const overflow = await adminPage.evaluate(() => document.body.style.overflow);
      expect(overflow).not.toBe('hidden');

      // The underlying content (H1) must still be hit-testable — click it.
      await adminPage.getByRole('heading', { level: 1 }).click();
      await expect(shell.menuPopover).not.toBeVisible();
    });
  });

  test.describe('Desktop browser regression (must pass unchanged — Ripley escalation gate)', () => {
    test('no bottom PWA toolbar; existing table + hamburger-free desktop nav intact', async ({
      adminPage
    }) => {
      const shell = new AppShellPage(adminPage);
      await adminPage.setViewportSize(DESKTOP_VIEWPORT);
      await adminPage.goto('/devices');

      await expect(shell.bottomNav).toHaveCount(0);
      await expect(adminPage.locator('table')).toBeVisible();
      // Desktop uses the user-menu dropdown as its sole nav entry, not a
      // hamburger — the hamburger button is `md:hidden`.
      await expect(shell.hamburgerTrigger).not.toBeVisible();
    });

    test('Table Columns preference still hides a column in the desktop table', async ({
      adminPage
    }) => {
      await adminPage.setViewportSize(DESKTOP_VIEWPORT);
      await adminPage.goto('/settings');
      await adminPage.getByRole('checkbox', { name: /^model$/i }).uncheck();

      await adminPage.goto('/devices');
      await expect(adminPage.locator('table')).toBeVisible();
      await expect(adminPage.getByRole('columnheader', { name: /^model$/i })).toHaveCount(0);
    });
  });

  test.describe('App-mode accessibility at an iPhone-sized viewport (F045 §7 / §6.2)', () => {
    test('devices list passes axe-core with zero violations in app mode', async ({ adminPage }) => {
      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await emulateStandalonePwa(adminPage);
      await adminPage.goto('/devices');
      await adminPage.waitForLoadState('networkidle');
      const violations = await runAxe(adminPage);
      expect(violations, JSON.stringify(violations, null, 2)).toEqual([]);
    });

    test('no primary control is obscured by the fixed bottom nav', async ({
      adminPage,
      request
    }) => {
      const shell = new AppShellPage(adminPage);
      const brand = await seedBrand(request);
      const category = await seedCategory(request);
      const device = await seedDevice(request, {
        brandId: brand.id,
        categoryId: category.id,
        name: `PWA Obscure Check ${Date.now()}`
      });

      await adminPage.setViewportSize(IPHONE_VIEWPORT);
      await shell.gotoAsPwa(`/devices?search=${encodeURIComponent(device.name)}`);

      const row = shell.deviceRow(device.name);
      await expect(row).toBeVisible({ timeout: 10_000 });
      await row.scrollIntoViewIfNeeded();

      const rowBox = await row.boundingBox();
      const navBox = await shell.bottomNav.boundingBox();
      expect(rowBox).not.toBeNull();
      expect(navBox).not.toBeNull();
      // The row's bottom edge must clear the nav's top edge — i.e. the
      // list's bottom padding (§5.6) keeps the last rows from sitting
      // underneath the translucent pill.
      expect(rowBox!.y + rowBox!.height).toBeLessThanOrEqual(navBox!.y);
    });
  });
});

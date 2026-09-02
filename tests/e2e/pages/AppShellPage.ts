/**
 * Page object for F045's installed-PWA app shell: the bottom nav pill +
 * Settings bubble (`AppBottomNav.svelte`), the compact anchored menu
 * (`AppMenuPopover.svelte`), and the two-line device rows
 * (`DevicePwaRow.svelte`). See `specs/_backlog/F045-pwa-shell-and-device-list.md`
 * and `.squad/decisions/inbox/ripley-pwa-device-navigation.md` (D-175..D-179).
 *
 * ## Standalone-mode emulation (F045 R8)
 * Playwright cannot natively emulate `display-mode: standalone`. Per the
 * spec's own risk mitigation, we inject a `matchMedia` override via
 * `addInitScript` so `isStandalonePwa()` (src/lib/auth/index.ts) resolves to
 * `true` on first paint, mirroring the real `(display-mode: standalone)`
 * media query. We do NOT touch `navigator.standalone` — that branch is
 * iOS-Safari-only and irrelevant to the Chromium/WebKit/Firefox runners this
 * suite targets.
 *
 * ## Selector assumptions Vasquez's implementation must satisfy
 * These are inferred from the F045 contract text where it doesn't pin an
 * exact `data-testid`/copy. Flagged here (and repeated in Apone's report) so
 * a mismatch is a one-line fix instead of a mystery failure:
 *
 * 1. **Bottom nav landmark** — located as "the `<nav>` that contains a link
 *    named /home/i", not by the `navigation.primary` aria-label text (not
 *    fixed yet). Only one `<nav>` should satisfy that filter in app mode.
 * 2. **Nav items** — accessible name is expected to contain "home", "add",
 *    "report", and "settings" (case-insensitive substrings), regardless of
 *    the exact i18n copy behind `navigation.home` / `.add` / `.reportsShort`
 *    / `.settingsShort`.
 * 3. **Add** is expected to be a `<button>` (it dispatches `onAdd`, not a
 *    route) named exactly "Add" (not "Add Device") so it doesn't collide
 *    with the desktop CTA / `AddDeviceFab`, both literally named
 *    "Add Device" / "Add device".
 * 4. **Settings bubble** — expected to be a sibling of the pill inside the
 *    same `<nav>`, not a descendant of the `role="group"` pill.
 * 5. **Popover** — targeted via `getByRole('menu')`. Safe only because the
 *    existing desktop user-menu dropdown (also `role="menu"`) is
 *    `hidden md:block` and therefore absent from the accessibility tree at
 *    narrow/app-mode viewports. If a second visible `role="menu"` is ever
 *    introduced at narrow widths, scope this by the hamburger trigger's
 *    `aria-controls` instead.
 * 6. **Device rows** — `DevicePwaRow.svelte` root is assumed to carry
 *    `data-testid="device-pwa-row"`, matching the existing
 *    `device-group-header` / `device-group-section-mobile` convention
 *    already in `DeviceTable.svelte`. This exact testid is NOT spelled out
 *    in F045 §5.4 — it is Apone's requested addition, called out explicitly
 *    in the delivery report.
 */

import type { Page, Locator } from '@playwright/test';

/**
 * Override `window.matchMedia('(display-mode: standalone)')` to report
 * `matches: true` before any page script runs, so `isStandalonePwa()`
 * resolves truthy on first client render. Re-applied on every navigation
 * because `addInitScript` re-runs per Playwright's contract.
 */
export async function emulateStandalonePwa(page: Page): Promise<void> {
  await page.addInitScript(() => {
    const STANDALONE_QUERY = '(display-mode: standalone)';
    const nativeMatchMedia = window.matchMedia?.bind(window);
    window.matchMedia = (query: string): MediaQueryList => {
      if (query === STANDALONE_QUERY) {
        return {
          media: query,
          matches: true,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false
        } as unknown as MediaQueryList;
      }
      return nativeMatchMedia
        ? nativeMatchMedia(query)
        : ({ matches: false, media: query } as MediaQueryList);
    };
  });
}

export class AppShellPage {
  constructor(private readonly page: Page) {}

  /** Emulate standalone PWA mode, then navigate. Use for every app-mode test. */
  async gotoAsPwa(path: string): Promise<void> {
    await emulateStandalonePwa(this.page);
    await this.page.goto(path);
  }

  /** The single `<nav>` landmark expected in app mode — see assumption #1. */
  get bottomNav(): Locator {
    return this.page
      .locator('nav')
      .filter({ has: this.page.getByRole('link', { name: /home/i }) });
  }

  /** The `role="group"` pill containing Home / Add / Reports. */
  get navPill(): Locator {
    return this.bottomNav.getByRole('group');
  }

  get homeItem(): Locator {
    return this.bottomNav.getByRole('link', { name: /home/i });
  }

  get addItem(): Locator {
    return this.bottomNav.getByRole('button', { name: /^add$/i });
  }

  get reportsItem(): Locator {
    return this.bottomNav.getByRole('link', { name: /report/i });
  }

  /**
   * Settings bubble — expected outside the pill (assumption #4) and,
   * per D-180, intentionally icon-only: it carries an accessible name via
   * `aria-label`/`title` rather than a visible text node. Resolved by
   * accessible role/name (not `hasText`, which would never match an
   * icon-only element) so this locator stays valid whether Settings is
   * rendered as a link or a button.
   */
  get settingsItem(): Locator {
    return this.bottomNav
      .getByRole('link', { name: /settings/i })
      .or(this.bottomNav.getByRole('button', { name: /settings/i }));
  }

  /** Existing hamburger trigger (`header.menu` / `header.closeMenu`, unchanged by F045). */
  get hamburgerTrigger(): Locator {
    return this.page.getByRole('button', { name: /^(menu|close menu)$/i });
  }

  /** The compact popover — see assumption #5. */
  get menuPopover(): Locator {
    return this.page.getByRole('menu');
  }

  get menuPopoverItems(): Locator {
    return this.menuPopover.getByRole('menuitem');
  }

  /** Desktop-only "Add Device" CTA (`hidden md:inline-flex`). */
  get desktopAddDeviceButton(): Locator {
    return this.page.getByRole('button', { name: /^add device$/i });
  }

  /** Mobile-web-only FAB (`md:hidden`), removed entirely in app mode. */
  get mobileAddFab(): Locator {
    return this.page.getByRole('button', { name: /^add device$/i });
  }

  get filterButton(): Locator {
    return this.page.getByRole('button', { name: /^filter$/i });
  }

  get searchInput(): Locator {
    return this.page.getByRole('searchbox');
  }

  /** F023 group section marker, reused verbatim per F045 §5.5. */
  get groupSections(): Locator {
    return this.page.getByTestId('device-group-section-mobile');
  }

  /**
   * A PWA device row, located structurally rather than by testid: the
   * immediate parent `<div>` of the row-open button (named after the
   * device). `DevicePwaRow.svelte`'s root does NOT currently carry
   * `data-testid="device-pwa-row"` (verified against the component as
   * implemented) — this locator works today without it, but a dedicated
   * testid is still the more stable long-term anchor and is requested of
   * Vasquez (see assumption #6 in the class doc and
   * `.squad/decisions/inbox/apone-pwa-shell-tests.md`). Swap this to
   * `getByTestId('device-pwa-row')` once that lands.
   */
  deviceRow(name: string | RegExp): Locator {
    return this.page.getByRole('button', { name }).locator('xpath=ancestor::div[1]');
  }

  actionsMenuTriggerFor(deviceName: string | RegExp): Locator {
    return this.deviceRow(deviceName).getByRole('button', { name: /more actions/i });
  }
}

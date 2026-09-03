# Manual PWA Validation Checklist

> **Authority:** produced under `specs/004-agentic-development-foundation/`
> (T102). This is the manual checklist named by `plan.md` §2.6/§2.10 and
> `coverage-migration.md` §7–§8 as the honest home for behaviour that a real
> installed PWA/browser environment must prove — Playwright is retired
> (`brief.md` §2.1) and there is no automated browser layer to prove it
> instead.

**Owner:** `briandenicola`.

**Class:** `REVIEWED` — a manual gap, never reported as automated coverage,
and **not merge-blocking** unless `briandenicola` later decides otherwise and
records that decision explicitly (`plan.md` §2.10).

**Cadence:** run before every release tag, and after any change to the PWA
shell (`src/app.html`, `vite.config.ts` PWA/Workbox block), the auth redirect
(MSAL config, `auth/login/**`, the `(authenticated)` guard), the service
worker/cache strategy, the install/update prompt (`PwaUpdatePrompt.svelte`),
or mobile safe-area/fixed chrome (`AppBottomNav.svelte`, `AppMenuPopover.svelte`).

**Why this can't be a unit/component/HTTP/contract test:** every row below
needs a real installed app, a real service worker, a real first paint, a real
IdP redirect, or a second rendering engine — none of which jsdom or an
in-process HTTP client can produce. Everything that *can* be proved lower has
already been moved there; see `coverage-migration.md` §6–§8 for the mapping.

## How to record a run

For each run, capture the fields below once per environment tested, then tick
each check `Pass` / `Fail` / `Blocked` with one line of evidence (what you saw,
or a screenshot/issue link if it failed). A missed or skipped run is recorded
as an explicit exception under `plan.md` §2.10, not silently dropped.

| Field | Value |
| --- | --- |
| Date | |
| Release tag / commit | |
| Environment (device) | e.g. iPhone 14 / Pixel 7 / desktop |
| Browser + version | e.g. iOS Safari 18 / Chromium 128 / Firefox 130 |
| Install state | installed (standalone) / browser tab |
| Tester | |

## Checklist

| # | Check | Pass/Fail | Evidence |
| --- | --- | --- | --- |
| M-01 | Install the app on iOS Safari (Add to Home Screen); it launches standalone with the correct name/icon | | |
| M-02 | Install on desktop Chromium; it launches standalone | | |
| M-03 | In the **installed** app on a phone: the opaque full-width bottom bar (Home / Add / Reports / Settings) renders edge-to-edge, every item is comfortably tappable, the bar is fully opaque (no content visible through it), and the last list row scrolls fully above the bar without being hidden | | |
| M-04 | In a **mobile browser tab** (not installed): no bottom bar is present | | |
| M-05 | After deploying a new build, the update prompt appears and reloads to the new version | | |
| M-06 | Go offline, navigate to an uncached route: the `/offline` shell is served, not a browser error page | | |
| M-07 | Offline: a previously viewed device list still renders from cache | | |
| M-08 | Offline: attempt a create/edit/delete — it fails **visibly** with an error, and no change appears to have been saved | | |
| M-09 | Restore connectivity: data refreshes without a manual reload | | |
| M-10 | With dark theme stored, hard-reload: no light flash before first paint | | |
| M-11 | Real Entra ID sign-in **and** sign-out in a browser; DevTools → Application shows **no** token-like key in `localStorage` | | |
| M-12 | Create a device from the modal, then import a small CSV via the real file picker; both appear in the list | | |
| M-13 | Export CSV: the download completes and opens cleanly in a spreadsheet | | |
| M-14 | WebKit/Safari spot check: `/devices`, a device detail page, and the filter sheet render correctly (safe-area insets, sticky headers) | | |
| M-15 | Firefox spot check: `/devices` + filter sheet; keyboard-only pass reaches the nav, opens the menu, and Escape restores focus | | |
| M-16 | Admin reference pages (`/admin/brands`, `/admin/categories`, `/admin/owners`, `/admin/locations`, `/admin/networks`, `/admin/tags`): keyboard navigation, labels, focus visibility, contrast, and responsive layout spot check | | |
| M-17 | On a standard phone viewport (≤ 390 px wide, e.g. iPhone 14 / Pixel 7): open the mobile nav popover (`AppMenuPopover`) — every nav item row is at least 44 px tall and the full label is readable without truncation in both light and dark themes | | |
| M-18 | On a desktop browser (≥ 768 px): open the header user-menu dropdown and the Configuration dropdown (`AppDesktopConfigMenu`) — each row is at least 44 px tall and renders correctly in both light and dark themes | | |
| M-19 | On an installed PWA at 375 px wide (e.g. iPhone SE): navigate to the Admin → Audit Log page, perform a horizontal touch-swipe on the audit table — all columns scroll into view while the page header, Filters toggle, and pagination controls remain stationary (not part of the scroll region) | | |
| M-20 | On iOS Safari (real device or Simulator) at 320–375 px: open the Add Device form and tap the Purchase Date field — the native date picker appears and the input does not overflow the form column at any picker stage; confirm the same on the Edit Device form | | |
| M-21 | On an installed PWA at 375 px wide: open the hamburger menu and observe the Appearance row — the Light/Dark/System selector shows **icon-only** buttons (sun, moon, grid) with no visible text labels and no truncation; each icon's selected state is visually clear; confirm the desktop Settings page still shows full "Light / Dark / System" text labels | | |
| M-22 | **#145 — Action column pinning (installed PWA, portrait, 320/375/390/430 CSS px):** On each viewport, open `/devices` in the installed PWA, switch to Table view, and scroll the table horizontally — the Actions column header and each row's action cell remain pinned to the right edge of the scroll container and never drift off-screen; device name column remains pinned to the left; all cell text is truncated (ellipsis) rather than pushing the column wider. `REVIEWED/manual — not jsdom-proven.` | | |
| M-23 | **#164 — Settings Table Columns Reset button (installed PWA, portrait, 320/375/390/430 CSS px):** Navigate to Settings → Table Columns on each viewport. The Reset button label reads exactly **"Reset"** (single word, the localized value from `settings.tableColumns.resetToDefault`) — no longer copy such as "Reset to Default". The label fits on a single line at all four widths with no wrapping. Tap the button and confirm the column list reverts to defaults and a success toast appears. `REVIEWED/manual — not jsdom-proven.` | | |
| M-24 | **#165 — Per-device action menu layering (installed PWA, portrait, 320/375/390/430 CSS px):** On the device list in PWA Cards view, tap the ⋮ kebab/ellipsis button on the **first** row, a **middle** row, and the **last** row in a group. Each time the action menu (Edit / Clone / Claim / Release / Change Status / View change history / Delete) must appear **fully visible above** all other content — it must not be clipped by the row container, the group boundary, or any sibling element; the full menu height is visible without scrolling; tapping outside dismisses it. `REVIEWED/manual — not jsdom-proven.` | | |
| M-25 | **#148 — Add/Edit Device form containment at narrow viewports (installed PWA, portrait, 320/375/390/430 CSS px):** On each viewport, open the Add Device form (and separately the Edit Device form from a device detail row). Verify: (a) the Purchase Date field label and native date input stay within the form column width — no horizontal overflow; (b) tapping the date input opens the native iOS date picker and the form does not scroll wider than the viewport at any picker stage; (c) the Purchase Price number input stays within its grid column (no overflow); (d) the Notes textarea fills its column without overflow; (e) the form as a whole does not cause the modal to scroll horizontally. `REVIEWED/manual — not jsdom-proven.` | | |
| M-26 | **#170 — Ellipsis right-alignment (installed PWA, portrait, iPhone width ≈ 375 CSS px):** Open `/devices` in PWA Cards view. Verify five specific rows across groups: "Mohu Leaf Stitch 60m Range / Mohu · Leaf Stitch", "4-Door French Door Fridge / Samsung · RF28JBEDBSG/AA", "Air Fryer / Ninja · DZ400 A", "Aqua Flosser / AquaSonic · Aqua Flosser PRO", "Coffee Maker / Keurig · K-Classic". All five ⋮ ellipsis buttons must form a **visibly straight vertical column** at the right inset — none displaced inward by short or long text. Text that is too long must truncate (ellipsis) rather than pushing the action button left or right. Each ⋮ button must be comfortably tappable (≥44 px touch target). Compact ghost style (no large border circle). Verify in both light and dark themes. `REVIEWED/manual — not jsdom-proven.` | | |
| M-27 | **Overflow menu density (installed PWA, portrait, 320/375/390/430 CSS px):** Tap the header hamburger to open the app menu. Every row (Devices, Reports, Import, Export, Audit Log, the Configuration group, Settings, Sign Out) must render as a **38.25 px block sitting flush against its neighbours** (`h-9` = 2.25rem at this app's 17px root, not the 36px the Tailwind name implies) — no blank band above or below the label, no visible whitespace gutter between rows. The active row's tint must be exactly the same height as an inactive row. Verify the whole menu fits without scrolling on a 390 px-tall-viewport phone where it previously scrolled, and that each row is still comfortably tappable with a thumb. Check both light and dark themes. `REVIEWED/manual — not jsdom-proven.` | | |
| M-28 | **Device details compact layout (installed PWA, portrait, 320/375/390/430 CSS px):** Open a device from the list (both the detail modal and the `/devices/{id}` page). Fields must render as a **flush inset list — label left, value right on one line, hairline divider between rows** — not as stacked label-over-value cards. Long values (Product URL, MAC address) must wrap or truncate inside their row without pushing the label off-screen; Purpose and Notes stay stacked. Confirm the same screen opened in **mobile Safari/Chrome (not installed)** still shows the roomy stacked layout, since the compact variant is gated on `display-mode: standalone`. Check both light and dark themes. `REVIEWED/manual — not jsdom-proven.` | | |

**28 checks, all owner `briandenicola`.** M-16 compensates for the route-level
composition gap identified by the post-major-work QC audit; M-17 and M-18 cover
the mobile-popover and desktop-nav tap-target density introduced by issues #134
and #144 (branch `squad/134-144-desktop-nav-density`); M-19 covers the real
touch-scroll gesture on the audit table (#146) — the component test proves the
DOM structure (scroll region, class, table containment) but cannot exercise
actual touch scrolling in jsdom; M-20 covers the iOS WebKit native date-picker
rendering at narrow viewports (#148) — the component test proves the
containment classes (`date-input-contain`, `min-w-0`, `w-full`) but cannot
exercise WebKit's intrinsic sizing or the native picker UI; M-21 covers the
icon-only theme toggle in the compact PWA menu (#147) — the component test
proves the iconOnly prop suppresses the span labels and retains aria-label, but
cannot observe real-device icon rendering or confirm no visual truncation;
M-22 covers the desktop-table action-column sticky-pin at real scroll width
(#145) — component tests prove the `sticky right-0` and `truncate` classes but
cannot exercise browser sticky positioning or text-overflow clipping in jsdom;
M-23 covers the Settings Reset button single-line label at narrow viewports
(#164) — component tests prove the `whitespace-nowrap` class and i18n routing
but cannot observe actual line-wrapping in a real browser;
M-24 covers the per-device action menu layering above group stacking contexts
(#165) — component tests prove `z-index: var(--z-dropdown)` and the absence of
`overflow-hidden` on the row container but cannot verify real overflow clipping
or CSS stacking resolution in jsdom;
M-25 covers the full Add/Edit Device form containment at narrow viewports
(#148) — component tests prove `date-input-contain`, `min-w-0`, `w-full`, and
modal `overflow-hidden` but cannot exercise iOS WebKit's intrinsic sizing,
native picker layout, or real overflow clipping;
M-26 covers the ellipsis right-alignment in the PWA device list (#170) —
component tests prove `.pwa-row` uses the two-column grid class contract
(`minmax(0,1fr) auto`) ensuring the action column is always last and carries
`h-11 w-11`, but cannot measure rendered pixel positions or verify the
visually-straight column in a real browser;
M-27 covers the R3 overflow-menu density — component tests prove the shared
`h-9` row block, the `gap-0` container, `p-1.5` panel padding and `my-1`
dividers, but cannot measure rendered row pitch (48.88 px → 38.25 px by
calculation), confirm the menu now fits without scrolling on a real phone, or
judge whether a 38.25 px row is still comfortable under a thumb;
M-28 covers the compact PWA device-detail layout — component tests prove the
`compact` variant drops the grid for a `divide-y` single-line row list with
right-aligned values and identical `dt` ordering, but cannot verify real
wrapping/truncation of long values, and cannot exercise the
`display-mode: standalone` gate that decides which variant a real device gets
(jsdom has no `matchMedia`, so tests only ever see the roomy default unless
`compact` is passed explicitly). The remaining
checks exist only because their risks require a real browser environment. None
duplicates an existing Vitest or HTTP
integration/contract assertion — each exists only because the risk is
`display-mode`, service-worker, first-paint, a real download, or engine-specific
rendering (`coverage-migration.md` §7). No item here claims an offline mutation
queue: the product has no queued-write feature — mutations are `NetworkOnly`
and simply fail visibly offline (M-08).

## Performance review

| # | Check | Pass/Fail | Evidence |
| --- | --- | --- | --- |
| P-01 | On the release candidate, profile `/devices`, a device detail route, and one admin route under a simulated 4G profile. Record FCP, LCP, TTI, TBT, CLS, initial and per-route gzipped JS, and the largest image against constitution §6.5.9. File an issue for every exceeded budget. | | |

P-01 is `REVIEWED` per release under ADR 0002 and `plan.md` §6.4. It is not a
CI result and must never be inferred from a green `task verify` run.

## Full disposition reference

The complete coverage matrix, per-spec rationale, and the accepted-gaps
register (`G-01`–`G-10`) that this checklist compensates for live in
[`../../specs/004-agentic-development-foundation/coverage-migration.md`](../../specs/004-agentic-development-foundation/coverage-migration.md)
§6–§8. This file is intentionally the short, durable operational checklist —
it does not repeat that matrix.

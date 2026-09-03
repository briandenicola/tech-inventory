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

**20 checks, all owner `briandenicola`.** M-16 compensates for the route-level
composition gap identified by the post-major-work QC audit; M-17 and M-18 cover
the mobile-popover and desktop-nav tap-target density introduced by issues #134
and #144 (branch `squad/134-144-desktop-nav-density`); M-19 covers the real
touch-scroll gesture on the audit table (#146) — the component test proves the
DOM structure (scroll region, class, table containment) but cannot exercise
actual touch scrolling in jsdom; M-20 covers the iOS WebKit native date-picker
rendering at narrow viewports (#148) — the component test proves the
containment classes (`date-input-contain`, `min-w-0`, `w-full`) but cannot
exercise WebKit's intrinsic sizing or the native picker UI. The remaining
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

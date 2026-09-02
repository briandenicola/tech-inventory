# F045: Installed-PWA App Shell & Device List Reshape

**Status**: ready (design reviewed 2026-09-02)
**Priority**: P1
**Effort**: M
**Value**: high
**Risk**: medium
**Target release**: v1.3
**Created**: 2026-09-02
**Owner**: vasquez (implementation) / apone (tests) / drake (glyphs)
**Design authority**: Ripley — see `.squad/decisions/inbox/ripley-pwa-device-navigation.md`
**Supersedes**: the *mobile nav chrome* portion of F027 (F027 keeps responsive
admin tables only — already delivered via `ResponsiveAdminList`)
**Extends**: F023 (adds a PWA-mode default grouping; does not change desktop)

---

## 1. Problem

The installed PWA re-uses the desktop app shell. Concretely, on an installed
iPhone/Android home-screen instance:

- Navigation is a **full-screen drawer** hung off the header hamburger. It
  covers the content the user was reading and costs two taps to reach Reports
  or Settings.
- The device list header packs H1 + view-mode toggle + Filter into one row and
  then a **half-width** search box (`md:max-w-lg`), which reads as desktop
  chrome squeezed onto a phone.
- Device cards are a **2-up grid** of truncated `name / model / status / brand /
  category` tiles with **no `...` actions menu** — actions are only reachable
  after opening the detail modal.
- Grouping defaults to **None** everywhere, so a 500–1000 device household
  scrolls a flat list on the smallest screen.
- `Settings → Table Columns` is presented as a global preference but only ever
  affects the desktop table, which is confusing when set from a phone.

## 2. Goal

Make the installed PWA feel like a native app: a persistent bottom navigation,
a search-first device list grouped by category, two-line rows with inline
actions, and a compact anchored menu instead of a drawer — **without changing
desktop browser behavior at all.**

## 3. Non-Goals (hard scope fence)

- No backend/API change. Zero controller, handler, DTO, or migration work.
- No change to desktop (`md+` browser) layout, table, or Table Columns feature.
- No change to auth, roles, or route guards. Nav items keep their existing
  `roles` gates from `appNav.ts`.
- No new grouping dimensions beyond the F023 set (`category | owner | year`).
- No gesture navigation, swipe-to-delete, or tab-bar reordering.
- No new icon library — Drake supplies inline SVG paths, consistent with the
  existing "inline SVG in layout components" decision (docs/backlog.md).
- No visual redesign of desktop tokens/colors.

---

## 4. Mode Definition (the single most important rule)

Three presentation modes exist. Every requirement below is scoped to one.

| Mode | Predicate | Chrome |
|---|---|---|
| **App mode** (`pwa`) | `isStandalonePwa() === true` | Bottom nav pill, anchored menu, grouped 2-line rows, full-width search |
| **Mobile web** (`mobile`) | `!standalone && viewport < md (768px)` | Existing mobile layout, **unchanged** |
| **Desktop** (`desktop`) | `viewport ≥ md` | Existing desktop layout, **unchanged** |

**Rules:**

1. App-mode chrome is gated on `isStandalonePwa()` **only** — never on viewport
   alone. A narrow desktop browser window must not sprout a bottom nav bar.
2. If an installed PWA is running on a wide screen (installed desktop PWA,
   iPad landscape), app-mode chrome still applies but the pill is
   `max-w-md mx-auto`. Do not add a fourth mode.
3. `isStandalonePwa()` already exists in `src/lib/auth/index.ts` (lines ~62–68:
   `display-mode: standalone` media query OR `navigator.standalone`). **Do not
   duplicate it.** Wrap it in a reactive store (§5.1) and re-export; the auth
   module keeps ownership of the primitive.
4. SSR: the predicate must resolve to `false` on the server. Render desktop/web
   chrome server-side and let the client upgrade. Do **not** produce a
   hydration mismatch — gate the bottom nav behind a `mounted` flag.

---

## 5. Implementation Boundary — Vasquez

### 5.1 New: `src/lib/stores/displayMode.svelte.ts`

Runes-based store. Exports:

```ts
export type DisplayMode = 'pwa' | 'mobile' | 'desktop';
export function createDisplayMode(): { readonly mode: DisplayMode; readonly isPwa: boolean; readonly isMounted: boolean }
export const displayMode: /* singleton */
```

- Reads `isStandalonePwa()` from `$lib/auth` on mount.
- Subscribes to `matchMedia('(display-mode: standalone)')` **change** events so
  the mode is live (a user can install mid-session on Android).
- Subscribes to `matchMedia('(min-width: 768px)')` for the mobile/desktop split.
- Both listeners must be torn down in the effect cleanup.
- `isMounted` starts `false`; SSR and the first client frame report `desktop`
  + `isMounted: false`.
- Must be injectable for tests — accept an optional options bag
  (`{ standalone?: boolean; wide?: boolean }`) exactly like
  `isStandalonePwa` consumers already do in `auth/index.ts`.

### 5.2 New: `src/lib/components/AppBottomNav.svelte`

Reference: the supplied podcast-app screenshot — a rounded pill containing
three items, plus a **separate** circular bubble to its right.

- Renders **only** when `displayMode.isPwa && displayMode.isMounted`.
- Structure:
  - `<nav aria-label={t('navigation.primary')}>` containing
  - a `role="group"` rounded pill (`rounded-full`, blurred translucent
    background, shadow) with **Home**, **Add**, **Reports**
  - a sibling circular button/link for **Settings** (`h-14 w-14 rounded-full`)
- Each item: icon on top (`h-6 w-6`), label underneath at `text-[10px]`/
  `text-xs` with `font-medium`. Label is **visible text**, not `sr-only` —
  the requirement is "tiny label underneath".
- Targets: minimum `h-11 w-11` hit area per constitution §6.5.6 (44×44
  preferred); the visual pill may be tighter but the tappable area may not.
- Positioning:
  `position: fixed; left/right: var(--space-4); bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-4)); z-index: var(--z-fixed);`
  Mirror the token usage already in `AddDeviceFab.svelte` — do not hardcode px.
- Active state: derived from `isNavItemActive($page.url.pathname, item)` in
  `appNav.ts`. Active item gets filled pill background + `aria-current="page"`.
- **Add** is an action, not a route. It dispatches an `onAdd` callback that the
  authenticated layout forwards to the devices page's existing
  `createModalOpen` flow. When the user is on a non-`/devices` route, Add
  navigates to `/devices?add=1`; the devices page opens the create modal for
  that param and strips it. Members/Admins only — hide for `Viewer` using the
  same `roles` predicate already in `appNav.ts` (`memberRoles`). When hidden,
  the pill renders two items and stays centered; do not leave a gap.
- Reduced motion: no slide-in animation when `prefers-reduced-motion` — reuse
  `$lib/utils/motion.ts`.

### 5.3 New: `src/lib/components/AppMenuPopover.svelte`

Reference: the supplied iOS context-menu screenshot — a compact rounded card
floating over the content, anchored to its trigger.

- Replaces the full-width `{#if mobileMenuOpen}<nav>` block in
  `routes/(authenticated)/+layout.svelte`.
- **Same item set, same menuitem order, same role gates.** It renders exactly
  what the drawer renders today: visible primary nav items → Admin section
  (heading + items) → Settings → identity chip + Sign out. No item may be
  dropped or added in this feature. The theme block sits **after** this menu
  boundary, not interleaved within it (D-181, amended 2026-09-02: `ThemeToggle`
  is a composite widget, not a menuitem-family element, and axe's
  `aria-required-children` flags it inside `role="menu"` even when wrapped in
  `role="group"` — moving it outside the menu boundary is the fix, and a
  single contiguous menu cannot interleave a non-menu sibling partway through
  it). General constraint going forward: ARIA `menu` composites in this
  codebase host command items only — no embedded widgets.
- Anchoring: absolutely positioned under the hamburger trigger,
  `right-0 mt-2 w-64 max-h-[70vh] overflow-y-auto rounded-2xl border shadow-xl backdrop-blur-md`.
  This is the *same* visual recipe as the existing desktop user-menu dropdown
  in `+layout.svelte` — extract it rather than inventing a second style.
- `z-index: var(--z-popover)` (60) so it clears the sticky page header (`--z-fixed`,
  30 when closed — corrected 2026-09-02, see
  `.squad/decisions/inbox/hicks-f045-revision.md` B1), the filters drawer
  (`--z-modal`, 50), and the bottom nav (30).
- Must not scroll-lock the body and must not render a full-screen backdrop.
  Dismiss on: outside click, `Escape`, and route change (the layout already has
  a `$page.url.pathname` effect that closes menus — keep it).
- Focus: `role="menu"`, items `role="menuitem"`, focus moves to the first item
  on open, focus returns to the trigger on close, `Tab` cycles within the
  popover while open. Arrow-key roving is preferred but not blocking.
- Because it now overlays content instead of pushing it, verify it does not
  clip inside the `sticky` header stacking context. If it does, portal it to
  `document.body` — this is the exact failure mode logged in D-167
  (z-index hierarchy) and the `PullToRefresh.containing-block.test.ts`
  regression; add an equivalent containing-block test.

### 5.4 New: `src/lib/components/DevicePwaRow.svelte`

- One device per full-width row (**not** the 2-up grid).
- Line 1: `device.name` — `text-base font-semibold`, single-line truncate.
- Line 2: `${brandName} · ${device.model}` — `text-sm text-neutral-500`,
  truncate. Brand resolves through the existing `lookupName(refData.brands, …)`
  helper in `DeviceTable.svelte`; hoist it to `$lib/utils` rather than copying.
  Omit the separator when either half is missing; render `—` only when both are.
- Trailing: the **existing** `DeviceActionsMenu.svelte` (currently used only in
  `DeviceDetailModal.svelte`). Reuse it as-is; pass the same props the modal
  passes. Do not fork it. Its own action-sheet behavior on mobile is already
  shipped (F042).
- Status pill stays (reuse `statusBadgeClass`), positioned inline on line 1's
  right or under line 2 — Drake's call, but it must not push the actions menu
  off-row at 360px.
- Selection checkbox: keep the existing `selectable` contract so bulk actions
  (F024) still work in app mode.
- Row body opens the device detail modal via the existing `onOpenDevice`.
- The row button and the actions menu must be **separate** interactive
  elements — no nested buttons.

### 5.5 Modified: `src/lib/components/DeviceTable.svelte`

- Add prop `presentation?: 'auto' | 'pwa'` (default `'auto'`).
- When app mode is active, render the `DevicePwaRow` list instead of the
  `md:hidden grid grid-cols-2` card grid. Group headers (the existing
  `device-group-section-mobile` section + chevron + count badge) are retained
  verbatim; only the row renderer changes.
- **`visibleColumns` must not reach the PWA row renderer.** Today
  `visibleColumns` is only consumed by the desktop `<table>` (lines ~200, ~267)
  and the mobile card already ignores it — preserve that and make it explicit
  with a comment, because the requirement is a stated invariant, not an
  accident.
- This component is already 596 lines (constitution §6.5.4 flags >200). Adding
  a third renderer inline is not acceptable. Extract the three renderers into
  `DeviceTableDesktop.svelte`, `DeviceTableCards.svelte`, `DevicePwaList.svelte`
  and keep `DeviceTable.svelte` as a thin selector. **This extraction is in
  scope and is the price of admission for the new renderer.**

### 5.6 Modified: `routes/(authenticated)/devices/+page.svelte`

- **Header row (app mode):** `H1 "Devices"` + view-mode control + Filter button.
  The desktop "Add Device" CTA stays hidden (`md:` only, unchanged). The
  existing `AddDeviceFab` is **removed in app mode only** — Add now lives in the
  bottom nav; two simultaneous add affordances is the exact duplication the
  code comments already call out for `EmptyState`. Keep the FAB for mobile-web
  mode.
- **Search (app mode):** promote out of the header actions row to its own
  full-width row directly under the title bar. Drop `md:max-w-lg` → `w-full`
  in app mode. Debounce stays 300ms; behavior otherwise unchanged.
- **Default grouping (app mode):** when the URL has **no** `groupBy` param and
  app mode is active, apply `groupBy=category` as an *implicit* default.
  - Model this exactly like the existing F026 implicit-Active-status default
    (`statusIsImplicitActive` at line ~142): the implicit value must **not** be
    written into the URL on load, must **not** count toward `activeFilterCount`,
    and an explicit `?groupBy=none` sentinel must be able to turn it off.
  - Rationale: URL-writing on mount fights the `setDevicesViewState` session
    restore and would make every shared link category-grouped.
  - Caution: grouping switches the page to a full-result-set fetch and
    disables infinite scroll. F023's `fetchAllDevicesForGrouping` was
    **unbounded** on `main` (it fetched every matching page, no row cap) —
    the earlier text here calling a 500-row ceiling "existing F023 behavior"
    was a factual error, corrected 2026-09-02 (D-182, see
    `.squad/decisions/inbox/ripley-f045-qc-audit-amendments.md` and
    `.squad/decisions/inbox/hicks-f045-revision.md`). At the stated 500–1000
    device household scale, an *unbounded* fetch in app mode would be the
    heaviest path in the product, so F045 introduces a **new, app-mode-only**
    cap: **`fetchAllDevicesForGrouping` takes an optional `maxRows` parameter,
    left unset for desktop/mobile-web callers (unbounded, unchanged from
    `main`) and passed as `MAX_GROUPED_DEVICES` (500) only by the
    standalone-PWA call site.** Render a "showing first 500" note only when
    `displayMode.isPwa` and the real total exceeds 500 — desktop must never
    show a truncation note it doesn't enforce. Do not raise the cap, and do
    not apply it to non-PWA callers, without a matching decision record.
- **Bottom padding:** the list container needs
  `padding-bottom: calc(env(safe-area-inset-bottom,0px) + 5.5rem)` in app mode
  so the last row clears the nav pill.

### 5.7 Modified: `routes/(authenticated)/+layout.svelte`

- Extract the drawer into `AppMenuPopover`; render `AppBottomNav` after
  `<main>`; keep header/footer for non-app modes.
- Hide the `<footer>` in app mode (version/GitHub links belong in Settings on a
  phone) — it otherwise sits behind the nav pill.
- **Collision audit — all of these are `fixed` and must be lifted above the nav
  pill in app mode:**
  - `AddDeviceFab.svelte` (bottom-left, `--z-fixed`) — removed in app mode.
  - `BackToTopFab.svelte` (fixed bottom) — offset by nav height.
  - `BulkActionBar.svelte` (`fixed inset-x-0 bottom-0 z-30`) — must sit
    **above** the nav pill or temporarily hide it while a selection is active.
    Decide once and comment it.
  - `ToastContainer` (top-right, z-50) — unaffected, verify only.
  - `PullToRefresh` — verify the nav pill is outside its transform containing
    block; there is a dedicated regression test file for this class of bug.
- Do **not** restore desktop primary nav links. The `regression-watch` comment
  in `+layout.svelte` (dd52e98 removed / 1de8da8 re-introduced) is binding.

### 5.8 Modified: `routes/(authenticated)/settings/+page.svelte`

`TableColumnSettings` gets an explicit scope note (new i18n key, e.g.
`settings.tableColumns.desktopOnly`: "Applies to the desktop table view only").
No behavior change — the setting already only feeds the desktop `<table>`.
This is a labeling fix so the invariant is visible to the user.

### 5.9 i18n

Every new string lands in `src/lib/i18n/en.json`. Actual new keys added:
`navigation.primary`, `settings.tableColumns.desktopOnly`,
`devices.groups.pwaDefaultNote` (landed under the existing `devices.groups.*`
namespace, not the `devices.grouping.*` namespace first drafted here), and
`devices.list.showingFirst`. The bottom nav's Home/Reports/Settings labels
and its Add action reuse existing keys (`navigation.home`,
`navigation.reports`, `navigation.settings`, `common.actions.add`) rather
than the `navigation.add` / `navigation.reportsShort` /
`navigation.settingsShort` keys originally anticipated here — no new key was
needed since those labels are identical to their desktop-nav counterparts.
Zero hard-coded strings (constitution §6.5.12).

---

## 6. Test Boundary — Apone

### 6.1 Vitest (unit / component)

| Target | Cases |
|---|---|
| `displayMode.svelte.ts` | standalone true → `pwa`; standalone false + wide → `desktop`; standalone false + narrow → `mobile`; SSR/pre-mount → `desktop` + `isMounted:false`; `matchMedia` change event flips mode; listeners removed on teardown |
| `AppBottomNav.svelte` | renders only when `isPwa`; pill contains exactly Home/Add/Reports; Settings is **outside** the pill; each item has a visible label; `aria-current="page"` on the active item; Add hidden for `Viewer`; hit areas ≥44px; axe clean |
| `AppMenuPopover.svelte` | item set + order **identical** to today's drawer for Admin, Member, and Viewer (three snapshots — this is the regression that matters); Escape closes and returns focus; outside click closes; no body scroll-lock; no full-screen backdrop element; axe clean |
| `AppMenuPopover` containing-block | mirrors `PullToRefresh.containing-block.test.ts` — asserts the popover is not clipped by an ancestor with `transform`/`filter`/`backdrop-filter` |
| `DevicePwaRow.svelte` | line 1 = name; line 2 = `brand · model`; missing brand or model degrades cleanly; `—` only when both absent; `DeviceActionsMenu` present and reachable by keyboard; row button and actions menu are not nested; axe clean |
| `DeviceTable.svelte` | `presentation='pwa'` renders rows not the 2-up grid; group headers + count badges survive in PWA mode; **`visibleColumns` has zero effect on PWA output** (pass a 1-column list, assert brand/model still render) — this is the explicit invariant |
| devices page | app mode with no `groupBy` → grouped by category; `?groupBy=none` → flat; implicit grouping does not appear in the URL; implicit grouping does not increment `activeFilterCount`; search input is full-width/no `max-w-lg` in app mode; `AddDeviceFab` absent in app mode, present in mobile-web mode |
| `tableColumns` prefs | unchanged behavior + the new desktop-only label renders |

Existing suites that **must be updated, not deleted**: `DeviceTable.test.ts`,
`AddDeviceFab.test.ts`, `appNav.render.test.ts`, `AppNavMenuHarness.svelte`,
`admin-page.test.ts`. Add a `DisplayModeHarness.svelte` alongside the existing
`AppNavMenuHarness`/`ResponsiveAdminListHarness` pattern rather than mocking
`matchMedia` ad hoc in every file.

### 6.2 Playwright (E2E, `tests/e2e/`)

- New `tests/e2e/journeys/15-pwa-shell.spec.ts` using a context with
  `display-mode: standalone` emulated (`page.emulateMedia({ media: 'screen' })`
  plus a `matchMedia` init-script override — Playwright cannot natively emulate
  `display-mode`, so inject the override in `addInitScript`; document that
  clearly in the page object).
- Journeys: bottom nav visible and tappable → Reports; Settings bubble
  navigates; Add opens the create modal from a non-devices route; hamburger
  opens a popover that does **not** cover the whole viewport (assert the
  underlying list is still hit-testable); device row shows two lines and its
  `...` menu opens.
- App-mode axe coverage landed in the new `15-pwa-shell.spec.ts` itself
  (bottom nav and devices-list passes, each asserting **zero axe
  violations** — constitution §6.5.6 merge gate) rather than as an extension
  to `13-a11y-smoke.spec.ts` as originally anticipated here; `13-a11y-smoke`
  remains desktop/mobile-web route coverage only. Accepted as equivalent
  coverage — the assertion still exists and still gates the merge, just in
  the journey file that owns app-mode setup instead of the general smoke
  file.
- Update `06-browse-filter.spec.ts` and `03-create-device.spec.ts` page objects
  for the moved Add affordance **in the same PR** — the F027 note about
  two-step breakage applies here verbatim.
- Regression guard: a desktop-viewport run of the existing suite must pass
  **unchanged**. If any desktop test needs editing, that is a design violation,
  not a test fix — escalate to Ripley.

---

## 7. Accessibility Requirements (merge gates)

1. Zero axe-core violations in unit and E2E for every touched view.
2. Bottom nav is a `<nav>` with an accessible name; items are links (or a
   button for Add); active item carries `aria-current="page"`.
3. Every nav item **inside the bottom-nav pill** (Home, Add, Reports) has a
   visible text label — icon-only is not acceptable there. The Settings
   bubble sits outside the pill and is the sole accepted exception (D-180,
   amended 2026-09-02): at the y-offset where a label would sit inside its
   59.5px circle the available chord is only ~44px, "Settings" at 10px/500
   measures ~42px, and most longer localisations would overflow the curve.
   `AppBottomNav.svelte` gives it an accessible name via `aria-label`/`title`
   instead of a visible text node. If this is overruled, fall back to Drake's
   documented `w-16` pill-widened variant rather than silently violating rule 3.
4. Tap targets ≥44×44 CSS px for nav, row actions, and the popover items.
5. Popover: focus trap while open, focus restored to trigger on close, Escape
   dismisses, `role="menu"`/`menuitem`.
6. Safe-area insets via `env(safe-area-inset-*)` on the nav, the list padding,
   and any repositioned FAB.
7. Contrast ≥4.5:1 for the tiny nav labels in **both** themes — 10px text on a
   translucent blurred pill is the highest-risk contrast surface in this
   feature. Verify against the dark palette explicitly.
8. `prefers-reduced-motion` honored for popover and nav transitions.
9. Screen-reader pass (VoiceOver on iOS) for the bottom nav and popover —
   required for net-new views per §6.5.6.

---

## 8. Risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | `display-mode: standalone` is unreliable on iOS Safari home-screen apps; `navigator.standalone` is the real signal there | High | Already handled — `isStandalonePwa()` ORs both. Do not "simplify" it. |
| R2 | SSR/hydration mismatch causes a nav-bar flash or a Svelte hydration error | High | `isMounted` gate; never render app chrome during SSR |
| R3 | Fixed-element collisions (FAB, BackToTop, BulkActionBar, toasts) | High | Explicit audit in §5.7; one z-index decision recorded in code |
| R4 | Popover clipped by the sticky header's stacking/containing block | Medium | `--z-popover`; portal fallback; dedicated containing-block test (prior art: PullToRefresh) |
| R5 | Default category grouping forces an unbounded full-result-set fetch on the weakest device on the network | Medium | App-mode-only 500-row cap (`maxRows` param, PWA caller only — desktop/mobile-web stay unbounded, D-182), show a truncation note only when it applies, do not raise the cap |
| R6 | `DeviceTable.svelte` grows past 700 lines with a third renderer | Medium | Mandatory extraction into three renderer components (§5.5) |
| R7 | Scope creep into "redesign the whole PWA" | Medium | §3 non-goals are binding; any addition needs a new backlog entry |
| R8 | Playwright cannot emulate `display-mode`, tempting a source-level test hook | Medium | `addInitScript` `matchMedia` override in the page object; no production test hooks |
| R9 | Nav item set drifts from the drawer's current options during the popover rewrite | Medium | Three role-based snapshot tests (§6.1) |
| R10 | Bottom nav "Home" ambiguity — the app has no `/` home for authed users | Low | Home = `/devices`. Decided; label it "Home", route it to `/devices`. |

---

## 9. Acceptance Checklist

- [ ] App mode is driven by `isStandalonePwa()` only; no viewport-only path
      produces bottom-nav chrome
- [ ] Desktop browser at every breakpoint is pixel-identical to `main`
- [ ] Mobile-web (non-installed) is unchanged, including the `AddDeviceFab`
- [ ] Title bar in app mode shows Devices + view control + Filter
- [ ] Search is full-width on its own row directly beneath the title bar
- [ ] App mode groups by category with no `groupBy` in the URL; the URL stays
      clean; `?groupBy=none` disables it; grouping does not inflate the filter
      count badge
- [ ] Device rows are full-width, two lines (name / brand · model), with the
      existing `DeviceActionsMenu`
- [ ] Bottom nav: Home + Add + Reports in one rounded pill; Settings in a
      separate circular bubble; icon over tiny label; active state; safe-area
      inset respected; ≥44px targets
- [ ] Add is hidden for `Viewer`; all nav items honor existing role gates
- [ ] Hamburger opens a compact anchored popover over the content with the
      **same options in the same order** as today's drawer for Admin, Member,
      and Viewer
- [ ] Table Columns settings provably do not alter PWA rows (unit-asserted)
      and the settings UI says so
- [ ] No fixed-element collisions: BulkActionBar, BackToTopFab, toasts,
      PullToRefresh all verified in app mode
- [ ] `DeviceTable.svelte` split into selector + three renderers; no file added
      or left over 400 lines
- [ ] All new strings in `en.json`
- [ ] Zero axe violations (unit + E2E, both themes)
- [ ] `pnpm run check`, `pnpm run lint`, `pnpm run test` clean; Playwright
      desktop suite green **without edits**
- [ ] `./scripts/verify.sh` clean

## 10. History

- 2026-09-02: created by Ripley from Brian's PWA field-test request. Design
  review complete; implementation boundary handed to Vasquez, test boundary to
  Apone. Recorded as a new backlog entry rather than an F027 amendment —
  see decision record for rationale.

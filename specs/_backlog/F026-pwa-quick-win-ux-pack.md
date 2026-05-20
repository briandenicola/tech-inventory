# F026: PWA Quick-Win UX Pack

**Status**: backlog
**Priority**: P1
**Effort**: S
**Value**: high
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: vasquez

## Problem
Field-testing the PWA surfaced a tight cluster of small UX defects that, taken
together, make the device list feel broken on mobile. None are big individually;
together they are blocking Brian's day-to-day use.

Items captured from the 2026-05-19 testing session:
1. Devices are hidden behind a transparent element and not selectable (a stray
   overlay is intercepting taps — likely a leftover loading/scrim or a
   z-index regression).
2. The "Add Devices" button takes too much chrome on mobile — should be a
   FAB-style `+` glyph instead of a labeled button.
3. The device list defaults to showing all statuses; in practice the household
   almost never wants Retired/Disposed in the default view — default the
   status filter to `Active` and surface a clear "Showing Active only —
   change" affordance.
4. On phones the card grid is one card per row, which wastes a lot of vertical
   space. Make it 2 cards per row at mobile breakpoint, denser typography.
5. Audit-log "View change history" currently navigates to a separate
   `/admin/audit?entityType=Device&entityId=…` page; on mobile this loses
   context. Render it as a drawer/modal on top of the device detail instead.
6. Pinch-zoom on the PWA causes a "scrolling wobble" — the viewport rubber-
   bands on iOS when the user accidentally pinches a card. Disable user-scaling
   on the installed PWA (`viewport-fit=cover, user-scalable=no`) so the
   layout stays steady.

## Proposed Solution
Ship a single PR that addresses all six items with no new dependencies.

- **Stray overlay**: bisect z-index in the device list route; suspect candidates
  are `LoadingOverlay`, the filter drawer backdrop, or the toast container.
  Add a Playwright regression: tap a device card and assert detail modal opens.
- **Add `+` FAB**: replace "Add Devices" button with a 56×56 FAB pinned bottom-
  right (Drake supplies the `+` glyph in design tokens). i18n key
  `devices.add.fab.label` for the accessible name.
- **Default Active filter**: change the default `status` query param on
  `/devices` to `Active`. Surface a "Showing Active — show all" link in the
  list header. Persist override via the existing F022 prefs store
  (`devicesPrefs.statusFilter`); don't add new storage.
- **2-up card grid**: change `grid-cols-1 md:grid-cols-2 lg:grid-cols-3` to
  `grid-cols-2 md:grid-cols-3 lg:grid-cols-4`, tighten card padding token,
  trim secondary metadata to two lines.
- **Audit modal**: extract the existing `/admin/audit` filtered view into an
  `AuditHistoryDrawer` component reusable from the device detail page. The
  standalone admin page keeps working for `/admin/audit` discovery.
- **Disable pinch-zoom**: update the `viewport` meta + manifest
  `display: standalone` so the installed PWA doesn't pinch-scale. Document
  that browser-tab usage retains zoom for accessibility.

## User Stories
- *As a household member, I tap a device card and the detail opens — every time.*
- *As Brian, I see only Active devices by default, with one tap to show
  Retired/Disposed when I need them.*
- *As Brian on iPhone, I can see twice as many devices per screen without
  losing legibility.*
- *As Admin, I can review a device's audit history without leaving the device
  detail context.*
- *As anyone, the PWA stops "wobbling" when I accidentally pinch.*

## Acceptance Criteria
- [ ] Tapping any device card opens the detail modal — covered by a new
      Playwright assertion in the existing device-list spec
- [ ] `+` FAB present on `/devices`, accessible-name asserted (axe + Testing
      Library), keyboard-reachable, focus ring visible
- [ ] `/devices` defaults to `status=Active`; "show all" link toggles to
      `status=All`; preference round-trips through the F022 prefs store
- [ ] Mobile breakpoint renders 2 cards per row; visual snapshot updated
- [ ] Device detail page exposes "View change history" button that opens
      `AuditHistoryDrawer` (modal/drawer), not a navigation
- [ ] Installed PWA manifest + viewport meta prevent pinch-zoom on standalone
      display mode; tab-mode behavior unchanged (a11y-preserving)
- [ ] Zero new axe-core violations on `/devices`, `/devices/{id}`, and the
      audit drawer
- [ ] All strings live in `src/lib/i18n/en.json` — no hard-coded text

## Out of Scope
- Replacing the entire navigation chrome (covered by **F027**)
- Infinite scroll / pull-to-refresh (covered by **F028**)
- Dark-mode toggle + audit-log color contrast (covered by **F029**)
- Reworking the filter UI structurally (F027); this entry only changes the
  *default value* and adds the "show all" toggle

## Dependencies
- Drake: `+` glyph in design tokens (or confirm existing glyph is reusable).
- F022 prefs store: already shipped (localStorage); reuse `devicesPrefs` keys.
- Audit-log drawer extraction: refactor of existing `/admin/audit` page; no
  backend changes needed.

## Open Questions
- Should the "+" FAB also appear on `/devices/{id}` detail (for "add similar"),
  or only on the list route? **Recommendation**: list-only for v1.1.
- Pinch-zoom disable conflicts with WCAG 1.4.4 *only* when text resize is also
  blocked; we keep browser font-size scaling intact, so installed-PWA scope is
  defensible. Bishop to sanity-check the accessibility statement.

## Notes / Research
- The "transparent overlay" bug is almost certainly a z-index regression — the
  loading skeleton or a `<dialog>` backdrop that never `.close()`s. First step
  is to reproduce with the Chrome 3D layer view.
- Memory: "All ListQueryValidator classes cap PageSize at 200" — Active-default
  filter doesn't change pagination; it changes the `status` query param only.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (items 1, 3, 6, 7,
  14, 15 in session plan).

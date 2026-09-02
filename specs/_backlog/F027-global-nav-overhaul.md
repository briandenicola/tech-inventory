# F027: Global Navigation Overhaul & Responsive Management Pages

**Status**: backlog
**Priority**: P2
**Effort**: M
**Value**: high
**Risk**: medium
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: vasquez

## Problem
The PWA's navigation chrome is inconsistent across routes and the admin
management pages (Brands, Categories, Locations, Owners, Networks, Tags, Audit)
use wide HTML tables that don't fit on a phone — they either overflow
horizontally with awkward inner scrollbars or collapse and lose columns.

Specific issues from the 2026-05-19 field-test:
- Filters on `/devices` live in a sidebar/inline panel that eats screen
  real estate on mobile. They should collapse into a hamburger/sheet menu.
- Some routes have a top-level "back" / page nav, others don't. There's no
  consistent global menu. Brian explicitly wants a hamburger on every
  authenticated page so the same gesture works everywhere.
- The admin management tables (`/admin/brands`, `/admin/categories`, etc.)
  render as desktop-style tables. On a phone they look unprofessional and the
  primary actions (edit/deactivate) are hard to hit.

## Proposed Solution
A single coordinated nav + responsive-tables pass.

### Global hamburger nav
- Every route under `(authenticated)` gets the same top app-bar with:
  - left: hamburger (opens app drawer with primary nav + Settings + Sign out)
  - center: route title (from i18n key)
  - right: route-local action slot (e.g., `+` FAB target on `/devices`,
    "Add Brand" on `/admin/brands`)
- Filters slide in from the right via a "Filters" button in the app-bar
  action slot, *not* as inline page content.
- Component: `<AppShell>` wrapping `(authenticated)/+layout.svelte`. Drake
  supplies the hamburger glyph; nav items come from a typed `navItems`
  config so order/labels are reviewable in one place.

### Responsive management pages
- Replace tables with a `<ResponsiveList>` that renders:
  - Desktop (`lg+`): a true table with sticky header and row-hover actions.
  - Tablet/Mobile (`<lg`): stacked cards (one row per card) with key/value
    pairs, primary action button, secondary actions in an overflow menu.
- Apply to: `/admin/brands`, `/admin/categories`, `/admin/owners`,
  `/admin/locations`, `/admin/networks`, `/admin/tags`, `/admin/audit`,
  `/admin/imports`.

## User Stories
- *As Brian on iPhone, I open any page and find navigation in the same place —
  a hamburger top-left.*
- *As Admin on mobile, I can manage Brands/Categories/Owners without
  pinch-zooming or horizontal-scrolling a table.*
- *As Brian, filtering devices does not push the device cards off-screen.*

## Acceptance Criteria
- [ ] `<AppShell>` wraps every `(authenticated)` route; visual smoke test
      asserts hamburger + title + action slot on each route
- [ ] Hamburger drawer lists: Devices, Admin (collapsible: Brands, Categories,
      Owners, Locations, Networks, Tags, Imports, Audit Log), Settings,
      Sign out. Order matches `src/lib/nav/navItems.ts` (new typed config).
- [ ] `/devices` Filters move into a right-side sheet triggered from the
      app-bar; the inline filter panel is removed.
- [ ] Every admin management route uses `<ResponsiveList>`; at viewport
      ≤768 px the page shows stacked cards, ≥1024 px shows a table.
- [ ] Keyboard nav: hamburger opens with Enter/Space, traps focus, Esc closes.
      Filter sheet identical.
- [ ] Zero axe-core violations on every refactored route; touch target ≥44 px
      everywhere per WCAG 2.5.5.
- [ ] All strings in `src/lib/i18n/en.json`.
- [ ] Vitest component tests cover the hamburger drawer's rendering, keyboard
      open/focus-trap/Escape-close behavior, and the `<ResponsiveList>`
      breakpoint switch (stacked cards ≤768px, table ≥1024px). No browser
      E2E layer exists to update nav-related journeys in post-retirement
      (`specs/004-agentic-development-foundation/brief.md` §2.1); a
      real-device visual pass of the refactored routes is added to
      `docs/testing/manual-pwa-validation.md` instead.

## Out of Scope
- Pull-down-to-refresh and infinite scroll (**F028**).
- Dark mode (**F029**).
- Per-user nav personalization or pinning favorite filters.
- A full design refresh — this is a structural rework, not a visual redesign.
  Tokens/colors stay as-is unless F029 lands first.

## Dependencies
- Drake: hamburger + close glyphs; confirm caret styling for collapsible
  Admin sub-menu.
- ADR (Ripley): adopt `<AppShell>` + responsive-list pattern as the project
  standard so future routes follow it without one-off layouts.
- F026 should land first — the `+` FAB lives in the new action slot.

## Open Questions
- Do we want the hamburger drawer to be persistent (always-on, ≥xl) like a
  desktop nav rail, or hamburger-on-all-sizes for consistency? Brian's note
  was "on all pages," which reads as "always available, even desktop." Default
  to hamburger-on-all-sizes; revisit if it feels too sparse on desktop.
- Filter-sheet right vs. left side? Right is conventional for "tools"; left
  collides with the hamburger.

## Notes / Research
- Existing `<ListPage>` partial in `src/TechInventory.Web/src/lib/components`
  is a good starting point — extend rather than replace.
- This work touches every authenticated route; coordinate with Apone so the
  affected Vitest component harnesses (and the
  `docs/testing/manual-pwa-validation.md` route list) are updated *in the
  same PR* to avoid two-step breakage.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (items 4, 9, 10 in
  session plan).
- 2026-09-02: **scope narrowed** (Ripley, design review D-176). The responsive
  management-pages half is delivered via
  `src/lib/components/admin/ResponsiveAdminList.svelte`. The mobile navigation
  chrome half — the full-screen hamburger drawer described above — is
  **superseded by F045** (`specs/_backlog/F045-pwa-shell-and-device-list.md`),
  which replaces the drawer with a compact anchored popover and adds a
  persistent bottom navigation for installed-PWA mode. The drawer description
  above is retained as the record of the original design, not as pending work.
  The open question "persistent desktop rail vs. hamburger-on-all-sizes" was
  closed in favor of hamburger-on-all-sizes (see docs/backlog.md, "Persistent
  desktop rail — not pursued").

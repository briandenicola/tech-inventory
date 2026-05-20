# Archived Decisions

Master record of project decisions, merged from decision inbox. Each entry includes decision date, author, context, rationale, and consequences.

---

## D-143: F027 Navigation Glyph Set

**Date:** 2026-05-20  
**By:** Drake (Designer / Visual Engineer)  
**Status:** Proposed  
**Related:** F027 spec (Global Nav Overhaul), PRD §F3 (Apple-elegant aesthetic), D-141 (F029 semantic tokens), Constitution §6.5.5 (design tokens), WCAG 2.5.5 (touch-target size)

### Decision

Three inline SVG glyphs for the F027 global navigation system:
- **Hamburger Glyph** (App Drawer Trigger): Three horizontal lines (y=6, y=12, y=18) with 6px gaps, stroke-width 2, `viewBox="0 0 24 24"`
- **Close Glyph** (Drawer & Sheet Dismiss): X pattern with two diagonal strokes, visually identical across both contexts (drawer and filter sheet) for muscle memory
- **Caret Glyph** (Collapsible Admin Sub-Menu): Chevron-down with rotate-180 on expand, matches existing user-menu pattern

All use `currentColor` for theme inheritance, read clearly at 24×24 in 44×44 touch targets. Touch-target enforcement: `min-h-11 min-w-11` for square buttons, `min-h-11` for full-width nav items.

### Rationale

- Hamburger icon is instantly recognizable; stroke-width 2 matches existing close glyph weight
- Close X pattern is already in use; diagonal lines contrast with horizontal hamburger lines for visual distinction
- Chevron-down already proven in user-menu dropdown (lines 166–175); reusing enforces consistency; iOS/macOS pattern fits PRD §F3 "mid-2010s Apple" aesthetic
- Inline SVG only (not separate files); no per-context variations for close glyph
- All glyphs use existing neutral palette (not new diff tokens); plays nicely with F029

### Consequences

- Vasquez must implement glyphs in `(authenticated)/+layout.svelte` or `<AppShell>` component with conditional rendering for hamburger/close toggle and admin caret rotation
- i18n keys needed: `header.openMenu`, `header.closeMenu`, `devices.filters.closeFilters`, `nav.admin`
- Playwright page object selectors for hamburger trigger must be updated (Apone coordination required)
- Drake validates touch targets and contrast post-integration via axe-core; zero violations required to merge

---

## D-142: F027 — `<AppShell>` and `<ResponsiveList>` as Standard Authenticated-Route Patterns

**Date:** 2026-05-20  
**By:** Ripley (Lead / Architect)  
**Status:** Proposed → Implemented when Vasquez ships  
**Related:** F027 spec, ADR-0002, D-137 (Apple-elegant aesthetic), D-005 (viewport matrix), Constitution §2.2

### Context

Brian's 2026-05-19 field test surfaced three failures:
1. No consistent navigation gesture; hamburger on mobile but dropdown on desktop
2. Filters consume screen real estate; should be a sheet/drawer
3. Admin tables overflow on mobile; no responsive layout

### Decision

#### 1. `<AppShell>` Component Contract

File: `src/TechInventory.Web/src/lib/components/AppShell.svelte`

Props:
- `titleKey`: i18n key for route title (center of app bar)
- `actions`: Snippet rendered in top-bar right action slot
- `children`: Named snippet for page content

Behavior:
- Top bar: hamburger (left) · title (center) · action slot (right)
- Hamburger opens left-side drawer overlay on **all viewport sizes** (hamburger-on-all-sizes)
- Drawer contains nav items from `navItems.ts`, Settings, Sign out
- Admin sub-menu collapses/expands in-drawer with caret toggle
- Focus trap: Tab cycles within open drawer; Escape closes and returns focus to hamburger
- Filter sheets render in the `actions` slot and slide from **right** side
- Route change auto-closes drawer
- Footer renders below page content inside AppShell

**Rationale for hamburger-on-all-sizes:**
Brian's explicit direction: "the same gesture on every page." A persistent desktop rail would introduce two navigation paradigms (rail + hamburger), increasing cognitive load for a single-household app. Phone-first for this use case; consistency over desktop density wins.

#### 2. `<ResponsiveList>` Component Contract

File: `src/TechInventory.Web/src/lib/components/ResponsiveList.svelte`

Props include:
- `columns: Column<T>[]` — column definitions with i18n headerKey, accessor, optional hideOnCard, optional cellClass
- `items: T[]` — data to render
- `keyAccessor: (item: T) => string` — unique key for Svelte each
- `primaryAction?: RowAction<T>` — prominent button on card, first action on table
- `overflowActions?: RowAction<T>[]` — rendered in "..." menu on table, stacked on card
- `cardSnippet?: Snippet<[T]>` — custom card rendering override
- `rowSnippet?: Snippet<[T]>` — custom row rendering override
- `loading?: boolean`, `error?: string | null`, `emptyKey?: string`, `onRetry?: () => void`

**Breakpoint:** `lg` (1024px). Below `lg` → stacked cards. At/above `lg` → table with sticky header and row-hover actions.

**Rationale:** `md` (768px) is too narrow for meaningful table columns in admin pages with 3–5 columns. At `lg` (1024px), ~960px usable width inside `max-w-7xl` container is comfortable for 4–5 columns.

#### 3. `navItems.ts` Typed Config

File: `src/TechInventory.Web/src/lib/nav/navItems.ts`

```typescript
export interface NavItem {
  labelKey: string;
  href: string;
  icon?: ComponentType;
  roles?: UserRole[];
  children?: NavItem[];
}
```

Config:
- Devices (all roles)
- Admin (Admin only, collapsible with children: Brands, Categories, Owners, Locations, Networks, Tags, Import, Export, Audit)
- Settings (all roles)

#### 4. Route-Naming Correction

- Spec says `/admin/imports` but actual route is `/admin/import` (singular). **Vasquez must use `/admin/import` (singular)** in navItems and all references.
- `/admin/export` exists and is functional. **Decision: `/admin/export` gets `<AppShell>` wrapper** but NOT `<ResponsiveList>` (it's a form page, not a data table). Same for `/admin/import` (3-step wizard).

#### 5. Page → Component Mapping

| Route | AppShell | ResponsiveList |
|-------|----------|----------------|
| `/devices` | ✅ | ❌ (has its own card/table toggle) |
| `/admin/brands` | ✅ | ✅ |
| `/admin/categories` | ✅ | ✅ |
| `/admin/owners` | ✅ | ✅ |
| `/admin/locations` | ✅ | ✅ |
| `/admin/networks` | ✅ | ✅ |
| `/admin/tags` | ✅ | ✅ |
| `/admin/audit` | ✅ | ✅ |
| `/admin/import` | ✅ | ❌ (wizard) |
| `/admin/export` | ✅ | ❌ (form) |
| `/settings` | ✅ | ❌ (form) |

### Consequences

**Positive:**
- Single navigation paradigm across all viewports — less cognitive overhead
- Admin pages usable on phones without horizontal scroll or pinch-zoom
- Touch targets ≥44px everywhere (WCAG 2.5.5)
- `navItems.ts` is the single source of truth
- New admin pages get responsive behavior by passing `columns` + `items`

**Negative:**
- Hamburger-on-all-sizes means desktop users always need one click to reach nav
- Migration touches every authenticated route in one PR — coordinate with Apone

**Neutral:**
- Existing `(authenticated)/+layout.svelte` remains the layout file — AppShell rendered inside it
- `<PaginationControls>` contract unchanged — sibling, not embedded

### Verification Checklist

- [ ] Visual smoke: every `(authenticated)` route shows hamburger + title + action slot at 375px and 1280px
- [ ] axe-core: zero violations on every refactored route
- [ ] Touch targets: all interactive elements ≥44×44px
- [ ] Focus trap: Tab trapped in open drawer; Escape returns focus to trigger
- [ ] Keyboard nav: Enter/Space opens hamburger; Escape closes; Admin sub-menu expandable with Enter
- [ ] Breakpoint: at 1023px admin pages show cards; at 1024px they show table
- [ ] i18n: no hard-coded strings in AppShell or ResponsiveList
- [ ] `navItems.ts` matches rendered drawer order exactly (unit test)

---

## D-141-F027: Field-Test Feedback → New Spec 003 (Not Amendment to 002)

**By:** Ripley  
**Date:** 2026-05-20  
**Status:** Proposed

### Context

Brian's field-test surfaced 11 issues. Question: fold into spec 002 or create spec 003?

### Decision

Create `specs/003-pwa-polish/` as a standalone spec.

### Rationale

1. Spec 002 is 33/53 tasks done with well-defined scope — amending it muddies completion tracking
2. Constitution §1: "Scope creep is the #1 enemy — defer to Non-Goals aggressively"
3. These are post-field-test refinements forming a coherent workstream (bugs + UX + features)
4. Reporting (#11) is a material new capability that doesn't fit 002's "Frontend MVP" framing
5. Dark mode was explicitly listed as non-goal in spec 002 §2.2 — promoting it requires a new scope decision

### Consequences

- Spec 002 Rounds 7–11 proceed unchanged (import, export, polish, E2E, PWA)
- Spec 003 can run in parallel with spec 002 remaining rounds
- Some P003 tasks extend spec 002 work (export, hamburger) — noted as "Extends" not "Replaces"
- 3 ADRs needed (dark mode strategy, insurance report format, merge endpoint design)

### Overlaps Resolved

- Infinite scroll supersedes T15 pagination (keeps a11y fallback)
- Hamburger nav amends T13 (consistency fix, not rewrite)
- Export report extends T38 (additive)
- Dark mode promoted from 002 non-goal to 003 P2 feature

---

## D-140: Hicks — Explicit Merge Routes + CSV-First Insurance Export

**Proposed by:** Hicks (Backend)  
**Date:** 2026-05-20  
**Status:** Proposed  
**Related:** `specs/003-pwa-polish/spec.md` §5.4–5.5, tasks P003-T09/P003-T10

### Decision

- Keep duplicate-merge endpoints explicit per resource: `POST /api/v1/brands/merge`, `POST /api/v1/categories/merge`, `POST /api/v1/locations/merge`
- Ship insurance export as **CSV-only** for v1 via `GET /api/v1/reports/insurance`
  - Generated-at comment line
  - Active-device rows
  - Optional `locationId` filter
  - `TOTAL` footer row
  - PDF stays deferred

### Rationale

- API stays resource-oriented and produces clearer OpenAPI contracts than a single runtime-switched `{entity}` endpoint
- Category merges need descendant/depth validation plus descendant reparenting — generic handler would hide category-specific business rules or add branching complexity inside controller layer
- CSV satisfies the field-test requirement immediately and avoids introducing a PDF rendering dependency before the insurance workflow is proven in the UI
- Shared merge abstractions remove duplication at the validator/response level while preserving per-entity business rules

### Consequences

- Frontend work targets stable, explicit merge routes without guessing entity names in path
- Shared merge abstractions remove duplication at validator/response level
- Future PDF export can layer on a separate endpoint or additional format option without breaking CSV contract

---

## D-139: Vasquez — Merge UI + Reports

**Date:** 2026-05-20

### Decision 1: Merge Target Pickers

Merge target pickers use `referenceDataStore`, not the current admin table page.

**Rationale:**  
- Brands and locations are paginated, so the current table can hide valid merge targets
- Reusable merge modal should receive the full active lookup list from `src/TechInventory.Web/src/lib/stores/referenceData.ts`
- Page refreshes that store after successful merge so forms and filters stop showing the merged-away entity immediately

### Decision 2: Frontend Reporting Normalizes Both API Shapes

Frontend reporting normalizes both current and planned API shapes.

**Rationale:**  
- Current backend/reporting models expose fields like `totalActiveDeviceCount`, `devices`, `daysRemaining`
- Requested frontend contract uses `totalDevices`, `items`, `daysUntilExpiry`
- `src/TechInventory.Web/src/lib/utils/reports.ts` adapts either shape into one UI model so `/reports` can ship now without blocking on final backend/OpenAPI cleanup
- Thin local adapter keeps UI moving without inventing ad-hoc fetch calls

### Reusable Patterns

- Merge modal needs more than paginated table row set to be trustworthy
- Both patterns reusable for future admin/reference workflows and reporting follow-up work

---

## D-138: Hicks & Vasquez — Authenticated Navigation Config + Shared Admin Hub Routes

**Date:** 2026-05-20

### Decision

- Centralized authenticated navigation metadata in `src/TechInventory.Web/src/lib/navigation/appNav.ts`
- Reused the same route list for desktop shell nav, mobile hamburger nav, and `/admin` hub cards
- Expanded admin navigation coverage to include Categories and Owners alongside Brands, Locations, Networks, Tags

### Rationale

The hamburger inconsistency was caused by route lists drifting apart after new admin pages landed. One shared config keeps labels, order, and active-state rules aligned across all authenticated surfaces and makes future route additions less error-prone.

### Related

Pairs with D-092 (admin hub as navigation surface)

---

## D-137: Vasquez — Audit Modal + Dark Mode Delivery Decisions

**Date:** 2026-05-20  
**Tasks:** P003-T07, P003-T08

### Decision 1: Reusable Native-Dialog Audit Modal

- Ship a single `src/TechInventory.Web/src/lib/components/AuditLogModal.svelte` built on native `<dialog>`
- Use from both device detail surfaces and from the admin hub
- Device-scoped opens pass `entityType="Device"` + `entityId` and render existing `DeviceAuditTrail.svelte` summary above paginated log
- Admin-global opens omit `entityId` and expose lightweight entity-type filter

**Why:**  
- Keeps audit UX consistent across surfaces, preserves user context, reuses already-shipped audit diff/pagination patterns
- Avoids maintaining separate drawer/page experiences

### Decision 2: Resolved Theme Store Drives Both Tokens and Tailwind Dark Utilities

- Centralize theme state in `src/TechInventory.Web/src/lib/stores/theme.svelte.ts`
- Persist the preference in `localStorage['theme-preference']`
- Resolve `light|dark|system` into a concrete theme and apply **both** `document.documentElement.dataset.theme` and the `.dark` class
- Keep the pre-hydration script in `src/app.html` responsible for setting the resolved theme before first paint

**Why:**  
- In this codebase, `data-theme` is needed for token overrides in `tokens.css`, but the existing UI depends heavily on Tailwind `dark:` utilities
- Applying both from one source of truth makes manual theme selection actually affect the full app instead of only token-backed surfaces

---

## D-136: Vasquez — Layout-Level Pull-to-Refresh Registry

**Date:** 2026-05-20

### Decision

- Add one reusable `PullToRefresh.svelte` wrapper at `src/TechInventory.Web/src/routes/(authenticated)/+layout.svelte`
- Let each authenticated page register a route-scoped refresh callback through `src/TechInventory.Web/src/lib/stores/pullToRefresh.ts`
- Fall back to SvelteKit `invalidateAll()` when a route has no custom data refresh callback

### Rationale

Centralizing the touch gesture, spinner, aria-live announcement, pointer-coarse guard, and `overscroll-behavior-y: contain` in the authenticated layout keeps behavior consistent across device/admin/reporting surfaces. Route-scoped callbacks preserve page-specific invalidation rules without duplicating touch logic on every page.

Examples:
- Clearing infinite-scroll accumulation on `/devices`
- Refetching device + reference data on detail/edit routes
- Reusing existing `load*` admin list functions

### Related

Pairs with P003-T03 (infinite scroll devices list), P003-T04 (pull-to-refresh mobile UX)

---

## D-135: Vasquez — P003-T02 Device Tag Sync

**Date:** 2026-05-20  
**Task:** P003-T02 — Fix tag assignment on device forms

### Decision

Use the existing device-tag relationship endpoints (`GET/POST /api/v1/devices/{id}/tags`, `DELETE /api/v1/devices/{id}/tags/{tagId}`) as the single source of truth for tag assignment. Do not invent a `tagIds` field on the device create/update API payloads.

### Rationale

The OpenAPI contract exposes tags as a separate relationship resource, and `DeviceResponse` does not embed assigned tags. Frontend create/edit flows therefore need a second persistence step after the base device save, plus a dedicated fetch for edit/detail hydration.

### Implementation Impact

- `DeviceForm` owns client-side `tagIds` state only
- Create/edit pages call `devices.syncTags(...)` after `devices.create(...)` / `devices.update(...)`
- Device detail/edit views fetch assigned tags explicitly so saved tags remain visible and preselected

---

## D-134-T01: Apone — F027 E2E Refactor Plan + Page-Object Scaffold

**Author:** Apone (Tester/QA)  
**Date:** 2026-05-20  
**Related:** F027 Global Navigation Overhaul spec  
**Status:** Plan — awaiting Vasquez's `<AppShell>` implementation

### Executive Summary

F027 replaces the current authenticated layout's nav structure with a unified `<AppShell>` component. This plan:

1. Audits 6 journey specs with nav/mobile-menu/admin-link coupling
2. Proposes a `tests/e2e/pages/` page-object scaffold with `AppShellPage` as the base
3. Defines locator-stability contracts Vasquez must bake into `<AppShell>` and `<ResponsiveList>` on day one
4. Expands axe-core coverage to require zero violations in **both light and dark themes** per route

### Journey Audit — Nav-Coupled Specs

**Require Moderate Updates:**
- `06-browse-filter.spec.ts` — Filters currently inline; post-F027 they move to right-side sheet. Filter locators move from inline page context to modal context; sheet open/close mechanics added.
- `10-reference-data-admin.spec.ts` — Admin nav links currently in desktop user-menu dropdown + mobile hamburger. Tests must: open hamburger → expand Admin section → click admin route link.
- `11-role-enforcement.spec.ts` — Role-gated admin links. Tests must: assert Viewer role does NOT see Admin section in hamburger drawer.

**Require Minor Updates:**
- `13-a11y-smoke.spec.ts` — Must run axe twice per route (light + dark) per constitution §6.5.7 + F027 AC. Each admin route now uses `<ResponsiveList>` which must be a11y-clean at both `<lg` (cards) and `≥lg` (table).
- `03-create-device.spec.ts`, `08-import-csv.spec.ts`, `14-import-model-display.spec.ts` — Direct URL navigation, no nav chrome interaction. If hamburger/filter button appears in viewport, may need `waitForLoadState('domcontentloaded')` to avoid racing against AppShell mount.
- `01-sign-in.spec.ts`, `04-edit-device.spec.ts`, `05-delete-device.spec.ts`, `07-detail-view.spec.ts`, `12-offline-app-shell.spec.ts`, `09-export-csv.spec.ts` — Zero nav coupling.

### Proposed Page-Object Scaffold

**Directory Structure:**
```
tests/e2e/pages/
├── AppShellPage.ts
├── DevicesListPage.ts
├── DeviceDetailPage.ts
├── DeviceEditPage.ts
├── AdminBrandsPage.ts
├── AdminCategoriesPage.ts
├── AdminOwnersPage.ts
├── AdminLocationsPage.ts
├── AdminNetworksPage.ts
├── AdminTagsPage.ts
├── AdminImportPage.ts
├── AdminAuditPage.ts
└── index.ts
```

**Base Class: `AppShellPage`**

Provides:
- `hamburgerButton()` → locator for hamburger button
- `drawer()` → locator for navigation drawer dialog
- `drawerCloseButton()` → locator for close button
- `openDrawer()` → click hamburger, wait for drawer visible
- `closeDrawer()` → click close or press Escape
- `navigateTo(navKey)` → open drawer, find nav link, click it
- `toggleAdminMenu()` → expand/collapse Admin collapsible
- `assertA11y(theme)` → run axe-core in light or dark theme

### Locator Contracts for Vasquez

Vasquez must guarantee these selectors in `<AppShell>` + `<ResponsiveList>` on day one:

- Hamburger button: `role="button"` with `aria-label` matching `/open menu/i`
- Drawer container: `role="dialog"` with `aria-label` matching `/navigation menu/i`
- Drawer close button: within drawer, `role="button"` with `aria-label` matching `/close/i`
- Nav links: semantic `<a>` or `<button>` with stable `aria-label` or text content
- Admin collapsible trigger: `aria-expanded={boolean}` for state
- Responsive table/card toggle: (if present) `role="button"` with clear state assertion
- Row action buttons: `role="button"` or menu trigger with stable labels

### Axe-Core Coverage Expansion

Post-F027, E2E must validate:
- Zero violations in **light theme** at default viewport (375px, 1280px)
- Zero violations in **dark theme** at default viewport (375px, 1280px)
- `<ResponsiveList>` at both card view (`<lg`) and table view (`≥lg`)
- All admin routes + `/devices` + `/settings` + `/admin/audit`
- Touch targets via bounding box assertion (≥44×44 px for interactive elements)
- Focus management (Tab trap in drawer, Escape to close)
- Keyboard navigation (Admin submenu toggle with Enter)

### Next Steps

1. Vasquez implements `<AppShell>` with these glyph specs (D-143) and locator guarantees
2. Apone scaffolds page objects and prepares E2E updates in parallel
3. Integration: Vasquez PR updates E2E selectors; Apone validates zero axe violations
4. Merge only after full test suite passes in both themes

---

## D-133: Formal Reports Backlog (2026-05-20)

**By:** Brian (via Copilot)  
**Date:** 2026-05-20  
**Status:** Proposed

### Decision

The following fun/whimsical report ideas must be formally captured in the backlog (not just session notes):
- Cell phones owned over the eras/decades
- Laptops over the decades
- "Achievement" style reports (e.g., "most brands used", "oldest device still active")
- Historical tech timeline visualization

### Rationale

These extend T11/T12 reporting work and should be tracked as future features (F-level backlog items). User request — Brian explicitly said "reports need to be added to our formal background so they're not lost"

### Consequences

- Deduplicate and formalize in `docs/backlog.md` with feature IDs (F035–F038)
- Establish priority (P3 for reports backlog, P2 for Silent SSO)
- Track status (Backlog vs. In Progress)

---

## Abbreviated Reference — Decisions in `merged/` (Prior Phases)

This section acknowledges decisions already archived in `.squad/decisions/merged/`:

- **D-125** (Apone Phase 2, 2026-05-18): Reference entity tests for 4 entities (brands/locations/networks/tags), categories/owners deferred to follow-up
- **D-124** (Apone Phase 2, 2026-05-18): Zod schema tests direct validation; defer page-level UI tests to E2E
- **D-123** (Apone Phase 2, 2026-05-18): Backdrop click tests deferred to E2E
- **D-122** (Phase 2 Round 6b, 2026-05-18): Admin namespace in i18n — centralized pattern
- **D-121** (Phase 2 Round 6b, 2026-05-18): Categories + Owners client API groups already present
- **D-120** (Phase 2 Round 6b, 2026-05-18): Owner deactivate 409 error display — toast with backend reason
- **D-119** (Phase 2 Round 6b, 2026-05-18): Owners role gate pattern — dual-layer (client redirect + backend enforce)
- **D-118** (Phase 2 Round 6b, 2026-05-18): Category search/filter approach — text filter with ancestor inclusion
- **D-117** (Phase 2 Round 6b, 2026-05-18): Parent selector UX — searchable dropdown (native select)
- **D-116** (Phase 2 Round 6b, 2026-05-18): Categories tree component pattern — flat list with depth indentation

All prior decisions remain in force and are referenced by current work.

---

**Last updated:** 2026-05-20T18:33Z by Scribe

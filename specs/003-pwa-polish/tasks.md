# Tasks — 003 PWA Polish & Field-Test Fixes

**Spec**: `specs/003-pwa-polish/spec.md`
**Status**: Draft
**Created**: 2026-05-20
**Origin**: Brian field-test feedback (11 items, #1 already fixed)

---

## Task List

### Round A: Bugs (P0) — Immediate

| ID | Task | Owner | Priority | Effort | Description | Definition of Done | Dependencies |
|----|------|-------|----------|--------|-------------|-------------------|--------------|
| ✅ P003-T01 | Fix audit log visibility | Vasquez | P0 | S | Audit log text is unreadable due to theme/font color contrast issue. Identify the `--color-*` token(s) used in audit components, ensure WCAG AA contrast ratio (4.5:1 minimum). | Audit log text readable in both light theme and system dark mode; contrast ratio ≥ 4.5:1 verified via axe-core. | — |
| ✅ P003-T02 | Fix tag assignment on device forms | Vasquez + Hicks | P0 | M | Tag multi-select on device create/edit form is broken or inaccessible. Investigate: is the component rendering? Is the API binding correct? Fix so users can add/remove tags on devices. | Tags can be added/removed on device create and edit forms; tags persist on save; visible on device detail page. | — |

---

### Round B: Mobile UX (P1) — High Priority

| ID | Task | Owner | Priority | Effort | Description | Definition of Done | Dependencies |
|----|------|-------|----------|--------|-------------|-------------------|--------------|
| ✅ P003-T03 | Infinite scroll on devices list | Vasquez | P1 | M | Replace pagination with infinite scroll using Intersection Observer. Keep server pagination API unchanged. Add "back to top" FAB. Retain pagination fallback for `prefers-reduced-motion`. | Devices load progressively on scroll; no page reload; FAB visible after scroll; a11y fallback works. | Spec 002 T15 (✅) |
| ✅ P003-T04 | Pull-to-refresh on all pages | Vasquez | P1 | M | Implement pull-to-refresh gesture on touch devices. Use `overscroll-behavior-y: contain` + custom touch handler. Trigger query invalidation. Visual spinner indicator. Only on `pointer: coarse` devices. | Pull-to-refresh works on mobile (phone/tablet); spinner shows during refresh; data reloads; no-op on desktop. | — |
| ✅ P003-T05 | Consistent hamburger nav on all routes | Vasquez | P1 | S | Audit all routes for hamburger menu presence. Ensure mobile nav renders on admin sub-routes (`/admin/brands`, `/admin/categories`, etc.), device sub-routes, and settings. Fix any route where nav is missing. | Hamburger menu visible and functional on every route at mobile viewport; nav items consistent. | Spec 002 T13 (✅) |
| ✅ P003-T06 | Responsive admin/management tables | Vasquez | P1 | M | Admin entity tables (brands, categories, locations, networks, owners, tags) don't fit on mobile. Implement responsive pattern: card layout on mobile (`< 768px`), table on desktop. Or horizontal scroll with sticky first column. | Admin tables usable on 375px viewport; no horizontal overflow without indication; touch targets ≥ 44px. | Spec 002 T27-T32 (✅) |
| ✅ P003-T07 | Audit log as modal (not page) | Vasquez | P1 | S | Convert audit log from standalone page to modal overlay. Trigger from: device detail page ("View History" button) and admin area. Modal shows filtered audit events for context (device-scoped or global). Paginated within modal. | Audit log opens as modal; filterable by device; pagination within modal; close returns to previous context. | P003-T01 |

---

### Round C: Features (P2) — New Capabilities

| ID | Task | Owner | Priority | Effort | Description | Definition of Done | Dependencies |
|----|------|-------|----------|--------|-------------|-------------------|--------------|
| ✅ P003-T08 | Dark mode toggle | Vasquez | P2 | M | Add manual dark/light/system toggle. Extend `tokens.css` with `[data-theme="dark"]` variants. Create `ThemeProvider` component. Persist choice in localStorage. Toggle UI in header or settings. | Dark mode renders correctly on all routes; toggle persists across sessions; respects system preference as default. | — |
| ✅ P003-T09 | Merge duplicate reference entities | Hicks + Vasquez | P2 | L | **Backend (Hicks):** New endpoint `POST /api/v1/{entity}/merge` — reassigns all FK references from source to target, soft-deletes source, logs AuditEvent. Entities: brands, categories, locations. **Frontend (Vasquez):** "Merge" button on admin pages → modal with target entity selector + confirmation. Admin-only. | Merge endpoint works for brands/categories/locations; all device FKs updated; source deactivated; audit logged; UI shows merge modal; tests pass. | Spec 002 T27-T30 (✅) |
| P003-T10 | Insurance export report | Hicks + Vasquez | P2 | M | **Backend (Hicks):** New endpoint `GET /api/v1/reports/insurance` — returns device data formatted for insurance: name, brand, category, serial, purchase date, price, location. CSV with summary header (total value, device count, generated date). **Frontend (Vasquez):** Button on export page (or standalone `/reports/insurance` route). | Insurance report downloads as CSV; includes summary row; all active devices included; filterable by location. | Spec 002 T38 (unstarted — can parallel) |

---

### Round D: Reporting Foundation (P2) — Design + API

| ID | Task | Owner | Priority | Effort | Description | Definition of Done | Dependencies |
|----|------|-------|----------|--------|-------------|-------------------|--------------|
| P003-T11 | ✅ Reporting API endpoints | Hicks | P2 | L | Design and implement reporting query endpoints: `GET /api/v1/reports/summary` (total devices, total value, by-category counts, by-location counts), `GET /api/v1/reports/warranties` (devices with warranty expiring in N days), `GET /api/v1/reports/spending` (purchase value over time, grouped by month/year). | 3 reporting endpoints implemented with tests; OpenAPI spec updated; no N+1 queries. | — |
| ✅ P003-T12 | Reporting UI — summary cards | Vasquez | P2 | M | Create `/reports` page with summary cards: total device count, total inventory value, top categories (bar list), top locations, devices by status. Consume `reports/summary` endpoint. Responsive grid layout. | Reports page renders summary data; responsive; loading/error states; axe-core zero violations. | P003-T11 |
| ✅ P003-T13 | Reporting UI — warranty expiry list | Vasquez | P2 | S | On `/reports` page (or sub-tab): list of devices with warranties expiring within 30/60/90 days. Sortable by expiry date. Link to device detail. | Warranty list renders; date filtering works; links to device detail. | P003-T11 |

---

### Round E: Tests (follows implementation)

| ID | Task | Owner | Priority | Effort | Description | Definition of Done | Dependencies |
|----|------|-------|----------|--------|-------------|-------------------|--------------|
| P003-T14 | Component tests for Round A+B fixes | Apone | P1 | M | Vitest tests: audit log contrast (axe-core), tag assignment form, infinite scroll sentinel, pull-to-refresh trigger, responsive table breakpoints, modal open/close. | 8+ component tests green; axe-core zero violations on all tested states. | P003-T01 through P003-T07 |
| P003-T15 | Component tests for Round C+D features | Apone | P2 | M | Vitest tests: dark mode toggle + persistence, merge modal flow, insurance report download, summary cards rendering, warranty list. | 6+ component tests green; axe-core zero violations. | P003-T08 through P003-T13 |
| P003-T16 | Backend tests for merge + reporting | Apone | P2 | M | xUnit + FluentAssertions: merge endpoint (FK reassignment, soft-delete, audit), insurance report (CSV format, summary row), reporting endpoints (correct aggregations). | 10+ backend tests green; edge cases covered (merge with no references, empty reports). | P003-T09, P003-T10, P003-T11 |

---

## Task Count: **16 tasks**

### Round Breakdown

| Round | Focus | Tasks | Owners | Can Start |
|-------|-------|-------|--------|-----------|
| **A** (Bugs) | P0 fixes | 2 (T01-T02) | Vasquez, Hicks | Immediately |
| **B** (Mobile UX) | P1 improvements | 5 (T03-T07) | Vasquez | Immediately (all prerequisites ✅) |
| **C** (Features) | P2 new caps | 3 (T08-T10) | Hicks + Vasquez | After Round A (T08 independent) |
| **D** (Reporting) | P2 API + UI | 3 (T11-T13) | Hicks + Vasquez | Independent of A/B/C |
| **E** (Tests) | Validation | 3 (T14-T16) | Apone | After respective implementation rounds |

---

## Parallelization Strategy

```
Timeline:
─────────────────────────────────────────────────────
Week 1:  [Round A: Bugs]  ║  [Round B: UX — T03,T04,T05,T06]
                          ║  [Round D: T11 — Hicks backend]
─────────────────────────────────────────────────────
Week 2:  [Round B: T07]   ║  [Round C: T08, T09, T10]
         [Round E: T14]   ║  [Round D: T12, T13]
─────────────────────────────────────────────────────
Week 3:  [Round E: T15, T16]
─────────────────────────────────────────────────────
```

**Key parallelism:**
- Rounds A + B + D can all start in parallel (no cross-dependencies)
- Hicks can start T11 (reporting API) while Vasquez does T03-T06
- T08 (dark mode) is independent — can slot anywhere
- T09 (merge) needs backend first (Hicks), then frontend (Vasquez)

---

## Cast Assignments

| Agent | Tasks | Scope |
|-------|-------|-------|
| **Vasquez** (Frontend) | 11 | Audit fix, tag fix (collab), infinite scroll, pull-to-refresh, hamburger audit, responsive tables, audit modal, dark mode, merge UI, insurance report UI, reports page |
| **Hicks** (Backend) | 4 | Tag fix (collab), merge endpoint, insurance report endpoint, reporting API (3 endpoints) |
| **Apone** (QA) | 3 | Component tests (Rounds A-D), backend tests (merge + reporting) |
| **Drake** (Design) | 0 | Consulted on responsive table pattern + dark mode token palette (no dedicated tasks) |

---

## Dependency Graph

```
P003-T01 (audit visibility) ─→ P003-T07 (audit modal)
P003-T01 ─→ P003-T14 (tests)

P003-T02 (tag fix) ─→ P003-T14 (tests)

P003-T03..T06 (UX) ─→ P003-T14 (tests)

P003-T09 backend (Hicks) ─→ P003-T09 frontend (Vasquez) ─→ P003-T15 (tests)
P003-T10 backend (Hicks) ─→ P003-T10 frontend (Vasquez) ─→ P003-T15 (tests)
P003-T11 (reporting API) ─→ P003-T12, P003-T13 ─→ P003-T15 (tests)

P003-T09, T10, T11 ─→ P003-T16 (backend tests)
```

---

## Effort Summary

| Effort | Count | Description |
|--------|-------|-------------|
| **S** (Small, < 2h) | 4 | T01, T05, T07, T13 |
| **M** (Medium, 2-6h) | 9 | T02, T03, T04, T06, T08, T10, T12, T14, T15 |
| **L** (Large, 6-12h) | 3 | T09, T11, T16 |

**Total estimated effort:** ~60-80 hours across all agents

---

## ADR Needed

- **ADR-XXX**: Dark mode implementation strategy (CSS custom properties vs. Tailwind dark: prefix vs. both)
- **ADR-XXX**: Insurance report format (CSV-only for v1, PDF deferred)
- **ADR-XXX**: Merge endpoint design (generic vs. per-entity)

## Backend Progress Notes

- **2026-05-20 (Hicks):** Backend delivery for `P003-T09` and `P003-T10` is complete: admin merge endpoints for brands/categories/locations and `GET /api/v1/reports/insurance` are implemented, tested, and reflected in `openapi.yaml`. Frontend follow-through remains before the shared tasks can be checked off.

## Frontend Progress Notes

- **2026-05-20 (Vasquez):** Follow-up F040 regression fix shipped on `/devices`: `DeviceListAddActions.svelte` now restores the add-device entry point as a route-linked pattern (`/devices/new`), with a bottom-left safe-area-aware mobile FAB, a desktop header link, Viewer-hidden create affordances, and an empty-state CTA that follows the same role gate. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, full `pnpm exec vitest run` ✅, `pnpm run build` ✅.
- **2026-05-20 (Vasquez):** Reworked `/devices` plus the Brands/Categories/Locations/Networks/Owners/Tags admin pages for the no-horizontal-scroll mobile pattern. Added `src\TechInventory.Web\src\lib\components\ActionOverflowMenu.svelte` and `ResponsiveListCard.svelte`, rewrote `src\TechInventory.Web\src\lib\components\DeviceTable.svelte` to render heading-first stacked cards below `md`, removed the old mobile table-mode toggle from `src\TechInventory.Web\src\routes\(authenticated)\devices\+page.svelte`, and updated responsive harness/component coverage. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, focused responsive Vitest ✅, full `pnpm exec vitest run` ✅ (**400 passed / 1 skipped**), `pnpm run build` ✅ after clearing stale `.svelte-kit` output when an initial manifest/prerender artifact flaked.
- **2026-05-20 (Vasquez):** Fixed the `/devices` mobile filter drawer so it behaves like a real PWA sheet on iPhone-sized viewports. `src\TechInventory.Web\src\lib\components\DeviceFilters.svelte` now uses an `h-dvh` flex-column shell with a sticky close header, sticky footer actions, safe-area padding, body scroll lock, Escape close, focus trap, and an internal `min-h-0 flex-1 overflow-y-auto` content region; `src\TechInventory.Web\src\routes\(authenticated)\devices\+page.svelte` now wires the trigger with `aria-controls` / `aria-haspopup="dialog"`, and `src\TechInventory.Web\src\lib\components\DeviceFilters.test.ts` covers the sheet semantics. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, focused `pnpm exec vitest run src/lib/components/DeviceFilters.test.ts` ✅, `pnpm run build` ✅; full `pnpm exec vitest run` still fails on the unrelated existing `src\lib\components\DeviceTable.test.ts` mobile-card assertion.
- **2026-05-20 (Vasquez):** The redundant `/admin` hub is retired. `src\TechInventory.Web\src\lib\navigation\appNav.ts` now exposes Audit Log as the Admin-only top-level link (`/admin/audit`) while the existing `ADMIN` subsection still lists Brands/Categories/Locations/Networks/Owners/Tags; `src\TechInventory.Web\src\routes\(authenticated)\admin\+page.ts` keeps `/admin` alive as a redirect to `/admin/audit`, and nav coverage now lives in `appNav.render.test.ts` with axe assertions. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, focused nav/redirect Vitest ✅; full `pnpm exec vitest run` and `pnpm run build` remain blocked by unrelated pre-existing failures in the shared worktree.
- **2026-05-20 (Vasquez):** Follow-up cleanup for the shipped F039 admin bulk actions removed the redundant per-item `Merge` buttons from Brands, Categories, Locations, and Networks. The shared `Merge Selected` bulk bar flow stays intact; page-local single-source handlers/toasts were pruned, `common.actions.merge` / `admin.merge.success` were removed from `src\TechInventory.Web\src\lib\i18n\en.json`, and `src\TechInventory.Web\src\routes\(authenticated)\admin\lookup-actions.test.ts` now locks the row contract to Edit + Deactivate only. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, `pnpm exec vitest run` ✅, `pnpm run build` ✅.
- **2026-05-20 (Vasquez):** Backlog items `F040`–`F043` are complete on `/devices`: the add FAB is safe-area aware in PWA mode, list detail opens in `DeviceDetailModal.svelte` via `?device=`, device actions collapse behind `DeviceActionsMenu.svelte`, and the modal/direct route both reuse `DeviceDetailFields.svelte` for the horizontal key-value layout. Validation: `pnpm run lint` ✅, `pnpm run check` ✅, full `pnpm exec vitest run` ✅, `pnpm run build` ✅; repo `scripts\verify.ps1` still stops only when Playwright attempts to call unavailable `docker`.

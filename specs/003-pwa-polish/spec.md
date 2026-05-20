# Spec 003 — PWA Polish & Field-Test Fixes

**Status**: Draft
**Phase**: 2.5 (post-field-test, pre-Phase 3 hardening)
**Owner**: Ripley (Lead/Architect)
**Created**: 2026-05-20
**Origin**: Brian's field-test feedback (`.squad/decisions/inbox/copilot-directive-2026-05-20T162609Z-field-test-backlog.md`)

---

## 1. Overview

Brian field-tested the PWA on mobile (phone) and desktop and surfaced 11 issues. Item #1 ("devices hidden behind transparent element") is already fixed. This spec addresses the remaining 10 items, grouped into bugs, UX improvements, and new features.

This is a **standalone spec** (not an amendment to 002) because:
- Spec 002 has well-defined scope and completion tracking (33/53 done)
- These items are post-field-test refinements that form a coherent workstream
- The reporting feature (#11) is a material new capability deserving its own design
- Constitution §1: "Scope creep is the #1 enemy" — new work = new spec

---

## 2. Goals & Non-Goals

### 2.1 Goals

- **Fix field-test bugs**: Audit log visibility, tag functionality broken/inaccessible
- **Mobile UX**: Infinite scroll, pull-to-refresh, consistent hamburger nav
- **Admin UX**: Responsive management tables, audit log as modal
- **Theming**: Dark mode manual toggle
- **Data quality**: Merge duplicate reference entities (locations, brands, categories)
- **Reporting**: Insurance export report + reporting/analytics foundation

### 2.2 Non-Goals

- ❌ Offline mutation queuing (Phase 3+)
- ❌ Full analytics dashboard with charts (Phase 4 — but this spec lays API groundwork)
- ❌ Natural language queries (F019 backlog)
- ❌ Photo attachments (v1.5 per PRD)

---

## 3. Relationship to Spec 002

### Overlaps & De-duplication

| This Spec | Spec 002 | Resolution |
|-----------|----------|------------|
| Infinite scroll (P003-T01) | T15 pagination (✅ shipped) | **Supersedes** T15's pagination — keep pagination as fallback for a11y, add infinite scroll as default |
| Hamburger menu (P003-T05) | T13 app shell (✅ shipped) | **Amends** T13 — hamburger was partially done, this ensures consistency on ALL routes |
| Export report (P003-T10) | T38 Export page (unstarted) | **Extends** T38 — T38 does raw CSV/JSON; this adds insurance-formatted report |
| Dark mode (P003-T06) | Spec 002 §2.2 deferred to v1.1 | **Promotes** from non-goal — Brian explicitly requested it in field test |

### Spec 002 tasks that remain unchanged
- Rounds 7-8 (Import/Export) proceed as designed — P003 *extends* export, doesn't replace it
- Round 9 (Polish) T40-T44 proceed — complementary, not overlapping
- Round 10-11 proceed as designed

---

## 4. Priority Classification

| Priority | Meaning | Items |
|----------|---------|-------|
| **P0** | Bug — broken functionality users encountered | Audit log unviewable, Tags not testable |
| **P1** | UX — field-test friction, directly requested | Infinite scroll, pull-to-refresh, hamburger nav, management tables, audit log modal |
| **P2** | Feature — new capability | Dark mode, merge duplicates, insurance export, reporting spec |

---

## 5. Technical Design Notes

### 5.1 Infinite Scroll
- Use Intersection Observer API on a sentinel element
- Keep server-side pagination API unchanged (`?page=N&pageSize=M`)
- Client fetches next page when sentinel enters viewport
- Retain "jump to top" FAB for accessibility
- Fallback: `prefers-reduced-motion` users get traditional pagination

### 5.2 Pull-to-Refresh
- Use `overscroll-behavior-y: contain` + custom touch handler
- Trigger TanStack Query `invalidateQueries()` on pull threshold
- Visual indicator: spinner at top of viewport
- Only active on touch devices (media query `pointer: coarse`)

### 5.3 Dark Mode
- Extend existing CSS custom properties in `tokens.css` with dark variants
- Add `ThemeProvider` that reads OS preference + manual override from localStorage
- Toggle in settings page (or header utility area)
- Persist preference in localStorage (acceptable — not a secret)

### 5.4 Merge Duplicates (Backend Required)
- New endpoint: `POST /api/v1/{entity}/merge` — body: `{ sourceId, targetId }`
- Backend reassigns all FK references from source → target, then soft-deletes source
- AuditEvent logged with before/after
- UI: Admin page shows "Merge" button → modal with target selector

### 5.5 Insurance Export Report
- New endpoint: `GET /api/v1/reports/insurance` — returns formatted report data
- Includes: device name, brand, category, purchase date, purchase price, serial number
- Output formats: PDF (via server-side generation) or structured CSV with summary row
- Decision: Start with CSV+summary; PDF deferred to follow-up if needed (ADR pending)

### 5.6 Audit Log Fixes
- **Visibility bug**: Audit text using `--color-text` token that has insufficient contrast in current theme
- **Modal conversion**: Replace `/admin/audit` page route with modal overlay triggered from device detail + admin dashboard

---

## 6. Assumptions (Ripley's Judgment)

1. "Tags not testable" means the tag assignment UI on device create/edit is broken or hidden — not that the Tags admin page (T32, ✅) is broken
2. "Hamburger menu on all pages" means the existing mobile nav from T13 isn't rendering consistently (possibly missing on admin sub-routes)
3. "Better looking management pages" targets the Round 6 admin UIs (brands, categories, etc.) — tables need responsive treatment
4. Insurance export is CSV-first (PDF is over-engineering for v1)
5. Reporting (#11) gets a lightweight spec section here but full analytics dashboard remains Phase 4

---

## 7. Dependencies on Spec 002

- P003 Round A (bugs) can start immediately — no spec 002 dependencies
- P003 Round B (UX) depends on spec 002 Rounds 0-6 being shipped (they are ✅)
- P003 Round C (features) — merge duplicates needs backend work; export extends T38
- P003 Round D (reporting) — can proceed independently of spec 002

---

## 8. Open Questions

| # | Question | Default (if no answer) |
|---|----------|----------------------|
| Q1 | Should insurance report be PDF or CSV? | CSV with summary header — PDF deferred |
| Q2 | Should dark mode persist per-device or sync via API? | localStorage only (no backend sync) |
| Q3 | Should merge duplicates require Admin role? | Yes — Admin only |
| Q4 | Tags "not testable" — is this device-tag assignment or tag admin CRUD? | Device-tag assignment on create/edit form |

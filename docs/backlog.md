# Backlog — Future Features

Formal tracking of accepted features not yet scheduled. Features are prioritized and updated as implementation progresses.

---

## Reports & Analytics

Extends spec-003 T11/T12 reporting work.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F035 | Era/decade device reports | Cell phones owned over the eras/decades; Laptops over the decades. `/reports` now includes the first era report card backed by `/api/v1/reports/eras` with category filtering; broader nostalgic report set still remains. | P3 | In Progress |
| F036 | Achievement reports | "Most brands used", "oldest device still active", collection milestones | P3 | Backlog |
| F037 | Historical tech timeline | `/reports` now includes a Tech Timeline card backed by `/api/v1/reports/timeline`, with category filtering, category/owner grouping, and active vs disposed lifespan bars. | P3 | In Progress |

---

## Authentication & Session

Extensions to OIDC/MSAL integration.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F038 | Silent SSO auto-login | MSAL `acquireTokenSilent` on app load; skip login page if valid session | P2 | In Progress |

---

## Admin Bulk Operations

Extends the multi-select pattern from Devices to all reference-data admin pages.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F039 | Multi-select for Brands/Categories/Locations/Networks | Add checkbox multi-select to Brands, Categories, Locations, and Networks admin pages (matching Devices pattern). When multiple items selected, offer: (1) bulk delete, (2) merge into one — user picks the merge target, all references are reassigned. | P2 | Backlog |

---

## PWA Mobile UX

Fixes and improvements for the progressive web app (phone-first) experience.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F040 | Restore (+) FAB for add device | The floating action button to add a new device is missing in PWA mode — restore it as a visible, always-accessible FAB on the device list view. | P1 | Backlog |
| F041 | Device details as modal | Device details should open as a slide-up modal/bottom sheet instead of navigating to a new page. Keeps context and feels native on mobile. | P2 | Backlog |
| F042 | Hamburger menu for device actions | The Options menu (Edit, Release Ownership, View Change History, etc.) should collapse into a hamburger/kebab overflow menu in PWA mode instead of exposed buttons. | P2 | Backlog |
| F043 | Device details table layout | Device details should render as a horizontal key-value table (`Column Name \| Property Value`) instead of stacking the label above the value. Cleaner, more scannable. | P2 | Backlog |
| F044 | Admin column order settings | Give admins the ability to configure the display order of columns on the device list/details views from an admin settings screen. Persisted per-household. | P3 | Backlog |

---

## Future Work (Unscheduled)

The following items are candidates for future scheduling but lack committed owners or timelines:

- **Custom icon set** — If app scales to 50+ unique icons, consider a dedicated icon component library (e.g., `<Icon name="hamburger" />`). Currently inline SVG in layout components is the right abstraction.
- **High-contrast mode variant** — WCAG AAA requires 7:1 contrast; current neutral-700/neutral-300 is ~4.6:1 (AA compliant). A future accessibility pass could add `forced-colors: active` media query support for Windows High Contrast Mode.
- **Animated hamburger→X morph** — Visually richer but adds 20+ lines of SVG/CSS. Current toggle (swap paths) is simpler and meets PRD "tasteful, never trendy."
- **Backend tree-aware category search** — Deferred; client-side filter sufficient for v1 (<100 categories expected).
- **Fuzzy matching for search** — Deferred; exact substring match covers 95% of use cases.
- **Persistent desktop rail** — Rejected for now; single-household app, phone-first use case. Consistency over desktop density.

---

**Last updated:** 2026-05-20T20:14Z by Squad

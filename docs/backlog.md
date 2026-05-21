# Backlog — Future Features

Formal tracking of accepted features not yet scheduled. Features are prioritized and updated as implementation progresses.

---

## Reports & Analytics

Extends spec-003 T11/T12 reporting work.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F035 | Era/decade device reports | Cell phones owned over the eras/decades; Laptops over the decades. `/reports` includes the era report card backed by `/api/v1/reports/eras` with category filtering. | P3 | Done |
| F036 | Achievement reports | "Most brands used", "oldest device still active", collection milestones | P3 | Closed — not required |
| F037 | Historical tech timeline | `/reports` includes a Tech Timeline card backed by `/api/v1/reports/timeline`, with category filtering, category/owner grouping, and active vs disposed lifespan bars. | P3 | Done |

---

## Authentication & Session

Extensions to OIDC/MSAL integration.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F038 | Silent SSO auto-login | Root auth bootstrap now restores cached Entra sessions before showing `/auth/login`, falls back to sign-in after a 3-second silent-SSO timeout, and keeps logout / second-tab behavior consistent without storing tokens outside MSAL sessionStorage. | P2 | Done |

---

## Admin Bulk Operations

Extends the multi-select pattern from Devices to all reference-data admin pages.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F039 | Multi-select for Brands/Categories/Locations/Networks | Add checkbox multi-select to Brands, Categories, Locations, and Networks admin pages (matching Devices pattern). When multiple items selected, offer: (1) bulk delete, (2) merge into one — user picks the merge target, all references are reassigned. | P2 | Done |

---

## PWA Mobile UX

Fixes and improvements for the progressive web app (phone-first) experience.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F040 | Restore (+) FAB for add device | The floating action button to add a new device is missing in PWA mode — restore it as a visible, always-accessible FAB on the device list view. The shared `AddDeviceFab` now stays fixed above the safe area so the add affordance survives installed-PWA chrome. | P1 | Done |
| F041 | Device details as modal | Device details now open as a slide-up bottom sheet on mobile and a centered modal on desktop, keeping list context while preserving Escape/backdrop dismiss and focus trapping. | P2 | Done |
| F042 | Hamburger menu for device actions | Device edit / claim / release / history / delete actions now live behind a shared kebab overflow menu with a mobile action sheet and desktop dropdown. | P2 | Done |
| F043 | Device details table layout | Device details now render through a horizontal key-value table component, replacing the old stacked label/value layout for better scanability. | P2 | Done |
| F044 | Admin column order settings | Backend API persists per-household device list/detail display order at `/api/v1/settings/display`. Frontend admin screen not pursued. | P3 | Closed — not required |

---

## Future Work (Unscheduled)

**Closed — not pursued post-v1.0.** The following candidates were evaluated and deferred indefinitely. They remain here as historical record of decisions made.

- **Custom icon set** — Inline SVG in layout components is the right abstraction at current scale.
- **High-contrast mode variant** — Current neutral-700/neutral-300 is AA compliant (~4.6:1). WCAG AAA / Windows High Contrast Mode not pursued.
- **Animated hamburger→X morph** — Current toggle (swap paths) meets PRD "tasteful, never trendy."
- **Backend tree-aware category search** — Client-side filter sufficient (<100 categories).
- **Fuzzy matching for search** — Exact substring match covers 95% of use cases.
- **Persistent desktop rail** — Phone-first single-household app; consistency over desktop density.

---

**Last updated:** 2026-05-21 by Brian — v1.0 shipped. F038 is the only remaining active backlog item; Future Work parking lot closed (not pursued).

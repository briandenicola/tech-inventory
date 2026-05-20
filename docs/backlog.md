# Backlog — Future Features

Formal tracking of accepted features not yet scheduled. Features are prioritized and updated as implementation progresses.

---

## Reports & Analytics

Extends spec-003 T11/T12 reporting work.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F035 | Era/decade device reports | Cell phones owned over the eras/decades; Laptops over the decades. `/reports` now includes the first era report card backed by `/api/v1/reports/eras` with category filtering; broader nostalgic report set still remains. | P3 | In Progress |
| F036 | Achievement reports | "Most brands used", "oldest device still active", collection milestones | P3 | Backlog |
| F037 | Historical tech timeline | Visual timeline of devices owned over time | P3 | Backlog |

---

## Authentication & Session

Extensions to OIDC/MSAL integration.

| ID | Feature | Description | Priority | Status |
|----|---------|-------------|----------|--------|
| F038 | Silent SSO auto-login | MSAL `acquireTokenSilent` on app load; skip login page if valid session | P2 | In Progress |

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

**Last updated:** 2026-05-20T14:39Z by Hicks

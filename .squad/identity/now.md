---
updated_at: 2026-05-19T20:50:00Z
focus_area: P4 — Continuous Iteration (post-MVP, backlog-driven). PWA field-test (2026-05-19) produced F026–F033; F026 is the recommended next pull.
active_issues: []
---

# What We're Focused On

The product is **production-validated** at `https://inventory.denicolafamily.com`
as of 2026-05-19. P0 (Foundation), P1 (Core API), P2 (Frontend MVP + Auth),
and P3 (Production Launch) all shipped. The phase model was rewritten on
2026-05-19 to match what actually happened — see `docs/prd.md` §13.

We are now in **P4 — Continuous Iteration**: work pulls from
`specs/_backlog/` (F-numbered entries). No further linear phases planned.

**Latest input — Brian's 2026-05-19 PWA field-test** produced eight new
backlog entries (F026–F033). Recommended sequencing (per session plan in
`~/.copilot/session-state/`):

1. **F026** — PWA quick-win UX pack (transparent-overlay bug, `+` FAB,
   Active-default filter, 2-up cards, audit modal, pinch-zoom off)
2. **F030** — Device tagging bug repro + fix
3. **F029** — Dark-mode toggle + audit-log contrast repair
4. **F028** — Infinite scroll + pull-to-refresh
5. **F027** — Global hamburger nav + responsive admin pages
6. **F031** — Merge duplicate reference data (Brands/Categories/Locations)
7. **F032** — Insurance-ready PDF report (first concrete Phase-4 reporting
   entry)
8. **F033** — Device images + room capture (umbrella, pairs with F018)

Background pulls (no new urgency): F018, F019, F020b, F021b, F024b, F025b.

Updated by coordinator at session start.
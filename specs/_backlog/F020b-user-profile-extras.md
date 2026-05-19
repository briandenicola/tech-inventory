# F020b: User Profile — Avatar, Preferences, Session Tab

**Status**: backlog
**Priority**: P3
**Effort**: M
**Value**: medium
**Risk**: low
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: unassigned
**Carved out from**: F020 (which shipped v1 with the Profile/display-name slice only).

## Problem
F020 v1 shipped the bare Profile tab (display name editable for self). The
larger F020 scope — avatar upload, personal preferences (dark-mode override,
default landing page, density), and a session tab — still has open design
questions and additional backend work (Attachment plumbing or interim base64
column, `UserPreference` table, etc.). That work is deferred so v1 could ship
without blocking on those decisions.

## Proposed Solution
Round out `/settings` with the remaining tabs and storage:
- **Avatar** upload (drag-and-drop, 5MB cap, square crop preview).
- **Preferences**: dark-mode override (D-137 follow-up), list density,
  default landing page.
- **Session tab**: last sign-in, active sessions list (informational only).

## Acceptance Criteria
- [ ] Avatar persists and renders in the top-right user menu + audit history.
- [ ] Preferences persist server-side so they follow the user across devices.
- [ ] Session tab renders last sign-in time + UA from the most recent
      auth event.
- [ ] Zero axe-core violations; 44px touch targets.
- [ ] Playwright happy-path covers each tab.

## Open Questions
- Avatar storage: Attachment table (preferred long-term) or interim base64
  column on `Owner`?
- Should preferences live in a new `UserPreference(key, value)` table or
  a JSON column on `Owner`?
- Audit policy: should preference changes be audited? (Lean: no — they're
  user-private cosmetics, not data integrity.)

## Out of Scope
- Notification preferences (no notification plumbing yet).
- Admin-managed bulk user import (F8 Admin Console territory).
- Multi-tenant / multi-household identity switching.

## Dependencies
- F020 v1 shipped — `/settings` page exists; just needs more sections.

## History
- 2026-05-19: carved out of F020 at v1 ship time so the display-name slice
  could ship without blocking on storage decisions.

# F020: User Profile & Personal Settings

**Status**: shipped (v1 — Profile/display-name only). Avatar / preferences / session tab carved out to F020b.
**Priority**: P2
**Effort**: M
**Value**: medium
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: unassigned

## Problem
The current app surfaces no per-user settings UI. PRD `U19` already promises
that *"As Member, I can upload a profile image for my account."* — but there is
no `/profile` or `/settings` route, and the only way to change a display name
or role is to edit the `Owner` row through the Admin CRUD or the Entra portal.

As more users come online and as we expand UI personalization (saved views,
density, dark-mode toggle per D-137, notification preferences, etc.), the
absence of a self-service settings surface forces every preference to be a
backend admin task. This will not scale even at household size.

## Proposed Solution
Add a `/settings` route under the authenticated layout that lets the **current
user** manage their own profile and personal preferences. The data model
already exists (`Owner.DisplayName`, an Entra-supplied object id, role); v1.1
adds an avatar URL or local attachment plus user-preference key/value rows.

Initial scope (v1.1):
1. **Profile tab**: display name (editable for self), email/oid (read-only,
   from Entra), role (read-only, badge), avatar upload (drag-and-drop, 5MB cap,
   square crop preview, stored via existing Attachment plumbing once it
   lands, or a base64 column in the interim).
2. **Preferences tab**: dark-mode override (D-137 follow-up), list density
   (compact / comfortable), default landing page (`/devices` vs `/dashboard`
   once dashboards land).
3. **Session tab**: last sign-in, active sessions list (informational only —
   real session revocation lives in Entra).

The nav drops an avatar circle in the top-right of the authenticated layout
header; clicking it opens a dropdown with **Settings**, **Sign out**, and
(for Admins only) **Admin Console**. The current Admin menu becomes a sibling
nav item that the user-menu dropdown links to, not a primary nav pill.

## User Stories
- **U19** (existing PRD): *As Member, I can upload a profile image for my
  account.*
- *As any authenticated user, I can change my display name so the audit log
  attributes my actions with a recognizable name.*
- *As any authenticated user, I can set my dark-mode preference independent of
  my OS so the app matches my visual preference in mixed environments.*
- *As Admin, I can override any user's display name from the existing
  `/admin/owners` page (no change — already supported).*

## Acceptance Criteria
- [ ] `/settings` route exists under the authenticated layout
- [ ] Avatar upload persists and renders in the top-right user menu + in any
      audit history rows attributed to that user
- [ ] Display name edits update the local `Owner` row and propagate to the
      auth store without a full reload
- [ ] Preferences persist server-side (new `UserPreference` table or JSON
      column on `Owner`) so they follow the user across devices
- [ ] All controls satisfy the 44px touch-target token + zero axe-core
      violations
- [ ] i18n keys live under `settings.*` in `en.json`
- [ ] At least one Playwright E2E covers: edit display name → reload → name
      persists in header + in audit log

## Out of Scope
- Admin-managed bulk user import (Admin Console F8 territory)
- Notification preferences (no notification plumbing exists yet)
- Multi-tenant / multi-household identity switching
- Two-factor / passkey enrollment (Entra owns this entirely)

## Dependencies
- Avatar upload requires either the Attachment entity (PRD §8, v2) or an
  interim base64 column on `Owner`. Decision needed before implementation.
- Dark-mode override depends on D-137 manual toggle (currently OS-only per
  `spec.md:147`).

## Open Questions
- Where does the avatar binary live? Attachment table (preferred long-term) or
  inline column (faster for v1.1)?
- Should display-name edits be audited as `OwnerSelfUpdated` distinct from
  Admin-driven `OwnerUpdated`?
- Do we expose the user-preference API to third parties, or keep it
  intentionally first-party-only and undocumented in OpenAPI?

## Notes / Research
- Auth store already carries `displayName` + an `avatar URL` slot per
  `specs/002-frontend-mvp/spec.md:132`. Wiring is in place; surface is missing.
- `Owner.IsActive` soft-delete already covers the "deactivate user" flow;
  avatar reset can piggyback on the same admin path.

## History
- 2026-05-19: created — captured during Add Device modal session as a follow-up
  to D-137 admin menu work
- 2026-05-19: shipped v1 — Profile tab (display name only).
  Backend added `PATCH /api/v1/owners/me` backed by `UpdateMyProfileCommand`
  (looks up the owner row via the current user's Entra object id, validates
  display name length + uniqueness, emits a standard `Owner Updated` audit
  row via the existing pipeline). Frontend added `/settings` page with
  read-only role + Entra OID, a toast on save, and an `updateCurrentUserDisplayName`
  store helper so the header chip refreshes without a reload. Settings link
  added to the user-menu dropdown (desktop + mobile). Avatar upload,
  preferences, and session tab carved out to **F020b**.

# F022: User Default Sort & Filter Preferences

**Status**: shipped (v1.0 — localStorage); server-sync follow-up tracked
**Priority**: P3
**Effort**: S
**Value**: medium
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-20
**Owner**: unassigned

## Problem
`/devices` already supports a rich set of URL-backed query parameters — `search`,
`brandId`, `categoryId`, `ownerId`, `locationId`, `networkId`, `status`,
`yearMin`, `yearMax`, `sort`, and `sortDir`. But every navigation back to the
list starts from the defaults: status=Active, sort=updatedAt desc, no filters.

Power users have a strong preferred view ("show me everything I own, sorted by
purchase date desc, grouped by location") and have to re-apply it after every
sign-in or back-nav. Bookmarks help but break when the user wants the same
defaults from the global nav.

## Proposed Solution
Persist per-user default query-string snapshots for the `/devices` route (and
any other list views once they land — `/admin/brands`, `/admin/locations`, …).

Initial scope:
1. **Save current view as default**: a "Save as default" action in the
   `/devices` filter toolbar that captures the current URL query string and
   stores it on the user's preferences row.
2. **Auto-apply on entry**: when the user navigates to `/devices` with no
   query string, the stored default is applied (via a client-side redirect or
   in-load hydration of the URL).
3. **Reset to system defaults**: a "Clear my default" action removes the
   stored preference.
4. **Named views (stretch)**: lets the user save multiple named filter sets
   ("My Apple gear", "Kitchen appliances") that appear as a dropdown next to
   the filter bar. Out of scope for v1.1 unless trivial after the storage
   piece lands.

## User Stories
- *As any authenticated user, I can save my current filter + sort combination
  as my default `/devices` view so the app remembers it across sessions and
  devices.*
- *As any authenticated user, I can reset my default back to the system
  default without having to re-edit a URL.*

## Acceptance Criteria
- [ ] Filter toolbar exposes a "Save as default" affordance, disabled when the
      current view already matches the stored default.
- [ ] Defaults persist server-side keyed by user id (likely the same
      `UserPreference` table introduced for F020).
- [ ] Default auto-applies only when the route is entered with **no** query
      string — explicit URLs (bookmarks, audit-log deep links, etc.) always
      win.
- [ ] At least one Vitest unit covering the merge logic and one Playwright
      E2E: set default → sign out → sign back in → defaults applied.
- [ ] Zero axe-core violations on the toolbar additions.

## Out of Scope
- Sharing saved views across users (single-household, low value)
- Server-side rendered defaults (current SvelteKit hydration is fine)
- Per-column show/hide preferences (separate F-item if requested)

## Dependencies
- Storage shape needs the `UserPreference` table from F020. Implement F020
  first, or co-design the table so this can land alongside.
- Filter state shape is currently defined in
  `src/TechInventory.Web/src/routes/(authenticated)/devices/+page.svelte` —
  factor into a typed schema (Zod) before persisting so changes don't
  silently corrupt stored defaults.

## Open Questions
- Should `status=Inactive` views be allowed as a default, or do we clamp to
  Active to prevent users from accidentally hiding their own devices?
- Do we version the stored snapshot to handle future schema changes (e.g.,
  new filter dimensions)?

## Notes / Research
- All filter state already round-trips through the URL — implementation is
  mostly UI + storage glue, no new query logic needed.
- Captured during R10 smoke session 2026-05-20 after the duplicate
  Import/Export nav cleanup.

## History
- 2026-05-20: created — captured during R10 smoke session as a v1.1 polish
  follow-up to the filter toolbar
- 2026-05-19: shipped v1.0 as localStorage-only (per-user oid key,
  schema-versioned via Zod, SSR-safe). Cross-device sync deferred to F020's
  `UserPreference` table. New module `src/lib/stores/userPrefs.ts`,
  toolbar additions in `DeviceFilters.svelte`, auto-apply on bare-URL entry
  in `/devices/+page.svelte`. 16 new Vitest cases.

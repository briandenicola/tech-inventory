# F023: Group Devices By Category, Owner, or Purchase Year

**Status**: shipped (v1)
**Priority**: P3
**Effort**: M
**Value**: medium
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: unassigned

## Problem
`/devices` today shows a flat, sorted list. As the inventory grows past a few
dozen rows it gets hard to answer questions like "which devices are in the
Kitchen?", "what did we buy in 2024?", or "what does Brian own?" without
applying a filter and losing the broader context.

A simple grouping mode lets the user keep the full list visible but
collapsed/expanded by a single dimension, which is faster than repeatedly
toggling filters.

## Proposed Solution
Add a **"Group by"** dropdown to the device list toolbar with options:
- *None* (default — current flat list)
- *Category*
- *Owner*
- *Purchase year*

When a grouping is active:
1. Rows are partitioned by the chosen dimension and rendered under a sticky
   group header that shows the group label + a count badge.
2. Each group is independently collapsible (Apple-style chevron). State is
   ephemeral — collapse/expand is not URL-persisted.
3. The current sort still applies *within* each group.
4. Pagination switches to "all rows for the matching filter, no per-page
   limit" while a grouping is active, OR groups are pre-paged client-side —
   pick whichever performs better with ~500 rows.
5. Group choice IS URL-persisted (`?groupBy=category`) so the view is
   shareable and survives reloads.

## User Stories
- *As any authenticated user, I can group the device list by Category to scan
  what we have in each bucket without losing the full inventory view.*
- *As any authenticated user, I can group by Owner to see who owns what
  across the household.*
- *As any authenticated user, I can group by Purchase year to spot
  age clusters when planning replacements.*

## Acceptance Criteria
- [ ] Toolbar exposes a "Group by" dropdown with the four options above; the
      choice round-trips through `?groupBy=` on the URL.
- [ ] Group headers show the group label and the count of devices in that
      group; clicking a header toggles collapse/expand.
- [ ] Current sort + filter behavior is preserved inside each group.
- [ ] "Purchase year" buckets devices by calendar year of `purchaseDate`;
      devices with no purchase date land in a single "Unknown" group at the
      bottom.
- [ ] At least one Vitest unit for the grouping/sorting logic and one
      Playwright E2E covering "group by Owner → expand one group → click a
      device → modal opens with the right device".
- [ ] Zero axe-core violations on the new toolbar + headers.

## Out of Scope
- Multi-level grouping (group by Category *then* Owner)
- Group-level bulk actions (covered by F024)
- Server-side rendered grouping (do it client-side after fetch)
- New grouping dimensions beyond Category/Owner/Purchase year (Network,
  Location, Status, etc. can be added later via the same UI scaffold)

## Dependencies
- Reference-data store already exposes brand/category/owner/location names
  — reuse for header labels.
- Sort/filter URL plumbing in `+page.svelte` is already typed; extend it
  with the `groupBy` field rather than introducing a parallel state layer.

## Open Questions
- Should the "None" option be hidden when an explicit group is active, with
  a "Clear grouping" affordance instead?
- Do we want a "show empty groups" toggle (e.g., to see categories with
  zero devices)? Probably no for v1, yes for admin-y views.

## Notes / Research
- Captured during R10/R11 testing 2026-05-19 alongside F024 (bulk
  selection) — both are list-view ergonomics that scale with inventory
  size.

## History
- 2026-05-19: created — captured during R10/R11 testing as a v1.1 list-view
  ergonomics improvement
- 2026-05-19: shipped v1 — frontend-only. Added Group By dropdown to the
  filters sidebar (None / Category / Owner / Purchase year). State is
  URL-persisted via `?groupBy=`; sorting still applies inside each bucket;
  ephemeral collapse state per group. When grouping is active the page
  switches to a 500-row fetch so groups span the full filter result set,
  and pagination controls are hidden. `groupDevices` helper extracted to
  `src/lib/utils/groupDevices.ts` with 7 vitest cases covering partition,
  sort order (year desc, alpha otherwise), and the Unknown (no
  purchaseDate) bucket. Multi-level grouping and group-level bulk actions
  remain explicitly out of scope.

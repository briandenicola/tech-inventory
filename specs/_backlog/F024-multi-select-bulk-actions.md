# F024: Multi-Select Devices For Bulk Actions

**Status**: shipped (v1)
**Priority**: P2
**Effort**: L
**Value**: high
**Risk**: medium
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: unassigned

## Problem
After importing a CSV, reorganizing households, or correcting a mass
mis-classification (e.g., every "Kitchen Appliance" should really be
"Appliance"), the user has to open each device individually, edit one field,
and save. This scales poorly: a 50-device cleanup is 50 modal round-trips.

A multi-select + bulk action flow lets the user say "these 20 rows → change
their category to X" or "these 10 rows → delete with reason 'duplicates from
SharePoint export'" in a single confirmed operation.

## Proposed Solution
Add row selection + a contextual bulk-action bar to `/devices`.

### Selection UX
1. A checkbox column appears at the start of each row (desktop) and on each
   card (mobile).
2. A header checkbox toggles "select all visible" (current page only, not
   the entire filtered set).
3. Shift-click extends the selection across a range (desktop power-user
   nicety; optional for v1).
4. A "Select all N matching rows" link appears in the action bar when the
   header checkbox is checked, to expand selection beyond the current
   page.
5. Selection state is in-memory only; it clears on navigation away from
   `/devices` or when filters change.

### Bulk action bar
When ≥1 row is selected, a sticky bottom bar appears with:
- "**N selected**" count + "Clear" link
- **Change Category** → opens a modal with a category picker and a confirm
  CTA that applies the new value to every selected row server-side
- **Change Owner** → same pattern, owner picker
- **Change Brand** → same pattern, brand picker (optional, since some
  devices legitimately have no brand)
- **Change Location** → location picker
- **Change Status** → enum picker (Active / Retired / Disposed / InRepair
  / Lent)
- **Delete…** → opens a multi-device delete modal with a single shared
  "reason" textarea and a type-to-confirm count ("type 7 to confirm
  deleting 7 devices")

### Backend
- New API endpoint per bulk operation, e.g.:
  - `PATCH /api/v1/devices/bulk` with body
    `{ deviceIds: string[], changes: { categoryId?, ownerId?, brandId?, locationId?, status? } }`
  - `DELETE /api/v1/devices/bulk` with body
    `{ deviceIds: string[], reason: string }`
- All bulk operations are transactional per request (all-or-nothing) and
  emit one `AuditEvent` per affected device so the audit log stays
  per-entity.
- Authorization mirrors single-item rules: Admin for bulk delete; Admin or
  Member for bulk field updates.

## User Stories
- *As an Admin, I can select multiple devices and change their category in
  one operation so I don't have to edit each one individually.*
- *As an Admin, I can bulk-delete devices with a single shared reason so
  cleaning up obvious duplicates is fast.*
- *As any authenticated user, my selection clears when I navigate away so
  I never accidentally apply a bulk change with stale state.*

## Acceptance Criteria
- [ ] Checkbox column + header "select all visible" on desktop; per-card
      checkbox on mobile.
- [ ] Bulk action bar appears only when ≥1 row is selected and shows the
      live count.
- [ ] Bulk-update endpoints accept up to 500 ids per request and complete
      atomically (rejecting the batch if any single update fails).
- [ ] Each bulk operation writes one `AuditEvent` per affected device with
      a shared correlation id linking them.
- [ ] Bulk delete requires type-to-confirm matching the device count and a
      reason ≥10 chars (same threshold as single delete).
- [ ] At least two Vitest units (selection state, bar-visibility) and one
      Playwright E2E ("select 3 of 5 → bulk-set category → verify all 3
      updated, other 2 unchanged").
- [ ] Zero axe-core violations on the selection column, header checkbox,
      and bulk-action bar.

## Out of Scope
- Cross-page persistent selection (within one filter set is enough for v1)
- Undo for bulk operations (a database-level rollback isn't worth the
  complexity; rely on audit log + manual revert for now)
- Bulk import/CSV-driven edits (the import flow already covers that)
- Bulk operations on reference entities (brands, categories, etc.) —
  separate F-item if needed
- Bulk owner reassignment via ownership transfer flow (use the existing
  per-device claim/release for sensitive transfers)

## Dependencies
- API needs new `IDeviceRepository.BulkUpdateAsync` + `BulkDeleteAsync`
  methods that wrap the existing per-device ops in a single transaction
  via the unit of work.
- AuditBehavior already serializes BEFORE/AFTER per request — the bulk
  handler will need to emit per-device audit entries explicitly rather
  than relying on the pipeline behavior.
- F023 (grouping) can land independently but the two interact: bulk
  selection inside a collapsed group should be possible (group-level
  "select all in group" is a nice stretch goal).

## Open Questions
- Hard limit on bulk batch size? 500 feels safe for SQLite + EF Core but
  worth measuring before shipping.
- Should partial-failure mode be available (best-effort, return per-row
  results) in addition to all-or-nothing? Probably no for v1 — atomic is
  easier to reason about.
- Does "Change Owner" need to honor the ownership-transfer audit semantics
  (Claim/Release) or is a direct write fine for an Admin bulk operation?

## Notes / Research
- Captured during R10/R11 testing 2026-05-19 alongside F023 (grouping) —
  both target the "I have a real-world inventory now, the basic flows
  don't scale" pain point that surfaces post-import.
- Single-device delete already has type-to-confirm + reason validation
  (`DeleteDeviceModal.svelte`) — reuse the same component pattern for the
  bulk variant.

## History
- 2026-05-19: created — captured during R10/R11 testing as a v1.1
  list-view ergonomics improvement
- 2026-05-19: shipped (v1) — backend (`POST /api/v1/devices/bulk/update`,
  `POST /api/v1/devices/bulk/delete`), DeviceTable selection column +
  header tri-state checkbox + per-card mobile checkbox,
  `BulkActionBar.svelte`, `BulkUpdateModal.svelte`, `BulkDeleteModal.svelte`
  wired on `/devices`. Notable deviations vs. spec:
  - **POST instead of PATCH/DELETE-with-body** to sidestep client/proxy
    DELETE-body stripping and to keep one consistent verb across the pair.
  - **Correlation id without schema change** — bulk audit payloads wrap the
    snapshot in `BulkAuditEnvelope(correlationId, payload)` so the
    append-only `AuditEvent` table stays as-is.
  - **Atomic via unit of work** — handler mutates in memory + calls
    `SaveChangesAsync` once at the end; any failure aborts the whole batch.
  - **Hard cap: 500 ids/request** (FluentValidation `InclusiveBetween(1, 500)`).
  - **Bulk delete is Admin-only**; bulk update permits any authenticated user
    (matches single-device rules).
  - Carved **F024b** for: Playwright E2E coverage, shift-click range select,
    select-all-N-matching across pages, undo, group-level "select all in
    group", and bulk owner-change via Claim/Release semantics.

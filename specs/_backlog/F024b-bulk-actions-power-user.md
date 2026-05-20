# F024b: Multi-Select Bulk Actions — Power-User Polish

**Status**: backlog
**Priority**: P3
**Effort**: M
**Value**: medium
**Risk**: low
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: unassigned
**Carved out from**: F024 (which shipped v1 with checkbox-select + 5 field-change modals + bulk delete + atomic backend).

## Problem
F024 v1 shipped the meat of bulk actions (selection UX, 5 field-change modals,
bulk delete with type-to-confirm + reason, atomic backend with per-device
audit + shared correlation id). What's still missing are the power-user
niceties and one safety net:

- **Playwright E2E** — the v1 ship has integration + Vitest coverage but
  no end-to-end "click 3, change category, verify in DB" walkthrough.
- **Shift-click range select** — handy for "all 20 between this row and
  that row" on desktop.
- **"Select all N matching rows"** — when the header checkbox is checked
  on a paginated set, an inline link to expand selection to the full
  filtered result set (instead of just the current page).
- **Undo** — at least a soft-undo window (within the AuditEvent shared
  correlation id) that re-applies the BEFORE snapshots. Currently the only
  way back is per-device edit.
- **Group-level "select all in group"** — when F023 grouping is active,
  one checkbox per group header that toggles every device in that group.
- **Bulk owner-change via Claim/Release semantics** — v1 does a direct
  write to `OwnerId` for speed/simplicity. If we want bulk transfers to
  appear in ownership history with the same nuance as single-device
  transfers, this needs to route through the ownership-transfer pipeline.

## Acceptance Criteria
- [ ] Playwright spec: "select 3 of 5 → bulk-set category → verify all 3
      updated, other 2 unchanged, AuditEvent count = 3 sharing one
      correlation id."
- [ ] Shift-click on a row checkbox selects the range from the
      last-anchored checkbox to the clicked one (desktop only).
- [ ] When `selectAllVisible` is on AND there are more results in the
      filtered set than the current page, show a "Select all N matching"
      action that expands selection to every id in the active query.
- [ ] Undo control surfaces in the success toast for ~10s after a bulk
      operation completes; clicking it issues a compensating bulk
      operation derived from the BEFORE snapshots stored in the audit
      payloads.
- [ ] When F023 grouping is active, group headers render a checkbox that
      toggles every device in the group (tri-state with same semantics
      as the global header checkbox).
- [ ] Decision recorded (ADR or comment) on whether bulk owner-change
      stays as a direct write or routes through Claim/Release.

## Dependencies
- F024 (shipped) — selection state + endpoints + modals.
- F023 (shipped) — grouping; group-level select-all only makes sense once
  grouping is in place.

## Notes / Research
- The 500-id-per-request cap is a guess. Measure with a >100-device
  household before raising; SQLite + EF Core may want batching at higher
  counts.
- Undo from audit BEFORE snapshots is straightforward for the 5 field
  updates (each AfterPayload's `BulkAuditEnvelope.Payload` includes the
  pre-mutation snapshot via BEFORE). Bulk delete undo would require a
  "revive" capability we don't expose today.

## History
- 2026-05-19: created — carved out from F024 v1 ship.

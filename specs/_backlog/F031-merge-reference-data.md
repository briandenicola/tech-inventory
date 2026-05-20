# F031: Merge Duplicate Reference Data (Brands, Categories, Locations)

**Status**: backlog
**Priority**: P2
**Effort**: M
**Value**: high
**Risk**: medium
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: hicks

## Problem
Reference data (Brands, Categories, Locations) accumulates duplicates over
time — typos, alternate spellings ("Sonos" vs. "SONOS"), historical labels
("Master Bedroom" vs. "Primary Bedroom" after a remodel), or import
artifacts from the SharePoint CSV. There is currently no way to merge two
options into one without manually re-pointing every device row in SQLite.

Brian's note from the 2026-05-19 field-test: "Locations & Brand & Category —
Give the user the ability to merge similar or duplicate options."

## Proposed Solution
Add a generic **merge** operation that re-points all referencing rows from
a *loser* entity to a *winner* entity, then soft-deletes the loser. The
operation is admin-only, fully audited, and irreversible (the loser is
soft-deleted, not hard-deleted — recovery requires manual SQL).

### Domain / Application
- New command per entity type: `MergeBrandsCommand(winnerId, loserId)`,
  `MergeCategoriesCommand`, `MergeLocationsCommand` (and, optionally,
  `MergeOwnersCommand`, `MergeNetworksCommand`, `MergeTagsCommand`).
- Each command:
  1. Validates both ids exist and `winnerId != loserId`.
  2. Re-points referencing rows (`Device.BrandId`, etc.) via a single EF
     `ExecuteUpdateAsync` (parameterized — no raw SQL).
  3. Soft-deletes the loser (`Status = Inactive`).
  4. Appends an `AuditEvent` row capturing
     `BeforePayload = { winner, loser, referenceCount }`,
     `AfterPayload = { winner, loserDeactivated: true }`,
     `Action = "Merge"`, on both the winner and the loser entity ids
     (one event per id so either entity's audit history shows the merge).
- Implements `IAuditable` per the audit convention.
- Returns `Result<MergeOutcomeDto>` with the count of re-pointed devices.

### Domain rule: hierarchy
For **Category** (which is hierarchical per PRD), the merge re-points child
categories as well: any category whose `ParentId == loserId` becomes a
child of `winnerId`. Validation rejects merges that would create a cycle.

### API
- `POST /api/v1/brands/merge` (and parallels), body `{ winnerId, loserId }`,
  Admin policy, returns 200 with the outcome DTO or ProblemDetails on
  validation failure.

### Frontend
- On each reference-data management page (after F027), add a "Merge…"
  action in the row overflow menu. Opens a dialog:
  - "Merge **Loser Name** into **…**" — searchable picker for the winner.
  - Preview: "This will move 14 devices from *Loser* to *Winner* and
    deactivate *Loser*. This action is irreversible."
  - Confirm requires typing the loser's name (destructive-action pattern).
- Toast on success with undo-hint that points to the audit log.

## User Stories
- *As Admin, I merge "SONOS" into "Sonos" and every device's brand pointer
  follows automatically.*
- *As Admin, I merge "Master Bedroom" into "Primary Bedroom" after our
  remodel and the device list updates immediately.*
- *As Admin, I can see in the audit log who merged what, when, and how
  many devices were affected.*

## Acceptance Criteria
- [ ] Domain command + handler for Brand, Category, Location implemented
      with `Result<T>` semantics and `IAuditable`
- [ ] Category merge correctly re-parents children and rejects cycle
- [ ] Re-pointing uses parameterized EF (no raw SQL)
- [ ] Loser is soft-deleted, never hard-deleted
- [ ] `AuditEvent` rows appended for both winner and loser ids
- [ ] `POST /api/v1/{entity}/merge` endpoint behind `Admin` policy with
      OpenAPI contract; ProblemDetails on validation failure
- [ ] Member/Viewer roles receive 403 (contract test mirrors existing
      Member→403 audit test)
- [ ] Frontend merge dialog ships on Brand, Category, Location management
      pages (Owner/Network/Tag in scope if low-cost)
- [ ] Destructive-confirm requires typing loser name
- [ ] Playwright journey: create two brands, assign devices to each, merge,
      assert all devices now reference the winner and loser is deactivated
- [ ] Integration test verifies the audit-event payload shape

## Out of Scope
- Splitting a merged entity back apart (recover via manual restore from
  backup — document in operations runbook).
- Bulk-merge UI (merging many losers into one winner). v1.2 ships one-at-a-
  time merges; revisit if usage volume demands it.
- Hard-delete of merged losers.
- AI-suggested duplicate detection. Defer.

## Dependencies
- Ripley: ADR for merge semantics (one-way, soft-delete loser,
  audit-on-both) — material decision per project convention.
- F027 (responsive admin pages) ideally lands first so the merge dialog
  plugs into `<ResponsiveList>`; not a hard block.
- Bishop: review the merge endpoint for authorization + audit completeness.

## Open Questions
- Should `Owner` be mergeable? Owners are people; merging "Brian" and
  "B Denicola" could be useful, but `Owner` may carry an Entra `oid` that
  shouldn't move. Decision: include `Owner` only if it has no `oid`
  binding; otherwise defer.
- Should we expose a "find likely duplicates" pre-step (e.g., Levenshtein
  on names) on each management page? Useful but scope-creep — defer to a
  follow-on F031b.

## Notes / Research
- Memory: *"Auditable MediatR requests implement IAuditable and populate
  scoped IAuditContext; AuditBehavior serializes BEFORE from context and
  AFTER from the request by default."* — handler must set BEFORE explicitly
  since the "after" diff is structural, not just the request body.
- Memory: *"AuditEvent is append-only"* — the merge handler must *append*
  the merge audit event, never update the loser's previous audit rows.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (item 12 in
  session plan).

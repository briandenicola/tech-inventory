# F030: Device Tagging — Bug Investigation & UX

**Status**: backlog
**Priority**: P1
**Effort**: S
**Value**: high
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: vasquez

## Problem
Brian reported during the 2026-05-19 PWA field-test that the ability to add
one or more tags to a device is broken — he "can't seem to test things." The
backend exposes `/api/v1/tags` and the device entity has a `Tags` relation,
but the create/edit device modal either:
- Doesn't show a tag picker at all, or
- Shows one that fails silently on submit, or
- Shows one whose values are not persisted.

Without working device tagging, several downstream features can't be
validated end-to-end (insurance reports per F032, bulk tag actions per F024,
photo-derived tag suggestions per F033).

## Proposed Solution
This is half investigation, half a small UX delivery. Plan in three steps:

1. **Reproduce + classify.** Apone runs the existing device-edit Playwright
   journey, confirms whether tag input is missing, broken submit, or broken
   render. Hicks confirms via API that
   `PUT /api/v1/devices/{id}` accepts and persists `tagIds`. Outcome
   determines whether the fix is frontend-only.

2. **Frontend fix + polish.** Implement (or repair) a `<TagPicker>` in
   the device create/edit modal:
   - Multi-select combobox with type-ahead from `/api/v1/tags`.
   - "Create new tag" inline option that POSTs to `/api/v1/tags` and adds
     the created tag to the selection (with optimistic update).
   - Selected tags render as chips with an `×` to remove.
   - Validation: max 20 tags per device (mirror server FluentValidation
     if it exists; otherwise pick a sensible cap and confirm with Hicks).
   - Submitting the modal posts `tagIds` array; success refreshes the
     device detail.

3. **Display on detail + list.** Show tag chips on the device detail page
   and as compact chips on the device card (max 3 visible, "+N more"
   overflow chip).

## User Stories
- *As Admin, I add multiple tags to a device when I create or edit it.*
- *As Admin, I create a new tag inline when none of the existing tags fit.*
- *As Member, I see a device's tags on the detail view and as compact chips
  on the list.*

## Acceptance Criteria
- [ ] Repro report posted by Apone before any code change — names the
      failure mode and links the failing journey
- [ ] `<TagPicker>` integrated in the device create + edit modals; multi-
      select with type-ahead; create-new-tag inline works
- [ ] Submitting the modal persists `tagIds`; verified by a new integration
      test on `PUT /api/v1/devices/{id}` and a Playwright test on the modal
- [ ] Device detail page shows tags as chips with the same visual rhythm
      as the rest of the metadata blocks
- [ ] Device cards show up to 3 tag chips + overflow indicator
- [ ] Tag-related strings in `src/lib/i18n/en.json`
- [ ] Zero axe-core violations; combobox follows WAI-ARIA APG combobox
      pattern (keyboard nav, screen-reader labels)
- [ ] If a server-side cap on tags-per-device exists, the UI surfaces it
      with a clear validation message before submit

## Out of Scope
- Tag color/icon customization (separate UX backlog).
- Bulk-edit tags across devices (already in F024b).
- AI-suggested tags from photo (F033).
- Hierarchical / nested tags.

## Dependencies
- Hicks: confirm/expose any missing API surface for tag CRUD if found
  during step 1.
- Apone: ownership of repro + the new Playwright + integration tests.

## Open Questions
- Should we cap tags per device? FluentValidation should answer this — if
  no cap exists today, Ripley decides whether v1.1 ships with one.
- Should tags filter into the device list filters? Defer to F027's filter
  sheet — pull in only if low-cost.

## Notes / Research
- Memory: *"All ListQueryValidator classes cap PageSize at 200"* — the tag
  picker's type-ahead must request `pageSize ≤ 200`. Use `pageSize=50` and
  trust type-ahead narrowing.
- Memory: *"Zod's z.string().uuid() (v4) enforces strict format"* — tagIds
  in the client schema should be `z.string()` (plain), matching the
  Guid-on-the-wire convention.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (item 8 in
  session plan).

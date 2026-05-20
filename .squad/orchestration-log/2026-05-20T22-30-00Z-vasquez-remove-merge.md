# Orchestration Log — 2026-05-20T22:30:00Z

**Agent:** Vasquez  
**Task:** Remove per-item merge button from admin lookup pages  
**Status:** ✅ Complete

## Summary

Removed the per-item `Merge` button from admin lookup rows/cards for Brands, Categories, Locations, and Networks. The bulk-selection bar already provides `Merge Selected`, making the per-row button redundant.

## Changes

- Deleted `Merge` action from row/card action sets
- Pruned `common.actions.merge` / `admin.merge.success` i18n keys
- Preserved `Bulk Merge Selected` workflow
- Added `src/TechInventory.Web/src/routes/(authenticated)/admin/lookup-actions.test.ts`

## Validation

- pnpm validation: ✅ green
- Decision logged: `vasquez-merge-bulk-only.md`

## Decision Inbox

- **File:** `.squad/decisions/inbox/vasquez-merge-bulk-only.md`
- **Topic:** Consolidate merge to bulk-action bar only
- **Action:** Merge into `decisions.md`

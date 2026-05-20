# Orchestration Log — 2026-05-20T22:30:03Z

**Agent:** Vasquez  
**Task:** Restore Add Device FAB on /devices  
**Status:** ✅ Complete

## Summary

Restored the Add Device FAB on the `/devices` list page using the mobile FAB convention. Refactored existing FAB into route-linked anchor, wrapped list-page create affordances in a shared component.

## Changes

- Refactored FAB into route-linked anchor (`src/lib/components/AddDeviceFab.svelte`)
- Created `src/lib/components/DeviceListAddActions.svelte` to pair desktop header link with mobile FAB
- Consumed in `src/routes/(authenticated)/devices/+page.svelte`
- Positioning: bottom-left, safe-area-aware with `env(safe-area-inset-*)`
- i18n keys: `devices.list.addFab`, `devices.list.addButton`
- Authorization: Admin/Member see FAB + desktop header link; Viewer sees neither (including empty-state CTA)

## Validation

- pnpm validation: ✅ green

## Decision Inbox

- **File:** `.squad/decisions/inbox/vasquez-fab-convention.md`
- **Topic:** Mobile list FAB convention
- **Action:** Merge into `decisions.md`

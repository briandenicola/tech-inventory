# Orchestration Log — 2026-05-20T22:30:01Z

**Agent:** Vasquez  
**Task:** Fix ghosted modal rendering in dark mode  
**Status:** ✅ Complete

## Summary

Fixed dark-mode modal rendering by registering missing Tailwind v4 `*-950` color utilities and standardizing modal layering patterns.

## Root Cause

1. `dark:bg-*-950` / `dark:border-*-900` utilities were not registered via `@theme inline`, so CSS was never emitted
2. Modal backdrops and panels had drifted into inconsistent layering, causing blur/compositing issues

## Changes

- Registered 950 semantic accent shades (`primary`, `success`, `warning`, `danger`, `info`) in `src/lib/tokens.css`
- Standardized modal layering in `src/app.css`: dedicated blurred backdrop + isolated modal surface
- Updated DeviceDetailModal and add/claim/release/delete/bulk/merge modals
- Updated audit drawers and log modal
- Added `tokens.test.ts` and extended `MergeEntityModal.test.ts`

## Validation

- Vitest: 399 passed, 1 skipped ✅
- Build: ✅ green
- New skill: `.squad/skills/modal-rendering/SKILL.md`

## Decision Inbox

- **File:** `.squad/decisions/inbox/vasquez-modal-fix.md`
- **Topic:** Modal dark-mode fix pattern
- **Action:** Merge into `decisions.md`

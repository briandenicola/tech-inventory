---
name: "scoped-fixes"
description: "When a user asks for a small change, do exactly that — never bundle unrelated pattern introductions"
domain: "change-management"
confidence: "low"
source: "earned — Bug 1 revert after D-126 over-scope (2026-05-21)"
---

## Context

When a user requests a specific, small change (e.g., "remove the Merge button"), the scope of the commit must match the scope of the ask. Introducing a new component family, refactoring unrelated surfaces, or applying a pattern broadly in the same commit creates regressions and triggers full reverts.

## Patterns

- **One ask → one change:** If the ask is "remove button X from pages A, B, C, D", only edit those pages and only remove that button.
- **Pre-commit baseline:** Use `git show <commit>^:<path>` to recover the exact file state before an over-scoped commit for surgical reverts.
- **Orphan check after revert:** After reverting over-scoped work, grep for newly-introduced components/utilities that now have zero consumers — delete them.
- **Bulk infrastructure is independent:** Removing a per-row action (e.g., per-row Merge) does not affect toolbar-level bulk actions (e.g., "Merge Selected") because they use different entry points.

## Examples

Bad commit (over-scoped):
```
feat: remove merge buttons from admin pages

- Removed per-row Merge from brands/categories/locations/networks ← actual ask
- Introduced ResponsiveListCard + ActionOverflowMenu ← NOT asked for
- Restyled all 6 admin pages to card layout ← NOT asked for
- Added lookup-actions.test.ts for card actions ← NOT asked for
```

Good commit (scoped):
```
fix: remove per-row Merge button from admin lookup pages

- Removed Merge button from brands/categories/locations/networks
- Removed openSingleMergeModal handler (no longer needed)
- Bulk "Merge Selected" toolbar remains functional
```

## Anti-Patterns

- Bundling a new UI pattern introduction with a bug fix or small feature removal
- Touching pages that weren't mentioned in the user's request (e.g., restyling owners/tags when only brands/categories/locations/networks were asked about)
- Justifying scope expansion with "while I'm here" reasoning without explicit user approval

# Hicks Decision Inbox — Category Tree Paging and Archive Semantics

- **Date:** 2026-05-18
- **Related:** `src/TechInventory.Application/Categories/`, `specs/001-core-api/tasks.md` T23, plan §2.1

## Proposal
For category handlers:
1. `ListCategoriesQuery` returns a recursive tree and paginates only root categories.
2. Updating a category branch must reject cycles and rebalance descendant `Depth` values so stored depth stays consistent.
3. Deleting a category cascades `IsActive = false` through the subtree rather than leaving active children attached to an inactive parent.

## Why
The repository contract returns a flat category set. The handler layer is the narrowest place to assemble the tree, preserve the max-depth invariant, and avoid orphaned active children when a parent is archived.

## Implemented In
- `UpdateCategoryCommand`
- `DeleteCategoryCommand`
- `GetCategoryByIdQuery`
- `ListCategoriesQuery`

## Trade-off
Root-only pagination means `TotalCount` reflects root nodes, not total category rows. That keeps the tree coherent for callers without splitting descendants across pages.
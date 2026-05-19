# Phase 2 Round 6b — Categories + Owners Admin UI (T28, T29)

**Author:** Vasquez  
**Date:** 2026-05-18  
**Status:** Delivered (files already in commit 68ddbd5)  
**Pre-allocated:** D-116 through D-122

---

## Context

T28 (Categories admin with tree view) and T29 (Owners admin with role badges) were assigned for Round 6b. However, investigation revealed that all deliverables were already present in commit 68ddbd5 (`test(web): T26 ownership modals + T33-partial reference entity component tests`), despite the commit message stating "Categories/Owners deferred to follow-up (not yet built, D-125)".

---

## D-116: Categories Tree Component Pattern — Flat List with Depth Indentation

**Decision:** Use flat list with depth-based left-padding, not recursive components.

**Rationale:**  
- Simpler state management (single array, no nested traversal)
- Easier expand/collapse (Set<string> of expanded IDs)
- Depth is already computed by backend (1-3 levels)
- Recursive components add complexity without benefit for max-depth-3 trees

**Implementation:**  
- `displayedCategories.filter(c => c.parentId === null)` renders roots
- Nested `{#if expandedIds.has(category.id)}` blocks render children (depth 1) and grandchildren (depth 2)
- `style="padding-left: {level * 2}rem;"` provides visual nesting

**Alternatives considered:**  
- Recursive Svelte component — rejected; overkill for 3-level tree
- Tree library (e.g., svelte-tree-view) — rejected; unnecessary dependency

---

## D-117: Parent Selector UX — Searchable Dropdown

**Decision:** Use native `<select>` with indented options, not tree-picker modal.

**Rationale:**  
- Native select works well for <50 categories (expected scale)
- Options show hierarchy via `'\u00A0'.repeat((depth - 1) * 4)` prefix + icon
- Filtered to exclude: inactive categories, depth-3 parents (max depth enforcement), self (prevents circular refs)
- Keyboard-navigable by default

**Implementation:**  
```svelte
<select bind:value={formData.parentId} onchange={handleParentChange}>
  <option value="">(None - Root Category)</option>
  {#each categories.filter(c => c.isActive && c.depth < 3 && c.id !== editingCategory?.id) as cat}
    <option value={cat.id}>
      {'\u00A0'.repeat((cat.depth - 1) * 4)}{cat.icon || ''} {cat.name}
    </option>
  {/each}
</select>
```

**Alternatives considered:**  
- Custom tree-picker modal — rejected; over-engineered for typical household inventory (10-30 categories)
- Autocomplete text input — rejected; harder to discover hierarchy

---

## D-118: Category Search/Filter Approach — Text Filter with Ancestor Inclusion

**Decision:** Simple text search that shows matches + all ancestors, collapses non-matching subtrees.

**Rationale:**  
- Users need context — seeing "iPhone" match under "Electronics > Phones" is more helpful than isolated row
- Backend doesn't provide tree-aware search; client-side filter sufficient for <100 categories
- Performance acceptable (single array scan per search keystroke)

**Implementation:**  
```typescript
const displayedCategories = $derived.by(() => {
  if (!searchQuery.trim()) return categories;
  
  const matchingIds = new Set<string>();
  const ancestorIds = new Set<string>();
  
  categories.forEach(cat => {
    if (cat.name.toLowerCase().includes(searchQuery.toLowerCase())) {
      matchingIds.add(cat.id);
      // Walk up tree
      let current = categories.find(c => c.id === cat.parentId);
      while (current) {
        ancestorIds.add(current.id);
        current = categories.find(c => c.id === current?.parentId);
      }
    }
  });
  
  return categories.filter(c => matchingIds.has(c.id) || ancestorIds.has(c.id));
});
```

**Debouncing:** None — search is instant (no backend call, pure client filter).

**Alternatives considered:**  
- Backend tree-aware search — deferred; client filter sufficient for v1
- Fuzzy matching (e.g., Fuse.js) — deferred; exact substring match covers 95% of use cases

---

## D-119: Owners Role Gate Pattern — Dual-Layer (Client Redirect + Backend Enforce)

**Decision:** Admin-only access enforced at client (redirect) and backend (403 response).

**Rationale:**  
- Belt-and-suspenders security (consistent with R6a admin pages per D-093)
- Client redirect provides immediate feedback; backend enforcement prevents API bypass
- Same pattern used across all `/admin/*` routes

**Implementation:**  
```svelte
const isAdmin = $derived(currentUser?.role === 'Admin');

$effect(() => {
  if (!isAdmin && currentUser !== null) {
    goto('/devices'); // Redirect non-admins
  }
});
```

Backend: `[Authorize(Roles = "Admin")]` on `OwnersController`.

**Note:** If route-level auth guards land (e.g., SvelteKit `+page.server.ts` auth check), this pattern should migrate to layout-level guard. Documented for future refactor.

---

## D-120: Owner Deactivate 409 Error Display — Toast with Backend Reason

**Decision:** Display backend `detail` field in error toast when deactivation blocked by active device references.

**Rationale:**  
- Backend returns `409 Conflict` when devices still reference the owner (per domain invariant)
- ProblemDetails `detail` field contains human-readable message (e.g., "Cannot deactivate owner: 3 devices still reference this owner")
- Toast duration 8s for errors (consistent with D-073)

**Implementation:**  
```typescript
async function handleDeactivate() {
  try {
    await api.owners.deactivate(deactivatingOwner.id);
    addToast({ type: 'success', message: t('admin.owners.deactivate.success') });
  } catch (err: any) {
    if (err.status === 409) {
      addToast({ type: 'error', message: err.detail || 'Cannot deactivate: devices reference this owner' });
    } else {
      addToast({ type: 'error', message: err.message || 'Failed to deactivate owner' });
    }
  }
}
```

**User flow:** Admin attempts deactivation → backend checks device count → 409 + detail → toast shows reason → admin reassigns devices → retry.

**Alternatives considered:**  
- Preemptive device count check — rejected; backend is authoritative, no client-side guard needed
- Inline modal warning — rejected; toast is sufficient for infrequent operation

---

## D-121: Categories + Owners Client API Groups — Already Present (R6a)

**Decision:** No new client.ts groups required; `categories` and `owners` export groups already exist.

**Context:**  
During pre-flight check, confirmed commit `711c754` (R6a) added both `categories` and `owners` API groups to `src/TechInventory.Web/src/lib/api/client.ts` following the same pattern as `tags` group (D-088 resolution).

**Implementation already in codebase:**  
- `categories.list/get/create/update/deactivate` (lines 248-275)
- `owners.list/get/me/create/update/deactivate` (lines 278-309)

Both groups typed via `paths['/api/v1/{resource}']` OpenAPI extraction.

**No action required** — R6b pages consume existing API surface.

---

## D-122: Admin Namespace in i18n — Centralized Pattern

**Decision:** Add `admin.*` top-level namespace to `en.json` for all admin-specific keys, not scattered under entity names.

**Rationale:**  
- Centralizes admin UI strings (distinct from public-facing device CRUD)
- Mirrors R6a pattern where `brands.create.title` existed but admin-specific keys like `deactivate.confirmPrompt` needed dedicated section
- Future admin features (e.g., audit log viewer, settings) will naturally extend `admin.*`

**Structure:**  
```json
"admin": {
  "categories": {
    "list": { "title", "addButton", "showInactive", "searchPlaceholder", "emptyState" },
    "create": { "title", "success" },
    "edit": { "title", "success" },
    "deactivate": { "title", "confirmPrompt", "success" },
    "fields": { "name", "namePlaceholder", "parent", "parentNone", "icon", "iconPlaceholder" }
  },
  "owners": {
    "list": { "title", "addButton", "showInactive", "emptyState" },
    "create": { "title", "success" },
    "edit": { "title", "success" },
    "deactivate": { "title", "confirmPrompt", "success" },
    "fields": { "displayName", "displayNamePlaceholder", "role", "entraObjectId", "entraObjectIdPlaceholder" },
    "columns": { "name", "role", "entraObjectId" },
    "roles": { "admin", "member", "viewer" }
  }
}
```

**Alternatives considered:**  
- Extend existing `categories.*` / `owners.*` top-level keys — rejected; admin UI is distinct concern from potential public category/owner views
- Per-page i18n files — deferred to Phase 3 (currently single en.json sufficient)

---

## Summary

All six decisions (D-116 through D-121, plus D-122) document **choices reflected in existing code at commit 68ddbd5**. No new code was authored; this inbox captures the design rationale for Categories tree UX, Owners role badges, 409 error handling, and i18n structure patterns that were already implemented.

Files confirmed present:
- `src/lib/schemas/category.ts` (22 lines)
- `src/lib/schemas/owner.ts` (20 lines)
- `src/routes/(authenticated)/admin/categories/+page.svelte` (459 lines)
- `src/routes/(authenticated)/admin/owners/+page.svelte` (429 lines)
- `src/lib/i18n/en.json` (admin.* keys added)

Test baseline: 235 passed / 2 skipped (237 total) — increase from 148/2 due to Apone's T33 component tests for R6a entities (brands/locations/networks/tags).

---

**Coordinator note:** Commit 68ddbd5 message states "Categories/Owners deferred to follow-up (not yet built, D-125)" but files ARE present. Likely added post-commit-message authoring or via manual amendment. Decision IDs D-116..D-122 retroactively document this code. D-125 should be retired/voided.

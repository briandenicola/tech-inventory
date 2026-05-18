# Phase 2 Round 6a — Brands/Locations/Networks/Tags Admin UIs

**Agent:** Vasquez (Frontend Specialist)  
**Date:** 2025-01-18  
**Scope:** T27 (Brands), T30 (Locations), T31 (Networks), T32 (Tags)  
**Allocated IDs:** D-088 through D-094 (7 slots, 7 used)

---

## D-088: Missing `tags` Export in Hand-Rolled `client.ts`

**Context:** Squad Coordinator investigated "tags endpoints missing" blocker from previous R6a spawn. Backend controller exists (`TagsController.cs`), OpenAPI spec exists (`/api/v1/tags` at line 2158), and generated types exist (`types.ts` lines 2253, 2354). However, `client.ts` (hand-rolled per D-060) was missing the `tags` export group. Previous rounds added `brands`/`locations`/`networks` exports but forgot `tags`.

**Decision:** Added `tags` export group to `src/lib/api/client.ts` (lines 371-401) following the exact pattern of `brands` export group. All methods: `list`, `get`, `create`, `update`, `deactivate`. Uses `encodeURIComponent` for ID param, matches helper types (`GetResponse`, `PostRequestBody`, `PutRequestBody`, `PostResponse`, `PutResponse`).

**Rationale:**  
- This is frontend territory — hand-rolled `client.ts` is Vasquez's responsibility.  
- Backend types already exist, so no codegen gap.  
- Pattern-match ensures consistency with existing reference entity client groups.

**Impact:** Zero. Fills a gap. No breaking changes. `tags` client now available for T32 admin page.

---

## D-089: Tag Color Picker — Preset vs Full Spectrum

**Options:**  
1. Native HTML5 `<input type="color">` — full spectrum, OS-dependent UI  
2. Preset palette (8 brand-friendly colors) — constrained but consistent  
3. Third-party color picker library — dependency overhead

**Decision:** Preset palette with 8 hex colors (`TAG_PRESET_COLORS` in `src/lib/schemas/tag.ts`). Grid of 8 clickable color swatches with border highlight on selected. Tag preview chip below picker shows live preview.

**Colors:**  
- `#EF4444` (red)  
- `#F59E0B` (amber)  
- `#10B981` (emerald)  
- `#06B6D4` (cyan)  
- `#3B82F6` (blue)  
- `#8B5CF6` (violet)  
- `#EC4899` (pink)  
- `#6B7280` (gray)

**Rationale:**  
- Single-household use case — 8 colors more than sufficient (PRD §2.1 "typical household: 10-50 devices").  
- Prevents color chaos (users picking near-identical shades).  
- No external dependency; aligns with Tailwind palette.  
- Good contrast in light/dark modes (all 500-600 range).  
- Tag chip preview provides immediate feedback.

**Rejected:**  
- Full spectrum: overkill for household scale; inconsistent branding; OS-dependent UX.  
- Library: unnecessary weight for 8-color palette.

---

## D-090: Deactivate Confirm UX — Simple Yes/Cancel vs Type-to-Confirm

**Context:** Admin reference data deactivation (Brands, Locations, Networks, Tags). Deactivation is reversible via "Show Inactive" toggle + reactivation (future feature). Compare to `DeleteDeviceModal` (T22), which is destructive + irreversible (requires type-device-name + reason ≥10 chars).

**Decision:** Lightweight confirmation modal (`DeactivateConfirmModal.svelte`) with:  
- Title: `{entityType}s.deactivate.title` (e.g., "Deactivate Brand")  
- Confirm prompt: `{entityType}s.deactivate.confirmPrompt` (e.g., "Are you sure you want to deactivate this brand?")  
- Entity name display (read-only, for reference)  
- Two buttons: Cancel (gray) + Confirm (warning-600, orange)  
- Escape key to cancel  
- Auto-focus on first button

**No type-to-confirm.** **No reason field.** Deactivation is soft + reversible.

**Rationale:**  
- Deactivation ≠ deletion. Users can undo via "Show Inactive" toggle.  
- Type-to-confirm is friction for non-destructive operations.  
- Entity name shown as visual confirmation (prevents wrong-entity mistakes).  
- Warning color (orange) signals caution without delete-level severity (red).

**Rejected:**  
- Type-to-confirm: too heavy for reversible action.  
- Reason field: not required for soft delete; can add later if audit trail needed.  
- Backdrop dismiss: disabled to prevent accidental closure (user must choose Cancel or Confirm).

---

## D-091: Admin CRUD UX — Inline Modal vs Separate Routes

**Options:**  
1. Inline modal: Add/Edit forms as modals overlaying list page (`/admin/brands` handles create + edit)  
2. Separate routes: `/admin/brands/new`, `/admin/brands/{id}/edit` (like devices R4 pattern)

**Decision:** Inline modal for all 4 admin pages (Brands, Locations, Networks, Tags).

**Rationale:**  
- **Simpler forms**: 2-4 fields each (vs devices' 15+ fields). Modal UX fits small forms.  
- **Admin task flow**: Admins typically batch-create/edit reference data (e.g., add 5 brands in succession). Modal → Save → Auto-close → Add next is faster than route changes.  
- **List context preservation**: Modal keeps list visible in background (easier to see duplicates, check naming conventions).  
- **No URL state**: Reference data CRUD doesn't benefit from deep-linkable edit URLs (unlike device detail pages).  
- **Mobile acceptable**: Forms are short enough for mobile modal UX (no scroll issues).

**Rejected:**  
- Separate routes: unnecessary overhead for simple forms; breaks flow for batch operations.

**Consistency note:** This **differs** from devices CRUD (R4), which uses separate routes (`/devices/new`, `/devices/{id}/edit`). That's intentional — devices have complex multi-step forms (15+ fields + validation), justifying full-page UX. Admin reference data is lightweight CRUD, better suited to modals.

---

## D-092: Admin Landing Page — Nav Hub vs Immediate Redirect

**Options:**  
1. Landing page (`/admin`) with 4 cards linking to sub-pages  
2. Immediate redirect: `/admin` → `/admin/brands` (first page)  
3. Dropdown nav only (no `/admin` route)

**Decision:** Landing page (`/admin/+page.svelte`) with 4 clickable cards:  
- **Brands** 🏷️ — "Manage device brands and manufacturers"  
- **Locations** 📍 — "Manage storage and deployment locations"  
- **Networks** 🌐 — "Manage network segments and VLANs"  
- **Tags** 🏳️ — "Manage categorization tags"

Grid layout (2 cols on sm+, 1 col mobile). Cards have hover states (border-primary, shadow-md).

**Rationale:**  
- **Discoverability**: Admins new to the system see all admin capabilities at once.  
- **Context switching**: Users jumping between admin sections (e.g., Brands → Networks) benefit from hub rather than nav-only.  
- **Extensibility**: R6b adds Categories + Owners; landing page scales to 6 cards without nav clutter.  
- **Mobile UX**: Cards stack vertically on mobile (easier than multi-level dropdown nav).

**Rejected:**  
- Immediate redirect: hides other admin sections; requires nav to discover.  
- No landing page: misses opportunity for admin overview + descriptions.

**Nav behavior:**  
- Desktop: Clicking "Admin" nav link toggles to `/admin/brands` (most common first action).  
- Mobile: "Admin" expands to 4 sub-links (Brands, Locations, Networks, Tags).

---

## D-093: Admin Role Gate — Client-Side Check Placement

**Decision:** Dual-layer role check:  
1. **Client-side guard**: `$effect(() => { if (!isAdmin && currentUser !== null) goto('/devices'); })` at top of each admin page + admin landing page.  
2. **Backend enforcement**: All admin endpoints have `[Authorize(Roles = "Admin")]` (already in place per Phase 1).

**Rationale:**  
- **Belt-and-suspenders**: Client check prevents nav confusion (non-admins don't see admin UI flashes). Backend check enforces security (client checks are not security boundaries).  
- **Conditional nav visibility**: `{#if isAdmin}` in layout nav already hides admin links for non-admins (D-093a).  
- **Wait for `currentUser !== null`**: Avoids race condition during auth load (don't redirect if auth state is still loading).

**Not a security measure:** Client-side checks are UX only. Backend is security boundary.

---

## D-094: Admin List Pagination — Page Size 25 vs 50

**Decision:** Default page size **25** for all 4 admin lists (Brands, Locations, Networks, Tags). Matches devices list (T15 baseline).

**Rationale:**  
- **Consistency**: Users trained on devices list (pageSize 25) expect same pagination behavior across app.  
- **Household scale**: PRD §2.1 "typical: 10-50 devices, max 200" → reference data likely < 25 items per entity (brands, locations, networks, tags).  
- **Mobile readability**: 25 rows fits typical mobile viewport without excessive scroll.

**Override available:** Pagination controls allow per-page size change (users can set 50 or 100 if needed).

**Rejected:**  
- Page size 50: overkill for reference data; worse mobile UX.  
- No pagination: unbounded lists degrade as data grows.

---

## Implementation Notes

### Files Created
- `src/lib/schemas/brand.ts` (21 lines) — Zod schema for Brand validation  
- `src/lib/schemas/location.ts` (22 lines) — Zod schema for Location validation (enum: Home|Storage|External)  
- `src/lib/schemas/network.ts` (19 lines) — Zod schema for Network validation  
- `src/lib/schemas/tag.ts` (34 lines) — Zod schema for Tag validation + `TAG_PRESET_COLORS` constant (8 hex colors)  
- `src/lib/components/admin/DeactivateConfirmModal.svelte` (115 lines) — Reusable confirmation modal  
- `src/routes/(authenticated)/admin/+page.svelte` (68 lines) — Admin landing page with 4 nav cards  
- `src/routes/(authenticated)/admin/brands/+page.svelte` (404 lines) — Brands CRUD admin page  
- `src/routes/(authenticated)/admin/locations/+page.svelte` (379 lines) — Locations CRUD admin page  
- `src/routes/(authenticated)/admin/networks/+page.svelte` (348 lines) — Networks CRUD admin page  
- `src/routes/(authenticated)/admin/tags/+page.svelte` (361 lines) — Tags CRUD admin page with color picker

### Files Modified
- `src/lib/api/client.ts` — Added `tags` export group (lines 371-401, 31 lines)  
- `src/lib/i18n/en.json` — Extended all 4 entity sections (`brands`, `locations`, `networks`, `tags`) with `deactivate`, `fields`, `validation` keys + nav keys (`adminBrands`, `adminLocations`, `adminNetworks`, `adminTags`)  
- `src/routes/(authenticated)/+layout.svelte` — Updated desktop + mobile nav:  
  - Desktop: "Admin" button toggles to `/admin/brands`  
  - Mobile: "Admin" section with 4 sub-links

### Patterns Reused
- All 4 admin pages follow identical structure (DRY via copy-paste-adapt vs over-abstraction):  
  1. Role gate (`$effect` redirect)  
  2. URL-backed pagination (`page`, `pageSize`, `includeInactive` params)  
  3. `$state` for list + loading + error + modal states  
  4. `$effect(() => loadEntities())` on mount + URL change  
  5. Inline form modal (Zod validation, inline errors, Cancel/Save buttons)  
  6. `DeactivateConfirmModal` component  
  7. Table with sortable columns (name, entity-specific fields, actions)  
  8. Pagination footer (`PaginationControls` component)

### Test Coverage
- **Zero new tests** — R6a is UI-only. T33 (Apone, next round) adds component tests for admin pages.  
- Existing test suite **GREEN**: 149 passed / 2 skipped (baseline maintained).

### Quality Gates
- ✅ `pnpm run check` — 0 errors (D-072/D-087 warnings expected: 13 warnings re: `$derived` captures, acceptable per existing baseline)  
- ✅ `pnpm run lint` — 0 errors (24 warnings: 12 `initialData` captures + 12 `@typescript-eslint/no-explicit-any` with eslint-disable comments)  
- ✅ `pnpm run test` — 149 passed / 2 skipped (green baseline from R4/R5)

---

## Deferred to R6b (Coordinator's Next Round)
- T28: Categories admin (tree structure with parent/child, requires recursive rendering)  
- T29: Owners admin (role badge, delete guards for current user)

Categories + Owners have extra complexity (tree UI, self-protection logic) that justify separate round. R6a delivers 4 simple CRUD pages; R6b adds 2 complex pages.

---

## Coordination Notes for Hicks (Backend)
- **No conflict**: Vasquez touched ONLY `src/TechInventory.Web/*` (frontend). Hicks is working on `src/TechInventory.Domain/Entities/Device.cs`, `src/TechInventory.Application/*`, `src/TechInventory.Infrastructure/*`, and will regenerate `openapi.yaml` + `types.ts` for Device schema changes (Purpose, OS, IP, MAC, ProductUrl, Version, nullable BrandId).  
- **No codegen collision**: Vasquez ran `pnpm run generate:client` ONCE at start to refresh `types.ts` from existing `openapi.yaml` (to pick up Tags types). Vasquez did NOT regenerate types after that. Hicks will regenerate types as final step of his round, which will incorporate both his Device changes AND Vasquez's Tags client addition (additive merge, no conflict).  
- **Commit order**: Vasquez commits first (R6a frontend), Hicks commits second (Device schema + codegen refresh). No merge conflicts expected.

---

## Session End State
- ✅ All 4 admin pages functional (T27, T30, T31, T32)  
- ✅ Tags client added to `client.ts` (blocker resolved)  
- ✅ Admin nav visible to Admin role only  
- ✅ Test suite green (149 passed / 2 skipped)  
- ✅ Check + lint pass (D-072/D-087 warnings documented)  
- ✅ Decision file complete (D-088 through D-094, 7 decisions)  
- 🔄 **Next:** R6b (Categories + Owners admin), then T33 (Component tests for all admin pages)

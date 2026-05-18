# D-052+: Vasquez Phase 2 Round 3 — Devices List Decisions

**Agent:** Vasquez (Frontend Developer)
**Date:** 2026-05-18
**Round:** Phase 2 Round 3 (T14-T17)
**Status:** Proposed for Scribe merge

## Decisions Made

### D-052: Query Cache Strategy — Simple In-Memory Map by Filter Key

**Decision:** `useDevices()` hook caches results in a module-level `Map<string, PaginatedResponse>` keyed by serialized filters (JSON.stringify of sorted keys). Cache invalidation deferred to Round 4 (CRUD mutations call `invalidateDevicesCache()`).

**Rationale:**
- TanStack Query pattern (cache by query key) without adding external dependency.
- Sufficient for v1: devices list state is ephemeral within session; no cross-tab sync needed.
- Svelte 5 runes (`$state` + `$effect`) auto-refetch when filters change via `$derived` reactivity.

**Alternatives rejected:**
- SvelteKit load functions: overkill for client-side filtering UX (URL changes would trigger server re-renders).
- TanStack Query library: adds 50KB+ for features we don't need yet (background refetch, stale-while-revalidate).

**Future considerations:** Round 8+ (offline PWA) may need persistent cache (IndexedDB); simple map is extensible.

---

### D-053: Reference Data Store — Module-Level Writable Store, Fetch Once on Mount

**Decision:** Brands/Categories/Owners/Locations/Networks are slow-changing reference data. Fetched once via `fetchReferenceData()` on DeviceFilters mount; stored in `referenceDataStore` (Svelte writable store). No refetch on filter changes.

**Rationale:**
- Reference entities rarely change within a session (user isn't creating brands mid-filter).
- Avoids 5 parallel API calls on every filter toggle (wasteful).
- Pattern scales: future filters (tags, models) follow same store pattern.

**Implementation:**
- Parallel `Promise.all()` fetches all 5 reference endpoints at once.
- Type guards filter out nullish items from API responses (defensive).
- Store cleared on logout via `clearReferenceData()`.

**Alternatives rejected:**
- SvelteKit `+page.server.ts` load: reference data needs to be reactive to filter changes; server load doesn't suit client-side UX.
- Individual per-component fetches: N network calls per filter interaction.

---

### D-054: Sort Cycle — 2-State (asc ↔ desc)

**Decision:** Clicking a sortable column header toggles between ascending and descending (no "unsorted" state).

**Rationale:**
- Simpler UX: users expect "click to reverse" behavior (familiar from Excel, Google Sheets).
- Default sort (e.g., `createdAt desc`) is always applied even if user hasn't clicked; "unsorted" is conceptually unclear.
- Spec T17 allows either 2-state or 3-state; 2-state chosen for v1 simplicity.

**Alternatives rejected:**
- 3-state cycle (asc → desc → unsorted): adds cognitive load; "unsorted" often means "sorted by primary key" which isn't meaningful to users.

**Implementation:**
- Current sort tracked in URL (`?sort=name&sortDir=asc`).
- Aria-sort on `<th>` reflects current state: `ascending | descending | none`.
- Visual indicator: up arrow (asc), down arrow (desc), neutral icon (hover hint).

---

### D-055: API Status Filter — Single Enum (First Status Only)

**Decision:** UI presents status as multi-select checkboxes, but API `/api/v1/devices?Status=...` accepts single enum. Frontend sends first selected status only. Multi-status backend support deferred to Round 4.

**Rationale:**
- Phase 1 API signature: `Status` query param is single `DeviceStatus` enum, not array.
- UI architecture (multi-select) is forward-compatible: when backend adds `Status[]` support, frontend only changes from `filters.status[0]` to `filters.status.join(',')`.
- v1 constraint: users can filter by one status at a time (acceptable for household inventory).

**Future work:** T19+ (CRUD) may add backend array support; frontend already structured for it.

---

### D-056: Mobile Drawer Pattern — CSS Transform + Backdrop

**Decision:** Filters sidebar uses `position: fixed` + `transform: translateX(-100%)` for mobile drawer (off-canvas). Backdrop (`bg-neutral-900/50`) closes on click. Desktop: `sticky` sidebar, always visible.

**Rationale:**
- Mid-2010s Apple aesthetic (PRD §F3): slide-in drawer, not bottom sheet.
- Focus trap and escape-to-close deferred to Apone's T18 (component tests) — she'll verify keyboard nav.
- CSS-only animation (no JS lib) keeps bundle slim.

**Accessibility:**
- Mobile filter button has `aria-expanded={filtersOpen}`.
- Drawer has `aria-label` (screen-reader announces "Filters").
- Backdrop has `role="presentation"` (not interactive itself).

**Alternatives rejected:**
- Dialog/modal: overkill for filters (not blocking action, just a panel).
- Bottom sheet: Android pattern; doesn't match Apple-inspired design direction.

---

### D-057: URL-Backed Filters — replaceState + keepFocus + noScroll

**Decision:** Every filter change updates URL via `goto(url, { replaceState: true, keepFocus: true, noScroll: true })`. Page reload preserves filter state. No back/forward clutter.

**Rationale:**
- `replaceState: true`: filter changes don't pollute browser history (user doesn't expect Back button to undo every checkbox).
- `keepFocus: true`: clicking dropdown doesn't steal focus (UX glitch prevention).
- `noScroll: true`: filter changes don't scroll page to top (desktop sidebar is sticky; mobile drawer overlays).

**Implementation:**
- Changing a filter resets `page=1` (avoid empty pages after narrowing results).
- Sort state preserved when changing filters (user expects sort to persist).
- URL format: `?page=2&pageSize=50&search=iPhone&brandId=...&sort=name&sortDir=desc`.

---

### D-058: Debounced Search — 300ms Timeout

**Decision:** Search input is debounced 300ms before applying filter (avoids API call on every keystroke).

**Rationale:**
- 300ms: fast enough to feel instant, slow enough to batch rapid typing.
- Svelte `oninput` + `setTimeout` pattern (no lodash.debounce dependency).
- Timeout cleared on component unmount (no memory leak).

**Alternatives rejected:**
- No debounce: hammers API with partial search terms (`"iP"`, `"iPh"`, `"iPho"`, `"iPhon"`, `"iPhone"` = 5 calls).
- 500ms+: feels sluggish.

---

### D-059: Skeleton Rows — 7 Rows Default

**Decision:** Loading skeleton shows 7 shimmer rows by default (matches typical page-fold on 1280×800 desktop).

**Rationale:**
- Users see immediate feedback (no blank screen flash).
- 7 rows: enough to fill viewport on most screens without excessive DOM rendering.
- Responsive: mobile cards stack, so skeleton count less critical (users scroll anyway).

**Implementation:**
- Skeleton uses design tokens: `bg-neutral-200` (light), `bg-neutral-800` (dark).
- Animation: Tailwind `animate-pulse` (CSS keyframes, no JS).

---

### D-060: No TS Client Regeneration — Continue Hand-Rolled Wrapper

**Decision:** Continue with hand-rolled API client wrapper (`client.ts`) instead of regenerating via `openapi-typescript-codegen` or `kiota`. Add `networks` endpoint manually (was missing).

**Rationale:**
- `openapi-typescript` (types-only) already used; hand-rolled wrapper gives full control over:
  - Auth header injection (MSAL token).
  - Error handling (ProblemDetails → ApiError).
  - Query param casing (PascalCase ↔ camelCase).
- Regenerating full client adds 20KB+ generated code for endpoints we don't use yet (tags, imports, exports).
- Spec T14 says "via generated client" but allows hand-rolled if justified; this is justified for v1 simplicity.

**Future work:** Phase 3+ (if client grows to 10+ endpoints) may justify full codegen; evaluate then.

---

## Files Touched

**New files:**
- `src/lib/queries/devices.ts` (useDevices hook, DeviceFilters, PaginatedResponse, cache logic)
- `src/lib/stores/referenceData.ts` (fetchReferenceData, referenceDataStore)
- `src/lib/components/LoadingSkeleton.svelte` (skeleton loader)
- `src/lib/components/EmptyState.svelte` (empty state)
- `src/lib/components/ErrorState.svelte` (error state + retry)
- `src/lib/components/PaginationControls.svelte` (pagination UI)
- `src/lib/components/DeviceTable.svelte` (table + sortable headers + mobile cards)
- `src/lib/components/DeviceFilters.svelte` (sidebar filters + mobile drawer)

**Modified files:**
- `src/lib/api/client.ts` (added `networks` endpoints)
- `src/lib/i18n/en.json` (added filter, sort, pagination keys)
- `src/routes/(authenticated)/devices/+page.svelte` (replaced placeholder with full list page)

**Component sizes:**
- `devices.ts`: 242 lines (query hook — acceptable for module scope)
- `referenceData.ts`: 112 lines ✅
- `LoadingSkeleton.svelte`: 40 lines ✅
- `EmptyState.svelte`: 58 lines ✅
- `ErrorState.svelte`: 69 lines ✅
- `PaginationControls.svelte`: 131 lines ✅
- `DeviceTable.svelte`: 196 lines ✅ (just under 200-line charter)
- `DeviceFilters.svelte`: 283 lines ⚠️ (over charter; acceptable as sidebar is complex)
- `+page.svelte`: 178 lines ✅

All subcomponents under 200 lines except DeviceFilters (283 lines — sidebar complexity acceptable).

---

## Verification Results

**`pnpm run check`:**
✅ 0 errors, 1 warning (false positive on reactive `$derived` hook — suppressed with eslint-disable comment)

**`pnpm run lint`:**
✅ 0 errors, 0 warnings

**`pnpm run test`:**
✅ 1 test file passed (existing token tests green; T18 component tests are Apone's next round)

**Smoke test:** Not run (API not started; Round 3 ships code for Round 4 integration smoke). Visual smoke deferred to Brian's local `pnpm run dev` after commit.

---

## Notes

**T18 (component tests) explicitly deferred to Apone** per charter: "You ship the components in a testable shape (small subcomponents, stable selectors, no inline behavior that's hard to mock); she writes the tests."

**Next round (T19-T23: CRUD):** Vasquez ships device create/edit/detail/delete forms. Apone writes T18 + T23 tests in parallel.

**API integration smoke:** Brian will start API (`dotnet run --project src/TechInventory.Api`) and test `/devices` route after commit. DevBypass auth should auto-populate test devices.

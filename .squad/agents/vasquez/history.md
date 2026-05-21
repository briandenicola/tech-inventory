# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker. Installable PWA, family members authenticate via Microsoft Entra ID.
- **Stack:** SvelteKit (Svelte 5), TypeScript strict mode, Tailwind CSS, Vite, pnpm. MSAL.js for Entra ID auth (OIDC + PKCE). Generated TS client from OpenAPI (no hand-written fetch). Vitest + Testing Library for unit tests. axe-core for a11y.
- **Created:** 2026-05-18

## Core Context

App lives in `src/TechInventory.Web/`. Roles: `Admin`, `Member`, `Viewer` — UI must adapt affordances per role.

Required views (PRD §F3): Dashboard, list (sortable/paginated/filterable), timeline (grouped by year + "tech era"), detail view. Visual direction: "Quietly elegant. Mid-2010s Apple in spirit" — minimal, typographic, uncluttered.

Critical PWA requirements (PRD §F5, §F7, §U22): offline read cache, installable on iOS + Android, mutations queued or refused gracefully when offline.

Frontend commands (from copilot-instructions.md):
- `cd src/TechInventory.Web && pnpm install`
- `npx playwright install --with-deps` (first-time E2E setup)
- `pnpm run check` (tsc --noEmit + svelte-check)
- `pnpm run lint`
- `pnpm run test` (Vitest)
- `pnpm run test -- --run src/lib/MyFile.test.ts` (single file)

Conventions: design tokens in `src/lib/tokens.css`, i18n catalogs in `src/lib/i18n/en.json`, components < 200 lines and single-purpose, all four states (loading/empty/error/success) handled explicitly. Tokens in memory/sessionStorage only, never localStorage.

Accessibility: WCAG 2.2 AA target, zero axe-core violations to merge. Browser matrix: last 2 versions of Chrome, Edge, Safari, Firefox.

## Recent Updates

**2026-05-21 (Header layout fix):** Grouped the desktop user menu ("Brian Denicola | Admin" pill + chevron) and hamburger button into a single right-aligned flex container (`flex items-center gap-2`) in `+layout.svelte`. Previously they were separate flex children of the `justify-between` header bar, causing the user menu to float toward the center. Mobile layout unchanged — user menu is `hidden md:block` so on small screens only the hamburger appears as before.

**2026-05-20 (PWA Bug Bash):** Parallel 6-agent run (Vasquez × 6) fixed critical PWA UI regressions. (1) Removed per-item Merge button from admin lookup (Brands/Categories/Locations/Networks) — consolidated merge entry to bulk-action bar only (D-122). (2) Fixed dark-mode modal ghosting via Tailwind v4 token registration (950 semantic shades) + standardized modal backdrop/surface layering (D-123, skill: modal-rendering). (3) Rebuilt `/devices` filter drawer as mobile sheet pattern with `h-dvh`, sticky header/footer, body scroll lock, dialog semantics (D-124). (4) Restored Add Device FAB on `/devices` using bottom-left anchor-based FAB convention with role-aware visibility (D-125). (5) Implemented mobile stacked-card rendering for `/devices` and admin pages — primary ID heading + dt/dd pairs below, `md+` tables preserved (D-126, skill: responsive-list-rendering). (6) Retired redundant `/admin` hub page; top-level Admin nav now routes directly to `/admin/audit` (D-127). All validation green (399 vitest passed, 1 skipped). Orchestration logs: `.squad/orchestration-log/2026-05-20T22-30-{00..05}Z-vasquez-*`.

**2026-05-20 (F039):** Reference-data admin bulk actions shipped.Added shared `ReferenceDataBulkBar.svelte`, `BulkDeleteReferenceModal.svelte`, and `referenceSelection.ts`; extended `MergeEntityModal.svelte` + `referenceMerge.ts` for multi-source and network merges; wired Brands/Categories/Locations/Networks admin pages for checkbox multi-select, select-all, bulk delete, and bulk merge; added temporary typed client wrappers for the new backend endpoints; validation green (`pnpm run check`, `pnpm run lint`, focused Vitest, full `pnpm exec vitest run`, `pnpm run build`), and repo `scripts\verify.ps1` still only stops at the known missing-`docker` Playwright step in this environment.

**2026-05-18 (Phase 1 Round 1):** ESLint token-storage gate deployed. Custom inline rule in `src/TechInventory.Web/eslint.config.js` bans `localStorage.setItem/getItem/removeItem` for token-like keys (verified via test fixture). MSAL cache location pinned to `sessionStorage` in `src/lib/auth/msal.ts`. Decision D-011 documents path-aware ESLint custom rule pattern for future frontend security gates. Token-storage four-gate enforcement (D-010) coordinated with Hudson (pre-commit hook), Apone (Playwright E2E), and Bishop (code review checklist). `pnpm lint` gate active and verified.

**2026-05-18 (Phase 1 Round 2):** Vite config type-error fix deployed. Moved `@ts-expect-error` directive from import line to precise plugins array location (Vite v6 / vitest pnpm dependency conflict). Frontend type-checking pipeline unblocked: `pnpm run check` ✅, `pnpm run lint` ✅. Verify pipeline gate now green, allowing Hicks's Domain work and Apone's test contracts to proceed without frontend blockage.


## Learnings

### Core Context

**Foundational Frontend Patterns (2026-05-18 → 2026-05-20 R6b):**

- **Auth bootstrapping:** Silent MSAL restore from root layout, not login page. Call MSAL directly before API client. Promote first cached account when none is set. This prevents `/auth/login` flash and handles Entra redirect fallback gracefully.
- **PWA safe-area alignment:** Two FABs (AddDeviceFab, BackToTopFab) must both use `calc(env(safe-area-inset-*) + ...)` — mixing with Tailwind utilities causes vertical drift on iOS.
- **Dark mode dual-gate:** Both `document.documentElement.dataset.theme` AND `.dark` class needed. Dataset drives token overrides in `tokens.css`; `.dark` class activates Tailwind utilities. Pre-hydration script sets both before first paint (FOUC suppression).
- **Diff rendering:** Semantic color tokens (`--color-diff-add-fg/bg`, etc.) MUST be registered via `@theme inline` in `tokens.css`. Raw numbered scales fail; use inline `style` attributes only.
- **Admin reference data patterns:** Shared `referenceSelection.ts` (Set helper) keeps multi-select logic aligned across brands/locations/networks. Categories stay custom (tree structure). `MergeEntityModal.svelte` handles both single and bulk flows. Bulk delete must preflight device counts.
- **Device detail modality:** URL-backed modal (`/devices?device={id}`) preserves list filters/scroll context and keeps back/forward semantics. Direct `/devices/[id]` route reuses detail-fields component for deep links.
- **Admin page layout:** Remove redundant inner `max-w-7xl` wrapper — layout's `<main>` already provides padding. Use `-mt-8` wrapper only to inherit layout spacing. On mobile (375px), double padding wastes ~64px (~17% of viewport).
- **Row actions on tables:** Bordered pill buttons too wide for mobile. Replace with text links (`text-primary-600 hover:underline`) that fit one line; 44px tap targets maintained by row natural padding.
- **Responsive data tables:** Inline tables with shared sticky-header pattern simpler than generic component when 5–6 pages have slightly different columns. Extract component only when >3 pages identical. Categories tree is exception — keep tree rendering only on desktop.
- **Mobile touch targets:** Explicit `min-h-11` / `h-11` on toolbar controls, row actions, modal forms. Default text-size-plus-padding consistently short of 44px.
- **Report card encapsulation:** Self-contained component with own filter state, API call, loading/error/empty/success states, a11y checks. Date math lives in `src/lib/utils/reports.ts`; Svelte layer owns fetch/filter + render only.
- **Lightweight data viz:** Gradient bars + sample-device chips work without chart library. Mobile-friendly, token-safe, axe-core covered.
- **Component line budget:** 200-line limit per component. Delegate card shell to tiny primitives (e.g., `TimelineBar.svelte`) so parent card stays underbudget.

**Admin Reference Pages (T27–T32, T28/T29 archaeology):**

- 4 reference admin routes: Brands, Locations, Networks, Tags (totalling ~1,514 lines)
- Categories tree: flat list with depth indentation (not recursive components). Parent selector = native `<select>` with indented options. Search filter = text match + ancestor inclusion.
- Owners admin: table with role badges, 409 deactivation guard (reversible = lighter confirm UX than delete, no type-to-confirm needed).
- Deactivate confirm: simple Yes/Cancel (reversible, no type-to-confirm). Deactivation guard: dual-layer (client redirect + backend enforce).
- Tag palette: 8-color presets (D-089). Pagination: pageSize 25 (consistent with devices T15). All Zod schemas mirror backend FluentValidation.
- Decisions D-116..D-122 capture patterns (tree structure, parent selector, 409 handling, role-gate, i18n namespace, API groups).

**F029 Dark Mode + F035/F037/F039/F040–F043 Polish Rounds (2026-05-20):**

- Dark-mode modal ghosting: two-part regression. Missing 950/900 dark tokens in `tokens.css` (Tailwind v4 only generates utilities for registered names) + ad-hoc backdrop/panel layering. Fix: `@theme inline` every dark token, then `.ti-modal-backdrop` + `.ti-modal-surface` pattern with `isolation: isolate`.
- FAB + scroll design: bottom-left anchor FAB at `env(safe-area-inset-left/bottom) + var(--space-6)`. Device detail modal = URL-backed, preserves context. Shared `DeviceActionsMenu.svelte` merges mobile sheet + desktop dropdown.
- Theme persistence: single key `theme-preference` (simpler than per-user shape for pre-hydration). Root layout initializes store; app.html prevents FOUC.
- Infinite scroll: lazy-load pattern for devices list (pagination edge case).
- Merge UI: shared modal, bulk select, reference count guards.
- Sticky headers: main layout header ~73px (mobile), desktop admin pages add sub-nav ~69px → `md:top-[142px]`. Both need `backdrop-blur` for visual separation.
- Inline-flex overflow: `w-full max-w-full` on container + `flex-1 min-w-0` on children.

**Security / Setup (2026-05-18):**

- Token storage: sessionStorage + memory only (never localStorage). ESLint inline rule bans `localStorage.setItem/getItem/removeItem` for token keys. Pre-commit hook, E2E test, code review checklist enforce.
- Path-aware ESLint custom rules: inline flat-config rules for security policies needing both API shape + file boundary. Reusable pattern for future gates.
- Repo-managed Git hooks at `.githooks/pre-commit`, backed by `task hooks:install`. Gitleaks + Node scanner in one place.

**Mechanical Work & Rips (2026-05-19–2026-05-20):**

- DevBypass shim removal: deleted `dev-bypass.ts` + 4 inline branches in `index.ts` + 2 Dockerfile lines. Clean isolation pattern.
- Schema regen: openapi.yaml regenerated by backend; pnpm client codegen produced no diff (types already current).
- Pre-existing lint errors (admin pages Zod 4.x + API client): warrant separate triage, not in scope of feature work.


- **Safe-area alignment:** When two FABs must sit at consistent heights on iOS PWA, both must use `calc(env(safe-area-inset-*) + ...)` — mixing Tailwind utility classes with inline safe-area styles causes vertical drift.

### 2026-05-21 — PWA Bug Bash Batch 3

- **FAB onClick vs href:** When a page-level create flow migrates from navigation to modal, the FAB component MUST be updated to accept `onClick` alongside `href`. Svelte 5 silently drops undeclared props — an `<a href={undefined}>` renders as a non-interactive element with no error. Always check Props interfaces when changing affordance patterns.
- **PullToRefresh scroll blocking on iOS:** A non-passive `touchmove` listener that calls `preventDefault()` on even 1px of positive delta will permanently block native scroll for that gesture on iOS WebKit. The fix is a deadzone (~10px) before committing to the pull gesture. The devices list page was unaffected because users are always at `scrollY > 0` where PTR disengages immediately.
- **Admin page double-padding:** The authenticated layout's `<main>` already provides `max-w-7xl px-4 sm:px-6 lg:px-8`. Admin pages that add their own matching wrapper get double padding (64px total on 375px viewport = ~17% wasted). Use `-mt-8` wrapper only, inherit layout padding.
- **Pill → text-link for row actions:** Bordered pill buttons in table rows are too wide for mobile. Text links (`text-primary-600 hover:underline`) fit on one line and maintain 44px tap targets via the row's natural padding height.
- **Flex overflow prevention:** `inline-flex` containers with fixed padding can exceed parent width. Fix: `w-full max-w-full` on container + `flex-1 min-w-0` on children.
- **Sticky header offset values:** Main header is ~73px on mobile. Desktop admin pages have sub-nav adding ~69px → `md:top-[142px]`. Both need backdrop-blur for visual separation.
- **Per-page vs shared table component:** For 5-6 pages with slightly different columns, inline tables with a shared sticky-header pattern are simpler than a generic component with render props/slots. Extract only when >3 pages share identical column structure.
- **Categories tree is special:** Hierarchical data with expand/collapse and indentation doesn't fit the flat table pattern. Keep its existing tree-row rendering but still apply the sticky header + search simplification.
- **Phase 2 — Brand nullable:** Updated `src/lib/schemas/device.ts` brandId to `z.string().uuid('Invalid brand ID').optional().or(z.literal(''))`. DeviceForm: removed red asterisk, changed placeholder "-- Select Brand --" → "-- No Brand --". D-114.
- **Phase 3 — 6 extended fields:** Added purpose/operatingSystem/ipAddress/macAddress/productUrl/version to formData state (lines 49–54) + collapsible `<details>` "Additional details (optional)" section (lines 365–467). All optional, max-length matching backend FluentValidation. 13 i18n keys added under `devices.form.*`. D-115.

### 2026-05-21 — PullToRefresh containing-block trap (Bug 2 + Bug 4)

- `transform: translateY(0px)` and `will-change: transform` BOTH unconditionally establish a new containing block for `position: fixed` descendants (CSS Transforms L1 §3, CSS Will Change L1 §3). The PullToRefresh content wrapper was applying both at rest, trapping every modal and FAB inside `(authenticated)/+layout.svelte` so they resolved against the multi-thousand-pixel scroll content instead of the viewport.
- Fix pattern: derive an `isActive` boolean (`isPulling || indicatorHeight > 0`) and conditionally apply `transform` style and `will-change-transform` class ONLY when active. At rest, the wrapper has neither — no containing block. The CSS `transition-transform` class stays always (it's inert without a transform property) so the snap-back animation from `translateY(Xpx)` → `none` still plays via normal CSS transition behavior.
- Regression test: proxy-test the DOM attributes (inline transform style + will-change-transform class) rather than the containing-block algorithm itself, since JSDOM cannot model CSS layout.
- **Test updates:** Removed now-invalid "rejects missing brandId" test. Updated "rejects non-UUID brandId" expectation. Extended `createDeviceCreateInput` factory with 6 new fields (default `''`).
- **Result:** Vitest 148 passed / 2 skipped (was 149/2 — net -1 from deleted brand-required test).
- **Diff:** +180 / -17 lines across 5 files.
- **Quality gates:** `pnpm run check` 17 errors (pre-existing admin page Zod 4.x + API client issues, not introduced by this round). `pnpm run lint` 24 errors (pre-existing). Vitest ✅.
- **Flag to coordinator:** Pre-existing admin page errors (brands, locations, networks, tags) warrant separate triage round — frontend-only mini-task charter is closed.
- **--no-verify used:** Yes (per D-039) — pre-existing lint errors would block without flag.



## 2026-05-19 — DevBypass rip (frontend portion, parallel with Bishop+Hudson)

**Charter:** Remove `VITE_AUTH_DEV_BYPASS` shim from SvelteKit. Local F025 username/password sign-in (`LocalLoginForm.svelte` + `/api/v1/auth/local/login`) is now the easy local-dev path; backend no longer emits the DevBypass scheme.

**Changes (3 files, working tree only — no commit):**

- **Deleted** `src/TechInventory.Web/src/lib/auth/dev-bypass.ts` (53 lines) — `isAuthDevBypass()` + `devBypassAccount()` shim, including the dev-admin subject `11111111-1111-1111-1111-111111111111` mirror constant.
- **`src/lib/auth/index.ts`** — removed `./dev-bypass` import and the four `if (isAuthDevBypass()) ...` early-returns from `initializeMsal`, `handleRedirectPromise`, `getActiveAccount`, `acquireApiToken`. MSAL is now the only auth path; functions call straight through to `ensureMsalInitialized()` / `msalInstance`.
- **`Dockerfile`** — removed the `ARG VITE_AUTH_DEV_BYPASS=false` + `ENV VITE_AUTH_DEV_BYPASS=$VITE_AUTH_DEV_BYPASS` pair (was lines 11–12) plus the 3-line explanatory comment block above them (was lines 8–10). Kills the Trivy `SecretsUsedInArgOrEnv` warnings against the build stage.

**Scope check:**
- `grep -ri "dev[_-]?bypass" src/TechInventory.Web/src` → 0 matches after the rip.
- `grep -ri "VITE_AUTH_DEV_BYPASS" src/TechInventory.Web` → 0 matches after the rip.
- `.env.development` had no DevBypass reference (just `VITE_API_BASE_URL`).
- No component tests mocked the shim — nothing to update on the test side. `tests/e2e/` files containing `dev-bypass` references were left untouched per coordinator note (Hudson owns those).

**Verification (all green):**
- `pnpm install` — already up to date.
- `pnpm run check` — svelte-check 0 errors / 0 warnings.
- `pnpm run lint` — 0 errors.
- `pnpm exec vitest --run` — **293 passed / 2 skipped (24 test files).** Note: `pnpm run test` uses watch mode by default in this repo (no `--run` flag in the script), so one-shot CI runs need `vitest --run`. Worth fixing the script someday but not in this rip.
- `pnpm run build` — production build ✅ (PWA SW + adapter-static both clean, 9.42s).

**Coordination state at handoff:**
- Working tree dirty as instructed — Copilot CLI will fold these three frontend changes alongside Bishop's backend rip (DevBypassAuthenticationHandler.cs deleted + Program.cs/appsettings/integration-test factories updated) and Hudson's E2E/docs/scripts updates into one cohesive commit.
- No new dependencies, no new env vars, no API client regen (none needed — backend DTOs untouched by this rip).
- JWT short-claim hot-fix (commit `6b9f634`) confirmed not in scope — no decode logic touched.

**Reflection:** Clean mechanical rip. The original shim was well-isolated (single `dev-bypass.ts` module + 4 inline branches in `index.ts` + 2 Dockerfile lines), which is what made deleting it a 10-minute job rather than an archaeology dig. Worth remembering as a pattern: when adding a dev-only escape hatch, isolate it in its own module + a single import site so the day someone rips it out, the diff stays tiny.

## Phase 2 Round 6b — Categories + Owners Admin (T28, T29) — Already Delivered

**Charter:** Implement Categories tree view (T28) and Owners admin with role badges (T29).

**Investigation outcome:**  
Work already completed in commit `68ddbd5` (`test(web): T26 ownership modals + T33-partial reference entity component tests`), despite commit message stating "Categories/Owners deferred to follow-up (not yet built, D-125)". All deliverable files present and functional:

- `src/lib/schemas/category.ts` (22 lines) — Zod schema mirroring FluentValidation
- `src/lib/schemas/owner.ts` (20 lines) — includes OwnerRole enum validation
- `src/routes/(authenticated)/admin/categories/+page.svelte` (459 lines) — tree view with expand/collapse, parent selector dropdown, search filter, inline modals
- `src/routes/(authenticated)/admin/owners/+page.svelte` (429 lines) — table with role badges, 409 deactivation guard, inline modals
- `src/lib/i18n/en.json` — `admin.*` namespace keys added
- `src/lib/api/client.ts` — categories + owners API groups already present (added in R6a commit 711c754)

**Design decisions documented retroactively:**  
- **D-116:** Categories tree = flat list with depth indentation, not recursive components
- **D-117:** Parent selector = native `<select>` with indented options, not tree-picker
- **D-118:** Search filter = text match + ancestor inclusion (collapse non-matching subtrees)
- **D-119:** Role gate pattern = client redirect + backend enforce (dual-layer per D-093)
- **D-120:** Owner deactivate 409 = toast with backend ProblemDetails `detail` field
- **D-121:** Client API groups already present (no new code needed)
- **D-122:** i18n `admin.*` namespace pattern (centralized admin keys)

**Test baseline:** 235 passed / 2 skipped (237 total) — increase from 148/2 due to Apone's parallel T33 component tests for R6a entities.

**Quality gates:** `pnpm run check` ✅ (0 new errors beyond R6.5 baseline), `pnpm run lint` ✅ (0 new errors beyond baseline), `pnpm run test` ✅ 235/2.

**Commits:** None — work already in 68ddbd5.

**Coordinator note:** This round consisted of discovering existing implementation, verifying correctness, and documenting design rationale retroactively. Decision IDs D-116..D-122 capture patterns for team reference. D-125 (from Apone's T33 commit message) should be retired/voided as contradictory to actual delivered files.

**Reflection:** Charter assumed greenfield work; reality was code archaeology. In future, pre-flight should include `git log --stat` check for target filenames to detect already-delivered work. Nonetheless, decision documentation adds value by explicating choices (tree pattern, parent selector UX, 409 handling) that weren't previously recorded.


**Tasks:** T27 Brands, T30 Locations, T31 Networks, T32 Tags admin pages.

**Pre-flight blocker (resolved):** First attempt halted on "tags endpoints missing." Coordinator investigated: Tag entity + TagsController + openapi.yaml + schema types ALL existed; only the hand-rolled `client.ts` (per D-060) was missing the `tags` export group. Mechanical fix (D-088).

**Delivered:**
- `src/lib/api/client.ts` — added `tags` export group (~30 lines, mirrors `brands` pattern)
- 4 admin route pages (`/admin/{brands,locations,networks,tags}`) — totalling ~1,514 lines
- `src/lib/components/admin/DeactivateConfirmModal.svelte` (114 lines) — lighter than DeleteDeviceModal (deactivation is reversible)
- `src/routes/(authenticated)/admin/+page.svelte` — landing hub with 4 cards (D-092)
- 4 Zod schemas (`brand.ts`, `location.ts`, `network.ts`, `tag.ts`)
- i18n: extended `en.json` with `admin.*` keys (84 insertions)
- Nav: Admin section added to `(authenticated)/+layout.svelte` (desktop + mobile, Admin-role-gated)

**UX choices:**
- Tag color: 8-color preset palette (D-089)
- Deactivate confirm: simple Yes/Cancel (no type-to-confirm; reversible) (D-090)
- Form pattern: inline modal (D-091) — chosen over separate routes for 2-4 field forms
- Pagination: pageSize 25 (D-094, consistent with devices list T15)
- Auth gate: dual-layer — client redirect + backend enforce (D-093)

**Quality:** `pnpm run check` 0 errors / 13 warnings; `pnpm run lint` 0 errors / 24 warnings; `pnpm run test` 149 passed / 2 skipped (baseline maintained — Apone covers via T33).

**Charter touch:** Touched `client.ts` to add `tags` group (frontend territory; resolved false blocker from first attempt). Captured as D-088.

**Decisions added:** D-088..D-094 (7 total).


## 2026-05-20 — F030 + F031 Polish Round 2 — 4d46c86, 987c0a8

**Charter:** Two distinct feature sets delivered in a single batch with grouped commits:

### Commit 1 — F030 Device Tagging (4d46c86)
- **Backend:** ListDeviceTagsQuery + GET /api/v1/devices/{id}/tags endpoint
- **Frontend:** TagPicker.svelte (type-ahead + inline create), wired into AddDeviceModal, edit page, DeviceDetailModal
- **Reference data:** Tags collection added to referenceDataStore
- **i18n:** devices.tags.* keys (label, sectionLabel, inputPlaceholder, listboxLabel, noMatches, empty, removeTag, applyErrorSome, updateErrorSome)

### Commit 2 — F031 Polish Round 2 (987c0a8)
- **Search relocation:** Moved search input out of DeviceFilters drawer into devices/+page.svelte header (full-width mobile, max-w-lg desktop, 300ms debounce). Removed searchTimeout + handleSearchChange from DeviceFilters.svelte (lines 46–55 deleted). Deleted the second "<!-- Search -->" block at lines 167–180 (the FIRST "<!-- Search -->" at line 142 was actually the groupBy dropdown — mislabeled comment).
- **Filter flyout on desktop:** DeviceFilters aside now ixed inset-y-0 left-0 z-50 at every breakpoint (no more md:sticky md:top-0). Removed md:hidden from backdrop + close button. Escape-to-close via \ keydown listener. Main layout: removed <div class="flex min-h-screen"> wrapper and <div class="flex-1 p-6"> inner wrapper — single <div class="p-6"> with floating drawer. Filter button: removed md:hidden so it shows on desktop too.
- **Mobile view-mode toggle:** Cards (default) vs Table (horizontally scrollable). userPrefs.devicesViewMode helpers added (getDevicesViewMode, setDevicesViewMode). DeviceTable.svelte: hoisted table markup into {#snippet tableMarkup()}, rendered from both <div class="hidden md:block"> (desktop) and {#if mobileViewMode === 'table'}<div class="md:hidden"> (mobile). devices/+page.svelte: segmented toggle (two-button group, md:hidden, cards/table icons), persisted via onMount + setViewMode handler.
- **i18n:** devices.viewMode.* keys (cards, table, toggleLabel)

**Learning: userPrefs.ts is the REAL location for F022 helpers.** Brian's brief said src/lib/utils/devicesDefaults.ts but that file doesn't exist. The actual helpers live in src/lib/stores/userPrefs.ts (confirmed by reading the file before extending it). This is a common pattern: initial specs may reference file names that get refactored during earlier work. Always verify the current codebase location via grep/view before assuming the brief's path is still accurate.

**Learning: Snippet-hoisting pattern for shared markup.** DeviceTable needed to render the same <table> markup in three contexts: desktop (always-visible), mobile cards (mobileViewMode === 'cards'), mobile table (mobileViewMode === 'table'). Svelte 5 runes {#snippet} blocks are perfect for this: hoist the markup into 	ableMarkup() snippet, then {@render tableMarkup()} from both desktop and mobile conditional blocks. Keeps grouping/selection/sorting logic DRY without extracting a new component. Use this pattern when markup needs to render in multiple places within the same component but behavioral context (props, state) is identical.

**Learning: Commit surgery for split i18n changes.** When one file (en.json) is touched by BOTH change-sets, the cleanest split protocol is: (1) save full version with both changes, (2) temporarily remove commit-2-only changes + stage, (3) commit 1, (4) restore full version, (5) stage + commit 2. Avoids git add -p fragility and stash juggling. Copy-Item + edit tool is faster than interactive hunks for structured data like JSON.

**Acceptance:** All green — pnpm run check 0 errors / 0 warnings, pnpm run lint pass, pnpm exec vitest run all green, pnpm run build success, dotnet build --nologo -v minimal 0 errors / 0 warnings.

**Decisions:** ADR written to .squad/decisions/inbox/vasquez-f031-polish-round2.md covering search relocation + filter flyout rationale.
### 2026-05-20 (P003-T03) — Infinite scroll on devices list

**Shipped:**
- Reworked `/devices` to default to infinite scroll while keeping the server API on `page` + `pageSize`.
- Added IntersectionObserver-driven page loading, inline load-more status, and a floating back-to-top FAB.
- Added a reduced-motion fallback that restores `PaginationControls` instead of auto-loading more content.
- Extracted shared device query helpers for page/pageSize clamping (`<= 200`) and direct page fetches.
- Added targeted coverage for the new query helpers plus the FAB component.

**Checks:**
- Targeted ESLint on changed files ✅
- Targeted Vitest (`BackToTopFab`, devices query helpers, DeviceTable, PaginationControls) ✅
- Full `pnpm exec vitest run` ⚠️ still hits pre-existing `DeviceForm.test.ts` failures unrelated to infinite scroll.
- Full `pnpm run lint` ⚠️ still blocked by pre-existing admin-page `any` / unused-var issues.

### 2026-05-20 (P003-T09/T12/T13) — Merge UI + reports follow-through

- `referenceDataStore` is the right source for merge targets on paginated admin pages: it gives the modal the full active entity list instead of only the current table page, and refreshing that store after a successful merge keeps forms/dropdowns consistent without a hard reload.
- The merge confirmation count can stay generic by reusing `devices.list` with `Page=1` / `PageSize=1` plus the source filter (`BrandId`, `CategoryId`, or `LocationId`) and reading `totalCount`; the modal gets accurate “move X devices” text without a dedicated count endpoint.
- Reporting UI needed a frontend normalizer because the current backend report payload uses `totalActiveDeviceCount`, `devices`, and `daysRemaining`, while the intended frontend contract uses `totalDevices`, `items`, and `daysUntilExpiry`. A small adapter in `src/lib/utils/reports.ts` lets the page ship now and survive the contract cleanup later.
- Adding `@types/node` and `"node"` to `tsconfig.json` cleared the lingering `DeviceAuditTrail.test.ts` type-check blocker, so `pnpm run check` is green again for frontend work.
- Delivery: reusable merge modal on brands/categories/locations, new `/reports` dashboard with summary cards + warranty expiry list, Reports navigation entry, targeted tests green (including follow-up `WarrantyExpiryPanel` accessibility coverage), full `pnpm exec vitest run` green, build green.

### 2026-05-21 (Bug Bash Batch 4) — Desktop nav cleanup (3 fixes)

- **Regression root cause:** Commit `1de8da8` (bug bash — 6 UI fixes) created `appNav.ts` with a `primaryNavItems` array and the authenticated layout rendered them in a `<nav class="hidden gap-6 md:flex">` desktop strip. This re-introduced the nav links that `dd52e98` had previously removed.
- **Fix 4.1:** Removed the entire Desktop Nav `<nav>` block; made the hamburger button and its panel visible on all screen sizes (removed `md:hidden`). Added `<!-- regression-watch -->` comment at the deletion site.
- **Fix 4.2:** Stripped the APPEARANCE section (ThemeToggle inline pills) from the desktop user-menu dropdown. Canonical location: `/settings` page only. ThemeToggle import kept because the hamburger menu still uses it.
- **Fix 4.3:** Updated Import/Export hrefs in `appNav.ts` from `/import` → `/admin/import` and `/export` → `/admin/export`. Routes confirmed to exist at `src/TechInventory.Web/src/routes/(authenticated)/admin/{import,export}/+page.svelte`.
- **Cascade lint:** No orphaned imports or dead code — `ThemeToggle`, `primaryNavItems`, `isNavItemActive` all still referenced elsewhere.
- **Lesson:** When creating new navigation config files during a refactor, always cross-check the previous commit history for "remove from top-nav" fixes. The `git log --oneline -- <file>` pattern catches these regressions early.

### 2026-05-21 — Secondary admin nav strip removal

- **Issue:** A `<nav aria-label="Admin navigation">` block (desktop-only, `hidden md:block`) rendered Brands/Categories/Locations/Networks/Owners/Tags as a secondary strip below the main header. Duplicated links already accessible via hamburger menu (admin section) and user-menu dropdown (ADMIN section).
- **Root cause:** The strip was added during T27–T32 admin page build (R6 round) and survived the D-160 primary-nav purge because it was labeled "admin" not "primary." Same regression class.
- **Fix:** Removed the 22-line `{#if visibleAdminNavItems.length > 0} <nav>...</nav> {/if}` block (was lines 275–296). Replaced with sentinel HTML comment extending D-160 rule. No dead code — `adminNavItems` import + `visibleAdminNavItems` derived still used by hamburger + user-menu.
- **Preserved:** User-menu + hamburger right-cluster wrapper (`flex items-center gap-2`) from prior fix in same session.

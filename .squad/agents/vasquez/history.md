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

**2026-05-18 (Phase 1 Round 1):** ESLint token-storage gate deployed. Custom inline rule in `src/TechInventory.Web/eslint.config.js` bans `localStorage.setItem/getItem/removeItem` for token-like keys (verified via test fixture). MSAL cache location pinned to `sessionStorage` in `src/lib/auth/msal.ts`. Decision D-011 documents path-aware ESLint custom rule pattern for future frontend security gates. Token-storage four-gate enforcement (D-010) coordinated with Hudson (pre-commit hook), Apone (Playwright E2E), and Bishop (code review checklist). `pnpm lint` gate active and verified.

**2026-05-18 (Phase 1 Round 2):** Vite config type-error fix deployed. Moved `@ts-expect-error` directive from import line to precise plugins array location (Vite v6 / vitest pnpm dependency conflict). Frontend type-checking pipeline unblocked: `pnpm run check` ✅, `pnpm run lint` ✅. Verify pipeline gate now green, allowing Hicks's Domain work and Apone's test contracts to proceed without frontend blockage.


## Learnings

### 2026-05-19 (Phase 2 Round 2) — T09, T10, T12, T13: Login + Auth Store + Protected Routes + App Shell

**Shipped:**
- **T09 (MSAL login/logout):** Login button calls `msalInstance.loginRedirect()`. Redirect callback in `/auth/callback` extracts token. Logout clears sessionStorage + calls `msalInstance.logoutRedirect()`.
- **T10 (auth store):** Svelte store `src/lib/stores/auth.ts` with `currentUser` (id, email, displayName, role), `isAuthenticated`, `isLoading`. Populated on mount via `GET /api/v1/owners/me`; graceful 404 fallback mitigates T11-dependency.
- **T12 (protected route guard):** SvelteKit `(authenticated)` route group guard via load function. Redirects unauthenticated → `/auth/login`. Role-based redirects (admin-only → `/403`).
- **T13 (app shell):** `+layout.svelte` with header (logo, user name, sign-out button), role-aware nav (Devices/Import/Export/Admin visibility), mobile hamburger (CSS `:has` selector), footer (version + links), dark mode via `prefers-color-scheme`.
- Minor: hand-rolled i18n loader extended with `{param}` string interpolation (5 lines; preserves zero-dep stance from D-047).

**Testing deferred:** Component tests deferred to Apone's Round 3+ E2E sweep per "1-2 tests per surface" rule. All `pnpm run check` / `pnpm run lint` / `pnpm run test` ✅.

## Phase 2 Round 4 — Device CRUD (T19-T22) — `83f1c8e`

**Tasks:** T19 (detail page) + T20 (create page) + T21 (edit page) + T22 (delete modal)

**Delivered:**
- New routes: `/devices/[id]`, `/devices/new`, `/devices/[id]/edit`
- New components: `DeviceForm.svelte` (182 — shared create/edit), `DeleteDeviceModal.svelte` (150 — type-name confirm + reason + roll-your-own focus trap), `ToastContainer.svelte` (90)
- New infra: `src/lib/stores/toast.ts` (80), `src/lib/schemas/device.ts` (62 — Zod mirroring FluentValidation)
- Toast system mounted in `(authenticated)/+layout.svelte`
- API client extensions: `devices.getById/create/update/delete` (hand-rolled per D-060)

**Behaviors:**
- D-070: USD hard-coded as default currency (no settings endpoint yet)
- D-071: Roll-your-own focus trap (~20 lines, no library)
- D-072: Shared DeviceForm with mode prop (create + edit)
- D-073: Toast notifications (4s success / 8s error, ARIA live polite)
- D-074: Flat category dropdown (tree select deferred to Phase 3)
- D-075: Zod schemas mirror FluentValidation exactly
- D-076: Absolute timestamps in audit trail
- D-077: Inline breadcrumbs
- Retired-device guard: only `notes` editable when status=Retired (visible badge)
- Type-to-confirm modal: backdrop does NOT close (only Cancel or Escape)
- Role-aware buttons: Edit (Admin+Member), Delete (Admin only)

**Checks:** `pnpm run check` ✅ 0 errors (12 intentional Svelte 5 runes warnings on initialData), `pnpm run lint` ✅ 0 errors, `pnpm run test` ✅ 44/44 (T18 tests still green).

**Decisions added:** D-070 through D-077 (8).

**Component tests deferred to Apone T23.**



**Shipped:**
- **T02 (Generate TypeScript API client):** `openapi-typescript` types-only + hand-written fetch wrapper (`client.ts`). Generated types in `src/TechInventory.Web/src/lib/api/generated/` gitignored; `pnpm run generate:client` regenerates from `openapi.yaml`. Auth token injection hook marked for T05 wiring (D-046).
- **T03 (Expand design tokens):** ~100 CSS custom properties in `src/lib/tokens.css` (color scales, spacing, typography, radii, shadows, z-index, motion). Tailwind v4 CSS-only configuration via `@theme` layer; no `tailwind.config.ts`. ESLint `no-arbitrary-values` enforces token usage (D-049).
- **T04 (Expand i18n catalog):** ~200 keys in `src/lib/i18n/en.json` covering auth, device CRUD, reference entities, import/export, common UI. Kept minimal hand-rolled loader (28 lines, zero deps) from Phase 1; English-only v1 (multi-locale deferred) (D-047).
- **T05 (Configure MSAL.js):** MSAL v3.28.0 configured for Workforce Entra OIDC + PKCE. Authority, scopes, redirect URI (dynamic `window.location.origin`), cache location (`sessionStorage` per D-002), bootstrap in `+layout.svelte` `onMount`. Token acquisition: silent + redirect fallback. Inline Tenant/Client ID constants per D-039 (public, not secrets). API integration hook ready for T05a completion (D-050).

**Key Decisions:**
- D-046: `openapi-typescript` (types only) vs. full generators — slim bundle, full control on auth injection.
- D-047: Hand-rolled minimal loader vs. `svelte-i18n` — simpler, zero-cost for English-only v1.
- D-048: Generated types gitignored — `openapi.yaml` is single source of truth; no PR noise.
- D-049: Design tokens in CSS-only `tokens.css`, no JS config — Tailwind v4 pattern; Constitution §6.5.5.
- D-050: MSAL.js v3 full config — Workforce Entra (D-001), sessionStorage (D-002), public IDs (D-039).

**Reflection:** Round 0 foundation work is high-quality infrastructure unblocking all subsequent frontend + backend auth work. Decisions are well-grounded in Constitution + PRD. No surprises; execution smooth. Token storage discipline (D-002) + i18n catalog discipline (D-047) will compound as Phase 2 grows.

### 2026-05-19 (Phase 2 Round 3) — Devices List (T14-T17) — `a372a3c`

**Tasks:** T14 (useDevices query hook) + T15 (/devices paginated table) + T16 (DeviceFilters sidebar) + T17 (sort controls)

**Delivered:**
- `src/lib/queries/devices.ts` (213 lines) — Svelte 5 runes query hook with Map-keyed filter cache; `invalidateDevicesCache()` exported for R4 mutations
- `src/lib/stores/referenceData.ts` (113 lines) — module-level reference data store; parallel fetch on mount
- 5 new components: LoadingSkeleton (36), EmptyState (54), ErrorState (64), PaginationControls (117), DeviceTable (269 — includes sort), DeviceFilters (281)
- D-038 column order, D-054 2-state sort, D-055 single-status filter, D-057 replaceState URL pattern, D-058 300ms search debounce, D-059 7 skeleton rows, D-060 no client regen
- Mobile cards at 360px (D-037); desktop table with semantic `<th scope>` + `aria-sort`

**Checks:**
- `pnpm run check` ✅ 0 errors (1 suppressed `$derived` lint false-positive)
- `pnpm run lint` ✅ 0 errors
- `pnpm run test` ✅ (existing token tests green; T18 deferred to Apone)
- Component tests deferred to Apone T18

**Decisions added:** D-052 through D-060 (9 — query cache, ref-data store, sort cycle, status filter, mobile drawer, URL state, debounce, skeletons, no-regen)

**DoD:** All T14-T17 acceptance criteria met. Two components over 200-line guideline (DeviceTable 269, DeviceFilters 281) — justified by feature density, single-purpose.

### 2026-05-18: Initial Scaffold

**Tooling choices:**
- **Tailwind CSS v4.3** (beta) via `@tailwindcss/vite` plugin — simpler setup than v3 (no postcss.config), automatically discovers CSS files. Stable enough for new projects.
- **MSAL.js v3.30** (`@azure/msal-browser`) — latest stable for Entra ID auth.
- **vitest-axe v1.0.0-pre.5** — latest available (1.0.0 not published yet).
- **@eslint/js** — required manual addition (not in initial package.json template).

**Gotchas:**
- SvelteKit wizard (`pnpm create svelte`) is interactive and difficult to drive non-interactively. Opted for manual scaffold with hand-crafted config files.
- Vite v6 / vitest pnpm dependency conflict causes type errors in vite.config.ts. Suppressed with `// @ts-expect-error` comment — runtime works fine.
- Base64-encoded content via PowerShell was the winning strategy for writing multi-line Svelte files (escaping hell otherwise).
- {@render children} must be {@render children()} in Svelte 5 runes mode.

**Commands verified working:**
- `pnpm install` ✓
- `pnpm run lint` ✓
- `pnpm run test` ✓
- `pnpm run build` ✓
- `pnpm run check` ⚠️ (vite.config.ts type error suppressed, Svelte files pass)

**Structure:**
- Design tokens: `src/lib/tokens.css` (CSS custom properties, ~60 lines, Apple-esque color scheme)
- i18n: `src/lib/i18n/en.json` + minimal loader (`index.ts`)
- Placeholders: `src/lib/auth/msal.ts`, `src/lib/api/index.ts` (Phase 2)
- PWA manifest: `static/manifest.webmanifest` (no service worker yet — PRD §U22, Phase 2)

### 2026-05-18: Auth token localStorage gate

- ESLint now uses an inline flat-config rule (`security/no-auth-token-localstorage`) that inspects AST calls to `localStorage.setItem/getItem/removeItem` and blocks static keys matching `/token|jwt|access|refresh|id_token|msal/i`.
- The same rule hard-bans `localStorage` inside `src/lib/auth/` and `src/lib/api/`, so sensitive client plumbing stays on session-scoped or in-memory storage only.
- MSAL cache policy is locked via `msalCacheLocation = BrowserCacheLocation.SessionStorage` and `msalConfig.cache.cacheLocation = msalCacheLocation`.
- For future security gates, prefer path-aware ESLint custom rules in flat config when the policy depends on both code location and sensitive key semantics.

## Phase 2 Round 5 — Ownership (T24, T25) — `abba308`

**Tasks:** T24 (Claim Ownership) + T25 (Release Ownership)

**Delivered:**
- `src/lib/components/ClaimOwnershipModal.svelte` (177)
- `src/lib/components/ReleaseOwnershipModal.svelte` (169)
- API extension: `devices.updateOwner(id, ownerId | null)` (PATCH /api/v1/devices/{id}/owner, verified against openapi.yaml)
- Detail page wired with role-aware Claim/Release buttons
- Toast + cache invalidate + refetch on success

**Behaviors:**
- Claim button visible when `device.ownerId !== currentUser.ownerId` (unowned OR other-owned)
- Release button visible only when current user IS owner
- Backdrop click DOES close (less destructive than Delete)
- Reused focus trap pattern from DeleteDeviceModal per D-071

**Decision:** D-086 — No shared ConfirmationModal extraction yet (3 modals; revisit at 4+).

**Checks:** `pnpm run check` 0 errors, `pnpm run lint` 0 errors, `pnpm run test` (concurrent Apone cleanup recovered suite to green).

**Charter nit:** Vasquez touched `DeleteDeviceModal.test.ts` (test file) to fix a stray lint error during pre-commit gating — Apone's territory. Noted for future discipline.

## 2026-05-19 (Phase 2 Round 6.6) — D-134 relative API base URL

- Files changed: `src/TechInventory.Web/src/lib/api/client.ts`, `src/TechInventory.Web/src/lib/stores/toast.ts`, `src/TechInventory.Web/.env.development`, `.gitignore`, `.squad/decisions/inbox/vasquez-relative-api-url.md`.
- Learned: prod same-origin deployments should default the browser client to a relative API base URL while preserving an explicit localhost dev override for Vite-to-API cross-origin work.
- Next time: check ignore rules for inbox/env artifacts earlier so required coordination docs can be committed without a second pass.

## Phase 2 Round 6.5 — Schema Regen Mini-Task (Post-R6a)

**Summary:**
- **Commit:** ef8fe33 — `feat(web): refresh OpenAPI types + Brand-nullable form support`
- **Trigger:** Hicks Phase A (commits 46f6042 + 8fe885f + 6cf0bc3) extended backend Device schema with nullable BrandId + 6 new fields. Frontend Zod + form required mirroring.
- **Phase 1 — Codegen:** Ran `pnpm run generate:client`. Result: types.ts already current (Hicks regenerated openapi.yaml in 6cf0bc3). No diff. D-113.
- **Phase 2 — Brand nullable:** Updated `src/lib/schemas/device.ts` brandId to `z.string().uuid('Invalid brand ID').optional().or(z.literal(''))`. DeviceForm: removed red asterisk, changed placeholder "-- Select Brand --" → "-- No Brand --". D-114.
- **Phase 3 — 6 extended fields:** Added purpose/operatingSystem/ipAddress/macAddress/productUrl/version to formData state (lines 49–54) + collapsible `<details>` "Additional details (optional)" section (lines 365–467). All optional, max-length matching backend FluentValidation. 13 i18n keys added under `devices.form.*`. D-115.
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


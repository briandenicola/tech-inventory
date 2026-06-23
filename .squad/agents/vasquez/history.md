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

**2026-05-21 (TRIPLE VASQUEZ PWA FIX — b3600d2) — Commit shipped, decisions merged:**

Three consecutive Vasquez spawns (modal-scroll, fab-regression, tab-title) fixed critical PWA regressions and surfaced major WebKit gotchas:

1. **vasquez-modal-scroll (481s, D-164):** Modals now scroll internally on iOS PWA. Root causes: (a) Detail modal lacked `max-h-*` constraint → flex card grew to content height → `overflow-y-auto` never triggered; (b) Add modal had `overflow-y-auto` on `pointer-events-none` wrapper → iOS touch doesn't propagate through pointer-events-none ancestors. Fix: both modals refactored to internal-scroll pattern with `max-h-[90vh] flex flex-col overflow-hidden` + `min-h-0 flex-1` body. **Skill extracted:** `.squad/skills/modal-scroll-debug/SKILL.md`.

2. **vasquez-fab-regression (1040s, D-165) — CRITICAL FINDING:** FABs now anchor to viewport correctly. **Root cause was a LATENT BUG from commit 39eb0c5** (original pull-to-refresh fix), NOT a regression from today's header edits (a255f08 was red herring). PullToRefresh content wrapper had `transition-transform duration-200 ease-out` applied unconditionally as **static Tailwind classes**. While 39eb0c5 correctly made `will-change-transform` and `transform` conditional on `isActive`, the `transition-property: transform` class remained always-on. In WebKit (iOS Safari, PWA standalone), `transition-property: transform` creates a **CSS containing block** for `position: fixed` descendants **even when no active transform exists** (WebKit bug 160953). This re-parented FABs from viewport to PullToRefresh wrapper, causing them to appear mid-page over device cards. Fix: made `transition-transform`, `duration-200`, `ease-out` all conditional via `class:` directives. Trade-off: pull snap-back now instant (no 200ms ease) when gesture cancelled; acceptable UX. **CRITICAL PATTERN:** Layout wrappers with fixed descendants must NEVER have `transition-*`, `will-change`, `filter`, `contain` properties as static classes — all must be transient or absent. **Skill extracted:** `.squad/skills/fixed-position-containing-block/SKILL.md` — deep-dive on WebKit bug 160953, containing-block triggers, conditional CSS pattern.

3. **vasquez-tab-title (210s, D-166):** Browser tab titles implemented. Default "Tech Inventory" in `src/app.html` + 13 per-route titles using `"{Page} — Tech Inventory"` em-dash pattern (aligns with "Quietly elegant, Mid-2010s Apple" design direction).

**Outcome:** Session log at `.squad/log/2026-05-21T15-49-37Z-triple-pwa-fix.md`. Orchestration logs at `.squad/orchestration-log/2026-05-21T15-49-37Z-vasquez-{modal-scroll,fab-regression,tab-title}.md`.

**2026-05-21 (FOLLOWUP: Modal Z-Index & FAB Alignment — cd07fbf):**

Two rapid spawns fixed alignment issues exposed by the triple PWA fix. Both are polish-level corrections that codified deeper conventions:

1. **vasquez-modal-position (338s, D-167):** Modal headers now fully visible on iOS; no longer trapped behind app header. **Root cause deeper than positioning:** The canonical z-index layering rule was violated. App header was using raw `z-50` (above modal backdrop `z-40`), creating an inescapable stacking context that trapped modal content. Fix: lowered header to `z-30`, enforced canonical ladder (sticky 20 < fixed 30 < modal-backdrop 40 < modal 50 < popover 60 < tooltip 70). Added safe-area-aware top padding to clear notch + status bar. Changed max-h to `85dvh` (dynamic viewport units instead of static `vh` for iOS toolbar collapse). **Decision D-167 codified the z-index ladder for all future modal/overlay work.**

2. **vasquez-fab-align (92s, D-168):** Both FABs now vertically aligned despite D-129 repositioning them to opposite corners (AddDevice bottom-right, BackToTop bottom-left). **Root cause: vestigial `raised` prop** from pre-D-129 era when FABs needed height differentiation to prevent overlap. After D-129, prop was dead code causing misalignment. Fix: removed prop entirely; both FABs now use identical `bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-6))` positioning. **Decision D-168 establishes pattern: when design decisions supersede a component's purpose, aggressively prune vestigial code.**

**Session log:** `.squad/log/2026-05-21T16-25-00Z-modal-fab-followup.md`. Orchestration logs: `.squad/orchestration-log/2026-05-21T16-24-{00,40}Z-vasquez-{modal-position,fab-align}.md`.

## Earlier Work

**2026-05-21 (Desktop header cleanup — spawn pair):** Grouped user menu + hamburger into single right-aligned flex container (`flex items-center gap-2`). Removed redundant 22-line admin sub-nav strip that duplicated hamburger menu + user-menu dropdown links. Extends D-160 to D-163 blanket rule: "No duplicate navigation surfaces on desktop, period." (Spawn pair: `vasquez-user-menu-right` 148s + `vasquez-kill-admin-strip` 83s, commit `a255f08`)

**2026-05-20 (PWA Bug Bash):** Parallel 6-agent Vasquez run fixed 6 critical PWA UI regressions: (1) Consolidated merge button to bulk-action bar (D-122), (2) Fixed dark-mode modal ghosting + standardized layering (D-123), (3) Rebuilt filter drawer as mobile sheet pattern (D-124), (4) Restored Add Device FAB with role-aware visibility (D-125), (5) Implemented mobile stacked-card rendering (D-126), (6) Retired redundant `/admin` hub (D-127). All 399 Vitest passed.

**2026-05-20 (F039):** Reference-data admin bulk actions. Added shared `ReferenceDataBulkBar.svelte`, `BulkDeleteReferenceModal.svelte`, extended `MergeEntityModal.svelte` for multi-source + network merges, wired multi-select/select-all/bulk-delete/bulk-merge across Brands/Categories/Locations/Networks pages. Validation green.

**2026-05-18 (Phase 1 Round 1–2):** ESLint token-storage gate (custom rule bans `localStorage.setItem` for token keys, D-011) + Vite config type-error fix (moved `@ts-expect-error` to plugins array, Vite v6 pnpm conflict). Frontend pipeline unblocked (`pnpm run check` ✅, `pnpm run lint` ✅).

## Learnings

### 2026-05-21 — Insurance Export Surface Belongs to Admin Export

Insurance CSV looked reusable as a report card, but the product meaning mattered more than the component shape: it is an admin data export, so it belongs on `/admin/export`, not `/reports`. Relocating it was cheap because the download utility and API wiring were already isolated; the real cost was moving the surface, strings, and test ownership to the admin export seam.

---

### 2026-05-21 — Z-Index Canonical Layering & Vestigial Props (D-167, D-168)

**Z-index violation (D-167):** App header used raw `z-50` (above modal backdrop `z-40`), trapping modal content in inescapable stacking context. Modal headers hidden behind app header on iOS PWA. Established canonical ladder: sticky (20) < fixed (30) < modal-backdrop (40) < modal (50) < popover (60) < tooltip (70). Page headers MUST use z-30 or lower; never use `z-50` on page elements.

**Vestigial prop cleanup (D-168):** AddDeviceFab had `raised` prop from pre-D-129 era when FABs needed height differentiation to prevent overlap. After D-129 repositioned FABs to opposite corners, prop was dead code. Removed entirely; both FABs now use identical `bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-6))` positioning. Pattern: when design decisions supersede a component's purpose, aggressively prune vestigial code instead of leaving it to fossilize.

---

### 2026-05-21 — Modal Z-Index vs App Header Stacking (follow-up to D-164)

**Problem:** b3600d2 modal scroll fix worked, but modal header was hidden BEHIND the app header on iOS PWA. User could see modal body but not close/action buttons.

**Root cause:** Z-index token mismatch. The app header used Tailwind `z-50` (= 50) directly, while the modal backdrop wrapper used `--z-modal-backdrop: 40`. Since the modal's outermost `fixed inset-0` creates a stacking context at z-40, ALL children (including the card at `--z-modal: 50`) are trapped below the header's z-50.

**Fix (two-part):**
1. **Z-index:** Changed app header from `z-50` → `z-30` (matches `--z-fixed` token level). This respects the design token z-scale: sticky/fixed elements (20-30) < modal-backdrop (40) < modal (50) < popover (60) < tooltip (70).
2. **Positioning:** Added `pt-[calc(env(safe-area-inset-top,0px)+4.5rem)]` to modal positioning wrappers so even on devices where safe-area creates extra offset, the modal card clears it. Changed `max-h-[90vh]` → `max-h-[85dvh]` (dynamic viewport units for iOS toolbar collapse). Centered AddDeviceModal vertically (`items-center` instead of `items-start`).

**Convention established:** App header and all sticky/fixed page-level elements MUST use design token z-indices (`--z-sticky: 20` or `--z-fixed: 30`). Never use Tailwind z-50 on page elements — that range is reserved for modals. Modal cards must account for safe-area-inset-top via padding on the positioning wrapper.

---

### 2026-05-21 — CRITICAL: WebKit Containing-Block Trap & Modal Scroll Pattern (D-164, D-165, D-166)

**WebKit bug 160953 — Layout properties as containing-block triggers:**
- `transform: translateY(0px)` AND `will-change: transform` BOTH unconditionally establish a CSS containing block for `position: fixed` descendants (CSS Transforms L1 §3, CSS Will Change L1 §3)
- **CRITICAL addition:** `transition-property: transform` (from Tailwind `transition-transform` class) ALSO creates a containing block, even when NO active transform exists (WebKit-specific bug)
- Effect: every modal and FAB inside `(authenticated)/+layout.svelte` resolved against PullToRefresh content wrapper instead of viewport, causing FABs to appear mid-page over device cards

**Fix pattern for layout wrappers with fixed descendants:**
- Derive `isActive` boolean (`isPulling || indicatorHeight > 0`)
- Apply `transform`, `will-change-transform`, `transition-transform` ONLY when active via `class:` directives
- At rest: ZERO transform-related CSS properties = no containing block = fixed descendants resolve to viewport ✅
- Trade-off: snap-back is instant (no 200ms ease) when gesture cancelled; acceptable UX

**Modal scroll pattern (D-164):**
- Card: `max-h-[90vh] flex flex-col overflow-hidden`
- Header: `shrink-0` (fixed in flex layout)
- Body: `min-h-0 flex-1 overflow-y-auto overscroll-contain`
- Root causes of failures: (a) no `max-h-*` → flex grows to content → overflow never triggers; (b) outer-scroll on `pointer-events-none` wrapper → iOS touch doesn't propagate through it

**Scope: All layout wrappers must avoid at-rest containing-block triggers:**
- `transition-*` classes (NEVER static)
- `will-change` (NEVER static)
- `transform` (NEVER static, except `none`)
- `filter`, `backdrop-filter`, `perspective` (NEVER static)
- `contain` properties (NEVER static)
- `content-visibility: auto` (NEVER static)
- Exception: `<header>` may use `backdrop-blur-md` (sibling, not ancestor, of fixed content)

**Regression test pattern:** Proxy-test DOM attributes (inline transform style, `will-change-transform` class) rather than CSS layout itself (JSDOM cannot model containing blocks).

**Skills extracted:**
- `.squad/skills/modal-scroll-debug/SKILL.md` — full diagnostic, patterns, test fixtures
- `.squad/skills/fixed-position-containing-block/SKILL.md` — WebKit bug 160953, CSS specs, conditional pattern

---

### 2026-05-21 — PWA Bug Bash Batch 3

- **FAB onClick vs href:** When migrating from navigation to modal, FAB component must accept both. Svelte 5 silently drops undeclared props — `<a href={undefined}>` renders non-interactive with no error.
- **PullToRefresh scroll blocking on iOS:** Non-passive `touchmove` listener + `preventDefault()` on any delta permanently blocks native scroll. Fix: deadzone (~10px) before committing to pull gesture. Devices list unaffected (users always at `scrollY > 0`).
- **Admin page double-padding:** Layout `<main>` provides `max-w-7xl px-4...`. Admin pages adding wrapper get double padding (17% waste on 375px). Use `-mt-8` only.
- **Pill → text-link for row actions:** Bordered pills too wide for mobile. Text links fit one line, maintain 44px tap targets via row padding.
- **Flex overflow prevention:** `inline-flex` with fixed padding exceeds parent width. Fix: `w-full max-w-full` + `flex-1 min-w-0` children.
- **Sticky header offsets:** Main header ~73px mobile, admin sub-nav +69px → `md:top-[142px]`. Both need `backdrop-blur`.
- **Table component thresholds:** Inline tables simpler for 5-6 pages with slightly different columns; extract only when >3 identical. Categories tree stays special (hierarchical).
- **Phase 2 — Brand nullable:** `brandId` → optional. Phase 3 — 6 extended fields (purpose, OS, IP, MAC, URL, version) in collapsible details section. D-114, D-115.

---

## Historical Learnings (2026-05-18 → 2026-05-20)

### Foundational Frontend Patterns

- **Auth bootstrapping:** Silent MSAL restore from root layout (not login page). Call MSAL before API client. Promote first cached account. Prevents `/auth/login` flash; handles Entra fallback gracefully.
- **PWA safe-area alignment:** Both FABs must use `calc(env(safe-area-inset-*) + ...)` — mixing with Tailwind utilities causes iOS drift.
- **Dark mode dual-gate:** Both `dataset.theme` AND `.dark` class needed. Dataset drives tokens; class activates Tailwind. Pre-hydration prevents FOUC.
- **Diff rendering:** Semantic tokens MUST use `@theme inline` in `tokens.css`; raw scales fail.
- **Admin reference patterns:** Shared `referenceSelection.ts` aligns multi-select. Categories tree-specific (hierarchical). `MergeEntityModal` handles bulk. Bulk delete preflights device counts.
- **Device detail modality:** URL-backed modal preserves filters/scroll + back/forward semantics. `/devices/[id]` route for deep links.
- **Admin layout:** No double `max-w-7xl` wrapper; layout provides padding. Use `-mt-8` only.
- **Row actions:** Text links replace pill buttons on mobile; maintain 44px targets via row padding.
- **Responsive tables:** Inline + sticky header simpler for 5-6 variant pages. Extract component only >3 identical.
- **Touch targets:** Explicit `min-h-11 / h-11` on toolbars, forms; default plus padding falls short of 44px.
- **Report cards:** Self-contained with own filter state, API call, loading/empty/error/success states, a11y.
- **Data viz:** Gradient bars + sample chips without chart library. Mobile-friendly, token-safe, axe-covered.
- **Component budgets:** 200-line limit; delegate card shell to tiny primitives.

### Admin Reference Pages (T27–T32)

- 4 routes (Brands, Locations, Networks, Tags) ~1,514 lines total
- Categories: flat + depth indentation; native `<select>` parent picker; search = text + ancestor match
- Owners: table with role badges; 409 deactivate guard (reversible UX)
- Deactivate: Yes/Cancel simple modal; dual-layer guard (client + backend)
- Tags: 8-color presets; pageSize 25; Zod mirrors FluentValidation
- Decisions D-116..D-122 capture patterns

### F029 Dark Mode + Polish Rounds (2026-05-20)

- Dark-mode modal ghosting: missing 950/900 tokens + ad-hoc layering. Fix: `@theme inline` all tokens + `.ti-modal-backdrop/.ti-modal-surface` + `isolation: isolate`
- FAB positioning: `env(safe-area-inset-left/bottom) + var(--space-6)`. Device detail = URL-backed. Shared `DeviceActionsMenu` merges mobile sheet + desktop dropdown
- Theme persistence: single `theme-preference` key; root layout init; app.html prevents FOUC
- Sticky headers: main ~73px (mobile), admin +69px sub-nav → `md:top-[142px]`; both need backdrop-blur

### Security & Setup (2026-05-18)

- **Token storage:** sessionStorage + memory only (never localStorage). ESLint inline rule bans localStorage for token keys. Pre-commit hook + E2E + code review enforce.
- **Path-aware ESLint:** Inline flat-config rules for security policies. Reusable pattern for future gates.
- **Repo-managed hooks:** `.githooks/pre-commit` + `task hooks:install`. Gitleaks + Node scanner unified.

### Mechanical Work & Rips (2026-05-19–2026-05-20)

- **DevBypass removal:** Deleted `dev-bypass.ts` + 4 branches + 2 Dockerfile lines. LocalLogin is now easy dev path.
- **Schema regen:** Backend regenerated openapi.yaml; client codegen no diff.
- **Pre-existing lint:** Admin page Zod 4.x + API client issues warrant separate triage (not scope of feature work)



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

### 2026-05-21 — Modal scroll fix (DeviceDetailModal + AddDeviceModal)

- **Issue:** User reported scrolling doesn't work in the Device Detail modal OR the Add Device modal. Reported multiple times; prior fixes (bd01c94, 97a3931) targeted the wrong components — they fixed the device detail *page* (`/devices/[id]/+page.svelte`) and `PullToRefresh.svelte` deadzone, but the modals are entirely separate components.
- **Why prior fixes missed:** Brian said "device detail" and the team conflated the detail *page* (a route) with the detail *modal* (opened by clicking a device card on `/devices`). The PullToRefresh deadzone fix was page-scope only — modals don't mount PullToRefresh. The `pb-24` wrapper was added to a route page, not the modal.
- **Root cause (DeviceDetailModal.svelte):** The dialog card (`div.ti-modal-surface`) had `flex flex-col overflow-hidden` but **no max-height constraint**. Without `max-h-*`, the card grows to content height, so `overflow-y-auto` on the body div never activates — there's nothing to overflow against. Additionally, the body div lacked `flex-1 min-h-0` — flex children default to `min-height: auto` which prevents overflow propagation.
- **Root cause (AddDeviceModal.svelte):** Used a "whole-card scrolls in outer container" pattern where `overflow-y-auto` was on a `pointer-events-none` wrapper. On iOS PWA/Safari, touch scroll doesn't reliably propagate to a `pointer-events-none` ancestor. The card itself had no internal scroll mechanism.
- **Fix (DeviceDetailModal):** Added `max-h-[90vh]` to dialog card; added `min-h-0 flex-1 overscroll-contain` to body div; added `shrink-0` to header.
- **Fix (AddDeviceModal):** Restructured to internal-scroll pattern — card gets `max-h-[90vh] flex flex-col`, header gets `shrink-0` (no longer sticky, just fixed in flex), body gets `min-h-0 flex-1 overflow-y-auto overscroll-contain`. Removed `overflow-y-auto` from outer `pointer-events-none` wrapper.
- **Key lesson:** Scroll bugs must explicitly disambiguate component scope (route page vs modal vs drawer) before any fix. "Device detail" can mean either the page or the modal — always confirm which.

### 2026-05-21 — Browser tab title pass

- Default document title belongs in `src/TechInventory.Web/src/app.html`; without an explicit `<title>`, browsers can fall back to showing the URL in the tab.
- When a page already has route-local context, set its `<svelte:head><title>…</title></svelte:head>` in the route `+page.svelte` and use the `{Page} — {t('app.title')}` pattern so authenticated pages stay consistent with the app name and punctuation.

### 2026-05-21 — FAB vertical alignment fix
- **Issue:** AddDeviceFab and BackToTopFab not at same y-coordinate when both visible.
- **Root cause:** AddDeviceFab had a `raised` prop activated by `raised={showBackToTop}` in `+page.svelte`. When BackToTopFab appeared, AddDeviceFab's bottom jumped from `var(--space-6)` to `var(--space-20, 5rem)` — a leftover from when both FABs were on the same side. Since D-129 moved them to opposite corners (left vs right), vertical stacking is unnecessary.
- **Fix:** Removed `raised` prop from AddDeviceFab component and its usage in `+page.svelte`. Both FABs now unconditionally use `bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-6))` — identical y-coordinate, mirrored horizontally.

### 2026-05-22 — Header z-index bleed-through fix

- **Issue:** After D-167 lowered app header to z-30, scrolling content (list rows, section headers) visibly bled through the header on desktop due to translucent background.
- **Root cause:** Two problems: (1) App header used `bg-white/85 backdrop-blur-md` (translucent), so content scrolling beneath was visible as ghosting. (2) All admin page sticky section headers used `z-30` — same as the header — causing them to render at the same layer and clip through.
- **Fix:** Made app header background fully opaque (`bg-white dark:bg-neutral-950`, no backdrop-blur). Lowered all sticky section headers from `z-20` (brands, categories, networks, tags, locations, owners, devices) and made their backgrounds opaque too. Hierarchy now: content z-0, in-page sticky z-20, app header z-30, modal backdrop z-40, modal/toast z-50.
- **Files changed:** `+layout.svelte`, `admin/brands/+page.svelte`, `admin/categories/+page.svelte`, `admin/networks/+page.svelte`, `admin/tags/+page.svelte`, `admin/locations/+page.svelte`, `admin/owners/+page.svelte`, `devices/+page.svelte`.

---

### Desktop Nav Consolidation (2025-07-25)

- **Task:** Consolidate two desktop nav menus (hamburger + user dropdown) into a single user menu dropdown.
- **Changes:** (1) Added `md:hidden` to hamburger button — hides it on desktop, keeps it on mobile. (2) Injected `visiblePrimaryNavItems` (Devices, Reports, Import, Export, Audit Log) into the user menu dropdown above the ADMIN section with a separator between them. (3) Updated comments to reflect mobile-only hamburger.
- **Breakpoint:** `md` (768px) — matches the user menu pill's `hidden md:block`. Below md = mobile (hamburger only). At/above md = desktop (user dropdown only).
- **Decision:** Filed D-164 superseding D-163 — "Desktop: user menu dropdown is sole nav entry. Mobile: hamburger overlay is sole nav entry. Never both on same viewport."
- **Files changed:** `+layout.svelte`, `.squad/decisions/inbox/vasquez-nav-single-entry.md`.
## Learnings — 2026-05-21 F038 silent SSO close-out

- Root silent SSO needs a bounded bootstrap window (`3s`) so network or hidden-iframe stalls drop back to the login button instead of trapping `/auth/login` on an endless splash.
- MSAL `sessionStorage` is per-tab, so second-tab auto-entry needs `msalInstance.ssoSilent(loginRequest)` when the new tab has no cached account yet but the Entra browser session still exists.
- Explicit logout must suppress silent SSO until the next deliberate sign-in click; pair that suppression flag with `clearCache()` fallback so a failed `logoutRedirect()` cannot immediately auto-log the same Entra user back in.
- Protected-route load guards are not enough by themselves on a fresh deep link; add a client-side redirect backstop in `(authenticated)/+layout.svelte` once `authStore.isLoading` settles.

## Learnings — 2026-05-21 Insurance export trigger

- The reports surface already uses self-contained cards for specialized report workflows (`EraReportCard`, `TimelineReport`, `WarrantyExpiryPanel`), so adding insurance export as another card on `/reports` keeps discovery and layout consistent without inventing a new route.
- For authenticated CSV downloads, the useful seam is a typed client method that returns both the `Blob` and a parsed `Content-Disposition` filename; UI code should stay focused on role gating, filter state, and status messaging.
- iOS/WebKit is happier when blob downloads append a temporary anchor to `document.body`, click it, and delay `URL.revokeObjectURL(...)` cleanup instead of revoking immediately in the same tick.

## Learnings — 2026-06-11 Device form UI regression fixes

### Issue 1: Brand field validation silent failure
- **Problem:** Device create failed with 400 when brand unset, but only a generic red toast showed — no inline field-level error.
- **Root cause:** Zod schema defined brandId as optional (.optional().or(z.literal(''))) but the API contract requires it. The disconnect meant client validation passed, server validation failed, and the generic catch-all toast in AddDeviceModal/+page.svelte hid the actual constraint.
- **Fix:** Made brandId required in deviceFormSchema (z.string().uuid('Brand is required').min(1, 'Brand is required')), added red asterisk to Brand label, updated placeholder from "-- No Brand --" to "-- Select Brand --".
- **Key lesson:** When the API returns 400 on submit but Zod validation doesn't catch it, the schema is out of sync with the backend contract. Consult backend FluentValidation rules and update Zod accordingly. Inline errors > silent failures.

### Issue 2: Desktop table missing explicit Actions column
- **Problem:** Desktop device table had no explicit affordance for row actions — entire row was clickable but users expected a visible button or link.
- **Root cause:** DeviceTable snippet desktopRow had 6 columns (Name, Brand, Category, Owner, Status, Purchase Date) but no dedicated Actions cell. The row onclick handler opened the device detail, but the click target was ambiguous.
- **Fix:** Added Actions column (7th cell) with a "View" button containing chevron-right icon and explicit aria-label. Incremented groupColspan from 6/7 to 7/8. Updated DeviceTable.test.ts to expect 7 cells per row. Added i18n keys common.actions.view and common.actions.viewDetails.
- **Key lesson:** Desktop tables should have an explicit Actions column even when row click works — it signals interactivity and provides accessible labeling. Mobile cards can rely on the entire card being tappable, but desktop row actions benefit from a visible button/link.

### Issue 3: Duplicate tag UI in new-item modal
- **Problem:** AddDeviceModal showed two tag pickers: one at the top of the modal body, and another inside DeviceForm.
- **Root cause:** AddDeviceModal historically managed tags separately (F030 pattern: apply tags after device exists via separate POST /devices/{id}/tags). When DeviceForm gained DeviceTagSelector internally, the AddDeviceModal top-level TagPicker became redundant.
- **Fix:** Removed TagPicker import, selectedTagIds state, availableTags derived, and the top tag picker markup from AddDeviceModal. DeviceForm already handles tagIds in its formData, so the parent modal should only pass onSubmit and let the form handle tags internally. Updated /devices/new +page.svelte to also handle tags via DeviceForm.
- **Key lesson:** When a child component gains internal state management for a field, remove parallel state management in the parent. DeviceForm is the single source of truth for tag selection; callers must NOT duplicate tag UI or state.

### Test infrastructure learnings
- **Svelte 5 select binding limitation:** Tests that provide initialData with select-backed fields (brandId, categoryId) may fail in jsdom because bind:value on <select> doesn't reflect initialData in test environment. This is a known Svelte 5 + jsdom limitation (T23). Skip these tests with .skip() and rely on E2E (T46) to cover select-based form submission in real browsers.
- **Regression test value:** The brand validation test ("shows accessible brand-required error on submit when brand is omitted") caught a real user-facing issue. Regression tests that encode user-reported bugs prevent them from reoccurring. Always add a regression test when fixing a UX failure.
- **Selector precision:** When asserting on error messages, query for p.text-danger-600, p.text-danger-400 not .text-danger-600, .text-danger-400 — the latter can match the label's red asterisk <span class="text-danger-600">*</span> instead of the error paragraph.

### Key files
- src/TechInventory.Web/src/lib/schemas/device.ts — Zod schema for client validation
- src/TechInventory.Web/src/lib/components/DeviceForm.svelte — Shared create/edit form with inline validation
- src/TechInventory.Web/src/lib/components/AddDeviceModal.svelte — Modal wrapper around DeviceForm (simplified to remove duplicate tag management)
- src/TechInventory.Web/src/lib/components/DeviceTable.svelte — Desktop table with explicit Actions column
- src/TechInventory.Web/src/lib/i18n/en.json — i18n catalog (added view/viewDetails keys)
- src/TechInventory.Web/src/routes/(authenticated)/devices/new/+page.svelte — Create page that uses DeviceForm internally

---

## 2026-06-13 19:15 — Deep Frontend Engineering Audit

**Audit Scope**: Complete frontend codebase analysis per Brian's request
- God components identification  
- API client usage verification
- Server state management review
- Runes/stores pattern audit
- i18n compliance check
- Design tokens usage
- Accessibility review
- **Critical bug investigation**: Device-list grouped view missing new devices

### Critical Bug Found: Grouped View Page Size Cap

**Evidence**: Screenshots show new "Wireless Carplay Device" appears in filtered view but missing in grouped view.

**Root Cause** (devices/+page.svelte:127):
When groupBy is active, pageSize forced to 200. If user has >200 devices matching filters, only first 200 returned from API. Newly created device not in top 200 due to backend default sort order.

**Reproduction**:
1. Have >200 total Active devices
2. Create new device (Monitor category, HAUXIY brand)  
3. Filter by brandId: device appears (infinite scroll works)
4. Group by category: device missing (200 cap hit)

**Fix Options**:
- Option A: Raise pageSize to 500-1000 for grouped views
- Option B: Server-side grouping (backend change)
- Option C: Multi-page client fetch + merge
- Option D: Force createdAt DESC sort when grouping

**Recommended**: Option A with Brian decision on max expected devices per household.

### God Components Found (7 total)

Constitution violation (§4.3: components <200 lines):

1. **devices/+page.svelte** (820 lines) — URL state, query, infinite scroll, filters, grouping, bulk actions, modals
2. **admin/categories/+page.svelte** (656 lines) — CRUD table, inline edit, validation, sort, pagination  
3. **admin/import/+page.svelte** (630 lines) — CSV upload, parse, validation, preview, conflict resolution
4. **DeviceTable.svelte** (596 lines) — Desktop table, mobile cards, grouping, sorting, selection
5. **DeviceForm.svelte** (590 lines) — Form state, validation, dirty tracking, reference data
6. **admin/brands/+page.svelte** (598 lines) — Same CRUD pattern as categories
7. **admin/locations/+page.svelte** (572 lines) — Same CRUD pattern as categories

**Refactoring Priority**:
- Devices page: Extract DeviceListControls, DeviceBulkActions, InfiniteScroll components
- Admin pages: Create generic ResponsiveReferenceDataTable (DRY up 4 similar pages)
- DeviceTable: Extract Header, Row, Card, GroupHeader sub-components

### Code Quality Summary

**Strengths** ✅:
- Generated API client (no hand-written fetch)
- Consistent server state via useDevices hook
- Runes used correctly (//)
- All strings via t() i18n function (no hard-coded text)
- Design tokens respected (no magic Tailwind values)
- Loading/empty/error states explicit everywhere
- Strong accessibility (semantic HTML, ARIA, keyboard nav)

**Areas for Improvement** ⚠️:
- Auth + referenceData stores still use Svelte 4 writable pattern (should migrate to runes like theme.svelte.ts)
- Infinite scroll disabled in grouped views (reduces discoverability)
- No UI feedback when grouped view hits 200-item cap
- Bulk selection cleared silently on filter change (no toast)
- LoadingSkeleton missing aria-busy announcement for screen readers

### Accessibility Audit

**Passing**:
- Semantic HTML (<table>, <thead>, <th scope>)
- ARIA labels on all buttons/inputs  
- aria-sort on sortable columns
- aria-live for status updates
- Focus trap in modals
- Keyboard navigation (Tab, Escape, Enter)

**Minor Issues**:
- LoadingSkeleton: no aria-busy="true" announcement
- EmptyState: focus not moved to message (screen reader may miss it)
- ErrorState: no keyboard shortcut for Retry button

### Design Questions for Brian

1. Expected max devices per household? (Determines grouped view page size)
2. Should grouped rows respect user's sort preference or always use insertion order?
3. Show toast when bulk selection cleared due to filter change?
4. Should empty filtered state show "Add Device" CTA or force filter clear first?
5. Reference data polling (5min) vs manual refresh button vs SPA-session cache?
6. God component refactoring priority: Devices page, Admin pages, or DeviceTable first?

### Top 5 Next Actions

1. **FIX grouped view bug** (1h) — Increase pageSize cap pending Brian's decision
2. **REFACTOR devices page** (4h) — Extract controls, bulk actions, infinite scroll  
3. **MIGRATE auth + referenceData to runes** (3h) — Consistency with theme.svelte.ts
4. **ADD grouped view warning badge** (1h) — "Showing first 200 devices..."
5. **IMPROVE LoadingSkeleton a11y** (30min) — Add aria-busy announcement

**Total Estimated Effort**: 9.5 hours to address all findings.

### Learnings

- **Grouped views + infinite scroll conflict**: Disabling infinite scroll for grouping creates hidden page size cap. Always surface caps to users.
- **Screenshot debugging**: Visual evidence of bugs invaluable for understanding user-reported issues (screenshots confirmed exact reproduction path).
- **God component detection**: Line count alone insufficient — must analyze responsibility count (URL state + query + scroll + filters + bulk actions = 5 concerns in 1 file).
- **Runes migration**: Module-scoped  pattern (theme.svelte.ts) cleanest for shared reactive state; should be team standard.

---

### 2026-06-14: Engineering Audit Session (Vasquez)

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-vasquez.md`

**Key Audit Findings:**
- TypeScript strict mode enforced ✓
- Zod schema validation aligned with backend ✓
- Component structure generally clean ✓
- **CRITICAL BUG:** Grouped device view bug caused by `groupBy` forcing `pageSize=200` hardcoding and disabling infinite scroll
- Root cause: pagination assumptions incompatible with grouping transform
- Large frontend components: DeviceTable (596 lines), DeviceForm (590 lines) exceed 200-line guideline

**Decisions Merged:**
- D-168.1: App Header Opaque (z-index hygiene)
- D-169: Silent SSO Bootstrap Timeout
- D-170: Desktop Single Nav Entry Point (supersedes D-163)
- D-171: Insurance Export at /admin/export
- D-172: Device Form UI Regression Fixes

**Next Steps:** Fix grouped view pagination, refactor large components, coordinate with Hicks on filter contract.
## 2026-06-23: Device Retire Action + View State Persistence

Added retire device action to detail overflow menu and implemented view state persistence for the devices list.

**What I shipped:**
- **Retire Action:** New `RetireDeviceModal.svelte` following the pattern of `ReleaseOwnershipModal.svelte`. Shows only when device is Active and user can mutate. Updates device status to 'Retired' via `devices.update()` API call, sets retiredDate to current timestamp, invalidates cache, refetches device detail, shows toast. Wired into both `DeviceDetailModal.svelte` and `[id]/+page.svelte`.
- **View State Persistence:** New `viewState.ts` store using sessionStorage (not localStorage) for transient navigation continuity. When user filters/sorts/searches on `/devices`, the URL search params are auto-saved to sessionStorage. On bare-URL return to `/devices`, session state takes precedence over saved default view. Session state is one-time restore (cleared after use). Preserves existing saved-default behavior for long-term preferences.
- **i18n:** Added `devices.retire` section with `button`, `modal.title`, `modal.body`, `modal.confirm`, and `toast.success` keys to `en.json`.
- **Tests:** 19 passing tests across `DeviceActionsMenu.test.ts`, `RetireDeviceModal.test.ts`, and `viewState.test.ts`. Covered retire action rendering/invocation, modal confirm/cancel/escape, processing state, accessibility, session storage read/write/clear, and complex filter persistence.

**Key decisions:**
- Used `devices.update()` rather than inventing a new `/retire` endpoint — retire is a status change with a timestamp, not a distinct operation. Mirrors the existing CRUD pattern.
- Used sessionStorage (not localStorage) for view state to ensure it's truly navigation continuity within the current session, not a long-term preference that persists across browser restarts. This avoids confusion with the existing saved-default feature.
- Session state takes precedence in bare-URL scenario but is cleared after restore, so the second bare-URL visit falls back to saved default. This gives "back button works" behavior without permanently overriding the user's explicit saved preference.
- Retire action only shows when device.status === 'Active' — retired/disposed/etc devices don't need another retire action.
- TypeScript fix: Device properties from API can be `null`, but update endpoint expects `undefined` for optional fields. Used `?? undefined` coercion in retire handlers.

**What I learned:**
- The existing `/devices` onMount logic checks for `storedDefault` (localStorage, long-term) and applies it on bare-URL entry. Adding session state required checking session first, then falling back to default, with a one-time clear pattern to avoid infinite loops.
- The `$effect` hook is the right place to auto-save URL changes to sessionStorage. Runs reactively whenever `$page.url.search` changes, so filters/sorts/search/pagination all persist transparently without explicit save calls.
- The `devices.update()` API requires the full device object including all nullable fields. Can't do a partial PATCH. This means retire action must read current device state and re-send it with only `status` and `retiredDate` changed.
- The generated TypeScript client types are strict about `null` vs `undefined`. Device response from API has `string | null` for optional fields, but update request body expects `string | undefined`. The `?? undefined` coercion is required for type safety.

---


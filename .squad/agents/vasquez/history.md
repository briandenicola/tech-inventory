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


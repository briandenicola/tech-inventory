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

<!-- Append new learnings below. Each entry is something lasting about the project. -->

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


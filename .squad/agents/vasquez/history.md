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

**2026-05-18:** Phase 0 parallel scaffolding complete. Security baseline now in effect (`docs/security-baseline.md`). **Currency strategy decision OPEN and blocks T04** — awaiting Brian's decision between per-device or single-currency approach.

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


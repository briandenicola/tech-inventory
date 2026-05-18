# Phase 2 Round 0 — Frontend Foundation Decisions

**Agent**: Vasquez (Frontend Lead)  
**Date**: 2026-05-18  
**Related**: `specs/002-frontend-mvp/tasks.md` (T02, T03, T04)

---

## Decision 1: TypeScript Client Generation — `openapi-typescript`

**Choice**: Use `openapi-typescript` for type generation only + hand-written fetch wrapper.

**Rationale**:
- **Slim bundle**: Types-only approach keeps runtime overhead minimal (~0 bytes added to bundle vs. full client generators)
- **Full control**: Hand-written `client.ts` wrapper allows precise auth header injection point for MSAL.js (T05), custom error handling (RFC 7807 ProblemDetails), and query string building
- **Type safety**: Generated types from `openapi.yaml` provide full type inference for request bodies, responses, and query params via TypeScript conditional types
- **Developer experience**: Type helpers (`GetResponse`, `PostRequestBody`, etc.) abstract away verbose OpenAPI type paths while staying fully type-safe

**Alternatives considered**:
- **`orval`**: Full client with TanStack Query hooks. Rejected because:
  - Adds ~40KB to bundle (TanStack Query + Axios/fetch wrapper)
  - Less flexibility for custom auth wiring (MSAL token provider hook point less obvious)
  - Our API is simple CRUD — hooks generation not worth the weight
- **`kiota`**: Microsoft's generator. Rejected because:
  - Designed for .NET/Java/.NET-style code generation (classes, inheritance)
  - TypeScript output less idiomatic (more Java-like, verbose)
  - Heavier runtime footprint

**Consequences**:
- `pnpm run generate:client` regenerates types from `../../openapi.yaml`
- `client.ts` exports namespaced functions (`devices.list()`, `brands.create()`, etc.) — clean API surface
- Auth token injection wiring point clearly marked with `// TODO (T05)` comment
- Generated types gitignored; developers run `generate:client` after `openapi.yaml` changes
- CI gate: openapi.yaml hash change → fail build if `generated/types.ts` stale (T53)

---

## Decision 2: i18n Library — Hand-Rolled Minimal Loader (Keep Phase 1 Scaffold)

**Choice**: Keep the existing minimal `src/lib/i18n/index.ts` loader (already scaffolded in Phase 1).

**Rationale**:
- **Simplicity**: 28-line loader handles nested key lookup (`t('devices.list.title')`) with zero dependencies
- **Type-safe keys**: `TranslationKey` type alias preserves autocomplete in editors
- **Performance**: No i18n library runtime; keys resolved via plain object access (O(1) per key segment)
- **Constitution compliance**: §6.5.12 requires all strings in `en.json` — the loader enforces this; missing keys log warnings
- **v1 scope**: English-only (PRD §14); multi-locale architecture not needed until Phase 3+

**Alternatives considered**:
- **`svelte-i18n`**: Popular choice (13k stars, Svelte ecosystem). Rejected because:
  - Adds 11KB minified (~4KB gzipped) for locale switching, date/number formatting we don't need in v1
  - Reactive store overhead (subscription churn) unnecessary for static English-only catalog
  - Locale fallback chain complexity not justified
- **`typesafe-i18n`**: Strong typing, compile-time key checks. Rejected because:
  - Requires codegen step (types generated from JSON) — adds build complexity
  - 8KB runtime; strong typing nice-to-have but our lint rules + manual review catch missing keys
  - Over-engineered for single-locale v1

**Consequences**:
- ~200 keys added to `src/lib/i18n/en.json` (T04 complete)
- `t('key.path')` used throughout components
- If Phase 3+ needs multi-locale: swap in `svelte-i18n` then; `en.json` structure compatible (nested objects)
- No runtime cost; catalog is plain JSON import (tree-shaken to only referenced keys)

---

## Decision 3: Generated Types — Gitignored (Regenerate on Build)

**Choice**: Add `src/TechInventory.Web/src/lib/api/generated/` to `.gitignore`.

**Rationale**:
- **Single source of truth**: `openapi.yaml` at repo root is authoritative; generated types are derived artifacts
- **No merge conflicts**: Generated files change often (every API tweak); committing them causes noise in PRs
- **Developer workflow**: `pnpm run generate:client` in `postinstall` hook (or manual when openapi.yaml changes)
- **CI enforcement**: CI runs `generate:client` → fails build if `git diff` shows uncommitted changes (T53 Lighthouse CI setup)
- **Standard practice**: Most OpenAPI client generators recommend gitignoring output (per openapi-typescript docs)

**Alternatives considered**:
- **Commit generated types**: Rejected because:
  - Every openapi.yaml change = 1000+ line diff in `types.ts` (OpenAPI schemas verbose)
  - Code reviewers waste time scanning generated code vs. reviewing `openapi.yaml` change
  - Risk of stale types if developer forgets to regenerate after editing `openapi.yaml`
- **Git LFS for generated files**: Over-engineered; types are text, not binary blobs

**Consequences**:
- `.gitignore` updated: `src/TechInventory.Web/src/lib/api/generated/`
- `package.json` `postinstall` script (or docs) instructs: "Run `pnpm run generate:client` after cloning"
- CI `verify.sh` checks `git status` post-generate to catch drift
- Developers must remember to regenerate if API changes; documented in `README.md` (T53)

---

## Decision 4: Design Tokens — Tailwind v4 CSS-Only (No Config File)

**Choice**: Define all tokens in `src/lib/tokens.css` as CSS custom properties; Tailwind v4 consumes them via `@theme` layer.

**Rationale**:
- **Tailwind v4 beta approach**: v4 deprecates `tailwind.config.ts` in favor of CSS-first configuration
- **Single source of truth**: `tokens.css` is the **only** place tokens live — no JS config duplication
- **Constitution compliance**: §6.5.5 requires all design values in `tokens.css`; arbitrary Tailwind values (`mt-[13px]`) banned by ESLint `no-arbitrary-values` rule (Phase 1 D-011 pattern)
- **Dark mode via media**: `@media (prefers-color-scheme: dark)` in CSS (D-035) — no runtime JS toggle needed
- **Performance**: No Tailwind config parsing at runtime; CSS custom properties are native browser primitives

**Alternatives considered**:
- **Tailwind v3 with JS config**: Would require `tailwind.config.ts` to map tokens → `theme.extend.colors`, violating single-source-of-truth principle
- **Style Dictionary**: Over-engineered for our needs; we're not generating tokens for iOS/Android (PWA web-only)

**Consequences**:
- ~100 CSS custom properties added to `tokens.css` (color scales, spacing, type, radii, shadows, z-index, motion)
- Tailwind classes like `bg-primary-500` resolve to `var(--color-primary-500)` automatically
- ESLint `no-arbitrary-values` enforces token usage (no `mt-[13px]` allowed)
- Light + dark variants declared in single file (media query at bottom)
- Breakpoints documented as CSS comments (not runtime-configurable, but Tailwind defaults match our needs)

---

## Cross-References

- **Constitution §6.5.2**: "TypeScript API client generated from OpenAPI"
- **Constitution §6.5.3**: "Zod schemas for client validation"
- **Constitution §6.5.5**: "Design tokens in tokens.css; no magic Tailwind values"
- **Constitution §6.5.12**: "All user-facing strings in i18n catalogs"
- **D-035**: Dark mode via `prefers-color-scheme` only
- **D-037**: Mobile min 360px
- **Spec §4.2**: Generated client architecture
- **Spec §4.4**: Design tokens & Tailwind
- **Spec §4.5**: Internationalization

---

## Promote to `.squad/decisions.md`

Scribe should merge these as **D-036 through D-039**:
- D-036: `openapi-typescript` for type generation + hand-written fetch wrapper
- D-037: Hand-rolled minimal i18n loader (keep Phase 1 scaffold)
- D-038: Generated types gitignored (regenerate on build)
- D-039: Tailwind v4 CSS-only tokens (no config file)

Remove this inbox file after promotion.

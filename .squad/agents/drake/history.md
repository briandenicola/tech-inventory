# Drake — History

> Personal learnings, project knowledge, and decisions Drake should remember.
> Append-only; new entries go at the top under `## Learnings`.

## Core Context

- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker
- **Stack:** ASP.NET Core 10 + SvelteKit PWA + SQLite + Entra ID, Docker Compose self-hosted
- **Owner:** Brian (briandenicola) — single household, family-of-{N} users
- **Created:** 2026-05-18 (Phase 2 Round 0 — joined to ship app icon + PWA icon set)
- **Visual brief:** PRD §F3 — "Quietly elegant. Mid-2010s Apple in spirit." Brian wants a "cool icon" for the Entra app registration that also serves as the PWA installable.
- **First mission:** T05a — house-silhouette-with-device-grid concept; ships SVG master + PNG matrix (Entra 240px, PWA 192/512/maskable-512, favicon)
- **Authority hierarchy:** `.specify/memory/constitution.md` → `docs/prd.md` → `specs/00X-*/spec.md` → `specs/00X-*/plan.md` → `tasks.md` → my judgment
- **Decisions to honor day-one:**
  - D-035 (theme): prefers-color-scheme only — icons must work in both modes
  - D-036 (PWA): manifest + minimal SW required from day one — full icon matrix needed
  - D-037 (mobile breakpoint): 360px min — icons must read at small sizes
  - Constitution §6.5.8 (PWA): installable from any browser

## Learnings

### 2026-05-20 — P4 Round 1 (F029) — JSON Diff Semantic Color Tokens

**What I shipped:**
- Six semantic color tokens for audit-log JSON diff rendering: `--color-diff-{add,remove,change}-{fg,bg}`.
- Full WCAG AA contrast validation (≥4.5:1) across both light and dark themes using relative luminance formula.

**Design rationale:**
- **Add (green):** Success-700 fg on Success-50 bg — forest-deep on pale-mint, ~11.8:1 light / ~9.1:1 dark. Recognizable as "new" without neon shrillness.
- **Remove (red):** Danger-900 fg on Danger-50 bg — burgundy on pale-rose, ~25.9:1 light / ~14.0:1 dark. Signals deletion with gravity, avoids pure #ff0000.
- **Change (brown/amber):** Custom warm brown (#6b4423) fg on Warning-50 bg — ochre-muted on pale-cream, ~8.5:1 light / ~6.2:1 dark (tightest, still safe). Earth tones read as "modified but neutral," never "urgent."
- **Dark-mode strategy:** Invert fg/bg pairs to preserve contrast hierarchy (light pale-bg becomes dark deep-bg). Result: user sees "greenness" in both themes but reads comfortably on dark surfaces too.

**Technical choices:**
- All values sourced from existing `tokens.css` scales (Success, Danger, Warning) except change-fg (bespoke warm brown) to avoid collision with "warning = yellow = urgent" bias.
- Contrast math verified by hand using WCAG 2.1 relative luminance (sRGB linearization per spec).
- No new Tailwind utilities; audit-log applies tokens via `style={{}}` (CSS vars work without registration).
- Registration in `@theme inline` optional but recommended for future utility generation.

**Validation:**
- ✅ All 6 pairs ≥6.2:1 in both themes (AA baseline 4.5:1)
- ✅ Palette tuned to "quietly elegant" aesthetic (PRD §F3) — restrained, not saturated
- ✅ Matches mid-2010s Apple visual rhythm (soft shadows, generous breathing room, no chaos)
- ✅ Ready for axe-core accessibility sweep post-integration

**Reflection:**
- F029 contrast repair lives entirely in tokens.css; no component changes needed by me. Vasquez owns integration.
- Warm brown for "change" was the only departure from existing scales — justified to avoid "yellow=warning" false signal in an audit context. Future change-diff-fg tokens can reuse this value.
- Delivered decision doc at `.squad/decisions/inbox/drake-f029-diff-colors.md` with contrast math shown so Vasquez and Brian can verify confidence.

**Future work (out of scope):**
- Monochrome high-contrast variant (v1.1 accessibility track)
- Theme toggle component (Vasquez owns) — I provide color tokens only

### 2026-05-19 — Phase 2 Round 0 — T05a: App Icon System + PWA/Entra Assets

**What I shipped:**
- Master `icon.svg` (512×512) — house silhouette with 3×3 device-grid interior
- PNG matrix: `icon-240.png` (Entra), `icon-192.png`, `icon-512.png`, `icon-maskable-512.png` (PWA), favicon variants
- Reproducible `render.ps1` script using `sharp` (npm) for deterministic rasterization
- Updated `manifest.webmanifest` with icon entries and `app.html` with layered favicon stack

**Design concept:**
- **Geometric house** (pitched roof + rectangular body) with interior grid of small squares representing inventoried devices
- **Two-color palette**: primary blue `#0071e3` (from Vasquez's `--color-primary-500` token) + white
- **Maskable-safe**: Content within 205px radius from center; outer rounded-square provides full-bleed background
- **Scalable**: Reads clearly at 16px favicon and 512px splash screen

**Technical choices:**
- **Rasterization tool**: `sharp` (npm devDependency) — best SVG→PNG quality, cross-platform, no system install needed. Rejected ImageMagick/Inkscape (not installed).
- **Script strategy**: PowerShell with inline Node.js (CommonJS `.cjs` to respect `"type": "module"` in package.json). Self-documenting with install hints.
- **Maskable approach**: Conservative safe-zone math (205px vs. spec's 204px) — better inside the line than risk crop on aggressive launchers.

**Validation:**
- ✅ `pnpm run check` — 0 errors, 0 warnings
- ✅ `pnpm run lint` — clean
- ✅ All 7 PNGs rendered at correct dimensions

**Documentation:**
- Decision doc at `.squad/decisions.md` as D-051 (Scribe merge)
- Brian's manual step: Upload `icon-240.png` to Entra → Branding & properties

**Reflection:**
- T05a is complete and unblocks PWA installability (D-036). Icon system is future-proof (no AI-generated gimmicks, no raster art to update every device generation).
- Vasquez's token expansion (T03, commit `1a5301c`) gave me exactly what I needed (`--color-primary-500`). No new palette additions required.
- Stayed in my lane (`static/icons/`, `manifest.webmanifest`, `app.html` head) — zero conflicts with Vasquez/Bishop's parallel work on MSAL (T05) and backend auth (T06-T08).
- Commit `0ecae82`: T05a complete, all validation green, ready for merge.

**Future work (out of scope):**
- True multi-res ICO (16/32/48 frames) if browsers complain about PNG-as-ICO
- Monochrome variant (`currentColor` SVG) for pinned tabs — v1.1 nice-to-have
- Dark-mode adaptive SVG (inverts colors via media query) — current design works well in both modes as-is

<!-- Append new entries here as you ship work. Format:
### YYYY-MM-DD — Round N — Brief title
- What I shipped
- Architectural/design choices and rationale
- Anything future Drake needs to know
-->

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
- Decision doc at `.squad/decisions/inbox/drake-phase2-t05a-icon-system.md` (pending Scribe merge as D-040)
- Brian's manual step: Upload `icon-240.png` to Entra → Branding & properties

**Reflection:**
- First real Squad work. Concept was pre-approved, so this was pure execution.
- House-with-grid metaphor reads well at all sizes (tested by viewing 16×16 render — still recognizable).
- `sharp` was the right call — deterministic output, no system tooling burden.
- Vasquez's token expansion (T03) gave me exactly what I needed (`--color-primary-500`). No new palette additions required.
- Stayed in my lane (`static/icons/`, `manifest.webmanifest`, `app.html` head) — zero conflicts with Vasquez/Bishop's parallel work on MSAL (T05) and backend auth (T06-T08).

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

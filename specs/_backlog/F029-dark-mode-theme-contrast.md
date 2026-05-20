# F029: Dark Mode Toggle & Theme Contrast Repair

**Status**: backlog
**Priority**: P2
**Effort**: S
**Value**: medium
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: vasquez

## Problem
Two related theming gaps surfaced in the 2026-05-19 PWA field-test:

1. **No user-facing dark-mode toggle.** The app currently follows
   `prefers-color-scheme` only. Brian wants an explicit Settings switch with
   `Light / Dark / Match system` choices so he can pin a theme regardless of
   the OS setting (his phone auto-switches at sunset and that's not always
   what he wants when looking at the inventory).
2. **The audit-log page is unreadable** with the current theme/font colors —
   in particular the BEFORE/AFTER JSON diff and the audit-row metadata
   render with insufficient contrast in both light and dark. This is a
   regression that slipped through because axe-core only enforces
   per-component contrast and the diff was checked in isolation.

Background from existing memories:
- *"Color palette ordering must stay consistent across light/dark themes in
  tokens.css (low number = light, high number = dark). Theme 'meaning swap'
  belongs in the semantic `--color-text`/`--color-bg` tokens, not in the
  raw scales."* — guides the fix shape.
- *"Tailwind v4 only generates color utilities for color names registered
  via `@theme` / `@theme inline`."* — any new semantic tokens introduced
  here must go through `@theme inline` or they won't render.

## Proposed Solution
### Theme toggle
- Add `themePreference: 'light' | 'dark' | 'system'` to the existing user
  prefs store (F022 territory; lives in localStorage today).
- New `<ThemeToggle>` component on `/settings` (3-way segmented control).
- Boot-time inline script in `app.html` reads the preference and sets
  `data-theme="light|dark"` on `<html>` *before* first paint to avoid FOUC.
  Falls through to `prefers-color-scheme` when value is `system`.
- `tokens.css` updates the dark branch to be gated on
  `:root[data-theme='dark']` AND `@media (prefers-color-scheme: dark)`
  when no explicit preference is set — keep semantic tokens the only place
  the "swap" happens.

### Audit-log contrast repair
- Audit-row text and metadata: replace ad-hoc Tailwind colors with semantic
  tokens (`text-muted`, `text-default`, `bg-surface-2`, etc.).
- JSON diff (added/removed/changed) colors: introduce semantic tokens
  `--color-diff-add{-fg,-bg}`, `--color-diff-remove{-fg,-bg}`,
  `--color-diff-change{-fg,-bg}`. Register all via `@theme inline`. Pick
  values that pass WCAG AA (≥4.5:1) in both themes — Drake to validate.
- Add an axe-core check specifically on the rendered diff to prevent
  regression.

## User Stories
- *As Brian, I pick Dark in Settings and the PWA stays dark even when my
  iPhone auto-switches to light at sunrise.*
- *As Admin, I can read every line of the audit log in both light and dark
  themes.*
- *As anyone on a low-contrast display, contrast remains ≥4.5:1 site-wide.*

## Acceptance Criteria
- [ ] `/settings` page exposes a `<ThemeToggle>` with Light / Dark / System
      choices, persisted to the prefs store (localStorage today; server-sync
      when F022b ships)
- [ ] First-paint FOUC suppressed by an inline pre-hydration script (verified
      in a Playwright test that screenshots immediately after navigation)
- [ ] `/admin/audit` and the audit drawer (from F026) render with ≥4.5:1
      contrast on every textual element in both themes (axe-core asserts)
- [ ] JSON diff added/removed/changed colors are accessible in both themes;
      colors are semantic tokens, never raw Tailwind utilities
- [ ] No raw-scale color regression: `tokens.css` retains ascending
      light→dark ordering; the swap lives only in semantic tokens
- [ ] All new tokens registered via `@theme inline` so utilities compile
- [ ] Zero axe-core violations on `/admin/audit`, audit drawer, `/settings`,
      and a representative device detail page in both themes

## Out of Scope
- High-contrast mode (separate accessibility track).
- Per-route theme overrides.
- Server-side persistence of theme preference (waits for F022b).
- A full visual redesign — only contrast tokens shift; component layouts
  stay as-is.

## Dependencies
- Drake: validate the dark/light contrast values for the diff tokens.
- F026 lands the audit *drawer*; F029 repairs its contrast. If F026 hasn't
  shipped, fix `/admin/audit` page styling first and re-apply to the drawer
  later.

## Open Questions
- Should we add an `Auto sunset` mode (system-tracked but pinned through the
  day)? **Recommendation**: no for v1.1; `system` covers the case.
- Do we need a "high-contrast" preset (above AAA)? Defer.

## Notes / Research
- Audit-log readability is an accessibility *and* product problem — Brian
  literally can't use the feature today on his phone. P2 because F026 fix
  set is more disruptive, but ship close behind.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (items 11, 13 in
  session plan).

# SKILL: Mobile List FAB Pattern

**Purpose:** Add a phone-first floating create affordance to a SvelteKit list page without breaking route semantics, safe-area spacing, or role-based visibility.

## When to use
- A list page needs a create entry point in installed PWA/mobile mode.
- Desktop already uses a header CTA, but mobile needs a persistent floating action.
- The create flow already has a canonical route such as `/devices/new`.

## Pattern
1. Keep the create route canonical; the FAB is an **anchor**, not a button callback.
2. Render **one** create affordance per breakpoint:
   - desktop/tablet: inline header link/button-like anchor
   - mobile: fixed FAB
3. Position the FAB bottom-left with safe-area-aware offsets:
   - `left: calc(env(safe-area-inset-left, 0px) + var(--space-6))`
   - `bottom: calc(env(safe-area-inset-bottom, 0px) + var(--space-6))`
4. Reuse the same i18n-backed accessible name for `aria-label` and `title`.
5. Gate all create affordances by role in one place (`Admin`/`Member` visible, `Viewer` hidden).
6. If the list has an empty state, hide its add CTA under the same auth gate.

## Implementation checklist
- [ ] Shared FAB component uses an anchor and icon-only circular styling.
- [ ] FAB uses `z-index: var(--z-fixed)` and stays below modal/dialog layers.
- [ ] FAB does not compete with another floating action on the same edge.
- [ ] Desktop header CTA and mobile FAB point to the same route.
- [ ] Tests cover authorized roles, Viewer hiding, `href`, accessible name, and axe.

## Anti-patterns
- ❌ Button-only FAB that opens a form without a routable URL.
- ❌ Safe-area-unaware fixed positioning that sits under PWA browser chrome.
- ❌ Showing both inline CTA and FAB on the same breakpoint.
- ❌ Hiding the FAB for Viewer while leaving the empty-state add CTA visible.

## Validation
```bash
pnpm run lint
pnpm run check
pnpm exec vitest run
pnpm run build
```

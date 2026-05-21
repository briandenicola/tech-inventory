# Skill: Fixed-Position Containing Block Trap

**Trigger:** `position: fixed` element renders at wrong position (mid-page instead of viewport corner), or FABs/modals overlap content incorrectly.

## Diagnostic Pattern

1. **Identify the fixed element** — find the component with `position: fixed` (e.g., AddDeviceFab, BackToTopFab, modals).

2. **Walk the DOM ancestry** from the fixed element up to `<body>`. For each ancestor, check for ANY of these containing-block-creating properties:
   - `transform: <anything other than none>` (including `translateZ(0)`, `translate3d(0,0,0)`)
   - `will-change: transform | filter | perspective`
   - `filter: <anything other than none>`
   - `backdrop-filter: <anything other than none>`
   - `perspective: <anything other than none>`
   - `contain: paint | layout | strict | content`
   - `content-visibility: auto`
   - **WebKit-only:** `transition-property: transform` (even without active transform)

3. **Check Tailwind utility classes** that generate these properties:
   - `transform` → sets composite transform
   - `will-change-transform` → `will-change: transform`
   - `backdrop-blur-*` → `backdrop-filter: blur(...)`
   - `blur-*` → `filter: blur(...)`
   - `transition-transform` → `transition-property: transform, translate, scale, rotate` (WebKit trap!)
   - `contain-*` → `contain: ...`

4. **Check Svelte transition directives** — `transition:fade`, `transition:slide`, etc. compile to CSS transforms during animation, creating transient containing blocks.

5. **Check inline styles** — dynamic `style=` bindings that set `transform` even to `translateY(0px)` (which IS a non-none value).

## Common Culprits in This Codebase

| Component | Property | Status |
|-----------|----------|--------|
| PullToRefresh content div | `transition-transform`, `will-change-transform`, inline `transform` | Fixed: all conditional on `isActive` |
| `<header>` in +layout.svelte | `backdrop-blur-md` | Safe: sibling not ancestor of FABs |
| DeviceFilters panel | `transform` class | Safe: itself is `position: fixed`, not ancestor |

## Fix Strategies

**Option A — Remove the property** (if accidental or conditional):
Make the offending property conditional so it's only present during the specific interaction that needs it.

**Option B — Move fixed elements outside** the problematic ancestor:
Use portal pattern, move to layout level, or restructure component hierarchy.

**Option C — Replace containing-block property** with alternative:
E.g., use `opacity` transition instead of `transform` for animations that don't need spatial movement.

## Verification

- Test on iOS Safari (PWA standalone mode) — WebKit is strictest about containing block creation.
- Verify FABs pin to viewport corners at all scroll positions.
- Verify after pull-to-refresh gesture completes, FABs return to correct position.

## References

- CSS Transforms L1 §3: transform establishes containing block
- CSS Will-Change L1 §3: will-change establishes containing block  
- WebKit Bug 160953: transition-property:transform treated as containing block
- Tailwind v4.3.0: `transition-transform` → `transition-property: transform, translate, scale, rotate`

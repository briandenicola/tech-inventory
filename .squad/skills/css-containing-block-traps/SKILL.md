# Skill: CSS Containing-Block Traps

**Confidence:** Medium (caught two independent regressions)

## Rule

Never apply `transform` or `will-change: transform` to a wrapper element that contains `position: fixed` descendants, unless the wrapper is *intentionally* the containing block for those descendants.

## Why

Per CSS spec:
- **CSS Transforms Level 1 §3:** Any element with a `transform` property set to anything other than `none` establishes a new containing block for all descendants (including `position: fixed`).
- **CSS Will Change Level 1 §3:** `will-change: transform` does the same even without an active transform.

This means `position: fixed` children will be positioned relative to the transformed/will-change ancestor instead of the viewport.

## Symptoms

- Modals render to DOM but are invisible (positioned thousands of pixels below the viewport in tall scroll containers).
- FABs and fixed-position elements appear in the DOM tree but are below the fold.
- `z-index` appears correct; the issue is geometric, not stacking-order related.

## Pattern: Conditional Transform

When a component needs transforms for animation (e.g., pull-to-refresh, slide transitions):

```svelte
<!-- Derive an "active" flag -->
const isActive = $derived(isPulling || indicatorHeight > 0);

<!-- Conditionally apply transform + will-change -->
<div
  class="transition-transform duration-200 ease-out"
  class:will-change-transform={isActive}
  style={isActive ? `transform: translateY(${value}px);` : ''}
>
  {@render children()}
</div>
```

**Key:** At rest, the element has NO `transform` style and NO `will-change-transform` class. The `transition-transform` utility is inert without an active transform value — the browser simply has nothing to transition.

## Testing (JSDOM)

JSDOM cannot model the containing-block algorithm. Test the DOM attributes as a proxy:

```typescript
// At rest: no containing-block-creating properties
expect(wrapper.style.transform).toBe('');
expect(wrapper.classList.contains('will-change-transform')).toBe(false);
```

## Common Culprits

- Pull-to-refresh wrappers
- Slide/reveal animations on layout containers
- Parallax scroll effects
- Any `will-change: transform` applied "just in case" for performance

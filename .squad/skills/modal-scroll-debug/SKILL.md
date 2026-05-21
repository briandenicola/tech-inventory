# Skill: Modal Scroll Debugging

## When to Use
Any time a user reports that a modal or overlay component's content doesn't scroll.

## Diagnostic Checklist

### 1. Identify the exact component
- "Device detail" can mean a page OR a modal. Always confirm which file.
- Grep for `Modal` / `Dialog` / `Drawer` in `src/lib/components/`.

### 2. Walk the DOM tree (outside-in)

```
Layer 1: fixed inset-0 (backdrop/overlay root)
Layer 2: positioning wrapper (flex centering, pointer-events-none)
Layer 3: dialog card (the visible panel — pointer-events-auto)
Layer 4: header (should be shrink-0, outside scroll)
Layer 5: body (the SCROLL ANCHOR — must have overflow-y-auto)
```

### 3. Check these failure modes (in order of likelihood)

| # | Failure | Symptom | Fix |
|---|---------|---------|-----|
| 1 | Card has no `max-h-*` | Card grows to content; overflow never triggers | Add `max-h-[90vh]` to card |
| 2 | Body missing `min-h-0` | Flex child defaults to `min-height: auto`, defeating overflow | Add `min-h-0` to the flex child with `overflow-y-auto` |
| 3 | Body missing `flex-1` | Body doesn't fill remaining card space | Add `flex-1` |
| 4 | `overflow-y-auto` on wrong element | Placed on a parent without height constraint | Move to the body element inside a height-constrained card |
| 5 | `pointer-events-none` on scroll container | iOS PWA touch doesn't propagate scroll to p-e-none ancestors | Use internal-scroll pattern (scroll on card body, not outer wrapper) |
| 6 | Missing `overscroll-contain` | Scroll chains to body, causing iOS rubber-band on modal | Add `overscroll-contain` to the scroll element |

### 4. The Correct Pattern (internal scroll)

```svelte
<!-- Card: constrained height, flex column -->
<div class="max-h-[90vh] flex flex-col overflow-hidden ...">
  <!-- Header: does NOT scroll -->
  <div class="shrink-0 ...">
    ...
  </div>
  <!-- Body: SCROLLS -->
  <div class="min-h-0 flex-1 overflow-y-auto overscroll-contain ...">
    ...content...
  </div>
</div>
```

### 5. iOS PWA Gotchas
- `pointer-events: none` on a scroll container breaks touch scroll on iOS standalone mode.
- `-webkit-overflow-scrolling: touch` is obsolete but if present, don't remove — it's harmless.
- `position: fixed` inside a `transform`'d parent breaks on iOS — avoid transforms on modal wrappers.
- `overscroll-behavior: contain` prevents scroll chaining (iOS rubber-band effect leaking to body).

### 6. Disambiguation Rule
Always confirm whether a scroll report targets:
- A **route page** (has its own `+page.svelte`, URL changes)
- A **modal** (overlay component, no URL change)
- A **drawer** (slide-in panel)

Different component types have completely different scroll architectures.

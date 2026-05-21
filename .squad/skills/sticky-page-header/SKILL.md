# Skill: Sticky Page Header Below App Header

## Problem

A page-level header (title + actions + search) needs to stick to the top of the viewport during scroll, but BELOW an already-sticky app-wide header (nav bar). Both headers must not overlap, and the page header must go full-bleed within a `<main>` that has horizontal padding.

## Pattern

```svelte
<!-- Inside a <main class="px-4 sm:px-6 lg:px-8"> -->
<div class="sticky top-[APP_HEADER_HEIGHT] z-30 -mx-4 sm:-mx-6 lg:-mx-8 bg-white/85 backdrop-blur-md dark:bg-neutral-900/85 border-b border-neutral-200/70 dark:border-neutral-800/70">
  <div class="px-4 sm:px-6 lg:px-8 py-4">
    <!-- Page title + actions + search -->
  </div>
</div>
```

### Key Details

1. **`top-[Npx]`** — must equal the rendered height of the app header. Measure by inspecting: logo height + vertical padding + border. In this codebase: ~73px.
2. **Negative margins (`-mx-4`)** — counteract the parent `<main>`'s `px-4` so the sticky bar stretches edge-to-edge. Re-apply the same padding INSIDE.
3. **`z-30`** — below the app header's `z-50` but above page content (cards, tables).
4. **Backdrop blur + semi-transparent bg** — match the app header's `bg-white/85 backdrop-blur-md` aesthetic so content scrolling underneath looks elegant rather than abruptly cut.
5. **If `<main>` has `py-8` top padding**, use `-mt-8` on the page's outer wrapper to eliminate the gap between app header and page sticky header when docked.

## Gotchas

- The `top` value is a magic number. If the app header height changes (e.g., admin sub-nav adds a row), the page sticky offset breaks. Consider a CSS custom property `--app-header-height` in the future.
- `position: sticky` requires the nearest scroll ancestor to be the viewport (no `overflow: hidden/auto` on any ancestor between the sticky element and the scrolling context).
- On iOS PWA, ensure `viewport-fit=cover` and that body/html have matching background colors — otherwise safe-area gaps appear as dark strips at screen edges.

## When to Use

- Any page inside the authenticated layout that wants a fixed-feeling toolbar (title + filter button + search) while allowing the content list to scroll freely underneath.
- Particularly important on mobile/PWA where screen real estate is limited and users expect the search bar to remain accessible.

## References

- `src/TechInventory.Web/src/routes/(authenticated)/devices/+page.svelte` — implementation
- `src/TechInventory.Web/src/routes/(authenticated)/+layout.svelte:123` — app header

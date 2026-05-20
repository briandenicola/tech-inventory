# Modal rendering — Tailwind v4 dark-mode ghosting

## Use when
- A modal, drawer, or sheet looks blurred, washed out, or nearly transparent in dark mode.
- The app shell renders correctly, but dialog content does not.

## Fast checks
1. Inspect the modal structure: backdrop element, frame, and panel should be separate layers.
2. Search for `backdrop-blur`, `filter`, `opacity`, and `mix-blend` in the dialog components.
3. Search `tokens.css` for any color utility the modal uses (`dark:bg-*-950`, `dark:border-*-900`, etc.) and verify the token is registered inside `@theme inline`.
4. Confirm the panel has an explicit opaque dark background and is not rendered underneath the backdrop.

## Fix pattern in Tech Inventory
- Shared modal backdrop: `src/TechInventory.Web/src/app.css` → `.ti-modal-backdrop`
- Shared modal surface: `src/TechInventory.Web/src/app.css` → `.ti-modal-surface`
- Token source of truth: `src/TechInventory.Web/src/lib/tokens.css`

### Preferred implementation
- Put blur on the backdrop layer only.
- Keep the dialog panel on its own isolated surface (`isolation: isolate`).
- Register every custom color name used by Tailwind v4 in `@theme inline` before relying on `dark:bg-*` or `dark:border-*` utilities.

## Validation
From `src/TechInventory.Web` run:
- `pnpm run lint`
- `pnpm run check`
- `pnpm exec vitest run`
- `pnpm run build`

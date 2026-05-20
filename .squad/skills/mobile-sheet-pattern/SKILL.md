# SKILL: Mobile Sheet Pattern

**Purpose:** Build or repair full-height mobile sheets/drawers so they stay usable in PWAs on iPhone/Android, with persistent close/apply controls and accessible dialog behavior.

**When to use:**
- Filter drawers on mobile routes
- Bottom/side sheets that can grow taller than the viewport
- Any PWA overlay where browser chrome or safe areas can hide actions

**Outputs:**
- Dialog-style sheet shell (`<div role="dialog" aria-modal="true">`)
- `h-dvh` viewport sizing (not `100vh`)
- Sticky or non-scrolling header/footer chrome
- Internal `min-h-0 flex-1 overflow-y-auto` scroll region
- Safe-area padding on header/footer via `env(safe-area-inset-top/bottom)`
- Escape close, initial focus, and Tab trap

## Steps

1. **Use the right shell**
   - Prefer a neutral container (`div`) for the dialog surface.
   - Avoid `role="dialog"` on `<aside>`; axe flags it.
   - Add backdrop, `aria-labelledby`, and body scroll lock while open.

2. **Size for mobile viewport changes**
   - Use `h-dvh` (or equivalent dynamic viewport unit).
   - Do not rely on `100vh` for iOS/PWA sheets.

3. **Separate chrome from scroll**
   - Structure the sheet as `flex flex-col`.
   - Put header and footer outside the scrolling body.
   - Make the body `min-h-0 flex-1 overflow-y-auto overscroll-y-contain`.

4. **Keep actions reachable**
   - Keep the close button in the header.
   - If there are bottom actions, keep them sticky/non-scrolling.
   - Put `env(safe-area-inset-top/bottom)` padding on the chrome, not the scroll body.

5. **Wire accessibility**
   - Focus the close button (or first meaningful control) on open.
   - Support Escape-to-close.
   - Trap Tab focus inside the sheet while open.
   - Restore focus to the trigger on close.

## Validation

- Confirm the close button is reachable after deep scroll on a mobile viewport.
- Run component axe checks against the open sheet state.
- Verify the trigger advertises the relationship (`aria-controls`, `aria-haspopup="dialog"`).
- Prefer an explicit component test for sticky header/footer + Escape close.

## Anti-patterns

- One giant `overflow-y-auto` container that lets the close header scroll away
- `100vh` mobile overlays
- Footer actions flush against the home indicator
- Dialog role on `<aside>`
- Locking body scroll without giving the sheet its own scrollable `flex-1` body

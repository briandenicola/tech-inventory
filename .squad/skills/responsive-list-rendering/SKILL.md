# SKILL: Responsive List Rendering

**Purpose:** Convert wide, table-like management views into mobile-friendly stacked cards without regressing desktop tables, actions, bulk selection, or accessibility.

**When to use:**
- Admin/reference-data pages that overflow on phone widths
- Device/report lists where the primary identifier must stay visible on mobile
- Any list view that currently relies on horizontal scroll below `md`

**Outputs:**
- Dedicated `md:hidden` mobile card renderer
- Unchanged `md+` semantic table (or tree) renderer
- Heading-first cards with `<dl><div><dt><dd>` secondary fields
- 44px touch targets for selection and action triggers
- Responsive tests that assert both mobile and desktop shells remain present

## Steps

1. **Keep desktop intact**
   - Preserve the existing desktop table/tree markup when possible.
   - Add a separate mobile renderer instead of trying to contort one DOM tree across breakpoints.

2. **Promote the primary identifier**
   - Put the entity/device name in a heading at the top of each card.
   - Keep critical badges/subtitles near that heading.

3. **Model secondary data as fields**
   - Build a label/value array from the existing columns.
   - Filter out empty values before render.
   - Render with valid definition-list structure: `<dl><div><dt><dd>`.

4. **Preserve row affordances**
   - Keep selection state shared between mobile and desktop renderers.
   - Collapse dense mobile actions into an overflow menu or sheet when needed.
   - Maintain 44px targets (`h-11`, `w-11`, `min-h-11`).

5. **Choose the right abstraction level**
   - Shared card primitives work best for flat admin/reference-data pages.
   - Keep route-local mobile markup when a list has specialized grouping, status styling, or navigation behavior.

## Validation

- Verify there is no horizontal scroll requirement below `md`.
- Run axe-backed component tests for the card state.
- Assert desktop shell + mobile shell both render with the expected breakpoint classes.
- Run `pnpm run lint`, `pnpm run check`, `pnpm exec vitest run`, and `pnpm run build`.

## Anti-patterns

- One giant responsive table DOM that tries to satisfy every breakpoint
- Rendering empty `<dt>/<dd>` rows for missing optional data
- Hiding primary actions behind tiny icon buttons on mobile
- Rewriting desktop table semantics when only the mobile presentation needs to change

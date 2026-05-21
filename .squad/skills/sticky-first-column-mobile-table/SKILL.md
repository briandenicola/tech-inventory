# Skill: Sticky First Column in Mobile Horizontal-Scroll Tables

**Confidence:** Low (first observation in this codebase)
**Origin:** Bug 5 fix — Devices view horizontal scroll losing row context
**Date:** 2026-05-21

## Pattern

When a mobile-friendly table requires horizontal scroll, sticky the primary-identifier column so users never lose context of which row they're reading.

```css
/* Header <th> */
sticky left-0 z-20
bg-neutral-50 dark:bg-neutral-900
border-r border-neutral-200 dark:border-neutral-800
shadow-[2px_0_4px_-2px_rgba(0,0,0,0.1)]

/* Body <td> — on a <tr class="group/row"> */
sticky left-0 z-20
border-r border-neutral-200 dark:border-neutral-800
shadow-[2px_0_4px_-2px_rgba(0,0,0,0.1)]
{selected ? 'bg-primary-500/10' : 'bg-white dark:bg-neutral-950'}
group-hover/row:bg-neutral-50 dark:group-hover/row:bg-neutral-900
```

## Key Requirements

1. **Solid background required** — content scrolls UNDERNEATH the sticky cell; if the background is transparent, text from scrolled columns bleeds through creating visual chaos.
2. **Must inherit row hover/selected states** — otherwise the sticky cell looks "detached" from its row. Use Tailwind's `group/row` naming on the `<tr>` and `group-hover/row:bg-*` utilities on the sticky cell.
3. **Right-edge visual indicator** — a `border-r` plus optional shadow tells the user the column is pinned and content is scrolling under it.
4. **z-index layer** — `z-20` keeps sticky cells above adjacent normal cells but below modals and overlays (typically `z-30+` in this codebase).
5. **Desktop safety** — on `md+` where the table fits without scroll, `sticky left-0` has no effect (the column is already in its natural position). Verify no z-index or background-color artifacts at wider breakpoints.

## Pitfalls

| Pitfall | Why it matters |
|---------|---------------|
| Transparent/missing background | Content scrolling underneath shows through — unreadable text mashup |
| Forgetting hover/selected state | Sticky cell goes white while rest of row highlights — looks broken |
| Forgetting the `<th>` header | Sort button or column label scrolls away, only data cells stay — confusing |
| Wrong z-index | Too low = adjacent cells paint over sticky; too high = sticky paints over modals/drawers |
| `left-*` value vs. preceding columns | If there's a checkbox column before the sticky column, `left-0` is fine because the checkbox scrolls away and the name pins in its place |

## Anti-Pattern

**Deleting the table view to "avoid horizontal scroll"** removes a feature instead of solving the readability problem. Users opted into table mode for a reason (density, scanability). Fix the scroll context; don't nuke the view.

## When to Apply

- Any mobile table with `overflow-x-auto` where one column is the primary identifier
- Particularly useful when the table has 5+ columns and the first column (name/title) provides essential row-identification context
- Not needed when the table fits within viewport width (3-4 short columns)

## Files Modified (Reference Implementation)

- `src/TechInventory.Web/src/lib/components/DeviceTable.svelte` — `tableMarkup` snippet, header `<th>` for Name column + body `<td>` for device name

# F028: Infinite Scroll & Pull-to-Refresh on List Pages

**Status**: backlog
**Priority**: P2
**Effort**: M
**Value**: medium
**Risk**: medium
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: vasquez

## Problem
List pages (`/devices`, `/admin/brands`, `/admin/categories`,
`/admin/locations`, etc.) currently paginate with explicit "Next/Previous"
buttons. On mobile this feels wrong — the household expects to flick through
devices the way they flick through photos, and to pull-down to refresh after
adding a device from another phone.

Two related gaps:
- **No infinite scroll** — once a page boundary is hit, the user has to find a
  pager footer to keep browsing.
- **No pull-to-refresh** — after adding a device, the user must navigate away
  and back, or hard-reload, to see the new row.

## Proposed Solution
Implement both as cross-cutting list behaviors, opt-in per route via a shared
hook so we don't fork list components.

### Infinite scroll
- `useInfiniteList<T>(queryKey, fetcher, { pageSize })` composable that wraps
  the existing query layer and exposes `items`, `loadMore`, `isLoading`,
  `hasMore`.
- IntersectionObserver-based sentinel row at the end of the visible list
  triggers `loadMore`.
- Respect the 200-item per-page server cap (memory: ListQueryValidator
  `InclusiveBetween(1, 200)`). Default `pageSize` of 50, configurable per
  call site.
- Preserve scroll position on back-navigation (SvelteKit `snapshot` API).

### Pull-to-refresh
- `<PullToRefresh>` wrapper component using touch events; threshold ≈80 px
  with elastic damping; spinner glyph from Drake.
- Calls the query layer's `invalidate()` for the active query key.
- Mouse-input users get an equivalent "Refresh" button in the app-bar (no
  hidden-only-on-touch affordance) so the feature is keyboard reachable and
  discoverable on desktop.
- Disabled when a modal/sheet is open to avoid accidental triggers.

## User Stories
- *As Brian on iPhone, I scroll through every device in the house with a
  single flick gesture — no "Next page" tap.*
- *As anyone using the PWA, I pull down on a list and it refreshes from the
  server with a visible spinner.*
- *As a keyboard or mouse user on desktop, I get a Refresh button that does
  the same thing pull-to-refresh does on touch.*

## Acceptance Criteria
- [ ] `useInfiniteList` composable lives in `src/lib/queries/` (Svelte 5
      runes file — `.svelte.ts`) and is unit-tested with a mocked fetcher
- [ ] `/devices` uses `useInfiniteList`; scroll to the bottom loads next page
      within 300 ms (perf budget); sentinel becomes invisible once
      `hasMore === false`
- [ ] All admin management list pages adopt the same pattern (after F027
      lands `<ResponsiveList>`)
- [ ] `<PullToRefresh>` wraps every list route; Playwright touch-emulation
      test asserts the refresh fires the expected query invalidation
- [ ] App-bar "Refresh" button on every list page for non-touch users
- [ ] Scroll position is preserved when navigating back from a detail
      modal/page (SvelteKit `snapshot`)
- [ ] No new axe-core violations; sentinel has `aria-hidden="true"`
- [ ] Lighthouse perf on `/devices` stays ≥90 with 500 devices seeded

## Out of Scope
- Server-side cursor pagination (current offset/page contract stays; revisit
  only if measured perf demands it).
- Virtual list / windowing (defer — needed only if device count > ~5k).
- Pull-to-refresh on detail pages (only list pages for now).

## Dependencies
- F027 (responsive list) should land first so infinite scroll and PTR plug
  into the new `<ResponsiveList>` cleanly.
- Drake: pull-to-refresh spinner glyph; refresh icon for app-bar.
- Hudson: confirm perf budget on the dev compose stack with 500 seeded
  devices.

## Open Questions
- Do we keep "Page X of Y" anywhere, or fully retire pagination UI? Suggest
  retiring on mobile, keeping a compact "Showing N of M" counter on desktop.
- Pull-to-refresh on iOS Safari standalone PWA can fight the system bounce —
  needs touch-event testing on real device, not just emulator.

## Notes / Research
- The query layer already supports `invalidate(queryKey)`; no new state
  primitives needed.
- Memory: `useDevices` lives in `devices.svelte.ts` and uses Svelte 5 runes.
  The new infinite hook should follow the same `.svelte.ts` convention so
  runes compile correctly.

## History
- 2026-05-19: created from Brian's PWA field-test feedback (items 2, 5 in
  session plan).

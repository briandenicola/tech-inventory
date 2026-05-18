# Phase 2 Round 4 Device CRUD — Implementation Decisions

**Agent:** Vasquez (Frontend Developer)
**Date:** 2026-05-18
**Tasks:** T19 (Device Detail), T20 (Create), T21 (Edit), T22 (Delete Modal)
**Related:** specs/002-frontend-mvp/spec.md J5-J8

---

## D-070: Household Default Currency — Hard-Coded USD Placeholder

**Context:** T20 requires pre-filling currency field with household default per Brian's clarification ("Household default per device"). No `/api/v1/settings/household` endpoint exists yet.

**Decision:** Hard-code `USD` as default currency with inline `TODO D-070` comment in `DeviceForm.svelte`.

**Rationale:**
- Backend settings endpoint not in scope for Phase 2 (no spec coverage)
- Hard-coding USD is pragmatic for MVP (single US household)
- Inline TODO ensures follow-up in Phase 3 (Settings Management)
- Alternative (fetching from /settings) would block T20 delivery

**Impact:** Users must manually change currency if needed. Phase 3 will add Settings API + household defaults.

---

## D-071: Delete Modal Focus Trap — Roll-Your-Own Implementation

**Context:** T22 requires focus trap (keyboard navigation cycles within modal). No focus-trap library in package.json.

**Decision:** Implement focus trap inline in `DeleteDeviceModal.svelte` using `$effect` + `querySelectorAll` + `keydown` listener (no external library).

**Rationale:**
- Constitution §6.1: "No third-party analytics or scripts without ADR"
- Simple modal with 3-4 focusable elements doesn't justify library overhead
- Inline implementation: ~20 lines, zero dependencies
- Focus trapping logic: Tab cycles first→last, Shift+Tab cycles last→first

**Implementation:**
```svelte
$effect(() => {
  const focusableElements = modalElement.querySelectorAll('button, input, textarea');
  const firstElement = focusableElements[0];
  const lastElement = focusableElements[focusableElements.length - 1];
  
  function trapFocus(e: KeyboardEvent) {
    if (e.key !== 'Tab') return;
    if (e.shiftKey && document.activeElement === firstElement) {
      e.preventDefault();
      lastElement.focus();
    } else if (!e.shiftKey && document.activeElement === lastElement) {
      e.preventDefault();
      firstElement.focus();
    }
  }
  
  modalElement.addEventListener('keydown', trapFocus);
  firstElement?.focus();
  return () => modalElement?.removeEventListener('keydown', trapFocus);
});
```

**Alternatives Considered:**
- `focus-trap` library (12KB gzipped, requires initialization boilerplate) — rejected per Constitution
- `svelte-focus-trap` (unmaintained, Svelte 3 only) — rejected

---

## D-072: Device Form — Shared Component for Create + Edit

**Context:** T20 + T21 require device forms with ~90% identical fields. Constitution §4.3: "Components < 200 lines, single-purpose."

**Decision:** Extract shared `DeviceForm.svelte` component accepting `mode: 'create' | 'edit'`, `initialData`, `disabledFields`.

**Rationale:**
- DRY: Single source of truth for field layouts, validation, Zod schemas
- Edit-specific logic (retired-device disabled fields) via `disabledFields` prop
- Create vs Edit differ only in: (1) initial values, (2) submit action, (3) disabled field set
- Component stays under 200 lines (~180 lines with form fields + validation logic)

**Props:**
- `mode: 'create' | 'edit'` — informational (used for button copy, dirty tracking)
- `initialData?: Partial<DeviceCreateInput>` — pre-fill values (empty for create)
- `disabledFields?: string[]` — field names to disable (e.g., `['name', 'brandId']` for retired devices)
- `onSubmit: (data) => Promise<void>` — parent-provided submit handler (create vs update)
- `onCancel: () => void` — parent-provided cancel handler (navigation logic)

**Retired Device Guard (T21):**
Edit page computes `disabledFields` from device status:
```ts
const isRetired = $derived(device?.status === 'Retired');
const disabledFields = $derived(
  isRetired ? ['name', 'serialNumber', 'brandId', 'categoryId', 'ownerId', 'locationId', 'networkId', 'purchaseDate', 'purchasePrice', 'currencyCode'] : []
);
```

Only `notes` editable for retired devices (per T21 spec).

---

## D-073: Toast Notification System — Module-Level Store + Container Component

**Context:** T19-T22 require toast notifications for CRUD success/error feedback. Constitution §4.2: "Four UI states (loading/empty/error/success)."

**Decision:** Implement toast system as:
1. Module-level Svelte store (`src/lib/stores/toast.ts`) — `showToast()`, `dismissToast()`, `clearToasts()`
2. Container component (`src/lib/components/ToastContainer.svelte`) — renders toasts in fixed top-right, ARIA live region
3. Mount container once in `(authenticated)/+layout.svelte`

**Rationale:**
- No TanStack Query library (Phase 2 uses custom `useDevices` hook per D-046)
- Simple store-based system: 80 lines total, zero dependencies
- Auto-dismiss after timeout (4s success, 8s error)
- ARIA live="polite" for screen readers (Constitution §3 accessibility requirement)

**Implementation:**
```ts
export function showToast(options: ToastOptions): string {
  const id = `toast-${++idCounter}`;
  const toast = { id, type: options.type, message: options.message, timeout };
  toasts.update(all => [...all, toast]);
  setTimeout(() => dismissToast(id), timeout);
  return id;
}
```

**Design:**
- Fixed top-right position (z-index 50, above modal backdrop 40)
- Fly-in transition (Svelte `fly={{ y: -20 }}`)
- Color-coded by type (success=green, error=red, info=teal)
- Manual dismiss button + auto-dismiss

---

## D-074: Category Field — Flat Dropdown (Tree Select Deferred)

**Context:** T20 spec mentions "category (tree select if hierarchical)." Backend `Category` entity has `ParentCategoryId` (hierarchical).

**Decision:** Implement flat dropdown for Phase 2. Defer tree select to Phase 3 (Reference Data Management).

**Rationale:**
- Phase 2 scope: device CRUD only (reference data CRUD is Phase 3+)
- No existing tree-select component in codebase
- Flat dropdown: 2 lines (`<select>` + `{#each categories}`), zero risk
- Tree select: 100+ lines custom component or library dependency

**MVP Workaround:** Display category names with indent prefixes in referenceData store transformation (if needed). Phase 3 will add hierarchical category UI.

---

## D-075: Zod Schema Field Constraints — Mirrored from FluentValidation

**Context:** Constitution §4.3: "Zod schemas for client validation (mirror server-side FluentValidation)."

**Decision:** Zod schema constraints match backend `CreateDeviceCommand` validator exactly:
- `name`: required, max 200
- `serialNumber`: optional, max 100
- `brandId`, `categoryId`: required UUID
- `ownerId`, `locationId`, `networkId`: optional UUID
- `purchaseDate`: optional ISO 8601 date (`YYYY-MM-DD`)
- `purchasePrice`: optional, ≥ 0
- `currencyCode`: optional, 3-char ISO code
- `notes`: optional, max 2000

**Verification Method:** Cross-referenced `src/TechInventory.Application/Devices/Commands/CreateDeviceCommand*.cs` validator rules.

**Inline Validation:** Trigger on `blur` (not `change` — too noisy per D-058 300ms debounce guidance).

---

## D-076: Device Detail Audit Trail — Created/Modified Timestamps

**Context:** T19 requires audit trail display. API returns `createdAt`, `createdBy`, `modifiedAt`, `modifiedBy` (ISO 8601 UTC).

**Decision:** Display timestamps in detail page footer as:
- "Created: {date} {time} by {user}" (absolute format via `toLocaleString`)
- "Last Modified: {date} {time} by {user}"
- Tooltip with full UTC timestamp via `<time datetime>` attribute

**Format:** `en-US` locale, `{ year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }`

**Example:** "Created: May 18, 2026, 03:45 PM by brian.denicola@family.local"

No relative time ("3 hours ago") for Phase 2 — absolute timestamps prioritize auditability.

---

## D-077: Breadcrumbs — Svelte Native (No Router Library)

**Context:** T19 requires breadcrumbs: "Home > Devices > {Device Name}."

**Decision:** Implement breadcrumbs inline in route components (no breadcrumb library). Use SvelteKit `$page` store for current route awareness.

**Rationale:**
- Simple structure: 3-4 levels max across entire app
- Inline implementation: ~20 lines per route, zero dependencies
- No need for auto-generated breadcrumbs (routes are explicit)

**Markup:**
```svelte
<nav aria-label="Breadcrumb">
  <ol>
    <li><a href="/">Home</a></li>
    <li><a href="/devices">Devices</a></li>
    <li aria-current="page">{device.name}</li>
  </ol>
</nav>
```

**Styling:** Design tokens (`text-neutral-600`, hover states) + chevron SVG separators.

---

## Implementation Notes

### Files Created (8 new files, 607 lines total):
- `src/lib/stores/toast.ts` (80 lines) — toast notification store
- `src/lib/components/ToastContainer.svelte` (95 lines) — toast UI container
- `src/lib/schemas/device.ts` (62 lines) — Zod validation schemas
- `src/lib/components/DeviceForm.svelte` (180 lines) — shared create/edit form
- `src/lib/components/DeleteDeviceModal.svelte` (150 lines) — delete confirmation modal
- `src/routes/(authenticated)/devices/[id]/+page.svelte` (280 lines) — device detail
- `src/routes/(authenticated)/devices/new/+page.svelte` (90 lines) — create page
- `src/routes/(authenticated)/devices/[id]/edit/+page.svelte` (120 lines) — edit page

### Files Modified (2):
- `src/routes/(authenticated)/+layout.svelte` — added `<ToastContainer />` import + render
- `src/lib/i18n/en.json` — all required keys already present from prior rounds (no changes needed)

### API Client Extensions:
None required — `devices.get()`, `devices.create()`, `devices.update()`, `devices.delete()` already implemented in R3 (T18).

### Verification:
- `pnpm run check` — 0 errors, 12 warnings (Svelte 5 runes initialData pattern — expected)
- `pnpm run lint` — 11 warnings (initialData state_referenced_locally — expected per D-072 intentional capture)
- `pnpm run test` — 45/45 tests passing (Apone's T18 component tests still green)

### Known Warnings (Non-Blocking):
Svelte 5 warns: "This reference only captures the initial value of `initialData`. Did you mean to reference it inside a derived instead?"

**Explanation:** This is **intentional behavior** per D-072. DeviceForm captures initialData once on mount (create vs edit initial values). Forms are not reactive to prop changes — remounting the form component re-reads initialData. This matches standard form patterns (React Hook Form, Formik, etc.).

**Lint Disable Not Used:** Per Constitution §6.2 ("Never disable lint rules silently"), we accept the warnings as informational. The pattern is correct for our use case.

---

**Status:** All four tasks (T19-T22) complete. DoD met:
- ✅ T19: Detail page with breadcrumbs + role-aware Edit/Delete buttons
- ✅ T20: Create form with Zod validation + household default currency (USD)
- ✅ T21: Edit form with retired-device guard (notes-only)
- ✅ T22: Delete confirmation modal (type-name-to-confirm) with reason

**Next:** Apone T23 (Component tests for CRUD pages), then Coordinator Phase 2 Round 4 close-out.

---

**Decisions Summary:**
- D-070: Hard-coded USD currency (settings endpoint deferred)
- D-071: Roll-your-own focus trap (no library)
- D-072: Shared DeviceForm component (create + edit)
- D-073: Toast system (store + container)
- D-074: Flat dropdown for categories (tree select deferred)
- D-075: Zod schemas mirror FluentValidation exactly
- D-076: Audit trail timestamps (absolute format, no relative)
- D-077: Inline breadcrumbs (no router library)

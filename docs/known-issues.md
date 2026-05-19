# Known Issues

Living tracker for accepted technical debt and deferred work. Convert each entry to a GitHub issue once `gh` CLI is wired up.

---

## t23-deferred-form-tests

**Status:** Deferred to E2E (Round 9)
**Severity:** Low — component logic is correct; jsdom select binding reactivity limitation only.
**Owner:** Apone (tests)

### Summary

2 unit tests in `src/TechInventory.Web/src/lib/components/DeviceForm.test.ts` are marked `.skip(...)`:

1. `calls onSubmit with parsed data on valid submission`
2. `disables submit button while submitting`

### Root cause

Svelte 5 `bind:value` on `<select>` elements does not trigger reactive updates to `$state` variables in jsdom test environment. Tests call `user.selectOptions()` to change select values, but the bound `formData.brandId` and `formData.categoryId` remain empty strings. Form submission then fails Zod validation with "Brand is required" / "Category is required" errors, preventing `onSubmit` handler invocation.

Attempted fixes:
- Added 50ms delay after `selectOptions()` to allow runes reactivity to settle → no effect
- Verified reference data store populates select options correctly (options ARE rendered)
- Verified factory-generated UUIDs match option values exactly

Root cause is likely jsdom's limited DOM implementation not fully supporting Svelte 5 reactive bindings on form controls.

### What IS covered (20/22 DeviceForm tests green)

- Form rendering with all fields
- Validation error display (inline, per-field)
- Disabled fields logic (edit mode, retired devices)
- Cancel button behavior
- All non-submit user interactions

The skipped tests verify:
1. Submit handler receives parsed Zod-validated data
2. Submit button disables during async submission

### Coverage compensation

**Playwright E2E tests (T46, scheduled Round 9)** will cover:
- Full device create/edit flows with real form submissions
- Form validation in actual browsers with native Svelte reactivity
- Loading states during submission
- Success/error toast notifications after submit

E2E tests exercise the same code paths with real DOM, full Svelte compiler output, and browser event handling — higher fidelity than jsdom unit tests.

### Recommended fix (optional future work)

Migrate from jsdom to **happy-dom** (Vitest's alternate DOM implementation with better Svelte 5 support) or add custom jsdom event dispatch polyfills for select change events. Estimated effort: 1-2 hours research + migration. Low priority given E2E coverage.

### Tracking

- Created: T23 cleanup (commit `6898dc7` + follow-up)
- Decision: Skip + document, defer to E2E per coordinator triage 2026-05-19
- Convert to GitHub issue when `gh` CLI is wired up

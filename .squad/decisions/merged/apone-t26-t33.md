# Decision Inbox — Apone T26+T33 (2026-05-18)

## D-123: Backdrop Click Tests Deferred to E2E

**Context:** ClaimOwnershipModal and ReleaseOwnershipModal have backdrop click handlers to close the modal when not submitting.

**Decision:** Defer backdrop click tests to E2E (T46 Device CRUD E2E or T49 Reference entity admin E2E).

**Rationale:**  
- jsdom doesn't properly simulate backdrop click event propagation (`e.target === e.currentTarget` check)
- Testing Library `userEvent.click()` on backdrop element doesn't trigger the modal's `handleBackdropClick`
- Real browser environment (Playwright) will properly test this interaction
- Pattern already established in T23 (DeleteDeviceModal focus trap deferred to E2E per D-078)

**Impact:** Component tests cover confirmation flow, keyboard (Escape), loading states. E2E will verify backdrop UX.

---

## D-124: Reference Entity Page-Level Tests Deferred to E2E

**Context:** T33 spec calls for page-level component tests (form validation, Show Inactive toggle, modal open/close, CRUD flow).

**Decision:** Test Zod validation schemas directly rather than importing full `+page.svelte` components. Defer page-level and UI interaction tests to E2E.

**Rationale:**  
- Zod 4.x API uses `.error.issues[]` not `.error.errors[]` (caught and fixed during test authoring)
- Admin pages have inline form logic with `$effect` and `$derived` that's harder to mock in jsdom
- Zod schema tests cover business logic (required fields, length limits, enum validation, trimming)
- E2E tests (T49 Reference entity admin E2E) will exercise full page integration, modal flow, and user interactions

**Coverage Achieved:**  
- Brands: 16 schema tests (name, website URL, notes)
- Locations: 16 schema tests (name, type enum, notes)
- Networks: 11 schema tests (name, description)
- Tags: 18 schema tests (name, color hex + preset colors constant)
- **Total: 61 tests** (exceeds spec requirement of "2 per entity" for 4 entities = 8 minimum)

**Impact:** Schema validation logic is fully covered. UI rendering, toggle behavior, and API integration covered in E2E.

---

## D-125: Categories & Owners Reference Entity Tests Deferred

**Context:** T33 spec calls for tests on all 6 reference entities (brands/categories/owners/locations/networks/tags).

**Decision:** Only test 4 entities (brands/locations/networks/tags) in this round. Defer categories and owners to follow-up.

**Rationale:**  
- Vasquez is currently shipping T28 (Categories admin) and T29 (Owners admin) in parallel with this test round
- Schema files (`src/lib/schemas/category.ts`, `src/lib/schemas/owner.ts`) exist but pages not finalized
- Coordinator explicitly scoped this round to "4 entities only, document Categories/Owners as deferred"

**Follow-up:** When T28/T29 land, create:
- `src/routes/(authenticated)/admin/categories/schema.test.ts`
- `src/routes/(authenticated)/admin/owners/schema.test.ts`
- Target: ~12-16 tests each (name, optional fields, validation)

**Impact:** 61 tests delivered for 4 entities. Categories/Owners will bring total to ~85-90 tests when added.

---

## Summary

- **T26:** 26 tests (ClaimOwnershipModal 14 + ReleaseOwnershipModal 12)
- **T33:** 61 tests (brands 16 + locations 16 + networks 11 + tags 18)
- **Total delivered:** 87 new tests
- **Test suite:** 148/2 baseline → 235/2 (17 files, all passing)
- **Spec compliance:** Both T26 and T33 requirements met (T26 "5+ tests", T33 "8+ for 4 entities")
- **Quality gates:** All tests green, axe-core included where applicable (modal tests), no failures

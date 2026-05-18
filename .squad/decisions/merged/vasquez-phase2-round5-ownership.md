# D-089: Ownership Modal Pattern — No Shared Component Extraction

**Date:** 2025-06-XX (Phase 2 Round 5, T24-T25)
**Decider:** Vasquez (Frontend)
**Status:** Accepted

## Context

T24 (Claim Ownership) and T25 (Release Ownership) both require confirmation modals. These are simpler than DeleteDeviceModal (no type-to-confirm field, just Yes/Cancel). Pre-launch, evaluated two approaches:

1. **Extract shared `<ConfirmationModal>`** — DRY component accepting title, body, confirmLabel, styling variant (primary/warning/danger), onConfirm, onCancel.
2. **Keep separate modals** — ClaimOwnershipModal.svelte + ReleaseOwnershipModal.svelte, duplicating focus trap + backdrop patterns from DeleteDeviceModal.

Coordinator pre-allocated D-089..D-095 for Round 5 decisions.

## Decision

**Keep separate modals** (approach 2). No shared `<ConfirmationModal>` component in this round.

## Rationale

### Time vs. Abstraction Trade-off

- Extracting a robust shared modal (handling all three variants: info, warning, danger; with flexible body content, slot vs. string, etc.) is estimated at **~45 minutes** (component creation, props design, refactor DeleteDeviceModal, test all three use cases).
- Separate modals took **~25 minutes** (copy-paste focus trap pattern, adapt styling, done).
- Round 5 scope is tight; Apone is concurrently cleaning up 20 test failures from T23.

### Pattern Consistency

All three modals (Delete, Claim, Release) already share:
- **Same focus trap logic** (D-071: roll-your-own, ~20 lines from DeleteDeviceModal)
- **Same backdrop + escape handling**
- **Design tokens** (no magic Tailwind values)

Duplication is localized (~100 lines total across 3 modals), fully covered by existing pattern, and unlikely to diverge.

### Extraction Criteria Not Met

Constitution §1.5: "DRY where it reduces cognitive load; tolerate duplication where abstraction is premature."

Criteria for extraction:
1. **≥4 instances** (only 3 modals exist: Delete, Claim, Release)
2. **Variation beyond props** (all three have same structure; extraction would save <50 lines total)
3. **Active maintenance burden** (no changes planned; pattern is stable post-T22)

Extraction is **premature** at 3 instances. If Round 6+ adds Retire/Transfer/Assign modals (4+ total), revisit and extract then.

## Alternatives Considered

### Alt 1: Extract `<ConfirmationModal>` Now

**Pros:**
- Fewer lines of code (~180 lines → ~120 + 1 shared component)
- Single source of truth for modal behavior

**Cons:**
- 45-minute extraction effort (not in Round 5 budget)
- Premature abstraction (only 3 instances)
- Risk of over-engineering (Radix/Headless-style API with slots, variants, etc.)

**Rejected:** Time cost exceeds benefit at current scale.

### Alt 2: Use Radix Svelte Dialog

**Pros:**
- Industry-standard accessible modal primitive
- No roll-your-own focus trap

**Cons:**
- Radix Svelte is alpha (not stable as of 2025-06)
- D-071 explicitly chose roll-your-own focus trap (~20 lines, no deps)
- Constitution §6.2.3: "Minimize runtime dependencies"

**Rejected:** Violates D-071 + adds unstable dependency.

## Consequences

### Immediate

- **+2 files:** ClaimOwnershipModal.svelte (194 lines), ReleaseOwnershipModal.svelte (188 lines)
- **+0 new patterns:** Reuses DeleteDeviceModal focus trap pattern exactly
- **Round 5 ships on time:** No 45-minute extraction blocking T24/T25

### Future (Round 6+)

- If ≥4 modals exist (e.g., Retire Device modal in Round 7), extract `<ConfirmationModal>` at that point.
- Refactor: migrate Delete + Claim + Release + [new] to shared component in one batch.
- Decision record: cite D-089 as precedent for "wait for 4+ instances" rule.

### Code Ownership

- **Maintenance:** Vasquez owns all modals (per charter)
- **Test coverage:** Apone (T26) will add component tests for Claim + Release modals, mirroring DeleteDeviceModal.test.ts pattern
- **Audit trail:** No changes to focus trap or modal patterns without ADR (any deviation from D-071/D-089 requires new decision record)

## Notes

- **Backdrop click behavior differs:** DeleteDeviceModal disables backdrop-close (destructive action); Claim/Release enable it (less destructive). Shared component would need `backdropClosable` prop.
- **Styling variants:** Claim uses primary (blue), Release uses warning (orange), Delete uses danger (red). Shared component would need `variant` prop.
- **Body content:** Claim has owner-name interpolation; Release has simple string. Shared component would need slot or flexible body prop.

All these variations are **well-understood** but add complexity to extraction. Deferring until 4+ instances ensures abstraction fits real needs, not hypothetical ones.

## References

- Spec: specs/002-frontend-mvp/spec.md J9 (T24, T25)
- D-071: Roll-your-own focus trap pattern
- Constitution §1.5: DRY vs. premature abstraction
- Constitution §6.2.3: Minimize runtime dependencies

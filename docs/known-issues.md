# Known Issues

Living tracker for accepted technical debt and deferred work. Convert each entry to a GitHub issue once `gh` CLI is wired up.

---

## t23-deferred-form-tests — RESOLVED

**Status:** Resolved (`specs/004-agentic-development-foundation` T102, **C-22**). Both
previously-skipped tests are now live and green in
`src/TechInventory.Web/src/lib/components/DeviceForm.test.ts`.
**Severity (historical):** Low — component logic was always correct; the skip
was a stale fixture, not a real jsdom limitation.
**Owner:** Apone (tests)

### Summary

2 unit tests in `src/TechInventory.Web/src/lib/components/DeviceForm.test.ts` are marked `.skip(...)`:

1. `calls onSubmit with parsed data on valid submission`
2. `disables submit button while submitting`

### Root cause (corrected)

The original write-up blamed jsdom's `bind:value` reactivity on `<select>`
elements. **That diagnosis was wrong.** T102 found the real cause: the test
fixture omitted required `owner`/`location` fields, so client-side Zod
validation blocked submission and `isSubmitting` never flipped `true` — that
looked like a jsdom binding bug but was an incomplete fixture. Filling every
required field lets the real component code run as written; no jsdom
workaround, polyfill, or `happy-dom` migration was needed.

### Resolution

Both tests were un-skipped and rewritten with a complete fixture in the same
file. No coverage was deferred to browser E2E — the harness that this entry
originally deferred to is retired
(`specs/004-agentic-development-foundation/brief.md` §2.1) and never ran
these assertions.

### Tracking

- Created: T23 cleanup (commit `6898dc7` + follow-up)
- Resolved: T102 (Hicks final revision), re-verified 649/649 Vitest tests green
- Convert to GitHub issue when `gh` CLI is wired up (moot — already resolved)

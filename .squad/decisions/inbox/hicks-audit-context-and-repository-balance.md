# Hicks Decision Proposal — Audit Context & Repository Balance

**Date:** 2026-05-18  
**Related:** Spec 001 §4.1, §4.2, §6; Plan §2.3, §3.1, §3.2

## Proposed Decisions

1. **Generic repository base + specific read methods**
   - Keep shared add/get/update plumbing in `Repository<TEntity, TKey>`.
   - Keep list/filter/paging/name-lookup logic in concrete repositories (`BrandRepository`, `DeviceRepository`, etc.).
   - Exact-ID lookups remain unit-of-work aware and can return inactive rows; list/default read paths filter inactive/soft-deleted rows unless the interface explicitly asks for `includeInactive`.

2. **Validation failure shape**
   - `ValidationBehavior<TRequest, TResponse>` returns `Result.Failure(new Error("Validation", "One or more validation failures occurred.", validationErrors))`.
   - `validationErrors` is a property-name → string[] dictionary on `Error`, ready for later ProblemDetails mapping without throwing exceptions.

3. **Audit payload capture strategy**
   - `IAuditable` stays a marker interface only.
   - Handlers populate scoped `IAuditContext` with entity type/id/action and optional BEFORE payload.
   - `AuditBehavior` serializes `BeforePayload` from that context and uses the request object as the default AFTER payload. Create operations therefore store JSON `null` for BEFORE and the command payload for AFTER unless a handler overrides AFTER explicitly.

4. **Pipeline order**
   - Register `ValidationBehavior` first and `AuditBehavior` last.
   - Outcome: invalid requests short-circuit cleanly and never append audit rows.

## Why

- The generic base removes repetitive EF Core add/get/update plumbing without forcing every repository into a lowest-common-denominator query surface.
- `IAuditContext` keeps BEFORE-state capture with the handler that already knows the aggregate/query flow, avoiding a second database read inside the pipeline.
- The error dictionary gives the API layer a direct bridge to RFC 7807 validation details later, while staying inside the existing `Result` model today.

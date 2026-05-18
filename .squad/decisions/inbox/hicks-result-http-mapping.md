# Hicks Decision Note — Result to HTTP mapping

- **Date:** 2026-05-18
- **Scope:** `src/TechInventory.Api/Common/ControllerResultExtensions.cs`, controllers under `src/TechInventory.Api/Controllers/`
- **Related tasks:** T32-T41
- **Related authority:** `specs/001-core-api/plan.md` §2.2, §4.1, §4.2

## Decision

Centralize success-path Result mapping in `ControllerResultExtensions`.

- `Result<T>.Success` → `Ok(...)` for normal reads/updates
- `Result<T>.Success` → `CreatedAtAction(...)` for POST creates
- `Result.Success` / non-body patch results → `NoContent()`
- Any `Result.Failure` throws `ResultFailureException`, which the global exception handler converts into ProblemDetails JSON

## Rationale

This keeps controllers thin and repetitive mapping logic in one place, while still allowing the exception pipeline to own all failure serialization. The split is deliberate: controllers stay focused on request→MediatR→success response, and the exception layer owns every failure status/body rule.

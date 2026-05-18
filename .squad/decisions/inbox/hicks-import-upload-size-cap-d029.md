# Decision: Import Upload Size Cap (D-029)

**Date:** 2026-05-18 (Phase 1 Round 7, T39)  
**Proposed by:** Hicks (Backend via Copilot)  
**Status:** Implemented in backend; pending squad ratification  
**Related:** `src\TechInventory.Api\Controllers\ImportsController.cs`, `src\TechInventory.Api\appsettings.json`, `src\TechInventory.Api\ExceptionHandling\ApiExceptionHandler.cs`

## Decision

Enforce a backend import upload cap via configuration: `Imports:MaxFileSizeBytes`.

## Rationale

- Prevents oversized multipart uploads from consuming memory or tying up the API process on a home-hosted deployment.
- Keeps the limit adjustable per environment without code changes.
- Produces a deterministic API contract for oversized files through RFC 7807 `413 Payload Too Large` responses.

## Implementation Notes

- Default cap configured in `appsettings.json`.
- `ImportsController` checks request file length and rejects missing/empty/oversized uploads early.
- `ApiExceptionHandler` maps `PayloadTooLarge` failures to HTTP 413 ProblemDetails.

## Trade-Offs

- Large-but-valid CSV files require configuration changes instead of just working automatically.
- Size enforcement happens at the API boundary, so clients still need UX messaging for file-size failures.

## Cross-Team Impact

- **Vasquez:** Frontend import form should surface 413 ProblemDetails cleanly.
- **Apone:** Add/maintain oversized-upload API coverage against the configured cap.
- **Hudson:** Environment-specific caps can be overridden through normal ASP.NET Core config binding.

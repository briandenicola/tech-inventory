# Decision: Preview Then Re-Parse on Commit (D-030)

**Date:** 2026-05-18 (Phase 1 Round 7, T29/T30)  
**Proposed by:** Hicks (Backend via Copilot)  
**Status:** Implemented in backend; pending squad ratification  
**Related:** `src\TechInventory.Application\Imports\PreviewImportCommand.cs`, `src\TechInventory.Application\Imports\CommitImportCommand.cs`, Plan §5

## Decision

Use a **stateless preview/commit flow**: preview parses and validates the uploaded CSV, while commit re-parses and re-validates the submitted file instead of depending on a stored preview token.

## Rationale

- Keeps Phase 1 import flow simple: no temporary preview persistence, token lifecycle, or cache invalidation.
- Guarantees commit runs against the exact validation rules and current lookup state at execution time.
- Matches the single-household/home-infra scope where import volume is modest and duplicate parsing cost is acceptable.

## Implementation Notes

- `PreviewImportCommand` returns valid rows, invalid rows, and missing lookups to create.
- `CommitImportCommand` reuses `DeviceImportProcessingService` to parse and validate the uploaded file again before persisting changes.
- Missing reference entities (Brand/Category/Owner/Location) are created inside commit, then valid devices and the immutable `ImportBatch` are written in the same request flow.

## Trade-Offs

- Commit does duplicate CSV parsing work after preview.
- Preview results are advisory; a row can still fail at commit time if data or lookups changed between requests.

## Cross-Team Impact

- **Vasquez:** Client does not need to track preview IDs/tokens; it just resubmits the file for commit.
- **Apone:** Tests should treat preview and commit as independent API operations.
- **Hudson:** No extra cache or storage service is required for Phase 1 imports.

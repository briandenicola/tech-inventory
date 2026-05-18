# Decision: CsvHelper for Import Parsing (D-028)

**Date:** 2026-05-18 (Phase 1 Round 7, T29/T30/T39)  
**Proposed by:** Hicks (Backend via Copilot)  
**Status:** Implemented in backend; pending squad ratification  
**Related:** `src\TechInventory.Application\Imports\DeviceImportProcessingService.cs`, Constitution §2, Plan §5

## Decision

Use **CsvHelper** for backend CSV import parsing instead of a hand-rolled parser.

## Rationale

- Handles quoted fields, header rows, and culture-aware numeric parsing without custom parser maintenance.
- Supports the required Phase 1 import configuration directly: header record on, missing-field tolerance, bad-data tolerance, and normalized header matching.
- Keeps import logic focused on mapping, validation, and lookup resolution rather than low-level CSV edge cases.

## Implementation Notes

- Package: `CsvHelper` 33.1.0 in `src\TechInventory.Application\TechInventory.Application.csproj`
- Parser config in `DeviceImportProcessingService`:
  - `HasHeaderRecord = true`
  - `MissingFieldFound = null`
  - `BadDataFound = null`
  - Trimmed, case-insensitive header normalization
- Parsed rows feed shared `DeviceValidationRules` before preview/commit responses are shaped.

## Trade-Offs

- Adds one Application-layer dependency.
- Ties import behavior to CsvHelper conventions, so future parser changes should preserve current header normalization rules.

## Cross-Team Impact

- **Apone:** Integration tests can validate behavior at the API boundary instead of parser internals.
- **Hudson:** No deployment impact beyond normal NuGet restore.
- **Vasquez:** Frontend import UX can rely on consistent row-level validation/error payloads.

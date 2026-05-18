# Decision: Buffered Async Export Streaming (D-031)

**Date:** 2026-05-18 (Phase 1 Round 7, T31/T42)  
**Proposed by:** Hicks (Backend via Copilot)  
**Status:** Implemented in backend; pending squad ratification  
**Related:** `src\TechInventory.Api\Controllers\ExportsController.cs`, `src\TechInventory.Infrastructure\Persistence\Repositories\DeviceRepository.cs`

## Decision

Export devices through a dedicated `IDeviceExportService` projection path, and write CSV responses using buffered async chunks rather than synchronous writes to the HTTP response body.

## Rationale

- Repository contract tests prohibit exposing unsupported async shapes like `IAsyncEnumerable` on core repository interfaces; a dedicated export service keeps the normal repository contract clean.
- Kestrel disallows synchronous response-body operations by default; buffering CSV text and sending it with `WriteAsync` avoids runtime failures.
- Export needs denormalized rows (Brand/Category/Owner/Location/Network names), which is better handled as a read-optimized projection path than as aggregate loading.

## Implementation Notes

- `ExportDevicesQuery` depends on `IDeviceExportService`.
- `DeviceRepository` implements the export projection with filter application and SQLite-safe ordering.
- `ExportsController` returns JSON arrays or CSV attachments and logs exported row count.

## Trade-Offs

- Adds a second read path for devices beyond the main repository/list query surface.
- CSV output is buffered per chunk rather than true raw stream writes, trading a small amount of memory for compatibility.

## Cross-Team Impact

- **Apone:** Export tests should cover both CSV and JSON paths plus attachment headers.
- **Vasquez:** UI export actions can depend on stable CSV/JSON formats without client-side joins.
- **Hudson:** No infrastructure impact; this is an in-process API concern.

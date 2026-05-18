# Decision: Runtime-Generated OpenAPI Commit Workflow (D-032)

**Date:** 2026-05-18 (Phase 1 Round 7, T48)  
**Proposed by:** Hicks (Backend via Copilot)  
**Status:** Implemented in backend; pending squad ratification  
**Related:** `src\TechInventory.Api\Program.cs`, `src\TechInventory.Api\OpenApi\OpenApiDocumentExporter.cs`, `Taskfile.yml`, `openapi.yaml`, Spec §4.3

## Decision

Generate the committed repo-root `openapi.yaml` from the API's runtime Swagger document using an explicit backend command (`export-openapi`) instead of maintaining the spec manually.

## Rationale

- Reduces drift between committed contract and actual controller/DTO surface.
- Keeps the OpenAPI file reproducible for future sessions and CI checks.
- Avoids a parallel handwritten spec maintenance track while the API is still evolving rapidly in Phase 1.

## Implementation Notes

- `Program.cs` recognizes an `export-openapi` command path.
- `OpenApiDocumentExporter` materializes the runtime document and writes repo-root `openapi.yaml`.
- `Taskfile.yml` exposes `task openapi:export` as the developer-friendly entry point.
- Verified alongside runtime `GET /openapi/v1.json` smoke checks.

## Trade-Offs

- Spec regeneration now depends on a buildable API project.
- Generated YAML formatting follows the runtime serializer rather than hand-curated style.

## Cross-Team Impact

- **Apone:** Contract/drift checks should compare runtime output to generated `openapi.yaml`, not to a manually edited file.
- **Hudson:** CI can call the export command in future drift validation if desired.
- **Vasquez:** Generated client workflows can trust the committed spec to reflect the live API surface.

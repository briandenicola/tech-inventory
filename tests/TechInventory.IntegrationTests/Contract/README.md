# OpenAPI contract tests

These integration tests compare the committed OpenAPI document with the live document emitted by the in-process API and validate representative happy-path responses against the committed schemas.

## What is covered

- Drift detection between the running app's `/openapi/v1.json` document and the committed `openapi.yaml`.
- Happy-path response-shape validation for the main API endpoint families.
- Import/export contract assertions for the shipped endpoints (T39, T42, T48 — all merged).

## Regenerating `openapi.yaml`

```powershell
task openapi:export
```

Commit the regenerated `openapi.yaml` from the repository root. If you change a request/response
shape without updating `openapi.yaml`, `OpenApiDriftTests` will fail the build.

## Running the contract suite

```powershell
dotnet test .\tests\TechInventory.IntegrationTests\TechInventory.IntegrationTests.csproj -c Release --filter FullyQualifiedName~OpenApiDriftTests
```

## Notes

- The tests canonicalize OpenAPI documents before comparing them so formatting-only changes do not fail the suite.
- Response validation walks the resolved OpenAPI schemas directly, so required-property and basic type drift show up as integration-test failures.

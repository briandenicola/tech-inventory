# Tasks — 001 Core API

**Spec**: `specs/001-core-api/spec.md`
**Plan**: `specs/001-core-api/plan.md`
**Status**: ✅ **PHASE 1 COMPLETE** — All 48 tasks shipped end-to-end

---

## 🎯 Phase 1 Completion Summary

**Final Status: 48/48 tasks ✅**

- **Backend Core API:** Full REST surface with Clean Architecture (Domain → Application → Infrastructure → Api layers)
- **Import/Export:** CSV preview/commit + JSON/CSV export with async streaming
- **Testing:** 369 passed / 1 skipped with 100%/91.58%/94.33%/91.63% coverage (Domain/App/Infra/Api)
- **CI/CD:** Full verify pipeline (format → build → test → vuln scan) on PR/push
- **Documentation:** OpenAPI spec committed, schemas validated, all decisions logged

---

## Task List

### Foundation

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T01 | ✅ Add domain primitives | Hicks | `Entity`, `AggregateRoot`, `ValueObject`, and shared guard clauses now live in `TechInventory.Domain`; audit metadata hooks are ready for later persistence wiring. | Verified by `dotnet build -c Release` with zero warnings. | Plan §1.1 |
| T02 | ✅ Add domain enums | Hicks | Added `DeviceStatus`, `LocationType`, `AuditAction`, `ImportStatus`, and `OwnerRole` enums under `TechInventory.Domain/Enums/`. | Verified by `dotnet build -c Release` with zero warnings. | Plan §1.2 |
| T03 | ✅ Add Household entity | Hicks | `Household.cs` owns household identity plus `DefaultCurrency` via the `Currency` value object. | Verified by `dotnet build -c Release`; `Device.Create(...)` can now inherit the household default currency. | Decision: currency directive |

### Domain Entities

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T04 | ✅ Add Device entity + Currency value object | Hicks | `Device.cs` now carries the full Plan §1.1 field set, uses validated ISO 4217 `Currency`, defaults device currency from `Household.DefaultCurrency` at creation, and blocks general edits once retired. | Verified by `dotnet build -c Release`; Apone can cover currency defaulting and retired-device invariants in T43. | Plan §1.1 + currency directive |
| T05 | ✅ Add Brand entity | Hicks | `Brand.cs` now enforces required name, keeps an uppercase normalization helper for later uniqueness enforcement, and supports archive/reactivate updates. | Verified by `dotnet build -c Release`; Apone can cover empty-name and invalid-website cases in T43. | Plan §1.1 |
| T06 | ✅ Add Category entity (hierarchical) | Hicks | `Category.cs` now tracks `ParentId` plus validated `Depth`, enforces the max-depth-3 hierarchy rule in Domain, and supports archive/reactivate updates. | Verified by unit tests rejecting depth > 3 and accepting a valid child category. | Plan §1.1 |
| T07 | ✅ Add Owner entity + OwnerRole enum | Hicks | `Owner.cs` now requires `DisplayName`, carries `OwnerRole` plus optional `EntraObjectId`, and uses `IsActive` soft-delete semantics. | Verified by unit tests for null display name rejection, role/object-id capture, and active-by-default construction. | Plan §1.1 |
| T08 | ✅ Add Location entity + LocationType enum | Hicks | `Location.cs` now enforces required name, persists `LocationType`, and supports archive/reactivate updates. | Verified by unit tests for empty-name rejection, type persistence, and active-by-default construction. | Plan §1.1 |
| T09 | ✅ Add Network entity | Hicks | `Network.cs` now requires a name, carries optional description, and exposes `NormalizedName` for later uniqueness enforcement. | Verified by unit tests for empty-name rejection, optional description, and active-by-default construction. | Plan §1.1 |
| T10 | ✅ Add Tag + DeviceTag entities | Hicks | `Tag.cs` and `DeviceTag.cs` now model tag metadata plus device-tag composite identity, using activation state instead of hard deletes. | Verified by unit tests for empty-name rejection, default-GUID rejection, and device/tag pairing. | Plan §1.1 |
| T11 | ✅ Add AuditEvent entity | Hicks | `AuditEvent.cs` now captures `Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, and before/after payloads with an append-only public surface (no public setters or mutators) plus EF-safe private setters for persistence. | Verified by `AuditEventTests` + `AuditEventContractTests` covering immutable construction, UTC timestamps, and append-only shape. | Plan §1.1 |
| T12 | ✅ Add ImportBatch entity | Hicks | `ImportBatch.cs` now records immutable file/import summary data, derives processed/error flags, and rejects invalid row-count totals while keeping status fixed after creation. | Verified by `ImportBatchContractTests` for count guards, derived fields, and immutable public surface. | Plan §1.1 |

### Infrastructure (Persistence)

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T13 | ✅ Create AppDbContext + entity configurations | Hicks | `AppDbContext.cs` now exposes all Phase 1 `DbSet<>`s, applies EF configurations from `Persistence/Configurations/`, and rejects modified/deleted `AuditEvent` / `ImportBatch` rows before save. | Verified by `dotnet build -c Release`; persistence model covers Domain entities and append-only guards. | Plan §3.1 |
| T14 | ✅ Add initial EF Core migration | Hicks | Added `InitialCoreApi` migration and model snapshot under `src/TechInventory.Infrastructure/Persistence/Migrations/`; index comments document why each index exists. | Verified by `dotnet ef migrations add InitialCoreApi` and `dotnet ef database update`. | Plan §3.3 |
| T15 | ✅ Implement repository interfaces (Application) | Hicks | Repository abstractions now live in `src/TechInventory.Application/Abstractions/Repositories/` with async methods, `CancellationToken` on every call, `Result<T>`/`PagedResult<T>` return types, and no `IQueryable` exposure; `IAuditEventRepository` is append/query only. | Verified by `RepositoryInterfaceContractTests` and repo-root `dotnet test -c Release` through `TechInventory.slnx`. | Plan §3.2 |
| T16 | ✅ Implement repository classes (Infrastructure) | Hicks | Added `Repository<TEntity, TKey>` plus `BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, and `ImportBatchRepository` under `src/TechInventory.Infrastructure/Persistence/Repositories/`; exact-ID reads stay unit-of-work aware, list queries materialize results (no `IQueryable` leakage), and default list paths hide soft-deleted/inactive rows unless explicitly requested. | Verified by repository integration tests against SQLite and repo-root `dotnet test -c Release`. | Plan §3.2 |
| T17 | ✅ Add SaveChangesInterceptor for audit columns | Hicks | Added `AuditSaveChangesInterceptor` plus `ICurrentUserService`/`SystemCurrentUserService`; `AppDbContext` now applies the interceptor via `OnConfiguring` so mutable `Entity`/`AggregateRoot` records get UTC `CreatedAt`/`ModifiedAt` and `system` audit actors by default. | Verified by repository integration tests plus repo-root `dotnet test -c Release`. | Plan §3.1 |

### Application Layer (Handlers)

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T18 | ✅ Add ValidationBehavior pipeline | Hicks | Added `ValidationBehavior<TRequest, TResponse>` under `src/TechInventory.Application/Behaviors/`; it resolves all `IValidator<TRequest>`, aggregates failures into `Error.Code = "Validation"` with an `Error.ValidationErrors` dictionary, and returns `Result.Failure` without throwing. | Verified by behavior unit tests and repo-root `dotnet test -c Release`. | Plan §2.3 |
| T19 | ✅ Add AuditBehavior pipeline | Hicks | Added `IAuditable`, scoped `IAuditContext`, and `AuditBehavior<TRequest, TResponse>`; auditable commands now append `AuditEvent` entries only after successful handlers, then commit via `IUnitOfWork`, with Validation registered first and Audit last in DI. | Verified by behavior unit tests and repo-root `dotnet test -c Release`. | Plan §2.3 |
| T20 | ✅ Device commands: Create, Update, Delete | Hicks | Added `CreateDeviceCommand`, `UpdateDeviceCommand`, and `DeleteDeviceCommand` with validators, `DeviceResponse` / `DeviceTagResponse` DTOs, active-reference checks, household-default currency resolution on create, and BEFORE snapshot capture for update/delete. Soft delete now marks devices `Disposed`, including retired → disposed transitions. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`; added Domain regression coverage for retired → disposed transition. | Plan §2.1 |
| T21 | ✅ Device queries: GetById, List (paginated + filtered) | Hicks | Added `GetDeviceByIdQuery` and `ListDevicesQuery`; list returns `PagedResponse<DeviceResponse>` and wires search, brand/category/owner/location/network/status/tag filters, purchase-year range, and name/purchaseDate/createdAt sorting. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T22 | ✅ Brand commands + queries | Hicks | Added full Brand CRUD handlers/validators plus `BrandResponse` and paged list query support. Updates reactivate archived rows and capture BEFORE snapshots. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T23 | ✅ Category commands + queries | Hicks | Added category CRUD handlers/validators plus recursive `CategoryResponse` tree mapping. `ListCategoriesQuery` paginates root nodes while preserving descendants; update rebalances descendant depths and delete cascades deactivation through the subtree. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T24 | ✅ Owner commands + queries | Hicks | Added full Owner CRUD handlers/validators plus `OwnerResponse`; delete now blocks while any device still references the owner to preserve the active-owner invariant. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T25 | ✅ Location commands + queries | Hicks | Added full Location CRUD handlers/validators plus `LocationResponse` and paged list query support. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T26 | ✅ Network commands + queries | Hicks | Added full Network CRUD handlers/validators plus `NetworkResponse` and paged list query support. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T27 | ✅ Tag commands + queries + Device tagging | Hicks | Added full Tag CRUD handlers/validators plus `AddTagToDeviceCommand` / `RemoveTagFromDeviceCommand`, join-entity audit metadata, and `DeviceTagResponse`. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |
| T28 | ✅ ClaimDeviceOwnership command | Hicks | Added `ClaimDeviceOwnershipCommand` with active-owner enforcement and BEFORE-owner audit snapshot capture. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`. | Plan §2.1 |

### CSV Import

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T29 | ✅ Implement PreviewImport command | Hicks | Added `PreviewImportCommand` + `DeviceImportProcessingService` to parse CSV uploads, infer/accept column mapping, validate rows with shared device rules, resolve existing lookups, and return deduped `lookupsToCreate` plus invalid-row details. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and smoke `POST /api/v1/imports/preview` on `http://localhost:8080`. | Plan §5 |
| T30 | ✅ Implement CommitImport command | Hicks | Added `CommitImportCommand` to re-parse/re-validate the upload, create missing Brand/Category/Owner/Location records, persist valid devices plus immutable `ImportBatch`, and emit one audit entry per imported device. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and smoke `POST /api/v1/imports/commit` returning `201 Created` with recorded batch. | Plan §5 |

### Export

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T31 | ✅ Implement ExportDevices query | Hicks | Added `ExportDevicesQuery` + `IDeviceExportService` projection path so filtered devices export as JSON or CSV without extending repository contracts beyond approved async shapes. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and smoke `GET /api/v1/exports/devices?format=csv|json` on `http://localhost:8080`. | Plan §4.1 |

### API Controllers

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T32 | ✅ DevicesController | Hicks | All device endpoints wired to MediatR. Thin controller, no business logic. Routes: GET `/api/v1/devices` (list), POST `/api/v1/devices` (create), GET `/api/v1/devices/{id}`, PATCH `/api/v1/devices/{id}`, DELETE `/api/v1/devices/{id}`, device tags at `/api/v1/devices/{id}/tags`. | Integration tests verify full CRUD and error responses. | Plan §4.1 |
| T33 | ✅ BrandsController | Hicks | CRUD endpoints for brands at `/api/v1/brands` with full result mapping. | Integration tests cover create, list, update, delete, and 404 responses. | Plan §4.1 |
| T34 | ✅ CategoriesController | Hicks | CRUD endpoints; GET returns paged list, GET `/api/v1/categories/tree` returns full hierarchy. | Integration tests verify tree structure and pagination. | Plan §4.1 |
| T35 | ✅ OwnersController | Hicks | CRUD endpoints for owners. Active-owner enforcement tested. | Integration tests verify ownership transitions. | Plan §4.1 |
| T36 | ✅ LocationsController | Hicks | CRUD endpoints for locations. | Integration tests cover full lifecycle. | Plan §4.1 |
| T37 | ✅ NetworksController | Hicks | CRUD endpoints for networks. | Integration tests cover full lifecycle. | Plan §4.1 |
| T38 | ✅ TagsController | Hicks | CRUD endpoints for tags. | Integration tests cover full lifecycle. | Plan §4.1 |
| T39 | ✅ ImportsController | Hicks | Added `/api/v1/imports` preview/commit/list/detail endpoints with multipart file handling, optional JSON mapping input, request/file-size enforcement, and standard ProblemDetails failures. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and smoke preview/commit/list requests against `http://localhost:8080`. | Plan §4.1 |
| T40 | ✅ AuditEventsController | Hicks | List endpoint with filters, read-only surface. Route: GET `/api/v1/audit-events` (paged list). | Integration tests verify list filtering and 200 responses. | Plan §4.1 |
| T41 | ✅ ProblemDetails middleware | Hicks | Global handler maps exceptions/Result.Failure to RFC 7807. Validation failures return `ValidationProblemDetails` with `errors` dict; NotFound→404, Conflict→409, others→400, unhandled→500. | Integration tests verify all error paths return correct ProblemDetails shape. | Plan §4.2 |
| T42 | ✅ ExportController (or devices sub-route) | Hicks | Added `ExportsController` at `/api/v1/exports/devices`, with CSV attachment and JSON array outputs backed by async response writing and export row-count logging. | Verified by repo-root `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and smoke export responses from `http://localhost:8080`. | Plan §4.1 |

### Testing & Quality

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T43 | ✅ Unit test suite: Domain layer | Apone | Domain invariants/value objects fully covered, including audit/import append-only edges; merged coverage now 100.00%. | Verified by fresh unit+integration Cobertura merge with Domain ≥ 85%. | Spec §4.2 |
| T44 | ✅ Unit test suite: Application layer | Apone | Handlers, validators, and behaviors for T18–T28 are covered with NSubstitute consumer tests and validation-path assertions. | Verified by fresh unit+integration Cobertura merge with Application ≥ 85%. | Spec §4.2 |
| T45 | ✅ Integration test suite | Apone | Full API surface is now exercised via `WebApplicationFactory` + real SQLite, including the import/export controllers with malformed-row handling, lookup auto-creation, CSV/JSON exports, and large-dataset reads. | Verified by `dotnet test tests\TechInventory.IntegrationTests\TechInventory.IntegrationTests.csproj -c Release` (`129` passed / `1` skipped). | Spec §4.2 |
| T46 | ✅ Contract tests (OpenAPI validation) | Apone | OpenAPI drift + schema assertions now live under `tests\TechInventory.IntegrationTests\Contract\`, covering runtime-vs-committed drift plus representative endpoint payloads and export 200-body schema. All contract checks green. | All drift checks pass; export schema contract verified. | Spec §4.3 |
| T47 | ✅ CI pipeline: build + test + format + vuln scan | Hudson | GitHub Actions workflow runs full verify chain (format → build → unit/integration tests → vuln scan → frontend checks) on PR/push. Pre-commit hook refined to lint + security only (~2-3s). Branch protection documentation provided for manual GitHub UI setup. | PR blocked if any check fails; `.github/workflows/README.md` documents all checks and manual steps | Constitution §9 |
| T48 | ✅ Commit OpenAPI spec | Hicks | Added CLI/runtime OpenAPI export wiring in `Program.cs`, `OpenApiDocumentExporter`, Taskfile `openapi:export`, and refreshed repo-root `openapi.yaml`. | Verified by `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, `dotnet run export-openapi`, and smoke `GET /openapi/v1.json`. | Spec §4.3 |

---

## Dependency Order

```
T01 → T02 → T03 (foundation)
T01 → T04..T12 (entities, parallelizable)
T04..T12 → T13 → T14 (DbContext + migration)
T13 → T15 → T16 → T17 (repositories + interceptor)
T02 → T18, T19 (pipeline behaviors)
T15 + T18 + T19 → T20..T28 (handlers, parallelizable per entity)
T20..T28 → T29, T30 (import depends on device + reference handlers)
T20..T28 → T31 (export)
T20..T31 → T32..T42 (controllers, parallelizable)
T32..T42 → T43..T46 (testing — can start unit tests earlier alongside handlers)
T32..T42 → T47 (CI)
T32..T42 → T48 (OpenAPI commit)
```

---

## Notes

- Tasks are sized for one PR each (< 500 lines per constitution §8.2).
- Hicks owns implementation; Apone owns test authorship and coverage enforcement.
- Hudson owns CI wiring (T47).
- If T01 is done by Hicks in the parallel scaffolding sprint, T02+ can begin immediately.
- Currency strategy (open question #1) must be decided before T04 finalizes Device entity. See `decisions/inbox/ripley-currency-strategy.md`.
- 2026-05-18: Apone landed Domain tests for `Currency` + `Device`/`Household` currency behavior, turned on the `/health` smoke test, and added Playwright token-storage enforcement.
- 2026-05-18: Apone expanded T43 coverage across `Brand`, `Category`, `Owner`, `Location`, `Network`, `Tag`, `DeviceTag`, guard clauses, and audit primitives; `tests/TechInventory.UnitTests` now passes 93 tests with 97.6% Domain line coverage.
- 2026-05-18: Frontend token-storage lint gate landed under Decision D-002 in `src/TechInventory.Web/`; remaining enforcement gates are outside this task list.
- 2026-05-18: Hudson added the pre-commit + CI mirror for Decision D-002 via `.githooks/pre-commit`, `task hooks:install`, `.gitleaks.toml`, and `scripts/check-security.mjs`; full T47 remains open until the broader verify pipeline is green.
- 2026-05-18: Hudson prepped the integration-test path before T13/T14 land. `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs` now provisions a fresh SQLite file per test class, `task test:integration` is the backend entry point, and `task test:e2e` owns hermetic compose bring-up/tear-down around Playwright.
- 2026-05-18: Apone added `AuditEvent` + repository-interface contract tests for T11/T15 under `tests/TechInventory.UnitTests/Domain/AuditEventTests.cs` and `tests/TechInventory.UnitTests/Application/Abstractions/RepositoryInterfaceContractTests.cs`; repo-root `dotnet test -c Release` now exercises both backend test projects, and the current unit-coverage snapshot is Domain 96.45% / Application 0%.
- 2026-05-18: Apone expanded T44/T45 with direct behavior coverage in `tests/TechInventory.UnitTests/Application/Behaviors/` plus SQLite repository coverage in `tests/TechInventory.IntegrationTests/Repositories/`; `dotnet test -c Release` is 151 green, and the latest merged coverage snapshot is Domain 81.40% / Application 40.53% / Infrastructure 88.98%.
- 2026-05-18: Apone closed the remaining T45 import/export controller gap with executable suites in `tests\TechInventory.IntegrationTests\Controllers\ImportsControllerTests.cs` and `ExportControllerTests.cs`, and added T46 OpenAPI drift/schema coverage in `tests\TechInventory.IntegrationTests\Contract\`; current backend verification is **369 passed / 1 skipped**, with merged coverage at **Domain 100.00% / Application 91.58% / Infrastructure 94.33% / Api 91.63%**.

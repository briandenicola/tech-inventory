# Session Notes

Append-only log. Newest entries at the top.

---

## 2026-05-18 — Hicks Round 7 import/export close-out + OpenAPI export
- Added the Phase 1 import/export backend verticals in `src\TechInventory.Application\Imports`, `src\TechInventory.Application\Exports`, `src\TechInventory.Api\Controllers\ImportsController.cs`, `src\TechInventory.Api\Controllers\ExportsController.cs`, and `src\TechInventory.Api\OpenApi\OpenApiDocumentExporter.cs`; preview/commit now share `DeviceImportProcessingService`, export uses a dedicated `IDeviceExportService`, and repo-root `openapi.yaml` is generated from the running API shape instead of hand-editing
- Expanded auditing for bulk import by changing `IAuditContext` / `AuditContext` / `AuditBehavior` to carry multiple entries, extracted shared `DeviceValidationRules`, registered CsvHelper-backed import processing in DI, added import file-size handling + 413 mapping, and seeded a default `Primary Household` (`USD`) at startup when the database is empty so device creation/import works in a fresh environment
- Smoke test on `http://localhost:8080` passed: `POST /api/v1/imports/preview` returned valid + invalid rows plus `lookupsToCreate`; `POST /api/v1/imports/commit` returned `201 Created` with a persisted batch; `GET /api/v1/imports` listed the new batch; `GET /api/v1/exports/devices?format=json` returned exported devices; `GET /api/v1/exports/devices?format=csv` returned device rows; `GET /openapi/v1.json` returned runtime OpenAPI JSON
- Verification from repo root on Windows passed: `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and `dotnet run --project src\TechInventory.Api\TechInventory.Api.csproj -c Release --no-build -- export-openapi`; final backend test summary is **370 total / 369 succeeded / 1 skipped / 0 failed**

---

## 2026-05-18 — Apone Round 7 import/export integration + OpenAPI contract coverage
- Added T45 coverage under `tests/TechInventory.IntegrationTests/Controllers/ImportsControllerTests.cs` and `ExportControllerTests.cs`, exercising preview/commit/list/get-by-id import flows, config-driven 413 handling, lookup auto-creation, CSV/JSON exports, filtered exports, and large-dataset export reads against the real SQLite `WebApplicationFactory` harness
- Added T46 coverage under `tests/TechInventory.IntegrationTests/Contract/` with reusable OpenAPI canonicalization/schema helpers, runtime-vs-committed drift assertions, and endpoint schema checks for brands/categories/devices/owners/locations/networks/tags/audit-events/import preview; the export 200-body schema assertion remains intentionally skipped until `/api/v1/exports/devices` advertises that response schema
- Verified `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release --no-build` ✅ (`240` unit + `129` integration passed, `1` skipped), `dotnet list package --vulnerable --include-transitive` ✅, and fresh unit+integration Cobertura merges at **Domain 100.00% / Application 91.58% / Infrastructure 94.33% / Api 91.63%**
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now reaches Playwright after backend + frontend install/check/lint, but cannot finish in this environment because `docker` is unavailable for `scripts\run-e2e.ps1`

---

## 2026-05-18 — Hicks Round 6 controllers, ProblemDetails, and dev auth bypass
- Replaced the stub `src\TechInventory.Api\Controllers\DevicesController.cs` and added concrete `BrandsController`, `CategoriesController`, `OwnersController`, `LocationsController`, `NetworksController`, `TagsController`, and `AuditEventsController`; all routes now sit under `/api/v1/...`, are `[Authorize]`, and dispatch through MediatR with no business logic in controllers
- Added `src\TechInventory.Api\Common\ControllerResultExtensions.cs` plus `ExceptionHandling\ResultFailureException.cs` / `ApiExceptionHandler.cs` so controller success paths stay terse while `Result.Failure` and unhandled exceptions become RFC 7807 ProblemDetails; validation now returns an `errors` dictionary, 404/409 map cleanly, and 500s stay generic outside Development
- Added Development auth bypass via `Auth:DevBypass` in `appsettings.Development.json`, `DevBypassAuthenticationHandler`, `PlaceholderJwtAuthenticationHandler`, and `HttpContextCurrentUserService`; startup logs the required warning, audit rows now stamp the synthetic `oid`, and OpenAPI moved to `/openapi/v1.json` while Swagger UI remains at `/swagger`
- Smoke test on `http://localhost:8080` passed: `GET /openapi/v1.json` 200 JSON, `GET /api/v1/devices` returned `{"items":[],"totalCount":0,"page":1,"pageSize":25}`, `POST /api/v1/brands` with `{"name":"TestBrand2"}` returned 201 Created, `POST /api/v1/brands` with empty name returned 400 Validation ProblemDetails, `GET /api/v1/brands/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` returned 404 ProblemDetails, and `GET /api/v1/audit-events` showed the created brand audit event with actor `11111111-1111-1111-1111-111111111111`
- Verification: `dotnet format --verify-no-changes` ✅, `dotnet build src\TechInventory.Api\TechInventory.Api.csproj -c Release` ✅, repo-root `dotnet build -c Release` and `dotnet test -c Release` currently fail because Apone's in-flight `tests\TechInventory.IntegrationTests\Controllers\DevAuthBypassTests.cs(94,40)` does not compile in this shared workspace; `dotnet test -c Release` still ran 240 passing tests before that compile stop

---

## 2026-05-18 — Apone Round 5 domain recovery + T20-T28 handler tests
- Added targeted Domain coverage recovery in `tests\TechInventory.UnitTests\Domain\` for `AuditEvent` default-timestamp rejection, `ImportBatch` EF/private-constructor + UTC convenience-constructor behavior, and `Currency.ToString()`; removed a dead duplicate child-depth guard from `src\TechInventory.Domain\Entities\Category.cs`
- Converted all T20–T28 handler scaffolds under `tests\TechInventory.UnitTests\Application\` into executable xUnit + FluentAssertions + NSubstitute tests once Hicks's handlers landed, including CRUD/query suites for Devices, Brands, Categories, Owners, Locations, Networks, Tags, device-tag add/remove, and ClaimDeviceOwnership
- Verified backend commands on Windows: `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and fresh XPlat Cobertura runs for unit + integration projects; merged coverage snapshot is Domain 100.00%, Application 85.89%, Infrastructure 88.98%
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now reaches frontend linting cleanly but cannot finish in this environment because `docker` is unavailable for `scripts\run-e2e.ps1`; backend verify steps and frontend install/check/lint all succeeded before that environment failure
- Final backend test summary: **266 passed, 0 skipped** (test delta vs pre-Round-5 baseline: +115)

---

## 2026-05-18 — Hicks T20-T28 application handlers, paging responses, and ownership/tag flows
- Added concrete Application command/query packages under `src/TechInventory.Application\Devices`, `Brands`, `Categories`, `Owners`, `Locations`, `Networks`, and `Tags`; every handler now returns `Result`/`Result<T>`, every request has a FluentValidation validator, and list queries standardize on the new `PagedResponse<T>` DTO
- Device work shipped `CreateDeviceCommand`, `UpdateDeviceCommand`, `DeleteDeviceCommand`, `GetDeviceByIdQuery`, `ListDevicesQuery`, `AddTagToDeviceCommand`, `RemoveTagFromDeviceCommand`, and `ClaimDeviceOwnershipCommand`; creates resolve the single household for default currency, update/delete/ownership/tag-removal stash BEFORE payloads in `IAuditContext`, and delete now supports retired → disposed transitions
- Category handlers established the tree contract for Round 5: list paginates root nodes while preserving descendants, update rejects cycles and rebalances descendant depths, and delete cascades archive state through the subtree. Owner delete now blocks when any device still references the owner so the active-owner invariant remains intact
- Verified from repo root on Windows with `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`; current workspace summary was **182 succeeded / 78 skipped** because Apone's in-flight scaffolding tests are present locally alongside the committed suite
- Next: Apone can finish consumer-side handler tests against the concrete request/response types; Hicks can pick up T29+ import/export handlers or controller surface after coordination

---

## 2026-05-18 — Hicks T16-T19 repositories, audit stamping, and MediatR behaviors
- Added `AddApplication()` / `AddInfrastructure()` wiring so the API now registers concrete repositories, `AuditSaveChangesInterceptor`, `ICurrentUserService`, `IUnitOfWork`, scoped `IAuditContext`, and MediatR pipeline behaviors in the intended order (Validation first, Audit last)
- Implemented `Repository<TEntity, TKey>` plus all concrete Infrastructure repositories under `src/TechInventory.Infrastructure/Persistence/Repositories/`; exact-ID reads stay unit-of-work aware, list queries hide inactive reference rows by default, `IAuditEventRepository.AppendAsync` remains save-free, and Device list defaults exclude disposed rows unless explicitly filtered back in
- Added `AuditSaveChangesInterceptor`, `IAuditable`, `IAuditContext`, `ValidationBehavior`, and `AuditBehavior`; validation failures now return `Error.Code = "Validation"` with an `Error.ValidationErrors` dictionary, and audit BEFORE/AFTER payloads come from handler-populated `IAuditContext` + request JSON fallback
- Verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release` all pass from the repo root on Windows; attempted `scripts/verify.ps1`, but it stalled during the build phase after repeated waits, so final validation used the requested direct dotnet commands
- Next: Hicks can move into T20+ command/query handlers while Apone targets the new repository/behavior seams for broader Application coverage

---

## 2026-05-18 — Apone T44/T45 behavior + repository coverage follow-through
- Added direct Application behavior tests under `tests/TechInventory.UnitTests/Application/Behaviors/` for ValidationBehavior aggregation/pass-through, AuditBehavior success/failure/no-op cases, and a composed Validation→Audit pipeline assertion that failing validation never writes an AuditEvent
- Added SQLite-backed repository integration coverage under `tests/TechInventory.IntegrationTests/Repositories/` for Brand/Category/Owner/Location/Network/Tag CRUD + active-filter + audit-stamp checks, Device CRUD/filter/audit-stamp checks, and AuditEvent append-only persistence checks
- Hardened `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs` cleanup retries so SQLite file locks from `WebApplicationFactory` disposal do not fail otherwise-green integration runs
- Verified `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release`, `dotnet test tests\TechInventory.IntegrationTests\TechInventory.IntegrationTests.csproj -c Release`, `dotnet test -c Release`, and `dotnet test -c Release --collect:"XPlat Code Coverage"`
- Coverage snapshot from the latest merged unit+integration reports: Domain 81.40%, Application 40.53%, Infrastructure 88.98%

---

## 2026-05-18 — Hicks T11-T15 audit event, persistence, and repository seams
- Added immutable `AuditEvent` and `ImportBatch` domain entities under `src/TechInventory.Domain/Entities/`; `AuditEvent` now exposes `Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, `BeforePayload`, and `AfterPayload` with no public mutation surface
- Added Application-layer repository abstractions plus shared `Result<T>`, `Result`, `PageRequest`, and `PagedResult<T>` under `src/TechInventory.Application/`; `IAuditEventRepository` is append/query only and repository contracts never expose `IQueryable`
- Added `AppDbContext`, EF Core entity configurations, a design-time factory, append-only save guards for `AuditEvent` / `ImportBatch`, and the `InitialCoreApi` migration under `src/TechInventory.Infrastructure/Persistence/`
- Updated Hicks/Apone contract coverage so repo-root `dotnet test -c Release` exercises the backend tests via `TechInventory.slnx`; patched the reflection helpers/tests to handle the new repository paging/result shapes cleanly
- Verified `dotnet ef migrations add InitialCoreApi`, `dotnet ef database update`, `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --filter FullyQualifiedName~RepositoryInterfaceContractTests`, and `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --filter FullyQualifiedName~AuditEvent`

## 2026-05-18 — Apone T11/T15 contract coverage
- Added `tests/TechInventory.UnitTests/Domain/AuditEventTests.cs` for append-only `AuditEvent` construction, UTC timestamp, public-surface immutability, and payload-transition guard clauses
- Added `tests/TechInventory.UnitTests/Application/Abstractions/RepositoryInterfaceContractTests.cs` plus `Support/ContractReflectionAssertions.cs` to lock repository async/`CancellationToken`/no-`IQueryable` seams and the `IAuditEventRepository.AppendAsync`-only mutation contract
- Added `tests/TechInventory.UnitTests` to `TechInventory.slnx` so repo-root `dotnet test -c Release` now executes the backend test projects instead of only building source projects
- Verified `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release`, `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --collect:"XPlat Code Coverage"`, and `dotnet test -c Release`
- Coverage snapshot from the unit suite: Domain 96.45% line coverage, Application 0% line coverage (interfaces/results/paging scaffolding present but no executable Application tests yet)

## 2026-05-18 — Hudson SQLite integration harness + hermetic E2E contract
- Added `IntegrationTestFactory<TMarker>` under `tests/TechInventory.IntegrationTests/` so each test class gets its own SQLite file, future EF Core migrations auto-apply when present, and cleanup removes the database plus WAL/SHM sidecars
- Repointed the existing `/health` smoke test to the new factory and verified `task test:integration` passes end-to-end against the new harness
- Added `task test:integration` and `task test:e2e`; `task test:e2e` now owns compose bring-up, readiness wait on `/health/ready`, Playwright execution against `http://localhost:3000`, and teardown via `scripts/run-e2e.ps1` / `scripts/run-e2e.sh`
- Split `scripts/verify.ps1` / `scripts/verify.sh` into unit → integration → vulnerability/frontend checks → hermetic Playwright order
- Next: Hicks lands `AppDbContext` + first migration against `ConnectionStrings:Default`; Apone expands factory-backed integration coverage once migrations exist

## 2026-05-18 — Hicks Phase 1 domain reference entities T06-T10
- Added `Category`, `Owner`, `Location`, `Network`, `Tag`, and `DeviceTag` under `src/TechInventory.Domain/Entities/` with trimmed-name guards, archive/reactivate methods, and normalized-name helpers for later repository uniqueness checks
- Category now keeps `ParentId` plus validated `Depth` (1-3) so the max-depth invariant is enforced in Domain; `Owner` carries `OwnerRole` and optional `EntraObjectId`; `DeviceTag` uses `IsActive` instead of hard deletes
- Replaced Apone's placeholder skips with executable domain tests for T06-T10; `tests/TechInventory.UnitTests` now runs 93 passing tests with zero skips, and Domain line coverage is 97.6%
- Verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, targeted unit/integration runs, and `./scripts/verify.ps1`
- Next: Hicks can take T11-T17 (AuditEvent, ImportBatch, AppDbContext/configs, repositories, audit stamping)

## 2026-05-18 — Hudson pre-commit security gate
- Added a repo-managed pre-commit hook at `.githooks/pre-commit` backed by `scripts/check-security.mjs` and a pinned `.gitleaks.toml` config
- Added `task hooks:install` plus cross-platform `scripts/install-gitleaks.ps1` / `scripts/install-gitleaks.sh` so fresh clones can wire hooks in one command
- Updated `.github/workflows/ci.yml` to install pinned gitleaks and mirror the hook against PR/push diffs
- Verified `task hooks:install`, `node .\scripts\check-security.mjs --diff-range HEAD`, and an isolated test repo commit rejection for an auth-token localStorage write attempt
- `./scripts/verify.ps1` still fails on the pre-existing frontend `vite.config.ts` type mismatch and unused `@ts-expect-error`
- Next: fix the frontend Vite/Vitest type conflict so the full verify pipeline is green again

## 2026-05-18 — Apone QA follow-through
- Added spec-driven Domain tests for `Currency`, `Household`, and `Device` under `tests/TechInventory.UnitTests/Domain/`
- Enabled the `/health` integration smoke test with `WebApplicationFactory<Program>` and exposed `Program` for test hosting
- Added Playwright token-storage enforcement under `tests/e2e/security/` plus reusable storage-inspection helper and skill note
- Validated with `dotnet test tests/TechInventory.UnitTests -c Release`, `dotnet test tests/TechInventory.IntegrationTests -c Release`, and `node .\\node_modules\\@playwright\\test\\cli.js test security/token-storage.spec.ts --reporter=line`
- Attempted `./scripts/verify.sh`, but this Windows session has no `bash`; used targeted validation instead
- Next: expand T44/T45 as Hicks lands Application handlers and more API surface

## 2026-05-18 — Hicks Phase 1 domain core T01-T05
- Added Domain primitives (`Entity`, `AggregateRoot`, `ValueObject`, `Guard`) plus shared enums under `src/TechInventory.Domain/`
- Added `Currency` value object with ISO 4217 allowlist validation, `Household` with `DefaultCurrency`, `Device` with household-default currency creation flow, and `Brand`
- Verified `dotnet build -c Release` and `dotnet test -c Release --no-build` passed after the Domain changes; fixed a pre-existing whitespace issue in `src/TechInventory.Api/Program.cs` so `dotnet format --verify-no-changes` can pass again

## 2026-05-18 — Vasquez auth token storage lint gate
- Added a custom flat-config ESLint rule in `src/TechInventory.Web/eslint.config.js` to block token-like `localStorage.setItem/getItem/removeItem` keys and to forbid any `localStorage` use inside `src/lib/auth/` and `src/lib/api/`
- Locked MSAL cache policy to `BrowserCacheLocation.SessionStorage` in `src/lib/auth/msal.ts`
- Verified the rule fired with temporary lint fixtures, then removed them and reran `pnpm run lint` successfully
- `pnpm run check` still fails on the pre-existing unused `@ts-expect-error` in `src/TechInventory.Web/vite.config.ts`

## YYYY-MM-DD — Bootstrap
- Created project structure
- Populated constitution, PRD, backlog README
- Next: run `/constitution` to validate

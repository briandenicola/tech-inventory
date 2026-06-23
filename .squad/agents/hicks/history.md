# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker.
- **Stack:** ASP.NET Core 10 Web API. Clean Architecture (Domain → Application → Infrastructure → Api). MediatR for commands/queries. FluentValidation. EF Core code-first + SQLite. Serilog structured logging. OpenTelemetry traces. ProblemDetails (RFC 7807) errors. OpenAPI 3.1 at `/swagger`. URL-versioned routes `/api/v1/...`.
- **Created:** 2026-05-18

## Core Context

Core entities (PRD §8): Device, Brand, Category (hierarchical), Owner, Location, Tag, DeviceTag, Attachment (v2), AuditEvent (append-only), ImportBatch.

Invariants: Device.ownerId references an active Owner. Retired Device is read-only except notes + disposalMethod. AuditEvents never updated or deleted.

Build / test commands (from copilot-instructions.md):
- `dotnet restore && dotnet build -c Release`
- `dotnet test -c Release`
- `dotnet test --filter "FullyQualifiedName~MyTest"`
- `dotnet format --verify-no-changes` (lint check)
- `dotnet list package --vulnerable`
- `./scripts/verify.sh` (full pipeline: format → build → test → vuln scan)
- `task up | task test | task down` for full local stack

85% line coverage minimum on Domain + Application (constitution / PRD §7).

Phase 1 lands in `specs/001-core-api/`. Pattern references: **R1** for MediatR handler structure (`src/Application/Features/`) and Problem Details middleware; **R2** for inventory domain shape and CSV import pipeline.

## Recent Updates

### 2026-05-20 — F044 display settings backend ✅ COMPLETE

**Delivered household-level display settings API for device list/detail ordering:**

- Added `HouseholdSetting` plus `IHouseholdSettingRepository`, `HouseholdSettingRepository`, EF configuration, and migration `20260520202952_AddHouseholdSettings` so per-household settings persist as unique `(HouseholdId, Key)` rows with JSON array payloads.
- Added the `src\TechInventory.Application\Settings\` vertical with shared defaults/allowlists in `DisplaySettingsCatalog`, MediatR query + command handlers, and FluentValidation rules for unknown identifiers, duplicate columns, and the required `name` list column.
- Added `SettingsController` at `/api/v1/settings/display`; GET seeds default rows when missing, PUT is Admin-only, successful updates append a `HouseholdSetting` audit entry, and invalid persisted settings return `409 Conflict` instead of silently leaking corrupt data.
- Added backend coverage in `tests\TechInventory.UnitTests\Application\DisplaySettingsHandlerTests.cs`, `DisplaySettingsValidationTests.cs`, `tests\TechInventory.IntegrationTests\Controllers\SettingsControllerTests.cs`, and `SettingsAuthorizationTests.cs`; regenerated repo-root `openapi.yaml` from the runtime document.
- Validation: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅ (**455 total / 450 passed / 5 skipped / 0 failed**). `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` still fails only because `docker` is unavailable for the Playwright compose step in this environment.

### 2026-05-20 (Spec-003 Batch 2) — T09/T10 merge + insurance backend ✅ COMPLETE

**Delivered backend contracts for duplicate cleanup + insurance export:**

- Added admin-only `POST /api/v1/brands/merge`, `/api/v1/categories/merge`, and `/api/v1/locations/merge` in the resource controllers plus MediatR commands/validators under `src\TechInventory.Application\Brands`, `Categories`, `Locations`, and shared merge helpers under `src\TechInventory.Application\Merges`. Merges validate distinct IDs, require active source/target records, reassign device FK references through `IDeviceRepository`, deactivate the source row, and write paired `AuditEvent` entries. Category merges also reparent descendants and block descendant/depth conflicts.
- Added `GetInsuranceReportQuery` in `src\TechInventory.Application\Reports\Queries\GetInsuranceReportQuery.cs`, `IReportingRepository.GetInsuranceReportItemsAsync(...)`, and `ReportsController.GetInsurance(...)`. The report returns a CSV attachment with a generated-at comment line, active-device rows only, optional `locationId` filtering, and a `TOTAL` footer row.
- Added backend coverage in `tests\TechInventory.UnitTests\Application\ReferenceMergeCommandHandlerTests.cs`, `tests\TechInventory.UnitTests\Application\ReportingQueryHandlerTests.cs`, `tests\TechInventory.IntegrationTests\Controllers\BrandsControllerTests.cs`, `CategoriesControllerTests.cs`, `LocationsControllerTests.cs`, `ReportsControllerTests.cs`, and `ReferenceMergeAuthorizationTests.cs`; regenerated repo-root `openapi.yaml` from the runtime document.
- Tightened the `IDeviceRepository` merge helper signatures to return `Result<int>` so the repository contract suite stays consistent with the Application-layer result pattern.
- Validation: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅ (**431 total / 426 passed / 5 skipped / 0 failed**). `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now reaches Playwright and fails only because `docker` is unavailable in this environment.

### 2026-05-20 (Spec-003 Batch 1) — T11 Reporting API Endpoints ✅ COMPLETE

**Delivered 3 endpoints with finalized reporting data model:**

1. **GET /api/v1/reports/summary** — Device inventory by status (Active, Retired, Disposed); total value
2. **GET /api/v1/reports/warranties** — Device warranty expiry tracking, sorted by expiration
3. **GET /api/v1/reports/spending** — Historical spend analysis, includes all lifecycle states

**Key Design Decisions:**
- **Persist warranty expiry on Device** — Added nullable `Device.WarrantyExpiry` via migration `20260520165251_AddDeviceWarrantyExpiry`. Rationale: reporting spec requires durable warranty data; existing aggregate lacked this field.
- **Normalize status to three buckets** — Returns Active/Retired/Disposed only. InRepair + Lent roll into Active bucket per spec/UI labels.
- **Spending as historical** — Includes any device with both PurchaseDate + PurchasePrice regardless of current state. Preserves spend history after retirement/disposal.

**Implementation:**
- EF Core projections (no N+1 queries)
- FluentValidation for input contracts
- OpenAPI auto-documented
- Test coverage: 398 passing
- Full verification: `dotnet format` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅

**Impact:**
- ✅ T12 (frontend reporting UI) has finalized API contracts and can proceed
- ✅ T13 (fun/whimsical reports) has foundation ready for narrative/timeline queries
- **Decision:** D-118 (reporting API: warranty, summary, spending)

**Blockers Cleared:** None — independent delivery. T12/T13 unblocked for next team session.

---

**2026-05-18 (Phase 1 Round 7):** T29/T30/T31/T39/T42/T48 import/export verticals landed end-to-end. Added `PreviewImportCommand` + `CommitImportCommand` with `DeviceImportProcessingService` (CsvHelper-backed parsing, re-parse-on-commit strategy), `ImportContracts`, and `ImportsController` with multipart file handling, 10MB size cap, and batch persistence; commit creates missing Brand/Category/Owner/Location records, persists immutable `ImportBatch`, and emits one audit event per device. Added `ExportDevicesQuery` + `IDeviceExportService` projection + `ExportsController` with async buffered CSV/JSON response writing and export-row logging. `Program.cs` seeds default `Primary Household` (USD) on empty DB and supports `export-openapi` CLI command; `Taskfile.yml` exposes `openapi:export` task. Repo-root `openapi.yaml` generated from runtime. Smoke tests passed: imports preview/commit/list, exports JSON/CSV, `/openapi/v1.json`. Commits: `00fe492`, `9dbfd51`. Verification: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅ (**369 passed / 1 skipped**)

**2026-05-18 (Phase 1 Round 6):** T32–T38, T40, and T41 landed in `src/TechInventory.Api` (commit `48c1920` + `74a1e21`). Attribute-routed controllers with explicit lowercase `/api/v1/...` paths for Devices, Brands, Categories (with tree route), Owners, Locations, Networks, Tags, and AuditEvents. Shared `ControllerResultExtensions.cs` maps `Result<T>.Success` → `Ok()` for reads, `CreatedAtAction()` for creates, `NoContent()` for patches; `Result.Failure` throws `ResultFailureException` for exception pipeline. Global `IExceptionHandler` + `ProblemDetailsFactory` (D-026, D-027) converts failures to RFC 7807 ProblemDetails (400/404/409/500 with validation `errors` dict). Dev auth bypass (D-022): `Auth:DevBypass=true` in `appsettings.Development.json` produces synthetic `dev-admin` principal with Admin role; startup throws if enabled outside Dev and logs warning. OpenAPI wired at `/openapi/v1.json`. Smoke tests: `GET /openapi/v1.json` → 200 JSON; `GET /api/v1/devices` → `{"items":[],"totalCount":0,"page":1,"pageSize":25}`; `POST /api/v1/brands -d '{"name":"TestBrand2"}'` → 201 with Location header; `POST /api/v1/brands -d ''` → 400 Validation ProblemDetails; `GET /api/v1/brands/{invalid-uuid}` → 404 ProblemDetails. 28 files modified. Verification: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, repo-root build blocked on Apone's integration test compile error (awaits fix).

**2026-05-18 (Phase 1 Round 5):** T20–T28 Application handlers complete (commit `1180cf6`). Device/Brand/Category/Owner/Location/Network/Tag command+query handlers finalized with validators, soft-delete semantics (retired → disposed transition for devices), PagedResponse paging for lists, category subtree recursion, owner active-owner delete-blocking, and tag-device join audit metadata. All 98 files verified: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` (182 succeeded, 78 skipped) ✅. **Coverage note:** Domain regression from 81.40% flagged in prior round; Apone recovered it in R5 to 100.00% via explicit AuditEvent/ImportBatch coverage work.

**2026-05-18 (Phase 1 Round 1):** Domain layer T01–T05 complete and tested. `EntityId` ULID primitives, `Currency` ISO 4217 value object, `Household`/`Device`/`Brand` aggregates with currency inheritance and retired-device edit guards all implemented. 13 Domain unit tests green (85% coverage). Currency decision finalized (D-008): per-device with household default. Pre-commit hook deployed for token-storage enforcement (Hudson). Vasquez's ESLint gate live. Apone's Domain contract tests executable. `dotnet build -c Release` ✅, `dotnet test -c Release` ✅, `dotnet format --verify-no-changes` ✅. Phase 2: Repository layer + Application command/query handlers.

**2026-05-18 (Phase 1 Round 2):** Domain layer T06–T10 complete and tested. `Category` (hierarchical depth validation), `Owner` (Entra identity hooks, role mapping), `Location`/`Network`/`Tag` (soft-delete semantics), `DeviceTag` (composite key, reactivation logic) all landed. 80 xUnit cases covering reference entities + Brand/Device supplemental. Domain line coverage: 97.6% (well above 85% floor). Verify pipeline green (`dotnet build`, `dotnet test`, `dotnet format` all ✅). Vasquez's vite.config fix unblocked frontend. Apone locked contract test pattern (D-016); blockers flagged: T11 (AuditEvent), T15 (Repository interfaces). Next phase: Application handlers + Infrastructure EF Core wiring.

**2026-05-18 (Phase 1 Round 4):** Infrastructure repositories + MediatR behaviors complete (T16–T19, commit `81f478d`). 10 concrete repositories implemented in `src/TechInventory.Infrastructure/Persistence/Repositories/` (`BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, `ImportBatchRepository`) with shared `Repository<TEntity, TKey>` base. `AuditSaveChangesInterceptor` stamps UTC audit columns. `ValidationBehavior` + `AuditBehavior` pipeline (D-020, D-021): validation first (returns `Result.Failure` cleanly), audit last (fires only on success). `IAuditContext` scoped strategy avoids second DB read. Exact-ID reads stay unit-of-work aware; list paths filter inactive/soft-deleted rows by default. Verify pipeline all green: `dotnet format`, `dotnet build -c Release`, `dotnet test -c Release` (151 tests ✅). Coverage snapshot: Domain 81.40%, Application 40.53%, Infrastructure 88.98%. Next: Coverage regression audit in Round 5; continue handler + controller wiring (T20–T42).


## CORS fix for local dev (D-133)

**Date:** 2026-05-18  
**Commit:** 908845a

### Diagnosis

CORS was completely absent from `Program.cs`. No `AddCors()` service registration or `UseCors()` middleware call. Browser preflight requests from Vite dev server (`http://localhost:5173`) to API (`http://localhost:8080`) failed with missing `Access-Control-Allow-Origin` header.

### Fix Pattern

1. **Config-driven allowed origins**: Read from `Cors:AllowedOrigins` array in appsettings (empty array = no CORS policy applied)
2. **Service registration** after `AddAuthorizationBuilder()`:
   ```csharp
   var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
   builder.Services.AddCors(options => {
       options.AddPolicy("ApiCorsPolicy", policy => {
           if (allowedOrigins.Length > 0) {
               policy.WithOrigins(allowedOrigins)
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
           }
       });
   });
   ```
3. **Middleware placement**: `app.UseCors("ApiCorsPolicy")` **before** `UseAuthentication()` (preflight OPTIONS requests bypass auth)
4. **Dev config**: Added `"Cors": { "AllowedOrigins": ["http://localhost:5173"] }` to `appsettings.Development.json`
5. **Production safety**: Production appsettings unchanged; no origins configured by default; operators whitelist explicitly

### Key Learnings

- CORS middleware order matters: must come before `UseAuthentication()` so preflight OPTIONS requests (which have no auth) can succeed
- `.AllowCredentials()` required for Entra bearer tokens + any cookies; incompatible with `AllowAnyOrigin()` (security by design)
- Config-driven approach lets production operators add specific origins without code changes
- Empty origins array = no-op policy (doesn't break startup if section missing)

### Quality Gates

- Build ✅ (16.1s)
- Tests ✅ (377 passed / 6 skipped / 0 failed in 28.4s)
- No behavior regressions

### Handoff Note

Brian must restart API (`Ctrl+C` then `task dev:up`) to pick up new config. After restart, Web sign-in flow should complete successfully.

---

## Learnings

### 2026-05-20: Reference bulk ops + network merge (F039)

- Shared bulk-operation contracts now live in `src\TechInventory.Application\BulkOperations\`; reference-entity bulk-delete handlers/controllers reuse `BulkOperationResponse` + `BulkAuditEnvelope` instead of inventing per-entity payload types.
- F039 keeps the same resource-oriented pattern as the earlier merge work: per-entity commands stay under `src\TechInventory.Application\{Brands,Categories,Locations,Networks}\Commands\`, controller endpoints live at `POST /api/v1/{entity}/bulk/delete`, and the new network merge seam is `src\TechInventory.Application\Networks\Commands\MergeNetworkCommand.cs` plus `POST /api/v1/networks/merge`.
- Category bulk delete must process selected categories deepest-first so a batch can safely include both a parent and child without double-updating the child; runtime OpenAPI should be refreshed from `/openapi/v1.json` after adding endpoints.

### 2026-05-20: Household display settings (F044)

- Per-household admin configuration fits well as a generic `HouseholdSetting` aggregate + repository keyed by `(HouseholdId, Key)`; JSON array values preserve user-defined order without hard-coding one schema/table per settings feature.
- `DisplaySettingsCatalog` is the right single source for defaults, allowlists, normalization, duplicate detection, and JSON serialization/deserialization so validators, handlers, tests, and OpenAPI-backed controllers all share one contract.
- GET can safely seed missing default rows for the single-household install, but handlers should still validate persisted JSON and return `409 Conflict` when stored keys/values are duplicated or invalid so bad state never silently leaks into the UI.

### 2026-05-20: Era/decade reporting (F035-T01/T02)

- Era report work fits the existing reporting vertical cleanly: query + validator in `src\TechInventory.Application\Reports\Queries\`, controller surface in `src\TechInventory.Api\Controllers\ReportsController.cs`, shared read models in `src\TechInventory.Application\Reports\ReportModels.cs`, and EF projection logic in `src\TechInventory.Infrastructure\Persistence\Repositories\ReportingRepository.cs`.
- `IReportingRepository.GetEraReportAsync(...)` is simplest as a single `Select(...).ToListAsync()` over active devices with non-null purchase dates, followed by in-memory decade grouping. That keeps the repository to one database round-trip while still allowing stable `sampleDevices` arrays without provider-specific SQL tricks.
- Deterministic `sampleDevices` ordering matters for the whimsical report UI and tests: order by most recent purchase year descending, then device name ascending, and take the first three names in each decade.

### 2026-05-20: Historical timeline reporting (F037)

- The timeline report follows the same reporting seam as the other report endpoints: shared models in `ReportModels.cs`, MediatR query/validator in `Reports\Queries\`, controller wiring in `ReportsController.cs`, and a single EF Core projection inside `ReportingRepository.cs`.
- Timeline data must include historical lifecycle states, so the repository intentionally filters only on `PurchaseDate` and optional category/date range; `RetiredDate` becomes the public `disposalDate` for both retired and disposed records because the domain has one lifecycle-end date field today.
- `estimatedValue` currently maps from `Device.PurchasePrice` (coalesced to `0m` when absent) so the frozen F037 contract stays numeric without inventing a new persistence field before the broader valuation story exists.

### 2026-05-20: Reference merge + insurance report patterns (P003-T09/T10)

- Shared merge validation lives in `src\TechInventory.Application\Merges\MergeReferenceEntityCommandValidator.cs` / `IMergeReferenceEntityCommand`, but each entity keeps its own handler + controller so category-specific tree rules stay explicit and the OpenAPI surface remains resource-oriented.
- `IDeviceRepository` reference-reassignment helpers should return `Result<int>` instead of raw `Task<int>` so repository contracts remain mock-friendly and consistent with the Application-layer `Result<T>` convention.
- Insurance export splits cleanly across layers: Infrastructure projects `InsuranceReportItem` rows, Application formats CSV bytes + the dated filename with `TimeProvider`, and API just returns `File(...)`; that keeps CSV formatting testable without controller business logic.

### 2026-05-20: Reporting API foundations (P003-T11)

- Reporting read-models live under `src\TechInventory.Application\Reports\` with MediatR query handlers and a dedicated `IReportingRepository` seam; Infrastructure owns the EF Core projections in `src\TechInventory.Infrastructure\Persistence\Repositories\ReportingRepository.cs`.
- Warranty reporting required a persisted `Device.WarrantyExpiry` field, so `Device`, the EF mapping/migration, and the existing device create/update/get DTOs + validators all had to move together; leaving it as a report-only projection would have made the feature impossible to populate from the API.
- Inventory summary treats any lifecycle state other than `Retired`/`Disposed` as the `Active` bucket, while the public status breakdown still returns only `Active`, `Retired`, and `Disposed` to match the reporting spec. Spending remains historical: it groups devices by purchase date + price regardless of current lifecycle state.

### 2026-05-18: Solution Scaffolding (Phase 0)

**Project Structure:**
- Solution: `TechInventory.sln` at repo root
- Projects under `src/`:
  - `TechInventory.Domain` — entities, value objects, domain interfaces. Zero framework dependencies.
  - `TechInventory.Application` — MediatR handlers (v14.1.0), FluentValidation (v12.1.1), use cases, Result<T>
  - `TechInventory.Infrastructure` — EF Core (SQLite), repositories
  - `TechInventory.Api` — controllers, Program.cs, healthchecks, Swagger, middleware
- Project references: Application → Domain; Infrastructure → Application + Domain; Api → Application + Infrastructure

**Framework & Packages:**
- Target: `net10.0` (SDK 10.0.204) — no fallback needed
- All projects: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors Condition="'$(Configuration)' == 'Release'">true</TreatWarningsAsErrors>`
- Serilog.AspNetCore 10.0.0 (Console + File sinks)
- OpenTelemetry 1.15.x (Extensions.Hosting, Instrumentation.AspNetCore, Exporter.OpenTelemetryProtocol)
- Swashbuckle.AspNetCore 7.2.0 for OpenAPI/Swagger (removed Microsoft.AspNetCore.OpenApi due to source generator conflicts)
- FluentValidation.AspNetCore 11.3.1
- Microsoft.EntityFrameworkCore 10.x + SQLite + Design (Infrastructure only)

**Program.cs Wiring:**
- Serilog as host logger via `builder.Host.UseSerilog()`
- MediatR scans Application assembly
- FluentValidation scans Application assembly
- AddProblemDetails() for RFC 7807 errors
- AddHealthChecks() mapped to `/health` and `/health/ready`
- Swagger enabled in Development + optional Production via config `Features:SwaggerInProduction`
- OpenTelemetry OTLP exporter optional via `OpenTelemetry:OtlpEndpoint` config
- Healthchecks return 200 OK even before DB is wired (liveness ready)
- Port 8080 for dev (launchSettings.json)

**DevicesController Stub:**
- Route: `GET /api/v1/devices` returns empty array with 200 OK
- TODO comment: `// TODO(spec-001): wire to MediatR query`

**Build Verified:**
- `dotnet build -c Release` succeeds with zero warnings
- TreatWarningsAsErrors enforced in Release mode

**Gotchas:**
- Microsoft.AspNetCore.Diagnostics is redundant in Web SDK (pruned package warning → removed)
- Microsoft.AspNetCore.OpenApi (v10.0.8) source generator has compatibility issues with Swashbuckle; removed in favor of Swashbuckle-only approach
- dotnet CLI `add package` occasionally hits file lock contention; resolved by manual csproj edits or retry

### 2026-05-18: Domain currency and aggregate foundations (Phase 1 T01-T05)

- `AggregateRoot : Entity` is the shared domain base shape. `Entity` owns `Id`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, plus `Touch()` / `SetAuditMetadata()` so Infrastructure can stamp audit columns later without leaking EF Core into Domain.
- `Currency` lives in `src/TechInventory.Domain/ValueObjects/Currency.cs` as a record value object. It trims input, uppercases it, requires exactly three ASCII letters, and rejects any code outside the embedded ISO 4217 allowlist.
- `Household` owns `DefaultCurrency`. `Device.Create(...)` takes a `Household` and falls back to `household.DefaultCurrency` when a per-device currency is not supplied; after creation, `Device.Currency` is independent and can diverge through `UpdateDetails(...)`.
- `Device.UpdateDetails(...)` throws when `Status == Retired`; only `UpdateNotes(...)` and `UpdateDisposalMethod(...)` remain legal for retired/disposed records. `UpdateDisposalMethod(...)` itself throws unless status is `Retired` or `Disposed`.
- Apone test targets: currency normalization (`usd` → `USD`), invalid code/length rejection, device creation inheriting household currency, explicit device currency override, retired-device general edits throwing, and retired/disposed-only disposal-method rules.

### 2026-05-18: Domain reference entities and tagging foundations (Phase 1 T06-T10)

- `Category` stores both `ParentId` and `Depth`; roots must be depth 1, children must be depth 2 or 3, and anything above 3 is rejected in Domain without waiting for repository checks.
- `Owner` uses `OwnerRole`, optional `Guid? EntraObjectId`, and `IsActive` archive semantics so Application/Auth work can attach Entra identities without reshaping the aggregate.
- `Location`, `Network`, and `Tag` follow the `Brand` pattern: required trimmed names, uppercase normalization helpers for later uniqueness enforcement, and `Deactivate()` / `Reactivate()` soft-delete flow.
- `DeviceTag` keeps composite identity as `DeviceId` + `TagId` and uses `IsActive` instead of hard delete; future repository logic should reactivate an existing pair instead of inserting duplicates.
- Apone test targets next: category root/child depth edge cases, owner Entra identity linking, network normalized-name uniqueness checks, and device-tag reactivation semantics.

### 2026-05-18: AuditEvent and repository contract foundations (Phase 1 T11-T15)

- `AuditEvent` lives in `src/TechInventory.Domain/Entities/AuditEvent.cs` with immutable public properties `Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, `BeforePayload`, and `AfterPayload`; there are no public mutators, and EF gets a private constructor/private setters only for materialization.
- `ImportBatch` is likewise immutable from the public API: `FileName`, row counts, `Status`, `CreatedAt`, and optional `ErrorLog` are fixed at construction, with derived `ProcessedCount` / `HasErrors` helpers.
- Repository abstractions now live in `src/TechInventory.Application/Abstractions/Repositories/`, not Domain. Every method is async, every signature carries `CancellationToken`, expected failures return `Result<T>` or `Result`, paged reads use `PagedResult<T>`, and `IQueryable` never crosses the boundary.
- `IAuditEventRepository` exposes `AppendAsync`, `GetByIdAsync`, and paged `ListAsync` only; there is no update/delete/remove surface. `IImportBatchRepository` is add/read only, while mutable aggregates use the shared `IAggregateRepository<TAggregate>` base plus materialized list/query methods.
- Infrastructure concrete repositories did not land in this round. What did land in Infrastructure is `AppDbContext`, entity configurations, append-only save guards for `AuditEvent`/`ImportBatch`, and the `InitialCoreApi` migration.

**Cross-agent notes (Phase 1 Round 3):**
- Apone locked the append-only contract with 8 AuditEvent reflection tests; immutability now executable spec.
- Hudson pre-deployed integration test factory: concrete repositories will inherit SQLite per-test-class isolation when T16 lands.
- Append-only enforcement strategy finalized (D-017): AppDbContext save guards + `IAuditEventRepository` interface contract form the double seam.

### 2026-05-18: Concrete repositories, audit stamping, and pipeline behaviors (Phase 1 T16-T19)

- Infrastructure repositories now live in `src/TechInventory.Infrastructure/Persistence/Repositories/` around a shared `Repository<TEntity, TKey>` base. Exact-ID lookups stay unit-of-work aware (`DbSet.FindAsync` / tracked entities first), while list/paged queries merge pending tracked entities with database rows so unsaved work remains visible inside the current EF Core unit of work.
- Concrete repository convention is one class per Application interface: `BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, and `ImportBatchRepository`. Reference-entity `ListAsync(includeInactive: false)` paths filter soft-deleted `IsActive == false` rows by default; exact-ID lookups intentionally still return inactive rows so handlers can reactivate/archive safely.
- `AuditSaveChangesInterceptor` is wired through `AppDbContext.OnConfiguring(...)` and uses `ICurrentUserService` (currently `SystemCurrentUserService`) to stamp UTC `CreatedAt` / `ModifiedAt` and `CreatedBy` / `ModifiedBy` on every tracked `Entity` / `AggregateRoot` add or update.
- MediatR pipeline ordering is now `ValidationBehavior` first, `AuditBehavior` last. Validation returns `Result.Failure(new Error("Validation", ...))` with `Error.ValidationErrors` keyed by property name instead of throwing; audit only runs when the inner handler returns success.
- `IAuditable` lives in `src/TechInventory.Application/Auditing/IAuditable.cs` as a pure marker interface, while handlers communicate audit metadata through scoped `IAuditContext`. BEFORE/AFTER payload strategy: handlers set entity/action/before-state into `IAuditContext`; `AuditBehavior` serializes `BeforePayload` from that context and falls back to the request object for `AfterPayload` (so creates serialize JSON `null` for BEFORE and the command payload for AFTER).

## Mid-Phase 2 — CSV Schema Reconciliation (Brian's real 551-device inventory) — `46f6042` + `8fe885f` + `6cf0bc3`

**Context:** Brian dropped `data/Devices.csv` (gitignored; 551 rows, SharePoint export). Coordinator paused Phase 2 RxX to reconcile the schema, then resumed parallel work.

### Phase A — Domain Schema + Migration — `46f6042`

**Schema changes (driven by Brian's 3 decisions):**
- `BrandId: Guid → Guid?` (37% of real devices have no vendor) — D-095
- Added 6 fields: `Purpose` (500), `OperatingSystem` (100), `IpAddress` (45 IPv6-safe), `MacAddress` (17, regex-validated, uppercase-normalized), `ProductUrl` (500, URI-validated), `Version` (50) — D-096
- License Key column DROPPED entirely (2% population + security burden) — D-097

**Migration:** `20260518215139_AddDeviceExtendedFieldsAndOptionalBrand` — additive, reversible, SQLite-compatible.

**Files modified:** 20 (1,314 +, 52 -). Included: Device.cs primary constructor, DeviceConfiguration EF mapping, CreateDeviceCommand + UpdateDeviceCommand validators, request/response DTOs, DeviceExportRow (Brand→nullable), `openapi.yaml` regen, Guard helpers for MAC/URL.

**Quality:** Build ✅, format ✅, 374 backend tests passing / 6 skipped / 0 failed.

### Phase B — CSV Import Mapper — `8fe885f`

**Approach:** Integrated into existing `DeviceImportProcessingService` (NOT a separate mapper class) — D-108. Reused existing CsvHelper + reference lookup catalog + ImportBatch infrastructure.

**Delivered:**
- 11 column aliases (Vendor→Brand, DeviceType→Category, Retired→Status, PurchasedDate→PurchaseDate, Networking→Network, DeviceName→Name, Url→ProductUrl) in `ImportFieldNames.cs`
- 4 SharePoint-specific helpers in `DeviceImportProcessingService.cs`: `ParseSharePointStatus` (Retired+Purpose regex 3-way mapping), `NormalizeNetworking` (N/A → null), `NormalizeMacAddress` (strip delimiters, output `XX:XX:XX:XX:XX:XX`), `NormalizeProductUrl` (HTTP/HTTPS absolute URIs)
- `CommitImportCommand.cs`: injected `INetworkRepository`, added Network auto-create case, extended `Device.Create` with 6 new params
- `ImportContracts.cs`: `ImportDevicePreview` extended with 6 fields
- 10-row synthetic sample fixture at `tests/TechInventory.IntegrationTests/Imports/SampleData/devices-sample.csv` covering all status branches + idempotency + malformed MAC + N/A networking + lenient date

**Status mapping (D-103):**
- `Retired=False` → Active
- `Retired=True` + Purpose `~/sold|given|donated|gifted|disposed|trashed/i` → Disposed (Purpose → DisposalMethod)
- `Retired=True` otherwise → Retired (RetiredDate fallback to PurchaseDate per D-100)

**Other key decisions:** D-098 (Networking auto-creates Network entities for v1 ergonomics, user can rename later), D-101 (reference data auto-create with idempotent name-match + batch cache), D-102 (synthetic fixture pattern; real CSV gitignored), D-104 (Network auto-create enabled), D-105 (N/A exact match), D-106 (MAC always colon-format), D-107 (HTTP/HTTPS only).

**Anomaly at handoff:** Phase B left 8 integration tests RED — root cause analysis showed the deeper issues (not just signature mismatch as coordinator initially diagnosed):
1. Import validator still required Brand (`.NotEmpty()`) despite entity nullable
2. `ParseSharePointStatus` called for ALL imports; rejected generic enum format used by existing tests
3. New SharePoint sample CSV missing `Owner` column

### Phase B-cleanup — Test Recovery + OpenAPI Drift — `6cf0bc3`

**Fixes:**
- Import validator: removed `.NotEmpty()` on Brand (D-109)
- Status parser: dual-format chain — try `Enum.TryParse<DeviceStatus>` first, fall back to `bool.TryParse` (D-110)
- SharePoint sample CSV: added `Owner` column with value `"Family"` (D-111)
- Regenerated `openapi.yaml` via `dotnet run -c Release -- export-openapi` (`ImportDevicePreview` now includes 6 new fields)

**Quality gates GREEN:** 240 unit + 137 integration / 6 skipped / 0 failed; build ✅, format ✅.

**Frontend handoff (D-112):** `schemas.ts` flagged stale — Vasquez owns regeneration (separate round).

**Decisions added:** D-095..D-112 (18 total across all three commits).

## Cross-Team Updates

### 2026-05-20: F030 Device Tagging — Vasquez integrated picker (Hicks' `ListDeviceTagsQuery` endpoint)

**Note from Vasquez (Frontend):** F030 device tagging picker shipped in commit 987c0a8 (field-test iteration). The picker consumes Hicks's `ListDeviceTagsQuery` endpoint (already landed) — working without issues. Tag selection / persistence / list display all wired correctly via generated TypeScript API client.



## 2026-06-13 — Deep Backend Audit & Monitor Visibility Bug Investigation

### Scope
Full backend audit covering: raw SQL, repository patterns, pagination efficiency, EF query patterns, controller/handler sizes, domain invariants vs validators, audit append-only enforcement, and household scoping. Investigated reported bug: newly created monitor appears after create and when filtering by brand, but NOT in the all inventory view.

### Key Findings

#### 1. **CRITICAL BUG: Ambiguous Default Status Filter Logic** (DeviceRepository.cs:228-235)
**Root cause of monitor visibility bug.**

When Status parameter is null, repository filters to exclude ONLY Disposed devices:
```csharp
if (criteria.Status.HasValue)
    query = query.Where(device => device.Status == criteria.Status.Value);
else
    query = query.Where(device => device.Status != DeviceStatus.Disposed);
```

**Problem**: Default behavior is inconsistent with user expectations. When no status is specified:
- Current: returns Active + Retired + InRepair + Lent (excludes only Disposed)
- Expected: likely should return only Active devices

**Bug scenario**:
1. Monitor created with default Status=Active
2. Brand filter view doesn't specify Status → gets default behavior (all except Disposed) → monitor appears
3. "All inventory" view likely requests Status=Active explicitly → monitor appears ONLY if still Active
4. If monitor was accidentally created with Status=InRepair or Lent, it appears in brand filter but not in explicit Active filter

**Fix**: Change line 234 to explicitly default to Active:
```csharp
query = query.Where(device => device.Status == DeviceStatus.Active);
```
OR document that null status means "everything except Disposed" and ensure frontend always passes explicit Status=Active when that's the intent.

#### 2. **CRITICAL PERFORMANCE: Pagination After Full Load** (Repository.cs:70-85, DeviceRepository.cs:27-38)
**Major N+1 / performance issue affecting all list operations.**

Pagination flow:
1. ApplyFilters() creates filtered IQueryable
2. ToPagedResultAsync() → MergeTrackedAsync() → .ToListAsync() loads **entire filtered result set** into memory
3. Merges with EF Local tracked entities (to include unsaved changes)
4. Sorts in memory using LINQ-to-Objects
5. **Then** applies Skip/Take for pagination

**Impact**:
- If database has 10,000 devices and user requests page 1 (25 items), all 10,000 are loaded into memory, sorted, then 25 returned
- Every list query triggers full table scan + in-memory processing
- Sorting happens in application layer, not database
- Network/memory overhead scales with total filtered count, not page size

**Why it exists**: To merge with EF's Local change tracker (unsaved entities in current UnitOfWork).

**Fix options**:
A. **Recommended**: Apply pagination at database level for read-only list queries:
   - Skip MergeTrackedAsync for AsNoTracking() queries
   - Apply .OrderBy() on IQueryable, then .Skip().Take(), then .ToListAsync()
   - Accept that unsaved entities won't appear in list results (acceptable for most UI scenarios)
B. Hybrid: Check if Local has relevant entities; if empty, use database pagination; if not, fall back to current merge logic
C. Document as "design tradeoff" and recommend keeping transactions short

**Related**: DeviceRepository.StreamExportAsync (line 54-64) also loads all filtered devices before sorting/streaming, but that's acceptable for export operations.

#### 3. **Repository Query Patterns: No Raw SQL** ✅
- Searched for FromSql, ExecuteSql, SqlQuery, raw SQL strings
- **Result**: Zero raw SQL usage. All queries use EF Core LINQ with parameterized expressions
- All filters applied via .Where() with strongly typed predicates

#### 4. **Controller Thickness: Acceptable** ✅
- Largest: DevicesController.cs (312 lines) — mostly request/command DTOs, not logic
- Controllers are thin: delegate to MediatR, use extension methods for Result→ActionResult mapping
- No business logic in controllers
- Request→Command mapping is boilerplate (could be reduced with source generators, but not a blocker)

#### 5. **Handler Sizes: Within Acceptable Range** ✅
- No handlers found exceeding 200 lines
- CreateDeviceCommandHandler: ~180 lines including reference validation (acceptable)
- BulkUpdateDevicesCommandHandler: ~200 lines (acceptable for bulk operation orchestration)
- Complex validation/orchestration is appropriate for handlers; no single-responsibility violations observed

#### 6. **Domain Invariants vs Validators: Correctly Separated** ✅
**Domain** (Device.cs, Brand.cs, etc.):
- Guards structural invariants: required fields, max lengths, format validation (MAC address, URLs)
- Enforces state transition rules (e.g., retired devices read-only except notes/disposal)
- Throws exceptions for invariant violations

**Validators** (FluentValidation):
- API input validation before domain layer
- User-facing error messages
- Cross-field validation (e.g., purchase year ranges)
- Reference existence/activeness checks happen in handlers (appropriate)

**Assessment**: Clean separation. Domain protects its own integrity; validators catch user errors early.

#### 7. **Audit Append-Only Enforcement: Properly Implemented** ✅
**AppDbContext.cs:65-82**: EnforceImmutableRecords() runs before SaveChanges:
```csharp
ThrowIfModifiedOrDeleted<AuditEvent>("AuditEvent rows are append-only...");
```
- Checks ChangeTracker.Entries<AuditEvent>() for Modified/Deleted states
- Throws InvalidOperationException if audit row is modified/deleted
- Also applies to ImportBatch (immutable after creation)

**IAuditEventRepository**: Exposes only AppendAsync, GetByIdAsync, ListAsync — no Update/Delete methods

**Assessment**: Audit log is properly write-once. Cannot be tampered with at repository or DbContext level.

#### 8. **Household Scoping: Not Applicable (Single-Household System)** ✅
**PRD line 56**: "❌ Multi-tenant SaaS — single household only"

No HouseholdId foreign key on Device, Brand, Category, Location, etc.
- Program.cs:377: Seeds single household record
- CreateDeviceCommandHandler: Validates exactly one household exists

**Assessment**: Intentional design for single-household use case. If multi-household is ever required, this becomes a breaking schema change.

#### 9. **List Endpoint Pagination & Filtering: Correct API Surface** ✅
**DevicesController.cs**:
- GET /api/v1/devices with query params: Page, PageSize, Status, BrandId, CategoryId, OwnerId, LocationId, NetworkId, Tags, Search, PurchaseYearFrom/To, SortBy, SortDescending
- Returns PagedResponse<DeviceResponse> with TotalCount, Page, PageSize
- Default PageSize=25, Page=1

**DeviceListCriteria.cs**:
- All filters nullable/optional
- Guards against empty GUIDs
- Validates date ranges (purchasedAfter <= purchasedBefore)

**Assessment**: API design is correct. Performance issue is in repository implementation, not endpoint design.

#### 10. **ReportingRepository: Multiple .ToListAsync() for Aggregations** ⚠️
**Minor concern**:
- GetSummaryAsync, GetEraReportAsync, GetTimelineReportAsync, etc. load filtered datasets into memory before grouping/aggregating
- Example (line 118-140): Loads all purchased devices, then groups by decade in memory

**Why acceptable**:
- Reports are read-only, infrequent, analytical queries
- Datasets are naturally bounded (e.g., purchased devices with dates)
- In-memory grouping/aggregation is simpler than complex SQL
- Performance is acceptable for family household scale (hundreds to low thousands of devices)

**Recommendation**: Monitor report query times. If slow, optimize specific reports with raw SQL or stored procedures.

### Design Questions for Brian

1. **Default Status Filter Behavior**:
   - Should Status=null mean "Active only" or "everything except Disposed"?
   - Should frontend always pass explicit Status=Active when showing main inventory?
   - Recommendation: Change default to Active-only for clarity

2. **Pagination Performance Tradeoff**:
   - Accept that unsaved entities won't appear in list views until SaveChanges()?
   - Is it acceptable to skip MergeTrackedAsync for read-only list queries to gain database-level pagination?
   - Recommendation: Yes — list views are read-heavy; unsaved entities appearing immediately is not critical

3. **Reporting Query Strategy**:
   - Current: Load filtered data, aggregate in memory
   - Alternative: Raw SQL or complex EF projections for aggregations
   - Recommendation: Keep current unless performance degrades

### Top 5 Backend Next Actions (Priority Order)

1. **FIX: Default Status Filter Logic** (HIGH PRIORITY — Active Bug)
   - File: src\TechInventory.Infrastructure\Persistence\Repositories\DeviceRepository.cs:234
   - Change: query = query.Where(device => device.Status == DeviceStatus.Active);
   - Test: Unit test for DeviceRepository.ListAsync with null Status
   - Verify: Frontend explicitly passes Status when needed

2. **OPTIMIZE: Database-Level Pagination for Device List** (HIGH PRIORITY — Performance)
   - File: src\TechInventory.Infrastructure\Persistence\Repositories\DeviceRepository.cs:27-38
   - Remove: ToPagedResultAsync → MergeTrackedAsync roundtrip
   - Replace with:
     ```csharp
     var ordered = ApplyQueryableOrdering(query, criteria.SortBy, criteria.SortDescending);
     var totalCount = await ordered.CountAsync(cancellationToken);
     var items = await ordered
         .Skip((criteria.PageRequest.Page - 1) * criteria.PageRequest.PageSize)
         .Take(criteria.PageRequest.PageSize)
         .ToListAsync(cancellationToken);
     return new PagedResult<Device>(items, totalCount, criteria.PageRequest.Page, criteria.PageRequest.PageSize);
     ```
   - Test: Integration test with 1000+ device dataset, verify page 1 doesn't load all rows

3. **TEST: Add DeviceRepository.ListAsync Coverage** (MEDIUM PRIORITY)
   - File: 	ests\TechInventory.UnitTests\Infrastructure\Repositories\DeviceRepositoryTests.cs (if exists) or Integration tests
   - Scenarios:
     - Default status filter (null → Active only after fix)
     - Explicit Status=InRepair should return only InRepair
     - Status=null should NOT return Disposed
     - Pagination correctness with large dataset
     - Sorting by Name, PurchaseDate, CreatedAt
   - Verify: .OrderBy() happens at database level (SQL trace)

4. **DOCUMENT: Repository Pattern Tradeoffs** (LOW PRIORITY)
   - File: docs\architecture\repository-pattern.md (create)
   - Explain: MergeTrackedAsync design rationale
   - Clarify: When to use .AsNoTracking() for read-only queries
   - Note: Unsaved entities won't appear in list results (by design after fix #2)

5. **REFACTOR: Extract ApplyQueryableOrdering** (LOW PRIORITY — Code Quality)
   - File: src\TechInventory.Infrastructure\Persistence\Repositories\DeviceRepository.cs:299-315
   - Issue: ApplyEnumerableOrdering and ApplyQueryableOrdering duplicate logic
   - Fix: Keep only ApplyQueryableOrdering after pagination fix; remove enumerable version
   - Benefit: Single source of truth for sorting logic

### Additional Observations

- **Soft Delete**: Implemented correctly via Status enum (Disposed), not IsDeleted flag
- **Brand/Category/Location Repositories**: Use DefaultQuery with .Where(x => x.IsActive) filter — clean pattern
- **Bulk Operations**: Properly use correlation IDs in audit events for traceability
- **No N+1 in Reports**: JOINs are done in single queries (e.g., GetInsuranceReportItemsAsync)
- **Tag Filtering**: Complex subquery in ApplyFilters (line 248-254) requires all specified tags (AND logic) — may need OR option in future

---

**Session completed**: 2026-06-13  
**Artifacts reviewed**: DeviceRepository, Repository base, all controllers, ReportingRepository, AppDbContext, domain entities, validators, 15 infrastructure repositories  
**Lines audited**: ~5,000 across backend layers  
**Critical issues**: 2 (default status filter, pagination performance)  
**Medium issues**: 0  
**Low issues/tech debt**: 3 (missing tests, documentation, refactor opportunity)

---

### 2026-06-14: Engineering Audit Session (Hicks)

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-hicks.md`

**Key Audit Findings:**
- No raw SQL/T-SQL detected — EF Core parameterized queries enforced ✓
- Thin controllers with minimal business logic ✓
- AuditEvent append-only enforcement correctly implemented ✓
- Repository pattern in place ✓
- **CRITICAL:** Pagination semantics need clarification — device list pagination behavior with filter changes mid-pagination needs alignment with API contract
- **CRITICAL:** Device status / list semantics inconsistently applied across endpoints — need clear contract for what "active" means

**Deliverables:**
- Audit findings documented in orchestration log
- 3 new decisions merged (D-170, D-172, D-173 touch backend concerns)
- Team coordination: findings shared with Ripley (architecture) for contract alignment

**Next Steps:** ADR for device list status filtering contract, pagination refactor for scalability.

---

### 2026-06-23: SQLitePCLRaw Vulnerability Fix (GHSA-2m69-gcr7-jv3q)

**Context:** Release Container Images workflow failing with NU1903 warning for SQLitePCLRaw.lib.e_sqlite3 2.1.11 vulnerability in Release builds.

**Root Cause:** Microsoft.EntityFrameworkCore.Sqlite 10.0.8 transitively pulled in vulnerable SQLitePCLRaw.lib.e_sqlite3 2.1.11.

**Solution:**
- Updated Microsoft.EntityFrameworkCore.Sqlite from 10.0.8 → 10.0.9
- Updated Microsoft.EntityFrameworkCore.Design from 10.0.8 → 10.0.9
- Added direct PackageReference to SQLitePCLRaw.bundle_e_sqlite3 3.0.3 to override vulnerable 2.1.11 dependency

**Files Changed:**
- src\TechInventory.Infrastructure\TechInventory.Infrastructure.csproj
- src\TechInventory.Api\TechInventory.Api.csproj
- tests\TechInventory.IntegrationTests\TechInventory.IntegrationTests.csproj

**Validation:**
- ✓ dotnet restore (0 warnings)
- ✓ dotnet build -c Release (0 warnings, 0 errors)
- ✓ SQLitePCLRaw packages now at version 3.0.3
- ✓ Release Container Images workflow: SUCCESS
- ✓ Quality Gate workflow: SUCCESS

**Commit:** 486fe6b

**Learning:** When EF Core SQLite dependencies have known vulnerabilities and the latest EF version hasn't caught up, adding an explicit SQLitePCLRaw.bundle_e_sqlite3 PackageReference at a patched version (3.0.3+) forces NuGet to resolve the safer transitive dependency chain.


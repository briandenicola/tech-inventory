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

**2026-05-18 (Phase 1 Round 6):** T32–T38, T40, and T41 landed in `src/TechInventory.Api` (commit `48c1920` + `74a1e21`). Attribute-routed controllers with explicit lowercase `/api/v1/...` paths for Devices, Brands, Categories (with tree route), Owners, Locations, Networks, Tags, and AuditEvents. Shared `ControllerResultExtensions.cs` maps `Result<T>.Success` → `Ok()` for reads, `CreatedAtAction()` for creates, `NoContent()` for patches; `Result.Failure` throws `ResultFailureException` for exception pipeline. Global `IExceptionHandler` + `ProblemDetailsFactory` (D-026, D-027) converts failures to RFC 7807 ProblemDetails (400/404/409/500 with validation `errors` dict). Dev auth bypass (D-022): `Auth:DevBypass=true` in `appsettings.Development.json` produces synthetic `dev-admin` principal with Admin role; startup throws if enabled outside Dev and logs warning. OpenAPI wired at `/openapi/v1.json`. Smoke tests: `GET /openapi/v1.json` → 200 JSON; `GET /api/v1/devices` → `{"items":[],"totalCount":0,"page":1,"pageSize":25}`; `POST /api/v1/brands -d '{"name":"TestBrand2"}'` → 201 with Location header; `POST /api/v1/brands -d ''` → 400 Validation ProblemDetails; `GET /api/v1/brands/{invalid-uuid}` → 404 ProblemDetails. 28 files modified. Verification: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, repo-root build blocked on Apone's integration test compile error (awaits fix).

**2026-05-18 (Phase 1 Round 5):** T20–T28 Application handlers complete (commit `1180cf6`). Device/Brand/Category/Owner/Location/Network/Tag command+query handlers finalized with validators, soft-delete semantics (retired → disposed transition for devices), PagedResponse paging for lists, category subtree recursion, owner active-owner delete-blocking, and tag-device join audit metadata. All 98 files verified: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` (182 succeeded, 78 skipped) ✅. **Coverage note:** Domain regression from 81.40% flagged in prior round; Apone recovered it in R5 to 100.00% via explicit AuditEvent/ImportBatch coverage work.

**2026-05-18 (Phase 1 Round 1):** Domain layer T01–T05 complete and tested. `EntityId` ULID primitives, `Currency` ISO 4217 value object, `Household`/`Device`/`Brand` aggregates with currency inheritance and retired-device edit guards all implemented. 13 Domain unit tests green (85% coverage). Currency decision finalized (D-008): per-device with household default. Pre-commit hook deployed for token-storage enforcement (Hudson). Vasquez's ESLint gate live. Apone's Domain contract tests executable. `dotnet build -c Release` ✅, `dotnet test -c Release` ✅, `dotnet format --verify-no-changes` ✅. Phase 2: Repository layer + Application command/query handlers.

**2026-05-18 (Phase 1 Round 2):** Domain layer T06–T10 complete and tested. `Category` (hierarchical depth validation), `Owner` (Entra identity hooks, role mapping), `Location`/`Network`/`Tag` (soft-delete semantics), `DeviceTag` (composite key, reactivation logic) all landed. 80 xUnit cases covering reference entities + Brand/Device supplemental. Domain line coverage: 97.6% (well above 85% floor). Verify pipeline green (`dotnet build`, `dotnet test`, `dotnet format` all ✅). Vasquez's vite.config fix unblocked frontend. Apone locked contract test pattern (D-016); blockers flagged: T11 (AuditEvent), T15 (Repository interfaces). Next phase: Application handlers + Infrastructure EF Core wiring.

**2026-05-18 (Phase 1 Round 4):** Infrastructure repositories + MediatR behaviors complete (T16–T19, commit `81f478d`). 10 concrete repositories implemented in `src/TechInventory.Infrastructure/Persistence/Repositories/` (`BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, `ImportBatchRepository`) with shared `Repository<TEntity, TKey>` base. `AuditSaveChangesInterceptor` stamps UTC audit columns. `ValidationBehavior` + `AuditBehavior` pipeline (D-020, D-021): validation first (returns `Result.Failure` cleanly), audit last (fires only on success). `IAuditContext` scoped strategy avoids second DB read. Exact-ID reads stay unit-of-work aware; list paths filter inactive/soft-deleted rows by default. Verify pipeline all green: `dotnet format`, `dotnet build -c Release`, `dotnet test -c Release` (151 tests ✅). Coverage snapshot: Domain 81.40%, Application 40.53%, Infrastructure 88.98%. Next: Coverage regression audit in Round 5; continue handler + controller wiring (T20–T42).


## Learnings

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

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

**2026-05-18:** Phase 0 parallel scaffolding complete. Security baseline now in effect (`docs/security-baseline.md`). **Currency strategy decision OPEN and blocks T04** — awaiting Brian's decision. Apone's integration test suite awaits `/health` endpoint wiring — see `tests/TechInventory.IntegrationTests/ApiSmokeTests.HealthEndpoint_Returns200Ok()` (currently skipped).

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

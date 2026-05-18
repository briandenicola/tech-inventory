# Tasks — 001 Core API

**Spec**: `specs/001-core-api/spec.md`
**Plan**: `specs/001-core-api/plan.md`
**Status**: Not Started

---

## Task List

### Foundation

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T01 | Create solution + projects | Hicks | `TechInventory.sln` with Domain, Application, Infrastructure, Api projects; test projects under `tests/`. All build green. | `dotnet build -c Release` passes with zero warnings | Plan §3 |
| T02 | Add shared kernel: Result<T>, Error, base Entity | Hicks | `Result<T>` record, `Entity` base class (Id, CreatedAt, ModifiedAt), guard clauses in Domain. | Unit tests for Result success/failure paths | Plan §2.2 |
| T03 | Wire Program.cs: DI, MediatR, FluentValidation, Swagger, health checks | Hicks | Minimal `Program.cs` with all service registrations. `/health` and `/swagger` respond. | `curl /health` → 200; `/swagger` returns JSON | Plan §4.4 |

### Domain Entities

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T04 | Add Device entity + DeviceStatus enum | Hicks | `Device.cs` with all fields from Plan §1.1, invariants enforced in constructor/methods. | Unit tests for invariants (e.g., retired device restrictions) | Plan §1.1 |
| T05 | Add Brand entity | Hicks | `Brand.cs` with Name uniqueness invariant. | Unit test: empty name throws | Plan §1.1 |
| T06 | Add Category entity (hierarchical) | Hicks | `Category.cs` with ParentId, max-depth-3 invariant. | Unit test: depth > 3 rejected | Plan §1.1 |
| T07 | Add Owner entity + OwnerRole enum | Hicks | `Owner.cs` with DisplayName required. | Unit test: null DisplayName throws | Plan §1.1 |
| T08 | Add Location entity + LocationType enum | Hicks | `Location.cs` with Type enum. | Unit test for construction | Plan §1.1 |
| T09 | Add Network entity | Hicks | `Network.cs` with Name uniqueness. | Unit test | Plan §1.1 |
| T10 | Add Tag + DeviceTag entities | Hicks | `Tag.cs`, `DeviceTag.cs` (composite key). | Unit test | Plan §1.1 |
| T11 | Add AuditEvent entity | Hicks | `AuditEvent.cs` — immutable after creation (no setters for mutation). | Unit test: can construct, fields readonly | Plan §1.1 |
| T12 | Add ImportBatch entity | Hicks | `ImportBatch.cs` with status enum. | Unit test | Plan §1.1 |

### Infrastructure (Persistence)

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T13 | Create AppDbContext + entity configurations | Hicks | `AppDbContext.cs` with all `DbSet<>`. Configurations enforce constraints (max lengths, indexes, relationships). | Builds clean; configurations cover all entities | Plan §3.1 |
| T14 | Add initial EF Core migration | Hicks | Migration creates all Phase 1 tables. Indexes justified in comments. | `dotnet ef migrations list` shows migration; `dotnet ef database update` succeeds | Plan §3.3 |
| T15 | Implement repository interfaces (Domain) | Hicks | `IDeviceRepository`, `IBrandRepository`, etc. in Domain/Interfaces/. | Compiles; no Infrastructure deps in Domain project | Plan §3.2 |
| T16 | Implement repository classes (Infrastructure) | Hicks | Concrete repos using AppDbContext. Generic base + specific queries. | Integration test: CRUD round-trip on SQLite | Plan §3.2 |
| T17 | Add SaveChangesInterceptor for audit columns | Hicks | Interceptor stamps CreatedAt/ModifiedAt/CreatedBy/ModifiedBy on save. | Integration test: dates auto-populated | Plan §3.1 |

### Application Layer (Handlers)

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T18 | Add ValidationBehavior pipeline | Hicks | Generic pipeline behavior that collects FluentValidation errors → Result.Failure. | Unit test: invalid command returns validation error | Plan §2.3 |
| T19 | Add AuditBehavior pipeline | Hicks | Pipeline behavior writes AuditEvent for IAuditable commands. | Unit test: after successful command, audit event written | Plan §2.3 |
| T20 | Device commands: Create, Update, Delete | Hicks | Handlers + validators for CreateDeviceCommand, UpdateDeviceCommand, DeleteDeviceCommand. | Unit tests with mocked repos; validators reject invalid input | Plan §2.1 |
| T21 | Device queries: GetById, List (paginated + filtered) | Hicks | Handlers for GetDeviceByIdQuery, ListDevicesQuery with all filter/sort params. | Unit tests; list returns PagedResponse | Plan §2.1 |
| T22 | Brand commands + queries | Hicks | Full CRUD handlers for Brand. | Unit tests | Plan §2.1 |
| T23 | Category commands + queries | Hicks | CRUD handlers; list returns hierarchical tree. | Unit test: tree structure correct | Plan §2.1 |
| T24 | Owner commands + queries | Hicks | CRUD handlers for Owner. | Unit tests | Plan §2.1 |
| T25 | Location commands + queries | Hicks | CRUD handlers for Location. | Unit tests | Plan §2.1 |
| T26 | Network commands + queries | Hicks | CRUD handlers for Network. | Unit tests | Plan §2.1 |
| T27 | Tag commands + queries + Device tagging | Hicks | Tag CRUD + AddTagToDevice / RemoveTagFromDevice commands. | Unit tests | Plan §2.1 |
| T28 | ClaimDeviceOwnership command | Hicks | Handler for PATCH /devices/{id}/owner. | Unit test: owner updated, audit written | Plan §2.1 |

### CSV Import

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T29 | Implement PreviewImport command | Hicks | Parses CSV, maps columns, validates rows, returns preview result. | Unit test with sample CSV; invalid rows flagged | Plan §5 |
| T30 | Implement CommitImport command | Hicks | Persists valid rows, creates lookup entities, writes ImportBatch. | Integration test: devices created, batch recorded | Plan §5 |

### Export

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T31 | Implement ExportDevices query | Hicks | Returns filtered devices as CSV or JSON stream. | Integration test: CSV parses cleanly | Plan §4.1 |

### API Controllers

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T32 | DevicesController | Hicks | All device endpoints wired to MediatR. Thin controller, no business logic. | Integration test: full CRUD via HTTP | Plan §4.1 |
| T33 | BrandsController | Hicks | CRUD endpoints for brands. | Integration test | Plan §4.1 |
| T34 | CategoriesController | Hicks | CRUD endpoints; GET returns tree. | Integration test | Plan §4.1 |
| T35 | OwnersController | Hicks | CRUD endpoints. | Integration test | Plan §4.1 |
| T36 | LocationsController | Hicks | CRUD endpoints. | Integration test | Plan §4.1 |
| T37 | NetworksController | Hicks | CRUD endpoints. | Integration test | Plan §4.1 |
| T38 | TagsController | Hicks | CRUD endpoints. | Integration test | Plan §4.1 |
| T39 | ImportsController | Hicks | Preview + Commit endpoints. | Integration test with file upload | Plan §4.1 |
| T40 | AuditEventsController | Hicks | List endpoint with filters. | Integration test | Plan §4.1 |
| T41 | ProblemDetails middleware | Hicks | Global handler maps exceptions/Result.Failure to RFC 7807. | Integration test: invalid request → ProblemDetails JSON | Plan §4.2 |
| T42 | ExportController (or devices sub-route) | Hicks | Export endpoint returns CSV/JSON. | Integration test | Plan §4.1 |

### Testing & Quality

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T43 | Unit test suite: Domain layer | Apone | All entity invariants, value objects tested. | Coverage ≥ 85% on Domain | Spec §4.2 |
| T44 | Unit test suite: Application layer | Apone | All handlers, validators, behaviors tested. | Coverage ≥ 85% on Application | Spec §4.2 |
| T45 | Integration test suite | Apone | Full API surface tested via WebApplicationFactory + real SQLite. | All endpoints exercised; happy + error paths | Spec §4.2 |
| T46 | Contract tests (OpenAPI validation) | Apone | Schemathesis or equivalent validates spec ↔ live API. | Zero drift between committed spec and runtime | Spec §4.3 |
| T47 | CI pipeline: build + test + format + vuln scan | Hudson | GitHub Actions workflow runs all checks. | PR blocked if any check fails | Constitution §7 |

### Documentation

| ID | Task | Owner | Outcome | Acceptance Check | Ref |
|----|------|-------|---------|-----------------|-----|
| T48 | Commit OpenAPI spec | Hicks | `openapi.yaml` generated and committed. | Valid per OpenAPI 3.1 schema | Spec §4.3 |

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

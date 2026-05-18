# Plan — 001 Core API

**Spec**: `specs/001-core-api/spec.md`
**Phase**: 1 (Weeks 3–5)
**Last Updated**: 2025-05-18

---

## 1. Domain Model

All entities live in `src/TechInventory.Domain/Entities/`. Value objects in `src/TechInventory.Domain/ValueObjects/`. Domain interfaces in `src/TechInventory.Domain/Interfaces/`.

### 1.1 Entities (Phase 1 Scope)

| Entity | Key Fields | Invariants |
|--------|-----------|------------|
| **Device** | Id (Guid), Name, Model, SerialNumber, BrandId, CategoryId, OwnerId, LocationId, NetworkId?, PurchaseDate?, PurchasePrice?, Currency?, Status (enum), Notes, RetiredDate?, DisposalMethod?, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy | ownerId → active Owner; retired Device read-only except notes + disposalMethod; Name required, non-empty |
| **Brand** | Id (Guid), Name, Website?, Notes?, IsActive, CreatedAt, ModifiedAt | Name unique, non-empty |
| **Category** | Id (Guid), Name, ParentId? (self-ref), Icon?, IsActive, CreatedAt, ModifiedAt | Name unique within same parent; max depth 3 |
| **Owner** | Id (Guid), DisplayName, EntraObjectId?, Role (enum), IsActive, CreatedAt, ModifiedAt | DisplayName required |
| **Location** | Id (Guid), Name, Type (enum: Home, Storage, External), IsActive, CreatedAt, ModifiedAt | Name unique, non-empty |
| **Network** | Id (Guid), Name, Description?, IsActive, CreatedAt, ModifiedAt | Name unique, non-empty |
| **Tag** | Id (Guid), Name, Color?, CreatedAt | Name unique, non-empty |
| **DeviceTag** | DeviceId, TagId | Composite PK; no duplicates |
| **AuditEvent** | Id (Guid), EntityType, EntityId, Action (enum: Created, Updated, Deleted), UserId?, Timestamp, BeforePayload (JSON), AfterPayload (JSON) | Append-only: no update, no delete |
| **ImportBatch** | Id (Guid), Filename, ImportedBy?, RowCount, SuccessCount, ErrorCount, Status (enum), ErrorLog (JSON), CreatedAt | Immutable after creation |

### 1.2 Enums

- `DeviceStatus`: Active, Retired, Disposed, InRepair, Lent
- `LocationType`: Home, Storage, External
- `AuditAction`: Created, Updated, Deleted
- `ImportStatus`: Pending, Completed, PartialSuccess, Failed
- `OwnerRole`: Admin, Member, Viewer

### 1.3 Deferred

- **Attachment**: v2 per PRD §13. Not modeled in Phase 1.

---

## 2. Application Layer

Project: `src/TechInventory.Application/`

### 2.1 MediatR Convention

Commands and queries follow this naming pattern:

```
Features/
  Devices/
    Commands/
      CreateDevice/
        CreateDeviceCommand.cs        (IRequest<Result<DeviceResponse>>)
        CreateDeviceCommandHandler.cs (IRequestHandler<...>)
        CreateDeviceCommandValidator.cs (AbstractValidator<...>)
      UpdateDevice/
      DeleteDevice/
      ClaimDeviceOwnership/
    Queries/
      GetDeviceById/
        GetDeviceByIdQuery.cs
        GetDeviceByIdQueryHandler.cs
      ListDevices/
        ListDevicesQuery.cs           (includes pagination, filter, sort params)
        ListDevicesQueryHandler.cs
  Brands/
    Commands/ ...
    Queries/ ...
  Categories/ ...
  Owners/ ...
  Locations/ ...
  Networks/ ...
  Tags/ ...
  Import/
    Commands/
      PreviewImport/
      CommitImport/
  Export/
    Queries/
      ExportDevices/
  AuditEvents/
    Queries/
      ListAuditEvents/
```

### 2.2 Result<T> Envelope

```csharp
public sealed record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public Error? Error { get; init; }
}

public sealed record Error(string Code, string Message);
```

Handlers return `Result<T>`. Controllers map:
- `IsSuccess` → 200/201/204
- `Error.Code == "NotFound"` → 404 ProblemDetails
- `Error.Code == "Validation"` → 400 ProblemDetails
- `Error.Code == "Conflict"` → 409 ProblemDetails

### 2.3 Pipeline Behaviors

Registered in DI order:
1. **LoggingBehavior** — logs request start/end, elapsed time
2. **ValidationBehavior** — collects FluentValidation failures → returns Result with validation error
3. **AuditBehavior** — for commands implementing `IAuditable`, writes AuditEvent after success

### 2.4 FluentValidation

Every command/query has a corresponding `*Validator.cs`. Validators:
- Use `RuleFor` with clear error messages
- Reference domain invariants but don't duplicate domain logic
- Run as pipeline behavior before handler execution (constitution §3.6)

---

## 3. Infrastructure Layer

Project: `src/TechInventory.Infrastructure/`

### 3.1 EF Core DbContext

```
Persistence/
  AppDbContext.cs
  Configurations/
    DeviceConfiguration.cs
    BrandConfiguration.cs
    CategoryConfiguration.cs
    OwnerConfiguration.cs
    LocationConfiguration.cs
    NetworkConfiguration.cs
    TagConfiguration.cs
    DeviceTagConfiguration.cs
    AuditEventConfiguration.cs
    ImportBatchConfiguration.cs
  Migrations/
    (EF Core generated, checked in, reviewed)
```

- SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite`)
- Connection string injected via configuration
- `SaveChangesInterceptor` stamps `CreatedAt`/`ModifiedAt`/`CreatedBy`/`ModifiedBy`

### 3.2 Repository Pattern

Interfaces defined in **Domain** layer:
```
src/TechInventory.Domain/Interfaces/
  IDeviceRepository.cs
  IBrandRepository.cs
  ICategoryRepository.cs
  IOwnerRepository.cs
  ILocationRepository.cs
  INetworkRepository.cs
  ITagRepository.cs
  IAuditEventRepository.cs
  IImportBatchRepository.cs
```

Implementations in **Infrastructure**:
```
src/TechInventory.Infrastructure/Repositories/
  DeviceRepository.cs
  ...
```

Generic base: `IRepository<T>` with `GetByIdAsync`, `ListAsync`, `AddAsync`, `UpdateAsync`. Specific repos extend with domain-specific queries.

### 3.3 Migrations

- Folder: `src/TechInventory.Infrastructure/Persistence/Migrations/`
- Naming: timestamp-based (EF default)
- Every new index requires a comment in the migration justifying it (constitution §4.1)
- Initial migration creates all Phase 1 tables

---

## 4. API Layer

Project: `src/TechInventory.Api/`

### 4.1 Controller Surface

All routes under `/api/v1/`:

| Resource | Endpoints |
|----------|-----------|
| Devices | GET (list, paginated+filtered), GET /:id, POST, PUT /:id, DELETE /:id |
| Devices (sub) | PATCH /:id/owner, POST /:id/tags, DELETE /:id/tags/:tagId |
| Devices (export) | GET /devices/export?format=csv\|json |
| Brands | GET, GET /:id, POST, PUT /:id, DELETE /:id |
| Categories | GET (tree), GET /:id, POST, PUT /:id, DELETE /:id |
| Owners | GET, GET /:id, POST, PUT /:id, DELETE /:id |
| Locations | GET, GET /:id, POST, PUT /:id, DELETE /:id |
| Networks | GET, GET /:id, POST, PUT /:id, DELETE /:id |
| Tags | GET, GET /:id, POST, PUT /:id, DELETE /:id |
| Imports | POST /preview, POST /commit |
| AuditEvents | GET (filtered by entityType, entityId, dateRange) |
| Health | GET /health, GET /health/ready |

### 4.2 ProblemDetails Middleware

Global exception handler maps:
- `ValidationException` → 400
- `NotFoundException` → 404
- `ConflictException` → 409
- Unhandled → 500 (generic message; details logged, not leaked)

All responses include `type`, `title`, `status`, `detail`, `instance` per RFC 7807.

### 4.3 Pagination DTOs

```csharp
public sealed record PagedRequest(int Page = 1, int PageSize = 25, string? SortBy = null, bool SortDescending = false);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
```

PageSize clamped: min 1, max 200 (constitution §4.2).

### 4.4 OpenAPI / Swagger

- Swashbuckle or NSwag generates OpenAPI 3.1
- Served at `/swagger` (PRD §F6)
- Schema committed at `src/TechInventory.Api/openapi.yaml` for client generation
- Regenerated on build; CI fails if committed spec diverges from runtime

### 4.5 Filtering

Device list supports query parameters:
- `search` (free-text across name, model, serial, notes)
- `brandId`, `categoryId`, `ownerId`, `locationId`, `networkId`
- `status` (enum filter)
- `tags` (comma-separated tag IDs, AND logic)
- `purchasedAfter`, `purchasedBefore` (date range)
- `sortBy` (name, purchaseDate, createdAt)
- `sortDescending` (bool)

---

## 5. CSV Import Pipeline

Per PRD §F1:

### 5.1 Two-Phase Flow

1. **Preview** (`POST /api/v1/imports/preview`):
   - Accepts multipart file upload
   - Parses CSV, maps columns to canonical schema
   - Validates each row against domain rules
   - Returns per-row results: `{rowNumber, status: valid|invalid, errors[], mappedData}`
   - Does NOT persist anything

2. **Commit** (`POST /api/v1/imports/commit`):
   - Accepts the same file (or a reference token from preview)
   - Persists valid rows as Devices (+ creates Brand/Category/Owner if not found)
   - Skips invalid rows
   - Creates `ImportBatch` record with summary
   - Writes AuditEvent per created device

### 5.2 Column Mapping

Default mapping for SharePoint CSV (configurable):
- `Title` → Device.Name
- `Brand` → Brand.Name (lookup or create)
- `Model` → Device.Model
- `Serial Number` → Device.SerialNumber
- `Category` → Category.Name (lookup or create)
- `Owner` → Owner.DisplayName (lookup or create)
- `Location` → Location.Name (lookup or create)
- `Purchase Date` → Device.PurchaseDate
- `Purchase Price` → Device.PurchasePrice
- `Status` → Device.Status (map known values)
- `Notes` → Device.Notes

### 5.3 Error Handling

- Per-row errors do not abort the batch
- Error log stored as JSON in ImportBatch.ErrorLog
- Client receives structured per-row error report

---

## 6. Audit Log

Per constitution §4.3 and PRD §F7:

- **AuditBehavior** (MediatR pipeline behavior) intercepts all commands that implement `IAuditable`
- Captures before-state (read before handler) and after-state (read after handler)
- Serializes to JSON payloads
- Writes AuditEvent via `IAuditEventRepository.AddAsync()`
- AuditEvent table has no UPDATE/DELETE operations — enforced at repository level (throw if called)

Query endpoint: `GET /api/v1/audit-events?entityType=Device&entityId={id}&from=...&to=...`

---

## 7. Testing Strategy (Phase 1)

### 7.1 Unit Tests (`tests/TechInventory.UnitTests/`)

- **Domain layer**: entity invariants, value object equality, enum behavior
- **Application layer**: handler logic (mocked repos via NSubstitute), validators, pipeline behaviors
- **Target**: ≥ 85% line coverage on Domain + Application (constitution §7.1)
- Framework: xUnit + FluentAssertions + NSubstitute

### 7.2 Integration Tests (`tests/TechInventory.IntegrationTests/`)

- Real SQLite (file-per-test or in-memory)
- Test through `WebApplicationFactory<Program>`
- Cover: each endpoint returns correct status codes, pagination works, filters work, audit events written
- CSV import round-trip test
- No mocked DbContext (PRD §7.5.2 testing discipline)

### 7.3 Contract Tests

- Run Schemathesis (or equivalent tool) against the running API
- Validates every endpoint matches the committed `openapi.yaml`
- Catches drift between spec and implementation

### 7.4 What's NOT Tested in Phase 1

- Auth flows (Phase 2)
- E2E / Playwright (Phase 4 — requires web client)
- Performance / load testing (Phase 5)

---

## 8. Open Decisions to Escalate

| # | Decision | Proposed | Status |
|---|----------|----------|--------|
| 1 | Currency handling (PRD §14) | Per-device ISO 4217 code with household default in appsettings | Written to `decisions/inbox/ripley-currency-strategy.md` |
| 2 | OpenAPI generator: Swashbuckle vs NSwag vs Scalar | Scalar (modern, .NET 10 native support) | Decide during T03 |
| 3 | Result<T> — custom vs existing lib (Ardalis.Result, FluentResults) | Custom minimal record; avoids external dep in Domain | Decided: custom |

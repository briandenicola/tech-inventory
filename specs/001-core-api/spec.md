# Spec 001 — Core API

**Status**: Shipped (48/48 tasks complete)
**Phase**: Canonical **P1 — Core API** under PRD §13 as rewritten 2026-05-19. *(Originally drafted as "Phase 1 (Weeks 3–5) per PRD §13" under the pre-rewrite numbering; the spec content is unchanged, only the phase label is reframed.)*
**Owner**: Ripley (Lead/Architect)
**Last Updated**: 2025-05-18

---

## 1. Scope

This phase delivers the **complete REST API** for device and reference-data management, including:

- CRUD endpoints for **Device** and all reference entities: **Brand**, **Category**, **Owner**, **Location**, **Tag**
- **CSV import** pipeline (PRD §F1): dry-run preview, partial-success per-row, ImportBatch audit record
- **OpenAPI 3.1** specification served at `/swagger`
- **AuditEvent** append-only writes on every mutation (constitution §4.3)
- **ProblemDetails** (RFC 7807) error responses on all endpoints (constitution §3.4)
- Pagination, filtering, and sorting per RFC conventions (PRD §F6)
- Health endpoints: `/health` (liveness), `/health/ready` (readiness)

All endpoints are unauthenticated in Phase 1. Auth is Phase 2 (`specs/002-auth-entra`).

---

## 2. Out of Scope

| Item | Deferred To |
|------|-------------|
| Authentication & authorization (Entra ID, roles, policies) | Phase 2 |
| Docker Compose production deployment, TLS, backups | Phase 3 |
| SvelteKit web client / PWA | Phase 4 |
| Attachment / photo storage | v2 (PRD §15 Glossary, §13 v2 Candidates) |
| Rate limiting | Phase 2 (requires identity) |
| Saved views per user | Phase 4 (UI concern) |
| Timeline view / "Tech Eras" | Phase 4 |
| Warranty reminders, depreciation tracking | v2 |
| Bulk edit UI | v2 (API bulk operations are in scope) |
| Export endpoint (CSV/JSON) | Phase 1 — included (PRD §F4) |

---

## 3. User Stories Satisfied

Per PRD §5, this phase satisfies:

| ID | Story | How Satisfied |
|----|-------|---------------|
| U1 | Admin imports SharePoint CSV | CSV import endpoint with dry-run + commit |
| U2 | Admin edits any device record | PUT `/api/v1/devices/{id}` |
| U3 | Admin views audit history | GET `/api/v1/audit-events` with entity filter |
| U4 | Admin exports filtered data | GET `/api/v1/devices/export?format=csv\|json` |
| U7 | Admin manages Locations | CRUD `/api/v1/locations` |
| U8 | Admin manages Networks | CRUD `/api/v1/networks` (modeled as a reference entity) |
| U9 | Admin manages Categories | CRUD `/api/v1/categories` (hierarchical) |
| U11 | Member searches by name/brand/year | GET `/api/v1/devices?search=...&brand=...&year=...` |
| U12 | Member claims device ownership | PATCH `/api/v1/devices/{id}/owner` |
| U13 | Member adds notes to devices | PATCH `/api/v1/devices/{id}` (notes field) |
| U15 | Member performs CRUD on devices | Full Device CRUD surface |
| U16 | Member applies category labels | POST/DELETE `/api/v1/devices/{id}/tags` |
| U21 | System logs every mutation | AuditEvent written by pipeline behavior |

---

## 4. Acceptance Criteria

### 4.1 Functional

1. **Device CRUD**: Create, Read (single + list), Update, Soft-Delete a Device via API; verify round-trip with integration test.
2. **Reference CRUD**: Each of Brand, Category, Owner, Location, Tag, Network has full CRUD.
3. **Category hierarchy**: Categories support `parentId`; API returns tree structure on list.
4. **Tagging**: A Device can have 0..N Tags; manage via sub-resource endpoint.
5. **CSV import**:
   - `POST /api/v1/imports/preview` returns row-level validation results without persisting.
   - `POST /api/v1/imports/commit` persists valid rows, skips invalid, returns ImportBatch summary.
   - ImportBatch record written to DB with filename, row count, error log.
6. **Export**: `GET /api/v1/devices/export?format=csv` and `format=json` return filtered data.
7. **Audit**: Every create/update/delete writes an AuditEvent with entityType, entityId, action, before/after payload, timestamp. AuditEvents are never updated or deleted.
8. **Pagination**: All list endpoints paginated (default 25, max 200 per constitution §4.2). Response includes `totalCount`, `page`, `pageSize`.
9. **Filtering & sorting**: Devices filterable by brand, category, owner, location, status, year range, free-text search. Sortable by name, purchaseDate, createdAt.
10. **ProblemDetails**: All 4xx/5xx responses conform to RFC 7807.
11. **OpenAPI**: `/swagger` serves valid OpenAPI 3.1 document; all endpoints documented.
12. **Health**: `/health` returns 200 (liveness); `/health/ready` checks DB connectivity.

### 4.2 Non-Functional

1. P95 API response < 300ms for single-entity CRUD on SQLite (PRD §7 Performance).
2. Domain + Application layer test coverage ≥ 85% (constitution §7.1).
3. Zero `dotnet format` violations.
4. Zero vulnerable packages at Moderate+ severity.
5. Integration tests use real SQLite (in-memory or file-per-test); no mocked DbContext.

### 4.3 Contract Tests

1. A Schemathesis (or equivalent) run against the live API validates every endpoint matches the OpenAPI spec.
2. Spec is committed at `src/TechInventory.Api/openapi.yaml` and verified in CI.

---

## 5. Reference Patterns

### R1: drinks-and-desserts

| Pattern | Source Path | Application Here |
|---------|-------------|-----------------|
| MediatR handler structure | `src/Application/Features/` | Command/query shape for all use cases |
| ProblemDetails middleware | `src/Api/Middleware/` | Global exception → ProblemDetails mapping |
| Health check setup | `src/Api/Program.cs` | `/health` and `/health/ready` wiring |

### R2: coin-collection-app

| Pattern | Source Path | Application Here |
|---------|-------------|-----------------|
| Inventory domain shape | `src/Domain/` | Device, Brand, Category entity design |
| CSV import pipeline | `src/Application/Import/` | Dry-run/commit two-phase import |

> **Note**: Reference SHAs are unpinned in `docs/references.md`. Before implementation, agents must fetch and pin. Constitution wins if conflict.

---

## 6. Key Constraints (Quoted from Constitution)

> "Dependencies point inward only; no leakage outward. Domain layer has **no** dependencies on frameworks, EF Core, HTTP, or I/O." — Constitution §2.2

> "AuditEvent table records all mutations (append-only, never updated)" — Constitution §4.3

> "Minimum 85% line coverage on Domain and Application layers" — Constitution §7.1

> "Pagination required on all list endpoints (default page size 25, max 200)" — Constitution §4.2

> "API errors returned as ProblemDetails (RFC 7807)" — Constitution §3.4

> "FluentValidation for all command/query inputs. Validation runs before handler execution (pipeline behavior)" — Constitution §3.6

---

## 7. Open Questions

| # | Question | Impact | Proposed Resolution |
|---|----------|--------|---------------------|
| 1 | Currency handling — single household currency or per-device? (PRD §14) | Device.PurchasePrice + Device.Currency fields | Default to per-device with ISO 4217 code; household default in config. See `decisions/inbox/ripley-currency-strategy.md`. |
| 2 | Network entity — is it a standalone reference entity or a Tag subtype? | API surface, data model | Standalone entity (parity with Location); PRD §U8 treats it as managed list. |
| 3 | Soft-delete on reference entities (Brand, Location, etc.) — allowed or archive-only? | API behavior | Soft-delete with `IsActive` flag; devices referencing inactive refs remain valid but display a warning. |

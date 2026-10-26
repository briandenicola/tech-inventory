# Specification: Read-only device feed for the household life store

**Status:** Complete
**Approved by:** briandenicola (explicit instruction, 2026-10-26: "start tech-inventory" for the life store full-read feed)
**Serves:** PRD §3 Goals: "all functionality available via versioned REST API" and "Export all data"
**Not:** an integration marketplace (PRD Non-Goals). This is one read endpoint for one household consumer.

## 1. Goal

The household life store reads each household app in full on every poll, skips
what is unchanged by hash, and treats a record that is no longer listed as deleted.
Tech Inventory gives it one paged, read-only endpoint in the shape the life store
reads from every app it owns: `{schema, records: [{kind, id, updatedAt, data}], next}`.

## 2. Scope

### In scope
- `GET /api/v1/devices/feed?page=&limit=`, read with an existing `inventory.read` key
  (`Authorization: ApiKey …`). It lives under `/api/v1/devices`, so the ADR 0003
  allow-list admits it unchanged.
- Every device, **every status** (Disposed included: nothing is hard-deleted, PRD §3),
  oldest `CreatedAt` first, then `Id`.
- Brand, category, owner, location, network and active tag **names** resolved in the record.

### Out of scope (Non-Goals)
- Writes, webhooks, change-since filters, or any schema change or migration.
- New key scopes, or changes to `ApiKeyScopePolicy`.
- A UI. The consumer is a service.

## 3. Contract

| Field | Rule |
|---|---|
| `schema` | `techinventory.lifestore.records/1` |
| `records[].kind` | `device` |
| `records[].id` | Device id (GUID string) |
| `records[].updatedAt` | Device `ModifiedAt` |
| `records[].data` | `DeviceFeedItem`: id, name, model, brand, category, owner, location, network, tags, status, purchaseDate, purchasePrice, currencyCode, warrantyExpiry, retiredDate, disposalMethod, purpose, operatingSystem, version, createdAt, modifiedAt |
| `next` | Token for the following page, or `null` on the last page |
| `limit` | Default 25, 1–200 (constitution §4.2) |
| `page` | Absent for the first page; otherwise a `next` value. Anything else returns `400` |

**Excluded, as for MCP (ADR 0004, spec 006 FR-5):** serial number, IP address, MAC
address, notes, product URL, and audit actors (`createdBy`, `modifiedBy`). The life
store hands what it reads to agents, so free text and network identifiers stay out.

**Paging stability:** devices are only ever added (deletion is a status), and new
devices sort last, so a page already read never shifts during a full read.

## 4. Acceptance criteria

| AC | Criterion |
|---|---|
| AC-1 | An `inventory.read` key gets `200` with every device, Disposed included, oldest first |
| AC-2 | Reference ids are resolved to names; only active tags are listed |
| AC-3 | The excluded fields of §3 are absent from the payload |
| AC-4 | `next` pages through all devices and is `null` on the last page |
| AC-5 | An unknown `page`, or `limit` outside 1–200, returns `400` |
| AC-6 | No credentials returns `401` |
| AC-7 | OpenAPI and the generated client are regenerated; `task verify` passes |

## 5. Tests

| Layer | What | Where |
|---|---|---|
| Unit | Handler record mapping and `next`; validator bounds and tokens | `tests/TechInventory.UnitTests/Application/DeviceFeedQueryTests.cs` |
| Integration (real HTTP, real key) | AC-1–AC-6 | `tests/TechInventory.IntegrationTests/Feed/DeviceFeedTests.cs` |

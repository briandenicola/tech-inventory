# Spec 005 — API Key Support

**Status**: Specified (implementation not started)
**Phase**: P4 — Continuous Iteration
**Owner**: Ripley (Lead / Architect)
**GitHub Issue**: [#149](https://github.com/briandenicola/tech-inventory/issues/149)
**ADR**: [`docs/adr/0003-api-key-authentication.md`](../../docs/adr/0003-api-key-authentication.md)
**Last Updated**: 2026-09-03

---

## 1. Problem

Family members want to drive device-inventory operations from CLI scripts,
scheduled tasks, and Home Assistant integrations without opening a browser.
The two existing authentication schemes (Entra ID OIDC + PKCE and F025 local
HS256) both require interactive sign-in and are unsuitable for headless use.

## 2. Scope

### In Scope

| Area | Detail |
|---|---|
| API key CRUD | Create, list, revoke endpoints under `/api/v1/api-keys` |
| Auth handler | Third forwarding target in the `TechInventoryAuth` PolicyScheme |
| Inventory access | Device CRUD and reference-data (brand, category, location, network, owner, tag) reads |
| Audit | AuditEvent on create/revoke; Serilog structured log on every auth attempt |
| Settings UI (Wave 2) | Personal key management page in authenticated settings |
| Admin UI (Wave 2) | Emergency revocation of any user's keys in admin panel |
| OpenAPI | `ApiKeyAuth` security scheme in `openapi.yaml` |
| Operations | `docs/operations.md` section for pepper management and key administration |

### Out of Scope (Non-Goals)

These items are explicitly excluded. Adding any requires a new ADR.

| Non-Goal | Rationale |
|---|---|
| Service principals / machine identities | Household app; human owners delegate via personal keys |
| OAuth client credentials flow | Over-engineered; adds Entra app-registration dependency (ADR 0003) |
| Viewer-role key creation | Viewer is read-only and not expected to automate; can be revisited |
| Admin / audit / import / export / report / settings / audit-event scopes | Keys are inventory-only; broader scopes require a scope-governance ADR |
| Fine-grained per-resource scopes (e.g., per-device, per-location) | Two coarse scopes (`inventory.read`, `inventory.write`) are sufficient for v1 |
| Rotate endpoint with grace period | Revoke-then-create workflow with 5-key quota is sufficient for a household |
| Non-expiring keys | Constitution §5 + ADR 0003: all credentials must expire |
| mTLS / IP allowlists as auth | Reverse proxy concern (architecture.md §6.3), not application-layer auth |
| Bulk key management (mass-revoke, mass-create) | Single-household; at most 5 keys per owner |
| Usage dashboard / analytics per key | Per-use AuditEvent is a non-goal (write amplification on SQLite); Serilog logs suffice |
| AuditEvent on every API key use | Write amplification; structured Serilog log per auth attempt instead |
| Browser E2E tests | Retired (ADR 0002); integration, component, and manual checks instead |

## 3. User Stories

### US-1: Create an API Key (Admin / Member)

> As an Admin or Member, I want to create a named API key with a chosen scope
> and optional custom expiry so that I can use it in scripts and integrations.

**Acceptance criteria:**

1. `POST /api/v1/api-keys` with `{ name, scope, expiresInDays? }` returns
   `201 Created` with the full key (`selector.secret`) displayed exactly once.
2. `name` is 1–100 characters, unique per owner.
3. `scope` is one of `inventory.read` or `inventory.write`.
4. `expiresInDays` defaults to 90; must be 1–365.
5. If the owner already has 5 active (non-revoked, non-expired) keys, the
   request returns `409 Conflict` with ProblemDetails `code=QuotaExceeded`.
6. Viewer-authenticated requests receive `403 Forbidden`.
7. An `AuditEvent` is recorded with `Action=Created`, the selector (not the
   secret), scope, and expiry.

### US-2: List My API Keys (Admin / Member)

> As an Admin or Member, I want to list my API keys (without secrets) so I
> can see which keys exist and when they expire.

**Acceptance criteria:**

1. `GET /api/v1/api-keys` returns a paginated list of the caller's keys.
2. Each item includes `id`, `name`, `selector`, `scope`, `createdAt`,
   `expiresAt`, `revokedAt` (nullable), `isActive` (computed: not revoked
   and not expired).
3. The response never includes the secret or the verifier hash.
4. Viewer-authenticated requests receive `403 Forbidden`.

### US-3: Revoke My API Key (Admin / Member)

> As an Admin or Member, I want to revoke one of my API keys so that it can
> no longer be used.

**Acceptance criteria:**

1. `DELETE /api/v1/api-keys/{id}` sets `RevokedAt` to now and returns
   `204 No Content`.
2. The key must belong to the caller (or the caller must be Admin — see US-4).
3. Revoking an already-revoked key is idempotent (`204`).
4. An `AuditEvent` is recorded with `Action=Deleted` and the selector.
5. Subsequent API requests using the revoked key receive `401 Unauthorized`.

### US-4: Admin Emergency Revocation

> As an Admin, I want to revoke any user's API key so that I can respond to
> a compromised credential without waiting for the key owner.

**Acceptance criteria:**

1. `DELETE /api/v1/api-keys/{id}` succeeds for Admin regardless of key
   ownership.
2. The audit event records the Admin as actor, not the key owner.
3. Admin UI for this (Wave 2) shows all keys with owner filter.

### US-5: Authenticate with an API Key

> As a script or integration, I want to send `Authorization: ApiKey
> <selector>.<secret>` and access inventory endpoints according to the key's
> scope.

**Acceptance criteria:**

1. A valid key with `inventory.read` scope can `GET /api/v1/devices` and
   all reference-data list endpoints.
2. A valid key with `inventory.write` scope can additionally `POST`, `PUT`,
   `DELETE` on `/api/v1/devices`.
3. An `inventory.write` key cannot access `/api/v1/audit-events`,
   `/api/v1/imports`, `/api/v1/exports`, `/api/v1/reports`, `/api/v1/settings`,
   or `/api/v1/auth/local/*`.
4. If the key's owner has been deactivated (`Owner.IsActive == false`) since
   key creation, the request receives `401 Unauthorized`.
5. If the key's owner's role has been demoted below the role required for
   the endpoint (e.g., demoted to Viewer), the request receives
   `401 Unauthorized` (live ceiling).
6. Expired keys receive `401 Unauthorized`.
7. Revoked keys receive `401 Unauthorized`.
8. All failure responses use the same ProblemDetails shape
   (`code=InvalidApiKey`) — no enumeration.

### US-6: Settings UI — Manage My Keys (Wave 2)

> As an Admin or Member, I want a page in Settings to create, view, and
> revoke my API keys without using `curl`.

**Acceptance criteria:**

1. Settings page at `/settings/api-keys` lists the user's keys.
2. "Create" flow displays the full key exactly once in a copy-to-clipboard
   field with a "you will not see this again" warning.
3. "Revoke" requires confirmation (destructive action per constitution §6.5.4).
4. All strings from `src/lib/i18n/en.json`; no hard-coded text.
5. Component test with loading/empty/error/success states.
6. axe-core accessibility: zero violations.

### US-7: Admin UI — Emergency Revocation (Wave 2)

> As an Admin, I want an admin panel page to see all API keys across all
> users and revoke any key in an emergency.

**Acceptance criteria:**

1. Admin page at `/admin/api-keys` shows all keys with owner name filter.
2. Revoke action requires typed-name confirmation for destructive action.
3. Component test with axe-core: zero violations.

## 4. API Contract

### 4.1 Endpoints

All endpoints require authentication. API key endpoints are not themselves
accessible via API key authentication — they require a bearer token (Entra
or Local).

#### `POST /api/v1/api-keys`

**Authorization**: `AdminOrMember` policy (Entra or Local bearer only)

**Request:**

```json
{
  "name": "Home Assistant",
  "scope": "inventory.read",
  "expiresInDays": 90
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | yes | 1–100 chars, unique per owner |
| `scope` | string | yes | `inventory.read` or `inventory.write` |
| `expiresInDays` | integer | no | 1–365; default 90 |

**Response `201 Created`:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Home Assistant",
  "selector": "<base64url-selector>",
  "scope": "inventory.read",
  "createdAt": "2026-09-03T20:00:00Z",
  "expiresAt": "2026-12-02T20:00:00Z",
  "key": "<selector>.<secret>"
}
```

The `key` field contains the full credential (`selector.secret`). It is
returned **exactly once** and never stored or retrievable again.

**Error responses:**

| Status | Code | Condition |
|---|---|---|
| `400` | `ValidationError` | Invalid name, scope, or expiry |
| `403` | `Forbidden` | Viewer role or API key auth attempt |
| `409` | `QuotaExceeded` | Owner already has 5 active keys |

#### `GET /api/v1/api-keys`

**Authorization**: `AdminOrMember` policy (Entra or Local bearer only)

**Query parameters:**

| Param | Type | Default | Constraints |
|---|---|---|---|
| `page` | integer | 1 | ≥ 1 |
| `pageSize` | integer | 25 | 1–200 |

**Response `200 OK`:**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Home Assistant",
      "selector": "<base64url-selector>",
      "scope": "inventory.read",
      "createdAt": "2026-09-03T20:00:00Z",
      "expiresAt": "2026-12-02T20:00:00Z",
      "revokedAt": null,
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1,
  "totalPages": 1
}
```

The response **never** includes `key`, `secret`, or `verifierHash`.

#### `DELETE /api/v1/api-keys/{id}`

**Authorization**: `AdminOrMember` policy (Entra or Local bearer only).
Admin can revoke any key; Member can revoke only their own.

**Response `204 No Content`** on success (including already-revoked — idempotent).

**Error responses:**

| Status | Code | Condition |
|---|---|---|
| `403` | `Forbidden` | Member attempting to revoke another user's key |
| `404` | `NotFound` | Key ID does not exist |

#### `GET /api/v1/api-keys/all` (Admin only, Wave 2)

**Authorization**: `Admin` policy (Entra or Local bearer only)

Returns all keys across all owners, with owner display name. Used by the
Admin emergency-revocation UI. Same response shape as `GET /api/v1/api-keys`
with an additional `ownerDisplayName` field.

### 4.2 OpenAPI Security Scheme

Added to `openapi.yaml` `components/securitySchemes`:

```yaml
ApiKeyAuth:
  type: apiKey
  in: header
  name: Authorization
  description: >
    API key authentication. Send as: Authorization: ApiKey <selector>.<secret>.
    Keys are scoped to inventory operations only.
```

Endpoints that accept API key auth list both `BearerAuth` and `ApiKeyAuth`
in their `security` array. Key management endpoints list only `BearerAuth`.

### 4.3 Auth Handler Behavior

The `ForwardDefaultSelector` in `Program.cs` is extended:

```
if header starts with "ApiKey " → TechInventoryAuth.ApiKey
if header starts with "Bearer " and issuer is techinventory-local → TechInventoryAuth.Local
if header starts with "Bearer " → TechInventoryAuth.Entra
if BOTH "ApiKey " and "Bearer " present → reject 401 (ambiguous)
if no Authorization header → reject 401
```

The `TechInventoryAuth.ApiKey` handler:

1. Parses `<selector>.<secret>` from the header value.
2. Looks up the `ApiKey` row by selector.
3. Computes `HMAC-SHA-256(secret, pepper)` and compares to stored verifier
   with `CryptographicOperations.FixedTimeEquals`.
4. Checks: not revoked, not expired.
5. Loads the key's `Owner` and checks: `IsActive`, current `Role`.
6. Builds a `ClaimsPrincipal` with:
   - `sub` / `oid` = owner ID
   - `name` = owner display name
   - `ClaimTypes.Role` = owner's current role (live, not snapshot)
   - `apikey_selector` = selector (custom claim for audit/logging)
   - `apikey_scope` = the key's scope
7. Downstream authorization policies (`Admin`, `AdminOrMember`) work
   unchanged — they check `ClaimTypes.Role`.
8. An additional scope-enforcement middleware or policy requirement ensures
   API key requests only reach endpoints permitted by the key's scope.

### 4.4 ProblemDetails Error Shapes

All API key auth failures return:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The API key is invalid or has been revoked.",
  "extensions": { "code": "InvalidApiKey" }
}
```

Quota exceeded on creation:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "You have reached the maximum of 5 active API keys. Revoke an existing key before creating a new one.",
  "extensions": { "code": "QuotaExceeded" }
}
```

### 4.5 Audit and Redaction

| Event | AuditEvent Recorded | Serilog Structured Log |
|---|---|---|
| Key created | Yes — `Action=Created`, payload: `{ selector, name, scope, expiresAt }` | Yes |
| Key revoked | Yes — `Action=Deleted`, payload: `{ selector, revokedBy }` | Yes |
| Auth success | No (write amplification) | Yes — `ApiKeySelector`, `ApiKeyOwnerId`, `ApiKeyScope` |
| Auth failure | No | Yes — `ApiKeySelector` (if parseable), failure reason category |

**Redaction rules:**

* The raw secret (`<secret>` portion) never appears in any log, audit
  record, error response, or database column.
* The selector is the only key material that appears in logs and audit.
* The verifier hash is database-internal; it is never serialized in API
  responses or audit payloads.

## 5. Domain Model

### 5.1 `ApiKey` Entity

New entity in `src/TechInventory.Domain/Entities/ApiKey.cs`:

```
ApiKey : AggregateRoot
├── Name           : string (1–100)
├── Selector       : string (base64url, 24 chars — 16 bytes encoded)
├── VerifierHash   : string (base64, HMAC-SHA-256 output)
├── Scope          : ApiKeyScope enum { Read, Write }
├── OwnerId        : Guid (FK → Owner)
├── HouseholdId    : Guid (FK → Household)
├── ExpiresAt      : DateTimeOffset
├── RevokedAt      : DateTimeOffset? (null = active)
├── RevokedBy      : string? (actor who revoked)
├── CreatedAt      : DateTimeOffset (from AggregateRoot)
├── CreatedBy      : string (from AggregateRoot)
├── ModifiedAt     : DateTimeOffset (from AggregateRoot)
└── ModifiedBy     : string (from AggregateRoot)
```

Domain methods:

* `Revoke(string actor)` — sets `RevokedAt`, `RevokedBy`. Idempotent.
* `IsActive(DateTimeOffset asOf)` — `RevokedAt == null && ExpiresAt > asOf`.

### 5.2 `ApiKeyScope` Enum

New enum in `src/TechInventory.Domain/Enums/ApiKeyScope.cs`:

```csharp
public enum ApiKeyScope
{
    Read = 1,   // inventory.read
    Write = 2,  // inventory.write (implies read)
}
```

### 5.3 Database

New table `ApiKeys` via EF Core migration in
`src/TechInventory.Infrastructure/Persistence/Migrations/`.

Configuration in
`src/TechInventory.Infrastructure/Persistence/Configurations/ApiKeyConfiguration.cs`:

* Unique index on `Selector`.
* Index on `OwnerId` for list queries.
* Composite index on `OwnerId, RevokedAt` for active-key quota check.
* `VerifierHash` is `nvarchar(128)`, not indexed (lookup is by selector).

## 6. Negative and Tamper Acceptance Criteria

These test scenarios pin security invariants:

| # | Scenario | Expected |
|---|---|---|
| N-1 | Request with unknown selector | `401 InvalidApiKey` |
| N-2 | Request with correct selector, wrong secret | `401 InvalidApiKey` |
| N-3 | Request with expired key | `401 InvalidApiKey` |
| N-4 | Request with revoked key | `401 InvalidApiKey` |
| N-5 | Request where owner is deactivated | `401 InvalidApiKey` |
| N-6 | Request where owner's role was demoted to Viewer | `401 InvalidApiKey` |
| N-7 | `inventory.read` key attempting `POST /api/v1/devices` | `403 Forbidden` |
| N-8 | `inventory.write` key attempting `GET /api/v1/audit-events` | `403 Forbidden` |
| N-9 | `inventory.write` key attempting `POST /api/v1/imports` | `403 Forbidden` |
| N-10 | API key attempting `POST /api/v1/api-keys` (self-bootstrap) | `403 Forbidden` |
| N-11 | Request with both `Bearer` and `ApiKey` headers | `401 Unauthorized` |
| N-12 | `Authorization: ApiKey malformed-no-dot` | `401 InvalidApiKey` |
| N-13 | Viewer-authenticated `POST /api/v1/api-keys` | `403 Forbidden` |
| N-14 | Member revoking another member's key | `403 Forbidden` |
| N-15 | Creating 6th active key (quota) | `409 QuotaExceeded` |
| N-16 | Secret value appears in any log output | **Must not** — verified by log assertion |
| N-17 | Secret or verifier hash in `GET /api/v1/api-keys` response | **Must not** |
| N-18 | Timing difference between wrong-secret and unknown-selector | < 1ms variance (fixed-time) |

## 7. Rollout, Backward Compatibility, and Operations

### 7.1 Rollout

* **Wave 1** (API): schema migration, auth handler, endpoints, integration
  tests, operations docs. The feature is usable via `curl` / scripts.
* **Wave 2** (UI): generated TypeScript client update, Settings page,
  Admin revocation page, component tests, accessibility, i18n, manual PWA
  validation items added.
* No feature flag required — the `ApiKey` auth handler simply has no keys
  to match until an Admin/Member creates one.

### 7.2 Backward Compatibility

* Existing Entra and Local bearer tokens continue to work unchanged.
* The `PolicyScheme` `ForwardDefaultSelector` gains a new prefix check;
  existing `Bearer` routing is unaffected.
* No endpoint contract changes — new endpoints only.
* Database migration is additive (new table, no existing table changes).

### 7.3 Operations

Add to `docs/operations.md`:

* **Pepper management**: `ApiKeys:HmacPepper` must be ≥ 32 bytes, generated
  via `openssl rand -base64 48`. Changing it invalidates all outstanding keys.
  Document rotation as a break-glass procedure.
* **Emergency revocation**: Admin revokes via `DELETE /api/v1/api-keys/{id}`.
  Wave 2 adds the admin UI.
* **Quota tuning**: the 5-key limit and 90/365-day expiry are compile-time
  constants in the Domain layer. Changing them is a code change + migration
  if column constraints are tightened.

### 7.4 Rate Limiting

API key requests are rate-limited per selector at the middleware level.
Default: 60 requests/minute. Configurable via `RateLimiting:ApiKey:*`
configuration keys. Response: `429 Too Many Requests` with `Retry-After`
header.

## 8. Testing Strategy

Per constitution §7 and ADR 0002 (retired browser E2E):

| Layer | What | Where |
|---|---|---|
| Unit (xUnit) | `ApiKey` entity invariants, scope logic, HMAC verification, selector generation, quota check | `tests/TechInventory.UnitTests/Domain/ApiKeyTests.cs`, `tests/TechInventory.UnitTests/Application/ApiKey/` |
| Integration (WebApplicationFactory) | Full HTTP round-trip: create → use → revoke → fail; all N-1..N-18 negative cases; auth handler routing; audit assertions; rate limiting | `tests/TechInventory.IntegrationTests/ApiKeys/` |
| Contract | OpenAPI drift detection includes new `ApiKeyAuth` scheme and endpoints | `tests/TechInventory.IntegrationTests/Contract/` |
| Frontend unit (Vitest) | Settings page component (loading/empty/error/success), one-time secret display, copy-to-clipboard, revoke confirmation, Admin revocation page | `src/TechInventory.Web/src/routes/(authenticated)/settings/api-keys/*.test.ts` |
| Accessibility (axe-core) | Zero violations on Settings and Admin API key pages | Included in Vitest component tests |
| Manual PWA validation | API key Settings page renders on mobile, offline shell shows cached page | `docs/testing/manual-pwa-validation.md` — new M-items |

## 9. Sequencing

```
Wave 1 — API / Security / Contract / Tests / Ops
├── T-001  Domain: ApiKey entity + ApiKeyScope enum
├── T-002  Infrastructure: migration, configuration, repository
├── T-003  Application: Create/List/Revoke handlers + validators
├── T-004  Api: auth handler (TechInventoryAuth.ApiKey scheme)
├── T-005  Api: scope-enforcement middleware/policy
├── T-006  Api: ApiKeysController + rate limiting
├── T-007  Api: ambiguous-credential rejection
├── T-008  OpenAPI: ApiKeyAuth security scheme
├── T-009  Unit tests (domain + application)
├── T-010  Integration tests (all N-cases + happy paths)
├── T-011  Contract test update
├── T-012  docs/operations.md — API key admin section
├── T-013  Verify pipeline green (task verify)
│
Wave 2 — Generated Client / Settings UI / Admin UI / Manual Checks
├── T-014  Regenerate TypeScript client from updated openapi.yaml
├── T-015  Settings UI: /settings/api-keys page
├── T-016  Settings UI: one-time secret display component
├── T-017  Settings UI: component tests + axe
├── T-018  Admin UI: /admin/api-keys page
├── T-019  Admin UI: component tests + axe
├── T-020  i18n: en.json entries for all API key strings
├── T-021  Manual PWA validation: add M-items
├── T-022  Full verify pipeline green
│
Review / Verification
├── T-023  Security review: auth handler, HMAC, timing, redaction
├── T-024  Code review: architecture compliance
├── T-025  PR merge
```

## 10. References

Issue #149 cites no `R<N>` reference from `docs/references.md`. No
reference was triggered. The design draws from the project's own
authentication pipeline and OWASP API Security Top 10 (2023) guidance.

## 11. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 0.1 | 2026-09-03 | Ripley | Initial specification from approved product decisions |

# Spec 005 — API Key Support

**Status**: Specified (implementation not started) — revised after Apone QC rejection
**Phase**: P4 — Continuous Iteration
**Owner**: Ripley (Lead/Architect) → revised independently by Hicks (Backend) under reviewer lockout
**GitHub Issue**: [#149](https://github.com/briandenicola/tech-inventory/issues/149)
**ADR**: [`docs/adr/0003-api-key-authentication.md`](../../docs/adr/0003-api-key-authentication.md)
**Last Updated**: 2026-09-03

---

## 1. Problem

Family members want to drive device-inventory operations from CLI scripts, scheduled tasks, and Home Assistant integrations without opening a browser. Both existing schemes (Entra ID OIDC+PKCE, F025 local HS256) require interactive sign-in.

## 2. Scope

### In Scope

| Area | Detail |
|---|---|
| API key CRUD | Create, list, revoke under `/api/v1/api-keys` |
| Auth handler | Third forwarding target in the `TechInventoryAuth` PolicyScheme, plus an ambiguous-credential guard evaluated first (ADR) |
| Inventory access | Device CRUD + reference-data (brand, category, location, network, owner, tag) reads |
| Audit | `AuditEvent` on create/revoke; Serilog structured log per auth attempt |
| Settings UI (Wave 2) | Personal key management in authenticated settings |
| Admin UI (Wave 2) | Emergency revocation of any user's keys |
| OpenAPI | `ApiKeyAuth` security scheme in `openapi.yaml` |
| Operations | `docs/operations.md` section for pepper management and key administration |

### Out of Scope (Non-Goals) — adding any requires a new ADR

| Non-Goal | Rationale |
|---|---|
| Service principals / machine identities | Household app; human owners delegate via personal keys |
| OAuth client credentials flow | Over-engineered; adds an Entra app-registration dependency (ADR 0003) |
| Viewer-role key creation | Viewer is read-only, not expected to automate |
| Admin/audit/import/export/report/settings scopes | Keys are inventory-only; broader scopes need a scope-governance ADR |
| Fine-grained per-resource scopes | Two coarse scopes suffice for v1 |
| Rotate endpoint with grace period | Revoke-then-create with a 5-key quota is sufficient for a household |
| Non-expiring keys | Constitution §5 + ADR 0003: all credentials must expire |
| mTLS / IP allowlists as auth | Reverse-proxy concern (architecture.md §6.3), not application-layer auth |
| Bulk key management | Single household; ≤5 keys per creator |
| Usage dashboard / per-use `AuditEvent` | Write amplification on SQLite; Serilog logs suffice |
| Browser E2E tests | Retired (ADR 0002); integration/component/manual checks instead |

## 3. User Stories

| # | Actor | Story | Key acceptance (all must hold) |
|---|---|---|---|
| US-1 | Admin/Member | Create a named key with scope + optional expiry | `POST` → `201` + one-time `key` (`selector.secret`); `name` 1–100 unique/creator; `scope` ∈ {`inventory.read`,`inventory.write`}; `expiresInDays` default 90, range 1–365; 6th active key → `409 QuotaExceeded`; Viewer → `403`; `AuditEvent(Created)` records selector+scope+expiry, never the secret |
| US-2 | Admin/Member | List my keys (no secrets) | `GET` → paginated `id,name,selector,scope,createdAt,expiresAt,revokedAt,isActive`; secret/verifier hash never present; Viewer → `403` |
| US-3 | Admin/Member | Revoke my key | `DELETE` → `204`, sets `RevokedAt`; idempotent on already-revoked; must belong to caller (or Admin, US-4); `AuditEvent(Deleted)`; subsequent use of the key → `401` |
| US-4 | Admin | Revoke any user's key (emergency) | `DELETE` succeeds regardless of ownership; audit records Admin as actor, not the key owner; Wave-2 Admin UI shows all keys with owner filter |
| US-5 | Script/integration | Authenticate with `Authorization: ApiKey <selector>.<secret>` | `inventory.read` → `GET` on devices+reference data; `inventory.write` → adds `POST`/`PUT`/`DELETE` on devices; deactivated/demoted/expired/revoked principal or key → `401 InvalidApiKey` (no enumeration, ADR) |
| US-6 (Wave 2) | Admin/Member | Settings page to create/view/revoke keys | `/settings/api-keys`; one-time secret in a copy-to-clipboard field with a "won't see this again" warning; revoke requires confirmation (§6.5.4); i18n only; component test w/ loading/empty/error/success; axe-core zero violations |
| US-7 (Wave 2) | Admin | Admin panel to revoke any key | `/admin/api-keys`; owner-name filter; typed-name confirmation to revoke (§6.5.4); axe-core zero violations |

## 4. API Contract

All endpoints require authentication. Key-management endpoints (`/api/v1/api-keys*`) accept **bearer only** (Entra or Local) — API-key authentication is rejected there (N-10).

### 4.1 Endpoints

| Method & Path | AuthZ | Request | Success | Key errors |
|---|---|---|---|---|
| `POST /api/v1/api-keys` | `AdminOrMember` | `{name, scope, expiresInDays?}` — see US-1 constraints | `201` — see §4.2 | `400 ValidationError`; `403 Forbidden`; `409 QuotaExceeded` |
| `GET /api/v1/api-keys` | `AdminOrMember` | `page`≥1 (default 1), `pageSize` 1–200 (default 25) | `200` paginated list, never `key`/`secret`/`verifierHash` | `403 Forbidden` |
| `DELETE /api/v1/api-keys/{id}` | `AdminOrMember` (Admin: any key; Member: own only) | — | `204` (idempotent) | `403 Forbidden`; `404 NotFound` |
| `GET /api/v1/api-keys/all` (Wave 2) | `Admin` | `page`, `pageSize` | `200` list + `ownerDisplayName` per item | `403 Forbidden` |

### 4.2 One-Time Creation Response

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

`key` is returned **exactly once** and is never stored or retrievable again. List responses (§4.1) reuse the same item shape minus `key`, plus `revokedAt`/`isActive`.

### 4.3 OpenAPI Security Scheme

```yaml
ApiKeyAuth:
  type: apiKey
  in: header
  name: Authorization
  description: >
    API key authentication. Send as: Authorization: ApiKey <selector>.<secret>.
    Scoped to inventory operations only.
```

Inventory endpoints list both `BearerAuth` and `ApiKeyAuth` in `security`; key-management endpoints list `BearerAuth` only.

### 4.4 ProblemDetails Codes

All API key responses use RFC 7807 ProblemDetails, e.g.:

```json
{ "type": "https://tools.ietf.org/html/rfc7235#section-3.1", "title": "Unauthorized", "status": 401,
  "detail": "The API key is invalid or has been revoked.", "extensions": { "code": "InvalidApiKey" } }
```

| Code | Status | When |
|---|---|---|
| `InvalidApiKey` | 401 | Unknown selector, wrong secret, expired, revoked, inactive principal, insufficient live role — **never distinguished** (no enumeration) |
| `AmbiguousCredential` | 401 | Both `Bearer` and `ApiKey` present — checked **before** scheme routing (ADR) |
| `QuotaExceeded` | 409 | Creating a 6th active key |
| `ValidationError` | 400 | Invalid `name`/`scope`/`expiresInDays` |
| `Forbidden` | 403 | Viewer on create/list; API-key auth on key-management endpoints; Member revoking another's key; scope-insufficient inventory access |

### 4.5 Audit and Redaction

| Event | `AuditEvent` | Serilog |
|---|---|---|
| Key created | Yes — `Action=Created`, `{selector,name,scope,expiresAt}` | Yes |
| Key revoked | Yes — `Action=Deleted`, `{selector,revokedBy}` | Yes |
| Auth success | No (write-amplification non-goal) | Yes — `ApiKeySelector`,`ApiKeyPrincipalId`,`ApiKeyScope` |
| Auth failure | No | Yes — selector (if parseable), failure category |

The raw secret never appears in logs, audit, database, or API responses; the selector is the only key material that does; the verifier hash is database-internal only.

## 5. Domain Model

`ApiKey : AggregateRoot` — `Name`(1–100), `Selector`(base64url/16B), `VerifierHash`(base64/HMAC-SHA-256), `Scope`(`ApiKeyScope`: `Read=1`,`Write=2`), **`PrincipalType`**(`ApiKeyPrincipalType`: `Owner=1`,`LocalUser=2`) + **`PrincipalId`**(Guid) — replaces a bare `OwnerId` FK because `LocalUser` is a distinct, non-`Owner` authentication entity (ADR); `HouseholdId`(Guid, resolved via `IHouseholdRepository.ListAsync`, never from `Owner.HouseholdId` — it doesn't exist); `ExpiresAt`, `RevokedAt?`, `RevokedBy?`, plus the standard `CreatedAt/By`, `ModifiedAt/By`. Methods: `Revoke(actor)` (idempotent), `IsActive(asOf)` = `RevokedAt == null && ExpiresAt > asOf`. Table `ApiKeys`: unique index `Selector`; index `(PrincipalType, PrincipalId)`; composite `(PrincipalType, PrincipalId, RevokedAt)` for the quota check. See `plan.md` §1 for full layer placement.

## 6. Negative and Tamper Acceptance Criteria

| # | Scenario | Expected |
|---|---|---|
| N-1 | Unknown selector | `401 InvalidApiKey` |
| N-2 | Correct selector, wrong secret | `401 InvalidApiKey` |
| N-3 | Expired key | `401 InvalidApiKey` |
| N-4 | Revoked key | `401 InvalidApiKey` |
| N-5 | Principal deactivated (`IsActive=false`) | `401 InvalidApiKey` |
| N-6 | Principal's role demoted below required (e.g., to Viewer) | `401 InvalidApiKey` |
| N-7 | `inventory.read` key → `POST /api/v1/devices` | `403 Forbidden` |
| N-8 | `inventory.write` key → `GET /api/v1/audit-events` | `403 Forbidden` |
| N-9 | `inventory.write` key → `POST /api/v1/imports` | `403 Forbidden` |
| N-10 | API key → `POST /api/v1/api-keys` (self-bootstrap) | `403 Forbidden` |
| N-11 | Both `Bearer` and `ApiKey` headers present | `401 AmbiguousCredential` |
| N-12 | `Authorization: ApiKey malformed-no-dot` | `401 InvalidApiKey` |
| N-13 | Viewer-authenticated `POST /api/v1/api-keys` | `403 Forbidden` |
| N-14 | Member revoking another member's key | `403 Forbidden` |
| N-15 | Creating a 6th active key | `409 QuotaExceeded` |
| N-16 | Secret value appears in any log output | Must not — asserted via log-sink inspection |
| N-17 | Secret or verifier hash in a list response | Must not |
| N-18 | Unknown-selector vs. wrong-secret code path (Apone blocker) | Both execute the identical HMAC-compute + `FixedTimeEquals` call, dummy verifier on miss — asserted by call-count/code-path test + security review (T-023); **no wall-clock or `<1ms` assertion permitted** |
| N-19 | Existing Entra bearer request, post-routing-change (regression) | Authenticates/authorizes identically to pre-change behavior |
| N-20 | Existing Local break-glass bearer request, post-routing-change (regression) | Authenticates/authorizes identically to pre-change behavior |
| N-21 | Rate limit exceeded | `429`, using **test-scoped** `RateLimiting:ApiKey` config — never the 60/min production default in a test |

## 7. Rollout, Compatibility, and Operations

* **Wave 1 (API)**: migration, auth handler, endpoints, integration tests, ops docs — usable via `curl`/scripts. **Wave 2 (UI)**: generated client, Settings page, Admin page, component tests, i18n, manual PWA checklist items. No feature flag — the handler simply has no keys to match until one is created.
* **Compatibility**: Entra/Local bearer tokens unaffected (N-19/N-20); `ForwardDefaultSelector` gains one branch + one guard; no existing endpoint contracts change; migration is additive only.
* **Operations** (`docs/operations.md`, new "API Key Administration" section): pepper generation (`openssl rand -base64 48`, ≥32 decoded bytes) kept distinct from `Auth:Local:SigningKey`; rotation invalidates all outstanding keys (documented as a break-glass procedure); emergency revocation via `DELETE /api/v1/api-keys/{id}`; quota/expiry are Domain-layer constants — changing them is a code change; rate limiting is per-selector, configurable via `RateLimiting:ApiKey:*`, default 60/min → `429` with `Retry-After`.

## 8. Testing Strategy

| Layer | What | Where |
|---|---|---|
| Unit (xUnit) | Entity invariants, scope logic, HMAC verification incl. dummy-path assertion, selector/secret generation, quota | `tests/TechInventory.UnitTests/{Domain,Application}/ApiKey*` |
| Integration (WebApplicationFactory) | Full round-trip; all N-1..N-21; auth routing incl. N-19/N-20 regression; audit assertions; rate limiting via test-scoped config | `tests/TechInventory.IntegrationTests/ApiKeys/` |
| Contract | OpenAPI drift incl. `ApiKeyAuth` scheme + endpoints | `tests/TechInventory.IntegrationTests/Contract/` |
| Frontend unit (Vitest) | Settings/Admin pages, secret display, copy-to-clipboard, revoke confirmation | `src/TechInventory.Web/src/routes/(authenticated)/{settings,admin}/api-keys/*.test.ts` |
| Accessibility (axe-core) | Zero violations, Settings + Admin pages | Included in Vitest component tests |
| Manual PWA validation | Mobile rendering, offline cached Settings shell | `docs/testing/manual-pwa-validation.md` — new M-items |

Constitution §7.3: tests must be deterministic — no real time, randomness, or network without controls. This governs both the timing-assertion prohibition (N-18) and the rate-limit test-scoping requirement (N-21).

## 9. Sequencing

Two waves plus review — 25 tasks total, all `⬜ Pending`. See `tasks.md` for the full task-by-task breakdown with owners, dependencies, and authorized files; this spec does not duplicate that list.

## 10. References

Issue #149 triggers no `R<N>` reference from `docs/references.md`. Design draws from the project's own authentication pipeline and OWASP API Security Top 10 (2023) guidance.

## 11. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 0.1 | 2026-09-03 | Ripley | Initial specification from approved product decisions |
| 0.2 | 2026-09-03 | Hicks (independent revision, reviewer lockout) | Fixed Apone blockers: removed wall-clock/`<1ms` timing assertions (N-18 rewritten to deterministic dummy-path verification); added N-19/N-20 bearer-regression cases and N-21 test-scoped rate-limit case; replaced `Owner`-only identity with `PrincipalType`+`PrincipalId` and explicit `HouseholdId` resolution; unified `AmbiguousCredential` status/code; added pepper entropy/distinctness requirement; condensed doc set to satisfy constitution §8.2 (<500-line PR diff) |

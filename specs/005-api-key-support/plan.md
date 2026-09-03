# Plan 005 — API Key Support

**Spec**: [`specs/005-api-key-support/spec.md`](spec.md)
**ADR**: [`docs/adr/0003-api-key-authentication.md`](../../docs/adr/0003-api-key-authentication.md)
**Status**: Documentation complete; all implementation tasks pending
**Last Updated**: 2026-09-03

---

## 1. Clean Architecture Placement

Each component lands in the layer prescribed by the constitution (§2) and
the existing codebase structure. Dependencies point inward only.

### 1.1 Domain Layer (`src/TechInventory.Domain/`)

No framework dependencies. Pure C#.

| Artifact | Path | Purpose |
|---|---|---|
| `ApiKey` entity | `Entities/ApiKey.cs` | Aggregate root: name, selector, verifier hash, scope, owner FK, household FK, expiry, revocation state. Domain methods: `Revoke()`, `IsActive()`. |
| `ApiKeyScope` enum | `Enums/ApiKeyScope.cs` | `Read = 1`, `Write = 2`. |

**Dependencies**: `Domain.Primitives` (existing `AggregateRoot`, `Guard`).
No new NuGet packages.

### 1.2 Application Layer (`src/TechInventory.Application/`)

MediatR handlers, FluentValidation, repository interface.

| Artifact | Path | Purpose |
|---|---|---|
| `IApiKeyRepository` | `Abstractions/Repositories/IApiKeyRepository.cs` | `GetBySelectorAsync`, `GetByIdAsync`, `GetByOwnerAsync`, `CountActiveByOwnerAsync`, `AddAsync` |
| `IApiKeyHasher` | `Abstractions/Services/IApiKeyHasher.cs` | `ComputeVerifier(byte[] secret) → string`, `VerifyConstantTime(byte[] secret, string storedHash) → bool` |
| `CreateApiKeyCommand` | `Features/ApiKeys/CreateApiKey.cs` | Handler: validate quota, generate selector + secret (CSPRNG), compute HMAC verifier, persist, return one-time key. Implements `IAuditable`. |
| `ListApiKeysQuery` | `Features/ApiKeys/ListApiKeys.cs` | Handler: paginated list by owner. Never returns secret/hash. |
| `RevokeApiKeyCommand` | `Features/ApiKeys/RevokeApiKey.cs` | Handler: ownership check (or Admin bypass), call `entity.Revoke()`. Implements `IAuditable`. |
| `CreateApiKeyValidator` | `Features/ApiKeys/CreateApiKeyValidator.cs` | FluentValidation: name 1–100, scope enum, expiresInDays 1–365. |
| `RevokeApiKeyValidator` | `Features/ApiKeys/RevokeApiKeyValidator.cs` | FluentValidation: id required. |

**Dependencies**: `Domain`, `MediatR`, `FluentValidation`.
The existing `AuditBehavior` (pipeline behavior) handles audit automatically
for handlers implementing `IAuditable`.

### 1.3 Infrastructure Layer (`src/TechInventory.Infrastructure/`)

EF Core persistence, HMAC hasher implementation.

| Artifact | Path | Purpose |
|---|---|---|
| `ApiKeyConfiguration` | `Persistence/Configurations/ApiKeyConfiguration.cs` | EF Core entity config: table `ApiKeys`, unique index on `Selector`, index on `OwnerId`, composite on `(OwnerId, RevokedAt)`. |
| `ApiKeyRepository` | `Persistence/Repositories/ApiKeyRepository.cs` | Implements `IApiKeyRepository` via `AppDbContext`. |
| Migration | `Persistence/Migrations/<timestamp>_AddApiKeys.cs` | Creates `ApiKeys` table. Additive — no existing table changes. |
| `HmacApiKeyHasher` | `Services/HmacApiKeyHasher.cs` | Implements `IApiKeyHasher`. Uses `HMACSHA256` with pepper from `IOptions<ApiKeyOptions>`. Fixed-time comparison via `CryptographicOperations.FixedTimeEquals`. |
| `ApiKeyOptions` | `Services/ApiKeyOptions.cs` | Configuration POCO bound to `ApiKeys:HmacPepper`. |

**Dependencies**: `Application`, `Microsoft.EntityFrameworkCore`, `System.Security.Cryptography`.
No new NuGet packages.

### 1.4 Api Layer (`src/TechInventory.Api/`)

Auth handler, controller, middleware, rate limiting.

| Artifact | Path | Purpose |
|---|---|---|
| `ApiAuthenticationSchemes` update | `Authentication/ApiAuthenticationSchemes.cs` | Add `ApiKeyScheme = "TechInventoryAuth.ApiKey"`. |
| `ApiKeyAuthHandler` | `Authentication/ApiKeyAuthHandler.cs` | Custom `AuthenticationHandler<>`: parse header, lookup by selector, HMAC verify, check expiry/revocation/owner status/role, build `ClaimsPrincipal`. |
| `ApiKeyScopeRequirement` | `Authentication/ApiKeyScopeRequirement.cs` | `IAuthorizationRequirement` + handler that checks the `apikey_scope` claim against the endpoint's required scope. |
| `PolicyScheme update` | `Program.cs` | Extend `ForwardDefaultSelector`: `ApiKey ` prefix → ApiKey scheme; ambiguous dual-header → reject. |
| `ApiKeysController` | `Controllers/ApiKeysController.cs` | Thin controller: `POST`, `GET`, `DELETE` + Admin `GET /all`. MediatR dispatch. `[Authorize(Policy = "AdminOrMember")]`, bearer-only (rejects API key auth on these endpoints). |
| Rate limiting | `Program.cs` or dedicated middleware | Per-selector rate limiter: 60 req/min default, `429 Too Many Requests`. |

**Dependencies**: `Application`, `Microsoft.AspNetCore.Authentication`.
No new NuGet packages beyond what ASP.NET Core provides.

### 1.5 Web Layer (`src/TechInventory.Web/`) — Wave 2

| Artifact | Path | Purpose |
|---|---|---|
| Generated client update | `src/lib/api/` | Regenerated from updated `openapi.yaml` — includes `ApiKeysService`. |
| Settings page | `src/routes/(authenticated)/settings/api-keys/+page.svelte` | List + create + revoke personal keys. |
| One-time secret component | `src/lib/components/ApiKeySecretDisplay.svelte` | Copy-to-clipboard, "you won't see this again" warning. |
| Admin page | `src/routes/(authenticated)/admin/api-keys/+page.svelte` | All-keys list with owner filter, emergency revoke. |
| i18n entries | `src/lib/i18n/en.json` | All API key UI strings. |
| Component tests | Adjacent `*.test.ts` files | Loading/empty/error/success states, axe-core zero violations. |

## 2. Dependency Ordering

```
T-001  Domain entity + enum                         (no deps)
  └─► T-002  Infrastructure migration + config + repo  (needs T-001)
        └─► T-003  Application handlers + validators    (needs T-001, T-002 interface)
              └─► T-004  Api auth handler                (needs T-003, T-002 for lookup)
                    ├─► T-005  Scope-enforcement middleware (needs T-004 claims)
                    ├─► T-006  Controller + rate limiting   (needs T-003, T-004)
                    └─► T-007  Ambiguous-credential logic   (needs T-004)
                          └─► T-008  OpenAPI update          (needs T-006 routes)
                                └─► T-009  Unit tests         (needs T-001..T-003)
                                      └─► T-010  Integration tests (needs T-004..T-008)
                                            └─► T-011  Contract tests  (needs T-008)
                                                  └─► T-012  Ops docs     (needs T-004)
                                                        └─► T-013  Verify pipeline

T-013 ──► T-014  Regenerate TS client  (needs T-008 + Wave 1 merged)
            └─► T-015..T-021  UI + i18n + manual checks
                  └─► T-022  Full verify
                        └─► T-023..T-025  Review + merge
```

## 3. Key Integration Points

### 3.1 Authentication Pipeline

The `ForwardDefaultSelector` lambda in `Program.cs` (currently lines ~73–97)
is the routing hub. The change adds one `if` branch for the `ApiKey ` prefix
and one guard for ambiguous dual-header requests. The existing Entra/Local
branches are untouched.

Seam: `src/TechInventory.Api/Program.cs:73–97` and
`src/TechInventory.Api/Authentication/ApiAuthenticationSchemes.cs`.

### 3.2 Authorization Policies

Existing policies (`Admin`, `AdminOrMember`, fallback `RequireAuthenticatedUser`)
work unchanged because the API key handler produces a `ClaimsPrincipal` with
`ClaimTypes.Role` matching the owner's live role. The scope enforcement is
additive — a new `IAuthorizationRequirement` that runs only when the
`apikey_scope` claim is present.

Seam: `src/TechInventory.Api/Program.cs:250–260` (authorization builder).

### 3.3 Current User Service

`HttpContextCurrentUserService` resolves identity from claims (`oid`, `sub`,
`name`, `ClaimTypes.Role`). The API key handler populates the same claims,
so audit stamping (`CreatedBy`, `ModifiedBy`) works without changes.

Seam: `src/TechInventory.Api/Authentication/HttpContextCurrentUserService.cs`.

### 3.4 Audit Pipeline

`AuditBehavior` fires for any handler implementing `IAuditable`. The
create/revoke handlers will implement `IAuditable`, so audit events are
recorded automatically. The audit payload serializer must be extended to
redact any field named `secret` or `verifierHash`.

Seam: `src/TechInventory.Application/Behaviors/AuditBehavior.cs`,
`src/TechInventory.Application/Auditing/IAuditable.cs`.

### 3.5 Owner Lookup

The auth handler must load the key's `Owner` at request time to check
`IsActive` and `Role`. This is a read via `IOwnerRepository.GetByIdAsync`.

Seam: `src/TechInventory.Application/Abstractions/Repositories/IOwnerRepository.cs`,
`src/TechInventory.Domain/Entities/Owner.cs`.

## 4. Configuration

| Key | Layer | Default | Secret? |
|---|---|---|---|
| `ApiKeys:HmacPepper` | Infrastructure (injected) | none — required | **Yes** — Docker secret or env var |
| `RateLimiting:ApiKey:PermitLimit` | Api | `60` | No |
| `RateLimiting:ApiKey:WindowSeconds` | Api | `60` | No |

The pepper has the same operational profile as `Auth:Local:SigningKey` —
it must be present for the API key feature to function. If absent, the
`HmacApiKeyHasher` throws at startup (fail-fast).

## 5. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Pepper leak allows offline key forging | Low | High | Pepper stored as Docker secret, never in env files committed to git. Pepper rotation documented in operations. |
| SQLite write amplification from per-use AuditEvent | — | — | Explicitly out of scope (spec §2 non-goals). Serilog structured log instead. |
| API key used to bootstrap more keys | Low | Medium | Key management endpoints reject API key auth (bearer-only). N-10 negative test. |
| Owner deactivated but keys still in use | Medium | Low | Live owner status check on every request. |
| Rate limiting bypass via many keys | Low | Low | 5-key quota per owner; single household. |

## 6. Status

| Phase | Status |
|---|---|
| ADR 0003 | ✅ Accepted |
| Spec 005 | ✅ Specified |
| Plan 005 | ✅ Planned |
| Tasks 005 | ✅ Defined (all pending) |
| Wave 1 implementation | ⬜ Not started |
| Wave 2 implementation | ⬜ Not started |

## 7. References

Issue #149 cites no `R<N>` reference. No reference was triggered.

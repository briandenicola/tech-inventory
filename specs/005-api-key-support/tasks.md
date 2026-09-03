# Tasks 005 — API Key Support

**Spec**: [`spec.md`](spec.md) | **Plan**: [`plan.md`](plan.md) | **ADR**: [`docs/adr/0003-api-key-authentication.md`](../../docs/adr/0003-api-key-authentication.md)
**Issue**: [#149](https://github.com/briandenicola/tech-inventory/issues/149)
**Last Updated**: 2026-09-03

---

## Wave 1 — API / Security / Contract / Tests / Ops

### T-001: Domain — ApiKey Entity and ApiKeyScope Enum

**Status**: ⬜ Pending
**Owner**: Implementer (Hicks or equivalent)
**Depends on**: —
**Authorized files**:
- `src/TechInventory.Domain/Entities/ApiKey.cs` (create)
- `src/TechInventory.Domain/Enums/ApiKeyScope.cs` (create)

**Description**:
Create the `ApiKey` aggregate root entity with properties: `Name`, `Selector`,
`VerifierHash`, `Scope` (`ApiKeyScope`), `OwnerId` (Guid), `HouseholdId` (Guid),
`ExpiresAt`, `RevokedAt`, `RevokedBy`. Domain methods: `Revoke(string actor)` —
idempotent soft revocation; `IsActive(DateTimeOffset asOf)` — true when not
revoked and not expired. Create the `ApiKeyScope` enum with `Read = 1`,
`Write = 2`.

**Acceptance check**:
- `ApiKey` extends `AggregateRoot` with zero framework dependencies
- `Revoke()` is idempotent (calling twice does not throw)
- `IsActive()` returns false for expired and revoked keys
- `Guard` validations on name (1–100), selector, verifier hash
- File compiles with `dotnet build src/TechInventory.Domain`

---

### T-002: Infrastructure — Migration, Configuration, Repository

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-001
**Authorized files**:
- `src/TechInventory.Infrastructure/Persistence/Configurations/ApiKeyConfiguration.cs` (create)
- `src/TechInventory.Infrastructure/Persistence/Repositories/ApiKeyRepository.cs` (create)
- `src/TechInventory.Infrastructure/Persistence/Migrations/*_AddApiKeys.cs` (create via `dotnet ef`)
- `src/TechInventory.Infrastructure/Persistence/AppDbContext.cs` (add `DbSet<ApiKey>`)
- `src/TechInventory.Application/Abstractions/Repositories/IApiKeyRepository.cs` (create)

**Description**:
Create EF Core configuration: table `ApiKeys`, unique index on `Selector`,
index on `OwnerId`, composite index on `(OwnerId, RevokedAt)`. Implement
`IApiKeyRepository` with methods: `GetBySelectorAsync`, `GetByIdAsync`,
`GetByOwnerAsync` (paginated), `CountActiveByOwnerAsync`, `AddAsync`.
Add migration via `dotnet ef migrations add AddApiKeys`.

**Acceptance check**:
- Migration applies cleanly to empty and existing databases
- Unique constraint on Selector prevents duplicates
- Repository methods are async with CancellationToken
- No raw SQL — all via EF Core LINQ
- `dotnet build src/TechInventory.Infrastructure` succeeds

---

### T-003: Application — Create/List/Revoke Handlers and Validators

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-001, T-002
**Authorized files**:
- `src/TechInventory.Application/Features/ApiKeys/CreateApiKey.cs` (create)
- `src/TechInventory.Application/Features/ApiKeys/ListApiKeys.cs` (create)
- `src/TechInventory.Application/Features/ApiKeys/RevokeApiKey.cs` (create)
- `src/TechInventory.Application/Features/ApiKeys/CreateApiKeyValidator.cs` (create)
- `src/TechInventory.Application/Features/ApiKeys/RevokeApiKeyValidator.cs` (create)
- `src/TechInventory.Application/Abstractions/Services/IApiKeyHasher.cs` (create)

**Description**:
`CreateApiKeyCommand`: validate quota (≤ 5 active), generate 16-byte selector +
32-byte secret via `RandomNumberGenerator`, compute HMAC verifier via
`IApiKeyHasher`, persist via repository, return one-time key string. Implements
`IAuditable`. `ListApiKeysQuery`: paginated list by owner, never returns
secret/hash. `RevokeApiKeyCommand`: ownership check (caller = owner OR Admin),
call `entity.Revoke(actor)`, implements `IAuditable`.

**Acceptance check**:
- CreateApiKey returns `Result<CreateApiKeyResponse>` with `key` field
- Quota check returns `Result.Failure` with appropriate error when > 5
- Secret is 32 bytes of CSPRNG, base64url-encoded
- Selector is 16 bytes of CSPRNG, base64url-encoded
- Revoke is idempotent
- All validators use FluentValidation; name 1–100, scope valid enum, expiresInDays 1–365

---

### T-004: Api — API Key Authentication Handler

**Status**: ⬜ Pending
**Owner**: Implementer (security-critical — requires security review T-023)
**Depends on**: T-002, T-003
**Authorized files**:
- `src/TechInventory.Api/Authentication/ApiKeyAuthHandler.cs` (create)
- `src/TechInventory.Api/Authentication/ApiAuthenticationSchemes.cs` (update — add `ApiKeyScheme`)
- `src/TechInventory.Api/Program.cs` (update — register scheme, extend ForwardDefaultSelector)
- `src/TechInventory.Infrastructure/Services/HmacApiKeyHasher.cs` (create)
- `src/TechInventory.Infrastructure/Services/ApiKeyOptions.cs` (create)

**Description**:
Implement `ApiKeyAuthHandler` as a custom `AuthenticationHandler<AuthenticationSchemeOptions>`.
Parse `Authorization: ApiKey <selector>.<secret>`. Lookup by selector,
HMAC-verify with `CryptographicOperations.FixedTimeEquals`, check expiry,
revocation, owner active status, owner role. Build `ClaimsPrincipal` with
`sub`/`oid` = owner ID, `name` = owner display name, `ClaimTypes.Role` = live
owner role, `apikey_selector`, `apikey_scope` custom claims. Register as
`TechInventoryAuth.ApiKey` scheme. Extend `ForwardDefaultSelector` with
`ApiKey ` prefix routing. Implement `HmacApiKeyHasher` with pepper from
`IOptions<ApiKeyOptions>`.

**Acceptance check**:
- Handler produces `ClaimsPrincipal` compatible with existing policies
- Fixed-time comparison used for HMAC verification
- Pepper is required at startup (fail-fast if missing)
- Serilog structured log on every auth attempt (success and failure) with selector, never secret
- All failure modes return uniform `401 InvalidApiKey`

---

### T-005: Api — Scope Enforcement Middleware/Policy

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-004
**Authorized files**:
- `src/TechInventory.Api/Authentication/ApiKeyScopeRequirement.cs` (create)
- `src/TechInventory.Api/Program.cs` (update — register requirement handler)

**Description**:
Create `IAuthorizationRequirement` + `AuthorizationHandler` that checks the
`apikey_scope` claim. If the claim is present (i.e., the request is API-key-
authenticated), verify the scope permits access to the current endpoint.
`inventory.read` permits GET on device + reference-data endpoints.
`inventory.write` permits GET + POST + PUT + DELETE on device endpoints plus
GET on reference-data. Both scopes deny access to admin, audit, import, export,
report, settings, and auth endpoints. If the claim is absent (bearer auth),
the requirement is satisfied (no scope restriction for interactive users).

**Acceptance check**:
- `inventory.read` key can GET devices but not POST
- `inventory.write` key can POST devices but not GET audit events
- Bearer-authenticated users are unaffected
- Requirement is a no-op when no `apikey_scope` claim exists

---

### T-006: Api — ApiKeysController and Rate Limiting

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-003, T-004
**Authorized files**:
- `src/TechInventory.Api/Controllers/ApiKeysController.cs` (create)
- `src/TechInventory.Api/Program.cs` (update — rate limiting configuration)

**Description**:
Thin controller with `POST /api/v1/api-keys`, `GET /api/v1/api-keys`,
`DELETE /api/v1/api-keys/{id}`, `GET /api/v1/api-keys/all` (Admin-only).
All endpoints require `AdminOrMember` policy (except `/all` which requires
`Admin`) and reject API key authentication (bearer-only). Configure per-selector
rate limiting: 60 req/min default, `429 Too Many Requests` with `Retry-After`.

**Acceptance check**:
- Controller dispatches to MediatR handlers, contains no business logic
- All endpoints return ProblemDetails on error
- API key auth is rejected on key management endpoints (N-10)
- Rate limiter returns 429 when limit exceeded

---

### T-007: Api — Ambiguous Credential Rejection

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-004
**Authorized files**:
- `src/TechInventory.Api/Program.cs` (update — ForwardDefaultSelector guard)

**Description**:
In the `ForwardDefaultSelector` lambda, if the `Authorization` header contains
both `ApiKey` and `Bearer` tokens (via multiple headers or comma-separated
values), immediately return a scheme that produces a `401 Unauthorized` response.
No fallthrough to any handler.

**Acceptance check**:
- Request with both `Authorization: Bearer ...` and `Authorization: ApiKey ...` → 401
- Single valid header continues to work for all three schemes
- N-11 negative test passes

---

### T-008: OpenAPI — ApiKeyAuth Security Scheme

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-006
**Authorized files**:
- `openapi.yaml` (update — add security scheme and endpoint definitions)

**Description**:
Add `ApiKeyAuth` to `components/securitySchemes`. Add API key endpoint
operations to `paths`. Tag inventory endpoints with both `BearerAuth` and
`ApiKeyAuth` in their `security` arrays. Tag key management endpoints with
`BearerAuth` only.

**Acceptance check**:
- `openapi.yaml` validates with an OpenAPI linter
- New endpoints documented with request/response schemas
- Contract tests will verify at T-011

---

### T-009: Unit Tests — Domain and Application

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-001, T-003
**Authorized files**:
- `tests/TechInventory.UnitTests/Domain/ApiKeyTests.cs` (create)
- `tests/TechInventory.UnitTests/Application/ApiKeys/*.cs` (create)

**Description**:
Unit tests for `ApiKey` entity (construction, `Revoke()`, `IsActive()`, Guard
validations), `ApiKeyScope` enum values, HMAC hasher (round-trip, wrong secret,
fixed-time assertion), `CreateApiKeyValidator` (name, scope, expiry bounds),
quota logic.

**Acceptance check**:
- All entity invariants pinned
- HMAC round-trip verified
- Invalid inputs rejected by validators
- `dotnet test tests/TechInventory.UnitTests --filter ApiKey` green

---

### T-010: Integration Tests — All Negative Cases and Happy Paths

**Status**: ⬜ Pending
**Owner**: Implementer (security-critical — requires security review T-023)
**Depends on**: T-004, T-005, T-006, T-007, T-008
**Authorized files**:
- `tests/TechInventory.IntegrationTests/ApiKeys/*.cs` (create)

**Description**:
Full HTTP round-trip integration tests via `WebApplicationFactory`:
1. Happy path: create key → use key to GET devices → revoke key → confirm 401.
2. All N-1 through N-18 negative/tamper scenarios from spec §6.
3. Quota enforcement (create 5, fail on 6th).
4. Admin cross-revocation (US-4).
5. Audit event assertions for create/revoke.
6. Rate limiting trigger (429 response).
7. Serilog log output assertion: selector present, secret absent.

**Acceptance check**:
- All 18 negative cases pass
- Audit events contain selector, never secret or verifier hash
- `dotnet test tests/TechInventory.IntegrationTests --filter ApiKey` green

---

### T-011: Contract Tests — OpenAPI Drift Detection Update

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-008
**Authorized files**:
- `tests/TechInventory.IntegrationTests/Contract/OpenApiDriftTests.cs` (update if needed)

**Description**:
Verify the `ApiKeyAuth` security scheme and new API key endpoints are present
in the runtime-generated OpenAPI spec and match `openapi.yaml`.

**Acceptance check**:
- Contract drift test passes with new endpoints included
- `dotnet test tests/TechInventory.IntegrationTests --filter OpenApi` green

---

### T-012: Operations Documentation — API Key Admin Section

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-004
**Authorized files**:
- `docs/operations.md` (update — add "API Key Administration" section)

**Description**:
Add operational documentation covering:
- Pepper generation (`openssl rand -base64 48`), storage, and rotation procedure
- Emergency revocation via API (`curl` example)
- Quota and expiry constants
- Monitoring: Serilog structured properties to alert on
- Pepper rotation impact (invalidates all keys)

**Acceptance check**:
- Section is consistent with ADR 0003 and spec 005
- Pepper generation command produces ≥ 32 bytes

---

### T-013: Verify Pipeline Green

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-009, T-010, T-011, T-012
**Authorized files**: none (verification only)

**Description**:
Run `task verify` end-to-end. All format checks, builds, tests, vulnerability
scans must pass.

**Acceptance check**:
- `task verify` exits 0
- No new `dotnet list package --vulnerable` findings
- Test-collection floors still met

---

## Wave 2 — Generated Client / Settings UI / Admin UI / Manual Checks

### T-014: Regenerate TypeScript Client

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-008 (Wave 1 merged)
**Authorized files**:
- `src/TechInventory.Web/src/lib/api/**` (regenerated)

**Description**:
Regenerate the TypeScript API client from the updated `openapi.yaml`. Verify
the `ApiKeysService` is generated with `create`, `list`, `revoke`, and
`listAll` methods.

**Acceptance check**:
- Generated client compiles with `pnpm run check`
- `ApiKeysService` methods match the spec contract

---

### T-015: Settings UI — /settings/api-keys Page

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-014
**Authorized files**:
- `src/TechInventory.Web/src/routes/(authenticated)/settings/api-keys/+page.svelte` (create)
- `src/TechInventory.Web/src/routes/(authenticated)/settings/api-keys/+page.ts` (create)

**Description**:
Settings page to list, create, and revoke the user's personal API keys.
Uses the generated client. Supports loading/empty/error/success states per
constitution §6.5.4. Revoke requires confirmation (destructive action).

**Acceptance check**:
- Page renders at `/settings/api-keys`
- All four UI states implemented
- Revoke confirmation dialog before deletion
- Component < 200 lines; single-purpose

---

### T-016: One-Time Secret Display Component

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-015
**Authorized files**:
- `src/TechInventory.Web/src/lib/components/ApiKeySecretDisplay.svelte` (create)

**Description**:
Reusable component shown after key creation. Displays the full key in a
read-only input with copy-to-clipboard button and a prominent "You will not
see this again" warning. After dismissal, the secret is cleared from memory.

**Acceptance check**:
- Copy-to-clipboard works
- Warning text from i18n catalog
- Secret cleared from component state on dismiss
- Accessible: label, focus management

---

### T-017: Settings UI — Component Tests and Accessibility

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-015, T-016
**Authorized files**:
- `src/TechInventory.Web/src/routes/(authenticated)/settings/api-keys/*.test.ts` (create)
- `src/TechInventory.Web/src/lib/components/ApiKeySecretDisplay.test.ts` (create)

**Description**:
Vitest + Testing Library tests for the Settings page and secret display
component. Each state (loading, empty, error, success) tested. axe-core
assertion: zero violations.

**Acceptance check**:
- All component states covered
- axe-core: zero serious/critical violations
- `pnpm test -- --run api-keys` green

---

### T-018: Admin UI — /admin/api-keys Page

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-014
**Authorized files**:
- `src/TechInventory.Web/src/routes/(authenticated)/admin/api-keys/+page.svelte` (create)
- `src/TechInventory.Web/src/routes/(authenticated)/admin/api-keys/+page.ts` (create)

**Description**:
Admin page showing all API keys across all users. Owner name filter.
Emergency revoke with typed-name confirmation (destructive action per
constitution §6.5.4). Admin-only route guard.

**Acceptance check**:
- Page renders at `/admin/api-keys`
- Owner name filter works
- Typed-name confirmation for revocation
- Non-Admin users redirected or shown 403

---

### T-019: Admin UI — Component Tests and Accessibility

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-018
**Authorized files**:
- `src/TechInventory.Web/src/routes/(authenticated)/admin/api-keys/*.test.ts` (create)

**Description**:
Vitest + Testing Library tests for the Admin page. States: loading, empty,
filtered, error. axe-core: zero violations.

**Acceptance check**:
- All states covered
- axe-core: zero serious/critical violations
- Revoke confirmation tested

---

### T-020: i18n — en.json Entries for API Key Strings

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-015, T-018
**Authorized files**:
- `src/TechInventory.Web/src/lib/i18n/en.json` (update)

**Description**:
Add all user-facing strings for API key management: page titles, labels,
confirmations, warnings (including "you will not see this again"), error
messages, empty states. No hard-coded strings in components per constitution
§6.5.12.

**Acceptance check**:
- All API key UI strings in `en.json`
- No hard-coded strings in `*.svelte` files for API key pages
- `pnpm run check` passes

---

### T-021: Manual PWA Validation — Add M-Items

**Status**: ⬜ Pending
**Owner**: Implementer (Vasquez or equivalent)
**Depends on**: T-015, T-018
**Authorized files**:
- `docs/testing/manual-pwa-validation.md` (update)

**Description**:
Add manual validation items for:
- Settings API keys page renders correctly on mobile (360px)
- Offline shell shows cached Settings page
- Admin API keys page renders correctly on mobile
- Copy-to-clipboard works on iOS Safari and Android Chrome

**Acceptance check**:
- New M-items numbered and added to checklist
- Owner assigned
- Items not reported as automated coverage

---

### T-022: Full Verify Pipeline Green (Wave 2)

**Status**: ⬜ Pending
**Owner**: Implementer
**Depends on**: T-017, T-019, T-020, T-021
**Authorized files**: none (verification only)

**Description**:
Run `task verify` end-to-end after all Wave 2 changes. Backend + frontend
builds, all tests, lint, format, type-check, vulnerability scan.

**Acceptance check**:
- `task verify` exits 0
- `pnpm run check` and `pnpm run lint` clean
- No new `npm audit` moderate+ findings

---

## Review / Verification

### T-023: Security Review — Auth Handler, HMAC, Timing, Redaction

**Status**: ⬜ Pending
**Owner**: Security reviewer (Bishop or equivalent)
**Depends on**: T-004, T-010
**Authorized files**: none (review only)

**Description**:
Security-focused review of:
- `ApiKeyAuthHandler` — constant-time comparison, no timing side-channels
- HMAC pepper handling — no accidental logging, fail-fast on missing pepper
- Failure uniformity — all error paths return identical `401 InvalidApiKey`
- Redaction — secret never appears in logs, audit, responses
- Ambiguous credential handling — dual-header rejection
- Rate limiting — per-selector, not bypassable by header tricks
- Owner live ceiling — role/status re-checked on each request

**Acceptance check**:
- Reviewer signs off on all items
- No findings of severity ≥ Medium

---

### T-024: Code Review — Architecture Compliance

**Status**: ⬜ Pending
**Owner**: Lead reviewer (Ripley or equivalent)
**Depends on**: T-013, T-022
**Authorized files**: none (review only)

**Description**:
Review for:
- Clean Architecture: no outward dependency violations
- Thin controller: no business logic in controller
- Domain: no framework dependencies
- MediatR pipeline used for non-trivial operations
- FluentValidation for all inputs
- Audit events via existing `AuditBehavior` pipeline
- Naming conventions, file placement, code style

**Acceptance check**:
- All layers correctly placed
- No `using Microsoft.EntityFrameworkCore` in Domain or controllers
- PR description references spec sections

---

### T-025: PR Merge

**Status**: ⬜ Pending
**Owner**: Brian (human approver)
**Depends on**: T-023, T-024
**Authorized files**: none

**Description**:
Merge the implementation PR(s) after passing security and architecture review.
Commit message references `#149`. Issue #149 closed by implementation PR, not
by this spec PR.

**Acceptance check**:
- All CI checks green
- Security review (T-023) approved
- Architecture review (T-024) approved
- `task verify` green on merge commit

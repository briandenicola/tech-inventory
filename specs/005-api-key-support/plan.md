# Plan 005 — API Key Support

**Spec**: [`spec.md`](spec.md) | **ADR**: [`../../docs/adr/0003-api-key-authentication.md`](../../docs/adr/0003-api-key-authentication.md)
**Status**: Documentation complete; all implementation tasks pending
**Revision**: v2 — Hicks (independent revision, reviewer lockout), 2026-09-03. Corrects Apone's blockers: principal-aware identity model (`PrincipalType`+`PrincipalId`, not a bare `Owner` FK), explicit `HouseholdId` resolution via the existing single-household pattern, pepper entropy/distinctness validation, deterministic timing/rate-limit test seams, and doc-set condensation for constitution §8.2.

---

## 1. Clean Architecture Placement

Every artifact lands in the layer prescribed by constitution §2; dependencies point inward only.

| Layer | Artifact | Path | Purpose |
|---|---|---|---|
| Domain | `ApiKey` entity | `src/TechInventory.Domain/Entities/ApiKey.cs` | Aggregate root — fields per spec §5; `Revoke(actor)`, `IsActive(asOf)`. Zero framework deps. |
| Domain | `ApiKeyScope` enum | `src/TechInventory.Domain/Enums/ApiKeyScope.cs` | `Read=1, Write=2`. |
| Domain | `ApiKeyPrincipalType` enum | `src/TechInventory.Domain/Enums/ApiKeyPrincipalType.cs` | `Owner=1, LocalUser=2` — discriminator resolved from the authenticated creator at creation time (ADR). |
| Application | `IApiKeyRepository` | `src/TechInventory.Application/Abstractions/Repositories/IApiKeyRepository.cs` | `GetBySelectorAsync`, `GetByIdAsync`, `GetByPrincipalAsync` (paginated), `CountActiveByPrincipalAsync`, `AddAsync`. |
| Application | `IApiKeyHasher` | `src/TechInventory.Application/Abstractions/Services/IApiKeyHasher.cs` | `ComputeVerifier`, `VerifyConstantTime` (real path) **and** `VerifyDummyConstantTime` (unknown-selector path — identical cost, no real compare; ADR). |
| Application | `CreateApiKeyCommand` + validator | `src/TechInventory.Application/Features/ApiKeys/{CreateApiKey,CreateApiKeyValidator}.cs` | Resolves caller `(PrincipalType, PrincipalId)` from `ICurrentUserService`/claims; resolves `HouseholdId` via `IHouseholdRepository.ListAsync` exactly-one (§3); quota ≤5 active; CSPRNG selector/secret; HMAC verifier; `IAuditable`. |
| Application | `ListApiKeysQuery` | `src/TechInventory.Application/Features/ApiKeys/ListApiKeys.cs` | Paginated by caller; never returns secret/hash. |
| Application | `RevokeApiKeyCommand` + validator | `src/TechInventory.Application/Features/ApiKeys/{RevokeApiKey,RevokeApiKeyValidator}.cs` | Caller = owning principal OR Admin; idempotent `Revoke(actor)`; `IAuditable`. |
| Infrastructure | `ApiKeyConfiguration` | `src/TechInventory.Infrastructure/Persistence/Configurations/ApiKeyConfiguration.cs` | Table `ApiKeys`; unique index `Selector`; index `(PrincipalType, PrincipalId)`; composite `(PrincipalType, PrincipalId, RevokedAt)` for quota. |
| Infrastructure | `ApiKeyRepository`, migration | `Persistence/Repositories/ApiKeyRepository.cs`, `Persistence/Migrations/*_AddApiKeys.cs` | Additive migration; no existing table changes; no raw SQL. |
| Infrastructure | `HmacApiKeyHasher`, `ApiKeyOptions` | `src/TechInventory.Infrastructure/Services/{HmacApiKeyHasher,ApiKeyOptions}.cs` | `HMACSHA256` + `CryptographicOperations.FixedTimeEquals`; startup validation of pepper presence + ≥32-byte decoded entropy (ADR §Storage), distinct from `Auth:Local:SigningKey`. |
| Api | `ApiKeyAuthHandler`, scheme const | `src/TechInventory.Api/Authentication/{ApiKeyAuthHandler,ApiAuthenticationSchemes}.cs` | Custom `AuthenticationHandler<>`; live principal re-resolution by `PrincipalType`; builds `ClaimsPrincipal` (`oid`/`sub`=`PrincipalId`, `name`, `ClaimTypes.Role`, `apikey_selector`, `apikey_scope`). |
| Api | `ApiKeyScopeRequirement` | `src/TechInventory.Api/Authentication/ApiKeyScopeRequirement.cs` | No-op unless `apikey_scope` claim present; else enforces the ADR scope table. |
| Api | `Program.cs` update | `src/TechInventory.Api/Program.cs` | Registers the `ApiKey` scheme; extends `ForwardDefaultSelector` with the ambiguous-credential guard (checked first) then the `ApiKey ` prefix branch (ADR). |
| Api | `ApiKeysController`, rate limiter | `src/TechInventory.Api/Controllers/ApiKeysController.cs` | Thin MediatR dispatch; bearer-only; rate limiter bound to configuration so integration tests can override it (test-scoped, never the production default at load). |
| Web (Wave 2) | Generated client, Settings/Admin pages, secret-display component, i18n | `src/TechInventory.Web/src/**` | Per spec US-6/US-7; loading/empty/error/success states; axe-core zero violations; no hard-coded strings. |

## 2. Dependency Ordering

```
T-001 Domain → T-002 Infrastructure → T-003 Application → T-004 Api auth handler
  → T-005 scope policy, T-006 controller+rate limit, T-007 ambiguity guard
    → T-008 OpenAPI → T-009 unit tests → T-010 integration tests → T-011 contract tests
      → T-012 ops docs → T-013 verify green
        → T-014 TS client (Wave 2, needs Wave 1 merged) → T-015..T-021 UI/i18n/manual
          → T-022 full verify → T-023 security review, T-024 architecture review → T-025 merge
```

## 3. Key Integration Points

| Seam | File | Change |
|---|---|---|
| Auth routing | `Program.cs` (`ForwardDefaultSelector`, ~L73–97) | +1 branch (`ApiKey` prefix) +1 ambiguity guard evaluated first; Entra/Local branches untouched — regression-tested (spec N-19/N-20). |
| AuthZ policies | `Program.cs` (~L250–260 policy builder) | Unchanged — `Admin`/`AdminOrMember` key on `ClaimTypes.Role`, populated identically by the new handler. New, additive: `ApiKeyScopeRequirement`. |
| Current user | `Api/Authentication/HttpContextCurrentUserService.cs` | Unchanged — already resolves `oid`/`sub` generically; works for either `PrincipalType` without modification. |
| Audit | `Application/Behaviors/AuditBehavior.cs`, `Auditing/IAuditable.cs` | Unchanged pipeline; payload serializer must redact any `secret`/`verifierHash` field. |
| Principal lookup | Existing `IOwnerRepository`, `ILocalUserRepository` | Handler loads whichever repository matches the key's `PrincipalType` — never assumes `Owner`. |
| Household resolution | `Application/Abstractions/Repositories/IHouseholdRepository.cs` | `ListAsync` exactly-one pattern reused verbatim from `CreateDeviceCommand` (`Application/Devices/Commands/CreateDeviceCommand.cs`, household-resolution helper) — not a new pattern. |

## 4. Configuration

| Key | Layer | Default | Secret? |
|---|---|---|---|
| `ApiKeys:HmacPepper` | Infrastructure | none — required, fail-fast if absent or <32 decoded bytes | **Yes** |
| `RateLimiting:ApiKey:PermitLimit` | Api | `60` (prod); test-scoped override in integration tests | No |
| `RateLimiting:ApiKey:WindowSeconds` | Api | `60` (prod); test-scoped override in integration tests | No |

## 5. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Pepper leak allows offline key forging | Low | High | Docker secret, never in committed env files; rotation documented; entropy validated at startup. |
| SQLite write amplification from per-use `AuditEvent` | — | — | Explicitly out of scope (spec §2 non-goals); Serilog log instead. |
| API key used to bootstrap more keys | Low | Medium | Key-management endpoints reject API-key auth (N-10). |
| Principal deactivated/demoted but key still used | Medium | Low | Live principal status/role check on every request, for either `PrincipalType`. |
| Selector existence inferable via timing | Low | Medium | Dummy fixed-time HMAC path on unknown selector (ADR), verified by code-path test + security review, not by measuring latency. |
| Rate-limit tests flood production defaults, flaking CI | Medium | Low | Test-scoped `RateLimiting:ApiKey` configuration override (spec N-21). |

## 6. Status

| Phase | Status |
|---|---|
| ADR 0003 | ✅ Accepted (revised) |
| Spec 005 | ✅ Specified (revised) |
| Plan 005 | ✅ Planned (revised) |
| Tasks 005 | ✅ Defined — all 25 pending |
| Wave 1 / Wave 2 implementation | ⬜ Not started |

## 7. References

Issue #149 triggers no `R<N>` reference from `docs/references.md`.

# Known Issues

Living tracker for accepted technical debt and deferred work. Convert each entry to a GitHub issue once `gh` CLI is wired up.

---

## auth-jwt-happy-path-tests

**Status:** Open (tracked since Phase 2 Round 1)
**Severity:** Low — production code is correct; only test harness is incomplete.
**Owner:** Bishop (auth) + Apone (test infra)

### Summary

5 integration tests in `tests/TechInventory.IntegrationTests/Auth/AuthIntegrationTests.cs` are marked `[Fact(Skip=...)]`:

1. `DevBypassDisabled_ValidTokenWithAdminRole_ReturnsSuccess`
2. `DevBypassDisabled_ValidTokenWithAdminRole_AuditLogsShowCorrectUser`
3. `DevBypassDisabled_ViewerRoleOnAdminEndpoint_Returns403Forbidden`
4. `HttpContextCurrentUserService_ResolvesFromJwt`
5. `ProductionWithDevBypass_ThrowsOnStartup`

### Root cause

The `JwtAuthFactory` test fixture cannot replace the production `JwtBearerOptions.Authority` + `ConfigurationManager` (which fetch live Entra JWKS) with an in-memory RSA signing key. Despite `PostConfigure<JwtBearerOptions>` overriding `TokenValidationParameters`, the `ConfigurationManager` re-instantiates and overwrites the test key, so `TestJwtBuilder`-signed tokens fail signature validation.

Both Apone (commit `fb9ba14`) and Bishop attempted multiple approaches:
- PostConfigure swap of `TokenValidationParameters` → `ConfigurationManager` wins
- Null `Authority` + null `ConfigurationManager` → middleware rebuilds them from `Configuration`
- Throwing `BackchannelHttpHandler` → handler not invoked because options are bound earlier than expected

### What IS covered (6/11 auth tests green)

- `DevBypassEnabled_UnauthenticatedRequest_ReturnsSuccessWithDevAdmin` — dev-bypass path
- `DevBypassDisabled_NoToken_Returns401Unauthorized` — middleware rejects missing tokens
- `DevBypassDisabled_InvalidToken_Returns401Unauthorized` — middleware rejects garbage tokens
- `DevBypassDisabled_TokenWithNoRoles_Returns401Unauthorized` — role-claim guard works
- 2 pre-existing `DevAuthBypassTests` from Phase 1

The negative-case coverage gives high confidence that the production JWT pipeline rejects bad tokens correctly. The acceptance path (valid Admin token → 200, Viewer → 403) gets exercised end-to-end via Playwright once Phase 2 ships the login flow (Round 2+).

### Recommended fix

Canonical ASP.NET Core test pattern: **replace the `JwtBearer` scheme entirely in tests** with a custom `AuthenticationHandler<AuthenticationSchemeOptions>` that constructs a `ClaimsPrincipal` from a header (e.g. `X-Test-User: oid=...;roles=Admin,Member`). This sidesteps `ConfigurationManager` entirely. Estimated effort: 1 focused session.

Alternative: spin up an in-process WebApplication serving the test JWKS and point `MetadataAddress` at it. More work, more realistic, but probably overkill for a household app.

### Tracking

- Created: Phase 2 Round 1 (after commits `023331e` Bishop, `fb9ba14` Apone, plus Bishop's followup attempt)
- Decision: Path B (skip + track) chosen by Brian to maintain Round 2 momentum
- Convert to GitHub issue when `gh` CLI is wired up (parking lot alongside gitleaks tuning + branch protection)

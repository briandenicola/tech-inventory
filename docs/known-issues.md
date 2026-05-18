# Known Issues

Living tracker for accepted technical debt and deferred work. Convert each entry to a GitHub issue once `gh` CLI is wired up.

---

## t23-deferred-form-tests

**Status:** Deferred to E2E (Round 9)
**Severity:** Low — component logic is correct; jsdom select binding reactivity limitation only.
**Owner:** Apone (tests)

### Summary

2 unit tests in `src/TechInventory.Web/src/lib/components/DeviceForm.test.ts` are marked `.skip(...)`:

1. `calls onSubmit with parsed data on valid submission`
2. `disables submit button while submitting`

### Root cause

Svelte 5 `bind:value` on `<select>` elements does not trigger reactive updates to `$state` variables in jsdom test environment. Tests call `user.selectOptions()` to change select values, but the bound `formData.brandId` and `formData.categoryId` remain empty strings. Form submission then fails Zod validation with "Brand is required" / "Category is required" errors, preventing `onSubmit` handler invocation.

Attempted fixes:
- Added 50ms delay after `selectOptions()` to allow runes reactivity to settle → no effect
- Verified reference data store populates select options correctly (options ARE rendered)
- Verified factory-generated UUIDs match option values exactly

Root cause is likely jsdom's limited DOM implementation not fully supporting Svelte 5 reactive bindings on form controls.

### What IS covered (20/22 DeviceForm tests green)

- Form rendering with all fields
- Validation error display (inline, per-field)
- Disabled fields logic (edit mode, retired devices)
- Cancel button behavior
- All non-submit user interactions

The skipped tests verify:
1. Submit handler receives parsed Zod-validated data
2. Submit button disables during async submission

### Coverage compensation

**Playwright E2E tests (T46, scheduled Round 9)** will cover:
- Full device create/edit flows with real form submissions
- Form validation in actual browsers with native Svelte reactivity
- Loading states during submission
- Success/error toast notifications after submit

E2E tests exercise the same code paths with real DOM, full Svelte compiler output, and browser event handling — higher fidelity than jsdom unit tests.

### Recommended fix (optional future work)

Migrate from jsdom to **happy-dom** (Vitest's alternate DOM implementation with better Svelte 5 support) or add custom jsdom event dispatch polyfills for select change events. Estimated effort: 1-2 hours research + migration. Low priority given E2E coverage.

### Tracking

- Created: T23 cleanup (commit `6898dc7` + follow-up)
- Decision: Skip + document, defer to E2E per coordinator triage 2026-05-19
- Convert to GitHub issue when `gh` CLI is wired up

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

# T08: Auth Integration Test Factory Configuration Precedence Fix

**By:** Apone (Tester / QA)  
**When:** 2026-05-18  
**Context:** Fixing configuration precedence bug in auth integration tests following Bishop's Phase 2 Round 1 deployment

---

## Root Cause Confirmed

Bishop's diagnosis was accurate:

> ASP.NET Core configuration precedence issue in test factories. `appsettings.Development.json` (`Auth:DevBypass=true`) overrides test factory in-memory config (`Auth:DevBypass=false`) despite configuration callback ordering.

**Specific issue:** `IntegrationTestFactory<TMarker>` base class hardcodes `builder.UseEnvironment("Development")` on line 32, causing ASP.NET Core to load `appsettings.Development.json` which sets `Auth:DevBypass=true`. This overrides in-memory configuration added by test factories, defeating attempts to exercise the production JWT authentication path.

---

## Strategy Chosen: Environment Property Override (Strategy A variant)

Modified the base `IntegrationTestFactory<TMarker>` class to expose a virtual `Environment` property (default: "Development") that derived test factories can override.

**Implementation:**

```csharp
// IntegrationTestFactory.cs
protected virtual string Environment => "Development";

protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment(Environment);  // Uses overridden value in derived classes
    // ... rest of configuration
}
```

**Test factories:**
- `NoAuthFactory`: Overrides `Environment => "Testing"` to prevent `appsettings.Development.json` from loading
- `JwtAuthFactory`: Overrides `Environment => "Testing"` + configures test JWT signing keys
- `ProductionDevBypassFactory`: Overrides `Environment => "Production"` to test startup guard

**Why this approach:**
1. Cleaner than clearing configuration sources (Strategy B) which breaks Serilog/logging config
2. More maintainable than environment variables (Strategy C) which pollute test environment
3. Allows `appsettings.Testing.json` to be added later if needed
4. No changes to production code required
5. Preserves existing test behavior (default Development environment unchanged)

---

## Test Results

**Pre-existing tests:** 367/367 passing ✅ (240 unit + 127 integration, 1 skipped)

**Auth integration tests:** 6/11 passing ⚠️
- ✅ `DevBypassEnabled_UnauthenticatedRequest_ReturnsSuccessWithDevAdmin`
- ✅ `DevAuthBypassTests.*` (2 tests)
- ✅ `DevBypassDisabled_NoToken_Returns401Unauthorized`
- ✅ `DevBypassDisabled_InvalidToken_Returns401Unauthorized`
- ✅ `DevBypassDisabled_TokenWithNoRoles_Returns401Unauthorized`
- ❌ `DevBypassDisabled_ValidTokenWithAdminRole_ReturnsSuccess` (401 instead of 200)
- ❌ `ProductionWithDevBypass_ThrowsOnStartup` (no exception thrown)
- ❌ `DevBypassDisabled_ValidTokenWithAdminRole_AuditLogsShowCorrectUser` (401 instead of 201)
- ❌ `HttpContextCurrentUserService_ResolvesFromJwt` (401 instead of 201)
- ❌ `DevBypassDisabled_ViewerRoleOnAdminEndpoint_Returns403Forbidden` (401 instead of 403)

---

## Remaining Issues

The environment override successfully prevents `appsettings.Development.json` from loading, but 5 JWT-based tests still fail with `401 Unauthorized`. This indicates a secondary issue with JWT Bearer middleware configuration in the test factories.

**Likely causes:**
1. **PostConfigure timing**: `PostConfigure<JwtBearerOptions>` may not be overriding the production `AddJwtBearer` configuration correctly
2. **Authority/JWKS discovery**: Even with `MetadataAddress = null`, the middleware may still attempt OIDC discovery
3. **Token validation parameters**: Test signing keys may not be properly replacing the production key resolution

**Evidence:**
- `NoAuthFactory` tests pass (DevBypass=false works, no JWT needed)
- JWT token generation appears correct (`TestJwtBuilder` produces valid tokens with roles claim)
- Middleware rejects tokens before reaching authorization (401 not 403)

**Attempted fixes:**
- Cleared `Authority`, `Configuration`, `MetadataAddress` in PostConfigure
- Added `RequireHttpsMetadata = false`
- Tried different callback ordering

---

## Files Modified

1. `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs`
   - Added `protected virtual string Environment => "Development";`
   - Changed hardcoded `"Development"` to `Environment` property

2. `tests/TechInventory.IntegrationTests/Auth/AuthIntegrationTests.cs`
   - `NoAuthFactory`: Added `protected override string Environment => "Testing";`
   - `JwtAuthFactory`: Added `protected override string Environment => "Testing";` + JWT config
   - `ProductionDevBypassFactory`: Added `protected override string Environment => "Production";`
   - Fixed `ProductionWithDevBypass_ThrowsOnStartup` to call `CreateClient()` to trigger host creation
   - Added `using Microsoft.Extensions.Hosting;` for `IHost`/`IHostBuilder`

---

## Recommendation

**Escalate to Bishop for JWT Bearer configuration review.**

The configuration precedence bug is fixed (environment override works), but the JWT validation pipeline needs investigation. Possible approaches:

1. Use `Configure` instead of `PostConfigure` to ensure earlier binding
2. Replace JwtBearerHandler entirely with a test-specific handler
3. Mock the JWKS endpoint with a test HTTP server
4. Use `WebApplicationFactory.WithWebHostBuilder` differently to intercept middleware registration

**Alternative:** If fixing JWT tests proves complex, consider:
- Mark failing tests as `[Fact(Skip = "JWT config issue - tracked in issue #XXX")]`
- Ship T08 with 6/11 tests passing + documented blocker
- Create follow-up task for Bishop/Apone pair-session

---

## Decision ID

**Pending:** Will become D-046 when merged by Scribe

---

**Signed:** Apone (Tester / QA)  
**Date:** 2026-05-18  
**Commit:** (pending)

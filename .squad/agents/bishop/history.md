# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device tracker. Single household. Privacy-first — data never leaves the host, no third-party telemetry.
- **Stack:** Microsoft Entra ID (Workforce tenant — household existing tenant), OIDC + PKCE flow. ASP.NET Core 10 auth/authorization. MSAL.js on the SvelteKit client. Roles: `Admin`, `Member`, `Viewer`. Local account for default administrator (bootstrap). Audit log (append-only AuditEvent table).
- **Created:** 2026-05-18

## Core Context

Security baselines (PRD §7): OWASP ASVS L2, OWASP API Top 10 (2023) verified, SBOM per release, WCAG 2.2 AA (Vasquez/Apone own a11y, I own the security side of identity).

Auth phase: `specs/002-auth-entra` (Phase 2, ~Weeks 6–7).

Required behaviors (PRD §F5–§F7):
- Local account for default admin (bootstrap)
- Entra ID OIDC for family members
- Per-endpoint policy enforcement, default-deny
- Configurable session timeout
- Sign-out on all devices (admin override)
- Every mutation logged: who, what, when, before/after
- Audit query endpoint (Admin only)
- AuditEvent table immutable (append-only)

Critical Playwright journeys I gate (PRD §7.5.4): #1 Sign in, #2 Sign in denied, #11 Role enforcement. These three are non-negotiable before v1.

Discipline (constitution / copilot-instructions.md):
- Tokens: memory or sessionStorage only, NEVER localStorage
- `gitleaks` pre-commit hook
- Trivy container scan
- SBOM per release
- No third-party analytics or scripts without ADR
- Never log secrets/PII (Serilog destructuring policies)

Open question resolved (PRD §14): "External ID vs. Workforce tenant — which is right for family use?" **DECIDED by Brian: Workforce tenant** (household existing tenant). External ID recommendation withdrawn. See decision `copilot-directive-2026-05-18T140924Z-entra-tenant.md`.

## Recent Updates

**2026-05-18 (Phase 1 Round 1):** Auth design revision complete. `docs/auth-design.md` v2.0 now reflects Workforce Entra ID tenant choice (Brian decided 2026-05-18T14:09:24Z). OIDC scopes (`openid profile email offline_access`), app role mapping (admin/member/viewer), token validation checklist (ASVS V2.10.2) documented in decision D-009. Phase 2 authN wiring unblocked. Currency decision finalized (D-008): per-device with household default. Hicks completes Domain T01–T05. Vasquez + Hudson deploy token-storage enforcement gates (D-010: 4-gate model). Apone writes 13 Domain unit tests + Playwright token-storage E2E across 6 browsers. All Phase 1 Round 1 orchestration complete; decisions merged to `.squad/decisions.md`.


## Learnings

### 2026-05-18 (Phase 2 Round 3) — D-136: Owner auto-provision on first sign-in

**Bug pattern:** 404-on-first-sign-in is a classic OIDC trap. Authenticating the principal is not enough; any endpoint that assumes a pre-existing local user row (`Owner`) will fail for a brand-new `oid` unless the app provisions that row on demand.

**What shipped:** `/api/v1/owners/me` now routes through `EnsureCurrentOwnerProvisionedCommand`, which returns the existing owner when present or creates one keyed by `EntraObjectId` when missing. `DisplayName` and `Role` come from claims (`ClaimTypes.Name`, `ClaimTypes.Role`) with safe fallbacks (`User {short}` and `Member`), and `ICurrentUserService` now exposes claim helpers so controllers stay thin.

**Why this shape is right:** In a single-household, Entra-authenticated app, every successfully authenticated principal is implicitly a household member. Auto-provisioning at the current-user endpoint keeps first sign-in idempotent, fixes dev bypass and real Entra flows with one code path, and preserves CQRS cleanliness by using a command rather than making a query write.

**Remember next time:** whenever identity is external (OIDC/Entra/Auth0/etc.), audit every "current user" or "profile" read path for hidden local-row assumptions. If the app stores a local projection of the external user, the safe default is read-or-create on first authenticated access, with audit coverage and duplicate-claim fallbacks considered up front.

### 2026-05-19 (Phase 2 Round 2) — T11: `GET /api/v1/owners/me` Endpoint

**Shipped:**
- **T11 (`GET /api/v1/owners/me`):** New endpoint via `GetOwnerByEntraObjectIdQuery` (MediatR) → existing `IOwnerRepository.GetByEntraObjectIdAsync` (already lived from Phase 1). Returns `OwnerResponse` (id, email, displayName, role). Wired to `ICurrentUserService` for current-user lookup. Enables T10 auth store population.
- **Testing:** 1 integration test added (dev-bypass path verified). Production path coverage inherited from T07 `HttpContextCurrentUserService` tests.
- **Audit impact:** Zero — endpoint is idempotent read-only query.
- **Final test count:** 374 passing / 6 skipped / 0 failed.

### 2026-05-19 (Phase 2 Round 1) — T06, T07, T08-partial: Entra JWT Bearer + HttpContextCurrentUserService

**Shipped:**
- **T06 (JWT Bearer validation middleware):** Dual-audience config (App ID URI + bare Client ID) wired in `Program.cs`. Authority set to Entra Workforce. Token validation params enforce issuer, audience, lifetime, signature checks. Authorization policies created for `Admin`, `Member`, `Viewer` roles mapped from `app_roles` claim via `OnTokenValidated` event (D-040, D-041). Dev bypass guard active (throws on non-Development env with bypass enabled, D-043).
- **T07 (HttpContextCurrentUserService):** Replaced `SystemCurrentUserService` with `HttpContextCurrentUserService`. Extracts `oid` from JWT claim, performs scoped `Owner` lookup. First-login onboarding: creates `Owner` if not found (auto-provisions from `oid`, `email`, `name`, first `app_roles` entry). Audit metadata now stamps with real user from Entra ID.
- **T08 (Integration tests):** Partial. Core test patterns implemented: test JWT builder (RSA 2048, in-memory keys per test, D-044); valid JWT → 200 OK; expired JWT → 401; invalid signature → 401; missing/empty `app_roles` → 401; role-based access control tests. **Infrastructure issue (NOT production code bug):** `appsettings.Development.json` `Auth:DevBypass=true` takes precedence over test factory in-memory config overrides. Production code is correct (verified via manual curl with mock JWT). Test harness fix deferred to follow-up task (Apone to resolve).

**Key Decisions:**
- D-040: Dual audience validation (both formats accepted).
- D-041: `OnTokenValidated` event for role mapping (cleaner than custom handler).
- D-042: Clock skew 2 minutes (vs. 5 min default; tighter security, acceptable for home infrastructure).
- D-043: Startup guard for dev bypass (fail-fast on misconfiguration).
- D-044: RSA 2048 test keys (matches production RS256 validation code path).
- D-045: `ICurrentUserService` stays single-method (YAGNI; expand on concrete need).

**Reflection:** T06-T07 are production-ready. Auth pipeline is correct: validated JWT → extracted roles → `HttpContextCurrentUserService` retrieves current user → audit stamps real actor. T08 test infrastructure needs a follow-up session (test factory config precedence issue), but this does NOT block Phase 2 merge — production code is verified correct. Commit `023331e`: T06+T07 complete + T08 partial. Awaiting Apone's test factory refactor to ship remaining integration tests.

### Entra Tenant Decision — REVERSED (2026-05-18)

**What changed**: External ID recommendation → Workforce tenant decision.

**Context**: 
- Bishop recommended Entra External ID for family use (70% cost savings, consumer-friendly).
- Brian decided: Use the household's **existing Workforce tenant** (same tenant for Office, Teams, etc.).
- **Why**: Simpler ops (one tenant to manage); users already provisioned; aligns with self-hosted philosophy.

**Implications for Phase 2**:
1. App registration in household's Entra tenant (not new External ID tenant).
2. No self-service sign-up flow; only household users can sign in (already provisioned by IT).
3. App roles defined: `admin`, `member`, `viewer` (mapped in Entra app manifest).
4. MSAL.js config: authority = `https://login.microsoftonline.com/{household-tenant-id}`.
5. First-login handler: Create `Owner` by `EntraObjectId` (auto-onboard).
6. Token issuer/audience/scope: Standard Workforce OIDC (`openid profile email offline_access`); no Graph API.

**Scope mapping** (final detail for Hicks):
- **JWT claim `app_roles`**: List of role values assigned to user (e.g., `["member", "admin"]`).
- **Backend**: Use first role from list as primary authorization role (app_roles[0]).
- **Storage**: Mirror in local `Owner.role` column for safety + optional override via admin UI.

**Cost impact**: Aligned with zero-cost for Workforce tenant (part of household Office subscription). External ID per-auth fees avoided entirely.

### ASVS L2 Controls Most Relevant to Tech Inventory

- **V2.10.2** (Credential Storage): JWT tokens never in localStorage; sessionStorage or memory only.
- **V4.1.2** (Access Control Policy Test): Every endpoint must have an explicit authorization policy (default-deny).
- **V4.1.3** (Access Control Bypass Test): Resource-level checks required in Application layer (BOLA prevention).
- **V5.3.1** (Input Validation): FluentValidation on all command/query inputs (pipeline behavior).
- **V6.1** (Data Classification): Device serials not PII; Entra tokens never stored in DB.
- **V8.1** (Defect Handling): Exceptions only for exceptional conditions; `Result<T>` for expected failures.

### Threat-Model Surfaces & Mitigations

Seven surfaces analyzed: Web Client, API, Database, Auth Provider (Entra External ID), Container Host, Reverse Proxy, Backup Destination.

---

### 🔴 CRITICAL PATTERN — 2026-05-21: WebKit bug 160953 — Layout Containing-Block Trap

**Context:** Vasquez discovered a critical WebKit rendering bug during PWA iOS testing (session `.squad/log/2026-05-21T15-49-37Z-triple-pwa-fix.md`).

**The Bug:** In WebKit (iOS Safari, PWA standalone), the CSS properties below create a **containing block** for `position: fixed` descendants **even when NOT actively used** (bug 160953):
- `transition-property: transform` (from Tailwind `transition-transform` class) ⚠️ MOST INSIDIOUS — appears inert but ALWAYS active in WebKit
- `will-change: transform | filter | perspective` (if present at rest)
- `transform: [any value except none]` (if present at rest)
- `filter`, `backdrop-filter`, `perspective` (if present at rest)
- `contain: paint | layout | strict | content` (if present at rest)
- `content-visibility: auto` (if present at rest)

**Impact:** When these properties exist on a layout wrapper, all `position: fixed` descendants (FABs, modals, bulk-action bars) re-parent from the viewport to that wrapper instead of staying viewport-anchored. Result: fixed elements appear mid-page and scroll with content.

**Real case:** PullToRefresh content wrapper had `transition-transform duration-200 ease-out` as **static Tailwind classes**. This created a WebKit containing block, trapping every FAB and modal in `(authenticated)/+layout.svelte`. Appeared as FABs mid-page over device cards. Latent bug from commit 39eb0c5, discovered 2026-05-21.

**Solution Pattern:**
- Derive an `isActive` boolean
- Apply transform-related classes ONLY when active via Svelte `class:` directives
- At rest: ZERO transform-related CSS = no containing block = fixed descendants resolve to viewport ✅

**For Bishop:** This pattern is critical for any layout changes you make. If you add new layout wrappers or modify existing ones, ensure no at-rest containing-block triggers exist. Test on iOS PWA standalone before merge.

**Reference:** `.squad/decisions/D-165` (decision + full spec), `.squad/skills/fixed-position-containing-block/SKILL.md` (deep-dive), `.squad/orchestration-log/2026-05-21T15-49-37Z-vasquez-fab-regression.md` (session notes).

Key high-residual-risk areas (require monitoring):
- **API BOLA/RBAC bypass**: Mitigated by code review + automated tests, not automated enforcement.
- **Host compromise**: Depends on Hudson's OS hardening (non-root containers + read-only FS mitigate).
- **Backup security**: TBD Phase 3; currently low SLA for backup encryption.

No Critical or High-risk vectors identified; proceed to Phase 2 with confidence.

### Entra External ID Recommendation (vs. Workforce)

**Decision**: Use External ID for family use case.

**Why**: Consumer-friendly sign-up (MSA/Google/Apple), 70% lower licensing cost (per-auth vs. per-user), simpler operations (no org sync), better offline support.

**Cost impact**: ~$0.02–0.10 per sign-in (5 family members × 30 days ≈ $3–15/month) vs. Workforce ($20–40/month).

### Token Storage Discipline

**Rule (immutable)**: JWT in memory or sessionStorage; NEVER localStorage.

**ASVS Basis**: V2.10.2 (credentials don't persist across page reloads).

**Enforcement**: Pre-commit hook + ESLint + Playwright test + code review.

**Impact**: Non-negotiable for ASVS L2 compliance; must be reviewed on every frontend PR.

### Audit Log as Security Control

AuditEvent table (append-only) is both a business requirement (PRD §F7) and a security control (repudiation prevention, forensics).

**Key design**: No Update/Delete on AuditEvent. Enforced at EF Core + DB schema level.

**Usage**: Admin-only query endpoint; queryable by entity type, time range, action. Enables blame attribution + pattern detection (e.g., bulk device deletes).

### 2026-05-19 (DevBypass Rip) — TestAuthHandler + UseTestAuth Knob

**Context:** Copilot CLI ripped the production `Auth:DevBypass` shim. I owned the backend integration-test cleanup alongside Vasquez (FE) and Hudson (E2E).

**Shipped:**
- `IntegrationTestFactory<TMarker>` gained `protected virtual bool UseTestAuth => true;`. The `TestAuthHandler` swap is now conditional. Also added missing `using Microsoft.AspNetCore.TestHost;` that the Copilot CLI WIP forgot.
- Rewrote `Auth/AuthIntegrationTests.cs`: dropped `DevBypassEnabled_*` (the bypass no longer exists) and `ProductionWithDevBypass_ThrowsOnStartup` + `ProductionDevBypassFactory` (no startup guard exists either — Program.cs simply has no bypass code). Renamed the JWT validation tests to drop the `DevBypassDisabled_` prefix. `NoAuthFactory` and `JwtAuthFactory` now override `UseTestAuth => false` and stripped the no-op `Auth:DevBypass` config keys. Fixed the JwtAuthFactory `PostConfigure<JwtBearerOptions>` to target `ApiAuthenticationSchemes.EntraScheme` (= `"TechInventoryAuth.Entra"`) instead of the stale hardcoded `"TechInventoryAuth"` — necessary because Program.cs split the composite into a policy scheme + per-issuer handlers.
- `Auth/LocalAuthEndpointTests.cs.LocalAuthFactory`: also overrode `UseTestAuth => false` (the local HS256 `must_change_password` gate would have been masked by TestAuthHandler), stripped the no-op `Auth:DevBypass` key, refreshed the XML doc.
- `Controllers/OwnersControllerTests.cs`: added `using TechInventory.IntegrationTests.Support;` so `TestAuthHandler.DefaultUserId` resolves.
- `Controllers/AuditEventsAuthorizationTests.cs` + `Support/TestAuthHandler.cs`: rephrased stale "dev-bypass handler" comments to reference `TestAuthHandler` directly.

**Verified:** `dotnet build -c Release` clean; `dotnet test tests/TechInventory.IntegrationTests -c Release` → 157 passed / 5 skipped / 0 failed (the 5 skips are the pre-existing T08 JWKS-discovery deferrals + 1 unrelated OpenAPI drift skip). Unit suite: 244 passed / 0 failed.

**Decision (drop to inbox):** D-XXX — `UseTestAuth` knob is the canonical opt-out for factories that need to exercise the real Entra/Local JWT pipeline. Production binary remains bypass-free; the only auth shortcut lives in the test project's `TestAuthHandler` and is per-factory opt-in.

**Security audit:** The new wiring preserves default-deny (ASVS V4.1.2). Tests that assert the 401/403 paths now run through the real `TechInventoryAuth` policy scheme + Entra JwtBearer handler instead of a contrived bypass — strictly better coverage. No production code path was loosened. `Auth:Local:SeedEnabled = false` stays forced in every test host so the seed service can't accidentally provision a known-password admin in a test SQLite file.

**Per instructions:** Did NOT commit. Working tree left dirty for Copilot CLI to fold into the bigger DevBypass-rip commit.

**Learnings:**
- When ripping a configuration-driven shim, `grep` the *test project* for the config key too. `Auth:DevBypass = "false"` was set defensively in 3 different factories; all three were no-ops after the rip but each was a code-smell that would mislead the next reader.
- The `WebApplicationFactory<T>` + `ConfigureTestServices` extension lives in the `Microsoft.AspNetCore.TestHost` namespace, not `Mvc.Testing`. Easy import to miss when refactoring.
- When Program.cs's auth registration changes shape (single scheme → composite policy scheme), every `PostConfigure<JwtBearerOptions>` in the test project needs its scheme name re-pointed. Hard-coded scheme strings are a latent bug; the `ApiAuthenticationSchemes` constant class exists for exactly this reason — use it.

### 2026-06-13 — Deep Security Engineering Audit (Phase 2 Post-Deployment)

**Audit Scope**: Comprehensive security review post-v1.0 ship. Covered default-deny authorization, role/resource checks, local auth fallback risks, token storage, client auth state, audit logging/PII, secrets handling, CORS/headers, dependency gates, threat-model drift, SQL injection patterns, and the reported inventory visibility bug as a potential security symptom.

**Evidence-Based Findings** (16 total: 0 Critical, 2 High, 5 Medium, 9 Low/Advisory):

#### HIGH-1: Missing Resource-Level Authorization (BOLA Risk) 🔴
**OWASP API Top 10 2023: API1 - Broken Object Level Authorization**
**Severity**: HIGH | **Risk**: Data disclosure + unauthorized mutation | **ASVS**: V4.1.3

**Evidence**: 
- `src/TechInventory.Application/Devices/Commands/UpdateDeviceCommand.cs:116-124` validates that the target Owner exists and is active, but does NOT verify the current user has permission to update that specific device.
- `src/TechInventory.Application/Devices/Commands/DeleteDeviceCommand.cs` (similar pattern)
- `src/TechInventory.Application/Owners/Commands/UpdateOwnerCommand.cs:18-60` — Admin can update ANY Owner record; no check that non-Admin users can only update their own profile.
- Controller layer: `DevicesController`, `OwnersController`, `BrandsController`, etc. all rely solely on `[Authorize]` (authenticated) or `[Authorize(Policy = AuthorizationPolicies.Admin)]` — no resource-level checks.

**Attack Scenario**: Member Alice (authenticated, OwnerId=A) can:
1. `PUT /api/v1/devices/{deviceId}` with `OwnerId: B` (Bob's ID) in the body
2. Backend validates Bob exists but does NOT check if Alice is Admin or if the device currently belongs to Alice
3. Alice reassigns Bob's device to herself

**Recommended Fix**:
1. Add `ICurrentUserService` to all mutating command handlers
2. For Device operations: If current user Role != Admin, enforce `device.OwnerId == currentUserId` or throw 403 Forbidden
3. For Owner operations: `/me` endpoint should be separate from Admin's `PUT /owners/{id}` 
4. Write integration tests per role (Admin can update any; Member can only update own)

**ASVS Reference**: V4.1.3 ("application uses a single and well-vetted access control mechanism for accessing protected data and resources")

#### HIGH-2: Local Auth Password Storage Inspection Needed 🔴
**Severity**: HIGH | **Risk**: Credential compromise if hash parameters are weak | **ASVS**: V2.4.1

**Evidence**: 
- `src/TechInventory.Infrastructure/Services/PasswordHasher.cs` not in file tree examined
- `src/TechInventory.Api/Authentication/LocalAdminSeedHostedService.cs:62` calls `hasher.Hash(password)` 
- `src/TechInventory.Application/Auth/Commands/LocalLoginCommand.cs:81` calls `passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordAlgorithm)`
- No evidence of key-stretching parameters (iterations, memory cost, parallelism) in reviewed code

**Question for Brian**: What is the current `PasswordHasher` implementation? Constitution mentions Argon2id (good), but I need to verify:
- Work factor (iterations ≥ 2, memory ≥ 19456 KiB per OWASP ASVS L2 V2.4.1)
- Salting per-password (not a global pepper)
- Timing-safe comparison in Verify

**Recommended Verification**:
1. Inspect `src/TechInventory.Infrastructure/Services/PasswordHasher.cs` 
2. If using Argon2id: ensure `iterations >= 2`, `memorySize >= 19456`, `parallelism >= 1`
3. If using bcrypt: ensure work factor ≥ 12 (current OWASP minimum)
4. Add unit test that hashing the same password twice yields different hashes (proves per-password salt)

**ASVS Reference**: V2.4.1 (passwords stored using approved one-way key derivation), V2.4.5 (sufficient iteration count)

#### MEDIUM-1: Must-Change-Password Gate Bypassable via Direct API Calls 🟡
**Severity**: MEDIUM | **Risk**: Local admin with default password can access full API if they skip UI | **ASVS**: V2.2.4

**Evidence**: 
- `src/TechInventory.Api/Program.cs:336-360` — must-change-password gate is custom middleware that only blocks requests if `must_change_password=true` AND path != `/api/v1/auth/local/change-password`
- BUT this middleware runs AFTER `UseAuthentication()` and `UseAuthorization()` (line 330), so any endpoint the user can reach gets processed before the gate fires
- An attacker with a default-password local admin JWT can call any endpoint if they craft raw HTTP requests (bypass the UI's change-password redirect)

**Attack Scenario**:
1. Seed local admin with `SeedRequireChangeOnFirstLogin=true`
2. Attacker signs in via `/api/v1/auth/local/login` → receives JWT with `must_change_password=true`
3. UI routes to change-password screen, but attacker uses `curl` with the JWT
4. `curl -H "Authorization: Bearer TOKEN" https://api/v1/devices` → 200 OK (middleware gate only fires if path is not `/health`)

**Recommended Fix**:
- Move must-change-password gate BEFORE `app.UseAuthorization()` OR
- Extend the AuthorizationHandler to check the `must_change_password` claim and fail authorization (not just respond with 403 in middleware)

**ASVS Reference**: V2.2.4 ("session binding to prevent hijacking")

#### MEDIUM-2: CORS AllowCredentials Without Strict Origin Validation 🟡
**Severity**: MEDIUM | **Risk**: Cross-origin attacks if origin list is too permissive | **ASVS**: V14.4.4

**Evidence**: 
- `src/TechInventory.Api/Program.cs:252-265`
- `.env.example:79`: `Cors__AllowedOrigins__0=https://inventory.denicolafamily.com`
- **Good**: Only one origin configured by default
- **Risk**: If operator adds multiple origins (LAN + WAN), or adds a wildcard-subdomain pattern, credentials can leak cross-origin

**Recommended Fix**:
1. Document in `.env.example` that CORS origin list must NEVER include wildcards when `AllowCredentials()` is set
2. Add startup validation that fails if any origin contains `*` and `AllowCredentials` is true
3. Consider runtime origin validator instead of static list if dynamic origins are needed

**ASVS Reference**: V14.4.4 (CORS Access-Control-Allow-Origin header uses strict allow list)

#### MEDIUM-3: Audit Log PII/Secrets Exposure Risk 🟡
**Severity**: MEDIUM | **Risk**: Sensitive data logged in before/after snapshots | **ASVS**: V8.3.4

**Evidence**: 
- `src/TechInventory.Application/Owners/Commands/UpdateOwnerCommand.cs:42` logs `beforePayload: beforeSnapshot`
- `OwnerResponse.cs` includes `displayName` (not PII by itself, but consider edge cases like email-as-displayName)
- Device audit includes `SerialNumber` — PRD §6.1 says "Device serials not PII", but they are sensitive for insurance/warranty claims

**Question for Brian**: 
- Are display names ever email addresses?
- Should serial numbers be redacted in audit log (visible to Admin only)?

**Recommended Fix**:
1. Add Serilog destructuring policy to scrub `PasswordHash` if it ever reaches a log statement
2. Review `beforePayload`/`afterPayload` for fields that should be redacted (e.g., replace serial with `"***-{last4}"`)
3. Add E2E test: `POST /api/v1/auth/local/change-password` must NOT log `NewPassword` in any audit entry

**ASVS Reference**: V8.3.4 (sensitive data is not logged)

#### MEDIUM-4: Entra JWT Audience Validation Permits Bare Client ID 🟡
**Severity**: MEDIUM | **Risk**: Token reuse from different app if Client ID collides | **ASVS**: V2.10.1

**Evidence**: 
- `src/TechInventory.Api/Program.cs:143`: `ValidAudiences = entraAudiences` 
- `.env.example:36-37`: Two audiences accepted (`api://` + bare GUID)
- Rationale: Entra can issue tokens with either format depending on `accessTokenAcceptedVersion` in app manifest

**Risk**: If another app in the same tenant shares the same Client ID (misconfigured), tokens from that app would be accepted here.

**Recommended Fix**:
1. Settle on ONE audience format (prefer `api://{clientId}`)
2. Update Entra app manifest to enforce `accessTokenAcceptedVersion: 2`
3. Remove bare Client ID from `ValidAudiences` after Brian confirms MSAL tokens match `api://` format
4. Document this in `docs/auth-design.md` under "Token Validation"

**ASVS Reference**: V2.10.1 (verify that tokens are issued by a trusted provider and for this application)

#### MEDIUM-5: No Rate Limiting on Local Auth Endpoints 🟡
**Severity**: MEDIUM | **Risk**: Brute-force attack on break-glass admin | **ASVS**: V2.2.1

**Evidence**: 
- `src/TechInventory.Api/Controllers/LocalAuthController.cs:21` — `/api/v1/auth/local/login` is `[AllowAnonymous]`
- No `[EnableRateLimiting]` attribute
- No middleware rate-limiter configured in `Program.cs`

**Attack Scenario**:
- Attacker runs 10,000 login attempts with common passwords
- Even with Argon2id (slow hashing), backend processes every request
- If seeded username is guessable (e.g., `admin`), brute-force is feasible

**Recommended Fix**:
1. Add ASP.NET Core rate limiting middleware (see Microsoft docs)
2. Apply to `/api/v1/auth/local/login`: 5 attempts per IP per 15 minutes
3. Add integration test that 6th attempt within window returns 429 Too Many Requests

**ASVS Reference**: V2.2.1 (anti-automation controls)

---

**POSITIVE FINDINGS** (14 items):
✅ Default-deny fallback policy  
✅ No raw SQL (all EF Core parameterized)  
✅ Token storage discipline (sessionStorage only)  
✅ Gitleaks pre-commit hook  
✅ Audit logging with actor stamps  
✅ Argon2id password hashing (per Constitution — verify impl)  
✅ CORS single allowed origin  
✅ Clock skew tuned to 2 min  
✅ Must-change-password flow exists  
✅ SBOM generation on main  
✅ Admin-only endpoints gated  
✅ Entra dual-issuer support (pragmatic)  
✅ Local JWT separate issuer  
✅ Secrets not in git  

**INVENTORY VISIBILITY BUG**: NOT a security issue. This is single-household (no multi-tenancy, no row-level filtering). If bug exists, likely UI filter state or cache staleness.

**THREAT MODEL DRIFT**: Medium residual risk. F025 local auth added password brute-force surface (MEDIUM-5). No CRITICAL drift.

---

**TOP 5 SECURITY NEXT ACTIONS** (Prioritized):

1. **[HIGH-1] Add Resource-Level Authorization** (2-3 days) — Enforce Member can only update own devices
2. **[HIGH-2] Verify Password Hashing Impl** (1 hour) — Confirm Argon2id params meet ASVS L2
3. **[MEDIUM-1] Fix Must-Change-Password Gate** (2 hours) — Move before UseAuthorization()
4. **[MEDIUM-5] Add Rate Limiting to Local Login** (4 hours) — 5 attempts/IP/15min
5. **[LOW-3] Enable Dependabot + Fail CI on CVEs** (1 hour) — Security → Dependabot → Enable

**Total effort**: 4-5 days for HIGH + MEDIUM fixes.

**References**: OWASP ASVS 4.0.3 L2, OWASP API Top 10 (2023) API1/API4, Constitution §2.4/§3.4, PRD §F5-F7

---

### 2026-06-14: Engineering Audit Session (Bishop)

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-bishop.md`

**Key Audit Findings:**
- Authorization checks implemented on public endpoints ✓
- No secrets detected in codebase ✓
- gitleaks pre-commit hook operational ✓
- **CRITICAL:** Missing resource-level authorization for Member device writes
  - Device endpoints allow Members to write any device
  - No ownership check: Member A can modify Member B's devices
  - Violates constitution §2 (resource-level authorization required)
- **CRITICAL:** Local auth/password/rate-limit gaps
  - Development auth bypass lacks audit trail
  - No rate limiting on API endpoints
- Audit/CORS/JWT audience risks
  - AuditEvent captures user principal, but no device ownership context
  - CORS not explicitly configured (defaults risky)
  - JWT audience mismatch could allow token reuse

**Next Steps:** Priority 1 = add ownership check to device write endpoints, Priority 2 = configure explicit CORS allow-list, Priority 3 = add JWT audience validation.

---

### 2026-06-26: iOS Standalone PWA Silent SSO Redirect — Security Review

**Orchestration Log:** `.squad/orchestration-log/2026-06-26T19-16-04Z-ios-pwa-auth.md`  
**Decision Merged:** D-170 (iOS Standalone Auto-Redirect Gate), D-171 (Branch Roadmap)

**Review Scope:** Vasquez's proposed iOS standalone PWA auto-redirect feature

**Verdict:** ✅ **Approved with 7 constraints**

**Findings:**

1. **Trust Boundary Preserved:** Auto-redirect invokes the same PKCE/OIDC flow as manual Sign In. No new cryptographic requirements; existing Entra session reuse is cryptographically safe.

2. **Token Storage Compliant:** `offline_access` scope addition stays within sessionStorage/memory-only policy (D-002, ASVS V2.10.2). No localStorage or custom persistence.

3. **Scope Correctness:** `offline_access` correctly placed in interactive login request (improves refresh-token availability), NOT in API token requests.

4. **Constraints Implemented & Tested:**
   - Standalone-only gate (matchMedia + navigator.standalone) ✅
   - Bootstrap completion gate (isLoading=false && isAuthenticated=false) ✅
   - Loop guard (sessionStorage suppression flag) ✅
   - Sign-out guard (existing suppression preserved) ✅
   - Storage guard (no localStorage/custom persistence) ✅
   - Scope guard (offline_access in interactive only) ✅
   - Test guard (20 Vitest tests covering all scenarios) ✅

5. **Test Coverage:** 20 tests across auth store, MSAL client, and login page. All scenarios covered: standalone/non-standalone, cached/uncached, suppression/clear, sign-out preservation.

6. **Documentation:** `docs/auth-design.md` §3 updated with iOS standalone flow, rationale, and scope choice.

**Security Basis for Approval:**

- Constitution §5.1: OIDC + PKCE + rotated refresh tokens — unchanged.
- Constitution §6.5.10 & D-002: Token storage ASVS V2.10.2 enforced.
- D-150: Silent bootstrap already approved; tokens remain in MSAL, server default-deny unchanged.
- D-169: Bounded fallback (one auto redirect + suppression guard) maintains loop prevention guarantee.

**Rationale:**

This feature closes a legitimate iOS standalone UX gap: per-launch sessionStorage isolation forces users to re-authenticate (via manual button tap) even when Entra has a valid browser session. Auto-redirect reuses that session safely and transparently, improving UX with zero auth-model changes or new attack surfaces.

**Next Steps:** PR from `fix/ios-pwa-silent-sso-redirect` → review → merge.


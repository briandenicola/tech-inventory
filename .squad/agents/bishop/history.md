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

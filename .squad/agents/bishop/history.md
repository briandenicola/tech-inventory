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

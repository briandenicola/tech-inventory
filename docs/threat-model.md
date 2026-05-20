# Threat Model — Tech Inventory v1

> **Phase note (added 2026-05-19)**: Authored before the 2026-05-19 PRD §13
> phase-model rewrite. Mitigation rows tagged `(TODO Phase 2)`,
> `(TODO Phase 3)`, etc. use the **original** PRD §13 numbering and
> describe planned work, not necessarily current status. Under the rewritten
> model, any still-open mitigations roll up under **P4 — Continuous
> Iteration** and should be promoted to `specs/_backlog/` if they are not
> already shipped. A formal re-baseline of this threat model against the
> shipped P0–P3 surfaces is itself a P4 candidate.

**Scope**: Self-hosted family device inventory tracker (ASP.NET Core + SvelteKit PWA + SQLite).  
**Methodology**: STRIDE per system surface.  
**Authority**: Constitution §5 (Security), PRD §7 (NFRs — Security/Privacy), PRD §14 (Open Questions).  
**Baseline**: OWASP ASVS L2, OWASP API Top 10 (2023), CWE Top 25.

---

## 1. System Surfaces

This system has seven security-relevant surfaces. Each is analyzed below using STRIDE (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).

| # | Surface | Technology | Owner | Lifecycle |
|---|---------|-----------|-------|-----------|
| 1 | **Web Client (PWA)** | SvelteKit + TypeScript + MSAL.js | Vasquez | Mobile, desktop, offline-capable |
| 2 | **API (Backend)** | ASP.NET Core 10 + Clean Architecture | Hicks | REST `/api/v1/*`, OpenAPI spec |
| 3 | **Database (SQLite)** | File-based EF Core, migrations | Hicks | On host, Docker volume, soft-deletes |
| 4 | **Auth Provider** | Microsoft Entra ID (Workforce tenant) + local-account fallback (F025 v1b) | Bishop | OIDC + PKCE primary; HS256 JWT local fallback per ADR D-140 |
| 5 | **Container Host** | Docker + Compose on home LAN | Hudson | Orchestration, volume mounts, networking |
| 6 | **Reverse Proxy** | TLS termination (external, e.g., Caddy) | Hudson | Public DNS, 443 binding, WAF (optional) |
| 7 | **Backup Destination** | TBD (PRD §14) — second host, external drive, or off-site | TBD | Recovery point objective (RPO) 24h |

---

## 2. Surface-by-Surface STRIDE Analysis

### Surface 1: Web Client (SvelteKit PWA)

#### Spoofing (Identity Fraud)
- **Threat**: Attacker injects fake token or bypasses MSAL.js to simulate another family member.
- **Mitigation**: 
  - MSAL.js validates issuer, audience, signature, expiry in-browser (enforced in config).
  - Tokens stored in memory or sessionStorage only (never localStorage — ASVS V2.10.2, API3:2023 Broken Object Level Authorization context).
  - Pre-commit hook validates no localStorage writes.
  - Token validation re-done on API (Defense in Depth).
- **Residual Risk**: Low. Token validation is cryptographic + network-validated.

#### Tampering (Code/State Injection)
- **Threat**: CSP bypass; attacker injects script to exfiltrate tokens or mutate state.
- **Mitigation**:
  - CSP `strict-dynamic` + nonce on all inline scripts.
  - SRI on any external scripts (Tailwind CDN, etc.).
  - No `dangerouslySetInnerHTML` or `eval` (TypeScript + lint enforces).
  - Vite build hashing on all assets.
- **Residual Risk**: Low. CSP + SRI + build hashing are well-understood mitigations.

#### Repudiation (Deny Actions)
- **Threat**: User claims they didn't perform a device edit.
- **Mitigation**:
  - All mutations logged server-side in AuditEvent table (append-only).
  - userId + timestamp + action + before/after payload in audit record.
  - Client-side request headers include correlation ID (traced to audit).
- **Residual Risk**: Very Low. Append-only audit log is immutable by design.

#### Information Disclosure (Leak Sensitive Data)
- **Threat**: Attacker exfiltrates device serials, locations, owner names, or session tokens via XSS or DevTools.
- **Mitigation**:
  - Tokens in memory/sessionStorage; no IndexedDB or localStorage.
  - No hard-coded secrets in frontend source (Trivy + gitleaks scan).
  - API rate-limited per user (TODO — covered in Phase 2).
  - HSTS + Secure flag on all cookies (Secure, HttpOnly, SameSite=Strict).
  - Output encoding default on all rendered content.
- **Residual Risk**: Medium (session tokens could be stolen via XSS until CSP/SRI deployment is hardened). Mitigated by in-memory token storage + HTTPS-only.

#### Denial of Service (Availability)
- **Threat**: Attacker floods client requests or consumes all local storage.
- **Mitigation**:
  - Service Worker caches app shell + read-only endpoints (offline mode).
  - API rate limiting per user (TODO — Phase 2).
  - Client-side request debouncing on search/filter.
  - Playwright E2E tests validate app shell caches and offline reads.
- **Residual Risk**: Low for read; Medium for mutations (no queue/replay until Phase 2 offline write).

#### Elevation of Privilege (RBAC Bypass)
- **Threat**: Viewer role sees/edits Admin-only endpoints or Member-only devices.
- **Mitigation**:
  - Roles (`Admin`, `Member`, `Viewer`) encoded in JWT claims (app_roles).
  - Every endpoint enforces role via ASP.NET Core authorization policy (default-deny).
  - UI hides edit/delete affordances for Viewer (but client-side check is NOT sufficient — API re-validates).
  - Playwright test #11 ("Role enforcement") validates Viewer cannot navigate to edit routes.
- **Residual Risk**: Very Low. Server-side policy enforcement is mandatory (ASVS V4.1.2, API2:2023).

---

### Surface 2: API (ASP.NET Core Backend)

#### Spoofing (JWT Tampering)
- **Threat**: Attacker replays, modifies, or forges a JWT to impersonate a user.
- **Mitigation**:
  - JWT signature validated via Entra ID public key (online).
  - Issuer, audience, and expiry checked on every request.
  - Refresh tokens rotated (TODO — Phase 2).
  - `nbf` (not-before) honored; no clock-skew > 5 minutes.
- **Residual Risk**: Very Low. JWT validation is cryptographic.

#### Tampering (API Injection / SQL Injection)
- **Threat**: Attacker injects SQL, NoSQL, LDAP, or command sequences in request parameters.
- **Mitigation**:
  - All queries via EF Core parameterized queries (no raw SQL with string concat).
  - FluentValidation + Zod validates input shape before handler execution.
  - ProblemDetails (RFC 7807) returned; no stack traces to client.
  - OpenAPI spec auto-generated from code (API contract is source of truth).
- **Residual Risk**: Very Low. EF Core parameterization is automatic; manual SQL forbidden by architecture.

#### Repudiation (Deny Server-Side Actions)
- **Threat**: Admin claims they didn't perform an import batch or role assignment.
- **Mitigation**:
  - AuditEvent table captures every mutation (CreateDevice, UpdateDevice, ImportBatch, RoleAssignment).
  - userId + timestamp + action + payload (before/after) immutable.
  - Audit log queryable by Admin only (Audit:Read policy).
  - Audit events correlated with correlation ID (trace back to Web request).
- **Residual Risk**: Very Low. Append-only schema + immutable columns prevent tampering.

#### Information Disclosure (Leaking Secrets / PII)
- **Threat**: Attacker exfiltrates device serials, Entra tokens, database passwords via error response or logs.
- **Mitigation**:
  - Serilog destructuring policy redacts passwords, tokens, secrets, sessionIds, EntraObjectIds (see §4 security-baseline.md).
  - Error responses (ProblemDetails) never include stack traces or database details in production.
  - API logs structured; no hard-coded secrets in source (gitleaks + Trivy scan).
  - Secrets injected via environment variables (Docker secrets / KeyVault-style abstraction).
- **Residual Risk**: Low. Serilog destructuring + ProblemDetails + environment injection mitigate most vectors.

#### Denial of Service (Availability / Rate Limiting)
- **Threat**: Attacker floods API with requests or exhausts database connection pool.
- **Mitigation**:
  - Rate limiting per user (sliding window, 100 requests/min by default — TODO Phase 2 config).
  - Database connection pooling configured in EF Core context.
  - Pagination mandatory (max 200 items/page).
  - Long-running imports (CSV) return async job ID + polling endpoint.
  - Healthchecks on API (Constitution §6.2 — Compose discipline).
- **Residual Risk**: Medium (DoS mitigation requires operational monitoring — Phase 3).

#### Elevation of Privilege (RBAC / BOLA / IDOR)
- **Threat**: Member views/edits another Member's device, or Viewer modifies reference data.
- **Mitigation**:
  - Every endpoint decorated with `[Authorize(Policy = "RequireAdmin")]` or equivalent (default-deny).
  - Application-layer checks: "Is this userId an Admin?" or "Is this device owned by a family member?"
  - No BOLA (Broken Object-Level Authorization) — device `id` in URL is validated against userId (ASVS V4.1.3, API1:2023).
  - Hicks implements per-endpoint examples; Bishop reviews.
- **Residual Risk**: Medium (requires discipline in every endpoint). Mitigated by code review + automated tests.

---

### Surface 3: Database (SQLite)

#### Spoofing (Unauthorized Access)
- **Threat**: Attacker connects directly to SQLite file and reads/modifies data.
- **Mitigation**:
  - SQLite file on Docker volume, not exposed outside container network.
  - API container is only client; no remote access.
  - File permissions: `0600` (read-write owner only).
  - (TODO Phase 3) Consider encrypting SQLite at rest (sqlite3 encryption extension).
- **Residual Risk**: Medium (file-level access requires host compromise). Mitigated by Docker isolation + host hardening (Hudson's scope).

#### Tampering (Data Mutation)
- **Threat**: Attacker directly modifies device records, audit log, or role assignments.
- **Mitigation**:
  - Soft delete only — no `DELETE` statements. Status field (Active/Retired/Disposed) tracks state.
  - AuditEvent table: columns marked `[Immutable]` in EF Core; inserts only (no updates or deletes).
  - Migrations are reviewed in code; schema changes tracked in version control.
  - Read-only replica (TODO v2 — for backups).
- **Residual Risk**: Low (AuditEvent immutability enforced at EF Core + DB schema level).

#### Information Disclosure (Data Exfiltration)
- **Threat**: Attacker gains file system access and exfiltrates SQLite database.
- **Mitigation**:
  - Database stored on encrypted host volume (encryption managed by infrastructure — Hudson's scope).
  - Backups encrypted (TODO Phase 3).
  - No PII or secrets stored (device serials are not PII; Entra tokens are not in DB).
  - Data retention policy: 7 years for audit; soft-deleted entities retained for history.
- **Residual Risk**: Medium (depends on host-level encryption). Documented as Hudson's responsibility.

#### Denial of Service (Query Performance)
- **Threat**: Attacker crafts queries causing table scans, lock contention, or high CPU.
- **Mitigation**:
  - EF Core queries validated in code review (no N+1 queries, forced `.Include()`).
  - Indexes on foreign keys, status, userId, timestamp.
  - Pagination enforced (max 200 items).
  - Slow-query logging via Serilog (OpenTelemetry).
- **Residual Risk**: Low (EF Core discipline + indexes + pagination).

#### Elevation of Privilege (Unauthorized DB Access)
- **Threat**: Database connection string leaked; attacker connects as API user and modifies all data.
- **Mitigation**:
  - Connection string in environment variables (never in source).
  - gitleaks pre-commit hook.
  - Docker secrets for production (sealed-secrets or similar).
  - Read-only queries use dedicated SQL Server login (TODO v2).
- **Residual Risk**: Medium (depends on secret management discipline). Mitigated by gitleaks + ADR process for credential rotation.

---

### Surface 4: Auth Provider (Microsoft Entra ID — Workforce tenant)

#### Spoofing (Token Issuance / OIDC Vulnerabilities)
- **Threat**: Attacker obtains Entra token via phishing, malware, or OIDC misconfiguration.
- **Mitigation**:
  - OIDC + PKCE (authorization code flow, never implicit).
  - Redirect URI whitelist in Entra app registration (no wildcards).
  - Client secret stored in Docker secrets (never in frontend).
  - Token lifetime ≤ 1 hour (Constitution §5.1).
  - Refresh tokens rotated every N requests (TODO Phase 2).
- **Residual Risk**: Very Low (OIDC + PKCE is industry standard). Phishing risk mitigated by Entra MFA (optional for family members — TBD Phase 2).

#### Tampering (Token Mutation)
- **Threat**: Attacker modifies JWT payload (role claims, oid) after issuance.
- **Mitigation**:
  - JWT signature validated cryptographically (Entra public key).
  - Role claims (`app_roles`) issued by Entra only; not derived from client input.
  - If roles need to be updated, Admin modifies in Entra or via local override table (lookup on first login).
- **Residual Risk**: Very Low. JWT signature is cryptographic.

#### Repudiation (Deny Sign-In Events)
- **Threat**: Attacker signs in as family member; member denies access.
- **Mitigation**:
  - Sign-in events logged in AuditEvent table (action: "SignIn", userId, timestamp, source IP).
  - Failed sign-ins logged separately (action: "SignInFailed", reason, timestamp).
  - Audit queryable by Admin (Audit:Read policy).
- **Residual Risk**: Very Low. Append-only audit log prevents denial.

#### Information Disclosure (Token / Secret Leakage)
- **Threat**: Entra client secret exfiltrated; attacker requests tokens on behalf of any family member.
- **Mitigation**:
  - Client secret in Docker secrets / sealed-secrets (never in source or .env).
  - gitleaks pre-commit hook scans for credentials.
  - Secret rotated every 6 months (ADR + procedure documentation).
- **Residual Risk**: Medium (depends on secret hygiene). Mitigated by pre-commit hook + ADR process.

#### Denial of Service (Token Request Flooding)
- **Threat**: Attacker floods Entra token endpoint, degrading sign-in for family members.
- **Mitigation**:
  - Token caching in memory (don't re-request if not expired).
  - Entra rate-limiting (Microsoft's responsibility).
  - Client-side rate-limiting on sign-in button (debounce).
- **Residual Risk**: Very Low. Entra is a managed service with built-in DDoS protection.

#### Elevation of Privilege (Role Tampering)
- **Threat**: Attacker modifies app_roles claim to grant Admin access.
- **Mitigation**:
  - Roles issued by Entra (via app role assignment in Azure Portal) — attacker cannot modify in flight (JWT signature).
  - Fallback: lookup user in local `Owner` table on first login, check role column; override Entra claim if admin configures.
  - Role assignment logged in AuditEvent (action: "RoleAssigned" or "RoleRevoked").
- **Residual Risk**: Very Low (JWT signature prevents tampering; Entra role assignment is out-of-band).

---

### Surface 5: Container Host (Docker on Home LAN)

#### Spoofing (Container / Host Identity)
- **Threat**: Attacker deploys rogue container with same name, intercepting traffic.
- **Mitigation**:
  - Compose file specifies image digest (not `latest` tag) — Constitution §6.1.
  - Image pulled from GHCR (GitHub Container Registry) — immutable by digest.
  - Host firewall rules restrict traffic to Compose network only.
- **Residual Risk**: Low (host firewall + image digest immutability).

#### Tampering (Container Image / Volume Modification)
- **Threat**: Attacker modifies container layers, SQLite volume, or config at rest.
- **Mitigation**:
  - Multi-stage Dockerfile; non-root user in container.
  - Read-only root filesystem (where feasible) — Constitution §6.1.
  - Volumes mounted with explicit permissions (0600 for SQLite).
  - Image scanned with Trivy (security vulnerabilities + secrets).
- **Residual Risk**: Medium (depends on host OS hardening — Hudson's scope).

#### Repudiation (Container Restart / History Loss)
- **Threat**: Container crashes; attacker claims logs don't exist.
- **Mitigation**:
  - Logs streamed to host volume (persistent across restarts).
  - Docker healthchecks on every service (Compose discipline).
  - Compose restart policy: `unless-stopped` (Constitution §6.2).
- **Residual Risk**: Very Low. Persistent logs + healthchecks provide audit trail.

#### Information Disclosure (Container Breakout / Secrets Exfiltration)
- **Threat**: Attacker breaks out of container, accesses host file system, or reads environment variables.
- **Mitigation**:
  - Non-root user in container (UID 1000 or higher).
  - Container capabilities dropped (CAP_NET_ADMIN, etc. — Compose spec).
  - Secrets in Docker secrets, not environment (where feasible) — Constitution §6.4.
  - Host kernel kept up-to-date (Hudson responsibility).
- **Residual Risk**: Medium (host OS security is outside Bishop's scope; documented as Hudson's responsibility).

#### Denial of Service (Resource Exhaustion)
- **Threat**: Container consumes all CPU/memory, crashing host.
- **Mitigation**:
  - Resource limits in Compose: `cpus: 2`, `mem_limit: 2G` (per service) — Constitution §6.2.
  - Database connection pooling.
  - Pagination + rate limiting.
- **Residual Risk**: Low (resource limits + monitoring).

#### Elevation of Privilege (Root Escape)
- **Threat**: Attacker gains root inside container, or escapes to host root.
- **Mitigation**:
  - Non-root user enforced (Dockerfile `USER 1000:1000`).
  - No `sudo` or `sudo` setuid binaries (distroless base — Constitution §6.1).
  - SELinux or AppArmor enabled on host (Hudson scope).
- **Residual Risk**: Very Low (non-root + distroless). Residual depends on kernel vulnerabilities (host OS patching — Hudson).

---

### Surface 6: Reverse Proxy (TLS Termination — External)

#### Spoofing (HTTPS Downgrade / MITM)
- **Threat**: Attacker intercepts traffic between client and proxy, or proxy-to-API.
- **Mitigation**:
  - TLS 1.2 minimum (Constitution §5.4).
  - HSTS + preload list.
  - Internal proxy-to-API: Docker network (never over WAN).
  - Certificate pinning (optional, recommend in v2).
- **Residual Risk**: Very Low. TLS 1.2 + HSTS + internal network provide strong protection.

#### Tampering (Response Tampering)
- **Threat**: Proxy modifies API response (e.g., injects malicious script).
- **Mitigation**:
  - SRI on frontend scripts.
  - TLS authenticated (certificate pinning optional).
  - API response headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`.
- **Residual Risk**: Very Low. SRI + CSP prevent script injection.

#### Information Disclosure (Logs / Traffic Inspection)
- **Threat**: Reverse proxy logs request/response bodies (including tokens, device data).
- **Mitigation**:
  - Proxy access logs sanitized (redact Authorization header, sensitive query params).
  - Logs stored securely (Hudson responsibility).
- **Residual Risk**: Medium (depends on proxy configuration & Hudson's ops discipline). TODO: add to Phase 3 hardening checklist.

#### Denial of Service (Slow Loris / Connection Pooling)
- **Threat**: Attacker holds many slow connections, exhausting proxy file descriptors.
- **Mitigation**:
  - Proxy connection timeouts (typically 60s idle).
  - Rate limiting at proxy (optional WAF).
  - Backend connection pool limits.
- **Residual Risk**: Low (proxy + backend limits mitigate most attacks).

#### Elevation of Privilege (Proxy Misconfiguration)
- **Threat**: Proxy forwards requests to unintended backend, or allows smuggling.
- **Mitigation**:
  - Explicit backend target (no dynamic routing).
  - HTTP request smuggling mitigations: normalize headers, disable keep-alive if needed.
  - Documentation in `docs/deployment/caddy.md` (and others) for correct proxy setup.
- **Residual Risk**: Low (documented setup + code review).

---

### Surface 7: Backup Destination (TBD)

#### Spoofing (Backup Tampering)
- **Threat**: Attacker modifies backup files to corrupt recovery.
- **Mitigation**:
  - Backup signed (HMAC or GPG) — TODO Phase 3.
  - Backup stored on read-only volume or secure remote (specification TBD).
- **Residual Risk**: Medium (depends on final backup design — PRD §14 to be resolved).

#### Information Disclosure (Backup Leakage)
- **Threat**: Backup file left on public share or S3 bucket.
- **Mitigation**:
  - Encrypted at rest (full-disk or file-level).
  - Access control: Admin only.
  - Off-site backups: encrypted end-to-end (TBD).
- **Residual Risk**: Medium (depends on final backup design — Phase 3 decision required).

#### Denial of Service (Backup Failure / Recovery Failure)
- **Threat**: Attacker deletes backup; RTO > SLA.
- **Mitigation**:
  - 3-2-1 rule (TBD): 3 copies, 2 different media, 1 off-site.
  - Quarterly restore drills (PLC §12).
  - Backup verification logs.
- **Residual Risk**: Low (if 3-2-1 rule implemented). TBD Phase 3.

---

## 3. Key Cross-Cutting Threats

### Threat: Supply Chain / Dependency Compromise
- **Vector**: Attacker compromises npm or NuGet package, injects backdoor.
- **Mitigation**:
  - `dotnet list package --vulnerable` (Constitution §5.8).
  - `npm audit --audit-level=moderate` (Constitution §5.8).
  - Dependabot / Renovate enabled (alerts on new vulns).
  - Pinned versions + lockfiles in source control.
  - Review PRs from Dependabot (prioritize high-severity).
- **Residual Risk**: Low (pinned versions + audit cadence + monitoring).

### Threat: Insider Threat (Family Member with Elevated Privilege)
- **Vector**: Family Admin uses access maliciously or accidentally deletes all data.
- **Mitigation**:
  - Audit log immutable (append-only).
  - Role-based access control (Admin ≠ unlimited).
  - Backup + restore drills (quarterly).
  - Data recovery from backup tested.
- **Residual Risk**: Medium (audit trail prevents denial, but not data loss in the moment). Mitigated by backups.

### Threat: Physical Host Compromise
- **Vector**: Attacker gains physical access to home LAN, plugs into switch, or compromises router.
- **Mitigation**:
  - Network segmentation (Compose networks: `app`, `data`).
  - Host firewall (iptables / ufw).
  - VLAN for app stack (recommended in deployment docs).
  - Regular host OS patching (Hudson scope).
- **Residual Risk**: Medium (depends on home network security posture — outside v1 scope).

### Threat: Entra Outage → Total Authentication Lockout
- **Vector**: Microsoft Entra (Workforce tenant) is unreachable, misconfigured, or the household admin loses access to the directory itself. Without a fallback, every household user is locked out of the system, including the operator who would normally fix it.
- **Mitigation** (shipped in F025 v1b per ADR D-140):
  - Env-var-seeded local admin account (`LocalAdminSeedHostedService`) — bootstrappable without a working SPA or Entra reachability.
  - Argon2id password hashing (OWASP 2025 baseline `m=19 456 KiB, t=2, p=1`).
  - `TechInventoryAuth` PolicyScheme routes by JWT `iss` so local-issued tokens (issuer `techinventory-local`, 8 h) coexist with Entra tokens without disabling Entra.
  - Force-rotation middleware: any local principal with `must_change_password=true` is blocked from everything except `POST /api/v1/auth/local/change-password`, so the seed password cannot be reused.
  - CRITICAL log line on every startup while seed env vars are present — operationally loud nag to clear the seed once Entra is healthy again.
  - Operator runbook in `docs/operations.md` § "Break-glass local admin (F025 v1b)".
- **Residual Risk**: Low–Medium. The seed password becomes a high-value target while it exists; mitigated by force-rotation on first login + Argon2id + the startup nag. Rate limiting on `/api/v1/auth/local/login` and per-account lockout enforcement are deferred to F025b (tracked in `specs/_backlog/F025b-local-admin-power.md`); until then, defense relies on Argon2id cost and the small attack surface of a homelab service behind NPM.

### Threat: Local Account Becomes Permanent Backdoor
- **Vector**: Operator seeds the local admin during bootstrap and never decommissions it. Seed env vars remain in `.env`, the account stays Active, and over time the password drifts toward weakness or compromise.
- **Mitigation**:
  - Runbook (`docs/operations.md`) explicitly calls out the post-Entra cleanup steps: clear `Auth__Local__Seed*` env vars, restart, optionally `UPDATE Owner SET IsActive=0 WHERE IsLocalAccount=1` once Entra is fully healthy.
  - CRITICAL-level log on every startup while seed vars are present (visible to anyone tailing logs).
  - `Auth__Local__SeedAllowInProd` knob must be explicitly set in `Production` — the default fails closed.
  - F025b will add an admin UI for deactivating local accounts without a SQL shell.
- **Residual Risk**: Medium. Process discipline is the primary control until F025b ships.

---

## 4. Summary by Risk Level

### Critical (0 known)
None identified. Proceed to Phase 2 implementation with confidence in auth design.

### High (0 known)
None identified.

### Medium
- Container breakout → host compromise (mitigated by non-root + distroless + kernel patching).
- Supply chain attack (mitigated by audit + pinned versions).
- Backup security TBD (Phase 3).

### Low
- BOLA in endpoints (mitigated by code review + automated tests).
- Reverse proxy misconfiguration (mitigated by deployment docs).

---

## 5. OWASP ASVS L2 & API Top 10 Alignment

| ASVS Section | Control | Status | Notes |
|---|---|---|---|
| V2.1 | Password policy | ✓ N/A | Using Entra ID; no local passwords for users. |
| V2.10 | Credential agility | ✓ | Token rotation in Phase 2. |
| V4.1 | Authorization architecture | ✓ | Default-deny + policy per endpoint. |
| V4.1.2 | Access control policy test | ✓ | Playwright test #11. |
| V4.1.3 | Access control bypass test | ✓ | BOLA tested per endpoint (Phase 1 API spec). |
| V6.1 | Data classification | ✓ | Device data non-PII; Entra tokens not stored. |
| V8.1 | Defect handling | ✓ | Exceptions only for exceptional conditions. |

| API Top 10 (2023) | Risk | Status | Notes |
|---|---|---|---|
| API1 | BOLA | ✓ | Resource-level checks in Application layer. |
| API2 | AuthN | ✓ | OIDC + PKCE, JWT validation. |
| API3 | BrokenObjectLevelAuthz | ✓ | No client-side-only checks; API re-validates. |
| API4 | Unrestricted Resource Consumption | ✓ | Rate limiting, pagination. |
| API5 | BrokenFunctionLevelAuthz | ✓ | Default-deny on every endpoint. |
| API6 | Unrestricted Access to Sensitive Business Flows | ✓ | Roles-based gating. |
| API7 | Mass Assignment | ✓ | FluentValidation + Zod on inputs. |
| API8 | API Security Misconfiguration | ✓ | OpenAPI spec + tests validate. |
| API9 | ImproperInventoryManagement | ✓ | API versioning `/api/v1/*`. |
| API10 | UnsafeConsumption of APIs | ✓ | No third-party APIs without ADR. |

---

## 6. Next Steps

1. **Phase 1 (Core API)**: Implement CRUD endpoints with authz policies stubs (TODO Phase 2).
2. **Phase 2 (Auth)**: Wire OIDC + Entra, implement role-based policies, audit log.
3. **Phase 3 (Infrastructure)**: Reverse proxy hardening, backup encryption, secret rotation SOP.
4. **Phase 5 (Hardening)**: Threat model review post-implementation, penetration testing (optional v2).

---

## 7. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-18 | Bishop | Initial STRIDE analysis; seven surfaces; ASVS L2 alignment. |
| 1.1 | 2026-05-19 | Scribe | Surface 4 updated for Workforce tenant + F025 v1b local fallback; added cross-cutting threats "Entra Outage → Total Authentication Lockout" and "Local Account Becomes Permanent Backdoor" per ADR D-140. |

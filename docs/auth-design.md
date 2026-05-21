# Authentication Design — Workforce Entra ID Tenant

> **Phase note (added 2026-05-19)**: Authored before the 2026-05-19 PRD §13
> phase-model rewrite. The Entra OIDC + PKCE design described here **shipped
> as part of canonical P2 — Frontend MVP + Auth** (production-validated
> 2026-05-19). Internal references to "Phase 2 / Phase 3 / Phase 4 / Post-
> Phase 2" labels reflect the original PRD §13 numbering; under the rewritten
> model, post-MVP auth follow-ups (MFA enforcement, token-revocation API,
> offline token caching) live in **P4 — Continuous Iteration** via
> `specs/_backlog/`. The `specs/002-auth-entra` directory referenced below
> was never created — auth shipped inside `specs/002-frontend-mvp/`.

**Authorship**: Bishop (Security & Auth Specialist)  
**Related Spec**: `specs/002-frontend-mvp/` (shipped as canonical **P2**; originally planned as `specs/002-auth-entra` / "Phase 2, Weeks 6–7")  
**Authority**: PRD §F5; Constitution §5.1; Decision: `copilot-directive-2026-05-18T140924Z-entra-tenant.md`

---

## 1. Tenant Choice: Workforce (Household Existing Tenant)

**Decision**: Use the household's **existing Workforce Entra ID tenant** (same tenant used for Office, Teams, etc.).

### Why Workforce, Not External ID

Family members are already provisioned in the household Entra tenant. Registering Tech Inventory as an additional app in the same tenant simplifies operations:
- No new tenant to manage; one less SLA to monitor.
- Users already have Entra credentials and MFA enrolled.
- Group memberships (Admin, Member, Viewer) already defined by household IT practice.
- Aligns with Brian's self-hosted, low-ops philosophy.

---

## 2. App Registration in Workforce Tenant

### Azure Portal Setup (Admin, Phase 2 Week 1)

1. **Register App**:
   - Azure Portal → Entra ID → App registrations → New registration.
   - Name: `Tech Inventory`
   - Supported account types: **Accounts in this organizational directory only** (single tenant).
   - Redirect URI: `https://app.example.com/auth/callback` (exact match).

2. **Configure Certificates & Secrets**:
   - Generate client secret (store in Docker `.env` secrets volume; never commit).
   - Secret expiry: 2 years; rotation reminder 30 days before expiry.

3. **Set API Permissions**:
   - Delegated: `openid`, `profile`, `email`, `offline_access` (standard OIDC scope).
   - No Graph API permissions needed for v1.

4. **Define App Roles** (or use Security Groups):
   - **Option A — App Roles** (recommended for simplicity):
     ```
     Admin    (value: admin)
     Member   (value: member)
     Viewer   (value: viewer)
     ```
   - **Option B — Security Groups**:
     - Create three groups in Entra: `Tech-Inventory-Admin`, `Tech-Inventory-Member`, `Tech-Inventory-Viewer`.
     - Configure app to read group membership from JWT claims.
   - **Recommendation**: Use App Roles for v1 (simpler, no sync). Migrate to Groups later if needed.

5. **Configure Token Claims**:
   - Add custom claim: `app_roles` (list of role values assigned to user).
   - Ensure JWT includes: `oid` (Entra ObjectId), `email`, `name`, `app_roles`.

### MSAL.js Configuration

**Frontend (`src/TechInventory.Web/src/lib/auth/config.ts`)**:

```typescript
export const msalConfig = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID}`,
    redirectUri: `${window.location.origin}/auth/callback`,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage", // NOT localStorage
    storeAuthStateInCookie: false, // tokens in memory only
  },
};

export const loginRequest = {
  scopes: ["openid", "profile", "email", "offline_access"],
};
```

**Environment Variables** (`.env.local`, not committed):
```
VITE_ENTRA_CLIENT_ID=<client-id-from-azure-portal>
VITE_ENTRA_TENANT_ID=<tenant-id-from-azure-portal>
```

---

## 3. Authentication Flow: OIDC + PKCE

### Diagram (Text)

```
Family Member (SvelteKit PWA)
         ↓
    [Sign In Button]
         ↓
  MSAL.js redirects to:
  https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize
    ?client_id=<CLIENT_ID>
    &response_type=code
    &scope=openid profile email offline_access
    &code_challenge=<BASE64-URL(SHA256(random))>
    &code_challenge_method=S256
    &redirect_uri=https://app.example.com/auth/callback
         ↓
  [Entra ID Login (Household User)]
         ↓
  Redirect back to:
  https://app.example.com/auth/callback
    ?code=<AUTH_CODE>
    &session_state=<STATE>
         ↓
  Client-side handler (SvelteKit):
    1. Verify state token
    2. Extract code
    3. Store code in memory (NOT localStorage)
    4. Call ASP.NET Core backend: POST /api/v1/auth/exchange
         ↓
  Backend (/api/v1/auth/exchange):
    1. Validate code + PKCE code_verifier
    2. Call Entra token endpoint (server-to-server, client secret in Docker secrets)
    3. Receive JWT (access token + refresh token)
    4. Validate JWT: issuer, audience, signature, expiry
    5. Extract claims: oid (Entra ObjectId), app_roles, email, name
    6. Look up user in local Owner table by EntraObjectId
    7. If not found, create Owner record (onboard on first sign-in)
    8. Return JWT to client (in response body; client stores in memory)
         ↓
  Client stores JWT in memory / sessionStorage
    - NEVER localStorage (ESLint + pre-commit enforced)
    - Set SameSite=Strict cookie as backup (API validates both)
         ↓
  All subsequent requests include: Authorization: Bearer <JWT>
         ↓
  API validates JWT on every request:
    - Signature (cryptographic verification)
    - Issuer: matches tenant token endpoint
    - Audience: matches app client ID
    - Expiry (must not be expired)
    - Not-before (nbf)
         ↓
  [Access Granted or 401 Unauthorized]
```

### Key Points

- **Tenant Authority**: All sign-in and token operations route through `login.microsoftonline.com/{tenant-id}`.
- **PKCE (Proof Key for Code Exchange)**: Mitigates authorization code interception. MSAL.js handles generation automatically.
- **Scope**: `openid profile email offline_access` (minimal; no Graph API scope).
- **Redirect URI**: Exact match required in Entra app registration (no wildcards).
- **Token Lifetime**: 1 hour (access token). Refresh token: 14 days (sliding window).
- **Refresh Strategy**: Client calls `acquireTokenSilent()` before expiry; automatic via MSAL.js.
- **Sign-Out**: Clears token from memory + sessionStorage + calls `/logout` on Entra. Clear cookies. Redirect to home page.

---

## 4. Role Assignment Strategy

**Recommendation**: **App Roles (assigned in Entra, stored in JWT).**

### Rationale

- **Entra App Roles**: Admin assigns roles directly in Azure Portal (no code deploy needed).
- **JWT Claim**: `app_roles` included on every token; API validates at request time.
- **Workflow**: Simple for household scale; no local override needed (Admin manages via Azure).

### Implementation Detail

1. **Entra Setup** (Admin, Azure Portal):
   - Register app roles in Entra app manifest:
     ```json
     "appRoles": [
       { "id": "550e8400-e29b-41d4-a716-446655440000", "value": "admin", "displayName": "Admin" },
       { "id": "550e8400-e29b-41d4-a716-446655440001", "value": "member", "displayName": "Member" },
       { "id": "550e8400-e29b-41d4-a716-446655440002", "value": "viewer", "displayName": "Viewer" }
     ]
     ```
   - Assign household members to roles via Entra → Enterprise Applications → Tech Inventory → Users and groups.

2. **JWT Claims**:
   - Entra includes `app_roles` claim in access token:
     ```json
     {
       "oid": "00000000-0000-0000-0000-000000000000",
       "email": "alice@household.onmicrosoft.com",
       "app_roles": ["member"]
     }
     ```

3. **Backend Lookup** (First login):
   - Extract `oid` + `email` + `app_roles` from JWT.
   - Look up user in local `Owner` table by `EntraObjectId`.
   - If not found:
     - Insert new `Owner` record: `EntraObjectId`, `email`, `role` (from JWT `app_roles[0]`).
     - Log: `AuditEvent { action: "UserFirstLogin", userId: <new_owner_id>, ... }`.
   - If found:
     - Validate `email` matches (Entra email immutable; warn if mismatch).
     - Use local `role` column for authorization (mirrors JWT for safety).
     - Update `LastLoginAt` timestamp.

4. **Role Override** (Optional, Admin local UI):
   - Admin endpoint: `PATCH /api/v1/admin/users/{userId}/role` (Admin:Write).
   - Change `Owner.role` in DB (overrides Entra role for this app only).
   - Log: `AuditEvent { action: "RoleAssigned", actor: admin_id, entity: user_id, role: "Member", ... }`.
   - **Note**: This local override is a safety valve; Entra app role is the source of truth.

---

## 5. Session & Token Strategy

### Token Lifetimes

| Token | Lifetime | Strategy |
|-------|----------|----------|
| **Access Token (JWT)** | 1 hour | Client requests new one 5 min before expiry via `acquireTokenSilent()`. |
| **Refresh Token** | 14 days (sliding) | Automatically rotated by Entra on each refresh. |
| **Session Cookie** | 8 hours idle (Secure, HttpOnly, SameSite=Strict) | Backup; API validates bearer token + cookie. |

### Refresh Strategy

1. **MSAL.js Caching**:
   - Client caches access token in memory.
   - On route change or 55 minutes elapsed, client calls `acquireTokenSilent()`.
   - MSAL.js automatically refreshes if stale (transparent).

2. **Backend Validation**:
   - Every API request validates JWT expiry (`exp` claim).
   - If expired, return `401 Unauthorized`.
   - Client catches 401 → call `/api/v1/auth/refresh` (if refresh token valid).
   - Backend calls Entra token endpoint (server-to-server).
   - Return new access token.
   - Client retries original request.

### Sign-Out Semantics

#### Single Device

1. User clicks "Sign Out" button.
2. Client clears JWT from memory + sessionStorage.
3. Client clears cookies (set `Set-Cookie` with expiry = now).
4. Client POST `/api/v1/auth/logout` (no JWT required).
5. Backend logs: `AuditEvent { action: "SignOut", userId, timestamp, ... }`.
6. Client redirects to `/` (home).

#### All Devices (Admin Override)

1. Admin goes to User Management page.
2. Finds user, clicks "Force Sign-Out All Devices".
3. Admin POST `/api/v1/admin/users/{userId}/revoke-tokens` (Admin:Write).
4. Backend:
   - Calls Entra to revoke all tokens for that user (if Entra supports; TODO: verify).
   - Or: Update local `Owner.TokenRevocationDate = now()`.
   - On next token validation, reject if issued before revocation date.
   - Log: `AuditEvent { action: "TokensRevoked", actor: admin_id, entity: user_id, ... }`.
5. All client instances detect 401 on next request → sign-out locally.

---

## 6. Silent SSO Bootstrap (F038)

**Status**: F038 shipped. The app now auto-logs returning users via cached Entra sessions.

### Design

On app load (root `+layout.svelte`), **before rendering the page**:

1. Call `handleRedirectPromise()` to check for a post-sign-in callback.
2. Attempt `msalInstance.acquireTokenSilent()` with the cached account (if one exists).
3. If acquisition succeeds within **3 seconds**, hydrate the auth store via `GET /api/v1/owners/me` and route to `/devices`.
4. If acquisition fails (no cached account, `interaction_required`, `login_required`, `consent_required`) or times out, treat as unauthenticated and show the login page.
5. Local break-glass sessions (from F025 v1b) continue to hydrate from sessionStorage and skip Entra entirely.

### UX Flow

| Scenario | Result |
|----------|--------|
| **Returning user** (MSAL cache valid) | 3s silent auth → dashboard (no login page shown) |
| **Returning user** (MSAL cache expired) | 3s timeout → login page shown (user clicks MSAL button → completes OAuth → dashboard) |
| **First visit** (no MSAL cache) | 3s timeout → login page shown |
| **Entra outage** (silent fails fast) | 3s timeout → login page shown (user can switch to local account) |
| **Local break-glass user** | sessionStorage token checked on root layout → dashboard (Entra skipped) |

### Multi-Tab Behavior

- When **tab A** signs out → all tabs' MSAL caches are cleared (browser-level storage).
- When **tab B** signs in → MSAL cache is updated.
- If **tab A** was idle during sign-in in tab B → next route change in tab A triggers silent acquisition, which succeeds because tab B's sign-in updated the shared MSAL cache.
- Result: sign-out is global; sign-in is global; no ghost sessions between tabs.

### Timeout & Fallback

The 3-second timeout is a UX balance:
- **Too short** (<1s): fast connection users get needlessly prompted; PWA feels sluggish.
- **Too long** (>5s): users on slow networks wait too long before being able to manually sign in.
- **3 seconds**: typical silent acquisition completes in <500ms on a cached account; timeout allows for network jitter and offline detection.

If silent acquisition doesn't complete in 3s, the app treats that as "no valid session" and shows the login page. The user can:
- Wait for page to load fully and click "Sign In" to try again.
- Switch to "Use a local account instead" if Entra is down.

---

## 7. Local Admin Bootstrap (PRD §F5)

**Status**: F025 v1b shipped. The original spec proposal in this section
described a bcrypt-hashed local account with a web bootstrap page; the
implementation that actually shipped uses Argon2id + an env-var driven
hosted-service seeder. The authoritative description of the shipped
behaviour is **ADR D-140** in [`.squad/decisions.md`](../.squad/decisions.md)
and the operator runbook in [`docs/operations.md`](operations.md#break-glass-local-admin-f025-v1b).
Carved-out items (admin CRUD UI, lockout enforcement, refresh tokens,
self-service convert-to-local) are tracked in
[`specs/_backlog/F025b-local-admin-power.md`](../specs/_backlog/F025b-local-admin-power.md).

### What shipped (F025 v1b)

| Concern                  | Implementation                                                                                                                                     |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| Password hashing         | **Argon2id** via `Konscious.Security.Cryptography.Argon2` 1.3.1 — OWASP 2025 baseline `m=19 456 KiB, t=2, p=1`, salt 16 B, hash 32 B. Encoded `$argon2id$v=19$m=…,t=…,p=…$saltB64$hashB64`. Tunable via `Auth__Local__Argon2__*`. Verification is fixed-time; an unknown algorithm tag fails closed. |
| Token format             | HS256 JWT, issuer `techinventory-local`, audience `Auth__Local__Audience`, 8 h lifetime. Claims: `sub` / `oid` / `name` / `preferred_username` / `role` / `auth_method=local` / `must_change_password`. |
| Token routing            | A `PolicyScheme` named `TechInventoryAuth` sniffs the incoming JWT `iss` claim and forwards to either the existing Entra `JwtBearer` scheme or the new Local `JwtBearer` scheme. Both schemes set `ClaimTypes.Role`, so existing `[Authorize(Roles=…)]` attributes are unchanged. |
| Public endpoints         | `POST /api/v1/auth/local/login` (anonymous; uniform `401 InvalidCredentials` for both unknown user and wrong password — no enumeration) and `POST /api/v1/auth/local/change-password` (requires a local-issued token). |
| Force-rotation middleware | Runs after `UseAuthentication`. Any local-auth principal with `must_change_password=true` receives `403 PasswordChangeRequired` on every endpoint except `/api/v1/auth/local/change-password`. |
| Bootstrap                | `LocalAdminSeedHostedService` — env-var-driven, idempotent. Refuses to seed in `Production` unless `Auth__Local__SeedAllowInProd=true`. Logs a CRITICAL warning on every startup while seed env vars are present, so leaving the seed configured in prod is loud. |
| Frontend                 | sessionStorage token (Constitution §6 / D-002) under keys `ti_local_token` + `ti_local_meta`. "Use a local account instead" toggle on the sign-in page. Dedicated `/auth/change-password` route guarded by a root-layout `$effect` that redirects there when `must_change_password=true`. Local sign-in preferred over MSAL when a local token is present. |

### Operator runbook

The end-to-end seed → rotate → decommission flow lives in
[`docs/operations.md` § "Break-glass local admin (F025 v1b)"](operations.md#break-glass-local-admin-f025-v1b)
and covers: required env vars, idempotent re-seed for password reset,
production safety knob, post-rotation cleanup, and the security properties
the v1b slice guarantees. The deployment-side knobs (signing key generation,
which env vars to set, NPM forwarding) are in
[`docs/deployment.md` § 7](deployment.md).

### Out of scope for v1b (deferred to F025b)

- Admin UI for managing local accounts (CRUD, reset, deactivate).
- Per-account lockout enforcement (`FailedAttemptCount` / `LockoutUntilUtc`
  columns exist but are not checked at login yet).
- IP-based rate limiting on `/api/v1/auth/local/login`.
- Refresh tokens / sliding sessions (the 8 h JWT is the whole story).
- Soft-delete semantics + last-Admin guard.
- Self-service "convert me to local" for an existing Entra admin.

### Threats addressed by v1b

- **Entra tenant outage → total lockout** — the original single point of
  failure that motivated F025. A bootstrapped local admin can sign in,
  perform recovery, and rotate Entra config without depending on Entra
  being reachable. Residual: the seed password is now a high-value target;
  mitigated by force-rotation on first login + Argon2id + the CRITICAL log
  line that nags the operator to clear the seed env vars.
- **Username enumeration** — uniform 401 responses; no per-error distinction.
- **Hash cracking** — Argon2id with OWASP 2025 parameters; tunable upward
  via `Auth__Local__Argon2__*` once we benchmark prod hardware (tracked in
  F025b).

### What this supersedes from the original §6 proposal

- The original §6 described `bcrypt` cost-factor ≥ 12; shipped is Argon2id
  per the rationale above.
- The original §6 described a `/admin/bootstrap` web page that
  self-destructs after first use; shipped is an env-var-driven hosted
  service so the bootstrap path works even when the SPA is broken (which
  is precisely when you need break-glass).
- The original §6 endpoint paths (`/api/v1/auth/local-login`,
  `/api/v1/auth/bootstrap`) are not what shipped — see the table above for
  actual paths.

---

## 8. Threats Addressed

| Surface | Threat | Addressed By |
|---------|--------|--------------|
| Web Client | Token spoofing/theft | MSAL.js validation + in-memory storage (not localStorage) |
| API | JWT tampering | Signature validation (cryptographic) + issuer/audience check |
| API | RBAC bypass | Authorization policy on every endpoint (default-deny) |
| API | BOLA (Broken Object-Level Authorization) | Resource-level checks in Application layer |
| Auth Provider | OIDC misconfiguration | PKCE + redirect URI whitelist + client secret in Docker secrets |
| Auth Provider | Token leakage | Refresh token rotation + token revocation support |
| Auth Provider | Phishing | MFA available in Entra (Admin can configure) |
| Bootstrap | Unauthorized first-run access | Bootstrap endpoint self-destructs after first use; rate-limited |
| Session | Session fixation | Session cookie SameSite=Strict + in-memory JWT |

(Cross-reference: `docs/threat-model.md` Surfaces 1–4.)

---

## 8. Open Questions for Brian

1. **MFA enforcement?**
   - Entra supports TOTP, phone call, email OTP, Windows Hello.
   - Recommend: Mandate MFA for Admin; optional for Member/Viewer.
   - **Decision needed**: Enforcement level at v1 launch?

2. **Token revocation (sign-out all devices)?**
   - Verify Entra supports token revocation endpoint.
   - If not available: Implement local `TokenRevocationDate` column (described in §5).
   - **Action**: Spike in Phase 2 Week 1.

3. **Offline sign-in fallback?**
   - If Entra unavailable: fall back to cached JWT (TTL window) or local account?
   - Recommend: Cache JWT with 24h TTL (PWA scenario, PRD §22).
   - **Decision needed**: Fallback behavior for v1?

4. **Audit retention?**
   - Constitution §5.7 mandates 7 years for all audit events.
   - Auth events (sign-in, sign-out, role changes) to be retained indefinitely.
   - Query endpoint Admin-only with time range filters.
   - **Confirm**: OK to retain auth events indefinitely?

---

## 9. Implementation Sequence (Phase 2)

**Hicks (Backend Developer) owns this work.**

1. **Week 6**:
   - Register Tech Inventory app in household Entra tenant (Azure Portal spike).
   - Define app roles: `admin`, `member`, `viewer`.
   - Design token endpoint `/api/v1/auth/exchange` + JWT validation (Bearer token).
   - Implement `/api/v1/auth/bootstrap` and `/api/v1/auth/local-login` endpoints (one-time bootstrap).

2. **Week 7**:
   - Implement MSAL.js client config (Vasquez; targets household tenant login endpoint).
   - Implement owner lookup by `EntraObjectId` (first-login auto-onboard).
   - Implement audit logging for auth events: sign-in, sign-out, role assignment, bootstrap.
   - Implement authorization policies on all endpoints: default-deny + role-based checks.
   - Playwright tests (4 critical journeys):
     - Sign in (valid household user).
     - Sign in denied (user not in Entra).
     - Role enforcement (Member cannot delete; Viewer cannot edit).
     - Sign-out on all devices (Admin override).

3. **Post-Phase 2** (v1.1, Phase 3+):
   - MFA enforcement (TOTP for Admin).
   - Offline token caching (PWA, Phase 4).
   - Token revocation API (Phase 3, if Entra supports).

---

## 10. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-18 | Bishop | Initial proposal: External ID recommended; OIDC+PKCE flow; role strategy; bootstrap. |
| 2.0 | 2026-05-18 | Bishop | **REVISED** per decision `copilot-directive-2026-05-18T140924Z-entra-tenant.md`: Use household Workforce tenant (not External ID). Updated app registration flow, MSAL.js config, role mapping, and bootstrap sequence. Removed External ID rationale. Added implementation sequence for Hicks. |
| 2.1 | 2026-05-19 | Scribe | **§6 rewritten to match F025 v1b reality**: Argon2id (not bcrypt), env-var hosted-service seed (not web bootstrap page), endpoints at `/api/v1/auth/local/{login,change-password}`, force-rotation middleware, `TechInventoryAuth` PolicyScheme. Cross-linked ADR D-140, `operations.md`, `deployment.md`, and `specs/_backlog/F025b-local-admin-power.md`. |

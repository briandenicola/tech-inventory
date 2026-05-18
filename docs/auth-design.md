# Authentication Design — Workforce Entra ID Tenant

**Authorship**: Bishop (Security & Auth Specialist)  
**Related Spec**: `specs/002-auth-entra` (Phase 2, Weeks 6–7)  
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

## 6. Local Admin Bootstrap (PRD §F5)

**Scenario**: First deployment. Admin needs a way to sign in before Entra app is fully configured, or as a fallback if Entra is unavailable.

### Design

1. **Local Account Credentials** (stored in DB, hashed with bcrypt):
   - Initial admin user created during bootstrap setup.
   - Credentials: `Username` + `PasswordHash` (bcrypt, cost factor ≥ 12).
   - Endpoint: `POST /api/v1/auth/local-login` (no authorization policy; public during bootstrap).

2. **Bootstrap Flow**:
   - First deployment, no users in DB → `/` redirects to `/admin/bootstrap`.
   - Bootstrap page displays form: "Set Admin Username" + "Set Admin Password".
   - Submit → `POST /api/v1/auth/bootstrap` (one-time endpoint; checks if any users exist).
   - Backend: Create `Owner` record: `username`, `role: Admin`, `PasswordHash`, `IsLocalAccount: true`, `EntraObjectId: null`.
   - Endpoint self-destructs (check `count(Owner) > 0` on next call → 403 Forbidden).
   - Log: `AuditEvent { action: "LocalAdminBootstrap", username, timestamp, ... }`.

3. **Entra Setup in Parallel**:
   - During bootstrap, Admin also completes Entra app registration in Azure Portal (Phase 2 runbook).
   - Once app is registered and Admin's household account is assigned the `admin` role in Entra:
     - Admin signs in with household credentials (Entra redirect).
     - First-login handler creates `Owner` record (if not exists) with `EntraObjectId` (no local password).
     - Two `Owner` records may coexist: one local (bootstrap), one Entra. Both Admin.
   - Client redirects to Entra login by default (if Entra configured); local login is fallback.

4. **Local Account Lifecycle**:
   - **During bootstrap**: Local account is the only way to sign in (Entra not yet ready).
   - **After Entra setup**: Both local and Entra sign-in work; Entra is preferred.
   - **Optional cleanup**: Admin can disable local login endpoint once Entra is stable (docs in ops runbook).
   - **Fallback**: If Entra is unavailable, local login is always available (fail-safe).

### Security Notes

- Local login endpoint rate-limited: max 5 failed attempts per IP per 15 min (TODO Phase 2).
- Bcrypt cost factor ≥ 12.
- No plaintext passwords logged (Serilog destructuring).
- Bootstrap endpoint verified (count > 0) before allowing next setup.
- Admin password not transmitted over unencrypted channels (HTTPS only in production).

---

## 7. Threats Addressed

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
| 2.0 | 2026-05-18 | Bishop | **REVISED** per decision `copilot-directive-2026-05-18T140924Z-entra-tenant.md`: Use household Workforce tenant (not External ID). Updated app registration flow, MSAL.js config, role mapping, and bootstrap sequence. Removed External ID rationale. Added implementation sequence for Hicks.

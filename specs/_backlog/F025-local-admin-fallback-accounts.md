# F025: Local Admin Fallback Accounts (Entra Continuity)

**Status**: backlog
**Priority**: P1
**Effort**: L
**Value**: high
**Risk**: high (security-critical)
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: unassigned

## Problem
Authentication today is 100% Entra ID via OIDC + PKCE (`Auth:Entra:*`). Per
the constitution every endpoint defaults to deny, and the only identity issuer
the API trusts is the configured Entra tenant. If Entra is unreachable for any
reason — tenant misconfiguration, expired client secret, Azure outage,
revoked-by-mistake app registration, or a coding mistake during a routine
rotation — **no one** can sign in, including the household admin who needs to
fix the configuration.

For a self-hosted family tool this is a single point of failure the household
admin cannot recover from without redeploying the API with an emergency
config. We want a documented, in-app fallback so the admin can keep using the
inventory and can also restore service for other family members.

## Proposed Solution
Add an opt-in **local credentials** provider alongside Entra. Local accounts
are minted by an existing Admin (already authenticated via Entra) through a
new admin UI. Local accounts can sign in with username + password through a
separate login form when Entra is unavailable. The default posture stays
Entra-first: local login is hidden behind a "Use a local account" link on the
login page and is only meaningful when at least one local account exists.

### Storage
- New `LocalUser` aggregate in `Domain`: `Id`, `Username` (case-insensitive
  unique), `DisplayName`, `Role` (`Admin` | `Member` | `Viewer`),
  `PasswordHash`, `PasswordAlgorithm` (e.g. `argon2id`),
  `MustChangePasswordOnNextLogin`, `FailedAttemptCount`, `LockoutUntilUtc`,
  `LastLoginUtc`, `IsActive`, `CreatedUtc`, `CreatedBy` (Entra oid of the
  admin who minted it), `LastPasswordChangeUtc`.
- `LocalUser` is **separate** from the existing `Owner` reference entity. An
  `Owner` row may optionally link to a `LocalUser` (nullable FK) so audit and
  device-ownership semantics stay unified, but the link is informational —
  Owner is a domain concept, LocalUser is an authentication concept.
- Password hashing uses **Argon2id** with the parameters chosen in an ADR
  (memory ≥ 19 MiB, iterations ≥ 2, parallelism = 1 as the 2025 OWASP
  baseline). The library choice is decided in the ADR (likely
  `Konscious.Security.Cryptography.Argon2` for .NET).
- All password hashes, lockout counters, and reset tokens live in the SQLite
  database — no plaintext, no shared secrets in config.

### Authentication wiring
- The existing `TechInventoryAuth` composite scheme gains a second forward
  handler: `LocalAccountAuthenticationHandler` that validates **short-lived
  JWTs signed by the API** (HS256, key from
  `Auth:Local:SigningKey`, minimum 256-bit). When the inbound bearer token
  starts with the local issuer (`iss = techinventory-local`), the local
  handler validates and stamps the claims; otherwise the Entra JWT bearer
  handler runs as today.
- Local-account JWTs include `sub = LocalUser.Id`, `name = DisplayName`,
  `role`, `iss = techinventory-local`, `aud = techinventory-api`, `exp = now
  + 8h`, `auth_method = local`. Refresh uses an HTTP-only `__Host-` cookie
  bound to the user agent IP+UA pair (cookie name: `ti_local_refresh`,
  rotated every refresh, 14-day absolute lifetime, 4-hour sliding window).
- Roles and authorization policies are unchanged — both providers feed the
  same `Admin | Member | Viewer` claim, so existing `[Authorize(Roles=…)]`
  attributes Just Work.

### API surface (all `Admin`-only unless noted)
- `POST /api/v1/local-accounts` — create. Body: `{ username, displayName,
  role, temporaryPassword? }`. If `temporaryPassword` is omitted the server
  generates a 16-char URL-safe string and returns it once in the response;
  the new account is created with `MustChangePasswordOnNextLogin = true`.
- `GET  /api/v1/local-accounts` — list (paged, sortable by username /
  lastLogin / role).
- `GET  /api/v1/local-accounts/{id}` — detail.
- `PATCH /api/v1/local-accounts/{id}` — update displayName / role /
  isActive. Self-demotion of the last Admin is rejected with `409`.
- `POST /api/v1/local-accounts/{id}/reset-password` — Admin-issued reset.
  Returns a new temporary password, sets `MustChangePasswordOnNextLogin`.
- `POST /api/v1/local-accounts/{id}/unlock` — clears
  `LockoutUntilUtc` + `FailedAttemptCount`.
- `DELETE /api/v1/local-accounts/{id}` — soft-delete (sets `IsActive=false`).
  Active sessions are not invalidated; revoke happens on next refresh.
- `POST /api/v1/auth/local/login` — **anonymous**. Body: `{ username,
  password }`. Returns `{ accessToken, expiresIn }` and sets the refresh
  cookie. Rate-limited: 5 attempts / 15 min / IP and 10 / 15 min / username
  before HTTP 429. On a 6th IP failure the response time is constant 1s
  (timing-attack mitigation).
- `POST /api/v1/auth/local/refresh` — **anonymous**, reads refresh cookie.
- `POST /api/v1/auth/local/logout` — clears refresh cookie + server-side
  revokes the token family.
- `POST /api/v1/auth/local/change-password` — authenticated, requires both
  the current password and the new one; rejects if `MustChangePassword`
  flag is set and current ≠ stored.

### Bootstrap recovery path
If Entra is fully unreachable AND no local Admin exists yet, the admin needs
a way in. Two complementary mechanisms:
1. **Env-var seed admin** — at startup, if `Auth:Local:SeedUsername`,
   `Auth:Local:SeedPassword`, and `Auth:Local:SeedEnabled=true` are all set,
   the API ensures a local Admin with those credentials exists with
   `MustChangePasswordOnNextLogin=true`. Logs a `[CRIT]` warning on every
   startup so the operator is reminded to remove the seed config after
   first use. Refuses to start if `SeedEnabled=true` and
   `ASPNETCORE_ENVIRONMENT=Production` unless `Auth:Local:SeedAllowInProd`
   is also `true` (escape hatch with explicit ack).
2. **CLI command** — `dotnet TechInventory.Api.dll create-local-admin
   --username … --password -` reads the password from stdin, hashes it, and
   inserts the row directly through EF Core. Documented in
   `docs/operations.md` as the disaster-recovery path; works when the API
   process is offline.

### Frontend
- New `/auth/login` (SvelteKit) page already exists for MSAL; add a "Use a
  local account" link that toggles to a `<LocalLoginForm>` with username +
  password fields, "Forgot password? — ask an Admin" copy, axe-clean
  validation messages, and a "Back to single sign-on" link.
- New `/admin/local-accounts` admin route (Admin only, matches existing
  `/admin/*` pattern). Lists local accounts with badges for `Locked`,
  `Must change password`, `Inactive`. Actions: create, reset password,
  unlock, deactivate, delete. Create modal shows the generated temp
  password once with a copy-to-clipboard button and a "Done, I saved it"
  confirm — the password is never persisted in cleartext anywhere else and
  cannot be re-shown.
- On first login with a temp password, the user is redirected to a
  `/auth/change-password` page that blocks navigation until the password is
  changed.
- Auth store learns about `authMethod: 'entra' | 'local'`; the user pill
  shows a subtle "local" badge so the user can tell which credential they
  used.

## Why now
- The user surfaced this as a real concern after the dev-bypass confusion:
  "what if something happens with Entra?"
- Single-tenant family Entra apps are exactly the population most likely to
  hit credential-rotation / certificate-expiry surprises with no on-call to
  unblock them.
- We currently have zero break-glass story for auth; the only recovery is to
  edit `appsettings.json`, flip `Auth:DevBypass` to `true`, and redeploy —
  which requires shell access and bypasses all auditing.

## Acceptance Criteria
- [ ] Admin authenticated via Entra can create, list, edit, reset,
      unlock, deactivate, and delete local accounts from
      `/admin/local-accounts`.
- [ ] A local account can sign in at `/auth/login` (after clicking "Use a
      local account") and gets the same role-based access as an Entra user
      with the same role.
- [ ] First-login with a temporary password forces a change before any
      other action succeeds.
- [ ] After 5 failed password attempts within 15 min, the account is locked
      for 15 min and a `LocalAccountLocked` audit event is written.
- [ ] Self-demotion of the last Admin (whether Entra-Admin or local-Admin)
      is rejected with HTTP `409` and a clear ProblemDetails payload.
- [ ] All local-account mutations write `AuditEvent` rows with actor =
      the calling admin's oid/sub, including the local user's id and the
      action verb (`Created`, `RoleChanged`, `Reset`, `Unlocked`,
      `Deactivated`, `Deleted`).
- [ ] Password storage uses Argon2id; no plaintext, no MD5/SHA1/SHA256-
      only. Hash parameters are read from `Auth:Local:Argon2:*`.
- [ ] Login + refresh + change-password endpoints are rate-limited per the
      thresholds above and emit `[WRN]` logs on lockouts.
- [ ] Env-var seed admin works on first boot, refuses to run in Production
      without explicit override, and logs a `[CRIT]` reminder on every
      startup while enabled.
- [ ] `task verify` is green: unit tests for the new handler / hasher /
      lockout logic, integration tests for login + change-password +
      lockout + last-Admin guard, contract tests for the new OpenAPI
      schemas, accessibility tests for the login + admin UI, and e2e
      coverage for "create local admin → entra goes away (toggle
      `Auth:Entra:Disabled=true`) → local admin signs in → unlocks self".
- [ ] Documentation: ADR (`D-14x`) capturing the design decision,
      `docs/operations.md` updated with the seed-admin and CLI recovery
      paths, README quick-start note that local accounts are opt-in.

## Out of scope (defer to future)
- Self-service password reset by email (no email infra; admin reset is the
  flow).
- TOTP / passkey enrollment for local accounts (treat as a follow-up;
  Entra still owns the strong-auth story for the common case).
- SCIM / external user provisioning.
- Per-account API tokens / personal access tokens.
- Federated identity from anything other than Entra (Google, GitHub, etc.).

## Open questions
- Should local-account login be **always available** or only when
  `Auth:Local:Enabled=true`? Default proposal: always-on once the schema
  ships, with the "Use a local account" link hidden when zero local
  accounts exist. Tradeoff: an unused-but-on auth path is still attack
  surface.
- Should we let an Entra Admin **convert** their own identity into a
  linked local account (one-click "create me a fallback")? Convenient but
  blurs the audit story; lean no for v1, revisit if the household admin
  asks.
- Hash parameters: pick values now (memory=19MiB, iter=2, par=1) or
  benchmark on the Pi-class hardware the household actually runs? Defer
  to the ADR.

## References
- Constitution §6 (Security): default deny, roles Admin/Member/Viewer,
  audit-on-mutation requirements all apply.
- Existing `Auth:Entra:*` config + `JwtBearer` handler in
  `src/TechInventory.Api/Program.cs`.
- `DevBypassAuthenticationHandler` — pattern reference for adding a second
  scheme to the composite `TechInventoryAuth` handler.
- OWASP Password Storage Cheat Sheet (Argon2id parameter guidance).
- F020 (User Profile Settings) — adjacent but distinct; F020 is about
  editing display attributes for already-authenticated users.

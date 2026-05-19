# F025b: Local Admin Fallback — Power-User Slice

**Status**: backlog
**Priority**: P2 (lift to P1 if v1b break-glass usage exposes friction)
**Effort**: M–L
**Value**: medium (UX + hardening on top of an already-working rescue path)
**Risk**: medium (still security-critical, but the foundation is in place)
**Target release**: TBD — after we have lived with v1b in production
**Created**: 2026-05-19
**Owner**: unassigned

## Context

F025 v1b shipped the minimum break-glass path so the household admin can log
in when Entra is unavailable. It deliberately deferred everything that wasn't
strictly required to recover service. F025b is the landing place for those
deferrals so they aren't lost.

Decision rationale: see **ADR D-140** in `.squad/decisions.md`.

## In Scope

1. **Admin UI for local accounts** (Admin role only):
   - List active + inactive local users.
   - Create new local user (Admin / Member / Viewer).
   - Reset password (sets `MustChangePasswordOnNextLogin=true`).
   - Deactivate / reactivate.
   - Soft-delete with audit trail.
   - "Last remaining active Admin" guard — cannot deactivate or downgrade the
     final Admin local account.

2. **Lockout enforcement.** v1b already stores `FailedAttemptCount` and
   `LockoutUntilUtc` on `LocalUser`; F025b actually checks them on login.
   Recommended defaults: 10 attempts within 15 min → 15 min lockout. Admin
   UI should expose a "clear lockout" action.

3. **IP-based rate limiting** on `POST /api/v1/auth/local/login` independent
   of the per-account lockout (mitigates distributed attempts against
   different usernames). Use ASP.NET Core's built-in rate limiter.

4. **Refresh tokens / sliding sessions.** v1b issues an 8 h JWT with no
   refresh path. F025b should issue a short-lived JWT + HTTP-only,
   `SameSite=Strict`, `Secure` refresh cookie with rotation on use.

5. **Self-service "convert me to local"** for an existing Entra Admin —
   lets a household admin pre-provision their own break-glass account
   without re-typing their identity. Mints a `LocalUser` linked to the
   active Entra principal, force-rotates on first local login.

6. **Audit events for local-auth lifecycle**: create, password change, reset,
   deactivate, soft-delete, lockout, lockout-cleared, refresh-token-revoked.
   The append-only `AuditEvent` table already handles the storage shape.

7. **Argon2 parameter benchmarking** on the Pi-class production host. v1b
   ships the OWASP minimum (`m=19_456 KiB, t=2, p=1`). F025b confirms the
   target hardware can comfortably handle a stronger profile or accepts
   the baseline as final.

## Out of Scope

- MFA on local accounts. We treat local accounts as break-glass and rely on
  the strong password + lockout combination.
- WebAuthn / passkeys (separate future spec, would supersede most of this).
- SCIM / external user provisioning for local accounts.

## Acceptance Criteria (Draft)

- An Admin can create, reset, lock, unlock, deactivate, and soft-delete local
  accounts from the admin UI with no shell access.
- Attempting to deactivate the last active Admin local account returns a
  409 with `code=LastLocalAdmin` and no state change.
- After 10 wrong-password attempts in 15 minutes, further attempts on the
  same account return 423 (Locked) until the lockout window passes or an
  Admin clears it.
- IP-based limiter caps unauthenticated login attempts to 30 per minute per
  IP; over-limit requests return 429 with the standard `Retry-After` header.
- Sessions refresh seamlessly while the refresh cookie is valid; revoking
  the cookie (sign-out, admin action) invalidates the session within one
  refresh cycle.
- All lifecycle actions write an `AuditEvent` row with actor + before/after
  payloads.

## Dependencies

- F025 v1b shipped (provides `LocalUser` aggregate, hasher, issuer, policy
  scheme, change-password gate).
- AuditEvent infrastructure (F021 — shipped).

## Open Questions

- Refresh cookie path — root `/` (covers SPA) vs `/api` (covers API only)?
  Need a security review before implementation.
- Should the admin UI surface "force sign out" (revoke all refresh tokens
  for a user)?
- Do we keep HS256 forever or move to RS256 when refresh tokens land (so
  a future second consumer could verify without sharing the signing key)?

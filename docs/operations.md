# Operations Runbook

This runbook covers production-relevant operations for the self-hosted Tech
Inventory deployment. It assumes the standard Docker Compose stack described
in `docs/architecture.md`.

---

## Break-Glass Local Admin (F025 v1b)

Entra ID is the primary identity provider. If Entra is unreachable — tenant
misconfiguration, expired client secret, Azure outage, app-registration
revoked by mistake — no one can sign in unless at least one **local
fallback account** exists. F025 v1b ships a bootstrap-only mechanism;
F025b will add an admin UI, lockout enforcement, and refresh tokens.

Decision context: ADR **D-140** in `.squad/decisions.md`.

### 1. Seed a local admin (first-time setup, recommended)

Add the following environment variables to the API container **before** the
first deploy. They are read by `LocalAdminSeedHostedService` at startup.

| Variable                              | Required | Notes                                                                                                                              |
| ------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `Auth__Local__SigningKey`             | ✅       | ≥ 32 characters; HMAC-SHA256 secret used to sign local JWTs. Generate with `openssl rand -base64 48`. **Treat as a secret.**       |
| `Auth__Local__SeedEnabled`            | ✅       | Set to `true` while seeding. Remove or set to `false` after the first successful sign-in + password rotation.                     |
| `Auth__Local__SeedUsername`           | ✅       | 3–64 chars, case-insensitive unique. Lower-invariant is canonical.                                                                 |
| `Auth__Local__SeedPassword`           | ✅       | Strong (≥ 16 chars recommended). The user is forced to rotate on first login regardless of strength.                              |
| `Auth__Local__SeedDisplayName`        | optional | Defaults to `Local Admin`.                                                                                                         |
| `Auth__Local__SeedAllowInProd`        | ✅ in prod | Refuses to seed in Production unless this is `true`. Defense-in-depth so seed env vars don't accidentally re-seed across restarts. |
| `Auth__Local__Argon2__*`              | optional | Tune `MemoryKiB`, `Iterations`, `Parallelism`. Defaults are OWASP 2025 baseline (`19456 / 2 / 1`).                                  |

**Behaviour on startup:**

- The hosted service logs a CRITICAL-level warning **every time the API
  starts** while `SeedEnabled=true`. This is intentional — if the warning
  keeps appearing in production logs, the operator has forgotten to remove
  the env vars after recovery.
- The seed is idempotent: re-running it re-hashes the password, forces
  `MustChangePasswordOnNextLogin=true`, and reactivates the account. This
  doubles as a password reset.
- In Production, startup fails fast unless `SeedAllowInProd=true`.

**Example (`docker-compose.override.yml` for first-time seed only):**

```yaml
services:
  api:
    environment:
      Auth__Local__SigningKey: "<base64-secret>"
      Auth__Local__SeedEnabled: "true"
      Auth__Local__SeedUsername: "rescue"
      Auth__Local__SeedPassword: "<temporary-strong-password>"
      Auth__Local__SeedAllowInProd: "true"
```

### 2. First sign-in + rotation

1. Open the web UI → **Sign In** page → click **Use a local account instead**.
2. Sign in with the seeded username + temporary password.
3. The app immediately routes to **Change your password**. Set a password
   that is at least 12 characters and differs from the temporary one.
4. The session ends; sign in again with the new password.

### 3. Decommission the seed

After step 2 succeeds:

1. Remove `Auth__Local__SeedEnabled`, `Auth__Local__SeedUsername`,
   `Auth__Local__SeedPassword`, and `Auth__Local__SeedAllowInProd` from the
   API environment (delete the override file or scrub the secret store).
2. Restart the API container.
3. Confirm the CRITICAL "local admin seed is configured" log line is gone.

`Auth__Local__SigningKey` **must remain set** — it is required to validate
existing local JWTs. Rotate it only when you intend to invalidate every
outstanding local session.

### 4. Routine recovery (no seed needed)

If a local account already exists and the password is just forgotten:

- Today (v1b): re-enable the seed for the same username with a new password
  (steps 1–3 above). The seed will reset that account's password.
- After F025b ships: use the admin UI's "Reset password" action — no
  restart required.

### 5. What v1b does **not** do (yet — see F025b)

- No admin UI for managing local accounts.
- No lockout after repeated failed attempts (counter is stored but not
  enforced).
- No IP-based rate limiting on `/api/v1/auth/local/login`.
- No refresh tokens — local JWTs expire after 8 hours; re-sign-in required.
- No "convert my Entra admin to local" self-service.

### 6. Security guarantees in v1b

- Passwords hashed with Argon2id (OWASP 2025 baseline parameters by default;
  tunable via `Auth__Local__Argon2__*`).
- Login responses are uniform `401 Unauthorized` with
  `code=InvalidCredentials` for both unknown user and wrong password (no
  username enumeration).
- Local JWTs are issued by issuer `techinventory-local` and routed through
  a separate JwtBearer scheme; the existing Entra scheme is untouched.
- Force-rotation middleware blocks every API call with
  `403 Forbidden` + `code=PasswordChangeRequired` for any local session
  that still has `must_change_password=true`, except the change-password
  endpoint itself.
- Tokens live in `sessionStorage` only (Constitution §6 forbids
  `localStorage`).

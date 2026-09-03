# 3. API Key Authentication

Date: 2026-09-03
Status: Accepted
Deciders: `briandenicola` (human approver), recorded by Ripley (Lead / Architect)
Related: GitHub issue #149, `specs/005-api-key-support/spec.md`

## Context

Tech Inventory authenticates users via two interactive bearer-token schemes —
Entra ID OIDC + PKCE (primary) and a local HS256 break-glass issuer (F025 v1b).
Both are session-oriented: they require a browser sign-in, produce short-lived
JWTs, and are unsuitable for headless automation.

The household admin wants to drive device-inventory CRUD from CLI scripts,
scheduled jobs, and Home Assistant integrations without opening a browser.
GitHub issue #149 ("Add API key support") captures this need.

The scope is deliberately narrow:

* **Inventory operations only** — device read/write plus reference-data (brand,
  category, location, network, owner, tag) reads. Admin, audit, import, export,
  report, and settings endpoints are excluded.
* **Admin and Member creators only** — Viewer accounts cannot create keys.
* **Personal delegated keys** — each key acts on behalf of its creating owner,
  subject to a live role/status ceiling at request time.
* **Capped lifetime and quota** — 90-day default, 365-day maximum, 5 active
  keys per owner, no non-expiring keys.

### Why Not OAuth Client Credentials or PATs?

| Alternative | Rejection Reason |
|---|---|
| OAuth client credentials (Entra app-only) | Requires a separate Entra app registration with admin consent. Over-engineered for a single-household app that already has two auth schemes; adds an external dependency for a feature whose value is headless simplicity. |
| Standard PAT (opaque, GitHub-style) | Functionally similar to what we build, but the term "PAT" implies a platform-wide identity scope. Our keys are deliberately narrower (inventory-only, household-bound). The implementation is the same; only the branding and scope differ. |
| mTLS / certificate-based | Excellent security, but operationally heavy for a home deployment. Requires a CA, client cert provisioning, and reverse-proxy configuration. Disproportionate to the threat model. |
| IP allowlists as sole control | Not viable as an authentication mechanism; useful only as a defense-in-depth layer. The reverse proxy already provides this optionally (architecture.md §6.3). |

## Decision

### Credential Format

* **Header**: `Authorization: ApiKey <selector>.<secret>`
* The `<selector>` is a short, URL-safe, base64-encoded public identifier
  (16 bytes / 128 bits) used for database lookup and log correlation. It is
  indexed and appears in audit entries and Serilog structured logs.
* The `<secret>` is 32 bytes (256 bits) of CSPRNG output, base64url-encoded.
  It is shown to the user exactly once at creation time and never stored.
* The combined token (`selector.secret`) is the credential the caller sends.

### Storage and Verification

* **No plaintext persistence.** The raw secret is never stored.
* The server stores `HMAC-SHA-256(secret, pepper)` as the verifier. The
  pepper is a deployment secret (`ApiKeys:HmacPepper`), injected via
  environment variable / Docker secret, separate from the local-auth
  signing key.
* Verification: compute `HMAC-SHA-256(presented-secret, pepper)` and compare
  to the stored verifier using a **fixed-time comparison**
  (`CryptographicOperations.FixedTimeEquals`).
* Selector + verifier live in an `ApiKey` table; the raw secret exists only in
  memory during creation and in the one-time HTTP response.

### Why HMAC-SHA-256, Not Argon2id?

API keys are high-entropy, machine-generated secrets (256 bits). Unlike
passwords, they are not susceptible to dictionary or brute-force attacks
against stolen hashes. HMAC-SHA-256 with a separate pepper provides:

* Constant-time verification without tuning parameters.
* Immunity to offline brute-force given the key entropy.
* The pepper adds a second factor: compromising the database alone is
  insufficient without the deployment secret.
* Argon2id would add per-request latency (~200ms at OWASP baseline) that is
  acceptable for interactive login but disproportionate for API key
  verification on every request.

### Identity Model: Personal Delegated

* Each API key is bound to an `Owner` (the creator) and the single
  `Household`. It acts as a delegated credential for that owner.
* At request time, the auth handler resolves the key's owner and checks:
  1. Key is not revoked.
  2. Key is not expired.
  3. Owner is active (`Owner.IsActive == true`).
  4. Owner's current role permits the requested scope (live ceiling).
* If any check fails, the request receives a uniform `401 Unauthorized`
  (`ProblemDetails` with `code=InvalidApiKey`). The response does not
  distinguish between unknown key, expired, revoked, or disabled owner —
  this prevents enumeration.

### Scope Model: Coarse, Inventory-Only

Two scopes, assigned at creation:

| Scope | Grants |
|---|---|
| `inventory.read` | `GET` on devices, brands, categories, locations, networks, owners, tags |
| `inventory.write` | All of `inventory.read` plus `POST`, `PUT`, `DELETE` on devices |

`inventory.write` implies `inventory.read`. Keys cannot access admin,
audit, import, export, report, settings, or local-auth endpoints regardless
of the owner's role.

### Lifetime and Quota

| Parameter | Value |
|---|---|
| Default expiry | 90 days from creation |
| Maximum expiry | 365 days |
| Active keys per owner | 5 |
| Non-expiring keys | **Not permitted** |

### Authentication Pipeline Integration

The existing `PolicyScheme` (`TechInventoryAuth`) in
`src/TechInventory.Api/Program.cs:73–97` uses `ForwardDefaultSelector` to
sniff the `Authorization` header and route to the Entra or Local scheme. The
API key handler is integrated as a **third forwarding target**:

1. If the header starts with `ApiKey `, forward to a new
   `TechInventoryAuth.ApiKey` scheme.
2. If the header starts with `Bearer ` with a `techinventory-local` issuer,
   forward to the Local scheme (unchanged).
3. Otherwise forward to the Entra scheme (unchanged).

If a request carries both `Bearer` and `ApiKey` headers (or any ambiguous
combination), the handler rejects with `401 Unauthorized` immediately — no
fallthrough, no second attempt.

### Audit

* **Create, revoke** — recorded as `AuditEvent` entries via the existing
  `AuditBehavior` pipeline (`src/TechInventory.Application/Behaviors/AuditBehavior.cs`).
  The audit payload includes the selector (never the secret), scope, expiry,
  and acting user.
* **Per-use logging** — API key authentications are logged via Serilog
  structured properties (`ApiKeySelector`, `ApiKeyOwnerId`) at
  `Information` level. A full `AuditEvent` row per use is a non-goal to
  avoid write amplification on a SQLite database receiving automated traffic.

### Failure Uniformity

All API key failure modes produce the same `401 Unauthorized` ProblemDetails
response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The API key is invalid or has been revoked.",
  "extensions": { "code": "InvalidApiKey" }
}
```

This covers: unknown selector, wrong secret, expired key, revoked key,
inactive owner, and owner role insufficient for the requested scope. No
information leakage.

### Rate Limiting

API key requests are rate-limited per selector (not per owner) at the
middleware level. Exact thresholds are deployment-configurable; the spec
recommends 60 requests/minute as a default with a 429 `Too Many Requests`
response. This is independent of any edge rate limiting the reverse proxy
provides (architecture.md §6.3).

## Consequences

### Positive

* Headless automation (scripts, cron, Home Assistant) can manage device
  inventory without interactive sign-in.
* The credential model is simple: one header, one lookup, one HMAC compare.
* Existing authorization policies (`Admin`, `AdminOrMember`) continue to
  work — the API key handler produces a `ClaimsPrincipal` with the same
  `ClaimTypes.Role` shape as the Entra and Local handlers.
* The selector/verifier split allows safe logging and correlation without
  exposing secrets.
* Pepper separation means a database breach alone cannot forge keys.

### Negative / Accepted

* A third authentication scheme increases the `ForwardDefaultSelector`
  complexity in `Program.cs`. Mitigated by clear header-prefix routing and
  integration tests that assert all three paths plus ambiguity rejection.
* API keys bypass MFA. Accepted because the household threat model
  (threat-model.md, Surface 2) rates automated-credential risk as Medium
  and mitigates via short lifetime, quota, live owner ceiling, revocation,
  and rate limiting.
* No key rotation endpoint (rotate = revoke old + create new). Accepted as
  a v1 simplification; the 365-day maximum and 5-key quota make manual
  rotation feasible for a household.

### Security / Operations

* **Pepper rotation**: changing `ApiKeys:HmacPepper` invalidates all
  outstanding keys. Document in `docs/operations.md` under a new
  "API Key Administration" section during implementation.
* **Emergency revocation**: Admin can revoke any user's keys via the API
  (Wave 1) and the Admin UI (Wave 2). No grace period.
* **Monitoring**: Serilog structured log on every API key auth attempt
  (success or failure) with selector, owner ID, and scope. Failures
  increment a counter visible in OpenTelemetry traces.
* **Secret display**: the raw key is returned exactly once in the creation
  response. The UI (Wave 2) must display it in a copy-to-clipboard field
  with a "you won't see this again" warning.

## References

Issue #149 cites no `R<N>` reference from `docs/references.md`. No
reference was triggered for this decision. The design is grounded in the
project's own authentication pipeline (`src/TechInventory.Api/Program.cs`,
`docs/auth-design.md`) and the OWASP API Security Top 10 (2023) credential
storage guidance (constitution §5.6).

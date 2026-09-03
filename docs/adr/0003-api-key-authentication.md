# 3. API Key Authentication

Date: 2026-09-03 (revised 2026-09-03 — independent correction pass after Apone's QC rejection of the original draft)
Status: Accepted
Deciders: `briandenicola` (human approver); recorded by Ripley (Lead/Architect); revised by Hicks (Backend) under reviewer lockout
Related: Issue #149 · `specs/005-api-key-support/spec.md` · `plan.md` · `tasks.md`

## Context

Tech Inventory authenticates interactively via Entra ID OIDC+PKCE (primary) and a local HS256 break-glass issuer (F025). Both require a browser sign-in and short-lived JWTs — unsuitable for headless automation (CLI scripts, cron, Home Assistant). Issue #149 asks for a non-interactive credential. Brian approved: inventory-only device read/write plus reference-data (brand, category, location, network, owner, tag) reads; Admin and Member as creators (Viewer excluded); a personal delegated identity carrying a live role/status ceiling; 90-day default / 365-day maximum expiry, 5 active keys per creator, no non-expiring keys; Settings UI + Admin emergency revocation deferred to Wave 2, after the Wave 1 API ships.

### Rejected Alternatives

| Alternative | Rejection Reason |
|---|---|
| OAuth client credentials (Entra app-only) | Needs a separate Entra app registration + admin consent — an external dependency disproportionate to a single-household app. |
| Standard opaque PAT (GitHub-style) | Same mechanics as this design; only naming/scope differ. Ours is deliberately narrower (inventory-only), so a dedicated design is still warranted. |
| mTLS / client certificates | Strong security, but needs a CA and reverse-proxy cert plumbing — disproportionate for a home deployment. |
| IP allowlists as sole control | Not an authentication mechanism; usable only as defense-in-depth at the reverse proxy (architecture.md §6.3). |

## Decision

### Credential Format

`Authorization: ApiKey <selector>.<secret>`. `<selector>` is 16 CSPRNG bytes, base64url — an indexed, loggable public lookup key. `<secret>` is 32 CSPRNG bytes, base64url — shown exactly once at creation, never stored or retrievable again.

### Storage, Verification, and the Dummy Path (Apone correction)

No plaintext secret is ever persisted; the server stores `HMAC-SHA-256(secret, pepper)` as the verifier. The pepper (`ApiKeys:HmacPepper`) is a deployment secret **distinct from `Auth:Local:SigningKey`** (the local-issuer JWT signing key) — the two must never share a value or source. **Startup validation (fail-fast):** the API refuses to start unless `ApiKeys:HmacPepper` is present and, once base64-decoded, is at least 32 bytes (256 bits); a missing or short pepper is a configuration error, not a runtime 500.

Verification computes `HMAC-SHA-256(presented-secret, pepper)` and compares to the stored verifier with `CryptographicOperations.FixedTimeEquals` — a data-independent-time comparison by construction, not a wall-clock benchmark. **Unknown-selector handling:** when no `ApiKey` row matches, the handler still computes an HMAC over a fixed dummy secret/verifier pair and calls `FixedTimeEquals` before failing, so the "selector exists vs. doesn't" branch performs the identical cryptographic call either way. This property is established **deterministically**: a unit test asserts the dummy-compare code path executes on every unknown-selector call (via a test seam/call-count, not elapsed time), plus focused security review (task T-023) inspecting the handler for early returns that would skip it. **No test — unit, integration, or otherwise — may assert on wall-clock timing or a `<1ms` variance threshold.**

### Identity Model: Personal Delegated, Principal-Aware (Apone correction)

A key is issued by whichever authenticated principal creates it — an Entra-backed `Owner` or a local-break-glass `LocalUser`. **`Owner` carries no `HouseholdId`, and `LocalUser` is a distinct authentication entity, never equated with `Owner`** (see `src/TechInventory.Domain/Entities/LocalUser.cs` remarks). A key therefore stores **both** an explicit discriminator (`PrincipalType`: `Owner` | `LocalUser`) + `PrincipalId`, **and** an explicit `HouseholdId` resolved the same way `CreateDeviceCommand` already resolves it — via `IHouseholdRepository.ListAsync`, which requires exactly one `Household` row (0 or >1 is `Conflict`), never derived from a non-existent `Owner.HouseholdId`. At request time, the handler re-resolves the key's principal by type (`IOwnerRepository` or `ILocalUserRepository`) and checks live, not from a snapshot: (1) key not revoked, (2) key not expired, (3) principal `IsActive == true`, (4) principal's current `Role` permits the requested scope. Any failure yields the single uniform response below — no enumeration of which check failed.

### Scope Model: Coarse, Inventory-Only

| Scope | Grants |
|---|---|
| `inventory.read` | `GET` on devices + reference data (brand, category, location, network, owner, tag) |
| `inventory.write` | All of `inventory.read`, plus `POST`/`PUT`/`DELETE` on devices |

Keys never reach admin, audit, import, export, report, settings, or `/api/v1/auth/local/*` endpoints, regardless of the live role.

### Lifetime and Quota

| Parameter | Value |
|---|---|
| Default / maximum expiry | 90 days / 365 days |
| Active keys per creator | 5 |
| Non-expiring keys | Not permitted |

### Pipeline Integration and Ambiguous Credentials (Apone correction)

The existing `PolicyScheme` (`TechInventoryAuth`, `Program.cs` `ForwardDefaultSelector`, ~line 73) gains a third forwarding target, evaluated **before** any concrete handler runs: if the `Authorization` header carries **both** an `ApiKey` value and a `Bearer` value (multiple headers or comma-joined), the selector returns a rejection target producing **`401 Unauthorized`, `code=AmbiguousCredential`** — one status/code, used identically here, in the spec, and in the task list; no scheme is invoked, no fallthrough occurs. Otherwise: `ApiKey ` prefix → `TechInventoryAuth.ApiKey` (new); `Bearer ` with `iss=techinventory-local` → `TechInventoryAuth.Local` (unchanged); any other `Bearer ` → `TechInventoryAuth.Entra` (unchanged). Both existing bearer paths must be **regression-tested** after this change (spec §6, N-19/N-20) — the new branch must not alter their routing or claims shape.

### Audit and Failure Uniformity

Create/revoke are `AuditEvent` entries via the existing `AuditBehavior` (selector, scope, expiry, acting principal — never the secret). Per-use success/failure is a Serilog structured log only (selector, principal id, scope) — a per-use `AuditEvent` is a non-goal (SQLite write amplification). Every API-key auth failure (unknown selector, wrong secret, expired, revoked, inactive principal, insufficient live role) returns the identical `401 Unauthorized` / `code=InvalidApiKey` ProblemDetails (spec §4.4) — distinct from `AmbiguousCredential`, which fires earlier in the pipeline, before scheme routing.

### Rate Limiting (Apone correction: deterministic tests)

Per-selector limiting (not per-principal) via ASP.NET Core's built-in rate limiter; production default 60 req/min → `429 Too Many Requests`. **Tests must not exercise the production default at load.** Integration tests override `RateLimiting:ApiKey:PermitLimit`/`WindowSeconds` to a small test-scoped value (e.g., 3/60s) via configuration, so the `429` path is deterministic and fast — never a 60-request flood against the production default.

## Consequences

**Positive:** headless automation works without interactive sign-in; one HMAC compare, one lookup; existing `Admin`/`AdminOrMember` policies work unchanged because the handler's `ClaimsPrincipal` carries the same `ClaimTypes.Role` shape either `PrincipalType` already produces; selector/verifier split keeps logs/audit secret-free; pepper separation means a database breach alone can't forge keys.

**Negative / Accepted:** a third forwarding branch adds `Program.cs` complexity, mitigated by the ambiguity guard and N-19/N-20 regression tests; keys bypass MFA (threat-model.md Surface 2 rates this Medium, mitigated by short lifetime/quota/live-ceiling/revocation/rate-limit); no rotate endpoint (revoke+create is sufficient at a 5-key quota).

## References

Issue #149 triggers no `R<N>` reference from `docs/references.md` (R1, R2 catalogued; neither applies). Design is grounded in the project's own pipeline (`Program.cs`, `docs/auth-design.md`) and OWASP API Security Top 10 (2023) credential-storage guidance (constitution §5.6).

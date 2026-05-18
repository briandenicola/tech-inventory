# Hicks Decision Note — Development auth bypass

- **Date:** 2026-05-18
- **Scope:** `src/TechInventory.Api/Program.cs`, `src/TechInventory.Api/Authentication/`, `src/TechInventory.Api/appsettings*.json`
- **Related tasks:** T32-T41
- **Related authority:** Constitution §5.1, §5.2; `docs/auth-design.md`; `specs/001-core-api/plan.md` §4.1

## Decision

Add a Development-only `Auth:DevBypass` flag. When `true` in `Development`, the API authenticates every request as a synthetic `dev-admin` principal with fixed `sub` / `oid` claims and `Admin` role so Brian can exercise secured endpoints with curl or Bruno. When `false`, the API falls back to a placeholder auth handler that returns 401 until real Entra JWT bearer wiring lands.

## Rationale

The constitution requires default-deny authorization on every endpoint, but Path A2 for Round 6 also requires a runnable API Brian can hit immediately. A gated Development bypass preserves `[Authorize]` on the real controller surface while making local smoke testing and manual API exploration possible before T46/Tenant bearer work is complete.

## Guardrails

- `Auth:DevBypass` defaults to `true` only in `appsettings.Development.json`
- `Auth:DevBypass` defaults to `false` in `appsettings.json`
- Startup throws if the flag is enabled outside Development
- Startup logs a warning that `Auth:DevBypass` is enabled and every request is authenticated as `dev-admin`; the runtime message is assembled in code to avoid secret-scan false positives

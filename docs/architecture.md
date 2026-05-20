# Architecture

## C4 Level 1 — Context

```
[Family member]  ──HTTPS──▶  [Nginx Proxy Manager (TLS termination)]
                                         │
                                         ▼
                              [web container (nginx)]   ◀── serves SvelteKit static bundle
                                         │
                                  /api/* forwarded internally
                                         ▼
                              [api container (ASP.NET Core 10)]   ◀── NOT exposed externally
                                         │                                   ▲
                                         ▼                                   │ OIDC + PKCE
                              [SQLite on Docker volume]              [Microsoft Entra ID]
                                                                     [Local JWT issuer (break-glass)]
```

Production URL: `https://inventory.denicolafamily.com`. The API is **not** published outside the Docker network — every request the browser makes is same-origin against the web container, which reverse-proxies `/api/*` to the api container. See ADRs **D-133** (CORS for dev), **D-134** (relative API base URL in prod), **D-135** (nginx reverse proxy), and **D-139** (same-origin directive) in `.squad/decisions.md`. NPM configuration cheat sheet: [`deployment.md` §8](deployment.md).

## C4 Level 2 — Containers

| Container | Image / Stack | Role | Exposed |
|-----------|---------------|------|---------|
| `web`     | `nginx:alpine` serving the SvelteKit `build/` output | Static SPA + `/api/*` reverse proxy | `:3000` (behind external TLS proxy) |
| `api`     | ASP.NET Core 10 (Clean Architecture: Domain → Application → Infrastructure → Api) | REST API, OpenAPI, auth, audit, MediatR pipeline | Internal `:8080` only |
| `db`      | SQLite file on the `techinv-data` Docker volume | Persistent store, EF Core migrations | None — file-only |
| `backup`  | Litestream sidecar (opt-in `--profile backup`) | Continuous SQLite replication / restore | None |

## Clean Architecture Layers (api)

```
TechInventory.Domain          ← entities, value objects, aggregate roots; zero framework deps
TechInventory.Application     ← MediatR handlers, FluentValidation, ICurrentUserService abstraction
TechInventory.Infrastructure  ← EF Core DbContext, repositories, Argon2id hasher, hosted services
TechInventory.Api             ← Minimal API / controllers, auth pipeline, Serilog, OpenTelemetry
```

Dependencies point inward only. Controllers are thin; business logic lives in Application handlers; persistence and auth integrations live in Infrastructure.

## Authentication

Two coexisting schemes routed by a single `TechInventoryAuth` `PolicyScheme` that sniffs the JWT `iss` claim:

1. **Entra ID** (primary) — workforce tenant, OIDC + PKCE, roles assigned via app roles.
2. **Local JWT issuer** (`techinventory-local`, break-glass) — F025 v1b, HS256 + Argon2id, 8h tokens, force-rotation middleware. See [`docs/auth-design.md`](auth-design.md) and ADR **D-140**.

Tokens live in `sessionStorage` only (Constitution §6, ADR **D-002**) — never `localStorage`. Local JWT storage keys are `ti_local_token` and `ti_local_meta`; MSAL also uses `sessionStorage`.

## Key Decisions

The authoritative ledger is [`.squad/decisions.md`](../.squad/decisions.md). The `docs/adr/` directory holds the template (ADR-0001) for any future standalone ADRs; in practice the team records decisions inline in `.squad/decisions.md` as D-NNN entries.

## See also

- [`auth-design.md`](auth-design.md) — Entra ID + local break-glass JWT pipeline
- [`security-baseline.md`](security-baseline.md) — token storage, logging, authorization rules
- [`threat-model.md`](threat-model.md) — STRIDE analysis per surface
- [`operations.md`](operations.md) — day-2 operations (break-glass admin)
- [`deployment.md`](deployment.md) — production deploy on NPM, backups, rollback


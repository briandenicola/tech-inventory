# Architecture

## C4 Level 1 — Context
[Family users] → [Caddy proxy] → [API] → [SQL]
                              ↘ [Entra ID]

## C4 Level 2 — Containers
- `web`   SvelteKit PWA (Caddy-served)
- `api`   ASP.NET Core 10
- `db`    sqllite
- `proxy` Caddy (TLS, routing)
- `backup` SQL backup sidecar

## Key Decisions
See `docs/adr/`.

# Architecture

## C4 Level 1 — Context
[Family users] → [External Proxy] → [UI] → [API] → [SQLite Db]
                              ↘ [Entra ID]

## C4 Level 2 — Containers
- `web`   SvelteKit UI + PWA 
- `api`   ASP.NET Core 10
- `db`    sqlite

## Key Decisions
See `docs/adr/`.

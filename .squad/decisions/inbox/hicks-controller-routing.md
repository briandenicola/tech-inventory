# Hicks Decision Note — Controller routing and OpenAPI surface

- **Date:** 2026-05-18
- **Scope:** `src/TechInventory.Api/Controllers/`, `src/TechInventory.Api/Features/Categories/`, `src/TechInventory.Api/Program.cs`
- **Related tasks:** T32-T40
- **Related authority:** `specs/001-core-api/tasks.md` T32-T40, `specs/001-core-api/plan.md` §4.1

## Decision

Use classic attribute-routed controllers with explicit lowercase `/api/v1/...` paths instead of minimal APIs.

- Resource controllers use explicit routes like `/api/v1/devices`, `/api/v1/brands`, and `/api/v1/audit-events`
- Categories expose both paged roots (`GET /api/v1/categories`) and full hierarchy (`GET /api/v1/categories/tree`)
- OpenAPI JSON is served at `/openapi/v1.json` while Swagger UI stays at `/swagger`

## Rationale

Apone's controller-focused integration suite and the task wording both favor a conventional controller surface. Explicit lowercase routes also remove ambiguity around tokenized `[controller]` casing and make the curl/Bruno examples stable for Brian this session.

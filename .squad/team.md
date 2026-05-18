# Squad Team

> tech-inventory — self-hosted family device/appliance inventory tracker. ASP.NET Core 10 API + SvelteKit PWA, SQLite, Entra ID auth, Docker Compose self-hosted on home infrastructure.

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Ripley | Lead / Architect | `.squad/agents/ripley/charter.md` | 🏗️ Active |
| Hicks | Backend Developer | `.squad/agents/hicks/charter.md` | 🔧 Active |
| Vasquez | Frontend Developer | `.squad/agents/vasquez/charter.md` | ⚛️ Active |
| Hudson | DevOps / Platform | `.squad/agents/hudson/charter.md` | ⚙️ Active |
| Bishop | Security / Auth | `.squad/agents/bishop/charter.md` | 🔒 Active |
| Apone | Tester / QA | `.squad/agents/apone/charter.md` | 🧪 Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | 🔄 Monitor |

## Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker. Single household. Replaces a long-running SharePoint List with a modern PWA + REST API.
- **Stack:** ASP.NET Core 10 (Clean Architecture, MediatR, FluentValidation, EF Core, SQLite, Serilog, OpenTelemetry). SvelteKit PWA (TypeScript strict, Tailwind, MSAL.js, generated TS client). Docker Compose, Task (taskfile.dev), GitHub Actions → GHCR.
- **Auth:** Microsoft Entra ID (External ID), OIDC + PKCE. Roles: Admin / Member / Viewer.
- **Testing:** xUnit + Testcontainers, Vitest + Testing Library, Playwright (mandatory E2E, Chromium + WebKit + Firefox), axe-core, Lighthouse CI.
- **Authority hierarchy:** `.specify/memory/constitution.md` → `docs/prd.md` → `specs/00X-*/spec.md` → `specs/00X-*/plan.md` → `tasks.md` → agent judgment.
- **References:** R1 drinks-and-desserts, R2 coin-collection-app (`docs/references.md`). Patterns yes, code no.
- **Created:** 2026-05-18

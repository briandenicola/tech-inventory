# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker. Single-household, authenticated via Microsoft Entra ID, deployed as Docker Compose on home infrastructure.
- **Stack:** ASP.NET Core 10 (Clean Architecture: Domain → Application → Infrastructure → Api), MediatR, FluentValidation, EF Core + SQLite, Serilog, OpenTelemetry. SvelteKit PWA (TypeScript strict, Tailwind, MSAL.js, generated TS client). Docker Compose deploy behind external reverse proxy. Task (taskfile.dev) for local automation.
- **Created:** 2026-05-18

## Core Context

Phased delivery per PRD §13: Phase 0 Foundation → Phase 1 Core API (`specs/001-core-api`) → Phase 2 Auth (`specs/002-auth-entra`) → Phase 3 Infrastructure (`specs/003-infrastructure`) → Phase 4 Web PWA (`specs/004-web-pwa`) → Phase 5 Hardening.

Document hierarchy (constitution §0): `.specify/memory/constitution.md` → `docs/prd.md` → `specs/00X-*/spec.md` → `specs/00X-*/plan.md` → `tasks.md` → agent judgment.

Reference repos (`docs/references.md`): **R1 drinks-and-desserts** (MediatR handler structure, Problem Details middleware, health checks), **R2 coin-collection-app** (inventory domain shape, CSV import pipeline, attachment storage, Docker Compose layout). Patterns yes, code no. Pin to SHA and cite `R<N>:<path>@<sha>`.

## Recent Updates

**2026-05-18:** Phase 0 parallel scaffolding complete. Security baseline now in effect (`docs/security-baseline.md` from Bishop). **Currency strategy decision OPEN and blocks T04** — awaiting Brian's decision between per-device (recommended) or single-currency approach.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **2025-05-18**: Drafted Phase 1 spec (`specs/001-core-api/spec.md`, `plan.md`, `tasks.md`). Key decisions:
  - Network is a standalone reference entity (not a Tag subtype) — PRD §U8 treats it as a managed list.
  - Result<T> is a custom minimal record in Domain (no external lib dependency — keeps Domain clean per constitution §2.2).
  - Currency proposed as per-device ISO 4217 with household default — decision pending Brian's input (`decisions/inbox/ripley-currency-strategy.md`).
  - 48 tasks decomposed; Hicks owns implementation, Apone owns test authorship, Hudson owns CI.
  - Reference entities use soft-delete via `IsActive` flag (consistent with Device soft-delete pattern).
  - AuditBehavior as MediatR pipeline (not SaveChanges interceptor) — gives richer before/after capture and respects the command boundary.

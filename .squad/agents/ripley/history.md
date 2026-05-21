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

- **2026-05-21 — v1.0 Documentation Alignment**: Performed end-of-day surgical pass on 11 docs post-v1.0 ship. Key updates: (1) README.md — IMAGE_TAG strategy now documents three patterns (`:vX.Y.Z` for production, `:main` for rolling dev, `:sha-<short>` for staging); previous guidance was "use latest as default" which is no longer accurate as `:latest` now pins to semver releases only, not main HEAD (D-154 via CI release-images workflow). (2) auth-design.md — added §6 "Silent SSO Bootstrap (F038)" documenting the 3-second timeout, multi-tab behavior, and Entra outage fallback (shipped in commits 788268c + follow-ups after D-150 proposal). (3) docs/prd.md — status updated from "Draft v0.1" to "v1.0 (shipped 2026-05-21)"; last-updated date was 2025-05-17 (stale by a year). (4) docs/backlog.md — F038 entry refined to note the 3-second timeout explicitly; F035, F036, F037, F044 status verified correct. (5) docs/deployment.md — §7 rewritten with three IMAGE_TAG strategies (Strategy A production pinning, Strategy B rolling dev, Strategy C staged rollout by SHA); previous example showed rolling back to `v0.0.9`, now shows v1.0 series to avoid stale references. (6) .copilot-state.md updated to reflect "v1.0 shipped" state and post-v1.0 next milestones (#17, #18 open for future work). (7) SESSION-NOTES.md appended with v1.0 ship summary and learnings. No changes needed: docs/architecture.md, docs/known-issues.md (describes pre-v1.0 deferred tests, still accurate), docs/operations.md (break-glass runbook unchanged), docs/references.md, docs/security-baseline.md (token storage rules unchanged), docs/testing.md (test counts, auth patterns still current), docs/threat-model.md. Scope discipline: surgical updates only, no rewrites of validated sections. Decisions D-164 (modal scroll), D-167 (z-index hierarchy), D-168 (vestigial props cleanup) were design decisions not documentation scope — noted for future alignment if those get formalized in architecture docs. No code files modified; validated `git diff --stat` shows only `.md` files touched. All docs internally consistent post-update.

- **2025-05-18**: Drafted Phase 1 spec (`specs/001-core-api/spec.md`, `plan.md`, `tasks.md`). Key decisions:
  - Network is a standalone reference entity (not a Tag subtype) — PRD §U8 treats it as a managed list.
  - Result<T> is a custom minimal record in Domain (no external lib dependency — keeps Domain clean per constitution §2.2).
  - Currency proposed as per-device ISO 4217 with household default — decision pending Brian's input (`decisions/inbox/ripley-currency-strategy.md`).
  - 48 tasks decomposed; Hicks owns implementation, Apone owns test authorship, Hudson owns CI.
  - Reference entities use soft-delete via `IsActive` flag (consistent with Device soft-delete pattern).
  - AuditBehavior as MediatR pipeline (not SaveChanges interceptor) — gives richer before/after capture and respects the command boundary.

- **2026-05-20**: Created spec 003 (`specs/003-pwa-polish/`) from Brian's field-test feedback. Key decisions:
  - New spec (not amendment to 002) — constitution §1 scope discipline; 002 has clear completion tracking.
  - 16 tasks across 5 rounds (A: bugs, B: mobile UX, C: features, D: reporting, E: tests).
  - Dark mode promoted from 002 non-goal to 003 P2 — Brian explicitly requested in field test.
  - Merge duplicates requires new backend endpoint (`POST /api/v1/{entity}/merge`) — Hicks owns.
  - Reporting gets lightweight foundation here (3 API endpoints + summary UI); full dashboard remains Phase 4.
  - Insurance export is CSV-first; PDF deferred (ADR needed if Brian wants PDF later).
  - Infinite scroll supersedes pagination but keeps a11y fallback for `prefers-reduced-motion`.
  - Rounds A+B+D can parallelize from day one; C depends on backend work from Hicks.

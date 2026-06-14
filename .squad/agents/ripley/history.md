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

- **2026-06-13 — Post-v1.0 Architecture Audit**: Conducted comprehensive codebase review post-v1.0 ship (Brian's request for deep engineering analysis). Key findings:
  - **CRITICAL BUG IDENTIFIED**: DeviceRepository lines 228-235 enforce implicit "Status != Disposed" filter when no explicit status filter provided. Frontend defaults to `status: ['Active']` on bare URLs (F026 feature), sending explicit filter. When user clicks "All statuses" (status=all sentinel), frontend passes `undefined` to API, backend applies "not Disposed" filter server-side. Result: Disposed devices never visible even when explicitly requested. Root cause: misaligned filter contract — frontend "all" means "send nothing", backend "nothing" means "exclude Disposed". Fix requires either (1) frontend send explicit all-status array, or (2) backend accept sentinel param, or (3) align on undefined=all convention.
  - **Architecture compliance**: Domain layer **zero** framework dependencies (✓ constitution §2.2). No EF/DbContext leakage into Domain or Application. Controllers **thin** (largest: DevicesController 313 lines, 90% DTOs; no business logic). Repository pattern enforced; no raw SQL anywhere. MediatR pipelines operational (ValidationBehavior → handler → AuditBehavior). Clean Architecture dependency flow: Api → Infrastructure → Application → Domain (✓).
  - **Component health**: Frontend largest components DeviceTable.svelte (596 lines), DeviceForm.svelte (590 lines) both **exceed** constitution §6.5.4 200-line guideline but are single-purpose with clear state boundaries; not god components but should be refactored into composition patterns for maintainability. Other components under limit.
  - **API client discipline**: Constitution §6.5.2 requires generated client, no hand-written fetch. Found 3 fetch calls in `client.ts` (lines 141, 219, 654) — all are **wrapper infrastructure** (apiFetch helper, blob download, raw endpoint call), not business-domain calls. Actual domain calls go through typed `devices.list()` etc. Pattern is compliant but blurs the line; worth ADR clarification on "generated client" scope (does it mean zero fetch usage, or zero domain-fetch usage?).
  - **Test coverage**: 88 backend test files, 52 frontend test files. Strong coverage foundation. No behavioral test gaps identified in spot-check, but constitution §Testing requires 85% line coverage on Domain+Application — recommend CI coverage gate validation.
  - **Missing patterns**: No ADR directory found (constitution violations require ADR per §Preamble). Several design decisions in .copilot-state.md / history.md should graduate to formal ADRs (e.g., MediatR pipeline choice, AuditBehavior sequencing, per-device currency strategy).
  - **Pagination concern**: DeviceRepository.ListAsync (lines 27-39) loads full result set from DB, then does **in-memory** pagination via `ToPagedResultAsync` → `MergeTrackedAsync` → `ToListAsync()` + skip/take. At scale (1000+ devices) this will degrade; recommend server-side pagination via `Skip().Take()` on IQueryable before materialization. Current impl works because it merges EF Local tracker (unsaved adds/updates), but that pattern should only apply to reference entities (brands, categories), not paginated device lists.
  - **Soft-delete alignment**: All entities use `IsActive` flag (Brand, Category, Location, Network) or status enum (Device). Audit table is append-only. ✓ Constitution §4.3.
  - **Observability**: Serilog + OpenTelemetry wired in Api project. Structured logging present. No PII leakage spotted in sampled log statements. ✓ Constitution §3.5.

---

### 2026-06-14: Engineering Audit Session

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-ripley.md`

**Key Audit Findings:**
- Architecture correctly enforced (Domain → Application → Infrastructure → Api)
- Critical device list filter contract risk identified (filter semantics divergence between UI/API/Domain)
- In-memory pagination flagged as high-risk for scalability
- Large frontend components exceed guidelines but acceptable for now
- API client / ADR process gaps documented

**Deliverables:**
- 6 orchestration logs created (one per team agent)
- 6 new decisions merged to decisions.md (D-168.1, D-169, D-170, D-171, D-172, D-173, D-174)
- Inbox decisions cleared (8 files deleted)
- Session log at `.squad/log/2026-06-14T00-17-12Z-engineering-audit.md`

**Status:** Audit complete. Findings logged for team review and prioritization.

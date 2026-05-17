---

## Artifact 2: `.specify/memory/constitution.md`

```markdown
# Project Constitution

> **This document is the non-negotiable contract for how this project is built.**
> Every AI agent session must read this file first. Every PR must comply.
> Violations require an explicit, documented waiver (ADR).

**Project**: Tech Inventory (self-hosted family device catalog)
**Version**: 1.0.0
**Ratified**: 2025-01-XX
**Amendment process**: PR with ADR; affected sections re-versioned

---

## 0. Hierarchy of Authority

When in doubt, defer in this order:
1. **This constitution** (highest authority)
2. **`docs/prd.md`** (product intent)
3. **`specs/00X-*/spec.md`** (current feature spec)
4. **`specs/00X-*/plan.md`** (technical plan)
5. **`specs/00X-*/tasks.md`** (active work)
6. **Agent's judgment** (lowest — and must be voiced, not assumed)

If a lower document conflicts with a higher one, **stop and raise it**.

---

## 1. Mission Alignment

- Every change must serve a goal in `docs/prd.md` (Section 3 Goals)
- No work proceeds without a backlog item, spec, or explicit instruction
- Scope creep is the #1 enemy — defer to `Non-Goals` aggressively

---

## 2. Architecture (Non-Negotiable)

### 2.1 API-First
- Every capability is exposed via versioned REST API **before** any UI work
- The OpenAPI specification is the contract; UI clients are generated from it
- No backend feature ships without API + tests; no UI feature ships without API

### 2.2 Clean Architecture
- Layers: `Domain` → `Application` → `Infrastructure` → `Api` / `Web`
- Dependencies point inward only; no leakage outward
- Domain layer has **no** dependencies on frameworks, EF Core, HTTP, or I/O
- Cross-cutting concerns (logging, auth, validation) belong in Application/Infrastructure

### 2.3 CQRS (Where Justified)
- Use MediatR for non-trivial commands and queries
- Commands return success/failure + minimal data; queries return read models
- Avoid CQRS for simple lookups; pragmatism over dogma

### 2.4 Thin Controllers
- Controllers parse input, invoke handler, return response
- **No** business logic in controllers
- **No** EF Core / DbContext usage in controllers

### 2.5 API Versioning
- URL-versioned: `/api/v1/...`
- Breaking changes require a new version + 6-month deprecation window
- Contracts live in `src/TechInventory.Contracts` (shared with clients)

---

## 3. .NET & C# Standards

### 3.1 Language & Runtime
- **.NET 10** (LTS); target framework `net10.0`
- C# latest language version
- **Nullable reference types** enabled project-wide
- **Treat warnings as errors** in Release builds

### 3.2 Code Style
- File-scoped namespaces
- Primary constructors where idiomatic
- `record` for DTOs and value objects
- `var` only when type is obvious from RHS
- Explicit access modifiers always
- `dotnet format` clean on every commit

### 3.3 Async Discipline
- Async **all the way** — no `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` in production code
- `CancellationToken` parameter on every async method (default if no plumbing path)
- `ConfigureAwait(false)` in library code (not in API project)

### 3.4 Error Handling
- **No** exception-driven control flow
- Use `Result<T>` or explicit success/failure types for expected failures
- Exceptions only for truly exceptional conditions
- API errors returned as **ProblemDetails** (RFC 7807)

### 3.5 Logging & Observability
- **Serilog** with structured logging
- Log levels: `Information` for milestones, `Warning` for recoverable issues, `Error` for failures
- **Never** log secrets, tokens, passwords, PII (serials, full names, locations)
- **OpenTelemetry** traces on all endpoints
- Correlation IDs propagated through all layers

### 3.6 Validation
- **FluentValidation** for all command/query inputs
- Validation runs **before** handler execution (pipeline behavior)
- Domain invariants enforced in the Domain layer, not validators

---

## 4. Data & Persistence

### 4.1 Database
- **sqllite+**
- **EF Core** code-first migrations; checked in; reviewed
- No schema changes outside migrations — ever
- Migration comments justify every new index

### 4.2 Query Discipline
- All queries **parameterized** (EF Core enforces this — no raw SQL with concat)
- N+1 queries audited; use `.Include()` or projection
- No `.ToList()` before filtering
- Pagination required on all list endpoints (default page size 25, max 200)

### 4.3 Soft Delete & Audit
- Soft delete via status flag — never `DELETE`
- Audit columns on all mutable tables: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`
- **AuditEvent** table records all mutations (append-only, never updated)

### 4.4 Backups
- Nightly automated SQL backups
- Backup retention: 30 daily, 12 monthly
- Quarterly restore drill (documented in runbook)
- Off-host backup copy required for production

---

## 5. Security (Non-Negotiable)

### 5.1 Authentication
- **Microsoft Entra ID only** — no local accounts, no password handling
- OIDC authorization code flow + PKCE
- JWT validation: issuer, audience, signature, expiry (`exp`), not-before (`nbf`)
- Token lifetime ≤ 1 hour; refresh tokens rotated

### 5.2 Authorization
- **Default deny** on every endpoint
- Authorization policies defined per endpoint
- Role-based: `Admin`, `Member`, `Viewer`
- Resource-level checks in the Application layer (not just controller attributes)
- **No client-side-only authorization** — always re-validated server-side

### 5.3 Secrets
- **Never** committed to source control
- `.env.example` documents required variables
- Production secrets via Docker secrets or external vault
- Pre-commit hook scans for secrets (`gitleaks`)
- Rotation cadence: 90 days for any long-lived secrets

### 5.4 Transport & Headers
- **TLS 1.3** at reverse proxy; TLS 1.2 minimum
- **HSTS** enabled with preload
- **CSP** with strict-dynamic where supported
- **SRI** for any external scripts
- Secure, HttpOnly, SameSite=Strict cookies
- CORS: explicit allowlist; no wildcards

### 5.5 Input & Output
- All inputs validated (FluentValidation + Zod on client)
- Output encoding default for all rendered content
- File uploads (v2): type/size validated, virus-scanned, stored outside web root
- No `eval`, no `dangerouslySetInnerHTML`, no raw HTML from users

### 5.6 Compliance Baselines
- **OWASP ASVS Level 2** as the security floor
- **OWASP API Security Top 10 (2023)** verified per PR
- **CWE Top 25** mitigations applied
- **NIST SSDF (SP 800-218)** practices followed
- **WCAG 2.2 AA** for accessibility

### 5.7 Audit & Forensics
- Every mutation logged with: actor, action, entity, before, after, timestamp
- Auth events logged: sign-in, sign-out, role change, failed auth
- Audit log queryable by Admin only
- Audit retention: 7 years (configurable)

### 5.8 Dependencies & Supply Chain
- `dotnet list package --vulnerable` clean (no Moderate+)
- `npm audit --audit-level=moderate` clean
- **Dependabot / Renovate** enabled for all package ecosystems
- **SBOM** generated per release (CycloneDX)
- **Trivy** scans every container image; High/Critical fail the build
- Pinned versions with lockfiles committed

---

## 6. Containers & Deployment

### 6.1 Image Hygiene
- Multi-stage Dockerfiles
- Distroless or `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` base for runtime
- **Non-root user** in all containers
- **Read-only root filesystem** where feasible
- No `latest` tags — pinned, digested references in compose
- Image size budgets: API ≤ 200MB, Web ≤ 50MB

### 6.2 Compose Discipline
- `docker-compose.yml` for shared config
- `docker-compose.override.yml` for local dev (hot reload, debug)
- `docker-compose.prod.yml` for self-hosted production
- **Healthchecks** on every service
- **Restart policies** explicit (`unless-stopped` for prod)
- Resource limits (`cpus`, `mem_limit`) set in prod
- Named volumes for state; bind mounts only when justified

### 6.3 Networking
- Reverse proxy (**Caddy**) terminates TLS
- API not exposed to host network — only via proxy
- DB only on internal compose network
- Separate networks for `public`, `app`, `data` tiers

### 6.4 Secrets in Deployment
- `.env` (gitignored) for local
- Docker secrets or sealed-secrets for prod
- No secrets in image layers (verified via Trivy)

---

## 7. Testing (Non-Negotiable)

### 7.1 Coverage
- **Minimum 85% line coverage** on Domain and Application layers
- Coverage report uploaded as CI artifact
- No drop in coverage allowed in a PR

### 7.2 Test Pyramid
- **Unit tests** (xUnit + FluentAssertions + NSubstitute): fast, deterministic, no I/O
- **Integration tests** (WebApplicationFactory + Testcontainers for SQL): cover boundaries
- **E2E tests** (Playwright on PWA + API smoke): critical user paths only
- **Contract tests** against OpenAPI spec

### 7.3 Test Discipline
- **TDD encouraged**: write failing tests first for new logic
- No `[Ignore]` / `[Skip]` without a linked issue and date
- Flaky tests are bugs — fix or remove within 1 sprint
- Tests must be deterministic — no real time, randomness, or network without controls

---

## 8. Workflow & Git

### 8.1 Commits
- **Conventional Commits** format
- **Signed commits** required on `main`
- One logical change per commit
- Commit message references task ID and spec section

### 8.2 Branches & PRs
- Trunk-based; short-lived feature branches
- **One task = one PR**
- PR diffs **< 500 lines** (split otherwise)
- PR template enforced: description, linked spec, checklist
- All CI checks must pass; at least 1 review for non-trivial changes

### 8.3 Branch Protection
- `main` requires: signed commits, linear history, CI green, review
- No force-push to `main`
- Direct commits to `main` blocked

---

## 9. Quality Gate (CI)

Every PR must pass:

- [ ] `dotnet format --verify-no-changes`
- [ ] `dotnet build -c Release` with warnings-as-errors
- [ ] `dotnet test` with coverage ≥ 85%
- [ ] `dotnet list package --vulnerable` clean
- [ ] CodeQL SAST clean (no High/Critical)
- [ ] `gitleaks` secret scan clean
- [ ] `trivy` container scan clean (no High/Critical)
- [ ] OpenAPI contract validation
- [ ] Web client: `tsc --noEmit`, ESLint, Vitest, Playwright smoke
- [ ] Lighthouse CI ≥ 90 across the board
- [ ] Accessibility (axe-core) zero violations
- [ ] SBOM generated and stored as artifact

---

## 10. AI Agent Operating Rules

### 10.1 Always
- Read this constitution at session start
- Read `.copilot-state.md`, current `spec.md`, current `tasks.md`
- Quote the spec section being implemented in commit messages
- Run the full verify script before declaring done
- Update `tasks.md`, `SESSION-NOTES.md`, `.copilot-state.md` at session end
- Cite this constitution by section when making design decisions

### 10.2 Never
- Use `any` / `dynamic` / `@ts-ignore` without an inline justification comment
- Disable lint rules silently
- Commit secrets, `.env` files, or generated artifacts
- Paraphrase files from memory — re-read or quote
- Continue past a drift signal — request `/compact` or end session
- Skip tests, mock away tests, or weaken assertions to pass
- Invent APIs, package names, or file paths

### 10.3 Context Discipline
- Prefer file paths + `grep`/`rg` over reading whole files
- Summarize tool output in ≤ 3 lines before continuing
- One feature per session — never juggle multiple specs
- Suggest `/compact` or session end at 50% context utilization
- Mid-session re-grounding via `/checkpoint` every ~5 turns
- End-of-session handoff via `/handoff`

### 10.4 Drift Recovery
If drift is suspected:
1. **Stop** — do not push through
2. Commit any working code as WIP
3. Write current-state paragraph in `SESSION-NOTES.md`
4. End session
5. Start new session with `/load-context`
6. Re-evaluate — possibly split the task

---

## 11. Documentation Requirements

- **PRD** (`docs/prd.md`): kept current; reviewed quarterly
- **Architecture** (`docs/architecture.md`): updated per material change
- **Threat Model** (`docs/threat-model.md`): updated per feature touching auth/data
- **ADRs** (`docs/adr/NNNN-*.md`): one per material decision
- **API docs**: OpenAPI generated, committed to `docs/api/`
- **Runbooks** (`docs/runbooks/`): backup, restore, upgrade, incident
- **Onboarding** (`docs/onboarding/`): family member sign-up steps

---

## 12. Audit & Continuous Improvement

### 12.1 Cadence
- **Weekly**: run `/audit` prompt; review findings; file issues for High/Critical
- **Per-release**: regenerate SBOM, threat model review
- **Quarterly**: PRD review, backlog re-prioritization, constitution amendments review
- **Annually**: full security audit, restore drill, dependency major-version review

### 12.2 Artifacts
- All audit reports in `audits/YYYY-MM-DD.md` — append-only
- All ADRs preserved indefinitely
- Session notes preserved for institutional memory

---

## 13. Definition of Done

A task is **done** when:

- [ ] All acceptance criteria met (quoted in PR)
- [ ] Tests written first; coverage ≥ 85% on touched files
- [ ] Lint, format, typecheck all clean
- [ ] Security scans clean (deps, SAST, secrets, container)
- [ ] Accessibility checks pass (web)
- [ ] Performance budget met
- [ ] OpenAPI updated if API surface changed
- [ ] Docs updated (ADR if material, architecture if structural)
- [ ] `tasks.md` checked off
- [ ] PR merged with signed commits
- [ ] Constitution compliance self-verified in PR

---

## 14. Amendment Process

1. Open an ADR proposing the change (`docs/adr/`)
2. Discuss in PR review
3. On merge, bump constitution version (semver):
   - **MAJOR**: removed/contradictory principles
   - **MINOR**: new principles added
   - **PATCH**: clarifications, typos
4. Update revision history (below)
5. Announce in `SESSION-NOTES.md`

---

## 15. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0.0 | 2025-01-XX | [You] | Initial ratification |

---

## 16. Signatures of Intent

By contributing to this project, every agent (human or AI) accepts these terms.
The constitution is the contract. Code that violates it is not "done."

> 🛡️ *Discipline now is freedom later.*
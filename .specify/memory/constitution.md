---

## Artifact 2: `.specify/memory/constitution.md`

```markdown
# Project Constitution

> **This document is the non-negotiable contract for how this project is built.**
> Every AI agent session must read this file first. Every PR must comply.
> Violations require an explicit, documented waiver (ADR).

**Project**: Tech Inventory (self-hosted family device catalog)
**Version**: 1.2.0
**Ratified**: 2026-05-17
**Last amended**: 2026-09-03 (post-major-work QC — performance enforcement aligned with ADR 0002, see §15)
**Amendment process**: PR with ADR; affected sections re-versioned

---

## 0. Hierarchy of Authority

When in doubt, defer in this order:
1. **This constitution** (highest authority)
2. **`docs/prd.md`** (product intent)
3. **`specs/00X-*/spec.md`** (current feature spec)
4. **`specs/00X-*/plan.md`** (technical plan)
5. **`specs/00X-*/tasks.md`** (active work)
6. **`specs/_backlog/F0XX-*.md`** (Feature Backlog)
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

### 2.6 Convention over Configuration
 - Where possible resource names are derived from conventions over configurations 

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
- **sqlite**
- **EF Core** code-first migrations; checked in; reviewed
- No schema changes outside migrations — ever
- Migration comments justify every new index

### 4.2 Query Discipline
- Repository Pattern. No sql queries sprinkled through the code.
- All queries **parameterized** (EF Core enforces this — no raw SQL with concat)
- N+1 queries audited; use `.Include()` or projection
- No `.ToList()` before filtering
- Pagination required on all list endpoints (default page size 25, max 200)

### 4.3 Soft Delete & Audit
- Soft delete via status flag — never `DELETE`
- Audit columns on all mutable tables: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`
- **AuditEvent** table records all mutations (append-only, never updated)

---

## 5. Security (Non-Negotiable)

### 5.1 Authentication
- First Account for system administrator and local testing 
- **Microsoft Entra ID ** — for standard user accounts and production 
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

### 5.4 Transport & Headers
- **TLS 1.2** minium at reverse proxy
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
- The app stack assumes an **external reverse proxy** terminates TLS
  (see §5.4). It is not bundled.
- DB is **never** exposed beyond the internal compose network
- Internal networks:
  - `app` — API + Web
  - `data` — API + DB (DB joined only here)
- The deployer's proxy is responsible for:
  - Public DNS and port 443 binding
  - WAF / rate limiting at the edge (optional but recommended)
  - IP allow/deny lists if used
- Documented integration patterns in `docs/deployment/`:
  - `npm.md` — nginx proxy manager (shared Docker network)
  - `traefik.md` — Traefik (labels-based discovery)
  - `caddy.md` — Caddy (Caddyfile reverse_proxy)
  - `external.md` — proxy on separate host (host port binding)

### 6.4 Secrets in Deployment
- `.env` (gitignored) for local
- Docker secrets or sealed-secrets for prod
- No secrets in image layers (verified via Trivy)

## 6.5 Web Client (PWA) — Non-Negotiable

### 6.5.1 Framework & Language
- **SvelteKit** with TypeScript **strict mode** (`strict: true`, `noUncheckedIndexedAccess: true`)
- **No `any`**; use `unknown` + narrowing
- Vite as build tool; pnpm as package manager
- One web project: `src/TechInventory.Web/`

### 6.5.2 API Consumption
- TypeScript API client **generated from OpenAPI** (e.g., `openapi-typescript-codegen` or `kiota`)
- **No hand-written `fetch` calls** to our API — always via the generated client
- Client regenerated in CI when `openapi.yaml` changes
- Versioned: `/api/v1/` only; never call unversioned endpoints

### 6.5.3 Architecture & State
- **Server state** managed via a query library (TanStack Query / SWR-style) with cache, retry, stale-while-revalidate
- **UI state** kept local to components or in Svelte stores; never co-mingled with server state
- **No global state singletons** for server data
- **Routes are thin**: data loading in `+page.ts` / `+layout.ts`; rendering in `+page.svelte`
- **No business logic in components** — extract to pure functions or stores

### 6.5.4 Component Discipline
- Components are **composable, single-purpose, < 200 lines**
- Props are typed; no implicit `any`
- Every interactive component supports:
  - **Loading state**
  - **Empty state**
  - **Error state**
  - **Success state**
- No component reaches outside its props/slots for data (no prop drilling > 2 levels — use context)

### 6.5.5 Design System
- **Design tokens** (color, spacing, typography, radius, shadow) defined once in `src/lib/tokens.css`
- **No magic values** in components — reference tokens via CSS custom properties
- **Tailwind CSS** configured to consume tokens (no arbitrary values like `mt-[13px]`)
- **Dark mode** via `prefers-color-scheme` + manual override, persisted per user
- **Component library** in `src/lib/components/` — documented in Storybook (or equivalent) for v2

### 6.5.6 Accessibility (Operationalized WCAG 2.2 AA)
- Semantic HTML first; ARIA only when no native equivalent
- **Every interactive element** is keyboard-operable
- **Focus visible** at all times; never `outline: none` without replacement
- **Color contrast** ≥ 4.5:1 text, ≥ 3:1 large text & UI components
- **Touch targets** ≥ 24×24 CSS px (WCAG 2.2 SC 2.5.8); 44×44 preferred
- **Forms**: every input has a `<label>`; errors announced via `aria-live`
- **Modals**: focus trap, return focus on close, Escape dismisses
- **Motion**: respect `prefers-reduced-motion`; no auto-playing animation > 5s
- **Screen reader testing** required for any net-new view (NVDA or VoiceOver)
- **axe-core** in unit and component tests; **zero violations** required to merge
- Engine-specific accessibility behaviour (WebKit / Firefox) is verified by the
  owned manual checklist (`docs/testing/manual-pwa-validation.md`), recorded as
  a declared gap — never as automated coverage (ADR 0002)

### 6.5.7 Responsive & Mobile
- **Mobile** PWA first class citizen equal to desktop design
- Design for 360px width minimum
- Responsive Design for mobile browsers but installable PWA is recommened
- Breakpoints: `sm 640`, `md 768`, `lg 1024`, `xl 1280` (Tailwind defaults)
- Touch-friendly: no hover-only affordances; everything tap-accessible
- **Safe-area insets** respected on iOS PWA

### 6.5.8 PWA Requirements
- **Installable** on iOS, Android, desktop (Chrome/Edge)
- Web App Manifest with: name, short_name, icons (192, 512, maskable), theme_color, background_color, display=standalone
- **Service Worker** via `vite-plugin-pwa`:
  - Pre-cache app shell
  - Runtime cache for API GETs (stale-while-revalidate, 5-min TTL)
  - **Mutations require online** — fail gracefully with retry queue (v2)
  - Update prompt on new SW available
- **Offline page** for navigation when uncached
- Installability and offline behavior are reviewed through
  `docs/testing/manual-pwa-validation.md`

### 6.5.9 Performance Budgets (Per Route)
| Metric | Budget | Enforcement |
|---|---|---|
| First Contentful Paint | ≤ 1.5s on 4G | Manual release check P-01 |
| Largest Contentful Paint | ≤ 2.5s | Manual release check P-01 |
| Time to Interactive | ≤ 3.0s | Manual release check P-01 |
| Total Blocking Time | ≤ 200ms | Manual release check P-01 |
| Cumulative Layout Shift | ≤ 0.1 | Manual release check P-01 |
| Initial JS bundle (gzipped) | ≤ 150KB | Manual release check P-01 |
| Per-route JS chunk (gzipped) | ≤ 50KB | Manual release check P-01 |
| Image LCP element | < 100KB, AVIF/WebP | Manual release check P-01 |

Performance budgets remain requirements, but they are `REVIEWED` per release,
not an automated browser gate. The explicit exception and closure trigger are
recorded in `specs/004-agentic-development-foundation/plan.md` §6.4.

### 6.5.10 Security (Client-Side)
- **MSAL.js** for Entra ID; PKCE; tokens in memory or sessionStorage (**never** localStorage)
- **No tokens in URLs, logs, or analytics**
- **CSP** strict; nonce-based for inline scripts where unavoidable
- **No third-party scripts** without ADR
- **No `dangerouslySetInnerHTML` equivalent** (`{@html}` in Svelte) without sanitization + ADR
- **Subresource Integrity (SRI)** on any external resource
- Client-side validation **mirrors** server-side (FluentValidation → Zod) — but **never replaces it**
- **Error messages** never leak server internals; map to user-safe copy

### 6.5.11 Forms
- **Zod** schemas for client validation; shape mirrors server contracts
- **Single source of truth**: schemas live in `src/TechInventory.Web/src/lib/schemas/`
- Inline validation on blur; submission validation on submit
- Disabled submit during request; never double-submit
- Optimistic updates only for non-destructive actions (with rollback)
- Destructive actions require explicit confirmation (typed name for delete, etc.)

### 6.5.12 Internationalization
- **All user-facing strings** in i18n catalogs (`src/lib/i18n/en.json`)
- **No hard-coded strings** in components
- Date, time, number, currency formatting via `Intl` APIs
- Locale fallback chain: user preference → browser → `en`
- v1 ships English; architecture must not block adding locales

### 6.5.13 Telemetry & Privacy
- **No third-party analytics** (no Google Analytics, no Mixpanel, etc.)
- **Self-hosted only** if telemetry is added (e.g., Plausible/Umami in own container)
- **No PII** in any client logs
- **Opt-in** for any telemetry; default off
- Errors reported to **self-hosted** error tracking (or local logs); never to SaaS

### 6.5.14 Testing (Web)
- **Unit**: Vitest + Testing Library; co-located `*.test.ts`
- **Component**: every interactive component has a test for each state (loading/empty/error/success)
- **Browser E2E**: **none.** The browser end-to-end layer is retired
  (**ADR 0002**); no automated browser suite exists in any role — required,
  scheduled, release, optional, or manual-dispatch — and no substitute
  browser-automation framework replaces it
- **Critical user paths** (sign in, list, detail, import, export, export
  filtering, role enforcement, offline shell) are verified as **real-HTTP
  integration/contract tests** plus **component tests**, per PRD §7.5.4
- **Accessibility**: `axe-core` in unit and component tests; zero violations
- **Manual**: browser-only behaviour (install, service worker/offline,
  engine-specific rendering) via the owned checklist
  `docs/testing/manual-pwa-validation.md` — a declared gap with an owner and a
  release cadence, never reported as automated coverage
- **Visual regression**: not adopted; any future proposal requires an ADR and
  must not reintroduce a browser-automation suite by the back door
- **No snapshot tests** for component HTML (brittle); prefer behavioral assertions

### 6.5.15 Browser Support
- **Evergreen only**: last 2 versions of Chrome, Edge, Safari, Firefox
- Mobile Safari iOS 16+
- Chrome Android last 2 versions
- **No IE, no legacy Edge, no transpile to ES5**
- Target: `ES2022`
- Polyfills justified per ADR

### 6.5.16 Build & Deploy
- Production build deterministic; no env-dependent code
- Source maps generated, uploaded to error tracker, **not served publicly**
- Static assets fingerprinted; long-cache via `Cache-Control: immutable`
- HTML uncached; revalidated each request
- Served by **Caddy** with compression (gzip + brotli)
- Reverse Proxy considered external concern of the application - TLS termination 

---

## 7. Testing (Non-Negotiable)

### 7.1 Coverage
- **Minimum 85% line coverage** on Domain and Application layers
- Coverage report uploaded as CI artifact
- No drop in coverage allowed in a PR

### 7.2 Test Pyramid
- **Unit tests** (xUnit + FluentAssertions + NSubstitute): fast, deterministic, no I/O
- **Integration tests** (WebApplicationFactory + real SQLite): cover boundaries over **real HTTP**, no mocked API
- **No automated browser layer** (ADR 0002): behaviour a browser suite would
  have asserted lives in HTTP integration/contract tests, component tests, or
  the owned manual PWA checklist
- **Contract tests** against OpenAPI spec, plus generated-client and EF
  migration drift gates

### 7.3 Test Discipline
- **TDD encouraged**: write failing tests first for new logic
- No `[Ignore]` / `[Skip]` without a linked issue and date
- Flaky tests are bugs — fix or remove within 1 sprint
- Tests must be deterministic — no real time, randomness, or network without controls

### 7.4 Local-First Testing
- All required test types must be runnable locally with one documented
  command (see PRD §7.5.5)
- **There is no browser E2E framework** — the layer is retired (**ADR 0002**).
  Adopting any automated browser-test framework, in any role, requires a new
  ADR superseding ADR 0002
- The authoritative verification entrypoint is **`task verify`**; it requires no
  Docker and downloads no browsers
- CI runs the same commands a developer runs locally
- Flaky tests are bugs (see PRD §7.5.8)

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
- Required status checks on `main` are named, not implied. The current target
  set is the Quality Gate jobs that actually exist: `verify`, `codeql`,
  `secrets`, `container-config-scan` (`sbom` runs only on `main` pushes and is
  therefore **not** required on PRs)
- No force-push to `main`; branch deletion disabled
- Direct commits to `main` blocked

> **Posture status (observed 2026-09-02, `enforcement: REVIEWED`, not
> `ENFORCED`):** `main` has **no branch protection and no rulesets**
> (`404 Branch not protected`, `rulesets == []`). The written recommendation for
> `briandenicola` to apply — with exact check names and JSON — is
> `specs/004-agentic-development-foundation/t105-governance-evidence.md` §4.
> Until it is applied, every check in §9 reports but blocks nothing, and that
> gap is recorded as an explicit visible exception at
> `specs/004-agentic-development-foundation/plan.md` §6.2 (§2.10's register
> entry for this rule), owned by `briandenicola`.

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
- [ ] Web client: `tsc --noEmit`, ESLint, Vitest unit + component tests (incl. `axe-core`)
- [ ] Retired-browser-harness stale-reference guard clean (ADR 0002)
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

### 11.1 Reference Implementations
- `docs/references.md` lists prior art that informs design decisions
- References are advisory; the constitution and PRD remain authoritative
- Patterns borrowed from references must be cited (repo + SHA + path) in
  the commit message and (if material) in an ADR
- Code copied verbatim requires an ADR including license review

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
- [ ] Component and/or HTTP integration tests added or updated for any
      UI-facing change; browser-only behaviour recorded against
      `docs/testing/manual-pwa-validation.md` (ADR 0002)
- [ ] `task verify` runs green locally on the change branch
- [ ] No new flaky tests (the affected suite run twice locally without failure)

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
| 1.0.0 | 2026-05-17 | [You] | Initial ratification |
| 1.1.0 | 2026-09-02 | Ripley (recorded for `briandenicola`) | **ADR 0002 — browser E2E layer retired.** Amended §6.5.6 (axe-core scope), §6.5.14 (web testing layers), §7.2 (test pyramid), §7.4 (local-first testing; `task verify` named as the authoritative entrypoint), §9 (quality-gate checklist), §13 (definition of done). Amended §8.3 to name the actual required check names and to record the observed unprotected posture as `REVIEWED`, not `ENFORCED`. No accessibility, security, coverage, or acceptance requirement was weakened. |
| 1.1.1 | 2026-09-02 | Hudson (T105 revision — Bishop reviewer-gate blocker B-3, `validation.md` §13.2) | §8.3's posture note pointed at `plan.md` §2.10 as if a live exception register entry existed there; none did (§2.10 is the principle statement, not a register). Corrected the pointer to `plan.md` §6.2, the new register entry that closes this gap (companion entry `plan.md` §6.3 covers the manual PWA checklist referenced by `docs/adr/0002-…` L111). No requirement, check, or posture was weakened — this is a citation correction, not a rule change. |
| 1.2.0 | 2026-09-03 | GitHub Copilot CLI (approved by `briandenicola`) | Post-major-work QC found that mandatory Lighthouse CI contradicted ADR 0002 and had no implementation. Removed the nonexistent browser-automation gate while preserving route performance budgets as manual release check P-01 (`REVIEWED`), recorded under `plan.md` §6.4. |

---

## 16. Signatures of Intent

By contributing to this project, every agent (human or AI) accepts these terms.
The constitution is the contract. Code that violates it is not "done."

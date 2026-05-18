# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device tracker. Runs on home infrastructure, single Docker host.
- **Stack:** Docker Compose (API + Web + SQLite db). Task (taskfile.dev) for local automation. GitHub Actions for CI, container images published to GHCR. Serilog → file/Seq for logs. OpenTelemetry for traces. Healthchecks on every service. External reverse proxy terminates TLS — not the API container.
- **Created:** 2026-05-18

## Core Context

Deployment phase: `specs/003-infrastructure` (Phase 3, ~Week 8).

NFRs (PRD §7): availability 99% (best-effort, single-host), RTO 4h / RPO 24h, nightly SQL backups with quarterly restore drills. Performance: P95 API < 300ms on home LAN.

Local stack contract (PRD §7.5.5):
```
task up      # bring up the full local stack (API, web, db)
task test    # run all mandatory tests against the running stack
task down    # tear down
```

Security baseline: non-root containers, read-only FS, network isolation, regular patching. SBOM per release. Trivy container scan in CI. `gitleaks` pre-commit hook.

Backend launch verification command: `./scripts/verify.sh` (format check → build → test → vulnerability scan).

CI quality gate must be green to merge: tests, security scans, SBOM.

## Recent Updates

**2026-05-18 (Phase 1 Round 1):** Pre-commit hook gate deployed. `.githooks/pre-commit` + `.gitleaks.toml` + `scripts/check-security.mjs` + `task hooks:install` now enforce token-storage policy at commit time. Hook verified: attempted commit with browser storage API for auth tokens correctly rejected. CI mirror in `.github/workflows/ci.yml` scans PR/push diffs. Decision D-012 documents repo-managed security hook pattern (cross-platform, no external package manager). Token-storage four-gate enforcement (D-010) coordinated with Vasquez (ESLint), Apone (Playwright E2E), and Bishop (code review checklist). Infrastructure-as-code handoff: developers run `task hooks:install` on fresh clone.


## Learnings

### 2026-05-18 — Parallel Scaffolding
- **Image bases**: API uses `mcr.microsoft.com/dotnet/sdk:10.0` build + `aspnet:10.0-alpine` runtime. Web uses `node:22-alpine` for both build and runtime (SvelteKit adapter-node).
- **Port choices**: API 8080 (standard ASP.NET Core), Web 3000 (SvelteKit adapter-node default). Dev compose exposes both; prod will sit behind reverse proxy.
- **SQLite volume strategy**: Not a container — SQLite is a file. Named volume `techinv-data` mounted at `/data` inside API container. API writes to `/data/techinv.db`. Documented clearly in compose header to avoid confusion.
- **Defensive Dockerfile patterns**: Web Dockerfile uses `COPY package.json pnpm-lock.yaml* ./` with wildcard glob to tolerate missing lockfile during parallel scaffolding. Build will fail gracefully if Vasquez's scaffold isn't done yet — that's expected.
- **Task installed locally**: Version 3.44.0. Docker not installed on this dev machine but that's fine — compose validation will happen in CI. Syntax looks clean from manual review.
- **Healthcheck strategy**: API has `/health` (liveness) and `/health/ready` (readiness). Web checks root `/`. Both use `wget` (baked into Alpine base). Compose uses `service_healthy` condition for web → api dependency.
- **Non-root + read-only FS**: All containers run as UID 1000. Tmpfs at `/tmp` for ephemeral writes. API needs writable `/data` for SQLite; web needs nothing writable except `/tmp`.
- **Resource limits**: API 512M limit / 256M reservation, Web 256M / 128M. Sensible for single-household workload on home server.

### 2026-05-18 — Security Hook Enforcement
- **Hook framework**: Repo-managed Git hooks live under `.githooks/` and are installed with `git config core.hooksPath .githooks`; the pre-commit hook delegates to `node ./scripts/check-security.mjs --staged`.
- **Install command**: `task hooks:install` downloads the pinned `gitleaks` binary into `.tools/gitleaks/`, wires `core.hooksPath`, and readies the pre-commit hook for fresh clones.
- **Regex pattern**: Future scans should keep enforcing `/localStorage\s*\.(set|get|remove)Item\s*\(\s*['"\`][^'"\`]*?(token|jwt|access|refresh|id_token|msal)/i` for auth-token persistence attempts.

### 2026-05-18 — SQLite Integration Test Harness
- **Factory shape**: `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs` is the reusable base. It overrides `ConnectionStrings:Default`, discovers registered `DbContext` service types, runs `Database.Migrate()` only when migrations exist, and deletes the `.sqlite`, `-wal`, and `-shm` files on dispose.
- **Isolation strategy**: Each integration test class uses `IClassFixture<IntegrationTestFactory<TTestClass>>`, so the generic marker type becomes the per-test-class SQLite file name and keeps test data isolated without Testcontainers.
- **Task contracts**: `task test:integration` is the backend HTTP/SQLite entry point; `task test:e2e` is the one-shot compose contract (`up --build` → wait for `/health/ready` → Playwright on `http://localhost:3000` → `down -v`); `task test` reuses a stack started by `task up` and delegates E2E execution to an internal ready-check runner.
- **Next-round handoff**: Hicks should register `AppDbContext` against `ConnectionStrings:Default`; if not, the TODO in `IntegrationTestFactory.ConfigureServices(...)` is where the DbContext registration should be replaced. Apone should keep future API suites on this fixture so migrations apply automatically once T13/T14 land.

**Cross-agent notes (Phase 1 Round 3):**
- SQLite isolation strategy (D-018) finalized: per-test-class files guarantee data cleanliness for consumer API tests.
- Hicks confirmed AppDbContext registration works with factory; no blocking issues.
- Verify pipeline updated: `scripts/verify.sh` / `verify.ps1` now includes three phases (format → build+test → vuln scan) for local/CI parity.

**2026-05-18 (Phase 1 Round 7) — T47 CI Pipeline Close-Out:**
- **Audit Complete:** CI workflow (ubuntu-latest) confirmed running full verify pipeline via `./scripts/verify.sh`. All Constitution §9 Phase 1 checks present: format, build, unit/integration tests, vuln scan, frontend check/lint, E2E tests, security scan (gitleaks via check-security.mjs).
- **Pre-commit Hook Refined:** Streamlined to lint + security only (removed format check due to performance). Pre-commit now runs fast gates (pnpm lint + check-security.mjs, ~2–3 seconds total) catching lint violations and secrets while format is left to CI. Format enforcement deferred to CI pipeline where it's already present and part of the full verify chain. Shell script remains cross-platform Git-compatible.
- **NuGet & pnpm Caching:** Both configured with fallback restore keys, saving ~1–2 minutes per run.
- **Workflows Documentation:** Created `.github/workflows/README.md` (comprehensive one-pager covering CI steps, caching, debug guidance, platform choices, and **manual GitHub UI setup Brian must perform** — requires enabling branch protection on `main` to actually block PRs on CI failure).
- **No High/Critical Gaps Found:** All mandatory Phase 1 checks running. Phase 3 TODOs (CodeQL SAST, Trivy container scan, SBOM) remain deferred as planned.
- **Branch Protection Manual Step:** Documented that Brian must go to Settings → Branches, add rule on `main`, enable "Require status checks to pass before merging" and select `ci / verify` as the required check. Without this, PRs can merge even if CI fails (GitHub UI enforcement, not CI-side issue).
- **E2E in CI:** docker-compose works in ubuntu-latest; Playwright E2E runs as part of verify.sh in CI. Locally, developers can skip E2E with `task test:unit` + `task test:integration` and run `task test:e2e` separately if Docker is available.
- **Decision D-024 (Pre-Commit Scope):** Approved — lint + secrets only, format deferred to CI for speed and decoupling.
- **Decision D-025 (CI Runner OS):** Approved — ubuntu-latest only; cost-effective, Docker built-in, Linux parity with production.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

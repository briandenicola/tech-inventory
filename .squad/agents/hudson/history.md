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

**2026-05-18 (Phase 1 Round 7) — T47 CI Pipeline Complete:**
- **Full Verify Audit:** CI workflow (ubuntu-latest) confirmed running complete verify pipeline: format → build → unit/integration tests → vuln scan → frontend check/lint → E2E tests. All Constitution §9 Phase 1 checks present and green.
- **Pre-commit Hook Refined:** Streamlined to lint + security only (~2-3s: pnpm lint + check-security.mjs). Format check deferred to CI (performance + decoupling).
- **NuGet & pnpm Caching:** Configured with fallback restore keys (~1-2 min savings per run).
- **CI Documentation:** `.github/workflows/README.md` created as one-pager with steps, caching strategy, debug guidance, platform rationale, and manual GitHub UI branch-protection setup instructions.
- **Branch Protection Manual:** Documented that Brian must set Settings → Branches → `main` rule to require `ci / verify` check (GitHub UI enforcement outside CI).
- **E2E in CI:** docker-compose + Playwright run on ubuntu-latest. Locally, developers can run `task test:unit` + `task test:integration` or `task test:e2e` separately.
- **Decisions D-028/D-029:** Pre-commit scope (lint+security, ~2-3s) and CI runner OS (ubuntu-latest) ratified.
- **Commits:** `e20a1bb` (T47 full verify on PR), `402eceb` (T47 Hudson audit findings), `ca85041` (T47 pre-commit refinement), `65e1184` (T47 CI setup checklist).

## Local validation Taskfile targets (post-Phase 2 R6b session)

**Date:** 2026-05-18 (Phase 2 post-R6b, local validation automation)
**Status:** Complete — 7 new Taskfile targets shipped
**Related:** Taskfile.yml, D-130, `.squad/decisions/inbox/hudson-validation-tasks.md`

**Context:** Brian's Phase 2 R6b session ended with manual validation steps: `dotnet ef database update`, `dotnet run --project src\TechInventory.Api`, `cd src\TechInventory.Web && pnpm run dev`, CSV import via cURL. Requested Taskfile automation for "one or two commands" fresh-checkout flow.

**Targets Shipped:**

1. **`task db:migrate`**: Apply EF Core migrations via `dotnet ef database update --project Infrastructure --startup-project Api`. SQLite file lives in repo root (dev) or Docker volume (compose).

2. **`task dev:api`**: Run API in Development mode (foreground, `http://localhost:8080`). Sets `ASPNETCORE_ENVIRONMENT=Development` to enable auth bypass (`DevBypassAuthenticationHandler` — no JWT/token required).

3. **`task dev:web`**: Run Web dev server (foreground, `http://localhost:5173`). Platform-specific `cd` to `src/TechInventory.Web` + `pnpm run dev`.

4. **`task dev`**: Print-only target. Displays instructions: "Run `dev:api` in Terminal 1, `dev:web` in Terminal 2." No automatic backgrounding (D-130: two-terminal pattern chosen for cross-platform reliability + log visibility).

5. **`task import:preview CSV=path/to/file.csv`**: POST CSV to `/api/v1/imports/preview` (multipart/form-data). Default: `data/Devices.csv` (Brian's 551-device file, gitignored). Pretty-prints `PreviewImportResult` JSON (Windows: `Invoke-WebRequest` + `ConvertTo-Json`, Linux/macOS: `curl` + `jq`).

6. **`task import:commit CSV=path/to/file.csv CONFIRM=yes`**: POST CSV to `/api/v1/imports/commit`. Destructive-op gate: requires `CONFIRM=yes` variable or prints warning and exits. Pretty-prints `CommitImportResult` (BatchId, ImportedRows, InvalidRows).

7. **`task validate:local`**: Headline target. Runs `restore` → `db:migrate` → prints success + next-step instructions (where to run `dev:api`/`dev:web`, health check URL, import commands, auth bypass reminder).

**Backgrounding Decision (D-130):**
- **Chosen:** Two-terminal pattern (no automatic backgrounding).
- **Why:** Cross-platform Taskfile backgrounding (Windows `Start-Process`, Linux `nohup &`) is fragile. Health-check loops add complexity. Explicit two-terminal pattern matches Brian's manual workflow, keeps logs visible in dedicated terminals, and works reliably on Windows/Linux/macOS.
- **Rejected:** `concurrently` npm package (adds dependency), `deps:` parallel (both block, logs interleaved), PowerShell background jobs / bash `&` (cleanup fragile, logs hidden).

**Auth Bypass Mechanism:**
- `appsettings.Development.json` line 3: `"Auth:DevBypass": true`
- `Program.cs` lines 45-62: `DevBypassAuthenticationHandler` registers when `Auth:DevBypass=true` and `ASPNETCORE_ENVIRONMENT=Development`
- Handler authenticates all requests as `dev-admin` (no JWT, no Entra, no header required)
- Import endpoints (`/api/v1/imports/*`) require `[Authorize]` (ImportsController.cs line 15); dev bypass satisfies this
- Guard: throws `InvalidOperationException` if `Auth:DevBypass=true` outside Development environment

**CSV Default Path:**
- `data/Devices.csv` (Brian's 551-device real data)
- `.gitignore` line 2 excludes this file from version control
- Both `import:preview` and `import:commit` accept `CSV=path/to/file.csv` override via Taskfile variables (`{{default "data/Devices.csv" .CSV}}`)

**Destructive-Op UX:**
- `import:commit` requires `CONFIRM=yes` variable
- Prints warning + exit if not set: "⚠️ WARNING: This will write devices to the database. Run with CONFIRM=yes to proceed: task import:commit CONFIRM=yes"
- Rationale: Explicit opt-in for destructive operation; protects against accidental data writes during experimentation

**Ports:**
- API: `http://localhost:8080` (launchSettings.json line 8)
- Web: `http://localhost:5173` (Vite default; package.json line 7 `vite dev` has no port override)

**Cross-Platform Implementation:**
- All targets with shell-specific commands use `platforms: [windows]` / `platforms: [linux, darwin]` pattern
- Windows: PowerShell (`Write-Host`, `Invoke-WebRequest`, `Get-Item`, `Test-Path`)
- Linux/macOS: bash (`echo`, `curl -F`, `jq`, `[ -f ]`)
- Multi-line string blocks (`cmd: |`) for instructional targets (dev, validate:local)

**Taskfile Header Update:**
- Added section documenting local validation targets:
  ```yaml
  # Local validation:
  #   task validate:local  — Fresh checkout → working dev environment with DB migrations applied
  #   task dev:api         — Run API (foreground, http://localhost:8080)
  #   task dev:web         — Run Web dev server (foreground, http://localhost:5173)
  #   task import:preview  — Preview CSV import (default: data/Devices.csv)
  #   task import:commit   — Commit CSV import to database (destructive)
  ```
- Rationale: Discoverability for new contributors; validation targets visible alongside existing `up`/`down`/`test`/`verify`

**Validation:**
- ✅ `task --list` shows all 7 new targets (plus 3 internal targets: `test:e2e:run`)
- ✅ Taskfile YAML syntax valid (no parse errors)
- ✅ All existing targets preserved (`up`, `down`, `test`, `verify`, `build`, `lint`, `openapi:export`, `logs`, `ps`, `clean`)
- ✅ Cross-platform cmd blocks for all interactive targets
- ✅ Default CSV path does NOT cause gitignored file to be tracked (`.gitignore` line 2 still in effect)

**Consequences:**
- Fresh checkouts: `task validate:local` → two terminals → `task import:commit CONFIRM=yes` → working dev environment with Brian's 551 devices imported
- No breakage: all Phase 1 R7 CI pipeline targets still work (`task verify` unchanged)
- Future: If docker-compose dev stack adds auth bypass, `import:*` targets can target compose API (`http://localhost:8080` from compose) instead of bare `dotnet run`

**Decision ID:** D-130 (local validation Taskfile targets)
**Decision Inbox:** `.squad/decisions/inbox/hudson-validation-tasks.md` (full rationale, auth bypass confirmation, backgrounding alternatives, open questions)

**Commit:** Will commit as `feat(taskfile): local validation + CSV import targets (db:migrate, dev, import:preview/commit, validate:local)` with body listing targets and D-130 reference.

## dev:up correction — one-command parallel (supersedes D-130 fragment)

**Date:** 2026-05-18 (Phase 2 post-R6b, correction round)
**Status:** Complete — `task dev:up` shipped with concurrently
**Related:** Taskfile.yml, package.json (new), pnpm-lock.yaml (new), D-131 (inbox/hudson-dev-up-fix.md)
**Commit:** `3a020d3` — feat(taskfile): real one-command dev:up — parallel API + Web with concurrently

**Context:** D-130 implemented two-terminal pattern (print instructions, no backgrounding) for cross-platform reliability. Brian explicitly requested **"one command task dev:up that does the db:migrate, dev:api and dev:web all in one go"**. User directive overrides D-130's two-terminal-only stance.

**Solution: `concurrently` npm package (battle-tested parallel process manager)**

1. **Root `package.json` created** with `concurrently` 9.2.1 as devDependency
   - Rationale: Repo-level dev tool (not web app runtime dep), adjacent to Taskfile.yml

2. **`task dev:up` target added:**
   ```yaml
   dev:up:
     desc: One-command dev — migrate + run API + Web in parallel (Ctrl+C exits both)
     deps: [db:migrate]  # sequential first
     cmds:
       - npx concurrently --kill-others --names "API,WEB" --prefix-colors "blue,green" \
           "dotnet run --project src/TechInventory.Api/TechInventory.Api.csproj" \
           "pnpm --dir src/TechInventory.Web run dev"
   ```
   - `--kill-others`: Ctrl+C kills both processes
   - `--names "API,WEB"`: Prefix output with [API]/[WEB]
   - `--prefix-colors`: Blue (API), green (Web) for readability
   - `ASPNETCORE_ENVIRONMENT=Development` set for auth bypass
   - Cross-platform (Windows `set + &&`, Unix inline env var)

3. **Standalone targets preserved:** `dev:api` and `dev:web` remain for solo debugging

4. **`validate:local` updated:** Now instructs `task dev:up` instead of two-terminal pattern

5. **Taskfile header updated:** Added `dev:up` to local validation section

**Why concurrently?**
- Cross-platform single command (no shell-specific scripting)
- Signal handling (`--kill-others` ensures clean teardown)
- Output prefixing makes interleaved logs readable
- Zero config (works via `npx`)
- Battle-tested in JS monorepo ecosystems

**Alternatives rejected:**
- PowerShell `Start-Job` + bash `& jobs`: platform-split, fragile cleanup
- `npm-run-all --parallel`: less ergonomic output
- Custom Node.js launcher: unnecessary maintenance
- Task parallel `deps:`: blocks, doesn't run in parallel

**Consequences:**
- ✅ `task dev:up` → db:migrate → API + Web parallel → Ctrl+C exits both (Brian's ask delivered)
- ✅ Root package.json + pnpm-lock.yaml committed (new devDependency)
- ✅ `dev:api` / `dev:web` still work for single-server debugging
- ⚠️ Fresh clones need `pnpm install` at root OR `npx concurrently` auto-installs on first run
- 🔄 D-130 fragment superseded: two-terminal pattern replaced for local dev workflow
- 🔄 D-130 other findings remain: auth bypass mechanism, CSV path default, CONFIRM gate

**Decision:** D-131 (`.squad/decisions/inbox/hudson-dev-up-fix.md`) — one-command parallel launcher supersedes D-130 two-terminal approach per user directive

**Not tested yet:** Actual parallel startup behavior, Ctrl+C teardown on Windows/Linux/macOS, signal propagation to child processes. Brian should verify.

## Windows Taskfile fix sweep (D-132)

**Date:** 2026-05-18 (Phase 2 post-R6b, emergency Windows compatibility fix)
**Status:** Complete — comprehensive Windows fix for all Taskfile targets
**Related:** Taskfile.yml, scripts/*.{ps1,sh}, D-132, commits `89cad1e`, `3a020d3`, `a2ae735`
**Commit:** `a2ae735` — fix(taskfile): Windows compatibility sweep

**Context:** Brian ran `task dev:up` on Windows. It failed with "Unable to retrieve project metadata" error on `db:migrate` target. Coordinator diagnosed root cause: **Task (taskfile.dev) uses `mvdan.cc/sh` (gosh — a Go-implemented POSIX-like shell) as its default shell on ALL platforms, including Windows.** It does NOT use cmd.exe or PowerShell, even for `platforms: [windows]` targets.

**Hudson's prior assumption was dangerously wrong:** "Backslashes on Windows should just work" led to shipping broken targets in `89cad1e` (validation targets) and `3a020d3` (dev:up concurrently). The moment Brian ran commands via `task` instead of directly in PowerShell, gosh broke every backslash-containing path.

**Root cause mechanics:**
1. **Backslash escape sequences:** In POSIX sh, `\T` in an unquoted argument consumes the backslash and yields literal `T`. So `src\TechInventory.Infrastructure\TechInventory.Infrastructure.csproj` became `srcTechInventory.InfrastructureTechInventory.Infrastructure.csproj` (no separators, file not found).
2. **cmd-specific syntax broken:** `set "VAR=value" && ...` doesn't work in gosh; `set` is for positional parameters (`$1`, `$2`), not environment variables.
3. **Multi-line PowerShell blocks broken:** Taskfile PowerShell blocks run through gosh, which can't parse PowerShell syntax.

**Five patterns applied to fix ALL targets:**

1. **Forward slashes for all paths** — .NET CLI, pnpm, npx, dotnet-ef all accept forward slashes on Windows
2. **Task's `env:` block for environment variables** — never use inline `set "VAR=value" &&` or `EXPORT VAR=value`
3. **`pnpm --dir` instead of `cd` commands** — avoids directory change + path separator issues
4. **Externalize PowerShell/bash-specific code to `scripts/*.{ps1,sh}`** — multi-line blocks extracted to:
   - `scripts/import-preview.ps1` + `scripts/import-preview.sh`
   - `scripts/import-commit.ps1` + `scripts/import-commit.sh`
5. **PowerShell for Windows directory removal** — cmd-only `if exist ... rd /s /q` replaced with `powershell -Command "Remove-Item ..."`

**Targets fixed (13 total):**
- `restore`, `build`, `lint`, `test` — `pnpm --dir` (no cd, no backslashes)
- `db:migrate` — forward slashes in --project paths (**CRITICAL** — was failing completely)
- `dev:api`, `dev:web`, `dev:up` — `env:` block + forward slashes + `pnpm --dir`
- `test:e2e:run` — `env:` block for BASE_URL, PowerShell Set-Location, forward slashes
- `openapi:export` — forward slashes
- `clean` — PowerShell `Remove-Item` (no cmd rd)
- `import:preview`, `import:commit` — externalized to scripts/*.{ps1,sh}

**Verification (Windows PowerShell 7, Task 3.44.0):**
- ✅ `task --list` — YAML parses, all 24 targets listed
- ✅ `task restore` — dotnet + pnpm deps installed (exit 0)
- ✅ `task db:migrate` — migrations applied, "already up to date" message (exit 0) — **THIS WAS THE CRITICAL FAILURE, NOW FIXED**
- ✅ `task dev:api` — started on http://localhost:8080, `ASPNETCORE_ENVIRONMENT=Development` set correctly, DevBypass warning visible, health check 200 OK
- ✅ `task dev:web` — Vite started on http://localhost:5173, "ready in 3178 ms"
- ✅ `task dev:up` — migrate ran, then concurrently launched `[API]` + `[WEB]` in parallel with color-coded prefixes (blue/green), both services started successfully

**Decision:** D-132 (`.squad/decisions/inbox/hudson-windows-taskfile-fix.md`) — comprehensive Windows compatibility sweep supersedes Windows-specific portions of D-130 and D-131

**Consequences:**
- ✅ All Taskfile targets now work on Windows, Linux, macOS
- ✅ Single cross-platform `cmd:` lines replace many `platforms:` splits (reduced YAML complexity)
- ✅ External scripts reusable for manual invocation or CI
- ⚠️ PowerShell scripts require execution policy bypass (`-ExecutionPolicy Bypass`)

**Lessons learned (Hudson's postmortem):**

1. **Task uses gosh on Windows, not cmd/PowerShell** — this is foundational, non-negotiable knowledge. Never assume platform-native shell semantics in Taskfile commands.

2. **Backslash escape semantics bite hard** — `\T` → `T`, `\I` → `I`, `\A` → `A`, etc. The "Unable to retrieve project metadata" error message from dotnet-ef was misleading; the real issue was mangled paths with no separators.

3. **`set` in POSIX sh is NOT environment variables** — it sets positional parameters (`$1`, `$2`, etc.). Must use Task's `env:` block or explicit shell invocation (`powershell -Command "..."`).

4. **`platforms: [windows]` does NOT switch to cmd/PowerShell** — it still runs through gosh. Only use `platforms:` for truly OS-specific commands (e.g., explicit `powershell.exe` or `bash` invocation).

5. **Forward slashes are universal** — .NET, Node.js, pnpm, dotnet-ef all accept them on Windows. No need for path separator abstraction or platform-split targets.

6. **Prior assumption was dangerously wrong** — "backslashes on Windows should just work" led to shipping broken targets in commits `89cad1e` and `3a020d3`. Brian's prior manual workflow worked because PowerShell (his shell) correctly interprets backslash paths and cmd-style environment variable syntax. The moment he ran the same commands via `task`, gosh broke every backslash-containing path. Hudson will never assume shell semantics again. **Test on the actual runtime (Task's gosh), not the dev's native shell.**

7. **Verification strategy for future Taskfile changes:** Always test via `task <target>` on Windows PowerShell, not by copying the command and running it directly in PowerShell. Direct PowerShell invocation bypasses Task's gosh shell entirely and gives false confidence.

**Handoff notes:**
- All targets verified working on Windows
- Brian can now run `task dev:up` successfully (one-command dev environment)
- CI pipeline unaffected (already used forward slashes for Linux runner)
- No open Windows compatibility issues remain

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-05-19 — D-135 Web reverse proxy runtime
- **Static adapter contract:** `src/TechInventory.Web/svelte.config.js` uses `@sveltejs/adapter-static`, so the container runtime must serve files from `build/`; `node build` is invalid and was a latent startup bug in the old Dockerfile.
- **Prod ingress shape:** Brian's home deployment is same-origin at `https://inventory.denicolafamily.com`; the web container owns both SPA hosting and `/api/*` proxying to `http://api:8080` over `techinv-net`.
- **Compose stance:** Keep web published as `3000:80` for local prod-shape testing, but treat the external TLS proxy as the only intended public entrypoint in production.
- **CORS defense-in-depth:** Even with same-origin proxying, `src/TechInventory.Api/appsettings.Production.json` should whitelist `https://inventory.denicolafamily.com` so accidental direct exposure still has an origin allow-list.
- **Verification lesson:** Check tool availability first; this Windows session still lacks both `docker` and `nginx`, so container validation can be blocked even when repo-local build and config changes are ready.

---

## 2026-05-19 — Prod deployment plumbing (single-compose rewrite)

**Branch:** `hudson/prod-deployment-plumbing` · **Commit:** `8f2661b` · **Requested by:** Brian

Shipped a single-PR rewrite of the prod deployment surface, mirroring Brian's coin-collection-app compose posture. Brian's directives for this round explicitly override prior infra plans.

**Files changed**
- `.env.example` — rewritten as a tight prod template. `IMAGE_TAG`, Entra required vars, `openssl rand -base64 48` signing key, opt-in break-glass seed, `Cors__AllowedOrigins__0`, optional OTEL/Serilog. `CHANGEME_` placeholders are greppable.
- `docker-compose.yml` — single prod-only file. GHCR images `ghcr.io/briandenicola/tech-inventory-{api,web}:${IMAGE_TAG:-latest}`. `${VAR:?msg}` for required env, `${VAR:-default}` for optional. Healthcheck-gated `depends_on`. Named volume `techinv-data`. `restart: unless-stopped`. NO hardening, NO backup sidecar, NO resource limits, NO build sections.
- `docker-compose.prod.yml` — DELETED. Overlay folded into the single file.
- `.github/workflows/release-images.yml` — narrowed to `v*.*.*` tag pushes only. Matrix job builds api+web for linux/amd64. Tags `:latest` AND `:${{ github.ref_name }}`. `provenance:false`, `sbom:false` (Bishop owns scanning).
- `docs/deployment.md` — 10-section runbook: prereqs, first-time setup (full-checkout + compose-only flavors), pull/start, NPM upstream config with nginx snippet, first sign-in (Entra + break-glass + decommission), updating, version pinning, troubleshooting, ad-hoc SQLite backup, related docs.

**Decisions baked in this round (Brian's explicit overrides)**
1. Single-compose: no `prod.yml` overlay, no `dev.yml`. Local dev uses `task dev:up` (dotnet+pnpm, no docker).
2. GHCR-only images; no local builds in the compose file.
3. No container hardening: stripped `read_only`, `tmpfs`, `cap_drop`, `security_opt: no-new-privileges`, deploy.resources. NPM + Entra are the security boundary.
4. No backup sidecar: Litestream service removed; Brian handles backups externally. `docs/deployment.md` §9 has an ad-hoc `sqlite3 .backup` one-liner.
5. `${VAR:?msg}` env validation pattern for required env.
6. `depends_on: service_healthy` web→api gating preserved.

**Open questions surfaced to Brian**
1. **Taskfile fallout** — `task up`, `task test:e2e`, `task backup:verify`, `task backup:restore`, `task clean` all reference the old compose shape (`build:` blocks or `docker-compose.prod.yml`). Will break next session. Per brief I did NOT touch Taskfile — flagged as follow-up PR.
2. **Seed env-var naming** — brief said `SeedEmail`/`SeedDisplayName`, but code (`LocalAdminSeedHostedService.cs`) binds `Auth:Local:SeedUsername` only. Used the code-binding names. May indicate planned-but-unshipped schema change.
3. **Release trigger scope** — narrowed to `v*.*.*` tag pushes only per brief. Previous workflow also tagged `:latest` on main push and had `workflow_dispatch`. Confirm strict tag-only is right.

**Validation done**
- `python -c "import yaml; yaml.safe_load(...)"` on both new YAML files — clean.
- Pre-commit hook (lint + secret scan) — passed.
- `docker compose config` — deferred (no Docker on Hudson's runtime).
- Full `verify.ps1` skipped intentionally (config/docs-only change, full E2E rebuild has poor cost-to-signal ratio; Quality Gate reruns on PR).

**PR (Brian opens manually — gh CLI not installed on runtime)**
- Branch pushed: `hudson/prod-deployment-plumbing`
- Open via: `https://github.com/briandenicola/tech-inventory/pull/new/hudson/prod-deployment-plumbing`
- Pre-filled body lives at `.git/PR_BODY_HUDSON.md` (uncommitted, in .git/, won't be tracked)
- Title: `feat(deploy): prod deployment plumbing (env, GHCR release, compose, runbook)`
- DO NOT MERGE per brief — Brian will revise.

**Refs:** D-135, D-139, D-140.

## 2026-09-02 — T101: retire the broken Playwright harness safely

**Branch:** `chore/agentic-development-foundation` (uncommitted — final commit
coordination happens after the full foundation package lands) · **Requested
by:** briandenicola · **Spec:** `specs/004-agentic-development-foundation/`

T102 had final independent APPROVED verdict from Ripley; T101 was explicitly
authorized. Deleted `tests/e2e/**` (28 tracked files, cross-checked
row-by-row against `coverage-migration.md` §4 waves D1–D5 before deletion —
nothing removed lacked a matrix row), `scripts/run-e2e.ps1`/`.sh`, and
`.squad/skills/playwright-e2e-scaffolding/`. Removed every Playwright
invocation from `Taskfile.yml`, `scripts/verify.ps1`/`.sh`,
`.github/workflows/ci.yml`. Neither root nor `src/TechInventory.Web`
`package.json` ever depended on Playwright; confirmed with a full clean
reinstall (`rm -rf node_modules && pnpm install --frozen-lockfile`) — 617
packages, 0 downloaded, no Playwright package, browser-cache directory
untouched.

**The guard-design lesson worth remembering:** a naive "fail if the word
Playwright appears anywhere active" guard is self-defeating the moment you
write your *own* retirement-notice prose in those same active files — every
sentence explaining "Playwright is retired" trips the guard you just built.
First pass produced 32 false-positive violations, all my own newly-authored
text. Two ways to fix this: (a) make the guard smarter about distinguishing
promises from historical notices (fragile, NLP-flavored, hard to audit), or
(b) just don't use the trigger word in prose you write in active files —
say "the retired browser-E2E harness" and point readers to the one
authoritative citation in `specs/` (already exempt) for the proper noun.
Went with (b): reworded ~15 files' worth of my own retirement-notice
sentences, added one narrow, well-justified exemption for the guard's own
script filename (self-invocation isn't a stale mention), and ended at a
genuinely dead-simple keyword-ban + path-exemption guard — 0 heuristics, 0
NLP, fully auditable by reading the exemption list. `docs/testing/manual-pwa-validation.md`
is the one deliberate exception where the word *must* stay, because the
checklist's own rationale depends on naming what it's compensating for.

Built `scripts/check-stale-playwright-references.mjs` (keyword scan + two
structural hard-fails: any `tests/e2e/` path, any `playwright.config.*`
anywhere) with `scripts/check-stale-playwright-references.test.mjs` (15
`node:test` cases, zero new framework, matches the existing
`scripts/check-security.mjs` pattern). Wired into `Taskfile.yml`
(`check:stale-refs`) and both `verify` scripts at the step Playwright used to
occupy. Live run: 0 active references across 901 tracked files.

**Two judgment calls flagged for independent review, not decided silently:**
1. `.squad/log/**` / `.squad/orchestration-log/**` — dated historical
   session logs not individually named in the T103 matrix's §5.5 RETAIN
   list, exempted on the same principle already applied to
   `.squad/session-log.md`/`agents/*/history.md` (gitignored for future
   writes, narrative past tense, dated filenames).
2. `.specify/memory/constitution.md` / `docs/prd.md` still mandate Playwright
   by name — §5.4 says amending them needs a separate ADR, which this task
   surfaces rather than performs. AC-005's "zero references" therefore has
   two explicit, matrix-authorized exceptions, recorded in `validation.md`,
   not silently swept under.

Issue #89 was already closed by briandenicola before this session (state
reason `NOT_PLANNED`) with no evidence comment — added a durable
retirement+migration evidence comment rather than reopening/reclosing.
Updated `tasks.md`, `validation.md`, `plan.md`, `coverage-migration.md` §13
with the full evidence trail. Did not touch T104 (verification-interface
consolidation) or T105 (tamper-testing) beyond the minimum guard wiring
T101 needed. Full validation: guard 0/901 + 15/15 tests, clean install 0
downloads, `dotnet build`/`dotnet test` (278 unit + 292 integration, all
passing), `pnpm run lint` clean, 83 Vitest files / 649 tests passing
including the previously-stale `DeviceForm.test.ts` (27/27, genuinely
un-skipped by T102, not just re-labeled).

**Refs:** T101, AC-005, `coverage-migration.md` §13, `validation.md` AC-005.

## 2026-09-02 — T104: one authoritative verification interface, Playwright-free

**Branch:** `chore/agentic-development-foundation` (uncommitted) · **Requested
by:** briandenicola · **Spec:** `specs/004-agentic-development-foundation/`

T101 and T102 were both `DONE` and reviewer-approved; T104 was released by
Ripley's T101 re-review with two conditions attached (`validation.md`
§7.8.5/§7.8.6): don't deepen the constitution/PRD Playwright contradiction,
and close F-4 (wire the stale-reference guard into `quality-gate.yml`;
resolve `.env.e2e`/`docker-compose.e2e.yml`). Made `Taskfile.yml` the single
verification entrypoint: `verify:fast` (format, backend build, frontend
type/Svelte check, lint, unit + frontend tests), `verify:contracts`
(stale-reference guard, OpenAPI/generated-client drift, EF migration drift,
real-HTTP integration tests), `verify:full` (fast + contracts + frontend
production build + vulnerability scan), `verify` (alias to full). Rewrote
`scripts/verify.ps1`/`.sh` as thin wrappers that just check `task` is on
PATH and run `task verify` — replacing two separately-maintained 9-step
pipelines. Rewrote `.github/workflows/quality-gate.yml`'s `dotnet`/`web`
jobs into one `verify` job that installs Task (`arduino/setup-task@v2`) and
calls `task verify` — this is what closes F-4, since `check:stale-refs` is
now part of that same call and runs in the actual merge-blocking workflow
for the first time.

**Two gotchas worth remembering for next time:**
1. `dotnet run --project X -- <args>` runs the child process with its CWD
   set to the *project* directory, not the caller's shell CWD. A relative
   output path in `check:openapi-drift` silently landed under
   `src/TechInventory.Api/TestResults/...` instead of the repo root until I
   forced an absolute path via Task's `{{.ROOT_DIR}}`.
2. Hit the exact self-inflicted trap documented in my own T101 entry above —
   wrote "stale-Playwright guard" in two `Taskfile.yml` task descriptions and
   tripped the guard's own keyword scan. Reworded to "retired-harness
   stale-reference guard" per the established convention (say what it does,
   cite the one exempt spec file for the proper noun).

Built `scripts/check-test-floors.mjs` — fail-closed collected-test floors for
unit/integration/frontend, matching the existing `check-security.mjs`/
`check-stale-playwright-references.mjs` pattern (repository-native tooling,
no new test framework). Floors are ~10% below **measured** baselines, not
guessed: unit 278→floor 250, integration 296 collected (292 passed/4
skipped)→floor 265, frontend 649 tests/83 files→floor 580/74. Proved the
fail-closed behavior directly: `dotnet test --filter
"FullyQualifiedName~NoSuchTestNamespaceXYZ"` exits 0 (not an error!) with a
TRX showing `<Counters total="0">` — exactly the silent-zero-collection
failure mode T103 found in the retired Playwright harness, and exactly what
these floors now catch. Added `--collect:"XPlat Code Coverage"` to the
floor-check's `dotnet test` invocations so Quality Gate's old coverage-report
side effect isn't lost in the consolidation (still not asserting a
percentage threshold — only test-count floors are enforced; that gap is
called out explicitly in `.github/workflows/README.md`'s security-gate
table, not silently dropped).

Pinned `dotnet-ef` as a local tool (`.config/dotnet-tools.json`, version
10.0.11) rather than relying on the previously-unpinned global install, so
`check:migration-drift` (`dotnet ef migrations has-pending-model-changes`)
resolves deterministically in CI too.

Resolved `.env.e2e`/`docker-compose.e2e.yml`: grepped for any consumer other
than the deleted Playwright harness, found none — all "real HTTP integration
tests" in this repo use in-process `WebApplicationFactory<Program>`, not a
running container. Deleted both files rather than renaming them; updated
the guard's `EXEMPT_EXACT_PATHS` (a file that doesn't exist needs no
exemption), its test file (flipped from "exempts" to "does NOT exempt" as a
regression guard), `.gitleaks.toml`'s now-dead `.env.e2e` allowlist entry,
and a stale `docker-compose.yml` comment naming a `task test:e2e` that no
longer exists. Also deleted `scripts/check-openapi-drift.sh` — it was
`quality-gate.yml`'s old drift check, and its logic is now 100% duplicated by
`Taskfile.yml`'s `check:openapi-drift`; nothing else referenced it.

**No stage of `verify:fast`/`verify:contracts`/`verify:full` requires
Docker.** Confirmed both by grepping the integration test project for any
Docker reference (none) and by running the full suite without a compose
stack up.

Validated everything directly on this Windows machine (no bash available):
`task verify:fast` ran clean end to end (format, build, unit 278/278,
frontend 649/649, lint, type-check — exit 0). `task verify:contracts` passed
`check:stale-refs` and `check:openapi-drift`, then stopped at
`check:client-drift` — this is *expected*, not a T104 defect: this branch's
own uncommitted T101/T102 changes to `openapi.yaml` (the 403-response
additions) create a real diff against git's last-recorded state, confirmed
present in `git status` before I touched anything. Did not commit it — not
my call, per the explicit instruction not to commit or push. Validated the
CI YAML changes are at least syntactically correct (`python -c "import
yaml; yaml.safe_load(...)"` on both workflow files) but could not run them
on GitHub Actions without pushing, so end-to-end CI execution of the new
`quality-gate.yml`/`ci.yml` is a disclosed, not-yet-observed gap — flagged
explicitly in `validation.md` §9.4 rather than claimed.

Did not touch `.specify/memory/constitution.md` or `docs/prd.md` — the
Playwright-mandate contradiction stays exactly as `validation.md` §7.8.5
requires: honored, not resolved, not deepened. Did not start T105 (branch
protection alignment, tamper-testing every guard) beyond exposing the stable
task/check names T105 will need. Updated `tasks.md`, `validation.md`,
`plan.md`, `coverage-migration.md` §14 with the full evidence trail. Marked
T104 `VALIDATING`, not `DONE` — that transition is a human/reviewer call
(`plan.md` §2.3), not mine to make.

**Refs:** T104, AC-008, `coverage-migration.md` §14, `validation.md` §9.


### 2026-05-19 — DevBypass rip from E2E stack (parallel with Bishop + Vasquez)

Brian asked the team to rip the entire `Auth:DevBypass` shim from production. My slice: own the E2E stack's transition from DevBypass-everywhere to F025 local-account sign-in. Worked in parallel with Bishop (backend tests / API code) and Vasquez (frontend shim removal).

**Files touched (all infra/test scaffolding — no app code):**
- `docker-compose.e2e.yml` — dropped `Auth__DevBypass: "true"` (api env) and `VITE_AUTH_DEV_BYPASS: "true"` (web build arg). Kept `ASPNETCORE_ENVIRONMENT: Development` (the seed service refuses Production without `SeedAllowInProd`; Development sidesteps that guard). Added `Auth__Local__Seed{Enabled,Username,Password,RequireChangeOnFirstLogin}` so `LocalAdminSeedHostedService` provisions a usable admin (`e2e-admin` / `e2e-admin-passw0rd-CHANGEME`) on every boot. `SeedRequireChangeOnFirstLogin=false` so the fixture can sign in without the change-password gate.
- `.env.e2e` — rewrote the top comment. Stub Entra config stayed (compose's `${VAR:?}` interpolation guards still require it; the policy scheme routes by `iss` so stubs are never consulted). Seed creds documented as a breadcrumb pointing at docker-compose.e2e.yml as the single source of truth. The clearly-fake repeated-pattern HS256 signing key + `#gitleaks:allow` inline stayed — local sign-in actually USES it now, so it must be present and ≥32 bytes.
- `tests/e2e/fixtures/auth.ts` — full rewrite. Seed creds exposed as `SEED_USERNAME`/`SEED_PASSWORD` constants (overridable via `E2E_SEED_USERNAME`/`E2E_SEED_PASSWORD`). New `seedLogin()` helper POSTs to `/api/v1/auth/local/login` against an ephemeral `APIRequestContext` and decodes the HS256 JWT in Node (base64url → `Buffer.from(...).toString('utf8')`). The `authenticated` fixture now does TWO things: (1) injects `ti_local_token` + `ti_local_meta` into sessionStorage via `page.addInitScript` BEFORE first navigation, so the SvelteKit root layout's `hydrateLocalSession()` picks them up cleanly; (2) overrides Playwright's default `request` fixture to attach `Authorization: Bearer <token>` so journey specs that destructure `{ adminPage, request }` (jrnys 3, 4, 5, 7, 8) keep working without per-test changes.
- `tests/e2e/fixtures/api.ts` — header comment rewritten to describe the bearer-token flow.
- `tests/e2e/journeys/01-sign-in.spec.ts` — full rewrite. Test 1 now asserts an unauthenticated visit to `/devices` redirects to `/auth/login` (used `page.waitForURL('**/auth/login')` to handle the root-layout-onMount → guard-redirect handoff race). Test 2 verifies root renders without `pageerror`.
- `tests/e2e/journeys/02-sign-in-denied.spec.ts` — selectors changed from `/dev-admin/i` to `/user menu/i` (matches the button's `aria-label={t('header.userMenu')}` accessible name, which is stable across auth methods). Added a `toContainText(/local admin/i)` sanity assertion that fail-fast-detects seed misconfiguration. The `test.fixme` is now potentially implementable (since `handleSignOut()` calls `clearAuth()` which drops local-session keys); left as fixme with an updated note about Playwright's per-test `addInitScript` re-hydration making a naive implementation flaky.
- `tests/e2e/journeys/07-detail-view.spec.ts` — one-word comment scrub ("dev-bypass identity" → "seeded local admin").
- `tests/e2e/README.md` — Authentication section rewritten end-to-end; dropped the dual-path narrative since there's only one path now.
- `docs/known-issues.md` — deleted the `auth-jwt-happy-path-tests` section (test names referenced `DevBypass*` integration tests that no longer exist; Bishop is concurrently deleting / rewriting those tests anyway).
- `docs/testing.md` — three line-level scrubs: "dev-bypass JwtBearer scheme" → "local-issuer JwtBearer scheme"; E2E fixture description rewritten; auth-in-tests table rewritten.
- `docs/deployment.md`, `docs/operations.md` — confirmed clean (no DevBypass references via grep).
- `.env.example` — added `Auth__Local__SeedRequireChangeOnFirstLogin=true` with a comment forbidding `false` in prod.
- `scripts/run-e2e.{sh,ps1}` — minimal scrub: removed the unused `VITE_AUTH_DEV_BYPASS=true` export (no consumer now that the build arg is gone) and rewrote the misleading "forces Development env / DevBypass" header comment. Logic flow is identical — still `--env-file .env.e2e -f docker-compose.yml -f docker-compose.e2e.yml`.

**Verification:**
- `git diff --stat` shows 13 files changed, all in my swimlane. No overlap with Bishop's modifications (DevBypassAuthenticationHandler.cs deletion, Program.cs, appsettings.json, integration tests, TestAuthHandler.cs) or Vasquez's (Dockerfile, lib/auth/index.ts, (authenticated)/+layout.svelte).
- `npx playwright test --list` over journeys/01, 02, 03, 07 enumerates **72 tests across all 6 browser projects** with no compile errors. Pre-existing `test.todo` errors in journeys 09-12 reproduce on a clean checkout too (verified via stash/pop) — not my regression.
- Could NOT run `task test:e2e` end-to-end: **Docker is not installed on this Windows dev host** (no docker.exe, no Docker Desktop, no WSL). Same gap I noted in the 2026-05-18 history entry. Left the working tree dirty per Brian's instruction so Copilot CLI can roll Hudson + Bishop + Vasquez into one commit; runtime verification will happen on a Docker-capable host (or in CI).

**Decisions / things future-me should know:**
- The `page.addInitScript` injection pattern is the only way that works. Tried mentally the "goto / → page.evaluate → goto /devices" sequence Brian suggested in the spawn prompt — it doesn't work because the root +layout.svelte's `onMount` only runs ONCE per page-context lifetime; subsequent client-side navigations within the same context don't re-trigger `hydrateLocalSession()`, so after the first goto's onMount marks the store unauthenticated, the second goto's (authenticated) guard sees `isAuthenticated=false` and redirects. `addInitScript` runs on EVERY navigation BEFORE any in-page script, so sessionStorage is always populated by the time hydration tries to read it. Documented this inline in the fixture.
- The user-pill button's accessible name is the `aria-label` ("User menu"), NOT the inner displayName text. The original `/dev-admin/i` regex looked wrong even before the rip — either those tests weren't running cleanly or Playwright's accessible-name computation has fallbacks I don't fully understand. Going forward, use `/user menu/i` and assert displayName via `toContainText(...)` as a separate, explicit check. More robust to i18n + DevBypass-style identity churn.
- The desktop user-pill is `class="hidden md:block"` — mobile projects won't see it. Journey 02 on chromium-mobile / webkit-mobile / firefox-mobile will likely fail. That's pre-existing structural debt (the mobile menu has a different DOM shape under the hamburger). Out of scope for this rip; flag if Apone hasn't picked it up.
- Seed creds (`e2e-admin` / `e2e-admin-passw0rd-CHANGEME`) live in TWO places: `docker-compose.e2e.yml` (authoritative) and `tests/e2e/fixtures/auth.ts` (defaults, overridable via env). If Brian wants to rotate, change BOTH — or set `E2E_SEED_USERNAME` / `E2E_SEED_PASSWORD` in the env that runs Playwright.
- Whoever runs the E2E suite first on a Docker host should expect the seed log to emit `[F025] Seeded local Admin account 'e2e-admin' with SeedRequireChangeOnFirstLogin=false (intended for dev/E2E only — never enable this in production).` That WARNING is a feature, not a bug.

**Open blockers for someone with Docker:**
1. Run `task test:e2e`. Expect journeys 01, 02 (desktop only), 03, 04, 05, 06, 07, 08, 13 to be the targets. Journeys 09, 10, 11, 12 are stubbed via `test.todo` (pre-existing, not mine).
2. If journey 01 test 1 ("redirects to /auth/login") flakes, the timeout on `waitForURL` (currently 15s) may need a bump — the (authenticated) guard waits on the root layout's MSAL probe to finish before reading the store.
3. If journey 02's `toContainText(/local admin/i)` fails, check api logs for the F025 seed warning — `LocalAdminSeedHostedService` writes a critical log on success.

### 2026-06-11 — Image Tagging & Corepack Policy Shift

- **Tagging policy change (D-014)**: Main branch pushes now publish images as `:latest` (rolling deployable) in addition to `:main` and `:sha-<short>`. The home server's default `${IMAGE_TAG:-latest}` now pulls current main state. Semver tags (`v*.*.*`) continue to publish `:latest`, their version tag, and `:sha-<short>`. Workflow comments were updated to clarify the new intent.
- **Corepack pnpm pin**: Added `src/TechInventory.Web/package.json::packageManager` as `pnpm@11.1.2`. Reason: Docker/Corepack had floated to pnpm 11.5.3, which enforces minimum-release-age against the existing lockfile and blocked image builds. Pinning the previously validated pnpm version keeps Docker installs deterministic without refreshing the dependency graph.
- **Constitutional reinterpretation**: §6.1 ("No `latest` tags — pinned, digested references in compose") remains in force; the principle is preserved because the deployer can still pin `IMAGE_TAG` in `.env` if stricter stability is needed. The workflow publishes `:latest`; the deployment chooses whether to consume it or lock a version. Decision filed in `.squad/decisions/inbox/D-014-image-tagging-policy.md`.

### 2026-06-13 — Deep Platform / Operations Audit

Conducted comprehensive audit of infrastructure, local automation, container security, CI/CD parity, and observability. Full report: `.copilot/platform-audit-2026-06-13.md`.

**Critical Findings (3)**:
1. **docker-compose.prod.yml referenced but deleted** — Commit 5d8751f merged prod overrides into docker-compose.yml, but Taskfile backup:verify/restore tasks still invoke the deleted file. `task backup:verify` and `task backup:restore` **fail immediately**. Git history confirms the file was intentionally removed; Taskfile was not updated.
2. **Backup infrastructure orphaned** — ops/litestream.yml exists with full Litestream config (S3 replica, 30-day retention, 24h snapshots), but deployment.md §9 states Litestream was "intentionally pulled out." Backups tasks remain in Taskfile. Creates confusion: does Brian want continuous Litestream or external snapshots only?
3. **Windows E2E determinism gap** — Taskfile `test:e2e:run` on Windows runs inline powershell that diverges from `scripts/run-e2e.ps1` (missing --env-file and compose-file args). Docker-compose invocation and readiness-check timing differ between the Taskfile and the script, creating race conditions on Windows.

**High-Risk Findings (4)**:
- Base image tags floating (no SHA digest pins) — API/Web Dockerfiles use `alpine`, `1.27.5-alpine`, `10.0-alpine` without digests. Trivy scans become non-reproducible.
- Web healthcheck endpoint is `/health` (liveness) vs API `/health/ready` (readiness) — asymmetry; unclear if intentional.
- Web Dockerfile has inefficient USER switching (root → upgrade → user 101).
- CI pipeline disabled and muted (workflow_dispatch only); no Windows-native GitHub Actions runner exists.

**Medium-Risk Findings (4)**:
- Observability (OpenTelemetry) configured but undocumented.
- IMAGE_TAG convention unclear — .env.example defaults to `:latest` despite deployment.md recommending semver pinning.
- ESLint/Prettier configs not explicit (may use package defaults).
- Health check endpoints not documented in OpenAPI spec.

**Validation Checklist**: All core operations validated as functional — compose up/down, healthchecks, E2E scripts, release pipeline, secret scanning. No active outages. Production-deployable but needs design alignment.

**Owner**: Hudson (audit complete) → Brian (design questions + approvals) → Hudson (remediation, est. 8–12 hours).

**Design Questions for Brian**:
1. Backup strategy: Is Litestream returning to prod, or are external snapshots permanent?
2. Windows CI: Do you want Windows-native GitHub Actions runners (docker available?), or Linux-only sufficient?
3. Container image digests: Should updates be manual (Hudson reviews + commits) or automated (Dependabot)?
4. Health check asymmetry: Should web use `/health/ready` or is `/health` intentional?

**Next Actions (Prioritized)**:
1. Resolve docker-compose.prod.yml mismatch (F1 CRITICAL) — decide backup strategy within 1 week
2. Fix Windows E2E determinism (F3 CRITICAL) — update Taskfile test:e2e:run to delegate to script
3. Pin container base image digests (H1 HIGH) — extract SHAs for all bases, update Dockerfiles
4. Add Windows GitHub Actions runner (H4 HIGH) — create test-windows job in quality-gate.yml
5. Update .env.example IMAGE_TAG default (M2 MEDIUM) — pin to v1.0.0 + add deployment warning

---

### 2026-06-14: Engineering Audit Session (Hudson)

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-hudson.md`

**Key Audit Findings:**
- Docker Compose stack functional ✓
- GitHub Actions CI workflow in place ✓
- Basic healthcheck endpoints defined ✓
- **CRITICAL:** Production compose/backup task drift
  - Home server compose configuration drifting from CI/CD image publishing
  - Backup task incomplete/undocumented
  - Risk: Prod deployment may not match tested artifacts
- **CRITICAL:** Windows E2E mismatch
  - E2E tests run on Linux/Mac in CI
  - Windows localhost environments have different behavior (port binding, path handling)
  - Risk: Tests pass in CI but fail locally on Windows
- Container image / healthcheck issues
  - Image build not capturing all dependencies (pnpm lockfile drift possible)
  - Healthcheck endpoints not consistently available on all services
  - Risk: Silent failures on container startup

**Decisions Merged:**
- D-173: Main Branch Image Tagging Policy Shift (`:latest` for rolling deployment)

**Next Steps:** Audit and sync prod compose with CI image definitions, add Windows-specific E2E test job, document backup strategy.


### 2026-09-02 — T105 revision: closing blockers B-3 and B-4 only

**Branch:** `chore/agentic-development-foundation` (uncommitted) · **Requested
by:** briandenicola · **Spec:** `specs/004-agentic-development-foundation/`.
Bishop's independent reviewer gate rejected T105 (`validation.md` §13) with
four blockers. Ripley and Apone were locked out for this revision cycle;
Hicks owned B-1/B-2 in parallel (already visible, fixed, in this shared
working tree — 22/22 `check:stale-refs` tests passing before I touched
anything). My scope was exactly **B-3** and **B-4**. Full record:
`specs/004-agentic-development-foundation/t105-setup-revision.md`.

**B-3 — §2.10 exception clause unmet.** Constitution §8.3 and
`docs/adr/0002-…` L111 both already asserted a `plan.md §2.10` exception
record existed for (a) unapplied branch protection and (b) the manual PWA
checklist — but neither record existed after §6.1 closed; both were forward
references to an empty register. Added `plan.md` §6.2 (branch protection)
and §6.3 (manual PWA checklist), mirroring §6.1's exact template (Rule
contradicted / Scope / Reason / Owner `briandenicola` / Start date
2026-09-02 / Closure trigger / Class `REVIEWED` not `ENFORCED`). Corrected
the constitution §8.3 and ADR 0002 citations to point at §6.2/§6.3
precisely instead of the generic `§2.10` (constitution version bumped
1.1.0→1.1.1, new §15 row explicitly noting no rule was weakened — pure
citation-precision fix). Annotated `t105-governance-evidence.md` §6 open
item 3 as resolved (one line, not a rewrite of Ripley's file). Appended a
"Revised by Hudson" block to `tasks.md`'s T105 section (same pattern as the
existing Hicks/T104 precedent) and a new AC-009 row to `validation.md` §5 —
**did not touch Bishop's §13 verdict text**. Reviewed
`.github/pull_request_template.md`'s Explicit Exceptions table: it's a
generic per-PR fill-in table, not a `§2.10`-specific citation, so it needed
no change.

**B-4 — clean-checkout `restore → verify` didn't provision gitleaks.**
`check:security` depended on a pinned gitleaks binary that only
`task hooks:install` provisioned, never `task restore`. Added a new
`tools:gitleaks` Task (idempotent — a `status:` check calling a new
cross-platform script `scripts/check-gitleaks-installed.mjs`, since Task's
`status:` field does **not** support the `{cmd, platforms}` object form used
elsewhere under `cmds:` — using it there raises `cannot unmarshal !!map into
string`, a real schema gotcha worth remembering). `restore` and
`hooks:install` now both depend on `tools:gitleaks` instead of each running
their own copy of `install-gitleaks.ps1`/`.sh` — one install owner.
`check:security` now declares `deps: [restore]` and documents that it
consumes, not installs, the binary, failing clearly (no silent network
fallback) if it's somehow missing. 10/10 new unit tests
(`check-gitleaks-installed.test.mjs`) cover path resolution per-platform and
exact-version matching (including a regression guard against substring
false positives like "8.30.10" matching pinned "8.30.1"), wired into
`restore`'s own `cmds:` so they always run. Removed the now-duplicate
"Install pinned gitleaks" steps from both `quality-gate.yml` and `ci.yml`,
replacing the latter with `task restore` (still needed there for an earlier
direct `check-security.mjs --diff-range` call before `task verify` runs).

**Clean-state proof, done carefully (no broad deletes, no restore-to-HEAD):**
moved the pinned `.tools/gitleaks/gitleaks.exe` aside via `Move-Item` (a
first attempt using `Remove-Item` was silently reversed by something in
this sandboxed environment before the next command ran — same hash, same
old timestamp — so I switched to rename+`Test-Path`, which reliably proved
absence); confirmed the new script independently reports "not installed"
(exit 1); ran `task restore`, which re-provisioned the exact pinned
version, byte-identical (SHA-256) to the pre-test backup; ran
`task check:security` against it — gitleaks executed correctly (no "not
installed" error). Cleaned up: removed the backup, re-verified the hash,
confirmed `git status` shows no tracked drift from the experiment.

**Two disclosed, out-of-scope observations, not fixed:** (1)
`validation.md` §13.2 itself quotes literal tamper-test text
`localStorage` . `setItem('access_token', [REDACTED AUTH-TOKEN PERSISTENCE PAYLOAD])`, which trips
`check-security.mjs`'s own `tokenStoragePattern` regex — meaning
`task check:security`/`task verify` currently fails on the *unmodified*
tree for a reason wholly unrelated to B-3/B-4; fixing it would mean editing
Bishop's verdict content, which is out of scope. (2) `check-security.mjs`'s
bare-`gitleaks`-on-`PATH` fallback can hang indefinitely on Windows when no
bundled binary and nothing on `PATH` exist (likely an App Execution Alias
quirk) — pre-existing script behavior, not touched here. Both recorded in
`t105-setup-revision.md` §4 for the next reviewer.

**Validated:** `task --list-all` parses clean; gitleaks unit suite 10/10;
`task check:stale-refs` 22/22 (0 violations, 940 files); `task
check:client-drift` 9/9; `task check:vulnerable` 13/13; `task hooks:install`
reuses `tools:gitleaks` correctly. Did not run a fully green `task verify`
— expected to still stop at `check:security` for the disclosed, unrelated
reason above, not from any regression in this revision. **Did not** mark
T105 `DONE`, edit Bishop's verdict, touch Ripley/Apone's authored content
beyond the single annotation noted above, or commit/push anything.

**Refs:** T105, AC-009, `validation.md` §13/§5, `plan.md` §6.2/§6.3,
`t105-setup-revision.md`, `.squad/decisions/inbox/hudson-t105-setup-revision.md`.


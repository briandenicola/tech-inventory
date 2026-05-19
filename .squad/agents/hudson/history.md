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

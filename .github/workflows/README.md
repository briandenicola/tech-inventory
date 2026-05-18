# GitHub Actions Workflows

This document describes the CI/CD workflows that enforce code quality and security gates on Tech Inventory.

## CI Pipeline (`ci.yml`)

**Triggers:** Push to `main`, Pull requests to `main`  
**Duration:** ~8–12 minutes  
**Status:** Required to pass before merge

### Workflow Steps

1. **Checkout code** — Full history (fetch-depth: 0) for commit analysis
2. **Setup .NET 10** — Installs the pinned .NET SDK
3. **Setup Node 22** — Installs the pinned Node.js runtime
4. **Enable pnpm** — Activates pnpm via corepack
5. **Install gitleaks** — Downloads the pinned gitleaks binary for secret scanning
6. **Security scan** — Runs `check-security.mjs` on the PR diff or commit range:
   - Detects localStorage auth token persistence (blocked pattern)
   - Detects common secrets via gitleaks (PII, API keys, etc.)
   - Fails the job if violations found
7. **Cache NuGet packages** — Restores .NET package cache for speed
8. **Cache pnpm store** — Restores frontend package cache for speed
9. **Restore dependencies** — `dotnet restore` to fetch NuGet packages
10. **Run verification pipeline** — Executes `./scripts/verify.sh`:
    - **Format check**: `dotnet format --verify-no-changes` (Roslyn C# style)
    - **Build**: `dotnet build -c Release` (warnings-as-errors enabled)
    - **Unit tests**: `dotnet test tests/TechInventory.UnitTests` (fast, isolated)
    - **Integration tests**: `dotnet test tests/TechInventory.IntegrationTests` (SQLite-based)
    - **Vulnerability scan**: `dotnet list package --vulnerable --include-transitive`
    - **Frontend dependencies**: `pnpm install --frozen-lockfile` in `src/TechInventory.Web`
    - **Frontend type-check**: `pnpm run check` (tsc + svelte-check)
    - **Frontend lint**: `pnpm run lint` (ESLint)
    - **E2E tests**: Runs `./scripts/run-e2e.sh` (brings up compose stack, runs Playwright, tears down)

### Local Equivalent

Developers can run the same checks locally before pushing:

```bash
# Full verification pipeline (as in CI)
task verify
# or manually
./scripts/verify.sh  # Linux/macOS
.\scripts\verify.ps1 # Windows
```

For faster iteration:
```bash
# Just unit + integration tests (no E2E, no Docker)
task test:unit
task test:integration

# With running stack (requires `task up` first)
task test
```

### Debugging CI Failures

1. **Format check failed**: Run `dotnet format` locally to auto-fix, then commit
2. **Build failed**: Run `dotnet build -c Release` locally to see compiler errors
3. **Test failed**: Run the specific test locally with `dotnet test --filter "FullyQualifiedName~..."` to reproduce
4. **Vuln check failed**: Run `dotnet list package --vulnerable` to see which packages need updates
5. **Security scan failed**: Check the error message in the log; `check-security.mjs` will highlight the file and line
6. **Frontend lint failed**: Run `cd src/TechInventory.Web && pnpm run lint -- --fix` to auto-fix
7. **E2E test failed**: Run `task test:e2e` locally to see detailed Playwright output

---

## Pre-Commit Hook (`.githooks/pre-commit`)

**Triggers:** `git commit`  
**Duration:** ~2–5 seconds  
**Status:** Blocks commit if any check fails; can be overridden with `--no-verify` (not recommended)

### Hook Steps

The pre-commit hook runs a **fast subset** of CI checks to catch issues before they reach the PR:

1. **Format check**: `dotnet format --verify-no-changes` (~1s)
   - Ensures C# code follows project style (Roslyn conventions)
   - Fails if formatting violations detected
2. **Lint check**: `pnpm run lint` in `src/TechInventory.Web` (~2s)
   - Runs ESLint on staged TypeScript/Svelte files
   - Fails if lint violations detected
3. **Security scan**: `node scripts/check-security.mjs --staged` (~1s)
   - Detects localStorage auth token persistence
   - Detects secrets (API keys, tokens, etc.)
   - Fails if violations found

### Why Not Full Tests in Pre-Commit?

The pre-commit hook intentionally **skips** build and test steps to keep it fast (~2–5s). Running the full pipeline would take 5–10 minutes, making commit workflow sluggish. The philosophy is:

- **Pre-commit**: Fast gates (format, lint, secrets) — catches 80% of issues
- **CI**: Full pipeline (build, tests, E2E) — catches the remaining 20% and verifies integration

### Override if Needed

If you want to commit without running the hook:
```bash
git commit --no-verify
```

(Not recommended; use only in emergencies and always run the full `task verify` before pushing.)

---

## Caching Strategy

### NuGet Cache
- **Key**: `runner.os-nuget-{hash of all *.csproj files}`
- **Path**: `~/.nuget/packages`
- **Fallback**: Restores from `runner.os-nuget-` prefix if exact key misses
- **Effect**: Saves ~30–60 seconds on dependency restore

### pnpm Cache
- **Key**: `runner.os-pnpm-{hash of pnpm-lock.yaml}`
- **Path**: `~/.local/share/pnpm/store`
- **Fallback**: Restores from `runner.os-pnpm-` prefix if exact key misses
- **Effect**: Saves ~1–2 minutes on frontend `pnpm install`

---

## Security Gates (Constitution §9)

The CI enforces the following security checks:

| Check | Tool | Phase | Status |
|-------|------|-------|--------|
| Code formatting | `dotnet format` | Phase 1 ✅ | Enforced |
| C# warnings-as-errors | Roslyn compiler | Phase 1 ✅ | Enforced |
| .NET vulnerability scan | `dotnet list package --vulnerable` | Phase 1 ✅ | Enforced |
| Auth token persistence scan | `check-security.mjs` (custom) | Phase 1 ✅ | Enforced |
| Secret detection | `gitleaks` (in check-security.mjs) | Phase 1 ✅ | Enforced |
| TypeScript strict mode | `tsc --noEmit` | Phase 1 ✅ | Enforced |
| Frontend lint (ESLint) | `pnpm run lint` | Phase 1 ✅ | Enforced |
| E2E smoke tests | Playwright | Phase 1 ✅ | Enforced |
| Code coverage ≥ 85% | Coverage reports | Phase 1 ✅ | Enforced (locally in verify.sh) |
| SAST (CodeQL) | GitHub CodeQL | Phase 2 (planned) | Not yet enabled |
| Container image scan | `trivy` | Phase 3 (planned) | Not yet enabled |
| SBOM generation | `syft` or `dotnet sbom` | Phase 3 (planned) | Not yet enabled |

---

## OS and Runner Selection

- **Current**: `ubuntu-latest` for CI, supports both Linux and Windows in local Taskfile
- **Rationale**: Ubuntu is cost-effective, has Docker built-in (needed for E2E), and .NET 10 is well-tested
- **Future**: Consider Windows runners if Windows-specific issues emerge
- **Local dev**: Works on Windows, macOS, Linux via platform-aware Taskfile

---

## Manual Branch Protection Setup (GitHub UI)

For the CI gates to actually block PRs, Brian must configure branch protection on `main`:

1. Go to **Settings → Branches**
2. Click **Add rule** (or edit existing `main` rule)
3. Enable:
   - **Require status checks to pass before merging**
   - Select `ci / verify` as required
   - **Require branches to be up to date before merging**
   - **Require code reviews before merging** (at least 1)
   - **Require signed commits**
4. Click **Save changes**

After this, PRs will be blocked if `ci / verify` fails.

---

## Troubleshooting

### "API readiness check failed at http://localhost:8080/health/ready"

**Cause**: E2E test tried to run but the API container didn't start in time.

**Fix**:
- Check if Docker Compose and Docker daemon are running
- Run locally: `task up` to verify stack starts, then `task test:e2e` separately
- In CI logs, scroll up to see API container startup errors

### "gitleaks is not installed"

**Cause**: The `install-gitleaks.sh` or `install-gitleaks.ps1` script didn't run or failed.

**Fix**:
- Local: Run `task hooks:install` to download and wire the hook
- CI: Check that the "Install pinned gitleaks" step succeeded

### "ENOENT: no such file or directory 'src/TechInventory.Web/pnpm-lock.yaml'"

**Cause**: Frontend project files are missing or pnpm lockfile isn't committed.

**Fix**:
- Ensure `src/TechInventory.Web/` directory and `pnpm-lock.yaml` are in the repo
- If they're new, commit them

### Tests pass locally but fail in CI (or vice versa)

**Cause**: Different environment (OS, .NET version, pnpm version, Docker state).

**Fix**:
- Local: Ensure you've run `task clean && task verify` to reset state
- CI: Check the runner OS and tool versions in the log
- Common: Stale Docker images/volumes — try `task down -v && task up` locally

---

## References

- **Verify Pipeline**: `./scripts/verify.sh` (source of truth for CI checks)
- **Local Automation**: `Taskfile.yml` (task runner definitions)
- **Security Policies**: `.specify/memory/constitution.md` (§9 Quality Gate)
- **Pre-Commit Setup**: `task hooks:install` (wires the hook and gitleaks binary)

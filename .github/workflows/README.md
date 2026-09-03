# GitHub Actions Workflows

> **Phase note (added 2026-05-19)**: The "Phase 1 / Phase 2 / Phase 3" labels
> in the security-check table below use the **original** PRD §13 numbering
> (pre-2026-05-19 rewrite). All "Phase 1" gates are enforced today. Items
> tagged "Phase 2 (planned)" or "Phase 3 (planned)" — CodeQL SAST, Trivy
> container scan, SBOM generation — should be considered **P4 — Continuous
> Iteration** candidates under the rewritten phase model and promoted to
> `specs/_backlog/` when work begins.

> **T104 note (specs/004-agentic-development-foundation)**: `Taskfile.yml`
> is now the one authoritative verification interface. Every check below
> is a `task` entrypoint; CI and local scripts (`scripts/verify.sh`,
> `scripts/verify.ps1`) call the same tasks rather than duplicating
> pipeline logic. See `verify:fast` / `verify:contracts` / `verify:full`
> in `Taskfile.yml` for the exact command graph.

> **T105 accuracy note (2026-09-02, `specs/004-agentic-development-foundation`)**:
> "Enforced" in this document means **"runs on every PR and fails the job"**.
> It does **not** mean "blocks merge". As observed on 2026-09-02, `main` has
> **no branch protection and no rulesets**, so every check below reports and
> none of them prevents a merge. The written recommendation (exact check names
> + JSON) is
> `specs/004-agentic-development-foundation/t105-governance-evidence.md` §3–§4;
> until it is applied, treat these gates as `REVIEWED`, not `ENFORCED`
> (`plan.md` §2.5).

This document describes the CI/CD workflows that enforce code quality and security gates on Tech Inventory.

## Quality Gate (`quality-gate.yml`)

**Triggers:** Push to `main`, Pull requests, weekly schedule (Monday 06:00 UTC)
**Status:** The workflow intended to gate merges — and the only one whose jobs
should ever be listed as required status checks. It is not merge-blocking until
branch protection is applied (see the T105 note above).

### `verify` job

Runs the exact same pipeline a developer runs locally:

```bash
task verify   # == verify:fast + verify:contracts + frontend build + vulnerability scan
```

That expands to, in order:

1. **`verify:fast`**
   - `check:format` — `dotnet format --verify-no-changes`
   - `build:backend` — `dotnet build -c Release` (warnings-as-errors)
   - `check:frontend` — `pnpm run check` (`tsc --noEmit` + `svelte-check`, after regenerating the OpenAPI client)
   - `lint` — ESLint on the frontend
   - `test:unit` — `dotnet test tests/TechInventory.UnitTests`, floor-checked (see below)
   - `test:frontend` — Vitest unit/component tests, floor-checked
2. **`verify:contracts`**
   - `check:stale-refs` — `check-stale-playwright-references.mjs` (fails if the retired browser-E2E harness returns to an active manifest, script, workflow, config, doc, or test tree)
   - `check:openapi-drift` — regenerates `openapi.yaml` from the API project (`export-openapi`) and structurally diffs it against the committed copy
   - `check:client-drift` — snapshots the working-tree `src/lib/api/generated/types.ts`, regenerates it from the working-tree `openapi.yaml`, diffs the regeneration against that snapshot, then restores the original file on both a pass and a failure (`scripts/check-client-drift.mjs`; never depends on anything being committed)
   - `check:migration-drift` — `dotnet ef migrations has-pending-model-changes` (fails if the EF Core model and the last migration have diverged)
   - `test:integration` — `dotnet test tests/TechInventory.IntegrationTests` (real SQLite via in-process `WebApplicationFactory<Program>`, no Docker), floor-checked
3. **`build:frontend`** — `pnpm run build` (production Vite build)
4. **`check:vulnerable`** — `dotnet list package --vulnerable --include-transitive --format json`, parsed by `scripts/check-vulnerable.mjs` and failed closed on any Moderate/High/Critical advisory (constitution.md §5.8: "clean (no Moderate+)"); the bare command exits 0 even when it reports vulnerabilities, so this stage never trusted its exit code alone

**No stage requires Docker.** Integration tests exercise the API in-process against a real SQLite database, not a running container.

### Collected-test floors

`scripts/check-test-floors.mjs` wraps the unit, integration, and frontend suites and fails closed if a suite silently collects zero or a suspiciously low number of tests (e.g. a broken filter, a config regression that stops discovery, or a project failing to build). Floors set below reflect **T104's own measured baseline**; they will need raising as the suites grow — that is expected and intentional, not a defect.

| Suite | Measured baseline (T104) | Floor | Command |
|---|---|---|---|
| `unit` | 278 tests | 250 | `task test:unit` |
| `integration` | 292 passed + 4 skipped = 296 collected | 265 | `task test:integration` |
| `frontend` (Vitest) | 649 tests / 83 files | 580 tests / 74 files | `task test:frontend` |

### Other jobs in this workflow

- **`codeql`** — CodeQL SAST for C# and TypeScript, uploaded as an artifact (SARIF upload to Code Scanning requires a paid GHAS tier on private repos; see the in-file comment).
- **`secrets`** — `gitleaks/gitleaks-action` secret scan.
- **`container-config-scan`** — Trivy `config` scan of the Dockerfiles/Compose, PR-only.
- **`sbom`** — CycloneDX SBOM generation on `main`.

### Local Equivalent

```bash
task verify          # full pipeline, same as CI
task verify:fast      # quick loop: format, build, type-check, lint, unit + frontend tests
task verify:contracts # stale-reference guard, drift checks, integration tests
./scripts/verify.sh   # Linux/macOS — thin wrapper, calls `task verify`
.\scripts\verify.ps1  # Windows — thin wrapper, calls `task verify`
```

### Debugging CI Failures

1. **`check:format` failed**: Run `dotnet format` locally to auto-fix, then commit
2. **`build:backend` failed**: Run `dotnet build -c Release` locally to see compiler errors
3. **`test:unit` / `test:integration` / `test:frontend` failed**: Re-run the specific suite locally (`task test:unit`, etc.); a "floor" failure with 0 collected tests means discovery broke, not that tests failed — check the runner output above the floor-check summary
4. **`check:vulnerable` failed**: `scripts/check-vulnerable.mjs` parses `dotnet list package --vulnerable --format json` and fails on any Moderate/High/Critical advisory (constitution.md §5.8). Run `dotnet list package --vulnerable --include-transitive` locally to see which packages need updates; a Low-only finding is printed but does not fail the gate
5. **Auth token / secret scan failed** (`ci.yml`, `secrets` job): Check the error message in the log
6. **`lint` failed**: Run `cd src/TechInventory.Web && pnpm run lint -- --fix` to auto-fix
7. **`check:stale-refs` failed**: A retired browser-E2E harness reference has returned to an active file; check the guard's output for the exact file/line and either remove it or move it under an exempt historical path (`scripts/check-stale-playwright-references.mjs`)
8. **`check:openapi-drift` / `check:client-drift` failed**: The committed `openapi.yaml` or generated TypeScript client is out of date — run `task openapi:export` / `pnpm run generate:client` and commit the result. `check:client-drift` compares a fresh regeneration against the working-tree file it snapshotted at the start of the run (not the index/HEAD copy), so it fails only on genuine drift and always restores the file it started with
9. **`check:migration-drift` failed**: The EF Core model changed without a migration — run `task db:migrate` (`dotnet ef migrations add ...`) and commit the migration

---

## CI Pipeline (`ci.yml`)

**Triggers:** `workflow_dispatch` only (not required for merge)
**Status:** Manual-only alternate entrypoint into the same `task verify` pipeline, plus an auth-token/secret diff scan (`check-security.mjs`). Quality Gate is the workflow that actually blocks merges; see the in-file header comment for the history of why this workflow is muted and what would be involved in re-enabling it (a decision left to a future task).

Both workflows call the identical `task verify` entrypoint (via `scripts/verify.sh`), so both install PyYAML identically before that step — `check:openapi-drift`'s comparison script needs it to parse the freshly-exported YAML document (T104 revision · B-3: `ci.yml` previously omitted this step while `quality-gate.yml` had it, even though both drive the same PyYAML-dependent check).

---

## Pre-Commit Hook (`.githooks/pre-commit`)

**Triggers:** `git commit`
**Duration:** ~2–3 seconds
**Status:** Blocks commit if any check fails; can be overridden with `--no-verify` (not recommended)

### Hook Steps

The pre-commit hook runs a **fast subset** of CI checks to catch issues before they reach the PR:

1. **Lint check**: `pnpm run lint` in `src/TechInventory.Web` (~1–2s)
   - Runs ESLint on staged TypeScript/Svelte files
   - Fails if lint violations detected
2. **Security scan**: `node scripts/check-security.mjs --staged` (~1s)
   - Detects localStorage auth token persistence
   - Detects secrets (API keys, tokens, etc.)
   - Fails if violations found

*Note: Format check (`dotnet format`) is **not** in pre-commit because it's already enforced by Quality Gate and would slow the hook (dotnet format can take 5+ seconds on full repo). Developers should run `task verify:fast` locally before pushing, and `task verify` before opening a PR.*

### Why Not Full Tests in Pre-Commit?

The pre-commit hook intentionally **skips** build, test, and format steps to keep it fast (~2–3s). Running the full pipeline would take several minutes, making commit workflow sluggish. The philosophy is:

- **Pre-commit**: Fast gates (lint, secrets) — catches most issues
- **Quality Gate**: Full `task verify` pipeline — catches the rest and verifies integration

### Override if Needed

If you want to commit without running the hook:
```bash
git commit --no-verify
```

(Not recommended; use only in emergencies and always run `task verify` before pushing.)

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

| Check | Tool | Phase | Status |
|-------|------|-------|--------|
| Code formatting | `dotnet format` | Phase 1 ✅ | Enforced (Quality Gate → `task verify`) |
| C# warnings-as-errors | Roslyn compiler | Phase 1 ✅ | Enforced |
| .NET vulnerability scan | `dotnet list package --vulnerable --format json`, parsed by `check-vulnerable.mjs` | Phase 1 ✅ | Enforced — fails closed on any Moderate+ advisory (constitution.md §5.8); the bare command's exit code is not trusted (it is 0 even when advisories are reported) |
| Auth token persistence scan | `check-security.mjs` (custom) | Phase 1 ✅ | Enforced (`ci.yml`, manual) |
| Secret detection | `gitleaks` (`secrets` job + `check-security.mjs`) | Phase 1 ✅ | Enforced |
| TypeScript strict mode | `tsc --noEmit` | Phase 1 ✅ | Enforced |
| Frontend lint (ESLint) | `pnpm run lint` | Phase 1 ✅ | Enforced |
| Collected-test floors (unit/integration/frontend) | `check-test-floors.mjs` | Phase 1 ✅ | Enforced (fails closed on zero/partial collection) |
| OpenAPI / generated-client drift | `check:openapi-drift`, `check:client-drift` | Phase 1 ✅ | Enforced |
| EF Core migration drift | `check:migration-drift` (`dotnet ef migrations has-pending-model-changes`) | Phase 1 ✅ | Enforced |
| Stale-reference guard (retired browser-E2E harness) | `check-stale-playwright-references.mjs` | Phase 1 ✅ | Enforced |
| Code coverage floor (percentage) | — | Phase 1 (planned) | **Not enforced as a numeric gate.** `--collect:"XPlat Code Coverage"` runs during `test:unit`/`test:integration` and produces Cobertura reports, but nothing currently asserts a percentage threshold; only *test-count* floors are enforced (see above). Distinct from and not yet substituting the constitution's 85% coverage target. |
| SAST (CodeQL) | GitHub CodeQL | Phase 2 (planned) | Enabled (`codeql` job, SARIF upload pending GHAS) |
| Container image scan | `trivy` | Phase 3 (planned) | Enabled (`container-config-scan` job, config-only on PR) |
| SBOM generation | `anchore/sbom-action` | Phase 3 (planned) | Enabled (`sbom` job, `main` only) |

---

## OS and Runner Selection

- **Current**: `ubuntu-latest` for CI, supports both Linux and Windows in local Taskfile
- **Rationale**: Ubuntu is cost-effective and .NET 10 is well-tested
- **Future**: Consider Windows runners if Windows-specific issues emerge
- **Local dev**: Works on Windows, macOS, Linux via platform-aware Taskfile

---

## Manual Branch Protection Setup (GitHub UI or API)

> **Recommendation only — not applied.** The full written recommendation, with
> the exact check names, the API payload, and the honest list of what is
> declined or unobserved, lives in
> `specs/004-agentic-development-foundation/t105-governance-evidence.md` §3–§4
> (T105 / AC-009). Applying repository settings is out of scope for that work
> package; `briandenicola` owns the decision.

For the CI gates to actually block PRs, `briandenicola` must configure branch
protection on `main`. Summary of the recommendation:

1. **Settings → Branches → Add rule**, pattern `main`
2. Enable **Require status checks to pass before merging** + **Require branches
   to be up to date before merging** (`strict: true`)
3. Required checks — only jobs that actually run on pull requests:
   `verify`, `codeql`, `secrets`, `container-config-scan`
   - **Not** `sbom` (runs on `main` pushes only; it is not a PR check)
   - **Not** `Build, Test, and Verify` from `ci.yml` (`workflow_dispatch` only)
   - The context strings are the **job names** as they appear in
     `gh pr checks <PR>` — confirm them against the first Quality Gate run on a
     real PR before saving, rather than typing them from memory
4. Disable **force pushes** and **branch deletion**; leave
   **Do not allow bypassing** (`enforce_admins`) **off** — the observed
   single-operator precedent in `briandenicola/Aurearia`
5. Code-owner review is **not** recommended: GitHub cannot request a review from
   the author, and this repository has one maintainer (`.github/CODEOWNERS`)

After this, PRs are blocked if the required Quality Gate jobs fail. Until then,
they are not.

---

## Troubleshooting

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

**Cause**: Different environment (OS, .NET version, pnpm version).

**Fix**:
- Local: Ensure you've run `task clean` (if present) then `task verify` to reset state
- CI: Check the runner OS and tool versions in the log
- No stage in `task verify` requires Docker or a running stack, so "works on my compose stack" drift should not apply here — if it does, that's a bug in the check, not an expected environment difference

---

## References

- **Verify Pipeline**: `Taskfile.yml` (`verify:fast` / `verify:contracts` / `verify:full` / `verify` — source of truth for both local and CI checks)
- **Thin wrappers**: `scripts/verify.sh`, `scripts/verify.ps1` (call `task verify`; no pipeline logic lives here)
- **Collected-test floors**: `scripts/check-test-floors.mjs`
- **Stale-reference guard**: `scripts/check-stale-playwright-references.mjs`
- **Security Policies**: `.specify/memory/constitution.md` (§9 Quality Gate)
- **Pre-Commit Setup**: `task hooks:install` (wires the hook and gitleaks binary)

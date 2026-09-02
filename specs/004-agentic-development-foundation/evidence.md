---
id: 004-agentic-development-foundation
document: evidence
status: T001–T004 DONE; T101–T105 APPROVED / NOT STARTED
collected_by: Ripley (Lead / Architect)
collected_at: 2026-09-02T10:07-05:00
---

# Evidence Report — Agentic Development Foundation

All findings below were collected fresh from repository and GitHub evidence on
2026-09-02. No artifact produced by any earlier agent was read or reused.

Citation format: `owner/repo:path@sha`. Where a fact could not be observed, it
is marked **`unknown`** with the reason. Nothing here is inferred.

**This document is history, not target strategy** (`plan.md` §2.9): its
Playwright facts record `d303cd6`; the target state is retirement (`brief.md`
§2.1).

---

## 1. Pinned SHAs

| Repository | Default branch | Pinned SHA | Primary language |
| --- | --- | --- | --- |
| `briandenicola/tech-inventory` | `main` | `d303cd6537392e2489222d5a0d5c946f39f2af0c` | C# |
| `briandenicola/watch-tracker-app` | `main` | `b40b7397fc101ee1eb3c1734110e2f17070efb9f` | C# |
| `briandenicola/Aurearia` | `main` | `50a71fdfb2f6fd2f143122f19787ee7619ced618` | Go |
| `briandenicola/drinks-and-desserts` | `main` | `0fccf9b783a01a82dfb29d076259cab2144d0795` | C# |

Short forms used throughout: `@d303cd6`, `@b40b739`, `@50a71fd`, `@0fccf9b`.
Note: `d303cd6` is simultaneously the current `main` HEAD of tech-inventory and
the merge commit of PR #140 — the incident in §5 is therefore at the tip.

---

## 2. T001 — Four-Repository Evidence Matrix

### 2.1 Required PR checks / branch protection

| | tech-inventory `@d303cd6` | watch-tracker-app `@b40b739` | Aurearia `@50a71fd` | drinks-and-desserts `@0fccf9b` |
| --- | --- | --- | --- | --- |
| `branches/main/protection` | `404 Branch not protected` | `404 Branch not protected` | **HTTP 200 — protected** | `404 Branch not protected` |
| `rulesets` | `[]` | `[]` | `[]` | `[]` |
| Required status checks | **none** | **none** | **7**: `Go API`, `Vue Web`, `Python Agent`, `Gitleaks`, `Govulncheck`, `npm audit`, `pip-audit` | **none** |
| `strict` (up-to-date required) | n/a | n/a | `true` | n/a |
| `enforce_admins` | n/a | n/a | `false` — admins may bypass | n/a |
| Force push / deletion | unrestricted | unrestricted | both disabled | unrestricted |
| Workflow count | 9 (`briandenicola/tech-inventory:.github/workflows/@d303cd6`) | 3 (`briandenicola/watch-tracker-app:.github/workflows/@b40b739`) | 8 (`briandenicola/Aurearia:.github/workflows/@50a71fd`) | 9 (`briandenicola/drinks-and-desserts:.github/workflows/@0fccf9b`) |

**Aurearia is the only one of the four with any enforced merge gate.** In the
other three, including tech-inventory, workflows run and report — but nothing
prevents a merge over a red or absent result.

> **`unknown`:** Historical branch-protection state. The GitHub REST API returns
> only the *current* configuration; whether tech-inventory `main` was protected
> at `2026-09-02T14:27:09Z` (the PR #140 merge instant) cannot be determined
> from it, and no audit-log access was attempted. All protection statements
> here describe the state **as observed on 2026-09-02**.

### 2.2 Local / CI verification parity

| | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| Task runner | `Taskfile.yml` (305 lines, 27 tasks) | `Taskfile.yml` | `Taskfile.yml` | `Taskfile.yml` + 4 includes |
| CI calls the task runner? | **No** — Quality Gate re-declares inline steps | **No** — inline | **No** — inline, but commands match | **No** — inline |
| Shared script layer | `scripts/verify.sh` (53 lines) called by `ci.yml` only | none | none | none |
| Observed divergence | Quality Gate runs `dotnet test -c Release --no-build --collect:"XPlat Code Coverage"` across the solution; `scripts/verify.sh` runs the two test projects separately and then `./scripts/run-e2e.sh`. **Only `verify.sh` runs Playwright.** | CI adds `--no-restore`/`--no-incremental` absent from Taskfile; CI NuGet audit has no Taskfile equivalent | `task openapi` and CI "Verify OpenAPI snapshot" run the identical `swag init` invocation; `task test-critical-workflows` → `npm run test:browser` is **not** called by any workflow | CI builds `src/api` only; Taskfile builds the full `src/WhiskeyAndSmokes.sln`; `copilot-instructions.md` names `npm test`/`npm run lint`, neither of which CI runs |

Citations: `briandenicola/tech-inventory:Taskfile.yml@d303cd6`,
`briandenicola/tech-inventory:scripts/verify.sh@d303cd6`,
`briandenicola/tech-inventory:.github/workflows/quality-gate.yml@d303cd6`,
`briandenicola/watch-tracker-app:Taskfile.yml@b40b739`,
`briandenicola/Aurearia:Taskfile.yml@50a71fd`,
`briandenicola/drinks-and-desserts:Taskfile.yml@0fccf9b`.

**No repository in the set has true parity.** All four re-declare commands in
CI. tech-inventory is the only one that funnels CI through a shared script — and
it funnels the *disabled* workflow through it.

### 2.3 Contract generation and drift detection

| | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| Committed API contract | `openapi.yaml` at repo root | **none found** | `docs/openapi.json`, `src/api/docs/swagger.{json,yaml}`, plus 9 hand-written `specs/*/contracts/*.openapi.yaml` | **none found** |
| How produced | `dotnet run -- export-openapi` from the API project | n/a | `swag init` from swaggo annotations | n/a |
| Generated typed client | `src/lib/api/generated/types.ts` via `pnpm run generate:client` | none | none (`swagger_types.go` hand-maintained) | none |
| **Spec-vs-code drift gate** | **Yes** — `scripts/check-openapi-drift.sh` + `scripts/compare-openapi.py`, run in Quality Gate job `dotnet` | none | **Yes** — `swag init` then `git diff --exit-code` in required check `Go API` | none |
| **Client-vs-spec drift gate** | **Yes** — regenerate then `git diff --exit-code -- src/lib/api/generated/types.ts`, Quality Gate job `web` | none | none | none |

Citations: `briandenicola/tech-inventory:scripts/check-openapi-drift.sh@d303cd6`,
`briandenicola/tech-inventory:.github/workflows/quality-gate.yml@d303cd6`,
`briandenicola/Aurearia:.github/workflows/ci.yml@50a71fd`.

Notably, `scripts/check-openapi-drift.sh` states its own provenance:

```bash
# Modelled on the "Verify OpenAPI snapshot" gate in
# briandenicola/aurearia, adapted for the format difference described in
# scripts/compare-openapi.py.
```
— `briandenicola/tech-inventory:scripts/check-openapi-drift.sh@d303cd6`

**Tech Inventory has the strongest contract tooling of the four** (two drift
gates versus Aurearia's one) — but Aurearia's single gate is a *required* check
and Tech Inventory's two are not, because `main` is unprotected (§2.1).
Capability is not the gap. Enforcement is.

### 2.4 Test boundaries and fidelity

| | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| Backend test projects | 2 (`tests/TechInventory.UnitTests`, `tests/TechInventory.IntegrationTests`) | 1 (`src/api.tests`) | 203 `_test.go` files + 40 Python test files | 1 (`tests/WhiskeyAndSmokes.Tests`) |
| Integration DB fidelity | **Real SQLite file per test class**, then `dbContext.Database.Migrate()` | in-memory SQLite via `Data Source=:memory:`, `MigrateAsync()` | in-memory SQLite via GORM `sqlite.Open(":memory:")` | **no DB** — `WebApplicationFactory<Program>` with every external dependency replaced by `Substitute.For<…>` |
| Browser E2E present | **Yes** — 17 spec files, 6 projects | **No** | Yes — 5 Playwright specs | **No** |
| Browser E2E runs in a merge-blocking check | **No** | n/a | **No** (`task test-critical-workflows` is local-only) | n/a |
| Skipped / stub tests | **19 `test.todo` + 4 `test.describe.skip`** in E2E; 5 `Skip = "…"` in .NET; 1 `it.skip` in Vitest | **0** | 8 `t.Skip` (3 conditional, 1 opt-in gate, 1 documented timing) | **0** |
| Coverage threshold enforced | `--collect:"XPlat Code Coverage"` collected; **no threshold gate found** | none | none | none |

Citations: `briandenicola/tech-inventory:tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs@d303cd6` (line 149, `dbContext.Database.Migrate()`), `briandenicola/drinks-and-desserts:tests/WhiskeyAndSmokes.Tests/CustomWebApplicationFactory.cs@0fccf9b`, `briandenicola/Aurearia:src/api/database/migration_test.go@50a71fd`, `briandenicola/watch-tracker-app:src/api.tests/TestDatabase.cs@b40b739`.

Two observations that cut against expectation:

1. **Tech Inventory has the highest-fidelity integration boundary of the four.**
   Per-class real SQLite files driven through the actual migration pipeline is
   strictly more faithful than in-memory SQLite (watch-tracker, Aurearia) and
   vastly more faithful than fully-mocked dependencies (drinks-and-desserts).
2. **Tech Inventory is the only repository in the set with stubbed tests, and
   it is the only one with an execution incident.** The two repos with zero
   skips are the two with no browser E2E at all.

### 2.5 Governance and instruction volume

| Repository | Agent-facing instruction files | Lines | Words |
| --- | --- | --- | --- |
| **tech-inventory** | `.squad/**` 216 files | **25,748** | **168,968** |
| | `.copilot/skills/**` 32 files | 3,705 | 20,614 |
| | `.specify/memory/constitution.md` | 566 | 3,425 |
| | `.github/copilot-instructions.md` | 136 | 830 |
| | `.github/prompts/**` 4 files | 29 | 164 |
| | **Total** | **30,184** | **194,001** |
| **Aurearia** | `.squad/**` + `copilot-instructions.md` + `CONTRIBUTING.md` | 24,849 | 168,070 |
| **drinks-and-desserts** | `.squad/**` + `.specify/**` + `copilot-instructions.md`, 81 files | 8,426 | 49,914 |
| **watch-tracker-app** | `.github/copilot-instructions.md` only | **80** | **594** |

Citations: `briandenicola/tech-inventory:.squad/@d303cd6`,
`briandenicola/Aurearia:.squad/decisions.md@50a71fd` (11,286 lines alone),
`briandenicola/drinks-and-desserts:.squad/@0fccf9b`,
`briandenicola/watch-tracker-app:.github/copilot-instructions.md@b40b739`.

Governance volume spans a **377× range** across four repositories by the same
author. There is no observable correlation between instruction volume and
enforcement posture: watch-tracker-app (80 lines) and Aurearia (24,849 lines)
both have zero skipped tests; Aurearia's enforcement comes from its
`branches/main/protection` configuration, not from its 11,286-line decision log.

> **`unknown`:** Aurearia's `.squad/log/` and `.squad/orchestration-log/`
> (33+ files) could not be checked out on Windows — filenames contain colons
> from ISO timestamps. Aurearia's true total is therefore **higher** than the
> 24,849 lines shown. This does not change the ordering.

A concrete instruction-accuracy defect was found in the smallest corpus:
`briandenicola/watch-tracker-app:.github/copilot-instructions.md@b40b739`
states *"There are no tests in this project"* while the repository contains
138 `[Fact]`/`[Theory]` methods and 15 Vitest files, and describes the frontend
as React 19 when it is Vue 3. **Small instruction sets go stale too.** Volume
reduction alone does not produce accuracy; freshness ownership does.

### 2.6 Feature / task structure

| | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| `specs/` feature folders | 3 numbered (`001-core-api`, `002-frontend-mvp`, `003-pwa-polish`) + 24 `_backlog/` files | **none** | **31** numbered + `_backlog/` + `main/` | 3 numbered + `_backlog/` mirror |
| `spec.md` / `plan.md` / `tasks.md` triad | 001 and 002 complete; **003 has `spec.md` + `tasks.md` but no `plan.md`** | n/a | present per feature | complete for all 3 |
| Task IDs | `T0NN` style | n/a | checkbox phases, no global IDs | checkboxes (51 / 57 / 40) |
| ADRs | **1** (`docs/adr/0001-record-architecture-decisions.md`) | **0** (no ADR directory) | **15** (`docs/adr/0001`–`0015`) | **0** formal; one `.squad/decisions.md` |

Citations: `briandenicola/tech-inventory:specs/@d303cd6`,
`briandenicola/Aurearia:docs/adr/@50a71fd`,
`briandenicola/drinks-and-desserts:specs/@0fccf9b`.

Aurearia has 15 ADRs against 31 features. Tech Inventory has 1 ADR against 3
features plus 24 backlog specs — and notably, the F045 work in PR #140 was
driven from `specs/_backlog/F045-pwa-shell-and-device-list.md`, i.e. a *backlog*
item, not a promoted feature folder with a plan.

### 2.7 Code ownership surface

| Repository | Source lines (first-party, excl. `node_modules`/`bin`/`obj`/`dist`/generated) | Modules |
| --- | --- | --- |
| tech-inventory | C# 25,755 (350 files); TS 13,628 (141); Svelte 16,174 (76) — **55,557** | 6 `.csproj` + 1 SvelteKit app |
| watch-tracker-app | C# 19,883 (248); TS+Vue 12,071 (77) — **31,954** | 2 `.csproj` + 1 Vue app |
| Aurearia | Go 158,673 (495); Vue 38,452 (197); TS 41,828 (271); Python 16,550 (101) — **255,503** | 1 Go module (12 pkgs) + Vue app + Python agent |
| drinks-and-desserts | C# 15,162 (95); Vue 8,776 (48); TS 2,227 (30) — **26,165** | 5 `.csproj` + 1 Vue app |

Command used for tech-inventory (others in the per-repo audits):

```powershell
$ex='\\node_modules\\|\\bin\\|\\obj\\|\\\.git\\|\\\.svelte-kit\\|\\dist\\|\\generated\\'
Get-ChildItem src,tests -Recurse -File -Filter *.cs |
  Where-Object {$_.FullName -notmatch $ex} | Get-Content | Measure-Object -Line
```

`.github/CODEOWNERS` exists **only** in tech-inventory among the four, and is an
unedited template — see §3.6.

### 2.8 Migration validation

| | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| Mechanism | EF Core, 11 files in `src/TechInventory.Infrastructure/Persistence/Migrations` | EF Core, 20 migrations in `src/api/Migrations` | GORM `AutoMigrate` | **none** (CosmosDB + LiteDB, schemaless) |
| Migrations exercised by tests | **Yes** — every integration test class runs `Database.Migrate()` against a real SQLite file | Yes — `MigrateAsync()` against in-memory SQLite | Yes — dedicated `src/api/database/*migration*_test.go` including two feature-specific migration-order regression tests | n/a |
| Model-vs-migration **drift** gate in CI | **No** — no `dotnet ef migrations has-pending-model-changes` anywhere in `.github/workflows/`, `scripts/`, or `Taskfile.yml` | **No** | No named drift step; migration tests run inside the required `Go API` check | n/a |

Citations: `briandenicola/tech-inventory:src/TechInventory.Infrastructure/Persistence/Migrations/@d303cd6`, `briandenicola/tech-inventory:tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs@d303cd6`, `briandenicola/Aurearia:src/api/database/migration_test.go@50a71fd`.

Aurearia's `feature353_migration_order_regression_test.go` and
`feature356_migration_order_regression_test.go` are the strongest observed
pattern: each production migration incident became a named, permanent test.

### 2.9 Known-incident safeguards

| Safeguard | tech-inventory | watch-tracker-app | Aurearia | drinks-and-desserts |
| --- | --- | --- | --- | --- |
| Asserts a minimum test count / fails on zero collected tests | **No** | **No** | **No** | **No** |
| Build/compile step precedes test step, so collection errors surface | Partially — `dotnet build -c Release` precedes `dotnet test --no-build`; **Playwright has no compile stage** | Yes for .NET | **Yes** — explicit `go build ./...` before `go test -v ./...`; `npm run type-check` before `npm run test`; pytest exits non-zero on collection error | Yes for .NET |
| `continue-on-error` / `\|\| true` / masking `if: always()` in CI | none found in `.github/workflows/` | none in workflows (`\|\| true` only in local `Taskfile.yml` docker task) | none in workflows (`\|\| true` only in local `Taskfile.yml` docker task) | **2** — `continue-on-error: true` + `if: always()` on SARIF upload steps in `security-scan.yml` lines 74–75, 113–114 |
| A merge-blocking check would catch a test-runner **collection** failure | **No** — the only workflow running Playwright is `workflow_dispatch`-only | n/a (no required checks) | **Yes** — `go build ./...` in the required `Go API` check | **No** — no required checks |

This row is the crux. **Not one of the four repositories can detect a test
suite that silently collects zero tests.** Aurearia escapes the class of failure
only because Go's compiler is in the required path — a language property, not a
designed control. Tech Inventory's Playwright suite had no equivalent
compile-time stage, and the workflow that would have executed it is disabled.

---

## 3. T002 — Tech Inventory Authority Source Inventory

All at `briandenicola/tech-inventory:…@d303cd6`.

### 3.1 Volume summary

| Area | Files | Lines | Words | Share of instruction corpus |
| --- | --- | --- | --- | --- |
| `.squad/**` | 216 | 25,748 | 168,968 | 85.3% of lines |
| `.copilot/skills/**` | 32 | 3,705 | 20,614 | 12.3% |
| `.specify/memory/constitution.md` | 1 | 566 | 3,425 | 1.9% |
| `.github/copilot-instructions.md` | 1 | 136 | 830 | 0.5% |
| `.github/prompts/**` | 4 | 29 | 164 | 0.1% |
| **Instruction corpus total** | **254** | **30,184** | **194,001** | 100% |
| `specs/**` (product intent) | 32 | 5,063 | 41,961 | — |
| `docs/**` (product intent) | 12 | 3,360 | 20,281 | — |
| `.github/workflows/**` (executable) | 9 | 1,499 | 6,192 | — |
| `scripts/**` (executable) | 14 | 801 | 2,515 | — |

**The constitution — the document with the highest declared authority — is
1.9% of the instruction corpus it is supposed to govern.**
`.squad/decisions.md` alone (4,679 lines) is **8.3×** the constitution.

### 3.2 `.squad/**` breakdown

| Subtree | Files | Lines | Words |
| --- | --- | --- | --- |
| `.squad/templates/` | 74 | 8,976 | 47,375 |
| `.squad/decisions.md` | 1 | 4,679 | 31,791 |
| `.squad/agents/` | 18 | 3,849 | 39,481 |
| `.squad/decisions/` | 23 | 2,676 | 17,665 |
| `.squad/orchestration-log/` | 48 | 2,173 | 11,512 |
| `.squad/skills/` | 18 | 1,342 | 7,305 |
| `.squad/log/` | 22 | 1,210 | 8,037 |
| `.squad/session-log.md` | 1 | 415 | 2,713 |
| `.squad/sessions/` | 1 | 148 | 1,557 |
| `.squad/casting/` | 3 | 113 | 253 |
| `.squad/identity/` | 2 | 45 | 249 |
| `.squad/routing.md` | 1 | 43 | 546 |
| `.squad/ceremonies.md` | 1 | 41 | 166 |
| `.squad/team.md` | 1 | 34 | 313 |

**Historical record accounts for 11,301 lines** (`decisions.md` 4,679 +
`decisions/` 2,676 + `orchestration-log/` 2,173 + `log/` 1,210 +
`session-log.md` 415 + `sessions/` 148) — **43.9% of `.squad/**` and 37.4% of
the entire instruction corpus**. This is the
single largest concentration, and it is *history*, not instruction.

### 3.3 Authority source table

Enforcement class: **E** = machine-blocking, **R** = a human must sign,
**A** = advisory only.

| Source | Purpose | Loaded by | Conflict / duplication | Current? | Class | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| `.specify/memory/constitution.md` (566 L) | Top authority per §0 hierarchy | Every agent, per `copilot-instructions.md` "Always read before any task" | Its §7.5 test expectations are not reflected in what Quality Gate actually runs | Current | **A** | **Retain.** Add per-clause E/R/A labels — deferred governance package. |
| `.github/copilot-instructions.md` (136 L) | Repo conventions, session protocol | GitHub Copilot automatically | Restates constitution §2 architecture rules and `docs/testing.md` policy | Current | **A** | **Retain, shorten.** Point to authority rather than restating it. |
| `.copilot/skills/**` (32 files, 3,705 L) | Behavioural skills incl. `ci-validation-gates`, `test-discipline`, `reviewer-protocol` | Copilot CLI skill loader | `test-discipline` overlaps `docs/testing.md` and constitution §7; `ci-validation-gates` overlaps `scripts/verify.sh` | Current | **A** | **Retain; audit for drift** against actual CI — deferred governance package. |
| `.squad/agents/` (18 files, 3,849 L) | Per-agent role charters | Squad orchestrator | Role duties overlap `.copilot/skills/` | Current | **A** | **Retain, shorten.** |
| `.squad/templates/` (74 files, 8,976 L) | Scaffolding templates | Squad tooling on demand | Largest single subtree; unclear how much is reachable | **Partly unknown** | **A** | **Audit; archive unreferenced** — deferred governance package. |
| `.squad/decisions.md` (4,679 L) + `.squad/decisions/` (23 files) | Squad decision log | Read on demand | **Parallel decision system to `docs/adr/`** (1 ADR). Two ledgers, unclear precedence | Current but append-only | **A** | **Archive the bulk; reconcile with `docs/adr/`** — deferred governance package. Candidate ADR topic. |
| `.squad/orchestration-log/`, `.squad/log/`, `.squad/session-log.md`, `.squad/sessions/` (5,146 L) | Historical run records | Read on demand | Pure history presented alongside instruction | Historical | **A** | **Archive.** Explicitly mark as non-instruction (principle 9). |
| `.squad/routing.md`, `team.md`, `ceremonies.md`, `casting/`, `identity/` (276 L) | Team routing/config | Squad orchestrator | Small, coherent | Current | **A** | **Retain.** |
| `specs/001-core-api/` (spec+plan+tasks) | Feature authority | Agents on the feature | — | Complete | **A** | **Retain.** |
| `specs/002-frontend-mvp/` (spec+plan+tasks) | Feature authority | Agents on the feature | — | Complete | **A** | **Retain.** |
| `specs/003-pwa-polish/` | Feature authority | Agents on the feature | **Missing `plan.md`** — breaks the §0 `spec → plan → tasks` chain | **Incomplete** | **A** | **Fix or archive.** |
| `specs/_backlog/` (24 files incl. `F045-pwa-shell-and-device-list.md`) | Unpromoted feature ideas | Ad hoc | **F045 was implemented and merged directly from `_backlog/` with no promoted plan** — see §5 | Mixed | **A** | **Retain; scope promotion by tier** — full spec folder at T2/T3 only (`plan.md` §2.4). |
| `docs/prd.md` (500 L) | Product intent, hierarchy rank 2 | Agents | §7.5.4 journeys were the source for E2E journeys 1–15 | Current | **A** | **Retain;** §7.5.3/§7.5.4 need an ADR amendment under retirement (`plan.md` §6). |
| `docs/testing.md` | Test policy | Agents | States policy that no gate enforces | **Stale vs. reality** | **A** | **Strip every Playwright/E2E promise** (T101); reconcile with the T103 migration matrix. |
| `docs/adr/0001-record-architecture-decisions.md` | ADR practice | Agents | Only 1 ADR exists; `.squad/decisions.md` became the de facto ledger | Current but unused | **A** | **Retain; make it the single ledger** — deferred governance package. |
| `docs/known-issues.md` | Known defects | Agents | Relationship to issue #89 not verified here | **unknown** | **A** | **Audit.** |
| `docs/references.md` (108 L) | Pinned external references (`R<N>`) | Agents, per session protocol | — | Current | **A** | **Retain.** |
| `.copilot-state.md` (53 L) | Session handoff pointer | Agents at session start | Overlaps `SESSION-NOTES.md` | Current | **A** | **Retain.** |
| `SESSION-NOTES.md` (425 L) | Append-only session history | Agents at session start | Overlaps `.squad/session-log.md` (415 L) — **two session logs** | Current | **A** | **Consolidate** — deferred governance package. |
| `.github/pull_request_template.md` (24 L) | Definition of Done | Humans opening PRs | DoD line "All CI checks green" is ambiguous — Quality Gate is not all CI | Current | **A** | **Strengthen to require acceptance evidence** (T105). |
| `.github/CODEOWNERS` (8 L) | Review routing | GitHub | **Placeholder `@your-github-handle`** — see §3.6 | **Stale / non-functional** | **A** | **Fix or remove** (T105). |
| `.github/workflows/quality-gate.yml` (128 L) | The de facto merge gate | GitHub Actions | Runs no E2E | Current | **A** (would be **E** with branch protection) | **Retain; add a collected-test floor** (T104) **and the stale-Playwright-reference guard** (T101). |
| `.github/workflows/ci.yml` (76 L) | Full verify incl. Playwright | Manual dispatch only | **Self-documented as muted** | Current but disabled | **A** | **Delete the Playwright stage** (T101); fold what remains into the single verification entrypoint (T104). **Not re-enabled as a browser suite in any form.** |
| `.github/T47-CI-SETUP-CHECKLIST.md` (284 L) | One-time CI setup checklist | Humans | Describes intended branch protection that §2.1 shows is not applied | **Stale — unexecuted** | **A** | **Execute or archive** (T105). |
| `scripts/verify.sh` (53 L) | Authoritative local verify | `ci.yml`, humans | Requires Docker at step 9/9; PR #140 author documented Docker absent | Current | **A** | **Retain; make CI call it** (T104). |
| `scripts/check-openapi-drift.sh` + `compare-openapi.py` | Contract drift gate | Quality Gate `dotnet` job | — | Current | **A** (would be **E**) | **Retain.** Best-in-class. |
| `scripts/run-e2e.sh` (44 L) | Compose-backed Playwright | `verify.sh`, `Taskfile.yml` | Windows/Linux variants duplicated in Taskfile | Current | **A** | **Delete** (T101); its readiness-poll logic collapses into T104. |
| `Taskfile.yml` (305 L, 27 tasks) | Local automation surface | Humans | **Not invoked by any workflow** | Current | **A** | **Make CI call it** (T104). |

### 3.4 The critical observation

**Every single row in §3.3 is class `A` (advisory).** Nothing in the Tech
Inventory authority corpus is `ENFORCED`, because `main` has no branch
protection (§2.1) — so even the workflows that fail correctly cannot block a
merge. And nothing is `REVIEWED`: CODEOWNERS is a placeholder and PR #140
merged with zero reviews (§5.2). 30,184 lines of instruction, one 566-line
constitution, nine workflows and fourteen scripts — and the enforcement class of
the entire system is *advisory*.

### 3.5 Duplication pairs found

| A | B | Overlap |
| --- | --- | --- |
| `SESSION-NOTES.md` (425 L) | `.squad/session-log.md` (415 L) | Two parallel session histories |
| `docs/adr/` (1 ADR) | `.squad/decisions.md` + `.squad/decisions/` (7,355 L) | Two decision ledgers, no stated precedence |
| `.copilot/skills/test-discipline` | `docs/testing.md` + constitution §7 | Three statements of test policy |
| `.copilot/skills/ci-validation-gates` | `scripts/verify.sh` + `quality-gate.yml` | Described gates vs. actual gates |
| `Taskfile.yml` e2e tasks | `scripts/run-e2e.sh` / `run-e2e.ps1` | Readiness-poll logic written twice per platform |
| `.github/copilot-instructions.md` §Architecture | constitution §2 | Restated architecture rules |

### 3.6 `.github/CODEOWNERS` — verbatim

```
# Default owners
*       @your-github-handle

# Sensitive areas
.specify/                   @your-github-handle
.github/                    @your-github-handle
deploy/                     @your-github-handle
src/TechInventory.Api/      @your-github-handle
```
— `briandenicola/tech-inventory:.github/CODEOWNERS@d303cd6`

`@your-github-handle` is not a GitHub account; the file has never been
personalised. No review could ever have been auto-requested, for any path,
including `.specify/` and `.github/`. It also references `deploy/`, a directory
that does not exist (top-level deployment lives in `ops/`).

---

## 4. Cross-Repository Synthesis

### 4.1 What the evidence actually supports

1. **Enforcement is a configuration property, not a documentation property.**
   Aurearia's advantage is one API object — `branches/main/protection` with 7
   required checks. Its 24,849 lines of `.squad/` instruction are not what stops
   a bad merge.
2. **Contract gates are the highest-leverage automated control observed.**
   Aurearia's `swag init` + `git diff --exit-code` and Tech Inventory's
   `check-openapi-drift.sh` + generated-client diff catch a whole class of
   integration failure at near-zero runtime cost, deterministically.
3. **Realistic boundaries beat broad boundaries.** Tech Inventory's real-SQLite
   + `Database.Migrate()` integration tests (§2.4) exercise the migration
   pipeline that a mocked factory (drinks-and-desserts) cannot touch at all.
4. **Small, owned surfaces fail less.** watch-tracker-app: 31,954 source lines,
   80 lines of instruction, 3 workflows, zero skipped tests, no incident.
5. **Fail-closed is a property of the *path*, not of the *tool*.** Playwright
   *did* fail closed on 2026-09-02 — it exited 1 (§5.1). It failed closed in a
   workflow nobody was required to wait for.

### 4.2 Explicit correction — browser E2E is not the reliability lever

The earlier claim in this workstream that reliability requires broad Playwright
E2E coverage is **not supported by this evidence and is withdrawn**:

- Aurearia — **strongest** enforcement of the four — runs **zero** Playwright
  in CI. Its 5 specs are reachable only via a local Taskfile task.
- watch-tracker-app and drinks-and-desserts have **no** browser E2E and **no**
  skipped tests.
- Tech Inventory has **the most** browser E2E (17 specs × 6 projects) and is
  **the only repository with the incident**.

The correlation runs the other way: the repo that invested most in browser
breadth accumulated 19 `test.todo` stubs, 4 `describe.skip` blocks, and a
load-time crash that hid all of it.

**What was concluded historically:** browser tests are justified only by
browser-specific risk — journeys 9–12, the four never implemented and the cause
of the crash, being the four with the weakest such justification, while F045's
`display-mode: standalone` detection and service-worker shell were the strongest.

**What was decided, and is authoritative:** `brief.md` §2.1 retires Playwright
outright. The residual browser-specific risk above does **not** return as an
automated suite of any kind — it becomes a named, owned manual validation
checklist with a release cadence (T102); everything else migrates to HTTP
integration/contract and component tests, per T103's matrix.

---

## 5. T003 — PR #140 / Issue #89 Failure Chain

### 5.1 Timeline (all times UTC, 2026-09-02)

| Time | Event | Source |
| --- | --- | --- |
| `14:23:41Z` | CI Pipeline run **33641758342** created — `event: workflow_dispatch`, `headBranch: feature/pwa-device-navigation`, `headSha: 14e0d6f1cfe816d910dbe0cf7cbed2ceb3dd8934` | [run 33641758342](https://github.com/briandenicola/tech-inventory/actions/runs/33641758342) |
| `14:23:48Z` | Quality Gate run **33641764904** jobs start (`dotnet`, `web`, `codeql`, `secrets`, `container-config-scan`) | PR #140 `statusCheckRollup` |
| `14:24:05Z` | Step 11 "Run verification pipeline" (`./scripts/verify.sh`) begins | run 33641758342 job 100286327715 |
| `14:25:44Z` | `🎭 [9/9] Running Playwright against the hermetic compose stack...` | run log |
| `14:26:28Z` | Last Quality Gate job (`codeql`) completes **SUCCESS**. Rollup: `dotnet` ✅, `web` ✅, `codeql` ✅, `secrets` ✅, `container-config-scan` ✅, `sbom` ⏭ SKIPPED | PR #140 `statusCheckRollup` |
| **`14:27:09Z`** | **PR #140 merged by `briandenicola` → `d303cd6537392e2489222d5a0d5c946f39f2af0c`.** 0 reviews, 0 review comments, 51 files, +5,233/−713 | `gh api repos/briandenicola/tech-inventory/pulls/140` |
| `14:27:19Z` | `TypeError: test.todo is not a function` × 4, at `journeys/09`, `10`, `11`, `12` line 11 | run 33641758342 log |
| `14:27:20Z` | `##[error]Process completed with exit code 1` | run 33641758342 log |
| `14:27:24Z` | Job "Build, Test, and Verify" concludes **failure** | run 33641758342 |

**The merge preceded the failure by 10 seconds** — the verification run was
still in flight when the merge button was pressed.

### 5.2 The unchecked box

PR #140's own Definition of Done, as written by its author, contained:

```
- [x] Backend build and 494 tests green (278 unit, 216 integration; 5 integration skips)
- [x] 560 Vitest tests green (1 pre-existing skip)
- [x] OpenAPI/generated client unchanged
- [x] Post-major-work reviewer gate approved after independent correction
- [ ] GitHub Quality Gate green
- [ ] Manual full CI / Playwright workflow green
```
— [PR #140 body](https://github.com/briandenicola/tech-inventory/pull/140)

and in Notes:

> "Local `scripts/verify.ps1` reached the Docker-backed Playwright step and
> stopped because Docker is not installed on the workstation. The manual
> `ci.yml` workflow is being run on this branch to execute that gate."

The author correctly identified the gap, correctly left the box unchecked, and
correctly started the run. **The merge happened anyway, 10 seconds before that
run reported.** The skip counts in the body (5 integration, 1 Vitest) were
independently verified as accurate at `@d303cd6` — 4 `Skip = "…"` in
`tests/TechInventory.IntegrationTests/Auth/AuthIntegrationTests.cs`, 1 in
`Contract/OpenApiDriftTests.cs`, and `it.skip` at
`src/TechInventory.Web/src/…/DeviceForm.test.ts:436`. **The reporting was
honest; the gate was absent.**

### 5.3 The defect

Playwright's `TestType` has no `.todo` method. 19 calls exist across 4 files:

```ts
test.describe.skip('Journey 9: Export CSV', () => {
  test.todo('Admin can initiate CSV export from device list');
```
— `briandenicola/tech-inventory:tests/e2e/journeys/09-export-csv.spec.ts@d303cd6` (lines 10–11)

| File | `test.todo` calls |
| --- | --- |
| `tests/e2e/journeys/09-export-csv.spec.ts` (15 L) | 4 (lines 11–14) |
| `tests/e2e/journeys/10-reference-data-admin.spec.ts` (16 L) | 5 (lines 11–15) |
| `tests/e2e/journeys/11-role-enforcement.spec.ts` (16 L) | 5 (lines 11–15) |
| `tests/e2e/journeys/12-offline-app-shell.spec.ts` (16 L) | 5 (lines 11–15) |
| **Total** | **19** |

Playwright loads every file matched by
`testMatch: ['journeys/**/*.spec.ts', 'security/**/*.spec.ts']`
(`briandenicola/tech-inventory:tests/e2e/playwright.config.ts@d303cd6`, line 14)
**before executing anything**, so a load-time `TypeError` aborts the whole run.

**Blast radius — zero F045 coverage.**
`briandenicola/tech-inventory:tests/e2e/journeys/15-pwa-shell.spec.ts@d303cd6`
is 409 lines containing **22 `test(...)` declarations**, written explicitly
against the F045 contract:

```ts
/**
 * Journey 15: Installed-PWA app shell (F045)
 * Design authority: `specs/_backlog/F045-pwa-shell-and-device-list.md` +
```
— same file, lines 2–4

`playwright.config.ts` declares **6 projects** (`chromium-desktop`,
`chromium-mobile`, `webkit-desktop`, `webkit-mobile`, `firefox-desktop`,
`firefox-mobile`), so those 22 tests represent up to **132 executions**. **None
ran.** The feature PR #140 shipped was verified by zero live browser tests,
which were written, committed and merged in the same PR.

Issue #89 — "E2E suite is completely broken — `test.todo()` crash + `seedDevice`
schema drift", opened `2026-07-17T20:34:25Z`, **still `OPEN`** — documents this
exact defect, names the same four files, and states:

> "Because Playwright loads every matched spec file before running anything,
> this `TypeError` at load time aborts the **entire** run."

**The defect was known, correctly diagnosed, and publicly filed 47 days before
it blocked the F045 verification run.** #89 also documents a second independent
defect (`seedDevice()` missing now-required `ownerId`/`locationId`, cascading to
~20 failures) which was never reached, because Bug 1 aborts first. Under
`brief.md` §2.1, #89 closes by **retirement and migration**, not by repair.

### 5.4 Every control that should have blocked this, and why it did not

| # | Control | Should have | Why it did not | Citation |
| --- | --- | --- | --- | --- |
| 1 | **Branch protection / required status checks on `main`** | Prevent merge until the E2E-bearing check reported success | **Not configured.** `branches/main/protection` → `404 Branch not protected`; `rulesets` → `[]`. No check on this repo is technically required. *(State at the merge instant is `unknown` — API exposes current config only.)* | §2.1 |
| 2 | **`ci.yml` — the only workflow running Playwright** | Run on every PR and block | **Deliberately muted.** `on: workflow_dispatch:` with the in-file comment: *"Disabled — left on workflow_dispatch only so it doesn't auto-fire on every push/PR… this workflow's verify.sh pipeline (Playwright E2E + Docker compose) is the one we keep wanting to repair-then-mute."* | `:.github/workflows/ci.yml@d303cd6` L3–8, L10 |
| 3 | **Quality Gate — the de facto merge gate** | Cover the risk the PR carried | **Has no E2E job.** Jobs are `dotnet`, `web`, `codeql`, `secrets`, `container-config-scan`, `sbom`. A PWA-shell PR was gated by unit tests, type-check, lint, and scanners. Green here says nothing about browser behaviour. | `:.github/workflows/quality-gate.yml@d303cd6` |
| 4 | **`scripts/verify.sh` step 9/9** | Be the local pre-merge gate | Correct in design (`set -euo pipefail`, `./scripts/run-e2e.sh` last), but **requires Docker**, which the author documented as absent from the workstation. The local gate was structurally unreachable for this author. | `:scripts/verify.sh@d303cd6` L48; `:scripts/run-e2e.sh@d303cd6` |
| 5 | **PR-template Definition of Done** | Prevent merge on an unchecked box | **Advisory only.** A markdown checkbox has no enforcement. Two boxes were unchecked at merge. | `:.github/pull_request_template.md@d303cd6` |
| 6 | **Code review** | A second person sees the unchecked DoD | **0 reviews, 0 review comments.** Self-merge by `briandenicola` on a 51-file, +5,233/−713 change. | `gh api …/pulls/140/reviews` → `[]` |
| 7 | **CODEOWNERS auto-request** | Force a review request | **Placeholder `@your-github-handle`** — not a real account, so no owner could ever be requested. | §3.6 |
| 8 | **Zero-test-collection guard** | Fail when a suite runs no tests | **Does not exist** in this repo, or in any of the four (§2.9). Playwright happened to exit 1, so the failure was *visible* — but nothing asserted that ≥N tests ran. | §2.9 |
| 9 | **Stub-test detection** | Flag specs that declare intent but assert nothing | **Does not exist.** Four 15–16 line spec files contributed zero coverage and appeared in the suite listing as legitimate journeys for months. | §5.3 |
| 10 | **Issue #89 → gate linkage** | Convert a known defect into a blocking condition | **No linkage exists.** #89 was accurate and actionable on 2026-07-17 and remained `OPEN`, advisory, and disconnected from every gate for 47 days. | [issue #89](https://github.com/briandenicola/tech-inventory/issues/89) |
| 11 | **Timing discipline** | Wait for the in-flight verification run | No mechanism ties merge eligibility to a `workflow_dispatch` run; GitHub has no concept of "wait for a manual run". Merge and verification were racing, and merge won by 10 seconds. | §5.1 |
| 12 | **`.github/T47-CI-SETUP-CHECKLIST.md`** | Establish the branch protection in control #1 | **Written but not executed** — 284 lines describing CI setup that §2.1 shows was never applied. | `:.github/T47-CI-SETUP-CHECKLIST.md@d303cd6` |
| 13 | **`docs/testing.md` + `.copilot/skills/test-discipline`** | Make the E2E expectation binding | **Advisory documents.** They describe a test posture that no configured gate enforces. | §3.3 |
| 14 | **"Post-major-work reviewer gate approved"** (PR DoD, checked) | Independent quality review | An agent-run review, self-reported as passed in the PR body. **An agent approving an agent's work is not independent review** — this is precisely the self-approval the first principles now forbid. | PR #140 body |

### 5.5 What this chain proves

The failure was **not** insufficient testing. The tests for F045 existed — 22 of
them, in the same PR. The failure was that **the path from "tests exist" to
"tests ran and passed" contained fourteen controls and not one was fail-closed.**

**What it does not prove.** It does not show that this suite should be made
mandatory. The suite was *already described as* the mandatory browser gate while
having collected zero tests since 2026-07-17: the claim was **ceremonial and
broken**. Making a harness that cannot collect into a required check would
enforce the ceremony, not the coverage — hence retirement (`brief.md` §2.1).

Three specific lessons, each traceable to a row above:

1. **A muted gate is worse than no gate** (row 2). `ci.yml`'s comment shows the
   team knew the E2E pipeline was the real gate and knew it was disabled. The
   disabled state became invisible because Quality Gate was green.
2. **Green is a statement about what ran, not about what matters** (row 3). Six
   checks passed. None of them touched a browser. Six green checks on a
   PWA-shell PR is a category error, not a quality signal.
3. **Advisory controls degrade silently** (rows 5, 7, 10, 12, 13, 14). The DoD
   checkbox, CODEOWNERS, issue #89, the T47 checklist, the test-discipline
   skill and the reviewer gate all *said* the right thing; none could *do* it.

---

## 6. Unknowns

| # | Unknown | Reason |
| --- | --- | --- |
| U-01 | Branch-protection state of tech-inventory `main` **at** `2026-09-02T14:27:09Z` | GitHub REST exposes current configuration only; no audit-log query was attempted. Current state (unprotected) is documented; the historical state is not knowable from this evidence. |
| U-02 | Whether Quality Gate was configured as a *required* check at merge time | Same as U-01. It was **green**; whether it was **required** is not observable. |
| U-03 | Aurearia `.squad/log/` and `.squad/orchestration-log/` volume (33+ files) | Filenames contain colons from ISO-8601 timestamps; not checkoutable on Windows. Aurearia's instruction total is therefore an **under**-count. |
| U-04 | Whether `.squad/templates/` (74 files, 8,976 L — the largest tech-inventory subtree) is reachable by any loader | Requires tracing Squad tooling behaviour; not attempted within T002 scope. |
| U-05 | Whether `docs/known-issues.md` records issue #89 | File was inventoried by size but its content was not cross-referenced against open issues. |
| U-06 | drinks-and-desserts `docker-publish.yml` and `security-scan.yml` `on:` triggers | Bulk file read was truncated during collection; only the `continue-on-error` lines were captured. |
| U-07 | Whether Tech Inventory's E2E suite passes once the `test.todo` crash and the `seedDevice` drift (issue #89 Bug 2) are fixed | Cannot be determined without running it; Docker was unavailable in this environment, exactly as it was for the PR #140 author. **Moot under `brief.md` §2.1** — the suite is retired, not repaired, so the question has no subject; T101 closes it with that reason. |
| U-08 | Why `tests/e2e/theme-fouc.spec.ts` (62 lines) sits outside `testMatch` (`journeys/**`, `security/**`) and therefore never runs | Observed but not investigated; may be intentional or may be a second silent-coverage-loss instance. **Resolved by T103** as an ordinary migration-matrix row. |

---

## 7. Evidence-Collection Method

- Four repositories cloned or inspected at the pinned SHAs in §1; `git rev-parse HEAD`
  verified against the pinned value for each external clone.
- GitHub facts via `gh` CLI as `briandenicola` (token scopes:
  `admin:public_key`, `gist`, `project`, `read:org`, `repo`).
- Run 33641758342 failure log retrieved via `gh run view --log-failed`.
- Line and word counts via PowerShell `Get-Content` / `Measure-Object`,
  excluding `node_modules`, `bin`, `obj`, `dist`, `.git`, `.svelte-kit` and
  generated client output. Commands shown inline in §2.7.
- **No artifact created by any earlier agent was read or reused.**

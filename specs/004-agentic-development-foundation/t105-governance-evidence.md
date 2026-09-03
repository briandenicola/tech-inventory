# T105 — Governance Evidence (Ripley)

**Task:** T105 · AC-009 (governance half) · `specs/004-agentic-development-foundation/`
**Author:** Ripley (Lead / Architect)
**Date:** 2026-09-02
**Branch:** `chore/agentic-development-foundation` (uncommitted working tree; nothing committed or pushed by this scope)
**Parallel scope:** Apone owns guard tamper tests and checker execution
(`t105-tamper-evidence.md`). Nothing in this file claims a tamper test was run.

> **T105 is NOT `DONE`.** This file records one half of the task. Canonical
> `tasks.md` / `validation.md` are deliberately untouched: the acceptance ledger
> may only be assembled once Apone's tamper evidence exists and a human
> approves. No agent marks its own work `DONE` (`plan.md` §2.3).

---

## 1. Scope executed here

| # | Deliverable | State |
|---|---|---|
| 1 | ADR recording the browser-E2E retirement decision | **Done** — `docs/adr/0002-retire-browser-e2e-framework.md` |
| 2 | Constitution amended (narrow) | **Done** — v1.0.0 → **1.1.0** |
| 3 | PRD amended (narrow), 13 journeys preserved | **Done** — §7.5.2–§7.5.5 |
| 4 | Document hierarchy made self-consistent | **Done** — `plan.md` §6 / §6.1 closed, `brief.md` §4 scope amendment |
| 5 | PR template requires named checks + per-AC evidence | **Done** — `.github/pull_request_template.md` |
| 6 | `CODEOWNERS` routed to the real owner | **Done** — `.github/CODEOWNERS` |
| 7 | `T47-CI-SETUP-CHECKLIST.md` resolved | **Done** — retired in place as a historical notice |
| 8 | Check-name enumeration + branch-protection recommendation | **Done** — §3, §4 below (recommendation only; **not applied**) |
| 9 | Declined / unobserved settings recorded honestly | **Done** — §5 |
| 10 | Guard tamper tests | **Not this scope** — Apone |

---

## 2. Exact governance changes

### 2.1 New ADR

`docs/adr/0002-retire-browser-e2e-framework.md` — *"Retire the browser
end-to-end test framework"*, Status **Accepted**, dated **2026-09-02**, decider
`briandenicola`, recorded by Ripley.

Normative content: **zero automated browser role in any form** — not required,
scheduled, per-release, optional, or manual-dispatch — and **no substitute
framework** (Cypress / Selenium / Puppeteer / WebdriverIO or equivalent); the
retirement is of the *layer*, not the vendor. Valuable behaviour lives at
(1) real-HTTP integration/contract tests, (2) component tests with `axe-core`,
(3) the owned manual checklist `docs/testing/manual-pwa-validation.md`.
Residual behaviour is an owned accepted gap (`G-01`…`G-09`), never silent
coverage. Accessibility, security, coverage and acceptance requirements are
explicitly **unchanged in strength**. **Reversal requires a new superseding
ADR** naming the merge-blocking role, the collected-test floor, and the owner —
it cannot be reversed by a code, workflow, or task-level change.

### 2.2 Constitution — `.specify/memory/constitution.md` (1.0.0 → **1.1.0**)

| Clause | Before | After |
|---|---|---|
| Header | `Version: 1.0.0` | `Version: 1.1.0` + `Last amended: 2026-09-02 (ADR 0002)` |
| **§6.5.6** Accessibility | "`axe-core` in unit + E2E tests; zero violations" | "`axe-core` in unit **and component** tests; zero violations" + engine-specific a11y named as a manual, declared gap. **Zero-violation bar unchanged.** |
| **§6.5.14** Testing (Web) | "**E2E**: \<retired framework\> on critical user paths…"; "Accessibility: axe-core in unit + \<retired framework\>"; "Visual regression: optional v2 (… \<retired framework\> snapshots)" | "**Browser E2E: none** (ADR 0002)" in any role, no substitute framework; critical paths verified as real-HTTP integration/contract + component tests per PRD §7.5.4; manual checklist named; visual regression not adopted and may not smuggle a browser suite back in |
| **§7.2** Test Pyramid | "Integration tests (WebApplicationFactory + **Testcontainers for SQL**)"; "**E2E tests** (\<retired framework\> on PWA + API smoke)" | Integration = `WebApplicationFactory` + **real SQLite over real HTTP, no mocked API**; "**No automated browser layer** (ADR 0002)"; contract tests extended to name generated-client and EF migration drift gates |
| **§7.4** Local-First Testing | "**\<retired framework\>** is the required E2E framework — no substitutes" | "**There is no browser E2E framework** — the layer is retired (ADR 0002). Adopting any automated browser-test framework, in any role, requires a new ADR superseding ADR 0002" + `task verify` named as the authoritative entrypoint (no Docker, no browser download) |
| **§8.3** Branch Protection | "`main` requires: signed commits, linear history, CI green, review" | Same requirements **plus** the real required-check names (`verify`, `codeql`, `secrets`, `container-config-scan`; `sbom` excluded) **plus** a posture block recording the observed unprotected state as `REVIEWED`, not `ENFORCED`, owned by `briandenicola` |
| **§9** Quality Gate | "Web client: `tsc --noEmit`, ESLint, Vitest, **\<retired framework\> smoke**" | "Web client: `tsc --noEmit`, ESLint, Vitest unit + component tests (incl. `axe-core`)" + a new line requiring the retired-harness stale-reference guard clean |
| **§13** Definition of Done | "\<retired framework\> tests added or updated for any UI-facing change"; "`task test` runs green"; "No new flaky tests (E2E run twice locally)" | "Component and/or HTTP integration tests added or updated for any UI-facing change; browser-only behaviour recorded against the manual checklist"; "`task verify` runs green"; "No new flaky tests (the affected suite run twice locally)" |
| **§15** Revision History | single 1.0.0 row | new **1.1.0** row naming every amended section and stating that no accessibility, security, coverage or acceptance requirement was weakened |

**Deliberately not touched:** §5 (security), §6.5.9 (performance budgets and
Lighthouse thresholds), §7.1 (85% coverage), §7.3, §10–§12, §14. Amending them
is not this decision's business.

### 2.3 PRD — `docs/prd.md`

| Clause | Change |
|---|---|
| **§7.5.2** required test types | End-to-End (UI) row → **"Retired — no browser framework · ❌ Not required (ADR 0002)"**. Accessibility row → axe-core in Vitest unit + component tests, "All UI components and route harnesses". New row: **Manual PWA validation · ✅ Mandatory per release (manual, `REVIEWED`)**. Visual-regression row → not adopted, ADR required, must not reintroduce a browser suite. Integration row corrected from "Testcontainers + real **PostgreSQL**" (never true; the project is SQLite) to in-process `WebApplicationFactory` + real SQLite, no Docker |
| **§7.5.3** was *"\<retired framework\> (Mandatory E2E)"* | Rewritten as **"Browser End-to-End — Retired (ADR 0002)"**: no automated browser suite in any role, no substitute framework, the three destination layers, gaps `G-01`…`G-09` as owned and never reported as automated coverage, and the stale-reference guard |
| **§7.5.4** was *"Critical User Journeys (\<retired framework\> Coverage Required)"* | Retitled **"Critical User Journeys (coverage required at the lowest reliable layer)"**. **All thirteen journeys preserved verbatim as mandatory intents**, each re-pointed to its approved evidence (`H-01`…`H-05`, `C-01`…`C-19`, `M-05`…`M-15`, gaps `G-01`…`G-09`) with the coverage-matrix citation. "Removing a journey requires an ADR" is **retained**, and a new rule added: moving a journey to manual-only requires a named owner, release cadence, and recorded accepted gap |
| **§7.5.5** was *"The `task test` Contract"* | **"The `task verify` Contract"** — clean-checkout run with no Docker and no browser download; `task test` still runs the test stages; `task up`/`down` remain for manual work but are not required to verify a change; CI invokes the same entrypoint |

**Journey → evidence map now in PRD §7.5.4** (13/13, none dropped): 1 sign-in →
`C-01` + `M-11`/`G-08`; 2 denied → `AuthIntegrationTests` + `C-03`; 3 create →
`H-01` + `AddDeviceModal`/`DeviceForm` component tests + `M-12`/`G-01`; 4 edit → `UpdateDevice`
+ `C-04`/`C-05`; 5 delete → `H-02` + `DeleteDeviceModal`; 6 browse/filter →
`deviceFilterUrl`/`viewState` + `C-06`; 7 detail → `GetDeviceById` +
`C-07`/`C-08`; 8 import → `ImportsControllerTests`/`H-05` + `C-09`/`C-19` +
`M-12`/`G-02`; 9 export → `ExportControllerTests`/`H-03` + `C-10` +
`M-13`/`G-03`; 10 reference admin → reference controller tests + `C-11`;
11 role enforcement → `H-04` (`ViewerRoleAuthorizationTests`) + `C-12`/`C-13`;
12 offline shell → `C-14`…`C-17` + `M-05`–`M-09`/`G-04`; 13 a11y smoke →
37 axe files + `C-18` (5 of 6 route harnesses; `/devices` = `G-09`) +
`M-14`/`M-15`/`G-05`.

### 2.4 Hierarchy consistency

- `plan.md` **§6** — the "no ADR is created here" position is marked **amended
  by the 2026-09-02 human decision**, with the original text retained for the
  record; branch protection remains recommendation-only.
- `plan.md` **§6.1** — the package-closure precondition (the named §2.10
  exception that the constitution and PRD still mandated a deleted framework) is
  marked **CLOSED** with its closure trigger itemised.
- `brief.md` **§4** — the non-goal "not rewriting the constitution and not
  creating an ADR yet" carries a dated, attributed **scope amendment** lifting
  it for T105 only, limited to clauses that mandated the retired framework or
  executable browser journeys. The approved brief's decision text (§2.1) is
  unchanged.

### 2.5 `.github/pull_request_template.md`

Replaced generic completion with evidence-bearing completion:

- **Acceptance-criteria evidence table** — one row per AC requiring the exact
  test name / path / command / run URL, with the explicit rule *"CI is green is
  not evidence"*.
- **Named required checks**, each with its GitHub check name: `verify`
  (with its full stage list), `codeql`, `secrets`, `container-config-scan`,
  plus `task verify` green locally. A comment records that `sbom` is a
  `main`-push job and not a PR check, and that until branch protection exists
  these boxes describe what the author *observed*, not what blocked them.
- **Collected-test floor declaration** (unit / integration / frontend) so a
  zero-collection run cannot pass as green.
- **Manual PWA checklist declaration** — mandatory tri-state (not applicable /
  ran checks `M-__` with runner + date + result / applicable but not run →
  exception) whenever the PR touches shell, navigation, service worker, offline,
  theming, install/manifest, or auth surfaces.
- **Explicit exceptions table** — rule, scope, **owner**, duration/closure
  trigger, with an explicit "no exceptions apply" box, per `plan.md` §2.10.
- Work-state line requiring the human `APPROVED` transition to be named.
- Definition of Done now says "tests at the **lowest reliable layer**" and
  "component and/or real-HTTP integration coverage for any UI-facing change
  (ADR 0002)".

### 2.6 `.github/CODEOWNERS`

Was an unedited placeholder: `@your-github-handle` on five patterns, one of
which (`deploy/`) does not exist. Every rule was therefore inert.

Now: `@briandenicola` (verified repository owner via
`gh api repos/briandenicola/tech-inventory`) on `*` plus governance
(`/.specify/`, `/docs/prd.md`, `/docs/adr/`, `/specs/`), automation
(`/.github/`), the verification surface (`/Taskfile.yml`, `/scripts/`), and the
API surface (`/src/TechInventory.Api/`, `/src/TechInventory.Domain/`,
`/openapi.yaml`). **Every path was existence-checked**; `deploy/`,
`src/TechInventory.Contracts/` and `docs/api/` were dropped as non-existent.
The file states its own limits in-line: GitHub cannot request a review from the
PR author, so with one maintainer code-owner review is unenforceable — hence
`require_code_owner_reviews` is **not** recommended in §4.

### 2.7 `.github/T47-CI-SETUP-CHECKLIST.md`

**Retired in place** (284 lines → a ~60-line historical notice). It was a dated
May-2026 setup narrative, not maintainable documentation, and it was
misleading in three specific ways, all now stated in the file itself:

1. It marked eleven checks **"✅ Enforced"** while `main` had no branch
   protection — checks that report but gate nothing.
2. It instructed the operator to require `ci / verify` / `ci / Build, Test, and
   Verify`, from a workflow that is `workflow_dispatch`-only.
3. Its E2E rows described the harness retired by ADR 0002.

The notice names its successors (`.github/workflows/README.md`, this file §3–§4,
`Taskfile.yml`), keeps a "what is still true and where it lives" table, and ends
with *"No control described in this file is enforced by this file."* The
original text stays in git history. Path deliberately preserved so existing
links resolve rather than 404.

### 2.8 `.github/workflows/README.md` (accuracy only)

Added a dated T105 note defining "Enforced" in that document as *"runs on every
PR and fails the job"* — **not** "blocks merge" — and recording the observed
unprotected state. Changed Quality Gate's `Status:` from "The active
merge-blocking workflow" to the workflow *intended* to gate merges, not
merge-blocking until protection is applied. Replaced the stale manual
branch-protection section with a pointer to §4 here, the correct check list,
the `sbom` and `ci.yml` exclusions, the `enforce_admins: false` precedent, and
the "confirm context strings against a real run" instruction.

---

## 3. Check names — current vs target

### 3.1 Observed check names (source: `gh pr checks 140`, the last real PR, merged 2026-09-02T14:27:09Z)

| Check name | Result then | Workflow |
|---|---|---|
| `dotnet` | pass 2m11s | Quality Gate (**pre-T104 layout — job no longer exists**) |
| `web` | pass 1m43s | Quality Gate (**pre-T104 layout — job no longer exists**) |
| `codeql` | pass 2m40s | Quality Gate |
| `secrets` | pass 6s | Quality Gate |
| `container-config-scan` | pass 19s | Quality Gate |
| `sbom` | **skipping** | Quality Gate (`if: github.ref == 'refs/heads/main'`) |

### 3.2 Target check names (source: workflow definitions in the working tree, post-T104)

| Check name | Workflow / file | Triggers | Runs on PRs? | Observed? |
|---|---|---|---|---|
| **`verify`** | `quality-gate.yml` job `verify` | push `main`, `pull_request`, weekly cron | **Yes** | **No — never executed on GitHub Actions.** The rewritten job exists only in the working tree |
| **`codeql`** | `quality-gate.yml` job `codeql` | same | Yes | Yes (PR #140) — job unchanged by T104 |
| **`secrets`** | `quality-gate.yml` job `secrets` | same | Yes | Yes (PR #140) — unchanged |
| **`container-config-scan`** | `quality-gate.yml` job `container-config-scan` | same, `if: pull_request` | Yes (PR events only) | Yes (PR #140) — unchanged |
| `sbom` | `quality-gate.yml` job `sbom` | same, `if: ref == main` | **No** (skipped on PRs) | Skipped (PR #140) — **must not be required** |
| `Build, Test, and Verify` | `ci.yml` job `verify` | `workflow_dispatch` only | No | Not a gate; do not require |
| `Build & push <image> image` | `release-images.yml` | push `main`, tags, dispatch | No | Post-merge; name is matrix-dynamic — never require |
| `Resolve image tag`, `Trivy scan — API image`, `Trivy scan — Web image` | `security-scan.yml` | `workflow_dispatch` only (disabled) | No | Do not require |
| `Squad Heartbeat (Ralph)`, `Squad Issue Assign`, `Squad Triage`, `Sync Squad Labels` | `squad-*.yml`, `sync-squad-labels.yml` | issues / push / schedule / dispatch | No | Ops automation — never a merge gate |

**Guards that are *inside* `verify`, not separate check names:** `check:format`,
`build:backend`, `check:frontend`, `lint`, `test:unit`, `test:integration`,
`test:frontend`, `check:stale-refs`, `check:openapi-drift`,
`check:client-drift`, `check:migration-drift`, `check:vulnerable`, and the
collected-test floors (`scripts/check-test-floors.mjs`). This is a deliberate
T104 consequence — one entrypoint, one check — and it means required-check
granularity is coarse: `verify` is pass/fail for twelve-plus guards. Recorded as
a tradeoff, not a defect: splitting them into separate required checks would
duplicate the pipeline graph that T104 exists to unify.

---

## 4. Branch-protection recommendation for `main` — **recommendation only, not applied**

**Owner of the decision:** `briandenicola`. Plan scope forbids applying
repository settings from inside this package (`brief.md` §4, `plan.md` §3).

### 4.1 Observed current posture (2026-09-02, re-verified during this task)

```text
gh api repos/briandenicola/tech-inventory/branches/main/protection
  → 404 "Branch not protected"
gh api repos/briandenicola/tech-inventory/rulesets
  → []
```

Nothing gates a merge to `main` today.

### 4.2 Precedent — `briandenicola/Aurearia` (the one audited repo with enforced checks), read live during this task

```json
{ "strict": true,
  "contexts": ["Go API","Vue Web","Python Agent","Gitleaks","Govulncheck","npm audit","pip-audit"],
  "enforce_admins": false, "reviews": null, "linear": false,
  "force": false, "del": false, "conv": false, "sigs": false }
```

### 4.3 Recommended payload

`PUT /repos/briandenicola/tech-inventory/branches/main/protection`

```json
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["verify", "codeql", "secrets", "container-config-scan"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": true,
  "lock_branch": false,
  "allow_fork_syncing": false
}
```

Apply with:

```bash
gh api -X PUT repos/briandenicola/tech-inventory/branches/main/protection \
  --input branch-protection.json
```

**Sequencing — do not skip:** open one PR from
`chore/agentic-development-foundation` **first**, let Quality Gate run, then
confirm the exact context strings with `gh pr checks <PR>` before writing them
into the payload. A required context that never reports leaves PRs permanently
"Expected — Waiting for status to be reported", which is a self-inflicted
outage, not a gate. `verify` has never run on GitHub Actions (§3.2).

**Rationale per field**

| Field | Value | Why |
|---|---|---|
| `strict` | `true` | Aurearia precedent; prevents a green-on-stale-base merge, the class of failure PR #140 belongs to |
| `contexts` | the four PR-running Quality Gate jobs | Only checks that actually run on `pull_request`. `sbom` is excluded because it is skipped on PRs; `ci.yml` and `security-scan.yml` are excluded because they are `workflow_dispatch`-only |
| `enforce_admins` | `false` | Observed single-operator precedent. The sole maintainer must retain a break-glass path; the value of the gate here is that bypass is *visible and deliberate*, not impossible |
| `required_pull_request_reviews` | `null` | Aurearia precedent, and GitHub cannot request review from the PR author. Requiring approvals in a single-operator repo produces either a permanent block or a rubber stamp. **This is the field where the recommendation knowingly falls short of constitution §8.3's "review"** — recorded in §5 as a declined control, not hidden |
| `required_linear_history` | `true` | Constitution §8.1/§8.3. Beyond the Aurearia precedent (`false`) — flagged as such |
| `allow_force_pushes` / `allow_deletions` | `false` | Aurearia precedent; protects the audit trail |
| `required_conversation_resolution` | `true` | Cheap; stops review threads dying unread. Beyond precedent (`false`) — flagged |
| `restrictions` / `lock_branch` / `block_creations` | `null` / `false` / `false` | No push allowlist and no archive semantics wanted on an active single-operator repo |

**Signed commits** are a *separate* endpoint and constitution §8.1 requires them
on `main`:

```bash
gh api -X POST repos/briandenicola/tech-inventory/branches/main/protection/required_signatures
```

Recommended **only after** confirming commit signing works locally on this
machine — enabling it first will block the operator's own merges. Whether
signing is currently configured is **unknown** (§6, U-3).

### 4.4 If the recommendation is declined

Declining is a legitimate outcome, but not a silent one. Record it as an
explicit visible exception under `plan.md` §2.10 with:
rule (`constitution §8.3`), scope (`main` unprotected), owner
(`briandenicola`), duration or closure trigger, and the accepted consequence
(every check in constitution §9 is `REVIEWED`, never `ENFORCED`; a green PR
proves a run happened, not that a gate held). Constitution §8.3 already carries
this posture note so the gap is visible in the highest-authority document, not
only here.

---

## 5. Declined, deferred, and unobserved — recorded honestly

| # | Item | Status | Note |
|---|---|---|---|
| D-1 | Required PR reviews on `main` | **Declined in the recommendation** | Single operator; GitHub cannot request self-review. Falls short of constitution §8.3's "review" — stated, not concealed |
| D-2 | `require_code_owner_reviews` | **Declined** | Same reason; `.github/CODEOWNERS` documents the limitation in-line |
| D-3 | `enforce_admins: true` | **Declined** | Observed precedent is `false`; break-glass must exist for a one-person repo |
| D-4 | Applying any repository setting | **Out of scope by plan** | `brief.md` §4 — recommendation only |
| D-5 | Splitting `verify` into per-guard required checks | **Declined** | Would duplicate the command graph T104 unified; coarse granularity accepted (§3.2) |
| D-6 | Requiring `sbom` | **Declined** | Never runs on PRs; requiring it would hang every PR |
| D-7 | Re-enabling `ci.yml` or `security-scan.yml` | **Not recommended** | Both are `workflow_dispatch`-only by prior decision; out of T105's scope to change |
| U-1 | **Has the post-T104 `verify` job ever run on GitHub Actions?** | **No — unobserved** | `gh run list` for the branch shows exactly one run: `Sync Squad Labels` (2026-09-02T22:50:58Z, success). No Quality Gate run exists at `764282e`/`b3c092f`. Every statement about `verify` in §3.2 is derived from the workflow definition, **not** from an observed run |
| U-2 | Whether the required contexts will match `verify`/`codeql`/`secrets`/`container-config-scan` byte-for-byte in the protection API | **Unknown until a run exists** | Job-name-derived; PR #140 reported bare job names (`dotnet`, `web`, …), which is the basis for expecting bare names. Confirm with `gh pr checks` before applying (§4.3) |
| U-3 | Whether commit signing is configured on the operator's machine | **Unknown — not probed** | Enabling `required_signatures` before confirming would block the operator |
| U-4 | Historical branch-protection state at the PR #140 merge instant | **Unknowable via REST** | Carried forward from `evidence.md` §2.1; no audit-log access attempted |
| U-5 | Whether GitHub Advanced Security / code scanning upload is enabled | **Unknown** | `codeql` deliberately uploads `upload: false` and keeps SARIF as an artifact, so requiring `codeql` is safe regardless |

**No claim in this file asserts that a gate passed before it was implemented and
observed** (AC-010). In particular: the amendments in §2 are *documents changed*,
not *gates passed*; §4 is a *recommendation*, not an applied setting.

---

## 6. Remaining unknowns and integration blockers

### B-1 — The new ADR trips the stale-reference guard (**must be resolved before the coordinator commits**)

`scripts/check-stale-playwright-references.mjs` scans every **tracked** file.
`docs/adr/0002-retire-browser-e2e-framework.md` is currently untracked, so
`task verify` passes today — but the moment it is staged the guard fails.

Measured, not guessed: driving the guard's exported pure classifier
`findStaleReferences()` over the real file contents (read-only; no repository
mutation, no tamper test, nothing in Apone's scope executed) returns

```text
violations: 9
docs/adr/0002-retire-browser-e2e-framework.md:1, 6, 20, 21, 25, 32, 52, 80, 118
```

and **0** violations for the other files this scope changed
(`.github/T47-CI-SETUP-CHECKLIST.md`, `.github/pull_request_template.md`,
`.github/CODEOWNERS`, `.github/workflows/README.md`).

**Required fix — owned by Apone (guard/script owner), not applied here:** add
one entry to `EXEMPT_EXACT_PATHS`:

```js
'docs/adr/0002-retire-browser-e2e-framework.md',
```

**Rationale:** identical in class to the existing `.specify/memory/constitution.md`
and `docs/prd.md` exemptions — a normative document must be able to name the
thing it retires, or the record becomes unreadable and the decision
uncitable. The exemption is one named file, not a `docs/adr/` prefix; a future
ADR that *reintroduces* a browser framework must therefore still be an explicit,
argued exemption rather than an inherited one. The ADR's filename is deliberately
neutral (`0002-retire-browser-e2e-framework.md`) so that citing its path from
non-exempt files does not itself trip the guard. If Apone prefers not to widen
the allowlist, the alternative is to rewrite the ADR body in the euphemism style
used elsewhere in `.github/` — **not recommended**: an ADR that cannot name its
own subject is a weaker artifact than a one-line, argued exemption.

### B-2 — This file's own path is not on the guard allowlist

`specs/004-agentic-development-foundation/t105-governance-evidence.md` is **not**
in `EXEMPT_SPEC_PATHS` (only the six original work-package files are). It is
written to avoid the retired tool's literal name for exactly this reason, and it
verified clean under the same read-only classifier run. Apone's parallel
`t105-tamper-evidence.md` faces the same constraint and is Apone's call.

### Open items for the integration turn (not done here, deliberately)

1. Apone's `t105-tamper-evidence.md` — every critical non-browser guard with a
   recorded deliberate break.
2. Canonical `tasks.md` / `validation.md` assembly (AC-009 ledger, T105 state
   transition) — **after** both parallel scopes land and are read.
3. ~~The §2.10 exception entries: the manual PWA checklist (owner + cadence) and,
   if declined, branch protection.~~ **RESOLVED by Hudson, 2026-09-02
   (T105 revision · B-3):** neither entry existed when Bishop's reviewer gate
   rejected on this exact gap (`validation.md` §13.2). Both are now recorded
   at `plan.md` §6.2 (branch protection — declined-for-now, owner
   `briandenicola`, closure trigger = the §4.3 payload applied and confirmed
   against a real Quality Gate run) and §6.3 (manual PWA checklist — owner
   `briandenicola`, closure trigger = per-release re-review or a superseding
   ADR). Constitution §8.3 (v1.1.1) and this file's own cross-references
   now point at the live entries instead of forward-referencing a register
   that did not exist. See `specs/004-agentic-development-foundation/t105-setup-revision.md`.
4. Human `APPROVED` transition and the reviewer gate. Neither half of T105 may
   approve itself.
5. `.squad/decisions.md` ledger entry for ADR 0002 and the branch-protection
   recommendation (coordinator-owned; the inbox record is
   `.squad/decisions/inbox/ripley-t105-governance.md`).

---

## 7. Files changed by this scope

| File | Change |
|---|---|
| `docs/adr/0002-retire-browser-e2e-framework.md` | **new** — ADR 0002 |
| `.specify/memory/constitution.md` | amended §6.5.6, §6.5.14, §7.2, §7.4, §8.3, §9, §13, §15; version 1.1.0 |
| `docs/prd.md` | amended §7.5.2, §7.5.3, §7.5.4, §7.5.5 |
| `specs/004-agentic-development-foundation/plan.md` | §6 amended; §6.1 closed |
| `specs/004-agentic-development-foundation/brief.md` | §4 dated scope amendment |
| `.github/pull_request_template.md` | rewritten — named checks + per-AC evidence + manual-checklist declaration + exceptions table |
| `.github/CODEOWNERS` | placeholder replaced with real, existence-checked routing |
| `.github/T47-CI-SETUP-CHECKLIST.md` | retired in place as a historical notice |
| `.github/workflows/README.md` | accuracy note + corrected branch-protection section |
| `specs/004-agentic-development-foundation/t105-governance-evidence.md` | **new** — this file |

Nothing was committed or pushed. No verification script, checker, test, or
`t105-tamper-evidence.md` was touched.

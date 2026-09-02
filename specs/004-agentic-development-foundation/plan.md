---
id: 004-agentic-development-foundation
document: plan
tier: T2
status: APPROVED
approved_by: briandenicola
approved_at: 2026-09-02T10:06:57-05:00
base_sha: d303cd6537392e2489222d5a0d5c946f39f2af0c
---

# Plan — Agentic Development Foundation

Implements [`brief.md`](./brief.md) against the findings in
[`evidence.md`](./evidence.md). Executable checklists live in
[`tasks.md`](./tasks.md); status lives in [`validation.md`](./validation.md).

**Phase boundary:** T001–T004 are `DONE` (documents only). **T103 is `DONE`** —
its deliverable is [`coverage-migration.md`](./coverage-migration.md), a fresh
analysis of the working tree at `d303cd6`. An earlier T103 run against the
*pre-retirement* brief was stopped; its artefact `e2e-classification.md` carried
no authority (`validation.md` §3.7) and **has been deleted**, with none of its
conclusions carried forward. **T102 is now `DONE` and reviewer-APPROVED** —
Ripley's final independent gate on 2026-09-02 re-ran every cited command,
tamper-tested the new contract guard, and closed blockers B3 / B2-R / B4; see
[`coverage-migration.md`](./coverage-migration.md) §12 and
[`validation.md`](./validation.md) AC-006 and §6. **T101 was `REJECTED`** at
Ripley's independent reviewer gate on two blockers (`validation.md` §7);
**Apone revised it 2026-09-02**, closing both blockers (`validation.md` §7.7,
`coverage-migration.md` §13.9), and **Ripley's re-review gate on 2026-09-02
returned APPROVED — T101 is now `DONE` and AC-005 is met** (`validation.md`
§7.8, `coverage-migration.md` §13.10). **T104 is `DONE`**: it was
**REJECTED at Apone's independent reviewer gate on 2026-09-02**
(`validation.md` §10) — implemented by Hudson subject to the two conditions
in `validation.md` §7.8.5/§7.8.6 (both honored, not resolved — see
`validation.md` §9) — on three blockers: B-1 `check:client-drift` failed
with provably zero drift so `task verify` had never completed, B-2
`check:vulnerable` could not fail yet was documented "Enforced", B-3 `ci.yml`
depended on PyYAML without installing it. **Hicks revised it 2026-09-02**,
closing all three blockers (`validation.md` §11), and **Apone's re-review
gate at commit `b3c092f` returned APPROVED — T104 is `DONE` and AC-008 is
met** (`validation.md` §12): each blocker was re-verified by reviewer-run
evidence (dirty-but-synchronized drift pass where `git diff` fails, a
genuinely stale client caught, direct **and** transitive vulnerable probes
both exiting 1, two-workflow PyYAML parity), and **`task verify` was observed
running end to end at exit 0 with no Docker and no browser**. Hudson's
lockout for that cycle ends with it; **T105 is now AUTHORIZED to begin** and
remains `APPROVED` and **not started**. T102 touched test files across
both suites plus a small, named set of production files, each recorded and
either a bug fix discovered by the new tests or an explicitly approved,
tightly-coupled authorization fix (`coverage-migration.md` §12.1; the
`viewer-auth-fix-results.md` citation there is a session artefact, not
present in this repository — the durable evidence is
`ViewerRoleAuthorizationTests`) — no unrelated file, and nothing outside the
`src/`, `tests/`, and `docs/testing/` paths this task names, has changed.

**Authoritative decision:** Playwright is retired (`brief.md` §2.1). No task in
this plan proposes PR-blocking, scheduled, release, or optional automated
browser execution. There is **no future automated Playwright role.**

---

## 1. Approach

1. **Retire before gating.** A suite that cannot collect cannot fail; gating on
   it would only make a ceremonial harness mandatory. T103 inventories what it
   claimed, T101 removes it, T102 rebuilds the valuable assertions at layers
   that run, T104/T105 make those binding.
2. **Move controls from advisory to enforced before adding new controls.** All
   28 catalogued authority sources are advisory (`evidence.md` §3.4); a 29th
   advisory document changes nothing.
3. **Prefer cheap deterministic gates over expensive probabilistic ones.**
   Contract drift, migration validation and typed HTTP integration cost seconds
   and never flake; browser automation was the opposite on both counts here.
4. **Make the failure mode of every automation "block", not "pass"** — and prove
   it by breaking it on purpose (T105).
5. **An honest declared gap beats a silent one** — what genuinely needs a
   browser becomes a named manual checklist with an owner and a cadence.

---

## 2. T004 — Approved First Principles and Scaled Work Model

The durable record. Satisfies **AC-004**. Approved by `briandenicola` at
`2026-09-02T10:06:57-05:00`.

### 2.1 Work states

```
DISCOVERY → PLAN_REVIEW → APPROVED → BUILDING → TESTING → VALIDATING → DONE
```

| State | Meaning | Exit condition |
| --- | --- | --- |
| `DISCOVERY` | Understanding the problem; gathering evidence. **No implementation.** | A problem statement and evidence exist. |
| `PLAN_REVIEW` | A written plan exists and is under human review. **No implementation.** | The human has read the plan and responded. |
| `APPROVED` | A human has explicitly authorized the named scope. | Recorded approver and timestamp. |
| `BUILDING` | Implementation of the approved scope only. | The approved scope is implemented. |
| `TESTING` | Tests written/run at the lowest reliable layer. | Tests exist and execute. |
| `VALIDATING` | Acceptance evidence gathered against the stated criteria. | Evidence recorded per criterion. |
| `DONE` | Acceptance evidence is complete and recorded. | — |

**States are not skippable.** Backward transitions are permitted and must be
recorded with a reason.

### 2.2 Discussion never authorizes implementation

Exploring an idea, agreeing it is good, or describing how it would work **is not
approval to build it**. Only an explicit `APPROVED` transition naming the scope
authorizes `BUILDING`. *Failure:* PR #140 shipped an unpromoted item with no
recorded approval gate (`evidence.md` §3.3, §5).

### 2.3 Agents cannot self-approve

An agent may not move work into `APPROVED`, may not mark its own work `DONE`,
and may not satisfy a review requirement for its own change. An agent reviewing
another agent's output is **not** independent review. *Failure this addresses:*
PR #140's "Post-major-work reviewer gate approved" DoD line was an agent-run
review, self-reported as passed, on a self-merged PR with zero human reviews
(`evidence.md` §5.4 row 14).

### 2.4 Work tiers — scaled ceremony

| Tier | Definition | Required ceremony |
| --- | --- | --- |
| **T0** | Mechanical. Formatting, typos, dependency bumps, generated-file refresh. No behaviour change. | **The request itself.** Green required checks. No issue, no plan, no spec folder. |
| **T1** | Bug fix or small change. Single component; behaviour change is local and reversible. | **A tracking issue + a mini-plan** (defect, intended fix, the test that fails before and passes after). No spec folder. |
| **T2** | Feature or cross-cutting change. Multiple components, or changes a contract, schema, or shared convention. | **A full work package** — `brief.md` + `plan.md` + `tasks.md` + `validation.md`, explicit `APPROVED` transition, acceptance evidence. |
| **T3** | High risk. Auth, data migration with loss potential, deployment topology, security posture, or anything irreversible. | Everything in T2, plus an ADR, a named human risk owner, and a stated rollback. |

Ceremony is **proportionate**. A promoted multi-file spec folder is required at
**T2/T3 only**; demanding one for every change trains everyone to skip ceremony,
which is the behaviour that produced PR #140 (51 files, +5,233/−713 —
unambiguously T2, executed with T0 ceremony).

### 2.5 Every important promise is ENFORCED, REVIEWED, or ADVISORY

| Class | Meaning | Failure mode |
| --- | --- | --- |
| **ENFORCED** | A machine blocks the action if the promise is unmet. | Cannot proceed. |
| **REVIEWED** | A named human must inspect and sign. | Cannot proceed without a signature. |
| **ADVISORY** | Guidance. Non-compliance is visible but not blocking. | Proceeds. |

Every promise that matters carries **exactly one** label. There is **no fourth
state** — an unlabelled promise where everyone assumes someone else is checking
is the failure mode this rule eliminates. Downgrading is an explicit, visible
change with a recorded reason (§2.10). *Evidence:* all 28 authority sources are
`ADVISORY` (`evidence.md` §3.4), six widely assumed to be blocking.

### 2.6 Tests at the lowest reliable layer

| Risk | Correct layer |
| --- | --- |
| Business rule, calculation, validation | Domain unit test |
| Command/query orchestration, authorization | Application unit test |
| Persistence, migration, query correctness | Integration test against a **real** database |
| Request/response shape, status codes, ProblemDetails | Contract / integration test |
| Contract-vs-implementation agreement | **Generation + diff gate** — not a test |
| Rendering, state, navigation affordances, accessibility of a component | Component test |
| `display-mode: standalone`, service worker, install prompt, engine-specific rendering | **Explicit manual validation checklist** — named owner, release cadence |

**There is no browser-automation layer in this model.** *(Amended 2026-09-02 by
`briandenicola` — `brief.md` §2.1; the original table reserved a browser-test
row, and retirement removes it.)* "It's a user journey" was never sufficient
justification. What is genuinely browser-specific becomes an **owned manual
checklist** (T102). *Evidence:* `evidence.md` §4.2.

### 2.7 Local and CI use the same authoritative commands

There is **one** authoritative command surface. CI invokes it. Humans invoke it.
"It passed locally" and "it passed in CI" must mean the same thing, or one is a
lie. Divergence is permitted only where a platform genuinely requires it, must
be declared in-repo at the point of divergence, and must not change what is
verified. *Evidence:* `evidence.md` §2.2 — none of the four repositories has
parity; Tech Inventory routes its script through the **disabled** workflow.

### 2.8 Completion requires acceptance evidence, not only green tests

`DONE` requires, per acceptance criterion, a recorded artefact: a run URL, test
name, diff, command output, or screenshot. Green checks are *inputs* to that
evidence, not a substitute. *Evidence:* PR #140 had six green checks and zero
executed browser tests (`evidence.md` §5.1, §5.3) — true and meaningless.

### 2.9 Historical context is not current instruction

A record of what was decided, discussed, or done is **history**. It informs; it
does not instruct. Only documents explicitly designated normative carry
instruction authority, and history must be structurally separable from it.
*Evidence:* `evidence.md` §3.2 — 11,301 lines (37.4%) are logs, session records
and append-only history inside instruction trees.

### 2.10 Exceptions are explicit and visible

Any deviation must be **written down** where the rule is stated, **attributed**
to a named human, **scoped** (which change, which rule, how long), and
**reviewable** later without archaeology. An undocumented exception is a defect;
a silent `ENFORCED` → `ADVISORY` downgrade is the case this targets. *Evidence:*
`.github/workflows/ci.yml@d303cd6` L3–8 is an honest written exception — *"the
one we keep wanting to repair-then-mute"* — recorded only inside the muted file,
attributed to nobody, unscoped, undated.

---

## 3. Exact Scope

The control baseline and target class per control is tabulated once, in
[`validation.md`](./validation.md) §2, and is not repeated here.

### T001–T004 — research and record · `DONE` · documents only

Outputs: `evidence.md` §1/§2/§4 (T001), §3 (T002), §5 (T003), `plan.md` §2
(T004). Checklists and unknowns are in [`tasks.md`](./tasks.md) Phase 1.

### T101–T105 — implementation · T103, T102, **T101 `DONE`** (T101 rejected → revised by Apone → reviewer-APPROVED, `validation.md` §7.8); **T104 `DONE`** (rejected by Apone → revised by Hicks → re-reviewed and APPROVED by Apone at `b3c092f`, `validation.md` §12); T105 `APPROVED` and AUTHORIZED, not started

Each boundary is fixed here so scope cannot silently expand at execution.
Checklists are in [`tasks.md`](./tasks.md); the diagnosis is in `evidence.md`.

| Task | Defect / context | In scope | Out of scope · precondition |
| --- | --- | --- | --- |
| **T101** — Retire the broken Playwright harness safely · AC-005 · **`DONE` (rejected at Ripley's first gate 2026-09-02, Hudson's self-recorded `DONE` withdrawn → revised by Apone 2026-09-02 → re-reviewed and APPROVED by Ripley 2026-09-02, `validation.md` §7.8)** | The harness has never collected: 19 unsupported `test.todo()` calls across `journeys/09-export-csv`, `10-reference-data-admin`, `11-role-enforcement`, `12-offline-app-shell` abort every run at module load (`evidence.md` §5.3). It was named as the mandatory browser gate while asserting nothing. | Remove `tests/e2e/**`, `playwright.config.ts`, the Playwright dependency in `package.json` / `pnpm-lock.yaml`, and every invocation in `Taskfile.yml`, `scripts/run-e2e.*`, `scripts/verify.*`, and `.github/workflows/**`. **Inventory before delete:** no file is removed until its T103 matrix row exists. Strip Playwright from verification promises — `docs/testing.md`, `.github/pull_request_template.md`, `.copilot/skills/test-discipline`, `.github/T47-CI-SETUP-CHECKLIST.md`. Add a **stale-reference guard** that fails the build on any returning Playwright reference. Close #89 citing retirement + migration. | *Out:* repairing, muting, scheduling, or re-enabling any browser suite; deleting anything lacking a T103 row. *Precondition:* **T103** — the matrix is the deletion authority. **Delivered 2026-09-02:** `tests/e2e/**` (28 files), `scripts/run-e2e.*`, `.squad/skills/playwright-e2e-scaffolding/` deleted, cross-checked against `coverage-migration.md` §4 waves D1–D5/D8. `scripts/check-stale-playwright-references.mjs` guard added (15/15 `node:test` cases, live run 0/901 files), wired into `Taskfile.yml`/`verify.ps1`/`verify.sh`. Clean install proven to download 0 browsers. Full evidence and remaining-reference classification in `coverage-migration.md` §13; `validation.md` AC-005. Two explicit, matrix-authorized exceptions recorded (not silent): `.specify/memory/constitution.md` and `docs/prd.md` still name Playwright as mandatory pending a separate ADR (§5.4) — surfaced here, not resolved. Issue #89 closed by `briandenicola` prior to this session; durable retirement+migration evidence comment added by Hudson. **Rejected 2026-09-02 (`validation.md` §7)** for B1 (stale `task test:e2e`/`task test:e2e:run` instructions surviving in `.github/T47-CI-SETUP-CHECKLIST.md`) and B2 (blanket `specs/` guard exemption hiding 16 live `specs/_backlog/**` Playwright promises). **Revised 2026-09-02 by Apone:** both blockers closed — B1's stale instructions and E2E troubleshooting block removed without claiming T104's future surface already exists; B2's guard exemption narrowed to a named 12-file historical allowlist, all 16 backlog files rewritten to real destinations, guard tests 15→18 (18/18 pass), live guard 0/901, tamper-tested with byte-identical restore. Full record: `coverage-migration.md` §13.9, `validation.md` §7.7. **Awaiting Ripley's re-review before this row returns to `DONE`.** **Re-reviewed and APPROVED by Ripley 2026-09-02** — both blockers verified closed by the reviewer directly (T47 checklist re-read end to end; all twelve allowlist entries audited as closed-phase records; independent whole-disk scan finding 56 files / 39 tracked with **zero** under `specs/_backlog/**`; the three flipped checkboxes verified against real tests by name), guard re-run **0/901**, guard tests **18/18**, and **three** reviewer-run tamper tests (active backlog file, brand-new unlisted `specs/005-*` path, structural `tests/e2e/` revival) each failing correctly and restored byte-identically with `git status` diffed to 0 lines. **T101 is `DONE`; AC-005 is met.** Full verdict, the constitution/PRD package-closure precondition, and findings F-1..F-4: `validation.md` §7.8. |
| **T102** — Migrate valuable coverage to lower reliable layers · AC-006 · **`DONE`** | The suite's assertions were the only stated coverage for export, role enforcement, reference-data admin, and the PWA shell — all unexecuted. Its `seedDevice()` fixture had also drifted from `CreateDeviceRequest` (`src/TechInventory.Api/Controllers/DevicesController.cs` L200–220), proving the fixture layer was unverified too. | Rebuild each T103-valuable assertion at its destination layer. **Real HTTP integration/contract tests** for API behaviour, authorization, export, reference-data mutation, CRUD, serialization — real app + real SQLite, no mocked API, non-2xx failing loudly with status and body. **Component tests** (Vitest + Testing Library + axe-core) for Svelte rendering, state, navigation affordances, accessibility. **Explicit manual validation checklist** — named owner, release cadence, recorded as a gap — for PWA install/offline/browser-engine behaviour. Replace seed-fixture drift risk with **typed HTTP integration/request builders or generated-contract checks** so drift is a compile error. | *Out:* repairing `tests/e2e/fixtures/api.ts` for continued use; changing the API contract to suit tests; presenting manual items as automated coverage. *Precondition:* **T103**. **Delivered 2026-09-02, corrected 2026-09-02 (Ripley B1–B3 review; Bishop fix), corrected again (Ripley second REJECTED verdict, findings B3/B2-R/B4; Hicks fix):** 5 `H-` backend + 20/22 `C-` frontend items done (2 accepted-gap items — **C-18** partial, 5 of 6 route axe harnesses, `/devices` is **G-09**; and **C-20**), the 15-check manual checklist published at [`docs/testing/manual-pwa-validation.md`](../../docs/testing/manual-pwa-validation.md), and a real-HTTP-discovered Viewer authorization defect fixed under explicit approval, then found to also affect the six reference-entity controllers (B1) and device claim/release ownership actions (B3) and closed there too. Ripley's second review found Bishop's B3 fix left a third, independently ungated `canClaim`/`canRelease` copy in `devices/[id]/+page.svelte` (fixed, +3 Vitest cases, test count corrected 645→649), **C-04**/**C-12** evidence overstated (now genuinely closed, all three surfaces), and 26 (not 21) `AdminOrMember`-gated operations undocumented for a `403` OpenAPI response (fixed, `openapi.yaml`/`types.ts` regenerated, a new 26-case parameterized contract test added) — `coverage-migration.md` §12. |
| **T103** — Coverage migration matrix and deletion map · AC-007 · **`DONE`** | 17 spec files (15 `journeys/`, 1 `security/`, 1 outside `testMatch`), no recorded justification per spec, **0 executing**. | Analysis only, and it precedes all deletion. One row per spec — and per test where they differ — recording: test count, behaviour asserted, valuable or not, destination layer (HTTP integration/contract · component · manual checklist · accepted gap), the named replacement, and the owner of any accepted gap. Every removed test must name replacement coverage or an explicitly accepted manual gap. Resolve U-08 (`tests/e2e/theme-fouc.spec.ts`, outside `testMatch`) as a matrix row like any other. Preserve PRD §7.5.4 traceability and surface any needed PRD amendment as an **ADR candidate, not an authorization**. The map is the deletion authority for T101 and the work list for T102. | *Out:* any assumption that browser coverage must be preserved; file deletion (T101); test authoring (T102). *Precondition:* none. **Delivered 2026-09-02 as [`coverage-migration.md`](./coverage-migration.md)** — 17 spec rows, 60 collectable cases reconciled against `--list`, 19 `test.todo` accounted for, U-08 closed, 5 `H-` + 22 `C-` T102 items, a 15-check manual checklist, and 9 owned accepted gaps (**G-09** added post-review for the `/devices` route axe harness the T102 execution did not author — see T102 row below). The earlier stopped run's artefact `e2e-classification.md` is **deleted** and carries nothing forward. |
| **T104** — One authoritative verification interface, Playwright-free · AC-008 · **`DONE` — REJECTED at Apone's independent reviewer gate 2026-09-02 (`validation.md` §10), REVISED by Hicks 2026-09-02 (`validation.md` §11), RE-REVIEWED and APPROVED by Apone at commit `b3c092f` 2026-09-02 (`validation.md` §12); AC-008 met, T105 authorized** | `Taskfile.yml` (305 lines, 27 tasks) is invoked by **zero** workflows; `scripts/verify.sh` is invoked only by the muted `ci.yml` and blocks on Docker at step 9/9 for E2E; the readiness-poll loop is written four times (`evidence.md` §2.2). | Make Task the single entrypoint humans and CI both invoke, covering **format · build · type-check · lint · unit · component · HTTP integration · contract-drift · migration**, with a recorded collected-test floor per surviving suite. Delete the browser stage and the readiness-poll duplication it existed for. Verify must run to completion on a clean checkout **without browser downloads**; state plainly which stages need Docker and whether local verify is partial. Declare platform-forced divergence inline. **The PR #140 author hit exactly this and it is not their error to absorb.** | *Out:* changing what is verified beyond removing the browser stage; rewriting `Taskfile.yml` wholesale. *Precondition:* T101, T102 — the entrypoint must reflect the retired harness and the migrated suites. **Delivered 2026-09-02:** `verify:fast`/`verify:contracts`/`verify:full`/`verify` implemented in `Taskfile.yml`; `scripts/check-test-floors.mjs` adds fail-closed collected-test floors for unit (278 measured / floor 250), integration (296 measured / floor 265), and frontend (649 tests, 83 files measured / floor 580 tests, 74 files) — floor-check zero-collection failure mode proven directly. `dotnet-ef` pinned via `.config/dotnet-tools.json`. `scripts/verify.ps1`/`.sh` rewritten as thin `task verify` wrappers. `.github/workflows/quality-gate.yml` rewritten to a single `verify` job calling `task verify`, closing F-4 (stale-reference guard now runs in the merge-blocking workflow). `.env.e2e`/`docker-compose.e2e.yml` deleted (no real non-browser role; all HTTP integration tests are in-process, no Docker). **No stage of `verify` requires Docker.** **Rejected 2026-09-02 (`validation.md` §10)** on three blockers: B-1 `check:client-drift` compared regenerated-from-working-tree output against the index/HEAD client and failed with provably zero drift; B-2 `check:vulnerable` could not fail (`dotnet list package --vulnerable` exits 0 regardless of advisories) yet was documented "Enforced"; B-3 `ci.yml` depended on PyYAML without installing it. **Revised 2026-09-02 by Hicks:** B-1 closed via `scripts/check-client-drift.mjs` (snapshot/regenerate/compare/always-restore, no index/HEAD dependency, 9/9 new unit tests, live tamper-tested); B-2 closed via `scripts/check-vulnerable.mjs` (parses `--format json`, fails closed on Moderate+ per constitution.md §5.8, 13/13 new unit tests, live-tamper-verified against a real Newtonsoft.Json 12.0.1 probe returning exit 1); B-3 closed by adding an identical PyYAML install step to `ci.yml`. `task verify` (the authoritative alias) now runs end-to-end and **exits 0** on this machine. Full evidence: `validation.md` §11. **Re-reviewed and APPROVED by Apone at `b3c092f` 2026-09-02** — every blocker re-verified by reviewer-run evidence, not from the revision report: a dirty-but-synchronized working tree passes `check:client-drift` while `git diff --exit-code` fails on the identical tree; a genuinely stale client is caught at the exact line and the file restored byte-identically; a generator failure fails closed; **direct and transitive** vulnerable probes each exit 1 where the bare `dotnet list package` exits 0; tool failure fails closed; PyYAML parity holds across the only two workflows invoking `task verify`; and **`task verify` was observed exit 0 end to end in 5m32s with no Docker and no browser**. Checker suites 9/9 and 13/13; floors unchanged by hash from the §10.2 tamper-approved file. **T104 is `DONE`; AC-008 is met; T105 is AUTHORIZED to begin** (`validation.md` §12). **Not claimed:** the constitution/PRD contradiction is not resolved (§7.8.5 condition honored); T105's branch-protection/tamper-testing work is not started; GitHub Actions execution is unobserved until a push — the only run at `b3c092f` is the ops workflow `Sync Squad Labels`. |
| **T105** — Align required checks and tamper-test guards · AC-009 | PR #140 merged with 0 reviews, 2 unchecked DoD boxes, a placeholder CODEOWNERS, no branch protection, and a 284-line unexecuted CI checklist (`evidence.md` §3.6, §5.2, §5.4). | Enumerate the check names that exist after T104 and produce a **written branch-protection recommendation** for `briandenicola` (Aurearia's `strict: true`, force-push and deletion disabled, `enforce_admins: false` is the observed single-operator precedent). **Tamper-test every critical non-browser guard** — stale-Playwright-reference guard, contract-drift gates, migration gate, collected-test floors, verification entrypoint — each with a recorded deliberate-break run. Require recorded acceptance evidence per criterion in `.github/pull_request_template.md`, replacing "All CI checks green" with named checks. Fix or remove `.github/CODEOWNERS`. Resolve `.github/T47-CI-SETUP-CHECKLIST.md`. Record the T102 manual PWA checklist — and declined branch protection, if declined — as explicit visible exceptions under §2.10 with named owners. | *Out:* applying GitHub repository settings from within this package; requiring any browser check. *Precondition:* T104 — check names must exist before they can be required or tampered with, and T104 must clear Apone's re-review first. |

---

## 4. Dependency Sequence

| Task | Depends on | Reason |
| --- | --- | --- |
| T004 | T001–T003 | Principles must be grounded in evidence, not asserted. |
| T103 | — | Analysis. Produces the migration matrix and deletion map that authorize every later step. Nothing blocks it. **`DONE` — [`coverage-migration.md`](./coverage-migration.md).** |
| T101 | T103 | Nothing may be deleted before its behaviour has a matrix row. **`DONE`** — rejected at the reviewer gate (2026-09-02, `validation.md` §7), revised by Apone (`validation.md` §7.7 / `coverage-migration.md` §13.9), and **re-reviewed and APPROVED by Ripley** (2026-09-02, `validation.md` §7.8 / `coverage-migration.md` §13.10). The deletion map is `coverage-migration.md` §4 (waves D1–D8), the reference classification is §5, and the completion record is §13. |
| T102 | T103 | The matrix is the work list of what must be rebuilt, and where. **`DONE`** — `coverage-migration.md` §12 (5 `H-` items done, 20/22 `C-` items done + 2 accepted-gap items [C-18 partial, C-20], 15 `M-` items published, 9 `G-` gaps owned). |
| T104 | T101, T102 | The verification surface must reflect the retired harness and the migrated suites. **T101, T102 and T104 are all `DONE` and reviewer-approved.** T104 was rejected at Apone's independent reviewer gate 2026-09-02 (`validation.md` §10) on B-1/B-2/B-3, **revised by Hicks** closing all three (`validation.md` §11), and **re-reviewed and APPROVED by Apone at commit `b3c092f`** (`validation.md` §12) with `task verify` observed exit 0 end to end — both §7.8.5/§7.8.6 conditions remain honored (contradiction not deepened; F-4 closed; `docker-compose.e2e.yml`/`.env.e2e` deleted with no real non-browser role). |
| T105 | T104 | Required check names must exist before they can be required or tampered with. **AUTHORIZED to begin** — T104 cleared Apone's re-review (`validation.md` §12); `check:client-drift` and `check:vulnerable` changed shape under the T104 revision (`validation.md` §11.1, §11.2) and carry reviewer-run tamper evidence (§12.1–§12.2), but T105 still owns the complete guard matrix. |

**Order: T103 → T101 / T102 (parallel) → T104 → T105.**

---

## 5. Risks and Tradeoffs

| # | Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| R-1 | Retirement removes an assertion whose risk was real but under-described. | Medium | Medium | T103 precedes deletion: every removed test names its replacement layer or a named, owned accepted gap. Reviewable in one table. |
| R-2 | The manual PWA checklist decays into a box nobody ticks — the failure mode this package exists to end. | **High** | High | It is `REVIEWED`, not `ENFORCED`: named owner, release cadence, recorded as an explicit visible exception under §2.10 (T105). It is never reported as automated coverage. |
| R-3 | Migrated HTTP/component tests assert less than the browser test did (e.g. blob download mechanics, real focus order). | Medium | Medium | T103 must mark such residue as an **accepted gap with an owner**, not silently drop it. An honest named gap is the deliverable, not a shortfall. |
| R-4 | Playwright creeps back in via a dependency, a script, or a copied workflow. | Medium | Medium | T101's stale-reference guard, tamper-tested under T105. Retirement is `ENFORCED` only once that guard passes from a clean checkout. |
| R-5 | Branch protection slows single-operator work, or is added while `main` stays unprotected so nothing changes. | High | **High** | `enforce_admins: false` is the observed precedent. T105 makes the posture decision explicit and owned; if declined, it is recorded as an exception (§2.10), not silence. Highest-leverage item in the plan — every other gate is advisory without it. |
| R-6 | New ceremony gets applied uniformly and everyone learns to skip it. | Medium | High | The T0–T3 tiers (§2.4) exist for this. T0 is the request plus green checks; T1 is an issue plus a mini-plan. |
| R-7 | Guards are added but never proved, repeating the "written but not executed" pattern of the T47 checklist. | Medium | High | T105 requires a recorded tamper test per critical guard. A guard with no deliberate-break evidence is not counted. |

**Accepted tradeoffs.** Merge latency increases — PR #140 shows the current
floor is "10 seconds faster than the truth". Automated browser coverage goes to
zero — it was already zero in practice, and an owned manual gap beats a suite
that crashes at load. `evidence.md` U-07 is **closed as moot**: the suite is
retired, not repaired. Governance consolidation is deferred (`brief.md` §3).

---

## 6. ADR Position

No ADR is created here. Candidates for explicit decision rather than default
implementation: **branch protection with required status checks on `main`**
(T105 — changes who may merge, not reversible by code alone); and **Playwright
retirement**, decided by `briandenicola` on 2026-09-02 (`brief.md` §2.1), which
warrants a durable `docs/adr/` record alongside
`docs/adr/0001-record-architecture-decisions.md`. Retirement also implies a
**PRD §7.5.3/§7.5.4 amendment** (those sections require browser journeys, and
§7.5.4 requires an ADR to remove one) — T103 surfaces it, without authorizing it.

### 6.1 Package-closure precondition — recorded under §2.10 (Ripley, 2026-09-02)

**Named exception, scoped, attributed, and dated — not a silent gap.**

- **Rule contradicted:** `.specify/memory/constitution.md` L357–359, L390,
  L402 ("**Playwright** is the required E2E framework — no substitutes"),
  L442, L536; `docs/prd.md` §7.5.2–§7.5.4 (L227–L258). Constitution §0 puts
  both **above** `brief.md`.
- **Scope of the exception:** the working tree has removed the framework these
  documents mandate, and the T101 guard is configured never to report them.
  Held open from T101's completion until the amendment lands.
- **Attributed to:** `briandenicola` (approver of `brief.md` §2.1); surfaced
  and held by Ripley at the T101 reviewer gate (`validation.md` §7.3.3) and
  re-affirmed at the T101 re-review (`validation.md` §7.8.5).
- **Not a T101 blocker:** neither document appears in AC-005's enumerated
  surface list, so they are *out of scope*, not *exceptions to zero*.
- **Closure trigger — mandatory before this work package closes:** an ADR in
  `docs/adr/` plus a constitution §6.5.7/§7/DoD and PRD §7.5.2–§7.5.4
  amendment. Tracked as a T105 / package-closure item.
- **Binding on T104:** T104 must not deepen the contradiction — no claim that
  its verification surface satisfies constitution §9/§6.5.14/L442's
  "Playwright smoke", and this exception must be carried forward visibly
  rather than inherited in silence.


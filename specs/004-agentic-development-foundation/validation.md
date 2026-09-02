---
id: 004-agentic-development-foundation
document: validation
status: T001–T004 evidence recorded; T103 evidence recorded; T102 evidence recorded and INDEPENDENTLY VERIFIED — APPROVED; T101 APPROVED at re-review (§7.8); T104 DONE — APPROVED at Apone's re-review (§12) after REJECTION (§10) and Hicks's revision (§11); AC-008 met; T105 AUTHORIZED to begin
last_updated: 2026-09-02 (T104 re-reviewed and APPROVED by Apone at `b3c092f` — B-1/B-2/B-3 verified closed by reviewer-run evidence, full `task verify` observed exit 0, T105 authorized — §12)
base_sha: d303cd6537392e2489222d5a0d5c946f39f2af0c
---

# Validation — Agentic Development Foundation

Criteria are stated once in [`brief.md`](./brief.md) §5 and abbreviated here.
Two claims are tracked and **must not be conflated**: **EVIDENCE RECORDED** — an
artefact exists demonstrating the criterion is met (for research tasks it *is*
the deliverable) — and **GATE PASSED** — a machine control exists, ran, and
blocked or allowed correctly.

**No gate in this work package is merge-required on GitHub.** The T104 checks
exist, run, and fail closed — reviewer-proven at commit `b3c092f`
(§12) — but no row claims a *GitHub-required* GATE PASSED, and none may until
T105 enumerates and requires them and the state is observed. This is the
lesson of the PR #140 chain (`evidence.md` §5): green checks were recorded as
completion evidence for a criterion no check covered.

---

## 1. Acceptance-Criterion Matrix

| AC | Criterion (abbrev.) | Task | Status | Evidence recorded | Gate passed |
| --- | --- | --- | --- | --- | --- |
| **AC-001** | Four-repo evidence matrix at pinned SHAs, cited, unknowns marked | T001 | ✅ `DONE` | `evidence.md` §1, §2, §4, §6, §7 | n/a — research task |
| **AC-002** | Authority-source inventory with class/freshness/recommendation and accurate volume | T002 | ✅ `DONE` | `evidence.md` §3 | n/a — research task |
| **AC-003** | PR #140 / #89 chain with every non-blocking control named | T003 | ✅ `DONE` | `evidence.md` §5 (timeline §5.1; 14 controls §5.4) | n/a — research task |
| **AC-004** | First principles and scaled T0–T3 work model recorded | T004 | ✅ `DONE` | `plan.md` §2.1–§2.10 | n/a — record task |
| **AC-005** | Playwright harness retired safely: `tests/e2e/**` gone as an executable contract, behaviour inventoried first, **zero** references in manifests/scripts/workflows/docs/PR template, stale-reference guard added, #89 closed citing retirement + migration | T101 | ✅ `DONE` — **rejected then revised then independently re-reviewed and APPROVED** (Ripley, 2026-09-02, §7.8). Rejected at the first gate (§7, B1 stale `task test:e2e` instructions in `.github/T47-CI-SETUP-CHECKLIST.md`, B2 blanket `specs/` guard exemption hiding backlog Playwright promises); revised by Apone (§7.7); both blockers verified closed by the reviewer directly, not from the revision summary (§7.8) | `tests/e2e/**` (28 tracked files), `scripts/run-e2e.ps1`/`.sh`, `.squad/skills/playwright-e2e-scaffolding/` deleted — cross-checked file-by-file against `coverage-migration.md` §4 waves D1–D5/D8 before deletion, nothing removed outside the matrix. Dependencies: only `tests/e2e/package.json` (deleted) ever declared Playwright; root/`src/TechInventory.Web/package.json` never did; `pnpm-lock.yaml`'s sole remaining Playwright-named string is `@vitest/browser-playwright`, vitest's own optional peer dependency, never installed (§5.6). Clean-install proof: `Remove-Item -Recurse -Force node_modules; pnpm install --frozen-lockfile` in `src/TechInventory.Web` — 617 packages resolved, **0 downloaded**, no Playwright package present, `%LOCALAPPDATA%\ms-playwright` browser-cache mtime unchanged (dated 2026-08-24, pre-dates this work). `Taskfile.yml`/`scripts/verify.ps1`/`verify.sh`/`.github/workflows/ci.yml` — every invocation removed, replaced by `check:stale-refs`. Stale-reference guard: `scripts/check-stale-playwright-references.mjs` (keyword scan + two structural hard-fails: any `tests/e2e/` path, any `playwright.config.*`), wired into `Taskfile.yml` and both `verify` scripts; repository-native coverage `scripts/check-stale-playwright-references.test.mjs`, **15/15 `node:test` cases pass** (not tamper-tested — that is T105's job, not claimed here). Live guard run: **`Stale-reference guard passed: 0 active Playwright references across 901 tracked file(s).`** Issue #89: closed by `briandenicola` prior to this session (state reason `NOT_PLANNED`, no evidence comment yet); durable retirement+migration evidence comment added at closing time — https://github.com/briandenicola/tech-inventory/issues/89#issuecomment-5515917506. **Two exceptions recorded — and reframed by the reviewer at the re-review (§7.8):** `.specify/memory/constitution.md` and `docs/prd.md` still name Playwright as mandatory. They are **outside AC-005's enumerated surface list** (`package.json`, `pnpm-lock.yaml`, `Taskfile.yml`, `scripts/**`, `.github/workflows/**`, `docs/testing.md`, PR template) — *out of scope*, not *exceptions to zero*. Amending those normative clauses requires a separate ADR, which §5.4 requires this task to *surface, not perform*. Recorded as a **mandatory package-closure precondition** (T105 / ADR), not a T101 gap. **Judgment call flagged for independent review, not a silent decision:** `.squad/log/**` and `.squad/orchestration-log/**` contain dated historical session logs not individually enumerated in §5.5's RETAIN list; they were exempted from the guard on the same historical-record principle already applied to `.squad/session-log.md`/`.squad/agents/*/history.md` (already gitignored for future writes, narrative past tense, dated filenames) — reasoning recorded in the guard's own header comment for Ripley/`briandenicola` to confirm or override. Full file-by-file diff and reference classification in `coverage-migration.md` §13 (T101 completion record) | ✅ `Stale-reference guard passed: 0 active Playwright references across 901 tracked file(s)`; guard test suite 15/15; clean install 0 downloads; `dotnet build -c Release` 0 errors; `dotnet test -c Release` 278 unit + 292 integration passed (4 pre-existing skips, unrelated); `pnpm run lint` clean; `pnpm test -- --run` 83 files / 649 tests passed, incl. `DeviceForm.test.ts` 27/27 (the previously-stale-skip tests are genuinely green). **Re-review re-run by Ripley 2026-09-02 (§7.8), not accepted from the revision report:** live guard `0 active Playwright references across 901 tracked file(s)`; guard tests **18/18**; three independent tamper tests (active `specs/_backlog/**` file, a brand-new unlisted `specs/005-*` path, structural `tests/e2e/` revival) each exit 1 with the exact file:line, all restored **byte-identically** (`git hash-object` match) and `git status --porcelain` diffed to **0 lines** against the pre-review baseline |
| **AC-006** | Valuable coverage live at lower layers — real HTTP integration/contract, component tests, and an owned manual checklist for browser-only behaviour; seed-fixture drift replaced by typed builders/generated-contract checks, `fixtures/api.ts` **not** repaired for continued use | T102 | ✅ `DONE` — **independently verified, APPROVED** (§6) | `coverage-migration.md` §12 (5 `H-` done — including the B1-expanded `ViewerRoleAuthorizationTests` and B4's 26-case `OpenApiDriftTests.AdminOrMemberGatedOperation_DeclaresForbiddenResponse`; 20/22 `C-` done, **C-04**/**C-12** genuinely closed only after Hicks's third-surface fix — 649/649 Vitest tests, corrected from 645/645 — + C-18 partial (5/6 route axe harnesses; `/devices` is accepted gap **G-09**) + C-20 accepted gap, 15-check manual checklist published), `docs/testing/manual-pwa-validation.md` (`t102-backend-results.md`, `t102-frontend-results.md`, and `viewer-auth-fix-results.md` are session artefacts from the original T102 execution, not present in this repository — the durable evidence is the test names and `coverage-migration.md` §12 sections cited here) | n/a — manual checklist is `REVIEWED`, not a gate; HTTP/component suites re-run green after the B1/B3 fixes and again after Hicks's B3/B2-R/B4 fixes — durable evidence is the test names and files cited in `coverage-migration.md` §12, not the session artefacts alone. **Reviewer re-ran every cited command directly (§6) — no reviewer claim rests on an executing agent's summary.** |
| **AC-007** | Coverage migration matrix and deletion map covering **every** existing Playwright spec — test count, behaviour, destination layer, named replacement or accepted-gap owner; no spec deleted without a row; **precedes** T101/T102 deletion | T103 | ✅ `DONE` | `coverage-migration.md` §2 (inventory: 28 tracked files, 17 specs, counts reconciled against `--list`), §3 (17 spec rows / per-test rows), §4 (deletion waves D1–D8), §5 (110 referencing files classified), §6 (5 `H-` + 22 `C-`), §7 (15 `M-`), §8 (8 `G-`), §9 (PRD §7.5.4 traceability), §10 (retirement completion definition) | n/a — analysis task, no gate |
| **AC-008** | One authoritative Task/local/CI verification surface with **no Playwright dependency**, covering format · build · type-check · lint · unit · component · HTTP integration · contract-drift · migration | T104 | ✅ `DONE` — **rejected (§10), revised by Hicks (§11), independently re-reviewed and APPROVED by Apone at `b3c092f` (§12)**. All three blockers verified closed by reviewer-run evidence, not from the revision report: **B-1** `check:client-drift` proven to pass a dirty-but-synchronized tree that the retired `git diff --exit-code` comparison fails, to fail a genuinely stale client, and to restore byte-identically on pass, on drift, and on generator failure; **B-2** `check:vulnerable` proven to exit 1 on real **direct** *and* **transitive** vulnerable probes that the bare `dotnet list package` command exits 0 on, and to fail closed on tool failure; **B-3** `ci.yml` and `quality-gate.yml` are the only two workflows invoking `task verify`, and both install PyYAML identically | `Taskfile.yml` (`check:client-drift`, `check:vulnerable` targets rewritten), `scripts/check-client-drift.mjs` + `.test.mjs`, `scripts/check-vulnerable.mjs` + `.test.mjs`, `.github/workflows/ci.yml` (PyYAML install step), `.github/workflows/README.md` — see §11 (revision) and §12 (independent re-review) | 🟡 **Fail-closed and observed locally; NOT yet a merge-required GitHub check** (that is AC-009/T105). Reviewer-run at `b3c092f`, Windows, no Docker, no browser (§12.4):** `task verify:fast` **exit 0** (unit 278/278, frontend 649 tests / 83 files); `task verify:contracts` **exit 0** (stale-refs 0/**933** + guard suite 18/18, OpenAPI drift current, `check:client-drift` exit 0, migration drift clean, integration **296 collected**); **`task verify` — the authoritative alias — ran end to end and exited 0 in 5m32s**, including the production frontend build and `check:vulnerable`. Checker unit suites re-run by the reviewer: **9/9** client-drift, **13/13** vulnerable. Floors unchanged by hash (`fe45ebb…`) from the §10.2 tamper-approved file. **GitHub Actions execution still NOT observed** — the only run at `b3c092f` is the ops workflow `Sync Squad Labels`; Quality Gate has not run and is not claimed to have passed |
| **AC-009** | Required checks and protection posture aligned to the checks that exist after T104; every critical non-browser guard tamper-tested | T105 | 🟢 `APPROVED` / **not started — now AUTHORIZED to begin**. T104 is `DONE` and AC-008 is met (Apone re-review, §12), so the check names T105 must enumerate, require, and tamper-test now exist and have been observed running | Floor-mechanism tamper evidence pre-recorded by the reviewer in §10.2 (4 cases, both mechanisms) and re-confirmed unchanged by hash in §12.5; the two rewritten checks (`check:client-drift`, `check:vulnerable`) carry reviewer-run tamper evidence in §12.1–§12.2 — T105 still owns the **full** guard matrix, plus findings F-5, F-10–F-13 (§12.7) | **NOT IMPLEMENTED** |
| **AC-010** | No claim asserts a gate passed before it existed | all | ✅ `HOLDING` | This document, §3 | n/a — a discipline |

---

## 2. Baseline at `d303cd6`

Measurable starting point. Sources: `evidence.md` §2.1, §2.3, §2.9, §3.1, §5.3.

| Control | Exists? | Runs where | Class today | Target |
| --- | --- | --- | --- | --- |
| `dotnet format` / `build` / `test` / `list package --vulnerable` | Yes | Quality Gate `dotnet` | ADVISORY | ENFORCED (T105) |
| OpenAPI spec-vs-code drift; generated-client-vs-spec drift | Yes | Quality Gate `dotnet` / `web` | ADVISORY | ENFORCED + tamper-tested (T105) |
| `pnpm run check` / `lint` / `test` / `build` | Yes | Quality Gate `web` | ADVISORY | ENFORCED (T105) |
| CodeQL · gitleaks · Trivy config scan | Yes | Quality Gate | ADVISORY | CodeQL ADVISORY; others ENFORCED (T105) |
| **Playwright browser tests** | Yes, but **cannot collect** | `ci.yml` — `workflow_dispatch` only | ADVISORY | **Retired** (`brief.md` §2.1) — mapped by T103, deleted by T101, coverage migrated by T102. **No merge, scheduled, release, or optional automated role remains** |
| **E2E seed fixtures vs. API contract** | **Drifted** — no `ownerId` / `locationId` | — | — | **Replaced** — H-01's typed request builders make a missing field a compile error (T102 done); `tests/e2e/fixtures/api.ts` itself awaits deletion under T101 |
| **Stale-Playwright-reference guard** | **No** | — | — | ENFORCED (T101), tamper-tested (T105) |
| **Manual PWA validation checklist** | **Yes — published** | `docs/testing/manual-pwa-validation.md` | REVIEWED | Named owner `briandenicola`, release cadence, not merge-blocking (T102 done); exception recorded if skipped (T105) |
| **Collected-test floor per surviving suite** | **No** | — | — | ENFORCED (T104) |
| **Single verification entrypoint** | **No** — Taskfile invoked by 0 workflows | — | — | Single surface (T104) |
| CODEOWNERS review routing · PR-template Definition of Done | Exist; CODEOWNERS **non-functional** (`@your-github-handle`) | GitHub / human | ADVISORY | REVIEWED (T105) |
| **Branch protection / required checks** | **No** — `404 Branch not protected`, `rulesets: []` | — | — | Decision required (T105) |

**Baseline: 0 ENFORCED, 0 REVIEWED, 13 ADVISORY, 5 absent entirely.**

Key measurements carried forward: instruction corpus 254 files / 30,184 lines
(37.4% historical, constitution 1.9%); E2E 17 spec files, 6 projects, **19
`test.todo` across 4 files**, **0 of 22 F045 tests executed**; 8 open unknowns.

---

## 3. Explicit Non-Claims

1. **No gate is merge-required on GitHub, and no GitHub Actions verification
   run has been observed.** T001–T004 produced documents; T104's checks are
   implemented, run locally, and fail closed (reviewer-proven, §12), but
   Quality Gate has not executed at `b3c092f` — the only run on the branch is
   the ops workflow `Sync Squad Labels` — and requiring the checks is T105's
   work.
2. **The E2E suite has not been observed passing, and never will be.** Issue #89
   remains `OPEN` and Docker was unavailable here. Under `brief.md` §2.1 the
   suite is **retired rather than repaired**, so the question has no subject.
3. **Branch-protection state at the PR #140 merge instant is unknown**
   (`evidence.md` U-01/U-02); no assertion is made that a required check was
   bypassed.
4. **No repository setting, issue, PR, workflow, script, test, or source file
   has been modified.** T105 *recommends* branch protection; the user applies it.
5. **Aurearia's instruction volume is under-counted** (`evidence.md` U-03).
   Ordering is unaffected; the figure is a floor.
6. **The correction in `brief.md` §2 is a claim about this evidence set only** —
   sufficient to withdraw the overstatement that reliability *requires* broad
   Playwright E2E, not to conclude browser tests are unnecessary in general.
7. **`e2e-classification.md` has been deleted and carries nothing forward.**
   It was the artefact of a T103 run dispatched against the *pre-retirement*
   brief and stopped mid-flight; it never counted as started, was never T101's
   deletion authority or T102's work list, and any retained, scheduled, release
   or optional automated browser suite it proposed **contradicted `brief.md`
   §2.1 and was void**. T103 was redone from a fresh reading of the working tree
   at `d303cd6`; AC-007 is satisfied by
   [`coverage-migration.md`](./coverage-migration.md) alone, and none of the
   deleted file's conclusions or identifiers is adopted.
8. **No document in this package proposes a future automated Playwright role.**
   `evidence.md` records historical Playwright facts; those are history, not
   target strategy (`plan.md` §2.9).
9. **The Viewer authorization fix (`AuthorizationPolicies.AdminOrMember` on
   `DevicesController`/`ImportsController`) is recorded as T102 evidence, not
   scope drift.** Real-HTTP replacement testing (H-04) found a genuine
   constitution §5.2 violation the retired, never-collecting Playwright suite
   had never verified either. `briandenicola` explicitly authorized the fix
   before it was made; it stayed inside the authorization policy H-04 was
   already proving and touched no unrelated controller (durable evidence:
   `ViewerRoleAuthorizationTests`, `coverage-migration.md` §12.1;
   `viewer-auth-fix-results.md` is a session artefact, not present in this
   repository).

---

## 4. Open Unknowns Carried Forward

| ID | Unknown | Resolved by |
| --- | --- | --- |
| U-01 | Branch protection state at `2026-09-02T14:27:09Z` | Likely unresolvable; T105 makes future state observable |
| U-02 | Whether Quality Gate was a *required* check at merge time | Same as U-01 |
| U-03 | Aurearia `.squad/log/` + `orchestration-log/` volume | Re-measure on a non-Windows host if needed |
| U-04 | `.squad/templates/` reachability (74 files, 8,976 L) | Deferred governance package (`brief.md` §3) |
| U-05 | Whether `docs/known-issues.md` records issue #89 | Deferred governance package |
| U-06 | drinks-and-desserts `docker-publish.yml` / `security-scan.yml` triggers | Re-read if that comparison becomes load-bearing |
| U-07 | Whether the E2E suite passes once #89's collection crash and fixture drift are fixed | ✅ **CLOSED AS MOOT by T101** — the suite is retired, not repaired (`brief.md` §2.1); `tests/e2e/` no longer exists as an executable contract, so "does it pass once fixed?" has no remaining subject. Recorded here rather than silently dropped |
| U-08 | Why `tests/e2e/theme-fouc.spec.ts` sits outside `testMatch` | ✅ **RESOLVED by T103** — `coverage-migration.md` §3.17. `playwright.config.ts` `testMatch` admits only `journeys/**` and `security/**`, so the root-level spec **collects 0 tests** (verified: `--list --project=chromium-desktop theme-fouc` → `Total: 0 tests in 0 files`). No reason for the placement is recorded anywhere in the repository, and the spec is independently drifted — it seeds `ti.userPrefs.v1.<id>` while `src/app.html` reads `theme-preference` |

---

## 5. Evidence Ledger

One row per acceptance criterion as it is satisfied. AC-008 and
AC-009 stay empty until that work exists **and has been observed running**;
AC-005 evidence was recorded 2026-09-02, rejected at the reviewer gate, revised,
and **approved at re-review the same day (§7.8)**;
AC-006 evidence was recorded 2026-09-02 (below); AC-007 is an analysis task
whose deliverable *is* its evidence.

| Date | AC | Task | Artefact | Type | Recorded by |
| --- | --- | --- | --- | --- | --- |
| 2026-09-02 | AC-001 | T001 | `evidence.md` §1, §2, §4, §6, §7 — four repos at pinned SHAs, nine dimensions, 8 unknowns marked | EVIDENCE RECORDED | Ripley |
| 2026-09-02 | AC-002 | T002 | `evidence.md` §3 — 28 authority sources, volume quantified, 6 duplication pairs | EVIDENCE RECORDED | Ripley |
| 2026-09-02 | AC-003 | T003 | `evidence.md` §5 — timestamped chain, 14 controls named with reasons | EVIDENCE RECORDED | Ripley |
| 2026-09-02 | AC-004 | T004 | `plan.md` §2.1–§2.10 — ten principles, each tied to cited evidence | EVIDENCE RECORDED | Ripley |
| 2026-09-02 | AC-005 | T101 | `scripts/check-stale-playwright-references.mjs` + `.test.mjs` (15/15 passing), guard live run `0 active Playwright references across 901 tracked file(s)`, clean `pnpm install --frozen-lockfile` with 0 downloads, `coverage-migration.md` §13 (T101 completion record), issue #89 evidence comment | EVIDENCE RECORDED, then **REJECTED at the independent reviewer gate 2026-09-02 (§7)** — deletions, guard fail-closed behaviour (all three branches tamper-tested by the reviewer), clean install and #89 verified good; **B1** `.github/T47-CI-SETUP-CHECKLIST.md` L158/L185–192/L244 still instruct `task test:e2e` / `task test:e2e:run`, removed from `Taskfile.yml`; **B2** the guard's blanket `specs/` exemption hides ten unchecked `specs/_backlog/**` promises to author new Playwright tests | Hudson; **rejected by Ripley**; revision assigned to Apone |
| 2026-09-02 | AC-005 | T101 | **Revision + re-review.** Apone closed B1 (`.github/T47-CI-SETUP-CHECKLIST.md` stale `task test:e2e`/`:run` instructions and readiness-troubleshooting block removed, `verify` descriptions corrected, no pre-claim of T104) and B2 (blanket `specs/` guard exemption replaced by a named twelve-file `EXEMPT_SPEC_PATHS` allowlist; all 16 `specs/_backlog/**` Playwright promises rewritten to real lower-layer/manual/accepted-gap destinations — `coverage-migration.md` §5.5a, §13.9; `validation.md` §7.7). **Reviewer re-executed everything (§7.8):** guard **0/901**; guard tests **18/18**; three tamper tests (active `specs/_backlog/**` file, brand-new unlisted `specs/005-*` path, structural `tests/e2e/` revival) each exit 1 at the exact file:line and restored byte-identically, `git status --porcelain` diffed to 0 lines against the pre-review baseline; whole-disk independent scan 56 files / 39 tracked, **zero** under `specs/_backlog/**` and zero in any manifest, script, workflow, config, instruction or executable test; the three flipped backlog checkboxes verified against real test bodies by name | **GATE PASSED — APPROVED (§7.8).** T101 `DONE`, AC-005 met, T104 authorized to begin subject to §7.8.5/§7.8.6 conditions | Apone (revision); **approved by Ripley** |
| 2026-09-02 | AC-006 | T102 | `coverage-migration.md` §12 — 5 `H-` backend tests done (integration suite **292 passed / 4 skipped / 296 total** as re-verified by Ripley on the final revision; the "240/245" figure recorded earlier in this row's history was the count immediately after the first Viewer authorization fix and is superseded); 20/22 `C-` frontend items done, C-18 partial (5/6 route axe harnesses; `/devices` recorded as accepted gap **G-09**, not previously disclosed — corrected per Ripley's B2 finding), C-20 an accepted, rationale-backed gap; 15-check manual PWA checklist published at `docs/testing/manual-pwa-validation.md`, owner `briandenicola`, `REVIEWED`; seed-fixture drift replaced by typed request builders (H-01); Viewer-mutation gap on Brands/Categories/Locations/Networks/Tags/Owners closed and the permanently-skipped `AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden` removed (Ripley's B1 finding) | EVIDENCE RECORDED, then CORRECTED 2026-09-02, then **INDEPENDENTLY VERIFIED 2026-09-02 (§6)** | Apone; corrected by Bishop, then Hicks; verified by Ripley |
| 2026-09-02 | AC-007 | T103 | `coverage-migration.md` §1–§11 — 17 specs and 11 support files given a disposition; 52 `test(` + 19 `test.todo` + 1 `test.fixme` reconciled against `--list` (60 collectable × 6 projects = 360; full run exits 1 with 0 collected); deletion waves D1–D8; 110 referencing files classified; 5 `H-` + 22 `C-` T102 items; 15-check manual checklist; 9 owned accepted gaps (**G-09** added post-review for the `/devices` route axe harness — see AC-006); U-08 closed | EVIDENCE RECORDED | Apone |
| 2026-09-02 | AC-008 | T104 | `Taskfile.yml` `verify:fast`/`verify:contracts`/`verify:full`/`verify`; `scripts/check-test-floors.mjs` (unit/integration/frontend collected-test floors, fail-closed proven with a deliberate zero-collection filter); `.config/dotnet-tools.json` pinning `dotnet-ef` 10.0.11; `scripts/verify.ps1`/`.sh` rewritten as thin `task verify` wrappers; `.github/workflows/quality-gate.yml` rewritten to a single `verify` job calling `task verify` (closes F-4); `.github/workflows/ci.yml` updated to install Task and updated header comment; `.github/workflows/README.md` rewritten; `.env.e2e`/`docker-compose.e2e.yml` deleted (no real non-browser role — confirmed via grep, all HTTP integration tests use in-process `WebApplicationFactory<Program>`); `scripts/check-openapi-drift.sh` deleted (logic now lives only in `Taskfile.yml`, avoiding duplication); guard exemption list and its test file updated to match; `.gitleaks.toml` dead allowlist entry removed; stale `docker-compose.yml` comment fixed — full detail in §9 | EVIDENCE RECORDED — **implementation complete, awaiting independent review; not self-marked `DONE`** (`plan.md` §2.3) | Hudson |
| — | AC-009 | T105 | — | pending | — |
| 2026-09-02 | AC-010 | all | This document — §1 separates *evidence recorded* from *gate passed*; §3 lists eight explicit non-claims | HOLDING | Ripley |

---

## 6. T102 Final Independent Reviewer Gate — **APPROVED**

**Reviewer:** Ripley (Lead / Architect). **Date:** 2026-09-02.
**Branch:** `chore/agentic-development-foundation`. **Verdict:** **APPROVED.**
**Revision under review:** Hicks's third-cycle fix for findings B3 / B2-R / B4.

This is a **reviewer verification record**, not a gate. No gate exists in this
work package (§3.1 still holds). Every figure below was produced by the
reviewer running the command directly on the working tree; **no line of it is
carried over from an executing agent's summary.**

### 6.1 Prior blockers — disposition

| Blocker | Verdict | Direct evidence the reviewer produced |
| --- | --- | --- |
| **B3** — Viewer claim/release must be absent on the direct `/devices/[id]` route while Admin and Member retain them; direct-route tests must prove all roles | **CLOSED** | `src/TechInventory.Web/src/routes/(authenticated)/devices/[id]/+page.svelte` L75/L77 now derive `canClaim`/`canRelease` from `canEdit &&` (Admin/Member). `page.test.ts` proves three roles on the direct route: Viewer-owner sees no actions menu at all, Admin-owner is offered Release Ownership, Member-non-owner is offered Claim Ownership. A repository-wide `canClaim|canRelease` search found exactly three derivation sites — `+page.svelte`, `DeviceDetailModal.svelte` L64–65, `deviceRowActions.svelte.ts` L70–83 — all three role-gated, all three role-tested; `DevicePwaRow.svelte` consumes the gated composable. No fourth copy exists. |
| **B2-R** — C-04/C-12 and all counts/citations must be truthful and durable; session-only artefacts must not be cited as repository evidence | **CLOSED** | Reviewer-run counts match the recorded claims exactly: frontend `pnpm run test -- --run` → **83 files / 649 tests passed**; backend `dotnet test -c Release` → Unit **278/278**, Integration **292 passed / 4 skipped / 296 total, 0 failed**; `dotnet format --verify-no-changes` clean; `pnpm run check` **0 errors, 0 warnings**; `pnpm run lint` clean; `pnpm run build` succeeds (**90 precache entries**). Every session artefact cited (`t102-backend-results.md`, `t102-frontend-results.md`, `viewer-auth-fix-results.md`, `t102-bishop-revision.md`, `t102-hicks-final-revision.md`) is labelled at each citation as not present in this repository, with durable test names/paths named alongside. Two stale historical figures (integration `240/245`, frontend `645`) have been marked superseded in this document; `coverage-migration.md` §12 carries the same correction. |
| **B4** — every `AdminOrMember`-gated mutation must document HTTP 403; verify the real operation count | **CLOSED** | Reviewer enumerated `[Authorize(Policy = AuthorizationPolicies.AdminOrMember)]` across `src/TechInventory.Api/Controllers/**` independently: **26** action-level usages — Brands/Categories/Locations/Networks/Tags/Owners Create/Update/Delete (18), `DevicesController` Create/Update/Delete/AddTag/RemoveTag/ClaimOwnership/BulkUpdateDevices (7), `ImportsController.CommitImport` (1). Neither 21 nor any other figure; **26 is correct**. All 26 carry `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]`. The `merge` endpoints on the reference controllers are `[Authorize(Roles = "Admin")]` and the `bulk/delete` endpoints are `AuthorizationPolicies.Admin`, so no ungated Viewer mutation remains on those controllers. |
| **B4 (contract)** — `openapi.yaml` and the generated TypeScript client must be synchronized, with a parameterized regression test covering the entire gated set | **CLOSED** | Reviewer parsed committed `openapi.yaml` and confirmed **26 of 26** gated operations declare a `403` response. Structural diff of the committed spec against its `HEAD` version: **68 operations before and after, 83 schemas before and after, 0 operations or schemas added or removed, and exactly 26 response-code changes — every one an added `403`.** The 8,166→5,476 line change is formatting from canonical regeneration, not content loss. Re-running `pnpm run generate:client` produced **no further diff**, so `types.ts` is in sync with the spec. `OpenApiDriftTests.AdminOrMemberGatedOperation_DeclaresForbiddenResponse` has exactly 26 `[InlineData]` cases matching the enumerated set one-for-one. |
| **B4 (guard is real)** — the new contract test must actually fail on regression | **CLOSED — tamper-tested** | The reviewer deliberately renamed the `403` response key to `499` on `POST /api/v1/brands` in the committed spec and re-ran the theory: **1 of 26 failed** with the precise message naming the operation and policy, 25 passed. `openapi.yaml` was then restored and hash-verified byte-identical, and `OpenApiDriftTests` re-run green (**36 passed, 1 pre-existing skip**). This is the only tamper test recorded in this work package so far; T105 still owes the rest. |
| **Doc alignment** — `brief.md`, `plan.md`, `tasks.md`, `validation.md`, `coverage-migration.md` must agree without overclaiming route-level axe or browser behaviour | **CLOSED** | All five documents record C-18 as **5 of 6** route axe harnesses with `/devices` as accepted gap **G-09** (owner `briandenicola`, compensating controls named in `coverage-migration.md` §8), C-20 as an accepted gap, and the manual checklist as `REVIEWED` and non-blocking. No document claims browser-engine, service-worker, install, or first-paint behaviour is automated; those are M-01–M-15 and G-01–G-09. The axe harnesses are correctly described as jsdom component tests, not browser runs. |

### 6.2 Consequence

**T102 is genuinely complete. AC-006 is satisfied.** The T101 precondition
chain (`plan.md` §3: T103 → T101/T102) is cleared on both sides: T103 gives
the deletion authority, T102 has the replacement coverage live and verified.
**T101 is authorized to begin.**

### 6.3 Non-blocking follow-ups for T104 / T105 — not conditions of this approval

1. `AdminOrMemberGatedOperation_DeclaresForbiddenResponse` pins the **current**
   26 operations via hard-coded `[InlineData]`. A *newly added* gated mutation
   that forgets its `403` would not be caught. T105 should replace the literal
   list with reflection over `[Authorize(Policy = AuthorizationPolicies.AdminOrMember)]`
   so the assertion set cannot fall behind the controllers.
2. `docs/testing/manual-pwa-validation.md` closes with "the accepted-gaps
   register (`G-01`–`G-08`)"; the register now runs to **G-09**. Cosmetic
   staleness in a T102 deliverable, not an overclaim — fold into T104/T105.
3. `devices/+page.svelte`'s bulk-action buttons render regardless of role
   (backend correctly enforces `AdminOrMember`/`Admin`). A UI-affordance
   inconsistency, not a security hole; Hicks disclosed it rather than hiding
   it. Track it as backlog, not as T101 scope.

---

## 7. T101 Independent Reviewer Gate — **REJECTED**, then revised, then **APPROVED at re-review (§7.8)**

> **Outcome pointer (added 2026-09-02).** §7.1–§7.6 are the *first* gate and
> are preserved verbatim as the historical record; they are not retracted or
> amended. §7.7 is Apone's revision record. **§7.8 is the binding current
> verdict: APPROVED — T101 is `DONE`, AC-005 is met, and T104 is authorized to
> begin.** Read §7.8 for the current state; read §7.1–§7.7 for how it got there.

**Reviewer:** Ripley (Lead / Architect) · **Date:** 2026-09-02 ·
**Branch:** `chore/agentic-development-foundation` (uncommitted working tree)
**Author under review:** Hudson · **Verdict at this first gate: REJECTED — 2
blockers.** **T104 was NOT authorized to begin** *(superseded by §7.8)*.
T101's self-recorded `DONE` does not stand *(and is not restored by §7.8 —
the approval rests on Apone's revision plus the reviewer's own re-execution)*.

### 7.1 What was independently verified and holds

Re-run and re-observed by the reviewer, not accepted from the T101 report:

| Check | Observation |
| --- | --- |
| Deletion authority | `git diff --diff-filter=D` → **31 files**: the 28 `tests/e2e/**` files, `scripts/run-e2e.ps1`, `scripts/run-e2e.sh`, `.squad/skills/playwright-e2e-scaffolding/SKILL.md`. Every one has a `coverage-migration.md` §4 (D1–D5) or §5.1 row. **Nothing was deleted without authority, and nothing authorized was left behind.** |
| No valuable loss | `docs/testing/manual-pwa-validation.md` is present with a named owner (`briandenicola`), a `REVIEWED` class, and a release cadence. The deleted skill was a recipe for standing the harness back up (§5.1) — correctly removed. |
| Harness gone | No `playwright.config.*` anywhere; no `tests/e2e/` tree; no `@playwright/*` in any `package.json`; no `@playwright`/`playwright` package in `node_modules`; no browser-install step in any workflow; `Taskfile.yml` no longer defines `test:e2e` or `test:e2e:run` (`task --list` confirms). |
| Lockfile | `src/TechInventory.Web/pnpm-lock.yaml` L3461/L3475 are the only Playwright-named strings: `@vitest/browser-playwright`, vitest's own **optional peer** (§5.6). Understood, unavoidable, no download path. |
| Guard — live run | `node scripts/check-stale-playwright-references.mjs` → exit 0, `0 active Playwright references across 901 tracked file(s)`. |
| Guard — tests | `node --test scripts/check-stale-playwright-references.test.mjs` → **15/15 pass**. |
| Guard — tamper test (keyword branch) | Appended `` Run `npx playwright test` before merging. `` to `docs/testing.md`. Guard → **exit 1**, `docs/testing.md:390`. Restored from backup; `git hash-object` **`f13c199c0220de0d8399b44ae4c4a5555f633ff5` before and after — byte-identical**. Guard back to 0/901. |
| Guard — tamper test (structural, config branch) | Created `playwright.config.ts` at root with **no** keyword in its content, `git add -N`. Guard → **exit 1**, `playwright.config.ts:1 path lies under the retired tests/e2e/ tree or is a Playwright config file`. Index reset, file removed. |
| Guard — tamper test (structural, tree branch) | Created `tests/e2e/probe.ts` containing only `export const revived = 1;`, `git add -N`. Guard → **exit 1**, `tests/e2e/probe.ts:1`. Index reset, file and directory removed. |
| Working tree after tamper tests | `git status --short` → **120 entries, identical to the pre-review state**. No reviewer residue. |
| Frontend lint | `pnpm run lint` → clean. |
| §13.4 known-issues claim | `pnpm run test -- --run src/lib/components/DeviceForm.test.ts` → **27/27 pass**. The `t23` correction is real, not asserted. |
| Issue #89 | `state=CLOSED`, `stateReason=NOT_PLANNED`, `closedAt=2026-09-02T15:55:30Z`; evidence comment `2026-09-02T20:26:27Z` accurately describes retirement + migration and cites the deletion map. Not reopened; no unrelated issue touched. |

**The guard is genuinely fail-closed on all three branches it claims.** That is
now observed, not projected — recorded here as reviewer evidence; T105 still
owns the formal tamper-test record for its own ledger.

### 7.2 Ruling on Hudson's judgment call #1 — `.squad/log/**`, `.squad/orchestration-log/**` — **CONFIRMED**

Independently checked, not accepted on assertion:

- `git check-ignore -v --no-index` returns `.gitignore:51:.squad/log/` and
  `.gitignore:50:.squad/orchestration-log/`. Both directories **are** ignored,
  so no new file can land there without `git add -f`. Hudson's load-bearing
  claim is true.
- All 30 matching lines were read. Every one is dated, past-tense narrative
  ("Apone runs…", "all 6 browser projects green", "126 cases collected").
  **None is configuration, an invocation, a manifest, or an unchecked promise.**

The exemption is narrowly safe and cannot mask active configuration. **Upheld.**
One residual noted for T105: `.gitignore` protects against *new* files, not
against *edits* to the already-tracked log files. Low risk, but it is the one
way this exemption could later be abused.

### 7.3 Ruling on Hudson's judgment call #2 — `constitution.md` / `docs/prd.md`

Three separate questions, answered separately:

1. **Is an ADR/PRD amendment required inside T101? No.** `brief.md` §4 makes
   "not rewriting the constitution and not creating an ADR yet" an explicit
   **approved non-goal**; `plan.md` §6 holds the ADR as a candidate;
   `coverage-migration.md` §5.4 classifies both files as *ADR candidate, not a
   T101 authorization*. Hudson surfaced rather than performed. **Correct call.**
2. **Can AC-005 be truthfully complete with these two files untouched? Yes —
   and the T101 record overstates the problem.** AC-005's enumerated surface is
   `package.json`, `pnpm-lock.yaml`, `Taskfile.yml`, `scripts/**`,
   `.github/workflows/**`, `docs/testing.md`, and the PR template.
   `.specify/memory/constitution.md` and `docs/prd.md` are **not in that list at
   all**. They are therefore *outside AC-005's scope*, not *exceptions to it*.
   `coverage-migration.md` §13.5 and `validation.md` §1 AC-005 both open with
   "AC-005 requires zero active references" — AC-005 says no such thing. This is
   a self-inflicted accuracy defect that makes a complete criterion look
   partial. Recorded as finding **F-3**, not a blocker.
3. **Is the document hierarchy contradictory today? Yes, materially.**
   `.specify/memory/constitution.md` L402 states "**Playwright** is the required
   E2E framework — no substitutes"; L357, L390, L285, L442 and the DoD at L536
   and L538 all still mandate it, as do `docs/prd.md` §7.5.2–§7.5.4 (L233, L236,
   L254). Constitution §0 ranks the constitution **above** `brief.md`, so the
   repository is currently in a state where the highest-authority document
   mandates a framework the working tree has removed, and the guard is
   configured to never report it. That is tolerable **only** as a declared,
   time-boxed exception. **It must be closed by an ADR plus a constitution
   §6.5.7/§7/DoD and PRD §7.5.2–§7.5.4 amendment before this work package is
   allowed to close.** Recorded as a package-closure precondition on T105 and
   raised to `briandenicola` in `.squad/decisions/inbox/ripley-t101-review.md`.
   It is **not** a T101 blocker.

### 7.4 Blockers

#### **B1 — The T47 CI checklist still instructs readers to run tasks that no longer exist**

`.github/T47-CI-SETUP-CHECKLIST.md` is named in `plan.md` §3's T101 in-scope
column and in `coverage-migration.md` §5.2 ("Remove the E2E stage, the 'E2E
smoke / Playwright / Enforced' table row, **and the E2E troubleshooting
section**"), and `tasks.md` T101 checkbox 3 records it as edited. It was edited
only in part. Surviving, and false as of this working tree:

| Line | Text | Why it is broken |
| --- | --- | --- |
| `.github/T47-CI-SETUP-CHECKLIST.md:189` | ``**Locally:** `task test:e2e` `` | `test:e2e` **no longer exists** in `Taskfile.yml` (`task --list` confirms). A reader following this gets a task-not-found error. |
| `.github/T47-CI-SETUP-CHECKLIST.md:191` | ``Pre-warm the stack: `task up`, then `task test:e2e:run` `` | `test:e2e:run` **no longer exists**. Same failure. |
| `.github/T47-CI-SETUP-CHECKLIST.md:158` | ``task verify  # Runs format, build, all tests, vuln scan, E2E`` | `verify.ps1`/`verify.sh` step 9/9 is now the stale-reference guard, not E2E. Actively misdescribes the verification surface T104 is about to consolidate. |
| `.github/T47-CI-SETUP-CHECKLIST.md:244` | `` | `scripts/verify.sh` | Local equivalent of CI pipeline (format → build → test → vuln scan → E2E) | `` | Same misdescription, in the reference table. |
| `.github/T47-CI-SETUP-CHECKLIST.md:185–192` | The "API readiness check failed at http://localhost:8080/health/ready" troubleshooting block | This **is** the "E2E troubleshooting section" §5.2 required removed. It survives intact. |

The stale-reference guard cannot catch any of these: it bans the string
`playwright`, and these lines say `e2e`. **This is precisely the class of
residue AC-005 exists to eliminate, in a file T101 claimed to have cleared.**

#### **B2 — The blanket `specs/` guard exemption hides ten live, unchecked promises to write Playwright tests**

`scripts/check-stale-playwright-references.mjs` L67 exempts the entire `specs/`
prefix. `coverage-migration.md` §5.5 authorizes that by classifying "21 files
under `specs/_backlog/`" as **"historical evidence only"**. That classification
is wrong. Constitution §0 ranks `specs/_backlog/F0XX-*.md` as **authority source
#6** — a forward-looking definition of done for work not yet built, not a log.
The exempted files contain **unchecked acceptance-criteria checkboxes**
requiring new tests in the retired framework:

| Evidence | Line |
| --- | --- |
| `specs/_backlog/F020-user-profile-settings.md:66` | `- [ ] At least one Playwright E2E covers: edit display name → reload → name` |
| `specs/_backlog/F020b-user-profile-extras.md:34` | `- [ ] Playwright happy-path covers each tab.` |
| `specs/_backlog/F021b-admin-logs-viewer.md:34` | `- [ ] Returns 403 for non-Admin (Playwright RBAC test covers it).` |
| `specs/_backlog/F022-user-default-sort-filter-prefs.md:56` | `- [ ] At least one Vitest unit covering the merge logic and one Playwright` |
| `specs/_backlog/F024b-bulk-actions-power-user.md:37` | `- [ ] Playwright spec: "select 3 of 5 → bulk-set category → verify all 3` |
| `specs/_backlog/F027-global-nav-overhaul.md:74` | `- [ ] Playwright: update existing nav-related journeys` |
| `specs/_backlog/F028-infinite-scroll-pull-to-refresh.md:65` | `- [ ] <PullToRefresh> wraps every list route; Playwright touch-emulation` |
| `specs/_backlog/F031-merge-reference-data.md:86` | `- [ ] Playwright journey: create two brands, assign devices to each, merge,` |
| `specs/_backlog/F034-orphaned-device-fields-display.md:108` | `- [ ] Playwright: add a smoke test that imports the canonical SharePoint` |
| `specs/_backlog/F045-pwa-shell-and-device-list.md:420` | `- [ ] pnpm run check, pnpm run lint, pnpm run test clean; Playwright` (plus §6.2, an entire prescribed Playwright test plan) |

The next agent that picks up F031 reads "`- [ ] Playwright journey: create two
brands…`" as its definition of done, and the guard stays silent — **and a brand
new `specs/` file prescribing Playwright would also sail through, permanently.**
`brief.md` §2.1 says there is "**no future automated Playwright role**"; these
ten lines promise nine of them. `plan.md` §2.9 ("historical context is not
current instruction") is exactly the principle being inverted here: unbuilt
backlog is instruction, not context.

This is a guard-design defect, and the guard is T101's deliverable. Hudson
followed `coverage-migration.md` §5.5 faithfully — the defect is inherited from
T103's classification — but "the matrix said so" does not outrank AC-005 or
`brief.md` §2.1.

**Acceptable resolutions (implementer's choice, one of):**
1. Narrow the guard exemption from `specs/` to the historical spec record only
   (e.g. `specs/001-*`, `specs/002-*`, `specs/003-*`,
   `specs/004-agentic-development-foundation/`), let it fail on
   `specs/_backlog/**`, and rewrite each of the ten lines to name its real
   destination layer from `coverage-migration.md` §6–§8 (Vitest component ·
   HTTP integration · manual checklist · accepted gap). Amend
   `coverage-migration.md` §5.5 to reclassify `specs/_backlog/**` as **REVISE**,
   not RETAIN, with the reason. Add a guard test asserting `specs/_backlog/**`
   is **not** exempt.
2. If `briandenicola` prefers not to touch the backlog now: record it as an
   **explicit visible exception** under `plan.md` §2.10 with `briandenicola` as
   named owner and a stated closure trigger ("resolved when each backlog item is
   next picked up"), *and* still narrow the guard so **new** `specs/` content
   cannot add fresh Playwright promises unnoticed.

Silence is not an option — `plan.md` §2.10 forbids it.

### 7.5 Findings — not blockers, carry to T104 / T105

- **F-1 — The guard does not run where the merge decision is made.**
  `.github/workflows/quality-gate.yml` (the merge-blocking workflow) does not
  invoke it; `ci.yml` is `workflow_dispatch`-only. Today the guard is local-only
  (`task check:stale-refs`, `verify.ps1`/`verify.sh` step 9/9), which
  `.github/workflows/README.md:146` labels honestly as "Enforced (locally in
  verify.sh)". `plan.md` §5 R-4 explicitly stages CI enforcement to T104/T105,
  so this is on-plan — but it is the *same shape* as the failure that produced
  this package, and T104 must close it, not inherit it.
- **F-2 — The guard scans only `git ls-files`.** An untracked
  `playwright.config.ts` or `tests/e2e/**` file is invisible to it locally.
  Correct for CI (clean checkout), a real local blind spot. T105's tamper test
  should record this boundary explicitly.
- **F-3 — AC-005 is misquoted in two places.** `coverage-migration.md` §13.5 and
  `validation.md` §1 AC-005 both assert "AC-005 requires zero active
  references"; AC-005's text enumerates seven surfaces and includes neither
  `constitution.md` nor `docs/prd.md`. Restate them as *out of scope*, not as
  *exceptions*, so the ledger stops understating a criterion that is otherwise
  met.
- **F-4 — `main()`'s `ENOENT` skip precedes the structural check.** A
  tracked-but-deleted path under `tests/e2e/` is skipped before
  `isReintroducedHarnessPath` ever sees it, and the reported "901 tracked files"
  currently includes 31 deleted-but-unstaged entries. Harmless today (the
  deletions are real); worth a one-line reorder and an accurate count.
- **F-5 — `docker-compose.e2e.yml:11` still points at `tests/e2e/fixtures/auth.ts`
  and `:16`/`.env.e2e:14` at `scripts/run-e2e.{sh,ps1}` — all deleted.**
  Correctly deferred to T104 by `coverage-migration.md` §5.3; listed so T104
  cannot lose it.

### 7.6 Consequence and assignment

**T101 is not complete. AC-005 is not satisfied.** `tasks.md`, `plan.md` and
`coverage-migration.md` §13 record T101 as `DONE`; that status is withdrawn to
`REJECTED` pending revision.

**T104 must not begin.** `plan.md` §4 makes T104 depend on T101, and B1 is
specifically a false description of the verification surface T104 is chartered
to consolidate — starting T104 now would build on documentation the reviewer has
already found untrue.

**Revision owner: Apone (Tester / QA).** Hudson is locked out of this revision
cycle as the author of the rejected work. B1 is verification-promise removal in
a CI/test document and B2 is test-boundary classification plus a guard
exemption — both squarely QA's competence. Apone must **not** repair the
harness, re-add any browser suite, or touch `constitution.md` / `docs/prd.md`
(ADR-gated, §7.3).

**Re-review returns to Ripley.** Both blockers closed, the guard re-run and
re-tamper-tested, and `coverage-migration.md` §5.5 amended, before T101 is
`DONE` and T104 is released.

### 7.7 Revision submitted — Apone, 2026-09-02 (awaiting Ripley re-review)

This section records what Apone did to close §7.4's two blockers. **It is not
a self-approval.** Per §7.6, only Ripley can return T101 to `DONE` and release
T104; that gate has not yet run. Nothing in §7.1–§7.6 above is edited or
retracted — this is an addition, not a correction of the verdict.

**B1 closed.** All five rows of the §7.4 B1 table are fixed in
`.github/T47-CI-SETUP-CHECKLIST.md`: the `task test:e2e` / `task test:e2e:run`
lines and the entire "API readiness check failed" troubleshooting block are
removed (replaced by a dated note naming what was removed and why, with T104
named as the open scope — no claim that T104's unified surface already
exists); the `task verify` comment and the Workflow Files reference-table row
now describe the pipeline that actually runs today (format → build → test →
vuln scan → stale-reference guard), and the Phase 1 Quality Gate Summary table
gained a row for the guard. Re-verified directly against the file, not only
via the guard (the guard cannot see `e2e`-worded residue, only the literal
string `playwright` — §7.4 note preserved as still true of the guard's design).

**B2 closed.** `scripts/check-stale-playwright-references.mjs` no longer
exempts a blanket `specs/` prefix. Per §7.4's resolution option 1: a named
`EXEMPT_SPEC_PATHS` allowlist now covers only the twelve historical files
recorded in `coverage-migration.md` §5.5 (narrower than even Ripley's own
`specs/001-*`/`specs/002-*`/`specs/003-*` prefix suggestion, to close the
"a brand new `specs/` file… would sail through" hole as tightly as possible —
prefix-based allowlisting would not have caught a new file dropped into an
already-exempt package prefix). `coverage-migration.md` §5.5 is amended to
reclassify `specs/_backlog/**` as **REVISE**, not RETAIN, with the reason
recorded, and a new §5.5a disposition table.

A repository-wide audit — not limited to §7.4's ten-line sample — found
**16** `specs/_backlog/**` files with a live Playwright promise: the ten
Ripley named plus F021, F023, F024, F026, F029, F030. Every one of the 16 is
rewritten to name its real destination from `coverage-migration.md` §6–§8
(HTTP integration test, Vitest component test, a
`docs/testing/manual-pwa-validation.md` addition, or a declared accepted gap
in the **G-09** family) — no promise deleted silently, no automated coverage
invented: three (F021, F024, F034) had their checkbox **checked** only after
confirming by name that the underlying test already exists and asserts the
claimed behaviour. Zero `playwright` references remain under
`specs/_backlog/**` (repo-wide case-insensitive text search).

**Guard tests: 15 → 18, 18/18 pass.** Two cases added: a synthetic
`specs/_backlog/**` fixture with a Playwright line **fails**; an arbitrary
`specs/` path outside the named allowlist (including a hypothetical new
package) **also fails** — closing exactly the permanent hole §7.4 named.
**Live guard: 0 active references / 901 tracked files.**

**Tamper test (this revision, on the backlog branch).** Appended a synthetic
unchecked Playwright line to `specs/_backlog/F031-merge-reference-data.md` →
guard **exit 1**, exact file:line reported → restored from a pre-tamper
backup → `git hash-object` before/after **identical**
(`5517f1f9b0a57ca6c80c36c4367b75383f8e25c0`) → guard **passed, 0/901** again.
This is additional to, not a replacement for, §7.1's structural-tamper record
(config-branch, tree-branch), which this revision did not touch or re-run.

**Repository-wide reference re-count.** 35 files remain (case-insensitive,
whole disk, since `specs/004-agentic-development-foundation/` is currently
untracked and invisible to `git`-based scans). All 35 classify as either an
explicit exemption (named historical spec files, `.squad/decisions*`,
`.squad/agents/*/history.md`, `.squad/log/**`/`.orchestration-log/**`,
`SESSION-NOTES.md`, `.copilot-state.md`, `docs/testing/manual-pwa-validation.md`,
`.env.e2e`, `docker-compose.e2e.yml`, the `pnpm-lock.yaml` peer-dependency
line) or a file that names only the guard's own script filename
(`.github/T47-CI-SETUP-CHECKLIST.md`, `.github/workflows/README.md`,
`scripts/verify.sh`, `scripts/verify.ps1`, `Taskfile.yml` — individually
confirmed to contain no other "playwright" text). **`constitution.md` and
`docs/prd.md` are untouched** — per §7.3's ADR-gated package-closure
precondition, not re-litigated here.

**Not touched by this revision:** §7.1–§7.6 above (Ripley's findings, left as
the historical record); Hudson's structural tamper tests (§7.1); T102 (§6);
any application/product code; `constitution.md`; `docs/prd.md`. T104 was not
started.

**Requesting re-review from Ripley.** Full change record:
`coverage-migration.md` §13.9; `tasks.md` T101 section; `.squad/agents/apone/history.md`;
`.squad/decisions/inbox/apone-t101-revision.md`.

---

### 7.8 T101 Re-Review Gate — Ripley, 2026-09-02 — **APPROVED**

**Verdict: APPROVED. T101 is `DONE`. AC-005 is met. T104 is authorized to
begin.** Hudson's original `DONE` was withdrawn at §7 and is not restored;
this approval rests on Apone's revision plus the reviewer's own re-execution,
never on the revision summary in §7.7 or `.squad/decisions/inbox/apone-t101-revision.md`.

### 7.8.1 B1 — CLOSED (verified against the file, not the guard)

`.github/T47-CI-SETUP-CHECKLIST.md` re-read in full. All five §7.4 B1 rows
are fixed:

- **L185-192 removed.** No `task test:e2e`, no `task test:e2e:run`, no "API
  readiness check failed" Docker-Compose readiness troubleshooting anywhere
  in the file. The replacement (L186-192) is a dated *removal note* that
  names what went and why, and explicitly says there is **no** consolidated
  replacement entry yet and that writing one is T104's scope. It does not
  pre-claim T104's unified surface.
- **L158 `task verify` comment** now reads format → build → tests → vuln scan
  → frontend check/lint → stale-reference guard. That matches
  `scripts/verify.sh:47-49` and `scripts/verify.ps1:54-57` step 9/9 as they
  actually exist today.
- **L244 Workflow-Files row** for `verify.sh` corrected identically.
- **L26 and L229** now mark the browser stage as retired-at-the-time rather
  than as a live capability, and a new L230 row names the guard as its
  replacement.
- `Taskfile.yml` independently confirmed to contain no `test:e2e` /
  `test:e2e:run` target; the only E2E-adjacent target is
  `check:stale-refs` (L87-90).

Residual `e2e` strings in the file (L44 "Verified Ubuntu runner can run
Docker Compose (for E2E)", L144 "Quick feedback loop (no E2E…)") are
past-tense setup record and a still-true statement respectively. Neither
instructs a reader to run anything that no longer exists. **B1 is closed.**

### 7.8.2 B2 — CLOSED (allowlist audited file-by-file, not accepted as stated)

- The blanket `specs/` prefix is gone from
  `scripts/check-stale-playwright-references.mjs`. `EXEMPT_PATH_PREFIXES` is
  now `.squad/decisions/`, `.squad/log/`, `.squad/orchestration-log/` only.
- `EXEMPT_SPEC_PATHS` is an exact-path allowlist of twelve files. The
  reviewer read every Playwright line in all six *tracked* entries
  (`specs/001-core-api/{plan,tasks}.md`,
  `specs/002-frontend-mvp/{plan,spec,tasks}.md`,
  `specs/003-pwa-polish/tasks.md`; the six
  `specs/004-agentic-development-foundation/*.md` entries are untracked today
  and are forward-looking). `001-core-api/tasks.md` is `✅ P1 COMPLETE`,
  `002-frontend-mvp/{plan,spec}` are `Shipped (production-validated
  2026-05-19)`, `003-pwa-polish/tasks.md:159` is a single dated past-tense
  Vasquez log line. All six are closed-phase records. **The allowlist is
  genuinely historical.** Apone's choice of exact paths over Ripley's
  suggested `specs/001-*`/`002-*` prefixes is the stronger construction and
  is endorsed as the standing pattern.
- **Independent repo-wide re-derivation, not a check of Apone's list.** A
  case-insensitive whole-disk scan (excluding `node_modules`, `.git`, build
  output) returns **56 files**; `git grep -I -i -l` returns **39 tracked**
  files. Every one classifies as an explicit exemption, an agent/session
  history, a decision ledger, `pnpm-lock.yaml`'s `@vitest/browser-playwright`
  optional peer line, or a file naming only the guard's own script filename.
  **Zero hits under `specs/_backlog/**`. Zero in any manifest, script,
  workflow, config, instruction file, or executable test.**
- All 16 backlog diffs were read line by line. Every rewritten promise names
  a concrete lower-layer destination. The three flipped checkboxes were
  verified against real code, by name, not by citation:
  `AuditEventsAuthorizationTests.GetAuditEvents_WhenCallerIsMember_ReturnsForbidden`
  (`…/Controllers/AuditEventsAuthorizationTests.cs:18`),
  `ViewerRoleAuthorizationTests.GetAuditEvents_WhenCallerIsViewer_ReturnsForbidden`
  (`…:104`),
  `DevicesControllerTests.BulkUpdateDevices_WhenValid_UpdatesAllDevicesAndAuditsEachWithCorrelationId`
  (`…/DevicesControllerTests.cs:556`),
  `SharePointCsvImportTests.CommitImport_CanonicalDevicesCsv_MohuLeafStitchRowPersistsModelAndPurpose`
  (`…:25`, asserting `Model == "Leaf Stitch"` and `Purpose == "Master TV"`
  at `:62-63`), `BulkActionBar.test.ts` (bar-visibility at `:36`, `:41`),
  `DeviceTable.test.ts` (grouped-header assertions at `:102-127`),
  `groupDevices.test.ts`. **No invented coverage found.** F023's and F045's
  declared **G-09**-family gaps are honest: `docs/testing/manual-pwa-validation.md`
  really does carry **M-03**, **M-10**, **M-14**, **M-15**, and §8 really
  does carry **G-08**/**G-09**. **B2 is closed.**

### 7.8.3 Guard and tamper evidence — re-executed by the reviewer

- `node scripts/check-stale-playwright-references.mjs` → **0 active
  Playwright references across 901 tracked file(s)**, exit 0.
- `node --test scripts/check-stale-playwright-references.test.mjs` →
  **18/18 pass**, including the two B2 regression cases.
- **Tamper A (active-backlog path — the branch B2 created).** A synthetic
  `- [ ] Playwright journey: …` line appended to
  `specs/_backlog/F026-pwa-quick-win-ux-pack.md` → **exit 1** at
  `specs/_backlog/F026-pwa-quick-win-ux-pack.md:121`. Restored;
  `git hash-object` **`91732bff76844917d2f7d1c4f6c4e17f62f2c3af`** before and
  after — byte-identical. Deliberately a *different* file from the F031 one
  Apone used.
- **Tamper B (brand-new spec package — the permanent hole B2 named).**
  `specs/005-ripley-probe/plan.md` containing `Run Playwright before merge.`,
  made visible to `git ls-files` with `git add -N` → **exit 1** at
  `specs/005-ripley-probe/plan.md:1`. Index and tree restored.
- **Tamper C (structural, no keyword).** `tests/e2e/probe.ts` containing only
  `export const revived = 1;` → **exit 1**, "path lies under the retired
  tests/e2e/ tree". Index and tree restored.
- After all three, the guard is clean again at 0/901 and
  `git status --porcelain` differs from the pre-review baseline by **0
  lines**. No reviewer artefact left behind.

### 7.8.4 Original T101 evidence re-confirmed, T102 intact

`tests/e2e/` absent from the working tree; **no** `playwright.config.*`
anywhere on disk; **no** Playwright entry in any non-`node_modules`
`package.json`; **no** `*playwright*` directory in
`src/TechInventory.Web/node_modules`; `.github/pull_request_template.md`
contains neither `playwright` nor `e2e`; `.github/workflows/ci.yml` mentions
E2E only in a historical comment; `docs/testing.md` describes the retirement
truthfully. Issue **#89** is still `CLOSED` / `NOT_PLANNED`. The manual
checklist survives with 15 checks, owner `briandenicola`, and a
release-tag cadence. File-modification timestamps confirm the revision's
blast radius was exactly the 16 backlog specs, the T47 checklist, the guard
and its test, this package's five documents, and Apone's own history and
decision record — **no product code, no test code, no `constitution.md`, no
`docs/prd.md`, and T104 was not started.**

### 7.8.5 Constitution / PRD contradiction — ruling re-affirmed, not softened

`.specify/memory/constitution.md` L357-359, L390, L402 ("**Playwright** is
the required E2E framework — no substitutes"), L442, L536 and `docs/prd.md`
L227-258 still mandate a framework this tree has removed. Neither document
appears in AC-005's enumerated surface list, so this is **not** a T101
blocker and T101 is not held for it. It remains a **mandatory
package-closure precondition**: an ADR plus a constitution §6.5.7/§7/DoD and
PRD §7.5.2-§7.5.4 amendment must land before this work package can close.

**Condition on T104's authorization:** T104 must not deepen the
contradiction. It may not add any claim that the new verification surface
satisfies constitution §9 / §6.5.14 / L442's "Playwright smoke", and it must
carry this precondition forward visibly (`plan.md` §2.10) rather than
inheriting silence. Making the verification surface authoritative while the
constitution still names a retired framework is precisely the drift this
package exists to end.

### 7.8.6 Findings — NOT blockers, carried forward

Recorded so they cannot vanish; none blocks T101 or T104.

1. **F-1 — generic `E2E` promises survive the backlog rewrite.**
   `specs/_backlog/F045-pwa-shell-and-device-list.md:357` ("Zero axe-core
   violations in unit **and E2E** for every touched view") and `:427` ("Zero
   axe violations (unit **+ E2E**, both themes)") contradict F045's own new
   §6.2 post-retirement note, three lines below `:427`. Untouched files
   `F018:103`, `F019:123`, `F025:170` carry the same shape. These were never
   Playwright-worded, are outside AC-005, and gate no built work — but they
   are unmeetable verification promises. **Owner:** the F045 backlog owner at
   pickup; sweep the rest in T105's documentation pass. **Closure trigger:**
   no `specs/_backlog/**` acceptance criterion names an automated E2E layer.
2. **F-2 — `validation.md` §7.7's "35 files remain" is understated.** The
   reviewer's own whole-disk case-insensitive scan finds **56** files (39
   tracked). The *classification* claim in §7.7 holds for all 56; the count
   does not. Corrected here rather than in §7.7, which is left as Apone's
   record.
3. **F-3 — exact-path exemptions are line-blind.** `specs/002-frontend-mvp/plan.md:128`
   and `:133` are **unchecked** Playwright DoD boxes sitting inside an
   allowlisted file. They are historical (package `Status: Shipped`), but a
   *new* Playwright promise appended to any of the twelve allowlisted files
   would be invisible to the guard. Same shape as §7.2's ".gitignore stops
   new files, not edits to tracked ones". **Record both in T105's tamper-test
   boundary statement.**
4. **F-4 — carried unchanged from §7.5:** the guard still does not run in
   `quality-gate.yml` (T104 must close it); it scans only `git ls-files`, so
   an untracked reintroduction is locally invisible (T105 boundary record);
   `main()`'s `ENOENT` skip precedes the structural check (T105);
   `docker-compose.e2e.yml:11`/`:16` and `.env.e2e:14` still reference
   deleted paths (T104, per §5.3).

### 7.8.7 Consequence

T101 → `DONE`. AC-005 → met. **T104 is released and authorized to begin**,
subject to §7.8.5's condition. T105 inherits F-1 through F-4. No agent
lockout carries forward from this cycle: the revision was accepted, so
Apone is not locked out of future work; Hudson's §7.6 lockout was
cycle-scoped to the rejected revision and expires with this approval.

---

## 9. T104 Implementation — Hudson, 2026-09-02 — `VALIDATING`, not self-marked `DONE`

Per `plan.md` §2.3, an agent may not mark its own work `DONE`. This section
records what was built and directly observed running on this machine
(Windows, no bash available) so an independent reviewer can verify without
re-deriving it from scratch. **Nothing below claims T105's scope** (branch
protection alignment, tamper-testing every guard) is complete.

### 9.1 Task graph — the one authoritative verification interface

`Taskfile.yml` is now referenced by every workflow and both compatibility
scripts. Exact graph (leaf-to-root):

- `restore` — `dotnet tool restore` (pins `dotnet-ef` via `.config/dotnet-tools.json`) + `dotnet restore` + `pnpm --dir src/TechInventory.Web install --frozen-lockfile`
- `build:backend` — `dotnet build -c Release`; `build:frontend` — `pnpm run build`; `build` — alias depending on both
- `check:format` — `dotnet format --verify-no-changes`
- `check:frontend` — `pnpm run check` (regenerates the OpenAPI client first, then `tsc --noEmit` + `svelte-check`)
- `lint` — depends on `check:format`, then runs ESLint only (no duplicated format check)
- `check:client-drift` — regenerates `src/lib/api/generated/types.ts`, `git diff --exit-code`s it
- `check:openapi-drift` — `dotnet run --project src/TechInventory.Api -- export-openapi <absolute path>` then `python`/`python3 scripts/compare-openapi.py` (platform-branched only on the interpreter name; same script both sides)
- `check:migration-drift` — `dotnet ef migrations has-pending-model-changes`
- `check:vulnerable` — `dotnet list package --vulnerable --include-transitive`
- `check:stale-refs` — `node scripts/check-stale-playwright-references.mjs` + its `node:test` suite
- `test:unit` / `test:integration` / `test:frontend` — each delegates to `node scripts/check-test-floors.mjs <suite>`, which runs the real suite (with `--collect:"XPlat Code Coverage"` for the .NET suites, preserving the coverage-collection side effect Quality Gate's old `dotnet` job had) and fails closed if the collected count is at or below its floor; `test` runs all three
- **`verify:fast`** = `check:format` + `build:backend` + `check:frontend` + `lint` + `test:unit` + `test:frontend`
- **`verify:contracts`** = `check:stale-refs` + `check:openapi-drift` + `check:client-drift` + `check:migration-drift` + `test:integration`
- **`verify:full`** = `verify:fast` + `verify:contracts` + `build:frontend` + `check:vulnerable`
- **`verify`** = alias for `verify:full` — the one command humans and CI both run

No command's logic is duplicated: `scripts/verify.ps1` and `scripts/verify.sh`
are now both thin wrappers (check `task` is on PATH, then run `task verify`);
`.github/workflows/quality-gate.yml`'s `verify` job installs the toolchains
and Task itself (`arduino/setup-task@v2`) and then also just runs
`task verify`; `.github/workflows/ci.yml` calls `scripts/verify.sh`, so it
inherits the same pipeline through one more layer of indirection, not a
parallel one.

### 9.2 Workflow changes

- **`.github/workflows/quality-gate.yml`** (the actual merge-blocking
  workflow): the previous `dotnet` and `web` jobs — which fully duplicated
  `dotnet format`/`build`/`test`/`list package --vulnerable` and
  `pnpm install`/`generate:client`/`check`/`lint`/`test`/`build` inline —
  are replaced by a single `verify` job that installs .NET, Node, Task, and
  PyYAML (needed to parse the freshly-exported OpenAPI YAML during drift
  comparison) and runs `task verify`. **This closes F-4**: the
  stale-reference guard (`check:stale-refs`, part of `verify:contracts`) now
  runs in the merge-blocking workflow for the first time. `codeql`,
  `secrets`, `container-config-scan`, and `sbom` jobs are unchanged — they
  are security-scanning tools independent of the verification pipeline, not
  duplicated verification logic.
- **`.github/workflows/ci.yml`**: still `workflow_dispatch`-only (re-enabling
  it on push/PR is explicitly left to a future task, not decided here). It
  now installs Task before calling `scripts/verify.sh` (which itself now
  calls `task verify`), and its header comment is corrected to state the
  current, accurate relationship to Quality Gate rather than the stale
  "verify.sh pipeline is the one we keep wanting to repair-then-mute" framing
  that predated Quality Gate's existence.
- **`.github/workflows/README.md`**: rewritten to describe the Task-based
  pipeline stages, the measured test floors and their basis, the corrected
  required-check name (`Quality Gate / verify`, not the retired `ci / verify`),
  and an explicit note that branch-protection alignment is T105's job, not
  verified here.

### 9.3 Collected-test floors and their measured basis

Measured directly on this machine before choosing each floor (not assumed):

| Suite | Command | Measured baseline | Floor | Floor rationale |
| --- | --- | --- | --- | --- |
| `unit` | `dotnet test tests/TechInventory.UnitTests` | 278 passed / 278 total | 250 | ~10% margin below baseline; a floor at exactly the baseline would false-positive on any single legitimately-skipped/parameterized-collapsed test |
| `integration` | `dotnet test tests/TechInventory.IntegrationTests` | 292 passed, 4 skipped, 296 total | 265 | same ~10% margin against the 296 collected-total figure (skips still count as collected) |
| `frontend` | `pnpm exec vitest --run` | 649 tests / 83 files | 580 tests / 74 files | same ~10% margin on both dimensions (test count and file count), so a suite that keeps its test count but silently drops whole files is still caught |

Fail-closed behaviour was proven, not assumed: running
`dotnet test --filter "FullyQualifiedName~NoSuchTestNamespaceXYZ"` against the
unit project exits 0 (not an error) with "No test matches the given testcase
filter" and produces a TRX with `<Counters total="0">` — `check-test-floors.mjs`
treats this as a hard failure below any floor, which is exactly the silent
zero-collection failure mode these floors exist to catch (the same class of
defect T103 found in the retired Playwright harness).

### 9.4 Validation run outcomes

- `task verify:fast` — **full run, exit 0.** All six constituent stages
  passed, including a from-scratch Vitest run (649/649) and a from-scratch
  `dotnet build -c Release` + unit-test run (278/278).
- `task verify:contracts` — `check:stale-refs` **passed** (0 active
  references across 898 tracked files after this session's file deletions);
  `check:openapi-drift` **passed** (structural diff clean); `check:client-drift`
  **fails**, and this is expected, not a defect: this branch's own
  uncommitted T101/T102 changes to `openapi.yaml` (the 26-case `403`
  response additions from T102's Hicks fix) had not been committed at the
  time of this run, and per this task's explicit instruction not to commit
  or push, that diff was left in place. `git status` at the very start of
  this session already showed `openapi.yaml` and
  `src/TechInventory.Web/src/lib/api/generated/types.ts` as modified, before
  any T104 change. This check will pass cleanly once that pre-existing branch
  work is committed. `check:migration-drift` and `test:integration` were not
  reached in this particular run because Task's default behaviour stops a
  `cmds:`-chained task list on the first failing step; both were verified
  independently in isolation earlier in this session (`dotnet ef migrations
  has-pending-model-changes` → exit 0, "No changes have been made to the
  model"; `dotnet test tests/TechInventory.IntegrationTests` → 292 passed /
  4 skipped / 296 total).
- `scripts/verify.ps1` and `scripts/verify.sh` were confirmed to invoke the
  identical `task verify` entrypoint (observed directly for `verify.ps1`;
  `verify.sh` cannot be executed on this Windows machine — no bash is
  available — and was kept syntactically aligned with `verify.ps1` by
  inspection, per this task's explicit instruction).
- `.github/workflows/quality-gate.yml` and `.github/workflows/ci.yml` were
  validated for YAML syntax (`python -c "import yaml; yaml.safe_load(...)"`
  on both files, clean) but **were not run on GitHub Actions** — that
  requires a push, which this task explicitly forbids. This is a known,
  disclosed limitation: the workflow wiring is syntactically correct and
  calls exactly the same `task verify` command already proven to work
  locally, but end-to-end CI execution has not been directly observed.

### 9.5 Docker requirement

**No stage of `verify:fast`, `verify:contracts`, or `verify:full` requires
Docker.** `test:integration` exercises the API in-process via
`WebApplicationFactory<Program>` against a real SQLite database file — no
container is started. Confirmed by grep across
`tests/TechInventory.IntegrationTests/` (no `docker` references) and by
directly running the suite without any compose stack up.

### 9.6 `.env.e2e` / `docker-compose.e2e.yml` resolution

Both files were **deleted**, not renamed or repurposed. Neither had a real
non-browser role: `docker-compose.e2e.yml` described a containerized stack
for the retired Playwright harness, and `.env.e2e` supplied stub Entra
credentials solely for that harness's `${VAR:?}` compose interpolation
guards. Nothing else in the repository referenced either file (confirmed via
`grep` before deletion). Cleanup performed alongside the deletion:
`scripts/check-stale-playwright-references.mjs`'s `EXEMPT_EXACT_PATHS` no
longer lists either path (a file that does not exist needs no exemption),
its header comment bullet is corrected from "revision deferred to T104" to
record that T104 deleted both, and the corresponding guard test was flipped
from "exempts" to "does NOT exempt" to lock in the new behaviour as a
regression guard. `.gitleaks.toml`'s `.env.e2e`-specific allowlist path entry
was removed. `docker-compose.yml`'s stale comment referencing `task test:e2e`
(a task that no longer exists) was corrected to name only the tasks that
still exist (`task up`, `task backup:*`).

### 9.7 Duplicate-logic cleanup beyond the workflows

`scripts/check-openapi-drift.sh` — the pre-existing bash script Quality
Gate's old `dotnet` job called — was **deleted**, not kept as an unused
alternate path. Its logic (regenerate via `export-openapi`, diff via
`scripts/compare-openapi.py`) is identical to `Taskfile.yml`'s
`check:openapi-drift` task, which is now the only place that logic lives.
Nothing else referenced the script outside itself and one historical
evidence citation in `evidence.md` (left untouched, per the instruction not
to erase historical evidence).

### 9.8 Explicitly not claimed

- **The constitution/PRD Playwright contradiction (`§7.8.5`) is not
  deepened, softened, or resolved.** No file changed in this task claims the
  new verification surface satisfies constitution §9/§6.5.14/L402/L442's
  Playwright-smoke mandate. The ADR + constitution/PRD amendment remains an
  open, named package-closure precondition for `briandenicola`
  (`§7.8.5`, `coverage-migration.md` §13.9 note).
- **T105 is not started.** No branch-protection setting was read or changed
  on GitHub; no guard was tamper-tested end-to-end in CI; the required-check
  name correction in `.github/workflows/README.md` is a documentation fix
  describing intended state, not a configuration change. Findings F-1, F-2,
  and F-3 (`§7.8.6`) are untouched and remain T105's to close.
- **T104 is not marked `DONE`.** This section records `VALIDATING` —
  implementation complete and evidence gathered — pending the human/reviewer
  approval `plan.md` §2.3 requires before any work moves to `DONE`.

---

## 10. T104 Independent Reviewer Gate — **REJECTED** (Apone, 2026-09-02)

Reviewer: Apone (Tester / QA). Author under review: Hudson. Every command
below was re-run by the reviewer on this machine; no line of this section
rests on the implementing agent's summary. Team-level record:
`.squad/decisions/inbox/apone-t104-review.md`.

**Verdict: REJECTED. AC-008 is not met. T105 is not authorized to begin.**
**Revision owner: Hicks** (Hudson is locked out for this cycle only).

### 10.1 Reviewer-run stage results

| Stage | Reviewer result — Windows, no Docker, no browser |
| --- | --- |
| `task verify:fast` | **exit 0**, full run: `dotnet format` clean · `dotnet build -c Release` 0 errors · `svelte-check` 0 errors / 0 warnings · ESLint clean · unit **278/278** · frontend **649 tests / 83 files** |
| `task check:stale-refs` | **exit 0**; guard's own `node:test` suite **18/18** |
| `task check:openapi-drift` | **exit 0** — "OpenAPI document is current"; `mkdir -p` resolves correctly under Task's embedded shell on Windows |
| `task check:migration-drift` | **exit 0** — "No changes have been made to the model since the last migration" |
| `task test:integration` | **exit 0** — **296 collected** (292 passed, 4 skipped) via in-process `WebApplicationFactory<Program>` + real SQLite; **no container started** |
| `task build:frontend` | **exit 0** — adapter-static production build |
| `task check:client-drift` | **exit 1** — **B-1**, a false failure |
| `task check:vulnerable` | exit 0 — **B-2**, cannot fail |
| `task verify` / `verify:contracts` / `verify:full` | **never observed to complete** — blocked at `check:client-drift` |

**Docker/browser independence confirmed.** No mandatory stage starts a
container or downloads a browser. §9.5's claim holds.

### 10.2 Collected-test floors — tamper-tested and APPROVED

Both floor mechanisms (TRX counter for .NET, JSON reporter for Vitest) were
deliberately broken and restored **byte-identically** (`git hash-object`
match on `scripts/check-test-floors.mjs` `fe45ebb…` and
`src/TechInventory.Web/vite.config.ts` `540ce6e…`; `git status --short`
returned to its 145-entry baseline).

| # | Tamper | Runner outcome | Guard outcome |
| --- | --- | --- | --- |
| 1 | `dotnet test --filter FullyQualifiedName~NoSuchNamespaceXYZ` (unit) | **exit 0**, TRX `total="0"` | **exit 1** — "collected 0 test(s), below the floor of 250" |
| 2 | `dotnet test --filter FullyQualifiedName~Domain` (unit) | **115 passed, exit 0** | **exit 1** — "collected 115 test(s), below the floor of 250" |
| 3 | Vitest `include` narrowed to `src/lib/tokens.test.ts` | **2 passed / 1 file, exit 0** | **exit 1** — test floor *and* file floor both fired |
| 4 | Vitest `include` matching nothing | 0 files | **exit 1** — 0 tests / 0 files |

Case 2 is the decisive one: an all-green suite that silently lost 59% of its
tests is still caught. The floors and the ~10% margin are **approved as
designed**. T105 still owns the complete guard tamper matrix; this covers the
floor mechanism only.

### 10.3 Blockers

**B-1 — `check:client-drift` uses a mixed reference point and fails when
nothing is wrong.** `Taskfile.yml:72-77`. `generate:client` reads the
**working-tree** `openapi.yaml`; `git diff --exit-code` then compares the
result against the **index/HEAD** copy of `types.ts`. Measured evidence:
`types.ts` hashes SHA-256
`0EF521499059AD2689ABCDE589813D9ACAFED55F0035C468721A107B3D07B95B` **both
before and after** `pnpm run generate:client` — the client and the spec are
in sync and there is no drift — yet the check exits 1 and prints T102's
intended `403` additions as the "diff". `check:openapi-drift`
(`Taskfile.yml:86-99`) does the same job correctly against the on-disk file
and passes; the two drift gates in one pipeline disagree on what "committed"
means. §9.4's characterization ("expected, not a defect… will pass once
committed") is **not accurate**: the artifact is already correct, the
comparison basis is wrong, and under this package's standing
do-not-commit instruction the check can never pass — which is why the
authoritative command has never been seen to finish. *Required:* generate to
a scratch path and compare against the working-tree file, matching
`check:openapi-drift`; then record an end-to-end `task verify` exit code.

**B-2 — `check:vulnerable` is fail-open and documented as "Enforced".**
`Taskfile.yml:101-104`, `.github/workflows/README.md:161`. Proven in an
isolated probe project pinned to `Newtonsoft.Json 12.0.1`:
`dotnet list package --vulnerable --include-transitive` printed
`> Newtonsoft.Json 12.0.1 12.0.1 High https://github.com/advisories/GHSA-5crp-9r3c-p9vr`
and **exited 0**. The task parses no output, so this stage of `verify:full`
cannot fail. The T104-authored README nevertheless lists it as `Enforced`.
The fail-open behaviour predates T104; the "Enforced" claim does not, and a
ceremonial gate labelled enforced is the exact defect class `brief.md` §1
exists to end. *Required:* make it fail closed, or reclassify it as ADVISORY
in both the README table and the task description.

**B-3 — `ci.yml` depends on PyYAML and does not install it.**
`.github/workflows/ci.yml:81` → `scripts/verify.sh` → `task verify` →
`check:openapi-drift` → `python3 scripts/compare-openapi.py`, which imports
`yaml` and exits with an install message when absent
(`scripts/compare-openapi.py:28-30`). `quality-gate.yml:38` installs PyYAML
and calls it required; `ci.yml` has no such step. Either the step is
unnecessary in Quality Gate or T104's rewiring broke `ci.yml`'s only job —
both cannot be true, and neither has been observed. *Required:* resolve in
writing and make the two workflows consistent.

### 10.4 Non-blocking findings

- **F-5** — `scripts/check-security.mjs` (auth-token-persistence / secret diff
  scan) is not a Task target; it runs only in the muted `ci.yml` and in the
  `--no-verify`-bypassable pre-commit hook, never in the merge-blocking
  workflow. That contradicts "Task is the sole authoritative surface". Add a
  `check:security` task or record an explicit visible exception.
- **F-6** — `restore` executes six times in one `verify:fast` run; each
  sub-task re-resolves its own `deps`. Correct, wasteful.
- **F-7** — `quality-gate.yml`'s `verify` job declares no `permissions:` block
  while every sibling job scopes its own.
- **F-8** — no numeric coverage gate exists. The README states this honestly,
  but constitution §7's 85% Domain+Application target stays unenforced:
  Cobertura reports are produced and nothing asserts against them. Belongs in
  the same ADR that resolves the Playwright mandate.
- **F-9** — the deleted `scripts/check-openapi-drift.sh` carried an
  empty-output guard (`if [[ ! -s "$generated" ]]`) with no equivalent in
  `Taskfile.yml`'s `check:openapi-drift`; it relies on `compare-openapi.py`
  failing on an empty document instead.

### 10.5 Confirmed correct — not re-litigated in revision

- **Deletions cost nothing real.** `.env.e2e` and `docker-compose.e2e.yml` at
  `d303cd6` each declare in their own headers that they exist for
  `scripts/run-e2e.{sh,ps1}`; those scripts are gone and every surviving HTTP
  test runs in-process. No non-browser capability was lost.
  `scripts/check-openapi-drift.sh`'s logic is present in `Taskfile.yml`
  (modulo F-9).
- **F-4 closed.** `check:stale-refs` runs inside the merge-blocking workflow
  for the first time, through `verify:contracts` → `task verify`.
- **No competing workflow can report a weaker green.** Only `quality-gate.yml`
  triggers on `pull_request`/`push: main`; `ci.yml` is `workflow_dispatch`
  only; `release-images.yml`/`security-scan.yml` are post-merge; the `squad-*`
  workflows are ops automation. Check name `Quality Gate / verify` is stable
  and suitable for T105.
- **Wrappers are fail-closed and non-duplicating.** `scripts/verify.ps1`
  (`$ErrorActionPreference = 'Stop'`, PATH probe, `exit $LASTEXITCODE`) and
  `scripts/verify.sh` (`set -euo pipefail`, PATH probe, `task verify` as the
  final command) contain no pipeline logic. `verify.sh` remains unexecuted —
  no bash on this host — and that stays disclosed, not claimed.
- **Playwright contradiction intact and honest.** `docs/testing.md` L8–L10
  still states that PRD §7.5.2–§7.5.4 and constitution §6.5.7/§7 describe a
  harness this tree has removed. No T104 file claims the new surface satisfies
  the Playwright-smoke mandate. §7.8.5's condition is honored; the
  contradiction is neither deepened nor falsely resolved.
- **No premature `DONE`.** `tasks.md`, `plan.md` L211/L224, `validation.md`
  §1, and `coverage-migration.md` §14 all read `VALIDATING`, consistently.
- **GitHub execution is not claimed.** Neither workflow has run; both parse.
  Recorded as `unknown` until a push, per AC-010.

### 10.6 Reviewer non-interference

No implementation file was repaired during this review. All four tamper edits
were reverted and hash-verified; `git status --short` is unchanged at 145
entries, and `src/TechInventory.Web/src/lib/api/generated/types.ts` carries
the same SHA-256 it did on arrival.

---

## 11. T104 Revision — Hicks, 2026-09-02 — `VALIDATING`, not self-approved

Revision owner: Hicks (Backend Developer), assigned by Apone's §10 rejection.
Hudson (the original author) is locked out for this cycle; this section is
independently re-derived from `Taskfile.yml`, `scripts/compare-openapi.py`,
both workflow files, and `.specify/memory/constitution.md` §5.8, not from
Hudson's own §9 summary. Team-level record:
`.squad/decisions/inbox/hicks-t104-revision.md`.

**This section reports what Hicks changed and verified. It is not a
re-review — Apone owns the re-review gate. T104 stays `VALIDATING` and T105
remains not authorized to begin until Apone signs off.**

### 11.1 B-1 closed — `check:client-drift` no longer compares two different points in time

`Taskfile.yml`'s `check:client-drift` target now runs
`node scripts/check-client-drift.mjs` instead of
`pnpm run generate:client` + `git diff --exit-code`. The new script
(`scripts/check-client-drift.mjs`):

1. Snapshots the current working-tree `src/lib/api/generated/types.ts` bytes
   in memory.
2. Regenerates the file in place via the exact same
   `pnpm --dir src/TechInventory.Web run generate:client` command a developer
   runs, from the current working-tree `openapi.yaml`.
3. Compares the regenerated content against the snapshot it just took (line-
   ending-normalized, so git's own `core.autocrlf` checkout behavior can never
   manufacture a false "drift").
4. **Always** restores the original snapshot bytes in a `finally` block —
   on a clean pass, on detected drift, and on a failure of the generate
   command itself — so the check never leaves the working tree modified and
   never depends on anything being committed.

This directly fixes the mixed-reference-point defect: the comparison basis is
now "this run's own regeneration vs. this run's own starting point," not
"working-tree regeneration vs. index/HEAD." It also directly answers Apone's
T102 concern — an uncommitted-but-internally-consistent spec change (T102's
`403` additions, already reflected in the client) produces no drift, because
both sides of the comparison are the *same* working-tree snapshot.

**Unit tests** (`scripts/check-client-drift.test.mjs`, 9/9 passing):
`compareArtifacts` pure-function cases (identical content, CRLF/LF
equivalence, real content difference with line-number reporting), and
`checkClientDrift` cases with injected fs/spawn stubs — clean pass (exit 0,
restores once), stale/drifted (exit 1, restores the *original* pre-
regeneration bytes, not the regenerated ones), the "already synchronized
despite an uncommitted spec change" scenario, generate-command failure
(non-zero exit — fails closed, still restores, does not read a possibly
broken regeneration), a spawn error (`pnpm` not found — fails closed, still
restores), and an unexpected throw mid-generation (restore still happens via
`finally`, exception still propagates).

**Live tamper test, this machine:**
- Baseline: `git hash-object` on `types.ts` = `cc678f4e…`. Ran
  `task check:client-drift` — **exit 0**, hash unchanged before/after.
- Tampered `types.ts` directly (appended a bogus exported type) to simulate a
  genuinely stale client — hash became `3829615c…`. Ran
  `node scripts/check-client-drift.mjs` — **exit 1**, "generated client
  differs from a fresh regeneration starting at line 4962" — the stale
  client was correctly caught. Confirmed the file was restored to the exact
  tampered/stale snapshot (`3829615c…`, unchanged by the check itself — no
  silent auto-fix, no dependency on a commit).
- Restored the true original by regenerating from the (untouched)
  `openapi.yaml` — hash returned to `cc678f4e…`, confirmed byte-identical to
  the pre-tamper baseline.
- **Learning, recorded for future tamper tests:** the first restoration
  attempt used `git checkout -- types.ts`, which reverts to the *index/HEAD*
  copy — on this branch, where `types.ts` already carries an uncommitted T102
  change, that silently discarded real uncommitted work instead of restoring
  the pre-tamper state. Recovered by re-running `pnpm run generate:client`
  against the (unmodified) `openapi.yaml`, which reproduced the exact
  original bytes. Tamper-test restoration on a dirty working tree must use a
  file-content snapshot (or re-derive the content, as here), never
  `git checkout --`, when the file being tampered is itself part of the
  branch's legitimate uncommitted state.
- Ran the check again through the full pipeline (`task verify:contracts`,
  `task verify:full`, `task verify`) — `check:client-drift` passed inside
  every one of them, confirmed by `types.ts` hash `cc678f4e…` unchanged at
  the end of each run.

### 11.2 B-2 closed — `check:vulnerable` now fails closed on Moderate+ advisories

`Taskfile.yml`'s `check:vulnerable` target now runs
`node scripts/check-vulnerable.mjs`, which runs
`dotnet list package --vulnerable --include-transitive --format json` — a
stable, versioned machine-readable format the installed .NET SDK (10.0.204)
supports natively, confirmed by direct probe — and parses the documented
`projects[].frameworks[].{topLevelPackages,transitivePackages}[].vulnerabilities[]`
shape. It fails closed:

- any advisory at or above the policy severity threshold → exit 1
- the `dotnet` command failing to launch, or exiting non-zero → exit 1 (a
  failed scan is never read as a clean one)
- output that isn't valid JSON → exit 1 (never silently treated as "no
  vulnerabilities found")

**Policy threshold, not invented:** `.specify/memory/constitution.md` §5.8
already states "`dotnet list package --vulnerable` clean (no Moderate+)".
The script encodes exactly that — Low-severity-only findings are still
printed (nothing hidden) but do not fail the gate; Moderate, High, and
Critical do.

**Unit tests** (`scripts/check-vulnerable.test.mjs`, 13/13 passing): severity
ranking (Low below threshold; Moderate/High/Critical at/above it,
case-insensitive; an unranked severity string fails closed);
`collectVulnerabilities` flattening across `topLevelPackages` and
`transitivePackages`; `parseVulnerabilityReport` against a clean fixture, a
vulnerable-top-level fixture (High), a vulnerable-transitive fixture
(Moderate, the exact policy floor), a Low-severity-only fixture (printed, not
failed), and a malformed-JSON fixture (fails closed with a clear error); and
`evaluateScanResult` against a clean run, **a vulnerable run that must return
a non-zero exit code**, a tool-failure run (`dotnet` exits non-zero), a
spawn-failure run (`dotnet` not found), and a malformed-output run.

**Live tamper test, this machine:**
- `node scripts/check-vulnerable.mjs` against the real repo — **exit 0**,
  "passed — no advisories at or above the policy threshold (moderate+)".
- Built a throwaway probe project (`dotnet new classlib`) pinned to
  `Newtonsoft.Json 12.0.1` — the same package/version Apone used to prove
  B-2 — and ran `node scripts/check-vulnerable.mjs <probe>.csproj` against
  it: printed `HIGH Newtonsoft.Json 12.0.1 … GHSA-5crp-9r3c-p9vr`, then
  **`check:vulnerable: FAILED — 1 advisory(ies) at or above the policy
  threshold (moderate+, constitution.md §5.8)`, exit 1**. The fail-open
  defect is closed: the exact input that previously exited 0 now exits 1.
  Probe project deleted after the test; it never touched this repository's
  tree.

### 11.3 B-3 closed — `ci.yml` now installs PyYAML identically to `quality-gate.yml`

Confirmed by direct search that `ci.yml` and `quality-gate.yml` are the only
two workflows that invoke `task verify` (and therefore
`check:openapi-drift` → `scripts/compare-openapi.py`, which imports `yaml`).
Added a `python3 -m pip install --quiet --disable-pip-version-check pyyaml`
step to `ci.yml`, immediately before its "Run verification pipeline" step,
with the same command `quality-gate.yml:38` already uses and a comment
explaining the shared dependency and cross-referencing this revision. Both
workflows now install PyYAML the same way before the same shared entrypoint —
one truthful setup contract, not two disagreeing ones. `.github/workflows/README.md`'s
CI Pipeline section states this explicitly.

### 11.4 Documentation corrected

`.github/workflows/README.md`: the `verify:contracts` step list, the
Security Gates table's `.NET vulnerability scan` row, and debugging-guide
items 4 and 8 now describe what the checks actually do (parse-and-threshold
for vulnerabilities; snapshot-regenerate-compare-restore for client drift) —
"Enforced" now corresponds to an actual fail-closed check, closing the
ceremonial-gate defect Apone identified.

### 11.5 Full pipeline run — this machine, Windows, no Docker, no browser

| Stage | Result |
| --- | --- |
| `task check:client-drift` (standalone) | **exit 0** |
| `task check:vulnerable` (standalone) | **exit 0** |
| `task check:stale-refs` | **exit 0** — 0/898 tracked files; guard suite 18/18 (new scripts introduce no Playwright references) |
| `task verify:fast` | **exit 0** — format clean, backend build 0 errors, svelte-check 0/0, lint clean, unit **278/278**, frontend **649 tests / 83 files** |
| `task verify:contracts` | **exit 0** — stale-refs 0/898, `check:openapi-drift` exit 0, **`check:client-drift` exit 0** (first time in this work package), `check:migration-drift` exit 0, integration **296 collected** (292 passed, 4 skipped) |
| `task verify:full` | **exit 0** — `verify:fast` + `verify:contracts` + production frontend build + **`check:vulnerable` exit 0** |
| `task verify` (the authoritative alias) | **exit 0**, full end-to-end run: unit 278/278, frontend 649/83, integration 296 collected, `check:client-drift` exit 0, `check:vulnerable` exit 0, production build succeeded |

No stage in any of the above started Docker or a browser. Working tree
confirmed byte-identical to its pre-revision state after every run: `types.ts`
hash `cc678f4e…` unchanged, `git status --short` at its expected 149-entry
count (the pre-existing 145-entry baseline plus the four new script files —
tests and Taskfile/workflow/README edits landed on already-modified tracked
files, adding no new entries).

**GitHub Actions execution remains unobserved** — neither workflow has run
since this revision; that stays `unknown` until a push, disclosed rather than
claimed, per AC-010.

### 11.6 Boundary honored

Touched only: `Taskfile.yml` (two task bodies), `scripts/check-client-drift.mjs`
(new), `scripts/check-client-drift.test.mjs` (new), `scripts/check-vulnerable.mjs`
(new), `scripts/check-vulnerable.test.mjs` (new), `.github/workflows/ci.yml`
(one step added), `.github/workflows/README.md` (descriptions corrected), and
this document plus `plan.md`/`tasks.md`/`coverage-migration.md` as revision
evidence. Did not touch Apone's non-blocking F-5–F-9 findings (none is a
prerequisite to closing B-1/B-2/B-3). Did not start T105. Did not mark T104
`DONE` or authorize T105 — both remain Apone's and the reviewer gate's calls.
No commit, no push.


---

## 12. T104 Re-Review Gate — Apone, 2026-09-02 — **APPROVED**

Reviewer: Apone (Tester / QA). Author under re-review: Hicks (revision owner
assigned by the §10 rejection; Hudson locked out for the cycle). Reviewed
state: commit `b3c092f` on `chore/agentic-development-foundation`, pushed to
origin, working tree clean. **Every result below was produced by the reviewer
on this machine at that commit. No line rests on Hicks's §11 summary.**
Team-level record: `.squad/decisions/inbox/apone-t104-rereview.md`.

**Verdict: APPROVED. T104 is `DONE`. AC-008 is met. T105 is AUTHORIZED to
begin.**

### 12.1 B-1 — CLOSED (comparison basis proven, not read)

`Taskfile.yml`'s `check:client-drift` now runs `scripts/check-client-drift.mjs`,
which snapshots the working-tree artifact, regenerates in place, compares
regeneration against that snapshot, and restores in a `finally`. Four
reviewer-run cases:

| # | Reviewer scenario | Result |
| --- | --- | --- |
| 1 | Clean committed state | **exit 0**, `types.ts` SHA-256 `0EF52149…` unchanged |
| 2 | **Dirty-but-synchronized** — appended a `ZReviewerProbeB1` schema to the working-tree `openapi.yaml`, regenerated `types.ts` (SHA-256 `3BA1F9C5…`); both files now differ from HEAD | **exit 0** — while `git diff --exit-code -- types.ts` (the retired B-1 comparison) **exits 1** on the identical tree. This is the decisive proof: same input, old check red, new check green, and the client genuinely is current |
| 3 | **Genuinely stale client** — kept the probed spec, reverted `types.ts` to its pre-probe bytes | **exit 1**, "differs from a fresh regeneration starting at line 4952 (4961 -> 4964 lines)"; file restored to the exact **stale** bytes (`0EF52149…` before and after) — no silent auto-fix |
| 4 | **Generator failure** — replaced `openapi.yaml` with invalid YAML | **exit 1**, "'pnpm run generate:client' failed (exit code 1) — cannot verify the client is current"; artifact restored byte-identically. Fails closed |

It compares working-tree to working-tree only; git is never consulted for the
comparison. `scripts/check-client-drift.test.mjs` re-run by the reviewer:
**9/9 pass**. `openapi.yaml` and `types.ts` were restored and confirmed
byte-identical to `HEAD` (`be64bf6b…` / `cc678f4e…`).

### 12.2 B-2 — CLOSED (fail-closed proven against real vulnerable inputs)

`check:vulnerable` now runs `scripts/check-vulnerable.mjs`, consuming
`dotnet list package --vulnerable --include-transitive --format json`.
Reviewer-run evidence, throwaway probes created outside the tracked tree and
deleted afterwards:

| # | Reviewer scenario | Raw `dotnet` | New check |
| --- | --- | --- | --- |
| 1 | **Direct** vulnerability — probe pinned to `Newtonsoft.Json 12.0.1` | prints `High … GHSA-5crp-9r3c-p9vr`, **exit 0** | `HIGH Newtonsoft.Json 12.0.1 (topLevelPackages, net10.0)` → "FAILED — 1 advisory(ies) at or above the policy threshold (moderate+, constitution.md §5.8)", **exit 1** |
| 2 | **Transitive** vulnerability — probe on `Newtonsoft.Json.Bson 1.0.1`, pulling `Newtonsoft.Json 10.0.1` | advisory reported | `HIGH … (transitivePackages, net10.0)`, **exit 1** — transitive coverage is real, not just parsed in fixtures |
| 3 | **Tool failure** — nonexistent project path | exit 1 | "exited 1 — treating as a failed scan, not a clean one", **exit 1** |
| 4 | **Clean** — this repository | — | "passed — no advisories at or above the policy threshold (moderate+)", **exit 0** |

Malformed-JSON fail-closed behaviour is covered by fixture, not by live
probe: `scripts/check-vulnerable.test.mjs` re-run by the reviewer, **13/13
pass**, including the malformed-output case, the Moderate-at-the-floor
transitive case, the Low-only non-failing case, and the unranked-severity
fail-closed case. Threshold verified against source authority, not the
revision note: `.specify/memory/constitution.md:188` — "`dotnet list package
--vulnerable` clean (no Moderate+)".

### 12.3 B-3 — CLOSED (no setup contradiction remains)

A workflow-wide search confirms `ci.yml` and `quality-gate.yml` are the only
two workflows that invoke `task verify` (directly or via `scripts/verify.sh`),
and both now run the identical
`python3 -m pip install --quiet --disable-pip-version-check pyyaml` step
immediately before it (`ci.yml:84-85`, `quality-gate.yml:38`). The dependency
is real — `scripts/compare-openapi.py` imports `yaml`. Both files parse as
valid YAML (reviewer-run `yaml.safe_load`). One setup contract, stated the
same way in both places.

### 12.4 Authoritative graph — run end to end at `b3c092f`

| Command | Reviewer result — Windows, no Docker, no browser |
| --- | --- |
| `task verify:fast` | **exit 0** — format clean · backend Release build · svelte-check · ESLint · unit **278/278** · frontend **649 tests / 83 files** |
| `task verify:contracts` | **exit 0** — stale-refs **0 active Playwright references across 933 tracked files** + guard suite 18/18 · `check:openapi-drift` "OpenAPI document is current" · **`check:client-drift` exit 0** · `check:migration-drift` "No changes have been made to the model" · integration **296 collected** (292 passed, 4 skipped) |
| **`task verify`** (authoritative alias) | **exit 0, 5m32s, complete end-to-end run** — every stage above plus production frontend build (`built in 15.15s`) and **`check:vulnerable` exit 0** |

No stage started a container or downloaded a browser; the only integration
host is in-process `WebApplicationFactory<Program>` over real SQLite files.
**The gap recorded in §10.1 — "`task verify` never observed to complete" — is
closed by the reviewer's own run, not by report.**

Guard-coverage note worth keeping: the stale-reference guard now scans **933**
tracked files, up from 901/898, because `specs/004-*` and `.squad/**` are
tracked as of `b3c092f`. The blind spot flagged during T101 — a git-aware
guard reporting "0 references" over a file set that excluded this entire spec
package — is now closed by observation, and the newly visible files are clean.

### 12.5 Single surface, wrappers, floors, competing signals

- **Task remains the single command surface.** `scripts/verify.ps1` and
  `scripts/verify.sh` are unchanged thin wrappers: PATH probe, then
  `task verify`, with zero pipeline logic. `verify.sh` remains unexecuted on
  this host (no bash) — still disclosed, not claimed.
- **Quality Gate invokes the same task.** `quality-gate.yml:39` is a bare
  `task verify`; no command logic is duplicated in the workflow.
- **Floors unchanged and still fail-closed.** `scripts/check-test-floors.mjs`
  hashes `fe45ebbde7c0c62957025868ee73e16817f34b62` — byte-identical to the
  file the reviewer tamper-tested in §10.2 (4 cases across both mechanisms,
  including the decisive all-green-but-59%-collected case). The approved
  behaviour is intact by identity, and it fired correctly in all three runs
  above.
- **No weaker competing success signal.** Trigger audit of all eight
  workflows: only `quality-gate.yml` runs on `pull_request`/`push: main`;
  `ci.yml` is `workflow_dispatch` only; `release-images.yml` /
  `security-scan.yml` are post-merge/manual; the four `squad-*` workflows are
  ops automation. Check name `Quality Gate / verify` is stable for T105.

### 12.6 Not claimed — and verified as not claimed

- **GitHub Actions execution is still unobserved.** `gh run list` for
  `chore/agentic-development-foundation` shows exactly one run at `b3c092f`:
  **Sync Squad Labels** (path-triggered on `.squad/team.md`, success). Quality
  Gate has **not** run; no PR exists for the branch. No document claims it
  passed. AC-010 holds.
- **The constitution/PRD Playwright contradiction remains explicit and
  unresolved.** `docs/testing.md:7-15` still states that PRD §7.5.2–§7.5.4 and
  constitution §6.5.7/§7 name a harness this tree has removed, and that
  amending them requires a separate ADR. `constitution.md` and `docs/prd.md`
  are untouched by T104. `plan.md` L288 and `validation.md` §7.8.5 keep it as a
  **mandatory package-closure precondition**. No T104 file claims AC-008
  satisfies the Playwright-smoke mandate.
- **Document alignment checked, not assumed.** `brief.md`, `plan.md`,
  `tasks.md`, `validation.md`, and `coverage-migration.md` §14 describe the
  same state; the counts they cite (278 / 649 / 296 / floors) match the
  reviewer's own runs.

### 12.7 Findings — NOT blockers, carried to T105

- **F-5 (carried, unchanged):** `scripts/check-security.mjs` is still not a
  Task target; `.github/workflows/README.md:164` labels it
  `Enforced (ci.yml, manual)`, but a `workflow_dispatch`-only workflow plus a
  `--no-verify`-bypassable hook is not enforcement. Add a `check:security`
  task or reword the row.
- **F-10 (new):** `scripts/check-client-drift.test.mjs` and
  `scripts/check-vulnerable.test.mjs` are executed by **no** task —
  `check:stale-refs` runs `node --test` on its own suite inline, these two do
  not. The regression tests protecting the two newly fail-closed guards can
  rot unobserved. Wire them into the pipeline in T105.
- **F-11 (new):** `check:vulnerable`'s malformed-output path is fixture-proven
  only; no live probe produced unparseable `dotnet` output. Acceptable, but
  T105's guard matrix should cover it end to end.
- **F-12 (new):** §11.6 and `coverage-migration.md` §14.4 state "No commit was
  made and nothing was pushed" — true when written, now superseded by the
  coordinator checkpoint `b3c092f`. Reconcile at package closure.
- **F-13 (new):** the only green check currently visible on this branch is
  **Sync Squad Labels**. T105's required-check enumeration must ensure an ops
  workflow's success can never be read as verification.
- **F-6 – F-9 (carried unchanged):** repeated `restore` resolution; missing
  `permissions:` on the `verify` job; no numeric coverage gate; the dropped
  empty-output guard from `check-openapi-drift.sh`.

### 12.8 Reviewer non-interference

No implementation file was repaired, and none was left modified. Every tamper
was reverted and hash-verified: `openapi.yaml` `be64bf6b…` and
`src/TechInventory.Web/src/lib/api/generated/types.ts` `cc678f4e…`, both
identical to `HEAD:b3c092f`; `git status --porcelain` shows only the
reviewer's own scratch directory, which was deleted at the end of the review.
Both throwaway vulnerability probes were deleted; neither was ever tracked.

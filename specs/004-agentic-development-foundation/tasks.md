---
id: 004-agentic-development-foundation
document: tasks
tier: T2
status: T001–T004 DONE; T103 DONE; T102 DONE and REVIEWER-APPROVED (Ripley, 2026-09-02 — `validation.md` §6); T101 DONE and REVIEWER-APPROVED (Ripley re-review, 2026-09-02 — `validation.md` §7.8) after REJECTION (`validation.md` §7) and Apone's revision (`coverage-migration.md` §13.9) closing blockers B1/B2; T104 **DONE and REVIEWER-APPROVED** (Apone re-review at `b3c092f`, 2026-09-02 — `validation.md` §12) after REJECTION (`validation.md` §10) and Hicks's revision (`validation.md` §11) closing B-1/B-2/B-3 — AC-008 met; T105 **DONE and REVIEWER-APPROVED** (Bishop final gate, 2026-09-02 — `validation.md` §16) after three rejections (§13, §14, §15) and revisions by Hicks (B-1/B-2), Hudson (B-3/B-4), Scribe (B-5) and Vasquez (B-6) — AC-009 met; merge readiness separate and still open (§16.8)
approved_by: briandenicola
approved_at: 2026-09-02T10:06:57-05:00
revised_at: 2026-09-02 (T102 completed and evidence consolidated by Apone —
  `coverage-migration.md` §12, `validation.md`); re-revised 2026-09-02 by
  Bishop after Ripley rejected Apone's consolidation for findings B1 (Viewer
  could mutate reference-entity data), B2 (C-18 falsely recorded as fully
  done), and B3 (Viewer could claim/release device ownership) — see
  `coverage-migration.md` §12 revision note and `t102-bishop-revision.md`
  (session artefact); re-revised again by Hicks after Ripley's second
  REJECTED verdict for findings B3 (Bishop's fix left a third, ungated
  `canClaim`/`canRelease` copy in `devices/[id]/+page.svelte`), B2-R (C-04/
  C-12 evidence and the 645 test count were overstated), and B4 (26, not 21,
  `AdminOrMember`-gated operations undocumented for `403`) — see
  `coverage-migration.md` §12 second revision note and
  `t102-hicks-final-revision.md` (session artefact). **Final reviewer gate
  2026-09-02: Ripley re-ran every cited command directly, tamper-tested the new
  contract guard, and returned APPROVED — `validation.md` §6. T102 is complete;
  T101 is authorized to begin.**
base_sha: d303cd6537392e2489222d5a0d5c946f39f2af0c
---

# Tasks — Agentic Development Foundation

Scope boundaries and rationale live in [`plan.md`](./plan.md) §3 and are not
restated here. A task is `[x]` **only** when its acceptance evidence is recorded
in [`validation.md`](./validation.md).

**Playwright is retired** (`brief.md` §2.1, approved by `briandenicola`
2026-09-02). No task below proposes PR-blocking, scheduled, release or optional
automated browser execution: **there is no future automated Playwright role.**
**Sequence: T103 → T101 / T102 → T104 → T105.**

| Task | Title | State | Tier | Depends on | AC |
| --- | --- | --- | --- | --- | --- |
| T001 | Build four-repository evidence matrix | `DONE` | T2 | — | AC-001 |
| T002 | Inventory and classify Tech Inventory authority sources | `DONE` | T2 | — | AC-002 |
| T003 | Document PR #140 / #89 control-failure chain | `DONE` | T2 | — | AC-003 |
| T004 | Record approved first principles and work-state model | `DONE` | T2 | T001–T003 | AC-004 |
| T103 | Coverage migration matrix and deletion map | `DONE` | T2 | — | AC-007 |
| T101 | Retire the broken Playwright harness safely | `DONE` · rejected → revised by Apone → **reviewer-APPROVED** (Ripley, `validation.md` §7.8) | T2 | T103 | AC-005 |
| T102 | Migrate valuable coverage to lower reliable layers | `DONE` · **reviewer-APPROVED** | T2 | T103 | AC-006 |
| T104 | One authoritative verification interface, Playwright-free | **`DONE`** — rejected by Apone (`validation.md` §10), revised by Hicks closing B-1/B-2/B-3 (`validation.md` §11), **independently re-reviewed and APPROVED by Apone at `b3c092f`** (`validation.md` §12); AC-008 met | T2 | T101, T102 | AC-008 |
| T105 | Align required GitHub checks and tamper-test guards | **`DONE` — REVIEWER-APPROVED at the final independent reviewer gate** (Bishop, 2026-09-02 — `validation.md` §16) · rejected three times and revised each time: B-1/B-2 (Hicks), B-3/B-4 (Hudson), B-5 (Scribe), **B-6 (Vasquez)** — all six closed and re-verified · B-6 literal split across backtick boundaries, non-matching, meaning preserved, count surface named; **0** matches across all **940** tracked + untracked non-ignored files; with the **complete change set staged**, `check:security --repo` → exit 0 over **940** files and **full `task verify` → exit 0**; index reset with **30/30** files byte-identical; paired probes prove the scanner still blocks the real call and no exemption was added · **AC-009 met.** Merge readiness is separate and still open (`validation.md` §16.8) | T3 | T104 | AC-009 |

---

## Phase 1 — Research and Record · COMPLETE

### T001 — Build four-repository evidence matrix · `DONE` · AC-001

- [x] Default-branch SHAs pinned → `evidence.md` §1 (`d303cd6`, `b40b739`,
      `50a71fd`, `0fccf9b`); every claim cited `owner/repo:path@sha`
- [x] All nine dimensions compared → §2.1 branch protection (only Aurearia is
      protected) · §2.2 local/CI parity (**none of the four** has it) · §2.3
      contract drift · §2.4 test boundaries · §2.5 governance volume (377×) ·
      §2.6 feature/ADR structure · §2.7 code surface · §2.8 migration validation
      · §2.9 safeguards (**no repo detects a zero-test-collection run**)
- [x] Inaccessible settings marked `unknown`, never inferred → §6, U-01…U-08

**Evidence:** `evidence.md` §1, §2, §4, §6, §7. **Unknowns:** U-01…U-03, U-06.

### T002 — Inventory and classify authority sources · `DONE` · AC-002

- [x] 28 sources catalogued — constitution, Copilot/Squad instructions, skills,
      specs, plans, tasks, ADRs, decision ledger, state/handoff, PR template,
      CODEOWNERS, Taskfile, scripts, workflows — each with purpose, audience,
      conflicts, freshness, enforcement class and a recommendation → §3.3
- [x] Volume quantified → §3.1 (254 files / 30,184 lines / 194,001 words);
      duplication (§3.5, six pairs) and stale sources (§3.3) identified

**Key finding:** all 28 sources are class `ADVISORY` (§3.4); the constitution is
1.9% of the corpus it governs and 37.4% of that corpus is history.
**Evidence:** `evidence.md` §3. **Unknowns:** U-04, U-05.

### T003 — Document PR #140 / #89 control-failure chain · `DONE` · AC-003

- [x] Timestamped chain confirmed → `evidence.md` §5.1: Quality Gate green
      (run 33641764904, *required* status `unknown` — U-02); merge at `d303cd6`
      `2026-09-02T14:27:09Z`, 0 reviews, 51 files, +5,233/−713; run 33641758342
      aborts on `TypeError: test.todo is not a function` **10 seconds later**
- [x] Zero F045 browser tests executed → §5.3 (409 lines, 22 `test(...)`,
      6 projects, **0 executed**); #89's second defect — `seedDevice()` schema
      drift — documented, never reached because collection aborts first
- [x] Every control that should have blocked named with its reason → §5.4,
      **14 controls** with citations

**Evidence:** `evidence.md` §5. **Unknowns:** U-01, U-02, U-07, U-08.

### T004 — Record first principles and work-state model · `DONE` · AC-004

- [x] Seven non-skippable work states → `plan.md` §2.1 · discussion never
      authorizes implementation §2.2 · agents cannot self-approve §2.3
- [x] Scaled ceremony §2.4 — **T0 = the request, T1 = issue + mini-plan,
      T2/T3 = full work package**, **preserved unchanged by this revision**
- [x] ENFORCED / REVIEWED / ADVISORY with no fourth state §2.5 · tests at the
      lowest reliable layer §2.6 (amended 2026-09-02: no browser-automation
      layer; irreducible browser risk becomes an owned manual checklist)
- [x] Local/CI command parity §2.7 · acceptance-evidence completion §2.8 ·
      history is not instruction §2.9 · explicit visible exceptions §2.10

**Evidence:** `plan.md` §2.1–§2.10.

---

## Phase 2 — Implementation · T103, T102, **T101 DONE** (T101 rejected → revised by Apone → reviewer-APPROVED, `validation.md` §7.8); **T104 DONE** (rejected by Apone → revised by Hicks → reviewer-APPROVED at `b3c092f`, `validation.md` §12), T105 APPROVED and AUTHORIZED, NOT STARTED

> **T103 is complete.** Its deliverable is
> [`coverage-migration.md`](./coverage-migration.md) — the deletion authority
> for T101 and the work list for T102. An earlier T103 run had been dispatched
> against the *pre-retirement* brief, which asked for a right-sized **retained**
> Playwright suite; it was stopped, and its artefact `e2e-classification.md`
> carried no authority (`validation.md` §3.7). **That file has been deleted**
> and none of its conclusions, identifiers, or recommendations is carried
> forward; `coverage-migration.md` is a fresh analysis of the working tree at
> `d303cd6`. **No file, test, config, workflow, script or repository setting
> outside this work package has been changed.**

### T103 — Coverage migration matrix and deletion map · `DONE` · AC-007

Analysis only, and it precedes all file deletion (T101) and test authoring
(T102). The matrix is T101's deletion authority and T102's work list; it records
no gate and deletes nothing — its deliverable is a document.

- [x] Inventory all 17 spec files (15 `journeys/`, `security/token-storage`, and
      `theme-fouc.spec.ts` outside `testMatch`) with test counts, including the
      19 unsupported `test.todo` declarations, reconciled against a recorded
      `--list` attempt → `coverage-migration.md` §2.1–§2.2: 28 tracked files;
      52 `test(` + 19 `test.todo` + 1 `test.fixme`; `npx playwright test --list`
      → **exit 1, `Total: 0 tests in 0 files`, 4 × `TypeError`**; with 09–12
      excluded → **`Total: 360 tests in 12 files`** (60 × 6 projects)
- [x] For every spec — and every test where they differ — record the behaviour
      asserted and judge it **valuable** or not, with reasons, on observed
      product behaviour rather than on what a spec's title claims → §3.1–§3.17,
      one sub-table per spec. Five assertions are judged **not** valuable and
      dropped with the reason recorded in their row
- [x] Assign each valuable behaviour a **destination layer** — real HTTP
      integration/contract · component test · manual validation checklist ·
      accepted gap — naming the replacement, or the owner accepting the gap.
      **Every removed test names replacement coverage or an explicitly accepted
      manual gap; no spec is deleted without a row** → §3, §6 (5 `H-`, 22 `C-`),
      §7 (15 `M-`), §8 (8 `G-`, owner `briandenicola`)
- [x] Decide stubs 09–12 individually, on evidence, and **not** on any
      assumption that they must be preserved as browser tests → §3.9–§3.12: 09
      is ~80% already live in `ExportControllerTests`; 10 is covered by the
      reference controller suites bar one cache-invalidation item; 11 needs a
      `ViewerRoleIntegrationTestFactory` (**H-04**); 12 is the only genuinely
      browser-bound stub, and one of its five items is aspirational (the product
      has no offline queue — mutations are `NetworkOnly`)
- [x] Resolve `evidence.md` U-08 — `tests/e2e/theme-fouc.spec.ts` outside
      `testMatch` — as a matrix row like any other → §3.17: **collected 0**
      (verified by `--list`), no recorded reason for the placement, **and** the
      spec is drifted (seeds `ti.userPrefs.v1.*` while `app.html` reads
      `theme-preference`). **U-08 closed**
- [x] Preserve PRD §7.5.4 journey traceability: every PRD journey maps to its
      post-retirement layer or a named accepted gap. Surface any required PRD
      amendment as an **ADR candidate, not an authorization** → §9: all 13
      journeys re-homed, none removed; the PRD §7.5.2–§7.5.4 and constitution
      §6.5.7/§7 amendment is surfaced as an ADR candidate only (§5.4)

**Evidence:** [`coverage-migration.md`](./coverage-migration.md) §1–§11;
`validation.md` §1 AC-007 and §5. **Unknowns closed:** U-08.
**Additional findings recorded for T101/T102:** two independent contract drifts
(`seedDevice()` missing required `OwnerId`/`LocationId`; the theme spec's storage
key) — §2.3; eight orphaned "deferred to E2E" comments in the surviving Vitest
suite — §5.2 (**C-22**); `docs/threat-model.md` V4.1.2 and
`docs/security-baseline.md` cite a "Playwright test #11" that **never existed as
executable code** — §3.11; the stale-reference guard must exempt history paths
(§5.5) and the `@vitest/browser-playwright` optional peer in
`src/TechInventory.Web/pnpm-lock.yaml` (§5.6).

### T101 — Retire the broken Playwright harness safely · **`DONE`** (rejected → revised by Apone → reviewer-APPROVED) · AC-005

> **Reviewer gate 2026-09-02 (Ripley): REJECTED — 2 blockers.** The self-recorded
> `DONE` below is **withdrawn**. Deletions, guard design, tamper behaviour,
> clean-install proof and issue #89 all verified good; **B1** —
> `.github/T47-CI-SETUP-CHECKLIST.md` L158/L185–192/L244 still instruct
> `task test:e2e` and `task test:e2e:run`, which no longer exist; **B2** — the
> guard's blanket `specs/` exemption hides ten unchecked backlog promises to
> write new Playwright tests. Full verdict and evidence:
> [`validation.md`](./validation.md) §7. Revision owner: **Apone** (Hudson
> locked out). **T104 must not begin until this is closed.**

> **Revision 2026-09-02 (Apone): both blockers closed, pending Ripley
> re-review.** **B1** — the stale `task test:e2e`/`task test:e2e:run`
> instructions and the E2E readiness-troubleshooting block are removed from
> `.github/T47-CI-SETUP-CHECKLIST.md`; the `verify.sh`/`verify.ps1` and
> reference-table descriptions now name the actual current pipeline (no
> "ends in E2E" claim, and no claim that T104's future unified verification
> surface already exists). **B2** — the guard's `EXEMPT_PATH_PREFIXES` no
> longer contains a blanket `specs/` entry; a named `EXEMPT_SPEC_PATHS`
> allowlist covers only the twelve historical/work-package files in
> `coverage-migration.md` §5.5. A repository-wide audit found **16**
> `specs/_backlog/**` files (not just Ripley's ten-file sample) carrying a
> Playwright reference; every one was rewritten to name a real
> destination — HTTP integration test, Vitest component test, a
> `docs/testing/manual-pwa-validation.md` addition, or a declared accepted
> gap — never left unautomatable and never silently dropped (full per-file
> table: `coverage-migration.md` §5.5a). Guard tests grew 15→**18/18
> pass**, including a case proving a synthetic `specs/_backlog/**` fixture
> fails and a case proving an unlisted `specs/` path also fails. Live guard:
> **0/901**. Tamper-tested against a real, then byte-identically restored,
> `specs/_backlog/F031-merge-reference-data.md` — full record:
> `coverage-migration.md` §13.9. **`constitution.md`/`docs/prd.md` untouched**
> (Ripley's package-closure precondition, ADR-gated, not this revision's
> scope). T104 was **not** started.

> **Re-review gate 2026-09-02 (Ripley): APPROVED — T101 is `DONE`, AC-005 is
> met, and T104 is authorized to begin.** Both blockers verified closed by the
> reviewer directly, never from the revision summary: `.github/T47-CI-SETUP-CHECKLIST.md`
> re-read end to end (no `task test:e2e`/`:run`, no readiness-troubleshooting
> block, `verify` descriptions match `verify.sh:47-49`/`verify.ps1:54-57`);
> all twelve `EXEMPT_SPEC_PATHS` entries audited and confirmed closed-phase
> records; an independent whole-disk case-insensitive scan found **56** files
> carrying the word (39 tracked), **zero** under `specs/_backlog/**` and zero
> in any manifest, script, workflow, config, instruction or executable test;
> the three flipped checkboxes verified against real tests by name. Reviewer
> re-ran the guard (**0/901**), the guard tests (**18/18**), and **three**
> tamper tests — an active `specs/_backlog/**` file, a brand-new unlisted
> `specs/005-*` path, and a structural `tests/e2e/` revival — each exit 1 with
> the exact file:line, each restored byte-identically, `git status
> --porcelain` diffed to 0 lines against the pre-review baseline. Full verdict,
> the constitution/PRD package-closure precondition, the condition attached to
> T104's authorization, and findings F-1..F-4:
> [`validation.md`](./validation.md) §7.8.


- [x] Confirm the T103 matrix covers **every** spec, then remove
      `tests/e2e/**` and `playwright.config.ts` · *evidence:* `git ls-files
      tests/e2e` (pre-deletion) returned exactly the 28 tracked files matching
      matrix rows D1–D5 (journeys, fixtures, pages, security, config, package
      files, README); `tests/e2e/node_modules` and
      `tests/e2e/playwright-report` were already gitignored/untracked.
      Deleted: `tests/e2e/` (28 files), `scripts/run-e2e.ps1`,
      `scripts/run-e2e.sh`, `.squad/skills/playwright-e2e-scaffolding/`
      (D1–D5, D8 — no undocumented deletions found; nothing outside the
      matrix was removed)
- [x] Remove the Playwright dependency and browser install from `package.json` /
      `pnpm-lock.yaml`, and every invocation in `Taskfile.yml`,
      `scripts/run-e2e.*`, `scripts/verify.*` and `.github/workflows/**` ·
      *evidence:* only `tests/e2e/package.json` (deleted) ever declared
      `@playwright/test`/`@axe-core/playwright`; root and
      `src/TechInventory.Web/package.json` never depended on Playwright.
      `pnpm-lock.yaml`'s only remaining Playwright-named string is
      `@vitest/browser-playwright`, vitest's own **optional peer dependency**
      (never installed, `coverage-migration.md` §5.6). `Taskfile.yml` lost
      `test:e2e:run`/`test:e2e` and gained `check:stale-refs`.
      `scripts/verify.ps1`/`scripts/verify.sh` step 9/9 now runs the guard
      instead of Playwright. `scripts/run-e2e.*` deleted outright.
      `.github/workflows/ci.yml`'s muting comment reworded off Playwright.
      Clean-install proof: `Remove-Item -Recurse -Force node_modules` then
      `pnpm install --frozen-lockfile` in `src/TechInventory.Web` — 617
      packages resolved, **0 downloaded**, no `@playwright/test`/`playwright`
      package present after install, and the pre-existing
      `%LOCALAPPDATA%\ms-playwright` browser cache directory's mtime was
      unchanged by the install (dated 2026-08-24, before this work)
- [x] Remove Playwright from every **verification promise** — `docs/testing.md`,
      `.github/pull_request_template.md`, `.copilot/skills/test-discipline`,
      `.github/T47-CI-SETUP-CHECKLIST.md`, `README`/onboarding · *evidence:*
      repo-wide guard scan (`node scripts/check-stale-playwright-references.mjs`)
      returns **0 active references across 901 tracked files**;
      `.github/pull_request_template.md` and
      `.copilot/skills/test-discipline/SKILL.md` already had no Playwright
      mentions (verified directly, not just via the guard). Edited for
      verification-promise removal: `docs/testing.md` (full rewrite of the
      E2E/Accessibility sections, ToC, Quick Start, auth table, debugging,
      flaky policy, "Writing a New Critical Journey"), `README.md` (Quick
      Start + command table), `.github/copilot-instructions.md`,
      `.github/T47-CI-SETUP-CHECKLIST.md`, `.github/workflows/README.md`,
      `docs/security-baseline.md`, `docs/threat-model.md`,
      `docs/auth-design.md`, `docs/known-issues.md` (t23 entry fully
      resolved — the underlying jsdom-diagnosis bug was wrong; corrected, not
      just de-referenced), `.gitignore`, `.dockerignore`,
      `src/TechInventory.Web/eslint.config.js`, five `.squad/**` files
      (`agents/apone/charter.md`, `agents/vasquez/charter.md`, `routing.md`,
      `team.md`, `templates/machine-capabilities.md`,
      `skills/token-storage-inspection/SKILL.md`, rewritten around the
      unit-level assertion pattern)
- [x] Add a **stale-reference guard** that fails the build if a Playwright
      reference returns to manifests, scripts, workflows, or test trees ·
      *evidence:* `scripts/check-stale-playwright-references.mjs` — keyword
      scan across all git-tracked files plus two independent structural
      hard-fails (any tracked path under `tests/e2e/`, any `playwright.config.*`
      file anywhere) that don't rely on the keyword at all. Wired into
      `Taskfile.yml` (`check:stale-refs`) and `scripts/verify.ps1`/`verify.sh`
      step 9/9 — the nearest existing verification surface, not a new T104
      interface. Repository-native test coverage (not tamper-tested — that is
      T105's job, not claimed here): `scripts/check-stale-playwright-references.test.mjs`,
      15 `node:test` cases at Hudson's original recording, **now 18/18** after
      Apone's revision narrowed the `specs/` exemption (two cases added
      proving a `specs/_backlog/**` fixture and an unlisted `specs/` path both
      fail — `coverage-migration.md` §13.9); live run against the repo:
      **0 active references / 901 tracked files**
- [x] Close issue #89 citing **retirement + migration** — never
      repair-for-continued-use — linking the T103 matrix and T102 replacements;
      #89 stays open until both are complete. Close `evidence.md` U-07 as
      **moot**: the suite is retired, not repaired, so "does it pass once
      fixed?" has no subject · *evidence:* `validation.md` U-07 closed as moot
      below; issue #89 closed via `gh issue close 89` citing this task's
      evidence (see PR/issue comment for the exact citation)

### T102 — Migrate valuable coverage to lower reliable layers · `DONE` · AC-006

- [x] **Real HTTP integration/contract tests** for API behaviour, authorization,
      export, reference-data mutation, CRUD and serialization — real app + real
      SQLite, no mocked API, non-2xx failing loudly · *evidence:* H-01–H-05
      (`coverage-migration.md` §12.1 names the exact test methods —
      `t102-backend-results.md` is a session artefact, not present in this
      repository); targeted run 45/45; full integration suite **292 passed /
      4 skipped / 296 total, 0 failed** as re-run by Ripley at the final
      review (the earlier "240/245" figure was the count immediately after the
      first Viewer authorization fix and is superseded) —
      `coverage-migration.md` §12.1, `validation.md` §6
- [x] **Component tests** (Vitest + Testing Library + axe-core) for Svelte
      rendering, state, navigation affordances, and accessibility · *evidence:*
      C-01–C-17, C-19, C-21, C-22 (20/22) named per-file in
      `coverage-migration.md` §12.2 (`t102-frontend-results.md` is a session
      artefact, not present in this repository);
      re-verified `pnpm vitest run --no-file-parallelism` 83/83 files,
      **649/649** tests (previously re-cited as 645/645 — corrected; **C-04**/
      **C-12** are genuinely Done only as of the 3 new
      `devices/[id]/page.test.ts` cases added for Ripley's second-review B3
      finding, see `coverage-migration.md` §12 second revision note),
      `pnpm run check` 0 errors, `pnpm run lint` clean, `pnpm run build`
      succeeds — `coverage-migration.md` §12.2. **C-18 is 5 of 6 route axe
      harnesses done, not 6 of 6** (no `/devices` list-route harness was ever
      authored — Ripley B2 finding, corrected); the `/devices` route is
      recorded as accepted gap **G-09**. C-20 recorded as an accepted gap with
      rationale, not a failure
- [x] **Manual validation checklist** for PWA install/offline/browser-engine
      behaviour that cannot be automated without Playwright — **named owner,
      release cadence, recorded as an explicit gap**, never a green automated
      claim · *evidence:* [`docs/testing/manual-pwa-validation.md`](../../docs/testing/manual-pwa-validation.md)
      (15 checks, owner `briandenicola`, `REVIEWED`, not merge-blocking) —
      `coverage-migration.md` §12.3
- [x] Replace the E2E seed-fixture drift risk with **typed HTTP
      integration/request builders or generated-contract checks**, so drift is a
      compile error. `tests/e2e/fixtures/api.ts` is **deleted, not repaired** ·
      *evidence:* H-01 uses full-field typed request payloads in
      `DevicesControllerTests`/`SharePointCsvImportTests` asserting against
      `CreateDeviceRequest`, so a missing `OwnerId`/`LocationId` fails to
      compile; `tests/e2e/fixtures/api.ts` remains scheduled for deletion under
      T101 (file deletion is T101's scope, not T102's)
- [x] Reconcile the matrix: every valuable row is live at its destination layer,
      or listed as an accepted gap with an owner · *evidence:* every `H-`, `C-`,
      `M-`, and `G-` identifier has exactly one disposition —
      `coverage-migration.md` §12.4

**Evidence:** `coverage-migration.md` §12; `validation.md` §1 AC-006 and §5;
`docs/testing/manual-pwa-validation.md`. **Viewer authorization defect and fix**
recorded as evidence of real-HTTP-replacement value, not scope drift: the
durable evidence is `H-04`/`ViewerRoleAuthorizationTests`
(`coverage-migration.md` §12.1) — `viewer-auth-fix-results.md` is a session
artefact, not present in this repository — `briandenicola` explicitly
authorized the tightly coupled production fix before it was made. **Ripley's B1 review finding**
found the identical bare-`[Authorize]` defect survived on ordinary
Brands/Categories/Locations/Networks/Tags/Owners create/update/delete; the
same, already-approved `AdminOrMember` enforcement was applied consistently
to those six controllers, the permanently-skipped
`AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden` (which
never executed and had been miscited as coverage) was removed, and real
403-Viewer coverage for all six controllers was added to
`ViewerRoleAuthorizationTests` — see `t102-bishop-revision.md` (session
artefact). **Ripley's second re-review (REJECTED)** found Bishop's B3 fix
incomplete — `devices/[id]/+page.svelte` held a third, independently
ungated `canClaim`/`canRelease` copy — and this section's C-04/C-12/645-test
evidence still overstated. Hicks gated the third surface, added 3 route-level
Viewer/Admin/Member cases to `devices/[id]/page.test.ts`, corrected the test
count to 649, added the missing `[ProducesResponseType(403)]` to all 26 (not
21) `AdminOrMember`-gated operations, regenerated `openapi.yaml` and
`types.ts`, and added `OpenApiDriftTests.AdminOrMemberGatedOperation_
DeclaresForbiddenResponse` (26 cases) — see `coverage-migration.md` §12
second revision note and `t102-hicks-final-revision.md` (session artefact).

### T104 — One authoritative verification interface, Playwright-free · **`DONE` — REJECTED, REVISED, then independently RE-REVIEWED and APPROVED** · AC-008

> **Re-review gate 2026-09-02 — Apone: APPROVED** (`validation.md` §12;
> `.squad/decisions/inbox/apone-t104-rereview.md`), reviewing commit
> `b3c092f`. All three blockers verified closed by reviewer-run evidence, not
> from Hicks's summary: **B-1** — `check:client-drift` passes a
> dirty-but-synchronized working tree that the retired `git diff --exit-code`
> comparison fails on the identical tree, fails a genuinely stale client at
> the exact line, and restores byte-identically on pass, on drift, and on
> generator failure; **B-2** — `check:vulnerable` exits 1 on real **direct**
> *and* **transitive** vulnerable probes where the bare `dotnet list package`
> command exits 0, and fails closed on tool failure; **B-3** — `ci.yml` and
> `quality-gate.yml` are the only workflows invoking `task verify` and both
> install PyYAML identically. **`task verify` was observed by the reviewer to
> run end to end and exit 0 (5m32s, no Docker, no browser)** — the §10.1 gap
> is closed. **T104 is `DONE`; AC-008 is met; T105 is AUTHORIZED to begin.**
> Not claimed: GitHub Actions execution (only the ops workflow `Sync Squad
> Labels` has run at `b3c092f`) and the constitution/PRD Playwright
> contradiction, which remains an explicit package-closure precondition.
> Findings F-5, F-10–F-13 carried to T105, none blocking.

> **Reviewer gate 2026-09-02 — Apone: REJECTED** (`validation.md` §10;
> `.squad/decisions/inbox/apone-t104-review.md`). Most of the surface is
> genuinely working and was re-run by the reviewer, and the collected-test
> floors were tamper-tested and **approved**. Three blockers stood:
> **B-1** `check:client-drift` (`Taskfile.yml:72-77`) regenerates from the
> working-tree `openapi.yaml` but diffs against the index/HEAD `types.ts`,
> so it fails with provably zero drift (`types.ts` SHA-256 identical before
> and after regeneration) — `task verify` had therefore **never been
> observed to complete**; **B-2** `check:vulnerable`
> (`Taskfile.yml:101-104`) cannot fail — `dotnet list package --vulnerable`
> exits 0 on a HIGH advisory (proven) — while
> `.github/workflows/README.md:161` calls it "Enforced"; **B-3**
> `.github/workflows/ci.yml` depends on PyYAML without installing it, unlike
> `quality-gate.yml:38`. **Revision owner: Hicks.** Hudson is locked out for
> this cycle. **T105 is not authorized to begin.**

> **Revised by Hicks, 2026-09-02** (`validation.md` §11;
> `.squad/decisions/inbox/hicks-t104-revision.md`). All three blockers
> closed: B-1 via `scripts/check-client-drift.mjs` (snapshot-regenerate-
> compare-restore, no longer index/HEAD-based, 9/9 new unit tests); B-2 via
> `scripts/check-vulnerable.mjs` (JSON-format parsing, fails closed on any
> Moderate+ advisory per constitution.md §5.8, 13/13 new unit tests,
> live-tamper-verified against a real Newtonsoft.Json 12.0.1 probe); B-3 via
> an identical PyYAML install step added to `ci.yml`. `task verify` (the
> authoritative alias) ran end-to-end and **exited 0** on this machine — the
> gap Apone recorded is closed. **Not self-approved.** Apone owns the
> re-review gate; T105 remains not authorized until that gate passes.

> **Implemented by Hudson, 2026-09-02** — evidence recorded in `validation.md`
> §9. Both conditions attached at authorization were honored, not
> resolved: (1) no claim in this work asserts the new verification surface
> satisfies constitution §9/§6.5.14/L402/L442's Playwright-smoke mandate —
> the ADR + constitution/PRD amendment remains an open package-closure
> precondition (`validation.md` §7.8.5); (2) F-4 is closed — the
> stale-reference guard now runs in `.github/workflows/quality-gate.yml` —
> and `docker-compose.e2e.yml`/`.env.e2e` were deleted (no real non-browser
> role), not merely revised.

- [x] Make Task the single entrypoint humans and CI both invoke (`Taskfile.yml`
      is referenced by **zero** workflows) · *evidence:* `Taskfile.yml`
      `verify:fast`/`verify:contracts`/`verify:full`/`verify`;
      `.github/workflows/quality-gate.yml`'s `verify` job and `ci.yml` both
      call `task verify` (the latter via `scripts/verify.sh`); `verify.ps1`
      and `verify.sh` are thin wrappers with no pipeline logic of their own —
      `validation.md` §9.1, §9.2
- [x] Cover **format · build · type-check · lint · unit · component · HTTP
      integration · contract-drift · migration** in that one surface, with a
      recorded collected-test floor per surviving suite · *evidence:* the task
      definitions + a full run log with counts — `validation.md` §9.1, §9.3;
      `task verify:fast` full run PASS; `task verify:contracts` full run
      **PASS**, including `check:client-drift` (rewritten by Hicks —
      `validation.md` §11.1, §11.5)
- [x] Delete the browser stage and the readiness-poll duplication that existed
      for it (written 4×) · *evidence:* already delivered by T101/T102;
      T104 additionally deleted `.env.e2e`, `docker-compose.e2e.yml`, and the
      now-duplicate `scripts/check-openapi-drift.sh` — `validation.md` §9.6, §9.7
- [x] Verify runs to completion on a **clean checkout with no browser
      download**; state which stages need Docker and whether local verify is
      partial, declaring platform-forced divergence inline · *evidence:* **no
      stage of `verify:fast`/`verify:contracts`/`verify:full`/`verify`
      requires Docker or a browser** (`validation.md` §9.5, §11.5);
      `verify:contracts` initially halted at `check:client-drift` (B-1, a
      false failure — `validation.md` §10.3), which Hicks's revision closed —
      `task verify` (the authoritative alias) now runs end-to-end and exits 0
      (`validation.md` §11.5)
- [x] **Revision (Hicks, 2026-09-02):** close Apone's three reviewer
      blockers without touching any confirmed-correct portion of the
      surface · *evidence:* `scripts/check-client-drift.mjs` (+ 9/9 unit
      tests), `scripts/check-vulnerable.mjs` (+ 13/13 unit tests), `ci.yml`
      PyYAML step, `README.md` corrections — `validation.md` §11; live tamper
      tests against a real stale-client edit and a real Newtonsoft.Json
      12.0.1 vulnerable probe, both restored/cleaned up; full `task verify`
      run exited 0. **Not self-approved** — awaiting Apone re-review.
- [x] **Re-review gate cleared (Apone, 2026-09-02, commit `b3c092f`):** all
      three blockers independently re-verified closed and the authoritative
      graph observed complete · *evidence:* `validation.md` §12 — B-1's four
      reviewer-run cases (clean · dirty-but-synchronized passing where
      `git diff --exit-code` fails · genuinely stale caught at line 4952 ·
      generator failure fail-closed, all restored byte-identically), B-2's
      direct **and** transitive live vulnerable probes both exiting 1 where
      the bare `dotnet list package` exits 0, B-3's two-workflow PyYAML
      parity, checker suites 9/9 and 13/13, and **`task verify` exit 0 end to
      end in 5m32s with no Docker and no browser**

### T105 — Align required GitHub checks and tamper-test guards · **`DONE` — REVIEWER-APPROVED** · AC-009 **met**

> **Final reviewer gate 2026-09-02 (Bishop): APPROVED — T105 is `DONE`, AC-009 is satisfied.**
> Full record: [`validation.md`](./validation.md) §16. This supersedes the three prior rejection
> preambles below, which are retained as the audit trail.
>
> **B-6 closed.** `t105-setup-revision.md:258` now splits the literal across backtick boundaries in
> the same style as the other five sites — reviewer-confirmed non-matching against the scanner's own
> pattern, with every diagnostic fact preserved (which pattern, which files, which guard caught it,
> that the payload was deliberately omitted). The misleading count is corrected in place to
> "exit 0 (933 **tracked** files clean)", naming its surface. An independent regex sweep of the
> scanner's pattern across all **940** tracked + untracked non-ignored files returns **0 matches**.
>
> **The proof the prior gate demanded.** The complete intended change set — 23 modified tracked
> files and all 7 untracked deliverables — was staged so `git ls-files` reported **940** rather than
> 933, and then: `check-security.mjs --repo` → **exit 0**, `Security scan (repo) passed for 940
> file(s)`; **full `task verify` → exit 0** (278 unit · 296 integration [292 passed / 4 skipped] ·
> 649 frontend across 83 files · stale-refs 0/940, suite 22/22 · OpenAPI current · client drift 9/9 ·
> migration drift clean · vulnerable 13/13 · gitleaks provisioning 10/10; 16 suite runs, `fail 0` in
> every one). This is the first run in the package whose security denominator actually contained
> every T105 deliverable. The index was reset immediately and all **30** change-set files hash-match
> their pre-staging SHA-256 snapshot — staging altered no bytes.
>
> **The scanner was not weakened — proven, not assumed.** A file containing the unredacted
> token-persistence call, staged, still fails with file, line and text (**exit 1**); the redacted
> form passes (**exit 0**). `scripts/check-security.mjs` is byte-identical and absent from the change
> set; `.gitleaks.toml` carries only the two prior narrow entries; no `EXEMPT_*`, markdown, or
> `specs/**` carve-out was added at any point in this package.
>
> **B-1…B-5 re-verified closed** against the final tree, not carried forward — including live
> negative probes (untracked spec promise → 1 violation; untracked new ADR → 1 violation; sibling
> copy of the exempt evidence file → 19 violations), resolving B-3 citations, and gitleaks 8.30.1
> provisioning at 10/10.
>
> **AC-009 is met; merge readiness is a separate gate and remains open.** `verify` has never
> executed on GitHub Actions, `main` still has no branch protection and no rulesets, the required
> context strings are unconfirmed against a real run, no PR has exercised the new template, and
> `required_signatures` must wait on confirmed local commit signing. The checkpoint commit must
> include all seven untracked deliverables — they are what makes the 940-file scan meaningful. New
> finding F-21 (ADR SHA1 provenance drift, non-blocking); F-17 elevated as the highest-value
> follow-up. See `validation.md` §16.7–§16.8.

> **Final re-review gate 2026-09-02 (Bishop): REJECTED — 1 new blocker; B-1…B-5 work verified sound.**
> Full record: [`validation.md`](./validation.md) §15.
>
> **Verified closed / sound:** B-1 and B-2 (Hicks), B-3 and B-4 (Hudson) all re-confirmed against
> the current tree — stale-refs baseline 0 across **940** files with every negative probe still
> failing correctly, suites 22/22, 10/10, 9/9, 13/13, gitleaks provisioning idempotent at the pin.
> **Scribe's B-5 redaction is correct and must be preserved:** scope limited to the four named
> sites, `scripts/check-security.mjs` byte-identical and absent from the diff, `.gitleaks.toml`
> carrying only prior-cycle entries, no markdown or `specs/**` carve-out, and every diagnostic fact
> preserved at each site (target file and line, scan mode, exit code, restoration hash,
> attribution). Sanitizing content instead of exempting paths was exactly right.
>
> **Open — B-6 (revision owner: Vasquez).** The redaction's own §8 changelog note, at
> `t105-setup-revision.md:258`, explains that all such literals were redacted while quoting one
> unredacted. `task verify` exits **0** today reporting `Security scan (repo) passed for 933
> file(s)` — but 933 is `git ls-files`, tracked-only, and all seven T105 deliverables are still
> untracked. Staging them and re-running the scanner exits **1** on that line, so the coordinator's
> next checkpoint commit turns the pipeline red. A green run whose denominator excludes the
> artefact under test is not proof; this is the third cycle in which the gap between the guards'
> 940-file and 933-file surfaces concealed a real defect.
>
> **Remedy:** redact that one line the same way the other four were redacted, and correct the §8
> sentence that reports a 933-file clean scan so it names which surface it means. Change nothing
> else — no scanner, no config, no exemption, no verdict, count, hash, or conclusion. Then re-run
> `task verify` **with the deliverables staged** and record the result.
>
> Ripley and Apone remain locked out of their originals; Hicks, Hudson and Scribe are locked out of
> the artefacts they authored in their own cycles.
>
> Merge readiness stays separate and still blocked: `verify` has never run on GitHub Actions,
> `main` has no protection or rulesets (re-verified live), required-context strings are unconfirmed,
> no PR has used the new template, and `required_signatures` must wait on confirmed local signing
> (`validation.md` §15.7).


> **Re-review gate 2026-09-02 (Bishop): REJECTED — 1 new blocker; B-1…B-4 CLOSED.**
> Full record: [`validation.md`](./validation.md) §14. Reviewed the assembled
> uncommitted tree atop `764282e` — Ripley's governance half and Apone's
> guard-proof half as revised by **Hicks** (B-1/B-2, `t105-evidence-revision.md`)
> and **Hudson** (B-3/B-4, `t105-setup-revision.md`).
>
> **Closed, verified by reviewer-run evidence rather than revision report:**
> **B-1** — the stale-reference guard now enumerates
> `git ls-files --cached --others --exclude-standard`, so a brand-new untracked
> file can no longer hide from it; reviewer probes confirm an untracked
> never-staged promise fails, an untracked new ADR fails, a sibling copy of the
> exempt evidence file fails with 19 violations, git-ignored paths stay excluded,
> baseline 0 across **940** files, suite **22/22**. **B-2** — the false
> "included in that 933" claim is struck through in place with the true proof
> beside it, the ADR hash corrected, and denominator drift disclosed. **B-3** —
> `plan.md` §6.2 and §6.3 are real, five-field, owned, dated register entries and
> both dangling citations (constitution §8.3 → v1.1.1, ADR 0002 L111) now
> resolve to them, with no requirement, bar, or journey weakened. **B-4** —
> `.tools/gitleaks` deleted outright, `task restore` re-provisioned the pinned
> binary byte-identically, 10/10 provisioning suite, idempotent re-run skips,
> install logic centralised in `tools:gitleaks` and consumed by `hooks:install`
> and both workflows.
>
> **Open — B-5 (revision owner: Scribe).** `task verify` does not pass on the
> assembled tree. It exits 1 at `check:security` — the guard this package itself
> added to the authoritative pipeline — because T105's own evidence and history
> prose reproduces a literal auth-token-persistence payload at
> `validation.md:1407`, `.squad/agents/hudson/history.md:687`,
> `t105-setup-revision.md:150`/`:194`, and `t105-tamper-evidence.md:532`. Only
> the two tracked files fail today; the failure grows to four the moment the
> change set is committed. Every other stage passes (278 unit · 296 integration ·
> 649 frontend across 83 files · 940 stale-refs · OpenAPI current · client clean ·
> migration clean · vulnerable clean). The adjudicated remedy is **exact
> contextual redaction of those four payloads only** — the scanner must not be
> weakened and documentation must not be broadly exempted (`validation.md`
> §14.1). Hudson disclosed this issue honestly rather than working around it.
>
> Ripley and Apone remain locked out of their original artefacts; Hicks and
> Hudson are locked out of the artefacts they authored this cycle. Scribe owns
> the redaction and must change no verdict, count, hash, or conclusion.
>
> Merge readiness stays separate and still blocked: `verify` has never run on
> GitHub Actions, `main` has no protection or rulesets, required-context strings
> are unconfirmed, no PR has used the new template, and `required_signatures`
> must wait on confirmed local signing (`validation.md` §14.9).


> **Reviewer gate 2026-09-02 — Bishop: REJECTED** (`validation.md` §13;
> `.squad/decisions/inbox/bishop-t105-review.md`). Both halves were executed —
> Ripley's governance work (`t105-governance-evidence.md`) and Apone's
> guard-proof work (`t105-tamper-evidence.md`) — and most of the surface is
> genuinely sound: the reviewer independently reproduced `task verify` exit 0
> end to end (278 unit / 296 integration / 649 frontend across 83 files, guard
> suites 20/20 · 9/9 · 13/13, 933 files scanned, no Docker, no browser) and
> re-ran a deliberate-break tamper test across **every** mechanism class —
> stale references, OpenAPI drift, generated-client drift, EF migration drift,
> collected-test floors, vulnerability direct **and** transitive, secret and
> auth-token scanning, and subtask failure propagation — each failing closed
> with a specific diagnostic and restored byte-identically. ADR 0002, the
> constitution 1.1.0 amendment (13 journeys preserved, no bar weakened), the PRD
> amendment, the PR template, `CODEOWNERS`, the T47 retirement and the
> check-name enumeration / branch-protection recommendation all pass review as
> written. **Four blockers stand:**
> **B-1** `t105-tamper-evidence.md` itself produces **18** active retired-harness
> references and fails `check:stale-refs` the moment it is tracked — T105's own
> deliverable breaks the pipeline it certifies;
> **B-2** the tamper record's "live positive proof" that the ADR passed inside a
> 933-file scan is false — the ADR is untracked and was never scanned (the
> reviewer produced the real proof: staged, 934 files, exit 0), and the recorded
> ADR hash is stale;
> **B-3** AC-009's exception clause is unmet — no §2.10 exception entry exists
> for the unapplied branch protection or the manual PWA checklist, yet
> constitution §8.3 and ADR 0002 already assert that it does;
> **B-4** `task verify` now requires a gitleaks binary that neither `task
> restore` nor the repository provides, while the amended PRD §7.5.5 documents a
> clean-checkout `restore` → `verify` contract that is therefore false.
> **Revision owners: Hicks (B-1, B-2), Hudson (B-3, B-4)** — neither authored the
> rejected artefacts. Ripley and Apone are locked out for this cycle.
> **Not claimed and still outstanding regardless of the blockers:** the post-T104
> `verify` job has never run on GitHub Actions, `main` remains unprotected
> (`404`, `rulesets == []`), context strings are unconfirmed against a real run,
> and no PR has yet used the new template. Merge readiness stays separate from
> AC-009 and contingent on actual PR workflow results.

> **Revised by Hudson, 2026-09-02** (B-3, B-4 only —
> `specs/004-agentic-development-foundation/t105-setup-revision.md`;
> `.squad/decisions/inbox/hudson-t105-setup-revision.md`). **B-3 closed:**
> `plan.md` §6.2 (branch protection — declined-for-now, owner
> `briandenicola`, closure trigger stated) and §6.3 (manual PWA checklist —
> owner `briandenicola`, per-release review trigger stated) now hold the
> exception records constitution §8.3 (v1.1.1) and `docs/adr/0002-…` L111
> already asserted existed; both cross-references corrected to point at the
> live entries instead of forward-referencing an empty register.
> **B-4 closed:** `Taskfile.yml`'s new `tools:gitleaks` task provisions the
> pinned gitleaks binary (idempotent via `scripts/check-gitleaks-installed.mjs`
> + `.test.mjs`, 10/10 passing, wired into `restore`'s own cmds so the suite
> is always reached); `restore` and `hooks:install` both depend on it instead
> of each running their own copy of the download logic; `check:security` now
> declares `deps: [restore]` and still fails closed with a clear message if
> the binary is somehow missing — no silent network fallback was added.
> Reviewer-run clean-state proof: the pinned `.tools/gitleaks/gitleaks.exe`
> was moved aside (controlled backup, not deleted), `task tools:gitleaks`'s
> status check correctly reported not-installed, `task restore` re-provisioned
> the exact pinned version (`gitleaks.exe version` → `8.30.1`, byte-identical
> SHA-256 to the pre-test binary), and `task check:security` then ran gitleaks
> successfully end-to-end (no "not installed" error). `quality-gate.yml` and
> `ci.yml`'s separate `bash ./scripts/install-gitleaks.sh` steps replaced with
> `task restore` so Task is the single owner of the install logic in both
> workflows. Windows path executed directly; Linux/bash path verified by
> inspection only — WSL/bash is not available in this environment (recorded,
> not hidden). **Not self-approved; not claimed as re-reviewed.** Ripley and
> Apone remain locked out this cycle. T105 is not re-marked `DONE`; Bishop's
> `REJECTED` verdict (§13) stands until Bishop (or another qualified reviewer)
> re-reviews.

> **Authorized 2026-09-02 by Apone's T104 re-review** (`validation.md` §12).
> T104 is `DONE` and AC-008 is met, so the check names T105 must enumerate,
> require, and tamper-test now exist and have been observed running.
> `check:client-drift` and `check:vulnerable` changed shape under the Hicks
> revision and carry reviewer-run tamper evidence (`validation.md` §12.1–§12.2);
> the floor mechanism carries §10.2's four-case evidence and is unchanged by
> hash (§12.5). T105 still owns the **complete** guard tamper matrix, and
> inherits findings F-5 and F-10–F-13 (`validation.md` §12.7) — notably that
> the two new checkers' own unit suites run in no task, and that the only
> green check currently visible on the branch is the ops workflow
> `Sync Squad Labels`. T105's first job is to enumerate and require the check
> names that exist after T104.

- [ ] Enumerate the check names that exist after T104 and produce a **written
      branch-protection recommendation** for `briandenicola` to apply, with
      exact check names, making the current posture observable in-repo (U-01/U-02)
- [ ] **Tamper-test every critical non-browser guard** — stale-Playwright-
      reference guard, contract-drift gates, migration gate, collected-test
      floors, verification entrypoint — each with a recorded deliberate break
      in which the guard fails · *evidence:* one run URL per guard
- [ ] Strengthen `.github/pull_request_template.md` to require recorded
      acceptance evidence per criterion, replacing "All CI checks green" with
      the named required checks · *evidence:* diff + first PR using it
- [ ] Fix or remove `.github/CODEOWNERS` (`@your-github-handle`, non-existent
      `deploy/` path) and resolve `.github/T47-CI-SETUP-CHECKLIST.md`
      (284 lines, unexecuted) — execute or archive · *evidence:* diffs + record
- [ ] Record the T102 manual PWA checklist — and branch protection if declined
      — as **explicit visible exceptions** under `plan.md` §2.10 with owners

---

## Cross-Cutting Checks Before Any Phase-2 Task Is Marked Done

- [ ] The authoritative verification command (or its T104 successor) run and
      recorded, with acceptance evidence in `validation.md` per criterion — not
      merely "checks green" (`plan.md` §2.8)
- [ ] Human `APPROVED` transition recorded for the specific scope; no agent
      self-approval and no agent marking its own work `DONE` (`plan.md` §2.3)
- [ ] Any deviation recorded as an explicit exception (`plan.md` §2.10)
- [ ] T3 tasks (T105) additionally: ADR written, risk owner named, rollback
      stated
- [ ] No task output reintroduces an automated Playwright role in any form —
      merge gate, scheduled run, release run, or optional suite (`brief.md` §2.1)

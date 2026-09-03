---
id: 004-agentic-development-foundation
title: Agentic Development Foundation
tier: T2
status: APPROVED
approved_by: briandenicola
approved_at: 2026-09-02T10:06:57-05:00
branch: chore/agentic-development-foundation
base_sha: d303cd6537392e2489222d5a0d5c946f39f2af0c
owner: Ripley (Lead / Architect)
supersedes: none
---

# Brief — Agentic Development Foundation

Companion documents: [`evidence.md`](./evidence.md) (findings, pinned
citations), [`plan.md`](./plan.md) (principles, scope, risks),
[`tasks.md`](./tasks.md) (checklists), [`validation.md`](./validation.md)
(status and acceptance ledger).

## 1. Problem

Tech Inventory merged PR #140 with every observable green check, and the change
still shipped without a single browser test having executed. The tests existed.
Nothing was **fail-closed** where the merge decision was made.

1. **The gate that could catch it was deliberately not a gate.**
   `.github/workflows/ci.yml` — the only workflow running Playwright — is
   `on: workflow_dispatch` only, its comment calling the E2E pipeline the one
   "we keep wanting to repair-then-mute" (`:.github/workflows/ci.yml@d303cd6`,
   L3–8). Quality Gate, the merge-blocking workflow, has no E2E job at all.
2. **A known, documented defect stayed advisory for 47 days.** Issue #89
   (opened 2026-07-17, still `OPEN`) describes both the `test.todo is not a
   function` collection crash and the `seedDevice()` schema drift that make the
   suite unrunnable. Nothing connected that knowledge to a control.
3. **Governance volume has outgrown governance authority.** 30,184 lines of
   instruction against a 566-line constitution — **53×** — 37% historical.

The deeper pattern: **discussion was treated as authorization**, **green was
treated as done**, and a suite described as the mandatory browser gate had not
collected a single test since 2026-07-17. The incident proves **a
claimed-mandatory suite was ceremonial and broken** — not that it must now be
made mandatory.

## 2. Evidence Summary

Full evidence with pinned citations is in [`evidence.md`](./evidence.md).

| Finding | Evidence |
| --- | --- |
| Tech Inventory `main` has **no branch protection and no rulesets** as observed 2026-09-02 (`404 Branch not protected`, `rulesets == []`). "Required checks" were conventional, not enforced. | §3.1 |
| PR #140 was merged by `briandenicola` at `2026-09-02T14:27:09Z` with **0 reviews** and its DoD line `- [ ] Manual full CI / Playwright workflow green` unchecked; the manual run that would have satisfied it failed **10 seconds later** on `TypeError: test.todo is not a function`. | §5.1, §5.2 |
| **19 `test.todo()` calls across 4 spec files** abort Playwright at module load, so **0 of the 22 F045 browser tests** in `journeys/15-pwa-shell.spec.ts` ever executed across 6 projects; behind that, `seedDevice()` omits the required `ownerId` / `locationId` (#89's second defect). | §5.3 |
| `.github/CODEOWNERS` is an unedited placeholder (`@your-github-handle`), so no code owner could ever be auto-requested. | §3.6 |
| Of four repositories audited, **only Aurearia has enforced required status checks** — and it runs **zero Playwright tests in CI**. No repository of the four can detect a run that collects zero tests. | §2, §2.9 |

### Correction to an earlier overstatement

Earlier framing asserted that reliability requires broad Playwright E2E
coverage. **The evidence does not support that claim, and this brief withdraws
it.** Aurearia has the strongest enforcement posture of the four and no browser
tests in CI; Tech Inventory has the most browser-E2E ambition and produced the
incident. Reliability correlates with fail-closed required checks, contract
drift detection, and realistic test boundaries.

## 2.1 Decision — Playwright Retirement

| Field | Value |
| --- | --- |
| **Decision** | **Retire Playwright from Tech Inventory.** It is not kept as a merge gate, a scheduled suite, a release suite, or an optional/manual automated suite. There is **no future automated Playwright role.** Aurearia demonstrates that reliable agentic development does not require browser E2E when contracts and CI are strong (`evidence.md` §2, §4.2); a harness that has never collected is not coverage. |
| **Approved by** | `briandenicola`, 2026-09-02 |
| **Class today** | **`REVIEWED`** — a human has decided and signed; the code, manifests, and workflows still reference Playwright. |
| **Becomes `ENFORCED` when** | `package.json`, `pnpm-lock.yaml`, `Taskfile.yml`, `scripts/**`, and `.github/workflows/**` contain **zero** Playwright references, and CI passes from a **clean checkout** with no browser download step. |
| **Disposition of assertions** | Useful assertions move to contract tests, real HTTP integration tests, component tests, or an **explicit human validation checklist** — never to a green automated claim that nothing executes. |
| **Declared gap** | What cannot be automated without a browser (`display-mode: standalone`, service-worker offline shell, install prompt, engine-specific rendering) becomes a **named, owned manual checklist with a release cadence** (T102) — an honest declared gap, not silent coverage. |

## 3. Desired Outcomes

**Recorded by T004** ([`plan.md`](./plan.md) §2): a named work-state model with
approval as a discrete human-owned transition; a scaled T0–T3 ceremony model;
the `ENFORCED` / `REVIEWED` / `ADVISORY` classification, no fourth state.

**Delivered by T101–T105:**

1. The broken Playwright harness is **retired safely** — behaviour inventoried
   first, removed as an executable contract, no stale references left (T101).
2. Valuable coverage is **migrated to lower reliable layers** — HTTP
   integration/contract, component tests, and an owned manual checklist for
   what cannot be automated without a browser (T102).
3. A **coverage migration matrix and deletion map** covers every existing spec;
   every removed test names a replacement or an accepted manual gap (T103 —
   analysis, precedes T101/T102).
4. One authoritative verification command surface **with no Playwright
   dependency**; local and CI results mean the same thing (T104).
5. Required checks and branch-protection posture aligned to real check names,
   with every critical non-browser guard tamper-tested (T105).

**Deferred to a later, separately approved package:** encoding the work-state
model into agent-facing instruction surfaces, and consolidating the 30,184-line
instruction corpus (`evidence.md` §3).

## 4. Non-Goals

- **Not** repairing, re-enabling, scheduling, or gating on Playwright in any
  form. §2.1 retires it; there is no PR-blocking, scheduled, release, or
  optional automated browser suite in the target state.
- **Not** rewriting the constitution and **not** creating an ADR yet — see
  [`plan.md`](./plan.md) §6.
  > **Scope amendment, 2026-09-02 (`briandenicola`).** This non-goal is
  > **lifted for T105 only**: the approver directed that the retirement ADR
  > (`docs/adr/0002-retire-browser-e2e-framework.md`) and the narrow
  > constitution/PRD amendments be completed inside this package, closing the
  > §6.1 package-closure precondition. The lift is limited to the clauses that
  > mandated the retired framework or executable browser journeys; no other
  > governance is rewritten.
- **Not** requiring a promoted multi-file spec for every implementation.
  Ceremony is scaled per [`plan.md`](./plan.md) §2.4.
- **Not** editing `.squad/`, `.copilot-state.md`, `SESSION-NOTES.md`,
  workflows, scripts, `Taskfile.yml`, tests, or product code during T001–T004.
- **Not** applying GitHub repository settings from inside this package; T105
  produces a written recommendation for `briandenicola` to apply.

**Issue #89 remains central evidence,** not a repair backlog. Its collection
crash and `seedDevice()` drift prove the harness was ceremonial. #89 closes only
after retirement (T101) and migration (T102) — never by repairing the harness.

## 5. Acceptance Criteria

AC-005 … AC-009 map one-to-one to T101 … T105; status is in
[`validation.md`](./validation.md) §1.

| ID | Criterion | Task |
| --- | --- | --- |
| **AC-001** | A four-repository evidence matrix exists, pinned to exact default-branch SHAs, covering required checks, local/CI parity, contract drift, test boundaries, governance volume, feature/task structure, code surface, migration validation, and incident safeguards. Every claim carries an `owner/repo:path@sha` citation. Unobservable items are marked `unknown` and never inferred. | T001 |
| **AC-002** | A complete inventory of Tech Inventory authority sources exists, each with purpose, audience, conflict/duplication notes, freshness, enforcement class, and a retain/shorten/archive/remove recommendation, with accurate line and word counts. | T002 |
| **AC-003** | The PR #140 / issue #89 failure chain is documented as a timestamped sequence, and **every** control that should have blocked the merge is named with the specific reason it did not, citing workflow, script, and test paths plus GitHub run and issue references. | T003 |
| **AC-004** | The approved first principles and scaled work model are recorded verbatim in enforceable language: the seven work states, non-authorization of discussion, no agent self-approval, the T0–T3 tiers, the ENFORCED/REVIEWED/ADVISORY classification, test-at-lowest-reliable-layer, local/CI command parity, acceptance-evidence completion, historical-context-is-not-instruction, and explicit visible exceptions. | T004 |
| **AC-005** | The Playwright harness is retired safely: `tests/e2e/**` no longer exists as an executable contract; every behaviour it claimed is inventoried into the T103 matrix before deletion; `package.json`, `pnpm-lock.yaml`, `Taskfile.yml`, `scripts/**`, `.github/workflows/**`, `docs/testing.md`, and the PR template contain **zero** Playwright references; a stale-reference guard fails the build if one returns. Issue #89 is closed citing retirement + migration, never repair-for-continued-use. | T101 |
| **AC-006** | Every assertion the T103 matrix marks as valuable is live at a lower layer: API behaviour, authorization, export, reference-data mutation, CRUD, and serialization as **real HTTP integration/contract tests**; Svelte rendering, state, navigation affordances, and accessibility as **component tests**; PWA install/offline/browser-engine behaviour as a **manual validation checklist with a named owner and a release cadence**, recorded as an explicit gap and never as a green automated claim. E2E seed-fixture drift risk is replaced by typed HTTP integration/request builders or generated-contract checks — `tests/e2e/fixtures/api.ts` is **not** repaired for continued use. | T102 |
| **AC-007** | A coverage migration matrix and deletion map covers **every** existing Playwright spec (15 `journeys/`, `security/token-storage`, and `theme-fouc.spec.ts` outside `testMatch`), each row naming: test count, the behaviour asserted, the destination layer (HTTP integration/contract · component · manual checklist · accepted gap), and the named replacement or the accepted-gap owner. No spec is deleted without a row. This analysis **precedes** T101/T102 file deletion. | T103 |
| **AC-008** | One authoritative verification command surface exists with **no Playwright dependency**: local invocation, `Taskfile.yml`, and CI invoke the same entrypoints across format, build, type-check, lint, unit, component, HTTP integration, contract-drift, and migration gates; any platform-forced divergence is declared inline at the divergence point and does not change what is verified. | T104 |
| **AC-009** | Required-check names and branch-protection posture are aligned to the checks that actually exist after T104; the PR template requires recorded acceptance evidence per criterion and names the required checks; `.github/CODEOWNERS` routes review to a real owner; and **every critical non-browser guard** introduced by T101–T104 (stale-Playwright-reference guard, contract-drift gates, migration gate, collected-test floors for the surviving suites, verification entrypoint) has a **recorded tamper test** in which a deliberate break makes the guard fail. If protection is not adopted, it is recorded as an explicit visible exception with a named owner. | T105 |
| **AC-010** | No claim anywhere in this work package asserts that a gate passed before that gate was implemented and observed. `validation.md` distinguishes *evidence recorded* from *gate passed*. | all |

## 6. Scope Boundary for This Phase

Executed: **T001–T004** — research, inventory, incident reconstruction, and
principle recording. Documents only. Authorized but **not started**:
**T101–T105**, including T103 — a partially executed run of it was stopped and
does not count as started. Scope is fixed in [`plan.md`](./plan.md) §3; order is
**T103 → T101/T102 → T104 → T105**.

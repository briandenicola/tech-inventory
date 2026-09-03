## Summary
<!-- What does this PR do, in one paragraph? -->

## Linked Spec / Task
- Spec or brief: `specs/00X-feature/spec.md` (or the approved brief)
- Task: `TNNN` / issue #NN
- Constitution sections: §X.Y
- Work state before this PR: `APPROVED` (a human approved this scope — discussion
  is not authorization, and no agent approves its own work)

## Acceptance Criteria — Evidence Per Criterion
> One row per acceptance criterion. **"CI is green" is not evidence.**
> Evidence = the exact test name, file path, command output, run URL, or
> checklist item that proves *this* criterion. A criterion with no evidence is
> not met.

| AC | Criterion (quoted from the spec) | Evidence (test name / path / command / run URL) | Met? |
|---|---|---|---|
| AC-1 | | | ☐ |
| AC-2 | | | ☐ |

## Required Checks — Named, Not Implied
Every box below is a **named** check. Tick it only if you saw it pass on this
PR's head commit (GitHub check name in parentheses).

- [ ] `task verify` green locally on this branch (authoritative entrypoint — no Docker, no browser)
- [ ] `verify` — Quality Gate job: format · build · type-check · lint · backend unit · frontend unit/component · stale-reference guard · OpenAPI + generated-client drift · EF migration drift · real-HTTP integration · production build · vulnerable-package scan
- [ ] `codeql` — SAST analysis (Quality Gate)
- [ ] `secrets` — gitleaks scan (Quality Gate)
- [ ] `container-config-scan` — Trivy config scan (Quality Gate; PR events only)

<!-- `sbom` runs on pushes to `main` only and is not a PR check. -->
<!-- Branch protection: see specs/004-agentic-development-foundation/t105-governance-evidence.md §4.
     Until it is applied, these checks report but do not block — ticking them is a
     statement of what you actually observed, not a formality. -->

Collected-test floors held (no silent zero-collection): ☐ unit ☐ integration ☐ frontend

## Definition of Done
- [ ] Tests written at the **lowest reliable layer**; coverage ≥ 85% on touched Domain/Application code
- [ ] Component and/or real-HTTP integration coverage added or updated for any UI-facing change (ADR 0002 — there is no browser E2E layer)
- [ ] Security scans clean (deps, SAST, secrets, container config)
- [ ] OpenAPI updated and the generated client regenerated (if API surface changed)
- [ ] Docs / ADR updated (if material)
- [ ] Constitution compliance verified (cite the sections)
- [ ] `tasks.md` / work-state ledger updated
- [ ] Signed commits; Conventional Commits; PR diff < 500 lines

## Manual PWA Checklist Declaration
> Required when this PR touches the web client's shell, navigation, service
> worker, offline behaviour, theming, install/manifest, or auth surfaces.
> These behaviours have **no automated coverage by design** (ADR 0002) — the
> checklist is the evidence.

- [ ] Not applicable — this PR touches none of those surfaces
- [ ] Applicable — ran `docs/testing/manual-pwa-validation.md`, checks: `M-__`, `M-__`
      · Run by: @______ · Date: ____-__-__ · Result: ☐ pass ☐ issues filed: #____
- [ ] Applicable but **not run** — recorded as an exception below (owner + duration required)

## Explicit Exceptions
> Anything not done, deferred, waived, or knowingly divergent goes here — visibly,
> with an owner and an end date. Silence is not an exception; it is a defect.
> A permanent exception is an ADR, not a checkbox.

| # | Rule / check not satisfied | Scope (what exactly is exempt) | Owner | Duration / closure trigger |
|---|---|---|---|---|
| 1 | | | @ | |

- [ ] No exceptions apply to this PR

## Notes
<!-- Risk, rollback plan, follow-ups, and anything a reviewer should not have to infer -->

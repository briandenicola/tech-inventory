# T47 CI Setup Checklist — **RETIRED (historical)**

**Status:** RETIRED 2026-09-02 · **not** current instruction
**Originally written:** 2026-05-18 by Hudson, for task T47
**Retired by:** Ripley, `specs/004-agentic-development-foundation/` T105 (AC-009)
**Superseded by:**
- [`.github/workflows/README.md`](./workflows/README.md) — what each workflow and
  check actually does today
- [`specs/004-agentic-development-foundation/t105-governance-evidence.md`](../specs/004-agentic-development-foundation/t105-governance-evidence.md)
  §3–§4 — the current check names and the written `main` branch-protection
  recommendation
- [`Taskfile.yml`](../Taskfile.yml) — `task verify`, the one authoritative
  verification entrypoint humans and CI both run

---

## Why it was retired rather than updated

This document was a one-time, dated setup narrative — "what Hudson did in May
2026 and what Brian must click next". It was never maintained, and by
2026-09-02 it was actively misleading in three ways:

1. **It claimed controls were "✅ Enforced" that nothing enforced.** Its
   Phase 1 summary table marked eleven checks `Enforced`. As observed on
   2026-09-02, `main` had **no branch protection and no rulesets**
   (`404 Branch not protected`, `rulesets == []`), so every check reported and
   none blocked a merge. A check that runs is not a check that gates.
2. **It named checks that no longer exist.** It instructed the operator to
   require `ci / verify` / `ci / Build, Test, and Verify` from
   `.github/workflows/ci.yml` — a workflow that is `workflow_dispatch`-only and
   carries no merge-blocking role. The merge-relevant workflow is **Quality
   Gate**, whose current job/check names are enumerated in the T105 evidence
   file above.
3. **It described a retired test layer as a live gate.** Its E2E rows described
   the browser end-to-end harness that was retired by
   [ADR 0002](../docs/adr/0002-retire-browser-e2e-framework.md). The build-time
   descendant of that step is the stale-reference guard
   (`scripts/check-stale-playwright-references.mjs`), which is a *different*
   control with a *different* purpose.

Keeping a stale operator runbook inside `.github/` is exactly the failure this
work package exists to end: **historical context read as current instruction**
(`specs/004-agentic-development-foundation/plan.md` §2.9). The original text
remains in git history (`git log --follow -- .github/T47-CI-SETUP-CHECKLIST.md`)
where a historical record belongs.

## What is still true and where it now lives

| Topic | Current source |
|---|---|
| What each workflow/check does, and how to fix a red one | `.github/workflows/README.md` |
| How to verify a change locally (identical to CI) | `task verify` — `Taskfile.yml`, `scripts/verify.ps1`, `scripts/verify.sh` |
| Pre-commit hook install | `task hooks:install` |
| Which check names to require on `main`, and the exact JSON | `specs/004-agentic-development-foundation/t105-governance-evidence.md` §3–§4 |
| Whether `main` is actually protected | Observe it: `gh api repos/briandenicola/tech-inventory/branches/main/protection` |

**No control described in this file is enforced by this file.** Nothing here
should be cited as evidence in a PR.

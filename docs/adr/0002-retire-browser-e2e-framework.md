# 2. Retire the browser end-to-end test framework (Playwright)

Date: 2026-09-02
Status: Accepted
Deciders: `briandenicola` (human approver), recorded by Ripley (Lead / Architect)
Supersedes: the Playwright mandates in `.specify/memory/constitution.md`
§6.5.6, §6.5.14, §7.2, §7.4, §9 and `docs/prd.md` §7.5.2–§7.5.4
Related: `specs/004-agentic-development-foundation/brief.md` §2.1 (approved
decision record), `specs/004-agentic-development-foundation/coverage-migration.md`
(migration matrix), `.squad/decisions.md` (ledger)

> This is one of the few decisions that warrants a standalone ADR rather than an
> inline `D-NNN` ledger entry (see [ADR 0001](./0001-record-architecture-decisions.md)):
> it removes a framework that two documents **above** the feature-spec layer of
> the §0 authority hierarchy previously mandated, so the amendment needs a
> long-form, externally citable record.

## Context

Tech Inventory named Playwright as its mandatory browser end-to-end framework in
two normative documents — the constitution (§7.4: "**Playwright** is the required
E2E framework — no substitutes") and the PRD (§7.5.3, and §7.5.4's thirteen
critical journeys). In practice the harness was ceremonial:

- 19 unsupported `test.todo()` calls across four journey specs aborted Playwright
  at module load, so the suite **collected zero tests** from 2026-07-17 onward
  (issue #89; `specs/004-agentic-development-foundation/evidence.md` §5.3).
- The only workflow that ran it, `.github/workflows/ci.yml`, was
  `workflow_dispatch`-only. The merge-blocking workflow had no browser job at
  all, so no PR could ever be blocked by it.
- PR #140 merged on 2026-09-02 with every visible check green, zero reviews, and
  its "Manual full CI / Playwright workflow green" definition-of-done box
  unchecked; the manual run that would have satisfied it failed ten seconds
  later on `TypeError: test.todo is not a function`.
- The `seedDevice()` fixture had drifted from `CreateDeviceRequest` (missing
  required `OwnerId` / `LocationId`), so even a collecting suite would have
  failed on setup.
- Of four audited repositories, the only one with enforced required status
  checks (`briandenicola/Aurearia`) runs **zero** browser tests in CI. Reliability
  correlated with fail-closed required checks and contract-drift detection, not
  with browser-E2E ambition (`evidence.md` §2, §4.2).

The failure mode was not "our browser tests are flaky". It was **a claimed-
mandatory gate that asserted nothing while documents at the top of the authority
hierarchy said it was mandatory**. Repairing the harness would restore the same
class of promise; the T103 migration analysis showed that essentially all of its
intended assertions were already live, or cheaply expressible, at lower and more
reliable layers.

## Decision

**Playwright is retired from Tech Inventory, entirely.**

1. **No automated browser role of any kind.** Not a required merge check, not a
   scheduled suite, not a release suite, not an optional or opt-in suite, and not
   a hidden/manual-dispatch suite. There is no future automated browser-E2E role
   in this repository. No substitute browser-automation framework (Cypress,
   Selenium, Puppeteer, WebdriverIO, or equivalent) is adopted in its place —
   the decision retires the *layer*, not merely the vendor.
2. **Valuable behaviour moves down, not away.** Every assertion the T103 matrix
   (`coverage-migration.md`) marked valuable is live at its destination layer:
   - API behaviour, authorization, export, reference-data mutation, CRUD and
     serialization → **real-HTTP integration / contract tests** (in-process
     `WebApplicationFactory<Program>` + real SQLite, no Docker);
   - Svelte rendering, state, navigation affordances and accessibility →
     **component tests** (Vitest + Testing Library + `axe-core`);
   - install / offline / service-worker / browser-engine behaviour → an
     **owned manual validation checklist** with a named owner and a release
     cadence: [`docs/testing/manual-pwa-validation.md`](../testing/manual-pwa-validation.md).
3. **Residual browser-only behaviour is a declared gap, never silent coverage.**
   Ten accepted gaps (`coverage-migration.md` §8, G-01…G-10) are recorded with
   owners. A manual check is reported as `REVIEWED`, never as a green automated
   claim.
4. **The thirteen PRD §7.5.4 critical journeys survive as intents.** They are
   not deleted. Each is re-pointed at its approved lower-layer or manual
   evidence; the requirement that removing a journey needs an ADR is unchanged.
5. **A stale-reference guard enforces the retirement.**
   `scripts/check-stale-playwright-references.mjs` fails the build if a
   reference returns to an active manifest, script, workflow, config, doc, or
   test tree, or if `tests/e2e/` or a `playwright.config.*` file reappears.
6. **Accessibility, security and acceptance requirements are unchanged in
   strength.** WCAG 2.2 AA, zero axe violations, OWASP ASVS L2, default-deny
   authorization and the coverage floor all stand exactly as before; only the
   *layer* at which browser-specific evidence is produced has moved.
7. **Browser-based performance automation is also outside the repository.**
   Post-major-work QC found the constitution and PRD still mandated Lighthouse
   CI even though no harness existed, contradicting this ADR's no-browser-
   automation decision. `briandenicola` approved alignment on 2026-09-03:
   route performance budgets remain requirements, reviewed manually per
   release as check P-01, and are never reported as an automated CI result.

**Approved by `briandenicola` on 2026-09-02** (`brief.md` §2.1). Recorded here
by Ripley under T105 of `specs/004-agentic-development-foundation/`.

## Consequences

**Positive**

+ The repository no longer mandates a framework it does not contain; the
  §0 authority hierarchy is self-consistent again.
+ Verification runs from a clean checkout with **no browser download and no
  Docker** (`task verify`), so local and CI results mean the same thing.
+ Coverage claims became truthful: 0 executing browser tests became 278 backend
  unit, 296 backend integration and 649 frontend unit/component tests with
  fail-closed collected-test floors, plus 16 owned browser/PWA checks and the
  P-01 manual performance review.
+ Merge-relevant guards (stale-reference, OpenAPI/client drift, EF migration
  drift, test floors) are cheap, deterministic and tamper-testable.

**Negative / accepted**

- Automated coverage of real browser engines drops to zero. It was already zero
  in practice; the honest version is an owned checklist (`M-01`…`M-16`) plus
  manual performance review P-01.
- Composed-page axe scanning in a real engine is lost (**G-05**); `/devices`
  has no route-level axe harness (**G-09**), and six reference-admin route
  compositions omitted during migration are recorded as **G-10**.
- Manual checks decay unless someone runs them. Mitigated by naming an owner and
  a release cadence, and by recording the checklist as an explicit visible
  exception (`plan.md` §6.3 — the register entry for §2.10's principle)
  rather than as coverage.
- Cross-layer composition (modal save → list repaint), real file-picker and real
  download mechanics are gaps **G-01**…**G-03**, covered by spot checks
  `M-12`/`M-13`.

**Reversal**

Reintroducing any automated browser-test layer — Playwright or any substitute,
in any role including "optional" or "scheduled" — requires a **new ADR** that
supersedes this one. It must state the merge-blocking role the suite will hold
from day one, the collected-test floor that proves it actually runs, and the
owner accountable for it. Removing the stale-reference guard requires the same.
This ADR may not be reversed by a code change, a workflow edit, or a task-level
decision.

# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device/appliance inventory tracker. Single-household, authenticated via Microsoft Entra ID, deployed as Docker Compose on home infrastructure.
- **Stack:** ASP.NET Core 10 (Clean Architecture: Domain → Application → Infrastructure → Api), MediatR, FluentValidation, EF Core + SQLite, Serilog, OpenTelemetry. SvelteKit PWA (TypeScript strict, Tailwind, MSAL.js, generated TS client). Docker Compose deploy behind external reverse proxy. Task (taskfile.dev) for local automation.
- **Created:** 2026-05-18

## Core Context

Phased delivery per PRD §13: Phase 0 Foundation → Phase 1 Core API (`specs/001-core-api`) → Phase 2 Auth (`specs/002-auth-entra`) → Phase 3 Infrastructure (`specs/003-infrastructure`) → Phase 4 Web PWA (`specs/004-web-pwa`) → Phase 5 Hardening.

Document hierarchy (constitution §0): `.specify/memory/constitution.md` → `docs/prd.md` → `specs/00X-*/spec.md` → `specs/00X-*/plan.md` → `tasks.md` → agent judgment.

Reference repos (`docs/references.md`): **R1 drinks-and-desserts** (MediatR handler structure, Problem Details middleware, health checks), **R2 coin-collection-app** (inventory domain shape, CSV import pipeline, attachment storage, Docker Compose layout). Patterns yes, code no. Pin to SHA and cite `R<N>:<path>@<sha>`.

## Recent Updates

**2026-05-18:** Phase 0 parallel scaffolding complete. Security baseline now in effect (`docs/security-baseline.md` from Bishop). **Currency strategy decision OPEN and blocks T04** — awaiting Brian's decision between per-device (recommended) or single-currency approach.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **2026-05-21 — v1.0 Documentation Alignment**: Performed end-of-day surgical pass on 11 docs post-v1.0 ship. Key updates: (1) README.md — IMAGE_TAG strategy now documents three patterns (`:vX.Y.Z` for production, `:main` for rolling dev, `:sha-<short>` for staging); previous guidance was "use latest as default" which is no longer accurate as `:latest` now pins to semver releases only, not main HEAD (D-154 via CI release-images workflow). (2) auth-design.md — added §6 "Silent SSO Bootstrap (F038)" documenting the 3-second timeout, multi-tab behavior, and Entra outage fallback (shipped in commits 788268c + follow-ups after D-150 proposal). (3) docs/prd.md — status updated from "Draft v0.1" to "v1.0 (shipped 2026-05-21)"; last-updated date was 2025-05-17 (stale by a year). (4) docs/backlog.md — F038 entry refined to note the 3-second timeout explicitly; F035, F036, F037, F044 status verified correct. (5) docs/deployment.md — §7 rewritten with three IMAGE_TAG strategies (Strategy A production pinning, Strategy B rolling dev, Strategy C staged rollout by SHA); previous example showed rolling back to `v0.0.9`, now shows v1.0 series to avoid stale references. (6) .copilot-state.md updated to reflect "v1.0 shipped" state and post-v1.0 next milestones (#17, #18 open for future work). (7) SESSION-NOTES.md appended with v1.0 ship summary and learnings. No changes needed: docs/architecture.md, docs/known-issues.md (describes pre-v1.0 deferred tests, still accurate), docs/operations.md (break-glass runbook unchanged), docs/references.md, docs/security-baseline.md (token storage rules unchanged), docs/testing.md (test counts, auth patterns still current), docs/threat-model.md. Scope discipline: surgical updates only, no rewrites of validated sections. Decisions D-164 (modal scroll), D-167 (z-index hierarchy), D-168 (vestigial props cleanup) were design decisions not documentation scope — noted for future alignment if those get formalized in architecture docs. No code files modified; validated `git diff --stat` shows only `.md` files touched. All docs internally consistent post-update.

- **2025-05-18**: Drafted Phase 1 spec (`specs/001-core-api/spec.md`, `plan.md`, `tasks.md`). Key decisions:
  - Network is a standalone reference entity (not a Tag subtype) — PRD §U8 treats it as a managed list.
  - Result<T> is a custom minimal record in Domain (no external lib dependency — keeps Domain clean per constitution §2.2).
  - Currency proposed as per-device ISO 4217 with household default — decision pending Brian's input (`decisions/inbox/ripley-currency-strategy.md`).
  - 48 tasks decomposed; Hicks owns implementation, Apone owns test authorship, Hudson owns CI.
  - Reference entities use soft-delete via `IsActive` flag (consistent with Device soft-delete pattern).
  - AuditBehavior as MediatR pipeline (not SaveChanges interceptor) — gives richer before/after capture and respects the command boundary.

- **2026-05-20**: Created spec 003 (`specs/003-pwa-polish/`) from Brian's field-test feedback. Key decisions:
  - New spec (not amendment to 002) — constitution §1 scope discipline; 002 has clear completion tracking.
  - 16 tasks across 5 rounds (A: bugs, B: mobile UX, C: features, D: reporting, E: tests).
  - Dark mode promoted from 002 non-goal to 003 P2 — Brian explicitly requested in field test.
  - Merge duplicates requires new backend endpoint (`POST /api/v1/{entity}/merge`) — Hicks owns.
  - Reporting gets lightweight foundation here (3 API endpoints + summary UI); full dashboard remains Phase 4.
  - Insurance export is CSV-first; PDF deferred (ADR needed if Brian wants PDF later).
  - Infinite scroll supersedes pagination but keeps a11y fallback for `prefers-reduced-motion`.
  - Rounds A+B+D can parallelize from day one; C depends on backend work from Hicks.

- **2026-06-13 — Post-v1.0 Architecture Audit**: Conducted comprehensive codebase review post-v1.0 ship (Brian's request for deep engineering analysis). Key findings:
  - **CRITICAL BUG IDENTIFIED**: DeviceRepository lines 228-235 enforce implicit "Status != Disposed" filter when no explicit status filter provided. Frontend defaults to `status: ['Active']` on bare URLs (F026 feature), sending explicit filter. When user clicks "All statuses" (status=all sentinel), frontend passes `undefined` to API, backend applies "not Disposed" filter server-side. Result: Disposed devices never visible even when explicitly requested. Root cause: misaligned filter contract — frontend "all" means "send nothing", backend "nothing" means "exclude Disposed". Fix requires either (1) frontend send explicit all-status array, or (2) backend accept sentinel param, or (3) align on undefined=all convention.
  - **Architecture compliance**: Domain layer **zero** framework dependencies (✓ constitution §2.2). No EF/DbContext leakage into Domain or Application. Controllers **thin** (largest: DevicesController 313 lines, 90% DTOs; no business logic). Repository pattern enforced; no raw SQL anywhere. MediatR pipelines operational (ValidationBehavior → handler → AuditBehavior). Clean Architecture dependency flow: Api → Infrastructure → Application → Domain (✓).
  - **Component health**: Frontend largest components DeviceTable.svelte (596 lines), DeviceForm.svelte (590 lines) both **exceed** constitution §6.5.4 200-line guideline but are single-purpose with clear state boundaries; not god components but should be refactored into composition patterns for maintainability. Other components under limit.
  - **API client discipline**: Constitution §6.5.2 requires generated client, no hand-written fetch. Found 3 fetch calls in `client.ts` (lines 141, 219, 654) — all are **wrapper infrastructure** (apiFetch helper, blob download, raw endpoint call), not business-domain calls. Actual domain calls go through typed `devices.list()` etc. Pattern is compliant but blurs the line; worth ADR clarification on "generated client" scope (does it mean zero fetch usage, or zero domain-fetch usage?).
  - **Test coverage**: 88 backend test files, 52 frontend test files. Strong coverage foundation. No behavioral test gaps identified in spot-check, but constitution §Testing requires 85% line coverage on Domain+Application — recommend CI coverage gate validation.
  - **Missing patterns**: No ADR directory found (constitution violations require ADR per §Preamble). Several design decisions in .copilot-state.md / history.md should graduate to formal ADRs (e.g., MediatR pipeline choice, AuditBehavior sequencing, per-device currency strategy).
  - **Pagination concern**: DeviceRepository.ListAsync (lines 27-39) loads full result set from DB, then does **in-memory** pagination via `ToPagedResultAsync` → `MergeTrackedAsync` → `ToListAsync()` + skip/take. At scale (1000+ devices) this will degrade; recommend server-side pagination via `Skip().Take()` on IQueryable before materialization. Current impl works because it merges EF Local tracker (unsaved adds/updates), but that pattern should only apply to reference entities (brands, categories), not paginated device lists.
  - **Soft-delete alignment**: All entities use `IsActive` flag (Brand, Category, Location, Network) or status enum (Device). Audit table is append-only. ✓ Constitution §4.3.
  - **Observability**: Serilog + OpenTelemetry wired in Api project. Structured logging present. No PII leakage spotted in sampled log statements. ✓ Constitution §3.5.

---

### 2026-06-14: Engineering Audit Session

**Orchestration Log:** `.squad/orchestration-log/2026-06-14T00-17-12Z-ripley.md`

**Key Audit Findings:**
- Architecture correctly enforced (Domain → Application → Infrastructure → Api)
- Critical device list filter contract risk identified (filter semantics divergence between UI/API/Domain)
- In-memory pagination flagged as high-risk for scalability
- Large frontend components exceed guidelines but acceptable for now
- API client / ADR process gaps documented

**Deliverables:**
- 6 orchestration logs created (one per team agent)
- 6 new decisions merged to decisions.md (D-168.1, D-169, D-170, D-171, D-172, D-173, D-174)
- Inbox decisions cleared (8 files deleted)
- Session log at `.squad/log/2026-06-14T00-17-12Z-engineering-audit.md`

**Status:** Audit complete. Findings logged for team review and prioritization.

---

### 2026-09-02: Design Review — PWA Device Navigation (`feature/pwa-device-navigation`)

**Deliverables:** `specs/_backlog/F045-pwa-shell-and-device-list.md` (implementation boundary for Vasquez, test boundary for Apone), `.squad/decisions/inbox/ripley-pwa-device-navigation.md` (D-175..D-179), F027 scope-narrowing note, F023 extension note, `docs/backlog.md` row.

**Learnings:**

- **`isStandalonePwa()` already exists** in `src/TechInventory.Web/src/lib/auth/index.ts` (~line 62), ORing `matchMedia('(display-mode: standalone)')` with `navigator.standalone` for iOS. It landed with D-170/D-171 for the iOS silent-SSO work. Any future "is this the installed app?" question reuses it — the auth module owns the primitive; UI wraps it reactively. Do not re-derive it and do not "simplify" the OR: iOS Safari home-screen apps do not reliably report `display-mode: standalone`.
- **Presentation mode ≠ viewport.** The project has repeatedly conflated "mobile" with "PWA" (`md:hidden` everywhere). Codified three modes — `pwa` / `mobile` / `desktop` — where installed-app chrome keys off standalone detection only. A narrow desktop window must never grow a bottom tab bar. This is the load-bearing rule of F045.
- **The implicit-default pattern is now project canon.** F026's `statusIsImplicitActive` (devices page ~line 142) established: apply a default, do *not* write it to the URL, do *not* count it in `activeFilterCount`, provide an explicit sentinel to defeat it. F045's PWA category grouping reuses it verbatim. URL-writing on mount would fight `setDevicesViewState` session restore and poison shared links.
- **F023 grouping has a hidden cost:** it switches the page to a 500-row fetch and disables infinite scroll. Making grouping a *default* moves the heaviest fetch path onto the weakest device on the network at a stated 500–1000 device scale. Held the 500 cap and required a truncation note rather than raising it — raising it is an API conversation, not a UI one.
- **Fixed-element collision is the recurring failure mode in this codebase.** `AddDeviceFab`, `BackToTopFab`, `BulkActionBar` (`fixed inset-x-0 bottom-0 z-30`), `ToastContainer`, and `PullToRefresh` all compete for the bottom of the screen. Adding a persistent bottom nav forces an explicit audit of all five. There is already a dedicated `PullToRefresh.containing-block.test.ts` guarding transform-containing-block clipping — required an equivalent test for the new menu popover.
- **`DeviceTable.svelte` is at 596 lines against a 200-line guideline** and already carries three renderers (desktop table, mobile 2-up cards, mobile scrollable table). A fourth renderer inline is not acceptable; made the split into a thin selector + per-mode renderers a precondition of the feature rather than a follow-up. Deferred refactors on this file have not happened on their own since the 2026-06-14 audit flagged it.
- **`visibleColumns` (Table Columns pref) is desktop-only by accident, not by contract** — the mobile card renderer simply never reads it. Brian's requirement makes it an invariant, so it now needs a unit assertion plus a user-visible label in Settings. Accidental invariants are the ones that break silently.
- **Mobile device cards ship no actions menu today.** `DeviceActionsMenu.svelte` (F042) is used only inside `DeviceDetailModal.svelte`, so every mobile action costs an extra tap through the detail modal. F045 reuses that component in the row rather than forking it.
- **Environment note:** UNC network paths (`\\nas...`) are blocked for file reads. One of three supplied reference images was unreadable; flagged in the decision record rather than guessing at its content.
- **Scope discipline call:** recorded as a new backlog entry (F045) rather than amending F027. F027's nav section *describes the drawer being replaced* — editing it in place would erase the record of why the drawer existed. Narrow the old entry, point it at the new one, keep both histories intact. Same reasoning as the 2026-05-20 "new spec 003, not an amendment to 002" call.

---

### 2026-09-02: QC Audit — F045 PWA Shell (`feature/pwa-device-navigation`, uncommitted) — REJECTED

**Verdict:** rejected; 4 blockers. Revision owner: Hicks (Vasquez locked out as
original author of the rejected frontend production artifacts).

**Learnings:**

- **A z-index token is not a drop-in for a Tailwind z-class.** D-E's recommended
  `style="z-index: {open ? var(--z-popover) : var(--z-sticky)}"` on `<header>`
  silently demoted the closed-state header from `z-30` to `20`. Seven pages
  (`devices` + six `admin/*`) carry an opaque `sticky top-[73px] z-20` sub-header
  that is *later in DOM order*, so an equal z-index hands them the tie and the
  desktop user-menu dropdown — the sole desktop nav entry — now paints beneath
  them. When a review prescribes a stacking fix, prescribe the *closed-state*
  value too, and enumerate the equal-z siblings before approving it.
- **Verify the premise before writing it into a decision.** D-177 asserted F023
  "already" had a 500-row grouped fetch cap. It did not — `fetchAllDevicesForGrouping`
  was unbounded. That error propagated into the F023 amendment text and produced
  a genuine desktop truncation change that no non-goal authorized. Read the
  function, don't recall it.
- **Two-sided implicit defaults need the write path spec'd, not just the read path.**
  The `effectiveGroupBy` read path is faithful to F026, but `updateFilters` writes
  the `groupBy=none` sentinel on *any* falsy `groupBy` in app mode — so the first
  search keystroke defeats the implicit grouping and pollutes the URL. F045 §5.6
  described the read path in detail and the write path in one clause. Spec the
  sentinel's *trigger condition* (explicit user selection only), not just its value.
- **Parallel test/implementation authoring needs a contract-freeze checkpoint.**
  Apone authored `15-pwa-shell.spec.ts` against F045 §5.2's `role="group"` pill and
  §7.3's "every nav item has a visible label"; Drake's D-D then made the Settings
  bubble deliberately icon-only and Vasquez omitted `role="group"`. Neither side is
  wrong in isolation; the branch is simply internally inconsistent and no one could
  run the E2E suite to find out. Require a selector-contract reconciliation pass
  before the two workstreams are declared done.
- **"Renders X inside Y" test names must actually assert containment.**
  `AppBottomNav.test.ts` claims pill/bubble structure but only checks that four
  elements exist, and its positioning test selects by `.app-nav` then asserts
  `toHaveClass('app-nav')`. Tautological assertions pass forever and guard nothing.
- **`git status` M with an empty diff is a CRLF stat artifact, not drift.**
  `src/lib/api/generated/types.ts` is rewritten byte-identically by
  `pnpm run check`'s `generate:client` step; with `core.autocrlf=true` and an
  LF-stored blob, git can never settle the stat cache. Confirmed by comparing
  `git hash-object` against `HEAD:` — identical. Check hashes, not status letters.

---

### 2026-09-02: T102 Final Reviewer Gate — Agentic Development Foundation (`chore/agentic-development-foundation`) — APPROVED

**Verdict:** APPROVED (third cycle; Apone rejected, Bishop rejected, Hicks's
revision accepted). T101 authorized to begin. Recorded in `validation.md` §6,
`.squad/decisions/inbox/ripley-t102-final-review.md`.

**Learnings:**

- **My own blocker count was wrong, and the fix was to be corrected, not
  matched.** My second review asserted 21 `AdminOrMember`-gated mutations
  needed a documented 403. The real number is 26. Hicks re-enumerated, found my
  error, and wrote the correction into the record instead of quietly shipping
  21. A reviewer's figures are evidence like any other — cite the enumeration
  method, not a hand count, or the next agent inherits the error as authority.
- **A big diff is not a big change — measure it structurally.** `openapi.yaml`
  went 8,166 → 5,476 lines, which reads like content loss. Parsing both
  versions showed 68 operations and 83 schemas before and after, with exactly
  26 differences, all added `403` responses. Line-count deltas from canonical
  regeneration tell you nothing; diff the parsed document, not the text.
- **Tamper-test the guard, don't just read it.** The new 26-case contract theory
  looks right, but "looks right" is what PR #140 had. Renaming one `403` key to
  `499` failed exactly one case with a precise message and left 25 green — that
  is the proof the guard is per-operation and real. Restoring by hash, not by
  eye, is part of the same discipline.
- **A hard-coded `[InlineData]` list pins the present and guards nothing
  future.** The theory covers all 26 operations that exist today; a 27th gated
  mutation added next month sails past it. Approved anyway — the blocker asked
  for coverage of the current set — but logged for T105 as a reflection-based
  enumeration. Know the difference between a regression pin and a policy gate,
  and say which one you accepted.
- **Duplicate derivations are the defect class, not the individual defect.**
  B3 survived two fixes because `canClaim`/`canRelease` existed in three
  independent places and each round only patched the copies it had looked at.
  The close-out that finally worked was a repository-wide search for the
  derivation, enumerating every site before declaring it closed. When rejecting
  a logic gap, name the *search* that proves completeness, not the files.
- **Stale figures need a superseding marker, not silence.** "240/245" and "645"
  survived two correction rounds because each round added the new number
  without retiring the old one. Numbers left standing get re-cited as current.
  Mark superseded figures at every occurrence or delete them.

---

### 2026-09-02: T101 Final Reviewer Gate — Retire the Broken Playwright Harness (`chore/agentic-development-foundation`) — REJECTED

**Verdict:** REJECTED; 2 blockers. Revision owner: Apone (Hudson locked out as
author). T104 held. Recorded in `validation.md` §7,
`.squad/decisions/inbox/ripley-t101-review.md`, `tasks.md`/`plan.md`/
`coverage-migration.md` §13 status lines.

**Learnings:**

- **A keyword guard defines the residue it can see, so the residue moves to the
  other word.** Every surviving defect in T101 says `e2e`, not `playwright`:
  `.github/T47-CI-SETUP-CHECKLIST.md` still instructs `task test:e2e` and
  `task test:e2e:run`, both deleted from `Taskfile.yml`. The guard was green at
  0/901 the whole time. When reviewing a guard, enumerate the vocabulary it
  *cannot* match and search that separately — `git grep -iE "tests/e2e|\be2e\b"`
  excluding the exempt paths found in one command what the guard was structurally
  incapable of finding.
- **"Historical" is a claim about a file's tense, not its directory.**
  `coverage-migration.md` §5.5 swept 21 `specs/_backlog/` files into "historical
  evidence only", and the guard exempted all of `specs/`. But constitution §0
  ranks `specs/_backlog/F0XX-*.md` as authority source #6 — those files are
  *unbuilt definitions of done*, and ten of them carry **unchecked** boxes
  demanding new Playwright tests. Test the classification by reading the checkbox
  state, not the folder name: `- [x]` is history, `- [ ]` is instruction.
- **Verify the exemption's premise, don't grade the reasoning.** Hudson's
  `.squad/log/**` call rested on "already gitignored for future writes". That is
  checkable in one command — `git check-ignore -v --no-index` returned
  `.gitignore:51` and `.gitignore:50` — and it was true. The exemption is safe
  *because the premise held*, not because the argument was well-written. Note
  also that `git check-ignore` without `--no-index` reports nothing for tracked
  files, which is why the first probe misleadingly returned rc=1.
- **An implementer can overstate their own failure.** §13.5 and `validation.md`
  §1 both open "AC-005 requires zero active references" and then apologise for
  two. AC-005 enumerates seven surfaces and includes neither `constitution.md`
  nor `docs/prd.md` — they were never in scope. Re-read the criterion's own text
  before accepting either a completion claim *or* a confession; Hudson graded
  himself against a stricter criterion than the one he was given, which corrupts
  the ledger just as much as grading himself too loosely.
- **The real governance defect was in neither flagged judgment call.** Both of
  Hudson's disclosed calls survived review; both blockers came from places
  nobody flagged. Disclosure is evidence of good faith, not a map of the risk —
  budget the review time for the unflagged surface.
- **Tamper-test the branches the guard claims separately, and prove restoration
  by hash.** The structural checks are the ones nobody tests, because writing a
  file whose *content* has no keyword feels contrived — `tests/e2e/probe.ts`
  containing `export const revived = 1;` is exactly the reintroduction the
  keyword scan would miss, and it failed correctly. `git hash-object` before and
  after (`f13c199c…` both) is the only acceptable proof the working tree came
  back; `git status` line count is a second, weaker check.
- **A guard that isn't in the merge-blocking workflow is the shape of the
  original incident.** `quality-gate.yml` does not run the stale-reference
  guard; `ci.yml` is still `workflow_dispatch`. `plan.md` R-4 stages that to
  T104/T105 so I did not block on it — but this package exists because a control
  lived somewhere other than where the merge decision is made. Say so out loud in
  the verdict, or T104 inherits it as settled.

---

### 2026-09-02: T101 Re-Review Gate — Playwright Retirement (`specs/004-agentic-development-foundation`) — **APPROVED**

**Verdict:** approved; T101 `DONE`, AC-005 met, T104 released with two attached
conditions. Revision owner was Apone (Hudson locked out of the rejected cycle).
Records: `validation.md` §7.8, `coverage-migration.md` §13.10,
`.squad/decisions/inbox/ripley-t101-rereview.md`.

**Learnings:**

- **Adopt the revision owner's better construction over your own suggestion.**
  My B2 resolution proposed `specs/001-*`/`002-*` prefix allowlists. Apone
  shipped an exact twelve-file allowlist instead and was right: a prefix would
  still let a *new* file dropped into an already-exempt package pass forever.
  A reviewer who insists on their own remedy when a stronger one is offered is
  optimising for authorship, not for the repository.
- **Every allowlist is line-blind, and that must be written down where the
  allowlist lives.** `specs/002-frontend-mvp/plan.md:128`/`:133` are unchecked
  Playwright DoD boxes *inside* an exempt file. Harmless today because the
  package header reads `Shipped (production-validated 2026-05-19)` — but the
  exemption is per-file, so a new promise appended to any of the twelve is
  invisible. This is the same shape as the earlier `.gitignore` finding:
  ignore rules stop new files, not edits to tracked ones. **Before approving
  any file-level exemption, check the exempt files' *status headers*, not just
  their content.** That header was the whole basis for calling them historical.
- **Re-derive the count, don't re-check the list.** §7.7 claimed "35 files
  remain" repo-wide. My own whole-disk scan returned 56 (39 tracked). The
  classification was right and the number was wrong — which is exactly the
  failure a reviewer who only spot-checks the named items will miss. Counts in
  evidence records are claims like any other.
- **Tamper-test a *different* file than the revision owner did.** Apone
  tamper-tested `F031`. I used `F026`, plus a brand-new `specs/005-*` path and
  a structural `tests/e2e/` revival, then diffed `git status --porcelain`
  against a pre-review baseline to prove I left nothing behind. Reproducing the
  author's exact tamper proves the author's run, not the guard.
- **`git add -N` is the right tool for tamper-testing a `git ls-files`-based
  guard** on an untracked path: it makes the file visible to `ls-files` without
  staging content, and `git rm --cached` reverses it cleanly. Worth knowing —
  the guard is invisible to untracked files, so you cannot test the "new file"
  branch any other way locally.
- **Verify a checked box against the *test body*, not the test name.** Three
  backlog checkboxes were flipped `[ ]` → `[x]`. I opened
  `SharePointCsvImportTests.cs` and confirmed it really asserts
  `Model == "Leaf Stitch"` and `Purpose == "Master TV"` at `:62-63`, and
  `BulkActionBar.test.ts` really has bar-visibility cases at `:36`/`:41`. A
  plausible test name is the cheapest way to launder an unmet promise.
- **Don't move the goalposts on a second gate.** I found residual generic
  `E2E` promises (`F045:357`, `F045:427`, and untouched `F018`/`F019`/`F025`)
  — the same *class* as B1, but never Playwright-worded, outside AC-005's
  enumerated surfaces, and gating no built work. Rejecting a revision on a
  finding I did not raise the first time would be authorship theatre. Recorded
  as **F-1** with a named owner and closure trigger instead. A reviewer's
  second pass must close the blockers it raised, not invent a third cycle.
- **AC wording is the scope boundary, and it cuts both ways.** AC-005
  enumerates seven surfaces. `constitution.md` and `docs/prd.md` are not among
  them, so they are *out of scope*, not *exceptions to zero* — I corrected
  `coverage-migration.md` §13.5's framing to say so. The same discipline that
  refuses to blame T101 for them also refuses to let them disappear: they are
  now a named, attributed, dated §2.10 exception at `plan.md` §6.1 with a
  closure trigger, and an explicit condition on T104.
- **Release the next task with conditions attached, in the task's own file.**
  T104 is authorized in `tasks.md` at its own heading — not only in the review
  document — carrying (1) do-not-deepen-the-contradiction and (2) ownership of
  the guard's absence from `quality-gate.yml`. Authorizations recorded only in
  a reviewer's verdict are archaeology by the time the next agent starts.
- **Lockout hygiene:** a lockout is scoped to the rejected cycle, not to the
  artefact. The revision was accepted, so Apone is not locked out, and
  Hudson's lockout expires with this approval. Say so explicitly — otherwise
  the next dispatcher has to guess.

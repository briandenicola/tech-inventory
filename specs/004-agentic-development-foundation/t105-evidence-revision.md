---
task: T105 revision — Bishop's blockers B-1 and B-2 only
author: Hicks (independent revision owner, B-1/B-2; Ripley and Apone locked
  out this cycle per Bishop's rejection)
date: 2026-09-02
base_sha: 764282eff32c64badff87f6e0ac36dcfb291f633
branch: chore/agentic-development-foundation (working tree, uncommitted)
parallel_scope: Hudson owns B-3/B-4 (AC-009's exception register, `check:security`
  dependency wiring) on the same branch, independently and concurrently.
  Nothing in this document touches that scope.
does_not_claim: T105 is not DONE. Canonical `tasks.md` and `validation.md` are
  untouched by design — Bishop owns the re-review verdict.
---

# T105 — B-1/B-2 Revision (Hicks)

## 0. What was rejected, restated

Bishop rejected the integrated T105 change set
(`.squad/decisions/inbox/bishop-t105-review.md`) on four blockers. This
document closes exactly two, both against
`specs/004-agentic-development-foundation/t105-tamper-evidence.md`:

- **B-1** — that file was untracked and, staged, tripped 18 violations of
  the retired-reference guard (its own tamper matrix necessarily quotes
  retired-harness invocations verbatim as evidence of what was broken and
  restored). No run in the record had ever scanned it.
- **B-2** — the file's §2.3 claimed a real accepted ADR was "included" in a
  933-file live scan while that ADR was itself untracked and outside the
  933; it also recorded a stale SHA1 for that ADR.

## 1. Decision — widen the guard's scan surface, not just its allowlist

**Considered:** exempt the new evidence file and stop there (B-1 alone), and
separately correct the false claim as a documentation fix (B-2 alone).

**Rejected as insufficient**, because it leaves the structural defect in
place: a guard that enumerates only `git ls-files` (the index) can never
prove it scanned a file no one has staged yet, no matter how the record is
worded. That is exactly how B-2 happened — the record asserted a scan result
about a file the tool physically could not have seen. Fixing the prose
without fixing the enumeration would only make the next false claim harder
to spot.

**Decision:** `scripts/check-stale-playwright-references.mjs`'s `main()` now
enumerates:

```
git ls-files -z --cached --others --exclude-standard
```

— tracked files (`--cached`) **plus** untracked-but-not-git-ignored files
(`--others --exclude-standard`) — instead of `git ls-files -z` (tracked
only). Git-ignored paths (`.tools/`, `node_modules/`, build output, etc.)
remain excluded; they are not part of the repository's active instruction
surface. This is the smallest change that satisfies the requirement: no new
dependency, no change to `findStaleReferences()`'s classification logic
(the pure function under unit test), only a wider file-discovery command in
the git-calling shell around it. The pass/fail console messages were
corrected to say `tracked + untracked non-ignored file(s)` instead of
`tracked file(s)`, so the tool's own output cannot make the same category of
false claim again.

**Why this satisfies "fail-closed for newly created/untracked files"
without relying on a user remembering to stage first:** a brand-new active
file with a forward-looking promise about the retired framework is caught
by a bare `node scripts/check-stale-playwright-references.mjs` (or
`task check:stale-refs`) the moment it exists on disk, before `git add`,
before a commit, before a PR. Nothing has to be staged for local
verification to see it. This was independently confirmed compatible with
existing repository behavior — no test, script, or workflow in this
repository depended on the guard being blind to untracked files — so no
alternative/documented-limitation path was needed.

**Trade-off, disclosed:** the guard's own denominator is no longer a fixed
number across runs on a dirty branch — it moves as untracked scratch files
appear and disappear (see §2's live counts, which shifted mid-session
purely from a concurrent, unrelated change on this shared branch). That is
the correct and intended behavior of a fail-closed scan of "what actually
exists," not a defect; a stable count was never a real guarantee once the
guard's job is to catch files nobody has staged.

## 2. Decision — exact-path exemption for `t105-tamper-evidence.md`, not a directory prefix

Same pattern already used for the retirement ADR
(`docs/adr/0002-retire-browser-e2e-framework.md`), applied to exactly one
new path:

```
specs/004-agentic-development-foundation/t105-tamper-evidence.md
```

added to `EXEMPT_SPEC_PATHS` (folded into `EXEMPT_EXACT_PATHS`) in
`scripts/check-stale-playwright-references.mjs`, with a header-comment and
inline-comment rationale. **Not** a
`specs/004-agentic-development-foundation/**` prefix, and not a change to
the existing `specs/_backlog/**` non-exemption or any other named path in
that allowlist — this is one file, named once, for the reason the file
itself states: its tamper matrix must quote retired-harness invocations
verbatim as evidence of what was broken and restored, which is historical
proof of a completed test, not a living instruction. No other file in
`specs/004-agentic-development-foundation/` was touched or newly exempted.
Ripley's parallel governance evidence file
(`t105-governance-evidence.md`) needs no exemption — it already scans clean
without one, confirmed by both Bishop's review and a direct re-run in this
session.

Two focused tests were added to
`scripts/check-stale-playwright-references.test.mjs`:

1. `exempts t105-tamper-evidence.md by its exact path — historical
   tamper-matrix evidence, not living instruction (T105 revision, B-1)` —
   drives `findStaleReferences()` with a representative excerpt of the real
   file's content (a quoted historical break/restore entry) at the exact
   path and asserts zero violations.
2. `does NOT exempt an unlisted/different foundation evidence file naming
   an active future … promise — regression guard against a
   specs/004-agentic-development-foundation/** blanket exemption (T105
   revision, B-1)` — drives the same function with a **different, unlisted**
   path in the same directory
   (`specs/004-agentic-development-foundation/t105-evidence-revision-probe.md`)
   containing a genuine forward-looking commitment to add a new browser
   test suite, and asserts it still fails at the correct line.

`node --test scripts/check-stale-playwright-references.test.mjs` →
**22/22 pass** (20 pre-existing + 2 new).

## 3. Live proof — exact scan semantics, counts, and hashes

All commands below were run directly against the real working tree on this
machine (Windows), no synthetic fixtures, no repository mutation left
behind. `git checkout --` was never used to restore anything.

### 3.1 Before this revision (the bug, reproduced)

```
node scripts/check-stale-playwright-references.mjs
```
→ exit 0 — 0 active references across **933 tracked file(s)** (old
enumeration; `git ls-files -z` only). At that moment `git status --short`
showed three untracked, non-ignored files:
`docs/adr/0002-retire-browser-e2e-framework.md`,
`specs/004-agentic-development-foundation/t105-governance-evidence.md`, and
`specs/004-agentic-development-foundation/t105-tamper-evidence.md` — none of
them scanned. This is the live reproduction of B-2's root cause: the tool
can pass "clean" while structurally blind to files that matter.

### 3.2 After the scan-surface widening (§1)

```
node scripts/check-stale-playwright-references.mjs
```
→ exit 0 — `0 active … references across 936 tracked + untracked
non-ignored file(s)`. 936 = 933 tracked + the same 3 untracked files above,
confirmed independently via:

```
git ls-files --cached --others --exclude-standard | Measure-Object -Line   # 936
git ls-files --others --exclude-standard                                    # the 3 paths above, exactly
```

The real ADR and this session's evidence files are now genuinely inside the
executed scan — not asserted, proven by the file list the tool itself
enumerated on this run.

### 3.3 Denominator drift — disclosed, not hidden

Later in the same session, `task check:stale-refs` reported **937** files.
`git status --short` at that point showed a fourth untracked, non-ignored
file, `scripts/check-gitleaks-installed.mjs` — Hudson's concurrent, unrelated
B-3/B-4 work landing on the same shared branch. This is expected: the
revised guard reports whatever the real untracked-non-ignored set is at run
time, and that set is not under this revision's control. It does not affect
either B-1 or B-2 — the ADR and the evidence file remain correctly included
regardless of what else is present.

### 3.4 Hashes — corrected and current

| File | SHA1 (current bytes, this session) | Note |
| --- | --- | --- |
| `docs/adr/0002-retire-browser-e2e-framework.md` | `70a965da62f4ec9a82bd4bb192466f37ee057537` | Corrects the stale `4ddc1978a83bed1bc6b389fca38bb48e4f22bfde` recorded in `t105-tamper-evidence.md` §2.3 (B-2). Read-only; never edited by Hicks. |
| `scripts/check-stale-playwright-references.mjs` | `4e6e094c251b2d6959f5f43de4b3beed8977a0d5` | After §1/§2 changes (scan-surface widening, exact-path exemption, corrected messages/comments). |
| `scripts/check-stale-playwright-references.test.mjs` | `99ca2f55ca0eb2167bbc45de371fd48f2703eb12` | After the 2 new tests in §2. |
| `specs/004-agentic-development-foundation/t105-tamper-evidence.md` | `d77763df649833946bb46bc4a09ac314f79c9137` | Final, corrected content (this revision's edits applied) — see §4. |

### 3.5 Tamper test 1 — untracked active doc/spec, retired-framework promise (new file type, not an ADR)

- Created (never staged, never committed):
  `specs/004-agentic-development-foundation/t105-tamper-probe-hicks.md`,
  containing a line committing to add a new browser-driven end-to-end suite
  next sprint using the retired tool by name.
- Command: `node scripts/check-stale-playwright-references.mjs`
- Observed: **exit 1** — 1 violation reported at
  `specs/004-agentic-development-foundation/t105-tamper-probe-hicks.md:3`,
  offending line quoted verbatim. Caught with **zero staging** — the file
  was never `git add`ed.
- Restore: `Remove-Item` the probe file (it was never tracked, so deletion
  is full restoration; nothing else on disk was touched).
- Re-run: **exit 0** — 0 active references across 936 file(s) (denominator
  back to the §3.2 baseline exactly).

### 3.6 Tamper test 2 — untracked new ADR, retired-framework promise (paired negative case, matching §2.3's original scenario)

- Created (never staged, never committed):
  `docs/adr/0003-t105-tamper-probe-hicks.md`, containing an explicit
  commitment to adopt the retired browser tool for visual snapshot testing —
  a live plan, not a historical citation.
- Command: `node scripts/check-stale-playwright-references.mjs`
- Observed: **exit 1** — 1 violation at
  `docs/adr/0003-t105-tamper-probe-hicks.md:3`, offending line quoted
  verbatim. Caught with **zero staging**, unlike the original §2.3 negative
  case, which required a deliberate `git add` to be visible to the
  tracked-only enumeration.
- Restore: `Remove-Item` the probe file (never tracked).
- Re-run: **exit 0** — 0 active references across 937 file(s) (Hudson's
  concurrent untracked file, §3.3, was already present by this point;
  denominator matches the state immediately before this probe was created).

### 3.7 Tracked/staged exact evidence file still passes

- `git add specs/004-agentic-development-foundation/t105-tamper-evidence.md`
  → `git status --short` shows `A  specs/…/t105-tamper-evidence.md`.
- Command: `node scripts/check-stale-playwright-references.mjs`
- Observed: **exit 0** — 0 active references across 937 file(s) (staging
  does not change which files exist on disk, only the index; the guard's
  file list already included this path as untracked before staging, so the
  count is unchanged).
- Restore: `git reset -- specs/004-agentic-development-foundation/t105-tamper-evidence.md`
  (unstage only — never `git checkout --`, and no working-tree bytes were
  ever touched by staging in the first place).
- Verified: `git hash-object` on the file before staging, immediately after
  staging, and immediately after unstaging all return the identical
  `d77763df649833946bb46bc4a09ac314f79c9137` (§3.4). Byte-for-byte
  unchanged throughout.

### 3.8 Repository state after all tamper tests

`git status --short` shows only: the intentional edits this revision made
(`scripts/check-stale-playwright-references.mjs`,
`scripts/check-stale-playwright-references.test.mjs`, and the corrected
`specs/004-agentic-development-foundation/t105-tamper-evidence.md`, all
still untracked/modified exactly as before this session began), Hudson's
independent concurrent B-3/B-4 changes (untouched by this document), and no
other repository's pre-existing modified/untracked files. No `t105-tamper-probe-hicks.md`,
no `0003-t105-tamper-probe-hicks.md`, no `t105-evidence-revision-probe.md` —
every scratch path created during this revision was deleted, and every
staged file was unstaged again.

## 4. Corrections applied to `t105-tamper-evidence.md` (B-1/B-2)

Edited in place, original authorship preserved, corrections marked visibly
rather than silently rewritten:

- Frontmatter: added a `revision:` block naming Hicks, the date, and the
  scope of this correction, pointing here for the full record.
- §2.3 "Live proof, positive case": the false sentence claiming the real ADR
  was "included in that 933 and no longer flagged" is struck through in
  place (not deleted — the original claim stays visible as evidence of what
  was wrong) with a `> CORRECTION` block explaining exactly why it was false
  (the ADR was untracked; 933 is tracked-only; the tool could not have seen
  it) and providing the true, current, reproducible proof (936 files,
  including the real ADR, confirmed by the actual enumerated file list).
- §2.3 "Post-change file state" table: the ADR's SHA1 is corrected in place
  (struck through, replaced) from the stale `4ddc1978…` to the current,
  verified `70a965da…`.
- New §2.3a: a short, explicit "B-1/B-2 revision" summary cross-referencing
  this document, so a reader of `t105-tamper-evidence.md` alone understands
  both defects were found and closed, without needing to reconstruct the
  history from the strikethroughs.
- Nothing else in `t105-tamper-evidence.md` was altered: §0.1–§0.3 (the two
  Task-wiring changes), §1 (guard inventory), §2.1–§2.2 (keyword and
  structural tamper tests, both proven with the file staged and therefore
  unaffected by the tracked-only blind spot), §3–§12 (every other guard's
  tamper matrix, all Guard 9/`check:security`-adjacent material, the final
  `task verify` table, and the closing "what could not be proven" section)
  are exactly as Apone wrote them. This document does not re-run or
  re-verify those other nine guards — that is outside B-1/B-2 and remains
  Apone's original, already-reviewed evidence.

## 5. Task verification

- `task check:stale-refs` — **pass**. Live guard: exit 0, 937 file(s)
  (current session state). Unit suite: 22/22.
- `task verify:contracts` — attempted. `check:stale-refs` (pass, as above)
  and `check:client-drift` (pass, 9/9 unit tests) both completed cleanly.
  The task graph then failed at `check:migration-drift`
  (`dotnet ef migrations has-pending-model-changes` reports pending model
  changes). This is **unrelated to B-1/B-2** — no file this revision
  touches is a domain model, a migration, or EF configuration — and is not
  investigated or fixed here; it is disclosed as an observed, pre-existing
  condition on this shared branch outside this revision's scope (Hudson's
  B-3/B-4 territory, or a prerequisite for whoever integrates the full
  branch). Fixing it would require touching files this task explicitly
  reserves to Hudson or to the coordinator.
- Full `task verify` was not attempted, since it depends on the same
  `check:migration-drift` stage.
- `Taskfile.yml` itself was not edited by this revision, per instruction —
  its parse-ability depended on Hudson's concurrent edits mid-session (it
  failed to parse early in this session, then parsed cleanly once Hudson's
  edit had progressed; this document neither caused nor fixed that).

## 6. Readiness for Bishop re-review

B-1 and B-2 are closed:

- B-1: `t105-tamper-evidence.md` now coexists with the guard via a single,
  named, exact-path exemption — not a directory prefix — paired with a
  negative regression test proving the exemption cannot be walked into a
  blanket allowance. Proven both staged and unstaged, byte-identical
  throughout.
- B-2: the false "included in the 933" claim and the stale ADR hash are
  corrected in place, struck through rather than silently erased. The
  underlying structural gap (a guard that could never prove it scanned an
  untracked file) is closed for every future run, not just papered over for
  this one ADR — proven by two independent tamper tests (a foundation-spec
  file and a new ADR) that now fail **without staging**, where the original
  guard could only be tested by first `git add`ing the probe.

B-3 and B-4 (Hudson's) are untouched by this document and are not claimed to
be resolved here. Canonical `tasks.md` and `validation.md` are untouched by
design. This document does not mark T105 `DONE` — that verdict is Bishop's.

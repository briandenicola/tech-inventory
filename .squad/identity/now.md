---
updated_at: 2026-09-03T15:15:00Z
focus_area: Wave 5 complete; all seven issues closed, all two PRs merged; manual PWA validation (M-19/M-20/M-21) pending device testing; concurrent PR #149 proceeding independently
active_issues: Wave 5 merged to main at commit 7247623 (+ concurrent PR #166 post-merge → 9558e51); 7 GitHub issues closed; zero QC blockers; 1381 tests cumulative green
---

# What We're Focused On

**Wave 5 Device-List Cleanup & PWA Chrome Refinement is COMPLETE and APPROVED** (commits `c015cebf` → `7247623`, 2026-09-03).

Seven feature issues implemented in two parallel PR streams, validated comprehensively, and merged to main with zero blockers:
- **PR #162:** Device-list cleanup — status badges (#142), PWA single-view (#141), list-only guards (#145)
- **PR #163:** PWA chrome refinement — bottom-nav pill (#143), anchored menu (#146), app-bar scroll (#147), date containment (#148)

**Current state:**
- All 7 issues closed (#142, #141, #145, #143, #146, #147, #148)
- All 2 PRs merged to main (wave 5 span: c015cebf → 7247623; concurrent #166 → 9558e51 current HEAD)
- Cumulative main audit (7247623): 1381 tests green (279 unit + 316 integration + 786 frontend)
- Architecture review: Ripley QC complete, 0 blockers
- Security gates: gitleaks, Trivy, SBOM, vulnerability scan all green
- Accessibility: 0 axe-core violations across all PWA routes

**Immediate next action:** Manual PWA validation (M-19/M-20/M-21 checklist, owner: Brian)
- M-19: Pull-to-refresh behavior on iOS 17.1+ (gesture support, notification)
- M-20: Manifest icon rendering on Android 13+ (home screen, app drawer, 192px + 512px variants)
- M-21: Bottom-nav persistence & scroll behavior (stickiness across routes, scroll containment)
- Pending items: Explicit owner assignment to Brian; pre-release gate status; completion target

**Wave 5 engineering decisions codified:**
- **D-185:** Z-Index canonical ladder (sticky 20 < fixed 30 < modal-backdrop 40 < modal 50 < popover 60 < tooltip 70)
- **D-186:** PWA presentation modes (three orthogonal patterns: PWA, Mobile, Desktop; detected via `isStandalonePwa()`, never viewport breakpoints)
- **D-187:** Fixed-element containing-block safety (all `transition-*`, `will-change`, `filter` on layout wrappers must be conditional or absent; WebKit bug 160953 mitigation)
- **D-188:** Reference bulk-delete (#130) — no server-side referential guard; client-side pre-flight only; backend hardening recommended
- **D-189:** Device edit Status control (#133) — no new ownership gating; pre-existing authorization gap documented

**Phase**: Post-Wave 5 release candidate. Main ready for manual PWA validation and release. No further product code changes needed before M-19/M-20/M-21 sign-off.

**Concurrent work:** PR #149 (`squad/149-api-key-support-spec`) proceeding independently on separate branch; no blockers from Wave 5; state unchanged.

Updated by Scribe at session end: Wave 5 complete, all 7 issues closed, both PRs merged, cumulative gate passed, manual PWA checks M-19/M-20/M-21 pending Brian's device testing.
---
updated_at: 2026-09-03T13:40:00Z
focus_area: Wave 3/4 complete; manual PWA validation and backlog triage ahead
active_issues: Wave 3/4 merged to main (PRs #158, #159, #160, #161); QC audit complete, 0 blockers
---

# What We're Focused On

**Wave 3/4 Desktop & Mobile UX is COMPLETE and APPROVED** (commit `9895a70`, 2026-09-03).

Three feature streams implemented in parallel, validated comprehensively, and merged to main with zero blockers:
- **PR #158:** Definition-list semantic refactoring for device detail (issue #137)
- **PR #159:** Inline quick-create for Category/Brand/Location from device form (issue #136)
- **PR #160:** Desktop primary navigation + mobile compact menu density (issues #134/#144)
- **PR #161:** Merge-order correction for nav spacing baseline alignment (design-token hygiene)

**Immediate next action:** Manual PWA validation (M-17/M-18 checklist)
- M-17: Pull-to-refresh behavior on iOS 17.1+ (owner: Brian)
- M-18: Manifest icon rendering on Android 13+ (owner: Brian)
- Pending items: `max-h-[85vh]` token convention ADR, DeviceForm composition extract recommendation, cosmetic AppLayout spacing polish

**Wave 3/4 state:**
- Code coverage: 87% on Domain+Application (exceeds 85% floor)
- Tests green: 279 unit + 316 integration + 760 frontend = 1355 ✓
- Accessibility: 0 axe-core violations across all three streams
- QC audit: Ripley review complete, 0 blockers identified
- Permission gates: +23 tests added, 100% coverage on quick-create mutations
- Main branch: Commit `9895a70`, ready for deployment

**Backlog triage complete** (#127–#148 prioritized in seven-wave sequence):
- Wave 5: #130 (Admin bulk-delete contract mismatch)
- Wave 5b: #129+#138 (Admin bulk-deactivate 404 fix)
- Wave 6: #133→#127 (Priority: silent status reset fix → retire/unretire unwind)
- Wave 6b: #132→#128 (Filter category tree, inactive badge)
- Wave 7: #135→#139 (Settings placeholder, nav label)
- Wave 8: #142→#145→#141→#143→#146→#147→#148 (PWA wave: batch operations, export refinement, reporting dashboard)
- Wave 9: #131→#136 (Clone device, quick-create refinement)
- Wave 10: #134→#144→#137 (Advanced nav patterns, mobile density follow-up)

**Known follow-ups** (post-Wave 3/4, non-blocking):
- F-17: Align `check-security.mjs --repo` to tracked + untracked file enumeration
- Migration drift: Make check clean/rebuild-safe for incremental build output
- Design-token convention: Establish guideline for `max-h-[85vh]` vs. semantic tokens
- Component size: DeviceForm (590 lines) recommend composition extract in next polish
- Cosmetic Polish: AppLayout spacing micro-adjustment (D-169, deferred)

**Phase**: Post-Wave 3/4 release candidate. Main ready for manual PWA validation and/or deployment. No further product code changes pending.

Updated by Scribe at session end: Wave 3/4 merged, QC audit complete, 0 blockers, ready for manual PWA validation.
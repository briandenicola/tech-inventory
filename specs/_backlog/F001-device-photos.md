IDs are **never reused** even if a feature is killed.

---

## 📝 Template

Every backlog file must use this template. Copy `_TEMPLATE.md` to start.

```markdown
# F00X: Feature Name

**Status**: backlog | clarifying | scheduled | in-progress | shipped | killed
**Priority**: P0 | P1 | P2 | P3
**Effort**: S (< 1 week) | M (1–2 weeks) | L (2–4 weeks) | XL (> 4 weeks)
**Value**: high | medium | low
**Risk**: high | medium | low
**Target release**: v1 | v2 | v3 | TBD
**Created**: YYYY-MM-DD
**Owner**: [name]

## Problem
What pain does this solve? Who feels it? How often?

## Proposed Solution
High-level idea. NOT implementation.

## User Stories
- As [persona], I want [capability] so that [outcome].

## Acceptance Criteria
- [ ] Given X, when Y, then Z

## Out of Scope
- What this feature explicitly will NOT do

## Dependencies
- Blocked by: F00X, spec 00X
- Related: F00Y

## Open Questions
- ?

## Notes / Research
- Links, sketches, references

## History
- YYYY-MM-DD: created
- YYYY-MM-DD: status → scheduled
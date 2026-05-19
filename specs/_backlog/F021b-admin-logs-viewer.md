# F021b: Admin System Logs Viewer (`/admin/logs`)

**Status**: backlog
**Priority**: P3
**Effort**: M
**Value**: medium
**Risk**: low
**Target release**: v1.2
**Created**: 2026-05-19
**Owner**: unassigned
**Carved out from**: F021 (which shipped v1 with `/admin/audit` only).

## Problem
F021 shipped the audit-log viewer; the second half of that spec — an in-app
Serilog viewer — is still missing. For a self-hosted, single-household
deployment without a separate observability stack, basic in-app log visibility
is the difference between "an admin can diagnose a problem" and "an admin
needs to SSH into the container."

## Proposed Solution
Admin-only `/admin/logs` route:
- Tail-style view of the last N Serilog entries (default 500, configurable).
- Filters: level (Debug/Info/Warn/Error), source context, free text.
- Live tail toggle (Server-Sent Events) — optional, polling fallback acceptable.
- Strict redaction: never render `Authorization`, `Cookie`, JWT contents, or
  any property tagged sensitive per security baseline.

## Acceptance Criteria
- [ ] `/admin/logs` lives under the existing Admin layout; nav menu gains
      "System Logs" entry.
- [ ] Level + source context + free-text filters work and persist via URL.
- [ ] Backend exposes `GET /api/v1/admin/logs` (Admin policy) returning a
      paginated tail.
- [ ] Returns 403 for non-Admin (Playwright RBAC test covers it).
- [ ] Sensitive properties are redacted server-side before reaching the wire.
- [ ] Zero axe-core violations; mobile-friendly.

## Open Questions (must resolve before implementation)
- **Sink choice**: tail rolling file vs add a parallel in-memory ring buffer
  sink that the viewer can query efficiently? In-memory is simpler but
  capacity-bound; file is durable but harder to parse. Decision likely
  warrants an ADR.
- Retention / rotation policy interplay if we go file-based.
- Whether SSE vs short-polling is worth the complexity for a single-household
  app.

## Out of Scope
- Long-term log shipping to external aggregator (Loki, ELK) — separate spec.
- Log retention/archival rotation as an ops concern.

## Dependencies
- F021 v1 shipped (`AuthorizationPolicies.Admin` already in place).
- Serilog sink decision (ADR).

## History
- 2026-05-19: carved out of F021 at v1 ship time so the audit slice could
  ship without blocking on the unresolved sink-choice question.

# F021: Admin Audit Log Viewer & System Logs UI

**Status**: shipped (v1 — `/admin/audit` only). `/admin/logs` carved out to F021b.
**Priority**: P2
**Effort**: M
**Value**: high
**Risk**: low
**Target release**: v1.1
**Created**: 2026-05-19
**Owner**: unassigned

## Problem
PRD `F7` (Audit & History) and `U3` (*"As Admin, I view audit history so I can
see who changed what"*) are explicit PRD requirements, and the backend is
already shipped:

- `AuditEvent` table is append-only with `Actor`, `EntityType`, `EntityId`,
  `Action`, `Timestamp`, `BeforePayload`, `AfterPayload` columns.
- `AuditBehavior` (MediatR pipeline) serializes BEFORE from `IAuditContext`
  and AFTER from any `IAuditable` request automatically.
- `IAuditEventRepository` exposes append + query operations.

However, there is **no admin UI** to query the audit log, filter by entity or
user, or inspect BEFORE/AFTER diffs. Admins currently have to query SQLite
directly or read structured Serilog output on the host to investigate
"who changed this device" — defeating much of the audit log's value.

A related gap: Serilog application logs (request log, errors, security events)
are written to the host file system but have no in-app viewer either. For a
self-hosted, single-household deployment without a separate observability
stack, basic in-app log visibility is the difference between "an admin can
diagnose a problem" and "an admin needs to SSH into the box."

## Proposed Solution
Add an Admin-only section under `/admin/audit` and `/admin/logs` that surfaces
the existing data without inventing new infrastructure.

### `/admin/audit` — Audit History viewer
- Paginated table: timestamp, actor (display name + avatar), entity type +
  link, action, change summary.
- Filters: entity type (Device/Brand/.../Owner), action
  (Create/Update/Deactivate/etc.), actor (owner dropdown), date range, free
  text on `EntityId`.
- Row click → side drawer or detail page with BEFORE/AFTER payload rendered as
  a **JSON diff** (added green, removed red, changed amber) using an
  established diff library (e.g., `jsondiffpatch` or `deep-object-diff`).
- "Export selected rows as CSV/JSON" reuses the existing export plumbing
  patterns from F4 (T39).
- Deep-link support: `/admin/audit?entityType=Device&entityId=<guid>` so the
  device detail page can link "View change history" → filtered view.
- Link integration on device detail page: a "History" tab or section anchored
  to the same `EntityId` lookup.

### `/admin/logs` — Application logs viewer
- Tail-style view of the last N Serilog entries (default 500, configurable).
- Filters: level (Debug/Info/Warn/Error), source context, free text.
- Live tail toggle (Server-Sent Events) — optional in v1.1, polling fallback
  acceptable.
- Strict redaction: never render `Authorization`, `Cookie`, JWT contents, or
  any property tagged sensitive per security baseline.

Both pages live behind the `Admin` policy and respect the existing
content-security baseline (no inline scripts, sanitized JSON rendering).

## User Stories
- **U3** (existing PRD): *As Admin, I view audit history so I can see who
  changed what.*
- *As Admin, I can answer "who deactivated this device last Tuesday?" without
  shelling into the container.*
- *As Admin, I can confirm a suspicious change was real-user-driven (Member)
  vs. system-driven (Import batch) by looking at the actor on each audit row.*
- *As Admin, I can investigate a 500 error reported by a family member by
  filtering logs to that user's `oid` and the relevant time window.*

## Acceptance Criteria
- [ ] `/admin/audit` route lives under the existing Admin layout; nav menu
      gains "Audit Log" entry alongside Brands/Categories/Owners
- [ ] Audit table supports all listed filters and survives reload via
      URL-backed search params (same pattern as `/devices`)
- [ ] BEFORE/AFTER diff drawer renders meaningfully for every entity type
      (smoke-test all 7: Device, Brand, Category, Owner, Location, Network,
      Tag)
- [ ] Device detail page links to the audit history for that device id
- [ ] `/admin/logs` route surfaces recent Serilog entries with level + free
      text filters
- [ ] Both routes return 403 (or redirect to `/403`) for non-Admin roles —
      covered by Playwright `J13`-style RBAC test
- [ ] Zero axe-core violations; works on mobile (filters collapse to drawer)
- [ ] No new secret leakage paths — verified by adding the routes to the
      sensitive-property redaction smoke test

## Out of Scope
- Long-term log shipping to external aggregator (Loki, ELK) — separate spec
  if/when we outgrow file-based logging
- Audit log retention policy or archival rotation (separate ops concern)
- Per-user notifications on audit events (requires notification plumbing)
- Modifying or deleting audit rows — table is append-only by constitution

## Dependencies
- Backend audit query endpoint (read-only, paginated, filterable) needs to be
  exposed on the API. `IAuditEventRepository.QueryAsync` exists; surface as
  `GET /api/v1/audit?...` with Admin policy and OpenAPI contract.
- JSON diff library decision (additive dep — needs ADR or lightweight D-note).
- For `/admin/logs`: Serilog sink choice (in-memory ring buffer? read tail of
  rolling file? structured JSON parse?). Needs decision before
  implementation.

## Open Questions
- Should the audit query endpoint paginate by cursor (timestamp) or
  offset/page? Cursor is more correct for append-only data but requires more
  client work.
- For the log viewer, is reading the Serilog rolling file the right source,
  or should we add a parallel in-memory sink that the viewer can query
  efficiently?
- Do Member-role users get any read access to audit rows that reference their
  own actions? (Probably no — keep the "Admin sees all" boundary clean for
  v1.1.)

## Notes / Research
- Backend audit infrastructure already merged — see memories: *"AuditEvent is
  append-only"* and *"Auditable MediatR requests implement IAuditable and
  populate scoped IAuditContext"*.
- Spec deferral statements that anticipate this work:
  - `specs/002-frontend-mvp/spec.md:41` — Admin user management UI deferred to
    v1.1.
  - `specs/002-frontend-mvp/spec.md:427` — Admin Console explicitly v1.1.
- PRD `F8` (Admin Console) overlaps — user list with roles & last login could
  live in a sibling `/admin/users` route from the same v1.1 effort.

## History
- 2026-05-19: created — captured during Add Device modal session in response
  to Brian asking about per-user settings + in-app log viewing
- 2026-05-19: shipped v1 (`/admin/audit`). Backend gained
  `[Authorize(Policy = AuthorizationPolicies.Admin)]` on the audit-events
  controller, `Auth:DevBypassRole` config knob so integration tests can stamp
  Member, and a Member→403 contract test. Frontend got `/admin/audit` page
  with URL-backed filters + paginated table, `AuditDiffDrawer` with
  added/removed/changed/unchanged coloring (in-house `jsonDiff.ts` helper,
  no new deps), Admin nav entry, and a "View change history" deep link from
  `DeviceDetailModal` → `/admin/audit?entityType=Device&entityId=<id>`.
  `/admin/logs` deferred to **F021b** (separate spec) because the Serilog
  sink-choice open question still needs an ADR.

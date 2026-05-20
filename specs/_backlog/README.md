# Feature Backlog

This directory is the **pre-formalized pipeline** for features. Ideas land here
first as lightweight notes, get triaged, prioritized, and — when ready — promoted
to a formal `specs/00X-feature/` directory via Spec Kit's `/specify` command.

---

## 📋 How It Works

```
idea
  ↓
specs/_backlog/F00X-name.md       ← capture (this directory)
  ↓
triage, prioritize
  ↓
specs/00X-feature-name/spec.md    ← promote via /specify
  ↓
/clarify → /plan → /tasks → /implement
  ↓
merged; this backlog item flipped to "shipped" and (when scope was carved)
followed by an `F00Xb-*.md` covering everything that was deferred
```

A backlog item is **lightweight on purpose**. Its job is to keep an idea alive
without burning the constraint of a full spec. The moment it's worth a full
spec, it gets promoted out of `_backlog/` into `specs/00X-feature-name/`.

---

## 🗂️ File Naming

`F` + 3-digit number + short kebab-case name, e.g. `F023-group-devices-by-dimension.md`.

When a shipped slice carves out follow-up work, the follow-up gets the same
number with a `b` suffix (e.g. `F020` ships → remaining scope tracked in `F020b`).
The pattern can extend (`c`, `d`, …) if a feature carves multiple times, but
in practice anything past `b` is a signal that the work deserves a fresh
top-level number instead.

Every backlog file should start with a `**Status**:` line whose first word is
one of:

| Status      | Meaning                                                              |
| ----------- | -------------------------------------------------------------------- |
| `backlog`   | Captured; not yet picked up.                                         |
| `triaged`   | Triaged; priority assigned; waiting for a slot.                      |
| `promoted`  | Promoted to `specs/00X-feature/`; backlog entry kept as a pointer.   |
| `shipped`   | Shipped (with the version that shipped and a note on any carve-out). |
| `dropped`   | Explicitly decided against; reason recorded in the body.             |

---

## 📊 Current State (snapshot)

Authoritative status lives in each `F*.md`'s own header — this table is a
human-friendly index, refreshed alongside doc audits. When the two disagree,
the per-file header wins.

> **Phase context**: Under the PRD §13 model rewritten 2026-05-19, all
> backlog work falls under **P4 — Continuous Iteration**. Older backlog
> files may still reference "Phase 4 — Polish & Reporting" wording from the
> pre-rewrite numbering; treat those as historical labels for what is now
> P4 work.

| Item    | Status                                                                                 | What shipped / what's left                                                                  |
| ------- | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| F018    | backlog                                                                                | Photo-to-device AI draft. Untouched.                                                        |
| F019    | backlog                                                                                | Household AI chatbot. Untouched.                                                            |
| F020    | shipped (v1 — Profile / display-name)                                                  | Avatar, preferences, session tab carved to F020b.                                           |
| F020b   | backlog                                                                                | Avatar, preferences, session tab — follow-up to F020 v1.                                    |
| F021    | shipped (v1 — `/admin/audit` only)                                                     | `/admin/logs` viewer carved to F021b.                                                       |
| F021b   | backlog                                                                                | `/admin/logs` viewer — follow-up to F021 v1.                                                |
| F022    | shipped (v1.0 — localStorage)                                                          | Server-sync follow-up still tracked in the file; no separate `b` entry yet.                 |
| F023    | shipped (v1)                                                                           | Group-devices-by-dimension landed whole; no carve-out.                                      |
| F024    | shipped (v1)                                                                           | Multi-select bulk actions landed; power-user extras carved to F024b.                        |
| F024b   | backlog                                                                                | Bulk-actions power-user features — follow-up to F024 v1.                                    |
| F025    | shipped (v1b — bootstrap seed + login + change-password) per ADR D-140                 | Admin CRUD UI, lockout enforcement, rate limiting, refresh cookies, soft delete, last-Admin guard, self-service convert-to-local carved to F025b. |
| F025b   | backlog                                                                                | Local-admin power features — follow-up to F025 v1b. See ADR D-140 for v1b scope rationale.  |
| F026    | backlog (P1)                                                                           | PWA quick-win UX pack: transparent-overlay bug, "+" FAB, Active-by-default filter, 2-up cards, audit modal, pinch-zoom off. |
| F027    | backlog (P2)                                                                           | Global hamburger nav + responsive admin management pages.                                   |
| F028    | backlog (P2)                                                                           | Infinite scroll + pull-to-refresh on list pages.                                            |
| F029    | backlog (P2)                                                                           | Dark-mode toggle in Settings + repair audit-log contrast.                                   |
| F030    | backlog (P1)                                                                           | Device tagging — repro the broken add-tags flow, ship `<TagPicker>`.                        |
| F031    | backlog (P2)                                                                           | Merge duplicate Brands / Categories / Locations with full AuditEvent trail.                 |
| F032    | backlog (P2)                                                                           | Insurance-ready PDF inventory report — first concrete Phase-4 reporting entry.              |
| F033    | backlog (P3)                                                                           | Device images, room-capture flow, auto-creation from photos. Umbrella; pairs with F018.     |

### Roll-up

- **Shipped (v1 or v1b)**: F020, F021, F022, F023, F024, F025 — **6 items**.
- **Carved follow-ups still in backlog**: F020b, F021b, F024b, F025b — **4 items**.
- **Untouched ideas**: F018, F019, F026, F027, F028, F029, F030, F031, F032, F033 — **10 items**.
- **Total backlog files**: 20.

### 2026-05-19 PWA field-test batch (F026–F033)

F026 through F033 were captured in one batch from Brian's PWA testing session
on 2026-05-19. The session plan that grouped his raw feedback into these
files lives in the agent's session-state folder; the per-file `History`
sections cite the originating item numbers. F032 also doubles as the answer
to Brian's question "what happened to the reporting backlog?" — Phase-4
reporting was scoped in `specs/002-frontend-mvp/plan.md:238` and `docs/prd.md`
but had no concrete backlog file until now.

### Notes on the shipped slices

- **F025 v1b** is the most recently shipped item and is the source of a lot of
  cross-doc churn. The authoritative description of what shipped lives in
  ADR **D-140** in [`.squad/decisions.md`](../../.squad/decisions.md);
  operator runbook lives in [`docs/operations.md`](../../docs/operations.md);
  deployment-side env knobs live in [`docs/deployment.md`](../../docs/deployment.md);
  developer-facing test inventory lives in [`docs/testing.md`](../../docs/testing.md).
- **F020 / F021 / F024** all carved follow-ups (`F020b` / `F021b` / `F024b`)
  rather than shipping their full original scope. That's the intended pattern
  — ship the user value, capture the rest, don't let one feature block on
  perfection.
- **F022** shipped against `localStorage` (per its original scope) and is the
  only currently-shipped feature that does *not* yet have a `b` entry; the
  server-sync follow-up is tracked inline in the F022 file. Promote to F022b
  when the server-sync work is ready to start.

---

## 🔁 Triage Cadence

Backlog triage is whatever the team agrees on — typically every couple of
working sessions, or when a slot opens up after a feature ships. The triage
deliverable is small: pick which backlog item promotes next, and (if the
shipped slice carved out work) ensure the `b` follow-up exists with a clear
"out of scope for v1" list.

Triage outputs land in [`.squad/decisions.md`](../../.squad/decisions.md)
when they change priority or scope.

---

## 🔗 Related

- [`.squad/decisions.md`](../../.squad/decisions.md) — authoritative decision
  ledger (D-001…D-140 at time of writing).
- [`docs/architecture.md`](../../docs/architecture.md) — system architecture
  (C4 L1/L2, auth pipeline) — useful context when promoting.
- [`docs/testing.md`](../../docs/testing.md) — what to add tests for when a
  backlog item promotes and ships.

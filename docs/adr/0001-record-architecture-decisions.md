# 1. Record Architecture Decisions

Date: 2026-05-17
Status: Accepted

## Context
We need an immutable trail of material decisions.

## Decision
The authoritative decision ledger for this project is [`.squad/decisions.md`](../../.squad/decisions.md), an append-only file containing numbered entries `D-001 …` (currently through **D-140**). Each entry records context, decision, rationale, related entries, and consequences.

This `docs/adr/` directory holds the standalone-ADR template (this file) for any decision that warrants its own document rather than an inline `D-NNN` entry. In practice the team has consolidated on the `.squad/decisions.md` ledger; standalone ADRs are reserved for decisions that need long-form rationale or external citation.

## Consequences
+ Single source of truth — search one file to find any decision
+ Institutional memory survives team / agent rotation
+ AI agents can re-ground from a single ledger before each task
+ Append-only invariant prevents retroactive rewriting
- Discipline required to write entries promptly
- File size grows monotonically — `.squad/decisions.md` is large; future archival policy may carve old entries into `decisions-archive.md`


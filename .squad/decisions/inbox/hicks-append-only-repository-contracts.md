# Hicks Decision Inbox — Append-only audit and repository contracts

## Proposed Decision

Land immutable `AuditEvent` / `ImportBatch` domain entities outside the mutable `Entity` base contract, keep repository abstractions in `TechInventory.Application`, and enforce append-only audit behavior in both the repository surface and `AppDbContext` save pipeline.

## Why this needs consensus

- `Entity` currently exposes public audit-stamping helpers (`SetAuditMetadata`, `Touch`) that are right for mutable aggregates but violate the append-only public-surface contract required for `AuditEvent`.
- Moving repository interfaces into `src/TechInventory.Application/Abstractions/Repositories/` formalizes a seam above Domain where `Result<T>`, paging DTOs, and persistence-facing query criteria can live without leaking EF Core or `IQueryable`.
- `IAuditEventRepository` now has `AppendAsync` plus query methods only, and `AppDbContext` rejects modified/deleted `AuditEvent` rows as a second-line safeguard.

## Landed shape in this branch

- `AuditEvent`: `Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, `BeforePayload`, `AfterPayload`; no public setters/mutators, private EF constructor only
- `ImportBatch`: immutable public fields for file/import summary, derived `ProcessedCount` / `HasErrors`, private EF constructor only
- Repository contracts: async-only, `CancellationToken` on every method, `Result<T>` / `Result` for expected failures, `PagedResult<T>` for paged reads, no `IQueryable` in parameters or return types
- Infrastructure: `AppDbContext` and migration guard append-only persistence now; concrete repository classes remain deferred to T16

## Questions for the team

1. Do we want to codify a dedicated immutable-domain-base type for append-only records, or keep `AuditEvent` / `ImportBatch` as stand-alone immutable entities?
2. Is `TechInventory.Application/Abstractions/Repositories/` the long-term home for repository contracts, criteria, and paging/results, or do we want a narrower sub-namespace split before handlers land?
3. Should all immutable persistence records get a DbContext-level modified/delete guard, or only security-sensitive append-only data like audit events?

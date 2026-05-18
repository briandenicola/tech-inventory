# Decision Proposal: SQLite Integration Isolation + Hermetic E2E Contract

**Author:** Hudson (DevOps / Platform)  
**Date:** 2026-05-18  
**Status:** Proposed  
**Related:** PRD §7.5.3, PRD §7.5.5, `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs`, `Taskfile.yml`

## Decision

1. Backend integration tests use **in-process SQLite with one fresh file per test class**, not Testcontainers.
2. `IntegrationTestFactory<TMarker>` owns that isolation by overriding `ConnectionStrings:Default`, recording any registered `DbContext` types, and applying EF Core migrations when both the DbContext and migrations exist.
3. `task test:e2e` owns the hermetic Playwright lifecycle: `docker compose up -d --build` → wait for `http://localhost:8080/health/ready` → run Playwright against `http://localhost:3000` → `docker compose down -v` in teardown.

## Rationale

- The architecture decision is SQLite-only. Running SQLite in-process matches production behavior better than standing up a container just to mimic a server database.
- A per-test-class database file keeps integration suites isolated without forcing every test method to pay startup cost.
- The one-shot E2E task gives CI and local laptops the same contract, including mandatory teardown even when Playwright fails.

## Consequences

- Future integration tests should follow the naming pattern `SomeFeatureTests : IClassFixture<IntegrationTestFactory<SomeFeatureTests>>` so the SQLite file maps cleanly to the owning test class.
- Hicks should register `AppDbContext` against `ConnectionStrings:Default`; if that is not possible, the TODO hook in `IntegrationTestFactory.ConfigureServices(...)` is the supported override point.
- Apone can expand HTTP coverage on the same factory immediately after migrations land; `task test:integration` is the entry point for that suite.

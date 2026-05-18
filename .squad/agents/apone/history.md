# Project Context

- **Owner:** Brian
- **Project:** Tech Inventory — self-hosted family device tracker. Mandatory testing bar enforced locally and in CI.
- **Stack:** xUnit + FluentAssertions + NSubstitute (backend unit). Testcontainers (backend integration). Vitest + Testing Library (frontend). **Playwright** (mandatory E2E, Chromium + WebKit + Firefox). axe-core for a11y. Lighthouse CI for performance. Schemathesis (or equivalent) for OpenAPI contract tests.
- **Created:** 2026-05-18

## Core Context

Authority: PRD §7.5 *Local Testing & Validation* + constitution §7 *Testing*. `docs/testing.md` operationalizes both.

Non-negotiables (`docs/testing.md` §Philosophy):
- Every change has tests at the appropriate level
- Playwright is the only E2E framework
- `task test` is the one command
- CI runs the exact same commands developers run
- Flaky tests are bugs — fix or delete within a working day
- Tests own their data, no shared fixtures across files
- No mocked DB in integration tests
- No mocked API in E2E
- No internet access in tests

Coverage floor: **85% line coverage on Domain + Application** layers.

13 mandatory critical user journeys (PRD §7.5.4) — all must be green before v1:
1. Sign in (Entra happy path, sign-out)
2. Sign in denied (no role)
3. Create device (Member)
4. Edit device (Member)
5. Delete device (Member, confirmation)
6. Browse and filter (Viewer; URL state, reload preserves view)
7. Detail view (Viewer; reference data resolved to labels)
8. Import CSV (Admin; preview, errors, commit)
9. Export CSV (Admin; filtered view parses cleanly)
10. Reference data admin (Admin creates Location, appears in form)
11. Role enforcement (Viewer cannot see edit/delete, direct nav refused)
12. Offline app shell (cached data viewable, mutations queued/refused gracefully)
13. Accessibility smoke (every route above passes axe-core, zero violations)

Removing a journey from this list requires an ADR.

Test commands:
- `dotnet test -c Release`
- `dotnet test --filter "FullyQualifiedName~MyTest"`
- `cd src/TechInventory.Web && pnpm run test`
- `pnpm run test -- --run src/lib/MyFile.test.ts`
- `npx playwright test`
- `npx playwright test tests/e2e/mytest.spec.ts`
- `task test` (full suite against running stack)

Playwright layout: tests in `tests/e2e/`, Page Object Model in `tests/e2e/pages/`. Test user provisioned via documented Entra test tenant fixture OR documented local-dev auth bypass (bypass NEVER in prod builds — enforced at API layer).

## Recent Updates

**2026-05-18 (Phase 1 Round 7):** Import/export close-out coverage landed under `tests/TechInventory.IntegrationTests/Controllers/ImportsControllerTests.cs`, `ExportControllerTests.cs`, and `tests/TechInventory.IntegrationTests/Contract/`. The import suite now locks preview/commit/list/get-by-id behavior, config-driven 413 handling, malformed-row reporting, and lookup auto-creation during commit; the export suite locks CSV/JSON happy paths, status filtering, and large-dataset export reads on the real SQLite `WebApplicationFactory` harness. T46 now has a reusable OpenAPI drift/schema harness that canonicalizes `/openapi/v1.json` against committed `openapi.yaml` and validates representative endpoint payloads, with one intentional skip remaining until `/api/v1/exports/devices` declares a 200-body schema. Final backend verification: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release --no-build` ✅ (**369 passed / 1 skipped**), `dotnet list package --vulnerable --include-transitive` ✅, and fresh merged coverage at **Domain 100.00% / Application 91.58% / Infrastructure 94.33% / Api 91.63%**. `scripts\verify.ps1` advanced through frontend install/check/lint and then stopped only because this environment lacks `docker` for the Playwright compose step.

**2026-05-18 (Phase 1 Round 6):** Controller HTTP integration coverage landed under `tests/TechInventory.IntegrationTests/Controllers/` for Devices, Brands, Categories, Owners, Locations, Networks, Tags, AuditEvents, ProblemDetails, and the Development auth bypass. Added **79** executable integration tests on top of Hudson's `IntegrationTestFactory<TMarker>` harness, with per-test database resets preserving D-018 isolation while keeping one SQLite file per test class. Hicks's landed route shapes are now locked by tests: CRUD lives at `/api/v1/{resource}`, categories expose `GET /api/v1/categories/tree`, audit events list at `/api/v1/audit-events`, device tag routes are `/api/v1/devices/{id}/tags` and `/api/v1/devices/{id}/tags/{tagId}`, and `PATCH /api/v1/devices/{id}/owner` returns **204 No Content**. Development auth bypass is also executable now: the test environment authenticates as a stable fixed subject with Admin role, and audit events capture that actor. Final backend suite summary: **345 passed / 0 skipped / 0 failed** (delta **+79** vs Round 5). Fresh merged coverage snapshot: **Domain 100.00%**, **Application 90.28%**, **Infrastructure 93.19%**, **Api 94.87%**. All requested checks green: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅, `dotnet test -c Release --collect:"XPlat Code Coverage"` ✅. **Bug fixed:** Category-archive tracking bug exposed by integration tests — surgical fix included (soft-delete cascade now correctly sets parent `IsActive=false` on ancestors when deleting intermediate nodes).

**2026-05-18 (Phase 1 Round 5):** Domain coverage fully recovered to 100.00% (from 81.40% regression flagged in R4). Added targeted coverage for `AuditEvent` default-timestamp guards and `ImportBatch` EF-safe private-constructor behavior via reflection-based immutability tests. Converted 102 skip-when-waiting handler scaffolds into 115 executable xUnit/NSubstitute tests once Hicks's T20–T28 handlers landed; handler test contracts lock active-reference validation, duplicate-name rejection via `Error.Code = "Conflict"`, BEFORE-snapshot audit capture, and owner delete-blocking invariant. Final coverage snapshot: **Domain 100.00%**, **Application 85.89%**, **Infrastructure 88.98%**. Backend test suite: **266 passed / 0 skipped**. All checks green: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅.

**2026-05-18 (Phase 1 Round 1):** Domain + integration tests complete. 13 xUnit tests in `tests/TechInventory.UnitTests/Domain/` covering Currency VO, Household defaults, Device inheritance/override, retired-device edit guards. `/health` integration test unskipped and passing. Playwright token-storage E2E (`tests/e2e/security/token-storage.spec.ts`) verifies no token-like keys in `localStorage` across 6 browser projects (Chromium, WebKit, Firefox × desktop + mobile). Decisions D-014 (Currency contract tests as executable spec) and D-015 (Playwright token-storage inspection pattern) document test patterns. 85% Domain layer coverage maintained. Token-storage four-gate enforcement (D-010) coordinated with Vasquez (ESLint), Hudson (pre-commit hook), and Bishop (code review checklist).

**2026-05-18 (Phase 1 Round 2):** Reference entity contract tests (T06–T10) complete and verified. 80 xUnit cases covering `Category`, `Owner`, `Location`, `Network`, `Tag`, `DeviceTag` plus supplemental Brand/Device state transitions. Domain line coverage: 97.6% (well above 85% floor). Decision D-016 (Reference Entity Contract Test Pattern) locked the spec-driven pattern for future aggregates. Blockers identified for next phase: T11 (AuditEvent append-only contract), T15 (Repository interface consumer tests via NSubstitute). Verify pipeline fully green: `dotnet test -c Release` ✅, coverage floor maintained.

**2026-05-18 (Phase 1 Round 4):** Behavior + repository coverage complete (T18–T19, +30 tests, 151 total green). `ValidationBehavior` + `AuditBehavior` pipeline ordering verified via integration test (D-021): validation first (short-circuits cleanly, never fires audit), audit last (appends only after success). 58 new behavior + repository integration tests in `tests/TechInventory.IntegrationTests/Repositories/` and `tests/TechInventory.UnitTests/Application/Behaviors/`. SQLite-backed concrete repository tests via `IntegrationTestFactory<TMarker>` confirm soft-delete filtering, inactive-row handling, unit-of-work awareness. Coverage snapshot: Domain 81.40%, Application 40.53%, Infrastructure 88.98%. **Regression note:** Domain coverage dipped from 96.45% pre-Round-4 to 81.40% post; likely Hicks's `AuditEvent`/`DbContext` additions not yet covered by test suite — flagged for explicit Round 5 audit. All checks green: `dotnet test -c Release` ✅, format ✅, build ✅.


## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-05-18: Import/export integration + OpenAPI drift patterns

**Import controller test pattern:**
- For config-driven API limits, derive a marker-specific `IntegrationTestFactory<TMarker>` and override configuration with `Imports:MaxFileSizeBytes` so 413 paths are executable without posting multi-megabyte fixtures.
- `POST /api/v1/imports/commit` requires a seeded `Household`; malformed-CSV commit cases should assert the returned batch/error summary instead of assuming a 400 transport failure.

**Export controller test pattern:**
- Use `HttpCompletionOption.ResponseHeadersRead` plus body parsing for export assertions, but avoid transfer-encoding assumptions under `TestServer`; the in-memory host may buffer even when the controller streams.
- Check `Content-Disposition` from either `response.Headers` or `response.Content.Headers` because `TestServer` can surface it through either collection.

**OpenAPI contract pattern:**
- Keep committed-vs-runtime drift checks and endpoint schema checks in the same `tests\TechInventory.IntegrationTests\Contract\` harness: canonicalize documents once, then validate representative JSON payloads with a local schema walker.
- Resolve the committed spec from repo-root `openapi.yaml` first, with `src\TechInventory.Api\openapi.yaml` as a transition fallback for older branches.

### 2026-05-18: Test Infrastructure Scaffolding

**Backend test projects:**
- Created `tests/TechInventory.UnitTests/` with xUnit + FluentAssertions + NSubstitute (net10.0)
- Created `tests/TechInventory.IntegrationTests/` with xUnit + Microsoft.AspNetCore.Mvc.Testing + Testcontainers
- Both projects build and run; smoke tests in place
- Project references to Hicks's source projects commented out with TODOs — will be uncommented once Domain/Application/Infrastructure/Api are scaffolded

**Playwright E2E:**
- Standalone project at `tests/e2e/` with @playwright/test + @axe-core/playwright
- Config: 6 project combinations (Chromium/WebKit/Firefox × desktop 1280×800 / mobile 375×667)
- Network isolation enforced via `fixtures/network.ts` — non-localhost calls fail tests
- All 13 critical journeys stubbed as `test.describe.skip()` with `test.todo()` placeholders:
  1. Sign in (Entra ID + sign-out)
  2. Sign in denied (no role)
  3. Create device (Member)
  4. Edit device (Member)
  5. Delete device (Member, with confirmation)
  6. Browse & filter (URL state, reload preservation)
  7. Detail view (resolved references)
  8. Import CSV (preview, errors, commit)
  9. Export CSV (filtered, valid)
  10. Reference data admin (Location appears in form)
  11. Role enforcement (Viewer restrictions)
  12. Offline app shell (PWA degradation)
  13. Accessibility smoke (axe-core, zero violations)
- Page Object Model convention documented in `tests/e2e/pages/README.md`
- Auth fixture placeholder (`fixtures/auth.ts`) awaiting Bishop's Entra ID test tenant docs OR local-dev bypass
- axe-core helper in `fixtures/axe.ts` ready for journey #13

**Vitest (Vasquez's territory):**
- Verified `src/TechInventory.Web/package.json` has `pnpm run test` wired to Vitest
- She included @testing-library/svelte, vitest-axe, and jsdom — contract satisfied
- Did not modify her files; noted verification in decision drop

**Playwright matrix decisions:**
- Chose 6-project fan-out (3 browsers × 2 viewports) to match PRD §7.5.3 requirements
- Desktop viewport: 1280×800 (standard laptop)
- Mobile viewport: 375×667 (iPhone SE / Pixel 5 range)
- CI runs all six; local devs can run `--project=chromium-desktop` for speed
- Network isolation pattern: `page.route('**/*', ...)` in `fixtures/network.ts` — blocks non-localhost by hostname check

**axe-core wiring pattern:**
- AxeBuilder from @axe-core/playwright, wrapped in `runAxe(page)` helper
- Returns violations array; test asserts `expect(violations).toEqual([])` — zero tolerance
- Journey #13 will exercise all routes; component-level axe checks are Vasquez's responsibility via vitest-axe

**Blocked on other agents:**
- Integration test's ApiSmokeTests awaits Hicks's `/health` endpoint → skipped with `[Fact(Skip = "...")]`
- Unit test project references await Hicks's `TechInventory.Domain` + `TechInventory.Application`
- Integration test project references await all four Hicks projects (Domain, Application, Infrastructure, Api)
- Playwright auth fixture awaits Bishop's `docs/auth-design.md` and test tenant provisioning OR local-dev bypass flag

**Commands verified:**
- `dotnet build tests/TechInventory.UnitTests -c Release` → success
- `dotnet test tests/TechInventory.UnitTests --no-build` → 1 smoke test passed
- `dotnet build tests/TechInventory.IntegrationTests -c Release` → success
- Playwright config loads without errors (node-based verification due to PowerShell execution policy)
- All 13 journey stubs present in `tests/e2e/journeys/` (01–13)

**Next steps for test authoring (when features land):**
- Uncomment project references in both test .csproj files
- Wire auth fixture (coordinate with Bishop)
- Unskip journey tests one-by-one as Hicks (API) and Vasquez (UI) deliver features
- Enforce 85% coverage floor on Domain + Application via `dotnet test --collect:"XPlat Code Coverage"`

### 2026-05-18: Domain currency contracts + token storage gate

**Domain test patterns:**
- `tests/TechInventory.UnitTests/Domain/` now carries direct spec-contract tests for `Currency`, `Household`, and `Device`
- Currency coverage locks the ISO 4217 behavior: uppercase accepted, lowercase normalized to uppercase, wrong-length rejected, non-allowlist rejected
- Device coverage locks household default inheritance, explicit per-device override, mismatched household/device currency validity, empty-name rejection, and retired-device read-only rules except notes + disposal method

**SessionStorage assertion approach:**
- `tests/e2e/security/token-storage.spec.ts` fulfills a mocked `http://localhost/mock-login` page with `page.route(...)` so browser storage is available under a real origin
- `tests/e2e/security/storage-inspection.ts` uses `page.evaluate(...)` to snapshot `localStorage` and `sessionStorage` keys only
- Assertion contract: no key matching `/token|jwt|access|refresh|id_token|msal/i` may exist in `localStorage`; MSAL keys are allowed in `sessionStorage`

**Skipped vs ready-to-run:**
- Ready now: 13 Domain unit tests, 1 `/health` integration test, and 1 Playwright token-storage spec across the 6-browser matrix
- Skipped awaiting Hicks for this slice: none — Hicks landed `Currency`, `Household`, `Device`, and `/health` while the tests were being authored

### 2026-05-18: T06-T10 reference-entity contract coverage

**Reference entity test shape:**
- For `Category`, `Owner`, `Location`, `Network`, `Tag`, and `DeviceTag`, the unit-test pattern is: constructor guard clauses first, then normalized/derived fields, then explicit state transitions (`Rename`, `Reparent`, `SetRole`, `SetType`, `UpdateDescription`, `UpdateColor`, deactivate/reactivate)
- Composite-link entities like `DeviceTag` should get pair-integrity tests (both IDs required, stored unchanged) plus lifecycle toggle checks when soft-active flags exist
- Reference entities that expose uniqueness helpers (`NormalizedName`, `NormalizedDisplayName`) should have those helpers asserted directly so repository uniqueness rules inherit a locked domain contract

**Repository-interface pattern (when T15 lands):**
- Consumer-side repository tests should use `NSubstitute` around the handler/service under test, assert the exact awaited repository call shape, and verify no extra writes happen outside the command/query contract
- Keep repository-interface tests at the Application layer only; Domain tests should stay on invariants and state transitions because uniqueness and persistence behavior belong above the aggregate

**AuditEvent append-only assertion approach (when T11 lands):**
- Assert immutability from the outside: constructor sets payloads/action/timestamp once, then tests verify there are no public mutator methods or writable public setters for audit fields
- Pair that with later consumer/repository tests that only allow add/create semantics; any update/delete contract should be absent or explicitly throw so append-only is executable, not just documented

### 2026-05-18: T11-T15 reflection contract coverage

**Reflection-based append-only assertion pattern:**
- When the entity surface matters more than implementation details, resolve the type by name and assert the public API contract directly: payload/action/timestamp capture, UTC timestamps, and zero public setters or mutator methods
- For `AuditEvent`, pair constructor guard tests with action-specific payload-transition tests (`Created` requires after, `Updated` requires before+after, `Deleted` requires before) so append-only behavior is locked at construction time

**Repository contract assertion pattern:**
- Application-layer repository interface tests can reflect every method signature and enforce the contract in one pass: async return type, `CancellationToken` present, and no `IQueryable` leakage anywhere in the interface surface
- `IAuditEventRepository` gets an extra append-only guard: require `AppendAsync` and forbid any method name matching `Update|Delete|Remove`

**NSubstitute interface-contract pattern:**
- Use `Substitute.For(new[] { interfaceType }, Array.Empty<object>())` when the interface is discovered by reflection; that keeps the test executable before consumer handlers exist and still proves the seam is mock-friendly
- Keep the reflection helper centralized (`tests/TechInventory.UnitTests/Support/ContractReflectionAssertions.cs`) so future contract suites reuse the same sample-value and signature-inspection logic

**Cross-agent notes (Phase 1 Round 3):**
- Reflection pattern (D-019) now team-wide template for Domain/Application contracts; 19 tests green proving immutability and async signatures.
- Hicks confirmed AppDbContext append-only guards work; no public mutator surface escapes.
- Hudson confirmed integration factory plays nicely with migration discovery; concrete repositories T16 will inherit SQLite isolation automatically.

### 2026-05-18: T16-T19 repository + behavior follow-through

**IntegrationTestFactory usage pattern:**
- Use `IntegrationTestFactory<TMarker>.ConnectionString` as the hermetic SQLite source, then create a fresh `AppDbContext` per test with the real `AuditSaveChangesInterceptor`; keep repository assertions in their own scope and let the factory own DB-file isolation per test class.
- On Windows, SQLite cleanup is noisy under `WebApplicationFactory`; make the factory delete path best-effort with short retries so green repository tests do not fail on lingering file locks during fixture disposal.

**Behavior-test NSubstitute pattern:**
- For `ValidationBehavior`, substitute `IValidator<TRequest>` directly and return `ValidationResult` instances with explicit `ValidationFailure` payloads so aggregation and handler short-circuiting stay executable.
- For `AuditBehavior`, substitute `IAuditEventRepository`, `IUnitOfWork`, and `ICurrentUserService`, then assert append/save side effects from the outside by matching the persisted `AuditEvent` fields rather than poking internals.

**Behavior-ordering integration test approach:**
- Compose the real `ValidationBehavior` around the real `AuditBehavior` with an `IAuditable` request and a failing validator; if validation returns a failure `Result`, both `AppendAsync` and `SaveChangesAsync` must stay untouched.
- This gives a fast integration-style proof of "Validation first, Audit last" without needing controller or handler scaffolding.

**Soft-delete filter assertion pattern:**
- Seed one active row and one soft-deleted row into the same per-class SQLite database, then assert the default list path returns only the active ID while the `includeInactive`/explicit-status path returns both IDs.
- Use IDs, not counts, for the assertion so the test stays stable even as the fixture accumulates unrelated rows across multiple facts.


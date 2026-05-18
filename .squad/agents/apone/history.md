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

**2026-05-18:** Phase 0 parallel scaffolding complete. Security baseline now in effect (`docs/security-baseline.md`). **Currency strategy decision OPEN and blocks T04** — awaiting Brian's decision. `task test` handoff to Hudson complete — Playwright E2E ready to wire into Taskfile.yml.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

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


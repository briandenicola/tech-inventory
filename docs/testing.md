# Testing Guide

> **Authority**: This document operationalizes PRD §7.5 *Local Testing & Validation*
> and constitution §7 *Testing*. When this guide and those documents conflict,
> the PRD and constitution win — and this guide should be updated to match.

This is the developer-facing guide to writing and running tests in this
project. If you've just cloned the repo, start with [Quick Start](#-quick-start).
If you're about to write a test, start with [Choosing the Right Test Type](#-choosing-the-right-test-type).

---

## 📑 Table of Contents

- [Philosophy](#-philosophy)
- [Quick Start](#-quick-start)
- [Choosing the Right Test Type](#-choosing-the-right-test-type)
- [Backend Unit Tests (xUnit)](#-backend-unit-tests-xunit)
- [Backend Integration Tests (Testcontainers)](#-backend-integration-tests-testcontainers)
- [Frontend Unit Tests (Vitest)](#-frontend-unit-tests-vitest)
- [Playwright E2E Tests](#-playwright-e2e-tests)
- [Accessibility Tests](#-accessibility-tests)
- [Performance Tests (Lighthouse CI)](#-performance-tests-lighthouse-ci)
- [Contract Tests](#-contract-tests)
- [Test Data & Fixtures](#-test-data--fixtures)
- [Authentication in Tests](#-authentication-in-tests)
- [The `task test` Contract](#-the-task-test-contract)
- [Debugging Failing Tests](#-debugging-failing-tests)
- [Flaky Test Policy](#-flaky-test-policy)
- [Writing a New Critical Journey](#-writing-a-new-critical-journey)
- [CI Behavior](#-ci-behavior)
- [For AI Agents (Copilot)](#-for-ai-agents-copilot)
- [Common Pitfalls](#-common-pitfalls)

---

## 🧭 Philosophy

We test for **three reasons** — in priority order:

1. **Confidence to ship**: a green build means a deployable build
2. **Confidence to change**: tests are scaffolding for refactoring
3. **Executable specification**: tests document what the system actually does

We do **not** test for:

- Coverage numbers (coverage is a *floor*, not a target)
- "Because the tool can"
- To compensate for missing types or weak validation

### Non-negotiables

- ✅ Every change has tests at the appropriate level
- ✅ Playwright is the only E2E framework
- ✅ Tests run locally with one command (`task test`)
- ✅ CI runs the exact same commands a developer runs
- ✅ Flaky tests are bugs — fix or delete within a working day
- ✅ Tests own their data — no shared fixtures across files
- ❌ No mocked databases in integration tests
- ❌ No mocked API in E2E tests
- ❌ No tests that require internet access

---

## 🚀 Quick Start

### First-time setup

```bash
# Prereqs: Docker, .NET 10 SDK, Node 22+, GNU task
git clone <repo> && cd <repo>

# Install JS deps and Playwright browsers
cd src/TechInventory.Web && npm ci
npx playwright install --with-deps
cd -

# Restore .NET deps
dotnet restore
```

### Run the whole suite (the way CI does)

```bash
task test           # backend unit + integration + frontend unit + E2E
```

Or scope it:

```bash
task test:unit          # backend xUnit only (no Docker)
task test:integration   # backend integration (Testcontainers Postgres)
task test:web           # frontend Vitest only
task test:e2e           # Playwright (requires `task dev:up` or equivalent)
task test:a11y          # @axe-core/playwright accessibility checks
task test:perf          # Lighthouse CI against the running web container
```

The Taskfile is the source of truth — if a script gets renamed, this section
gets stale faster than the table can update. When in doubt: `task --list`.

---

## 🧭 Choosing the Right Test Type

Decide by **what would break first if this code were wrong**:

| Symptom of breakage                                              | Test type                            | Project                                         |
| ---------------------------------------------------------------- | ------------------------------------ | ----------------------------------------------- |
| A pure function returns the wrong value                          | Backend xUnit unit                   | `tests/TechInventory.UnitTests`                 |
| A Svelte component renders wrong markup or fires the wrong event | Frontend Vitest unit                 | `src/TechInventory.Web` (`*.test.ts` adjacent)  |
| An HTTP endpoint returns the wrong status / shape / authz result | Backend integration (Testcontainers) | `tests/TechInventory.IntegrationTests`          |
| OpenAPI spec drifts from the running API                         | Contract                             | `tests/TechInventory.IntegrationTests/Contract` |
| A user-facing journey is broken end-to-end                       | Playwright E2E                       | `tests/e2e`                                     |
| A page violates WCAG 2.1 AA                                      | Accessibility (axe in Playwright)    | `tests/e2e/a11y`                                |
| A page's perf budget regresses                                   | Lighthouse CI                        | `tests/lighthouse`                              |

**Rule of thumb**: pick the cheapest test that would have caught the bug.
A unit test that exercises a regex is worth ten E2E tests that happened to
fail because that regex was wrong.

---

## 🧪 Backend Unit Tests (xUnit)

Live in `tests/TechInventory.UnitTests`. Pure logic only — domain rules,
mappers, validators, value objects. No DB, no HTTP, no `IServiceProvider`.

- Naming: `<ClassUnderTest>Tests.cs` → `Method_Condition_ExpectedResult`.
- Assert with `FluentAssertions`. No `Assert.Equal` in new tests.
- One assertion concept per test; multiple `.Should()` chained is fine when
  they describe the same concept.
- `[Theory]` + `[InlineData]` is preferred over loops.

Run: `dotnet test tests/TechInventory.UnitTests`

---

## 🧪 Backend Integration Tests (Testcontainers)

Live in `tests/TechInventory.IntegrationTests`. These spin up a real
Postgres via Testcontainers and exercise the API through `WebApplicationFactory`.
**No mocked DB. Ever.**

- `IntegrationTestFactory` boots the API with a per-test Postgres container.
- `ControllerTestBase` provides an authenticated `HttpClient` via
  `TestJwtBuilder` (HS256 token matching the dev-bypass JwtBearer scheme).
- Migrations run on container start — schema drift is caught immediately.
- Audit-stamp assertions go through `AuditEventAssertionHelper`.

**Security-critical tests** worth knowing by name:

| File                                                    | What it pins                                                                                                  |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `Auth/Argon2idPasswordHasherTests.cs`                   | Hash format (`$argon2id$v=19$m=…,t=…,p=…$…$…`), verification round-trip, wrong-password failure, malformed-hash failure, parameter sensitivity. Pins F025 v1b / ADR D-140 hashing contract. |
| `Auth/LocalAuthEndpointTests.cs`                        | `POST /api/v1/auth/local/login` happy path + uniform 401 on unknown user / wrong password (no enumeration); `POST /api/v1/auth/local/change-password` happy path + force-rotation middleware (`403 PasswordChangeRequired` when `must_change_password=true` on any other endpoint). |
| `Auth/AuthIntegrationTests.cs`                          | `TechInventoryAuth` PolicyScheme routes by `iss`; Entra and local tokens coexist; both set `ClaimTypes.Role`. |
| `Controllers/DevAuthBypassTests.cs`                     | The dev-only test JWT scheme is wired correctly and is not exposed in `Production`.                           |
| `Controllers/AuditEventsAuthorizationTests.cs`          | Role gates on the audit-events endpoint (Admin-only).                                                         |
| `Controllers/ProblemDetailsTests.cs`                    | API error shape conforms to RFC 7807 across all controllers.                                                  |

Run: `dotnet test tests/TechInventory.IntegrationTests`
(First run downloads the Postgres image — give it a minute.)

---

## 🧪 Frontend Unit Tests (Vitest)

Live alongside the code they test: `src/TechInventory.Web/src/**/*.test.ts`.
Use `@testing-library/svelte` for component tests; plain Vitest for stores,
schemas, and utils.

- Run all: `cd src/TechInventory.Web && pnpm test`
- Run one: `pnpm test -- LocalLoginForm`
- Watch mode: `pnpm test -- --watch`

**Security-critical frontend tests**:

| File                                                           | What it pins                                                                                                 |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `lib/components/LocalLoginForm.test.ts`                        | Local-account toggle on the sign-in page; submits to `/api/v1/auth/local/login`; persists `ti_local_token` + `ti_local_meta` to **sessionStorage** (never localStorage); routes through `auth` store. |
| `routes/auth/change-password/page.test.ts`                     | Force-rotation page submits to `/api/v1/auth/local/change-password`; clears `must_change_password` on success; redirects per `auth` store. |
| `lib/stores/auth.test.ts`                                      | Token storage rule (sessionStorage only), `mustChangePassword` flag handling, sign-out clears both MSAL and local-account state. |
| `lib/tokens.test.ts`                                           | Design-token snapshot (D-137/D-138 visual baseline) — fails loud if Tailwind tokens drift.                   |

Every Svelte component that talks to auth should have a sibling
`*.test.ts` that asserts it uses the `auth` store, not raw `fetch` + token
juggling. Token storage rule violations should be caught here before they
ever reach a Playwright run.

---

## 🎭 Playwright E2E Tests

Live in `tests/e2e/`. Uses **npm**, not pnpm (per `Taskfile.yml`); install
with `npm ci` inside `tests/e2e`. Page objects live in `tests/e2e/pages/`
(see `tests/e2e/pages/README.md` for conventions).

- Spec files: `tests/e2e/specs/*.spec.ts`
- Critical journeys (the ones that gate releases) are tagged `@critical`.
- Auth is wired through a **dev-bypass fixture** (`tests/e2e/fixtures/auth.ts`)
  that mints a test JWT against the dev-only bypass scheme. Tests do not go
  through the real Entra redirect.

Run:

```bash
cd tests/e2e
npx playwright test                       # all
npx playwright test --grep @critical      # release gates only
npx playwright test --ui                  # interactive
```

Headed debug:

```bash
PWDEBUG=1 npx playwright test specs/devices-crud.spec.ts
```

---

## ♿ Accessibility Tests

`@axe-core/playwright` runs against each critical journey. Failure threshold:
**zero serious or critical violations**. `minor` and `moderate` are logged
but do not fail the build (yet — see F0xx backlog).

Run: `task test:a11y`

---

## ⚡ Performance Tests (Lighthouse CI)

Lighthouse runs against the built `web` container. Budgets live in
`tests/lighthouse/budget.json`. CI gates on:

- Performance ≥ 90 (mobile)
- Accessibility ≥ 95
- Best Practices ≥ 95

Run: `task test:perf` (requires `task dev:up` first).

---

## 📜 Contract Tests

`tests/TechInventory.IntegrationTests/Contract/OpenApiDriftTests.cs` reads
`openapi.yaml` from the repo root and asserts every documented operation
exists at runtime, with the documented request/response shape. If you add
or change an endpoint, regenerate `openapi.yaml` (see
`tests/TechInventory.IntegrationTests/Contract/README.md`) — the drift test
will fail loudly otherwise.

---

## 🧬 Test Data & Fixtures

- Each test owns its data. No shared fixtures across files.
- Use **builders** (e.g. `DeviceBuilder`, `OwnerBuilder`) for entity setup,
  not bare object initializers — builders centralize required-field changes.
- Seed data through the repository, not raw EF — that exercises the real
  audit-stamp pipeline.
- `WithCleanDatabase()` extension on the test factory truncates between
  tests when needed; default is per-test Postgres container so isolation
  is free.

---

## 🔐 Authentication in Tests

| Layer        | How auth works                                                                                                                                       |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| Unit (BE)    | Not applicable — pure logic.                                                                                                                         |
| Integration  | `TestJwtBuilder` mints an HS256 token consumed by the dev-only JwtBearer scheme. Set `role`, `oid`, `name` per test. Local-fallback paths use the same builder but with issuer `techinventory-local`. |
| Unit (FE)    | Mock the `auth` store, not `fetch`. If you find yourself stubbing `sessionStorage`, prefer a store-level fake.                                       |
| E2E          | `tests/e2e/fixtures/auth.ts` — `useAuthenticatedPage(role)` fixture mints a dev-bypass JWT and primes sessionStorage before the page loads. No Entra redirect in E2E. |

**Do not** test against a live Entra tenant from CI. The whole point of the
dev-bypass scheme is that auth is exercised in isolation.

---

## 🤝 The `task test` Contract

`task test` is the single command CI runs, and it is the single command a
developer runs before pushing. If `task test` is green locally, it should
be green in CI — modulo Docker version and machine speed.

If you find yourself thinking "I'll just push and let CI catch it" — fix
the thing that's making `task test` slow or noisy. That's a bug in the
test harness, not a process problem.

---

## 🪲 Debugging Failing Tests

- **Integration test fails only in CI**: usually a timezone or culture
  assumption. Run `TZ=UTC task test:integration` locally to reproduce.
- **Playwright fails only headless**: usually a missing `await page.waitFor…`.
  Use `--headed --slowmo=250` to see what the test sees.
- **Vitest fails after Tailwind change**: probably `lib/tokens.test.ts`.
  Snapshot intentionally — see D-137/D-138 visual baseline.
- **Argon2 test slow**: tuning parameters live in
  `Auth/Argon2idPasswordHasherTests.cs`. The default uses smaller params
  for CI-speed; production uses OWASP 2025 baseline.

---

## 🌪️ Flaky Test Policy

A flaky test is a **bug** with the test, the system under test, or the
fixture. The policy:

1. First reproduction: open a `known-issues.md` entry within the day.
2. Two consecutive flakes: either fix it or delete it. No `[Skip]` for
   more than one working day.
3. Quarantine via `@flaky` tag (Playwright) or `Trait("flaky", "true")`
   (xUnit) is acceptable for ≤ 24h — long enough to schedule the fix.

---

## ✍️ Writing a New Critical Journey

Critical journeys are tagged `@critical`. To add one:

1. Identify the user value — the journey must map to a PRD scenario.
2. Build the page objects first (`tests/e2e/pages/<feature>.page.ts`).
3. Write the spec against the page objects, not against raw selectors.
4. Tag `@critical` only after the test has been green on three consecutive
   CI runs.
5. Update `tests/e2e/README.md` if the journey changes the auth or seed
   flow.

---

## 🔧 CI Behavior

- `quality-gate.yml` runs `task test` on every push and PR.
- `security-scan.yml` runs the dependency / secret / SBOM checks.
- Both workflows are owned by Bishop; do not edit them as part of a
  feature PR — open a separate PR if a workflow change is genuinely needed.
- A failed test fails the gate. There is no "rerun and hope" — fix or
  revert.

---

## 🤖 For AI Agents (Copilot)

- **Always** add or update tests in the same PR as the production code.
- **Never** weaken an assertion to make a test pass. If a test "shouldn't
  expect that anymore", explain the regression in the PR description and
  link the decision in `.squad/decisions.md`.
- When touching auth, run `tests/TechInventory.IntegrationTests/Auth/*`
  locally and confirm `LocalLoginForm.test.ts` + `change-password/page.test.ts`
  still pass — those four files together pin F025 v1b.
- Prefer fixing a flaky test to skipping it. If skipping is the only
  option, open a `known-issues.md` entry **in the same PR**.

---

## 🪤 Common Pitfalls

- **Using `localStorage` anywhere in auth code**: forbidden by D-002 /
  security-baseline §1. Token storage is sessionStorage-only, period.
- **Writing E2E tests that mock the API**: defeats the point. Use
  integration tests instead.
- **Asserting on time-of-day**: use `IClock` / `TimeProvider`, not
  `DateTime.UtcNow`.
- **Building giant fixtures shared across files**: don't. Each test owns
  its data.
- **Importing from `src/` into `tests/e2e`**: tests/e2e is a separate
  package with its own `package.json`. Cross-package imports break CI.

---

## 📚 Related Documents

- [`docs/auth-design.md`](auth-design.md) — auth scheme, F025 v1b §6.
- [`docs/security-baseline.md`](security-baseline.md) — token storage rule,
  Serilog, secrets, dependency policy.
- [`docs/threat-model.md`](threat-model.md) — STRIDE per surface; Entra
  outage threat tied to F025 v1b mitigation.
- [`docs/operations.md`](operations.md) — operator runbook (break-glass).
- [`docs/deployment.md`](deployment.md) — Hudson's prod deploy guide.
- [`.squad/decisions.md`](../.squad/decisions.md) — authoritative decision
  ledger (D-001 onward).

---

## Revision History

| Version | Date       | Author | Changes                                                                                                                                            |
| ------- | ---------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0.1     | 2026-05-17 | Bishop | Initial scaffold (TOC + philosophy + Quick Start preamble).                                                                                        |
| 1.0     | 2026-05-19 | Scribe | Filled in every promised TOC section. Added F025 v1b / D-140 references to the security-critical test files: `Argon2idPasswordHasherTests`, `LocalAuthEndpointTests`, `LocalLoginForm.test.ts`, `auth/change-password/page.test.ts`. |
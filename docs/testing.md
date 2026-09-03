# Testing Guide

> **Authority**: This document operationalizes PRD §7.5 *Local Testing & Validation*
> and constitution §7 *Testing*. When this guide and those documents conflict,
> the PRD and constitution win — and this guide should be updated to match.
>
> **Browser automation:** retired by accepted ADR 0002. The constitution
> §6.5.14/§7 and PRD §7.5.2–§7.5.5 were amended with that decision. Browser-only
> PWA behavior is recorded through the owned manual checklist, never reported
> as automated coverage.

This is the developer-facing guide to writing and running tests in this
project. If you've just cloned the repo, start with [Quick Start](#-quick-start).
If you're about to write a test, start with [Choosing the Right Test Type](#-choosing-the-right-test-type).

---

## 📑 Table of Contents

- [Philosophy](#-philosophy)
- [Quick Start](#-quick-start)
- [Choosing the Right Test Type](#-choosing-the-right-test-type)
- [Backend Unit Tests (xUnit)](#-backend-unit-tests-xunit)
- [Backend Integration Tests (SQLite)](#-backend-integration-tests-sqlite)
- [Frontend Unit Tests (Vitest)](#-frontend-unit-tests-vitest)
- [Contract Tests](#-contract-tests)
- [Test Data & Fixtures](#-test-data--fixtures)
- [Authentication in Tests](#-authentication-in-tests)
- [The `task verify` Contract](#-the-task-verify-contract)
- [Debugging Failing Tests](#-debugging-failing-tests)
- [Flaky Test Policy](#-flaky-test-policy)
- [Writing a New Critical Journey](#-writing-a-new-critical-journey)
- [Accessibility Tests](#-accessibility-tests)
- [Performance Review](#-performance-review)
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
- ✅ No automated browser E2E — that harness is retired (`specs/004-agentic-development-foundation/brief.md` §2.1); HTTP integration, Vitest component tests, and the manual PWA checklist are the destination layers (`specs/004-agentic-development-foundation/coverage-migration.md` §6)
- ✅ Required verification runs locally with one command (`task verify`)
- ✅ CI runs the exact same commands a developer runs
- ✅ Flaky tests are bugs — fix or delete within a working day
- ✅ Tests own their data — no shared fixtures across files
- ❌ No mocked databases in integration tests
- ❌ No tests that require internet access

---

## 🚀 Quick Start

### First-time setup

```bash
# Prereqs: .NET 10 SDK, Node 22+, GNU task (Docker only if you're using `task up`)
git clone <repo> && cd <repo>

# Restore pinned tools and all project dependencies
task restore
```

### Run the whole suite (the way CI does)

```bash
task verify         # the same complete pipeline CI invokes
```

Or scope it:

```bash
task test:unit          # backend xUnit only (no Docker)
task test:integration   # backend integration (in-process SQLite)
task check:stale-refs   # fails if the retired browser-E2E harness has returned
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
| An HTTP endpoint returns the wrong status / shape / authz result | Backend integration (SQLite) | `tests/TechInventory.IntegrationTests`          |
| OpenAPI spec drifts from the running API                         | Contract                             | `tests/TechInventory.IntegrationTests/Contract` |
| A user-facing journey is broken end-to-end                       | HTTP integration + Vitest component tests (browser E2E retired) | `tests/TechInventory.IntegrationTests`, `src/TechInventory.Web` — see `specs/004-agentic-development-foundation/coverage-migration.md` §6, §9 |
| A page violates WCAG 2.1 AA                                      | Accessibility (`vitest-axe` in component tests)    | `src/TechInventory.Web` (route-level axe harnesses) |
| Only a real installed app / service worker / second rendering engine can prove it | Manual PWA validation checklist | `docs/testing/manual-pwa-validation.md` |
| A page's performance budget regresses | Manual release profiling (P-01) | `docs/testing/manual-pwa-validation.md` |

**Rule of thumb**: pick the cheapest test that would have caught the bug.
A unit test that exercises a regex is worth ten integration tests that
happened to fail because that regex was wrong.

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

## 🧪 Backend Integration Tests (SQLite)

Live in `tests/TechInventory.IntegrationTests`. These exercise the API through
`WebApplicationFactory` against a real, per-test SQLite database. **No mocked
DB and no Docker.**

- `IntegrationTestFactory` boots the API with a unique SQLite database file.
- `ControllerTestBase` provides an authenticated `HttpClient` via
  `TestJwtBuilder` (HS256 token matching the local-issuer JwtBearer scheme).
- Migrations run at factory startup — schema drift is caught immediately.
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
juggling. Token storage rule violations are caught here — this is the only
automated layer that exercises them; browser E2E is retired (see
`specs/004-agentic-development-foundation/coverage-migration.md` §6).

---

## ♿ Accessibility Tests

Accessibility assertions live alongside the Vitest component tests that
render each route (`vitest-axe` / equivalent axe-core bindings), not in a
separate browser-driven suite — the previous axe-in-browser layer
was retired along with the rest of that harness
(`specs/004-agentic-development-foundation/coverage-migration.md` §6, C-18).
Failure threshold: **zero serious or critical violations** in any covered
route's component test. Any route not yet covered at the component level is
tracked as an explicit gap (see `coverage-migration.md` §8, G-09/G-10) and is
exercised instead by the manual PWA validation checklist
(`docs/testing/manual-pwa-validation.md`).

Run: `cd src/TechInventory.Web && pnpm test` (accessibility assertions run
as part of the normal Vitest suite, not a separate command).

---

## ⚡ Performance Review

ADR 0002 excludes browser-based performance automation. The budgets in
constitution §6.5.9 remain required and are reviewed before each release using
P-01 in `docs/testing/manual-pwa-validation.md`. Record the measurements and
file an issue for each exceeded budget. Never infer P-01 from a green CI run.

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
- Each integration-test class owns a unique SQLite database file; reset helpers
  clear state between cases when needed.

---

## 🔐 Authentication in Tests

| Layer        | How auth works                                                                                                                                       |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| Unit (BE)    | Not applicable — pure logic.                                                                                                                         |
| Integration  | `TestJwtBuilder` mints an HS256 token consumed by the dev-only JwtBearer scheme. Set `role`, `oid`, `name` per test. Local-fallback paths use the same builder but with issuer `techinventory-local`. |
| Unit (FE)    | Mock the `auth` store, not `fetch`. If you find yourself stubbing `sessionStorage`, prefer a store-level fake.                                       |

**Do not** test against a live Entra tenant from CI. The whole point of the
F025 local-account path is that auth is exercised in isolation against a
seeded admin row. There is no browser-driven E2E auth fixture any more —
that harness is retired; the equivalent coverage lives in the integration
tests above plus the manual PWA validation checklist's sign-in steps
(`docs/testing/manual-pwa-validation.md`).

---

## 🤝 The `task verify` Contract

`task verify` is the single command CI runs, and it is the single command a
developer runs before opening a PR. It covers format, build, type-check, lint,
unit/component tests, real-HTTP integration tests, contract and migration drift,
test-collection floors, dependency vulnerabilities, and security scanning.
It requires neither Docker nor a browser download.

If you find yourself thinking "I'll just push and let CI catch it" — fix
the thing that's making `task verify` slow or noisy. That's a bug in the
test harness, not a process problem.

---

## 🪲 Debugging Failing Tests

- **Integration test fails only in CI**: usually a timezone or culture
  assumption. Run `TZ=UTC task test:integration` locally to reproduce.
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
3. Quarantine via `Trait("flaky", "true")` (xUnit) or an equivalent skip
   annotation (Vitest) is acceptable for ≤ 24h — long enough to schedule
   the fix.

---

## ✍️ Writing a New Critical Journey

Critical journeys no longer get a dedicated browser-automation spec — that
harness is retired. To add coverage for a new user-facing journey:

1. Identify the user value — the journey must map to a PRD scenario.
2. Cover the request/response contract with a backend integration test
   (`tests/TechInventory.IntegrationTests`) exercising the real HTTP
   pipeline end to end for that journey.
3. Cover the UI behaviour with Vitest component tests
   (`src/TechInventory.Web`), including an axe-core assertion for the
   route/component involved (see [Accessibility Tests](#-accessibility-tests)).
4. If the journey depends on a real installed PWA, a second rendering
   engine, or genuine offline/service-worker behaviour that neither layer
   above can prove, add a checklist item to
   `docs/testing/manual-pwa-validation.md` instead of trying to automate it.
5. See `specs/004-agentic-development-foundation/coverage-migration.md` §6
   for the full journey-by-journey replacement mapping used when this
   harness was retired.

---

## 🔧 CI Behavior

- `quality-gate.yml` runs `task verify` on every push and PR, plus CodeQL,
  gitleaks, Trivy configuration scanning, and main-only SBOM generation.
- `ci.yml` is manual-dispatch diagnostics and also invokes `task verify`.
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
- **Writing tests that mock the API**: defeats the point of an integration
  test. Use `WebApplicationFactory` against the real pipeline instead.
- **Asserting on time-of-day**: use `IClock` / `TimeProvider`, not
  `DateTime.UtcNow`.
- **Building giant fixtures shared across files**: don't. Each test owns
  its data.

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

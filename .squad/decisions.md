# Squad Decisions

## Active Decisions

### D-001: Entra Tenant Type — Workforce (Not External ID) — **DECIDED**

**Decided by:** Brian (via Copilot, 2026-05-18T14:09:24Z)  
**Related:** PRD §14, `docs/auth-design.md` (v2.0), `.squad/decisions/inbox/copilot-directive-2026-05-18T140924Z-entra-tenant.md`

**Decision:** Use the existing household **Workforce Entra ID tenant** (same tenant used for Office, Teams, etc.). Tech Inventory registers as one more app in that mix.

**Rationale:** Users already provisioned in Workforce tenant; no External ID sign-up flow needed. Simpler operations.

**Implications:**
- App registration in household's existing Workforce tenant
- Roles (Admin / Member / Viewer) mapped via App Roles or Security Groups
- `docs/auth-design.md` revised to reflect Workforce choice (v2.0 complete)
- Token issuer/audience: `login.microsoftonline.com/{tenant-id}`

**Next:** Bishop revises auth-design.md (Phase 0.5 ✅ complete). Hicks wires AuthN in Phase 2.

---

### D-002: Token Storage — Memory/SessionStorage Only (Never localStorage)

**Proposed by:** Bishop (Security & Auth Specialist)  
**Date:** 2026-05-18  
**Status:** Approved for implementation  
**Related:** ASVS V2.10.2, `docs/security-baseline.md` §1

JWT tokens stored in **memory or sessionStorage only. Never localStorage.** Aligns with OWASP ASVS L2 and minimizes XSS token exfiltration window.

**Implementation:** MSAL.js configured with `BrowserCacheLocation.SessionStorage`. ESLint rule blocks `localStorage.setItem()`. Playwright test verifies tokens not in localStorage after sign-in.

**Enforcement:** Pre-commit hook, code review checklist on every PR.

---

### D-003: SQLite Volume Strategy — Named Volume, No DB Container

**Author:** Hudson (DevOps)  
**Date:** 2026-05-18  
**Status:** Decided  
**Related:** PRD §7.5.5, Constitution §2, `docker-compose.yml`

**No dedicated database container.** SQLite (file-based, embedded in API process) mounted as named volume `techinv-data` at `/data`. Connection string: `Data Source=/data/techinv.db`.

**Rationale:** SQLite doesn't benefit from a separate container; named volumes more portable than host paths. Matches prod deployment pattern (Brian's home server).

**Consequences:** Backup strategy must target named volume. Multi-replica scaling impossible (not a concern for single-household app).

---

### D-004: Web Runtime Port 3000 (SvelteKit adapter-node)

**Author:** Hudson (DevOps)  
**Date:** 2026-05-18  
**Status:** Decided  
**Related:** `src/TechInventory.Web/Dockerfile`, `docker-compose.yml`

SvelteKit Web runs on **port 3000 using adapter-node** for both dev and prod. Production-like image in dev catches packaging issues early; simpler Dockerfile (one runtime path).

**Trade-off:** Rebuild required for UI changes during integration testing (acceptable). Developers can run `pnpm run dev` locally (outside Docker) for hot reload.

---

### D-005: Playwright Browser & Viewport Matrix — 3 browsers × 2 viewports

**Agent:** Apone (Tester/QA)  
**Date:** 2026-05-18  
**Status:** Approved  
**Related:** PRD §7.5.3, `tests/e2e/playwright.config.ts`

**6 Playwright projects:** Chromium/WebKit/Firefox × desktop (1280×800) / mobile (375×667).

**Rationale:** Satisfies PRD requirement for "per critical flow" browser and viewport coverage. Desktop (1280×800) for typical laptop; mobile (375×667) for iPhone SE / Pixel 5 range.

**Impact:** ~6× CI test duration (acceptable). Developers can run single project locally (`npx playwright test --project=chromium-desktop`) for speed.

**Alternatives rejected:** Single viewport per browser (insufficient); tablet viewport (v2 deferral).

---

### D-006: Test Project Scaffolding Complete

**Agent:** Apone (Tester/QA)  
**Date:** 2026-05-18  
**Status:** Approved — dependencies noted  
**Related:** `tests/`, PRD §7.5

Backend unit tests, integration tests, and Playwright E2E all scaffolded. 13 critical user journeys stubbed.

**Blockers:**
- Unit/Integration tests await Hicks's Domain + Api projects (project refs commented out)
- Integration smoke test (`ApiSmokeTests.HealthEndpoint_Returns200Ok`) skipped; awaits `/health` wiring
- Playwright auth fixture awaits Bishop's auth design or documented local-dev bypass

**Handoff notes:** Hudson wires `test:e2e` task in Taskfile.yml. All test infrastructure production-ready.

---

### D-007: Tailwind CSS v4 (Beta)

**Agent:** Vasquez (Frontend Developer)  
**Date:** 2026-05-18  
**Status:** Implemented  
**Related:** `src/TechInventory.Web/package.json`, `src/lib/tokens.css`

SvelteKit uses **Tailwind CSS v4.3.0** with `@tailwindcss/vite` plugin.

**Rationale:** Simpler setup (no postcss.config.js), Vite-native, stable for new projects. Future-proof (v4 is the forward path).

**Trade-off:** Beta status (no known blockers). Migrating from v3 → v4 later would add friction, so v4 chosen upfront.

**Validation:** Builds and lints cleanly. Design tokens work as expected.

---

### D-008: Currency Strategy — **DECIDED**

**Decided by:** Brian (via Copilot, 2026-05-18T14:09:24Z)  
**Related:** PRD §14, `specs/001-core-api/spec.md`, `.squad/decisions/inbox/copilot-directive-2026-05-18T140924Z-currency.md`

**Decision:** Per-device currency. `Device` carries `Currency` (ISO 4217 code). `Household` has `DefaultCurrency`; new devices inherit unless overridden at creation.

**Rationale:** Single-household app but imported data may have mixed currencies (gifts, travel). Per-device avoids data loss; minimal schema cost. No exchange-rate conversion needed.

**Implications:**
- `Currency` value object (ISO 4217, 3-char, validated)
- `Household.DefaultCurrency` inherited by new devices
- Per-device override supported at creation/edit time
- DTOs + FluentValidation validate against ISO 4217 allowlist
- T04 (Device entity) unblocked

**Status:** Implemented. Phase 1 T01–T05 (Hicks) complete.

---

### D-009: OIDC Scopes & App Role Mapping

**By:** Bishop (Security & Auth Specialist)  
**Date:** 2026-05-18  
**Status:** Decided & implemented  
**Related:** `docs/auth-design.md` (v2.0), `.squad/decisions/inbox/bishop-entra-scopes-and-roles.md`

**OIDC Scopes:** `openid profile email offline_access` (minimal surface, no Graph API delegation).

**App Roles → Local Roles:**
- `admin` (Entra) → `Admin` (Tech Inventory): full read/write/delete, user mgmt, audit query, system config
- `member` → `Member`: read all devices, write own-owned devices, update profile
- `viewer` → `Viewer`: read-only

**Mapping Logic:**
1. Extract JWT `app_roles` claim (list)
2. Empty list → reject 401
3. First element maps to Tech Inventory role; unknown values → reject 401
4. First-login: create `Owner` record with mapped role, `EntraObjectId`, `Email`

**Token Validation (per ASVS V2.10.2):**
- Signature via Entra JWKS
- Issuer: `https://login.microsoftonline.com/{tenant-id}/v2.0`
- Audience: app client ID
- Expiry + not-before checks
- Required claims: `oid`, `email`, `app_roles`

**Phase 2 open questions:** Multi-role support, guest user fallback, local role override conflict resolution.

---

### D-010: Token Storage Enforcement — Four-Gate Model

**By:** Brian (via Copilot, 2026-05-18T14:09:24Z)  
**Status:** Decided & all gates deployed ✅  
**Related:** `.squad/decisions/inbox/copilot-directive-2026-05-18T140924Z-token-storage.md`

**Decision:** Forbid `localStorage` for auth tokens via four gates:

1. **ESLint Rule** (Vasquez): Custom path-aware rule in `src/TechInventory.Web/eslint.config.js` bans `localStorage.setItem/getItem/removeItem` for token-like keys
2. **Pre-commit Hook** (Hudson): `.githooks/pre-commit` scans for `localStorage` + token patterns; gitleaks with `.gitleaks.toml`
3. **Playwright Assertion** (Apone): `tests/e2e/security/token-storage.spec.ts` verifies tokens not in `localStorage` post-login (6 browser projects)
4. **Code Review Checklist** (Bishop): security review template includes token-storage audit

**MSAL.js:** Cache location pinned to `BrowserCacheLocation.SessionStorage` (or in-memory).

**Implementation:** All gates verified; pre-commit hook blocks localStorage+token attempts; pnpm lint fails on violations.

---

### D-011: Path-Aware ESLint Custom Rules

**By:** Vasquez (Frontend Developer)  
**Date:** 2026-05-18  
**Status:** Implemented & approved  
**Related:** `.squad/decisions/inbox/vasquez-path-aware-eslint-security-gates.md`

**Pattern:** Adopt inline custom ESLint rules in flat config for security policies that depend on both:
1. The API shape being called
2. The file/module boundary where that call appears

**Rationale:** Token-storage policy needed two dimensions at once (ban token keys + stricter enforcement in auth modules). Inline rules stay in-repo, versioned with app, easy to extend.

**Reuse guidance:** Use for future frontend security gates:
- Forbidding risky browser APIs in auth/network modules
- Blocking hard-coded secrets or bearer-token headers in client code
- Enforcing stricter rules in service-worker or PWA cache code

**Shape:** Normalize file paths, inspect AST call sites, match sensitive key names with explicit regex, emit clear remediation messages.

---

### D-012: Repo-Managed Git Hooks

**By:** Hudson (DevOps)  
**Date:** 2026-05-18  
**Status:** Implemented & verified  
**Related:** `.squad/decisions/inbox/hudson-repo-managed-security-hooks.md`

**Decision:** Adopt repo-managed Git hooks at `.githooks/pre-commit` backed by `task hooks:install`, pinned gitleaks binary at `.tools/gitleaks/`, and shared Node scanner (`scripts/check-security.mjs`).

**Why:**
- Cross-platform without requiring Python, Go, or repo-root package manager
- Local hook and PR CI run same token-storage regex and gitleaks config
- Single-command setup on fresh clone: `task hooks:install`

**Enforcement Pattern:**
- Ban auth token persistence: `/localStorage\s*\.(set|get|remove)Item\s*\(\s*['"\`][^'"\`]*?(token|jwt|access|refresh|id_token|msal)/i`
- Run gitleaks with `.gitleaks.toml`
- CI mirrors hook by scanning PR/push diff with `node scripts/check-security.mjs --diff-range <range>`

**Verification:** Hook verified; attempted storage of auth token via browser API correctly rejected.

---

### D-013: Domain Entity Base & Currency Validation

**By:** Hicks (Backend / Domain)  
**Date:** 2026-05-18  
**Status:** Implemented & tested  
**Related:** `.squad/decisions/inbox/hicks-domain-primitives-currency-validation.md`

**Decision:** Model Domain aggregates on `AggregateRoot : Entity`, with `Entity` owning shared identifier and audit metadata hooks. Value objects as records on minimal `ValueObject` base. Validate `Currency` in Domain against ISO 4217 allowlist.

**Why:**
- Infrastructure can stamp audit metadata later via `SetAuditMetadata()` / `Touch()` without EF Core dependencies in Domain
- `Device.Create(...)` inherits `Household.DefaultCurrency` while allowing per-device override
- Deterministic Domain tests on currency normalization, retirement guards without waiting on FluentValidation or MediatR

**Implications:**
- `Entity` base: `Id` (ULID), audit properties, `SetAuditMetadata()`, `Touch()` methods
- `Currency` value object: ISO 4217 validation, case-insensitive normalization
- `Household` aggregate: `Name`, `DefaultCurrency`, audit hooks
- `Device` aggregate: `HouseholdId`, `Name`, `Brand`, `Currency`, `Status` (Active/Retired/Archived), `PurchaseDate`, `RetiredDate`
- Retired-device invariant: only `RetiredDate` editable once `Status == Retired`
- `Brand` entity: lightweight domain object for categorization

**Test Coverage:** 13 Domain unit tests green (Currency VO, Household default, Device inheritance/override, empty-name rejection, retired-device edit restrictions).

---

### D-014: Currency Contract Tests as Executable Spec

**By:** Apone (QA)  
**Date:** 2026-05-18  
**Status:** Implemented (13 tests green)  
**Related:** `.squad/decisions/inbox/apone-currency-contract-tests.md`

**Pattern:** Domain currency tests assert spec directly against public API:
- `Currency.From(...)`, `new Household(...)`, `Device.Create(...)` express business contract plainly
- Assert ISO 4217 happy-path (`USD`), lowercase normalization, wrong-length rejection, non-allowlist rejection
- Assert household-default inheritance, per-device override, mismatched currency validity, retired-device write restrictions

**Why:** Keeps tests readable as executable spec instead of coupled to implementation details.

**Coverage:** 13 tests in `tests/TechInventory.UnitTests/Domain/` (Currency, Household, Device):
- 4 Currency tests (valid/invalid codes, normalization, allowlist)
- 3 Household tests (default inheritance, override, audit touch)
- 6 Device tests (currency inheritance, override, retired guards, role mismatch logging, status transitions)

---

### D-015: Playwright Token-Storage Inspection Pattern

**By:** Apone (QA)  
**Date:** 2026-05-18  
**Status:** Implemented (6 browser projects green)  
**Related:** `.squad/decisions/inbox/apone-token-storage-inspection.md`

**Pattern:** Playwright token-storage checks snapshot both `localStorage` and `sessionStorage` via `page.evaluate`, assert token-like keys never in `localStorage`, MSAL keys allowed in `sessionStorage`.

**Shape:**
- Centralize regex for token-like keys
- Read storage keys only; values irrelevant for policy gate
- Use mocked login flow until real auth lands; swap page setup while keeping assertions

**Why:** Gives Bishop's storage decision executable guard now instead of waiting on full auth wiring.

**Implementation:** `tests/e2e/security/token-storage.spec.ts` runs across 6 Playwright projects (Chromium, WebKit, Firefox × desktop + mobile). All green.

---

### D-016: Reference Entity Contract Test Pattern

**By:** Apone (QA)  
**Date:** 2026-05-18  
**Status:** Implemented (80 xUnit cases green, 97.6% Domain coverage)  
**Related:** Phase 1 T06–T10 (Category, Owner, Location, Network, Tag, DeviceTag)

**Pattern:** One contract-test file per reference aggregate, organized in consistent shape:

1. Constructor invariants / guard clauses
2. Normalized or derived fields (for uniqueness/display helpers)
3. Explicit state transitions and mutators (`Rename`, `Reparent`, `SetRole`, `SetType`, `UpdateDescription`, `UpdateColor`)
4. Soft-active lifecycle toggles (`Deactivate` / `Reactivate`)

**Why:** Keeps Domain suite spec-driven and predictable. Makes gaps obvious: if Hicks adds a new aggregate mutator, it needs an explicit state-transition test, not just constructor coverage.

**Implementation:**
- `tests/TechInventory.UnitTests/Domain/CategoryTests.cs` (root/child depth validation)
- `tests/TechInventory.UnitTests/Domain/OwnerTests.cs` (role, Entra identity hooks)
- `tests/TechInventory.UnitTests/Domain/LocationTests.cs` (normalized naming, soft-delete)
- `tests/TechInventory.UnitTests/Domain/NetworkTests.cs` (normalized naming, soft-delete)
- `tests/TechInventory.UnitTests/Domain/TagTests.cs` (normalized naming, soft-delete)
- `tests/TechInventory.UnitTests/Domain/DeviceTagTests.cs` (composite key, soft-active, reactivation)

**Follow-ons:**
- When repository interfaces land (T15), mirror this with Application-layer NSubstitute tests from consumer side
- When `AuditEvent` lands (T11), add append-only contract file: assert immutable public surface + create-only repository semantics

---

### D-017: AuditEvent Append-Only Enforcement Strategy

**By:** Hicks (Backend Developer)  
**Date:** 2026-05-18  
**Status:** Decided & implemented (T11 complete)  
**Related:** `src/TechInventory.Domain/Entities/AuditEvent.cs`, `src/TechInventory.Infrastructure/Persistence/AppDbContext.cs`

**Decision:** Keep `AuditEvent` / `ImportBatch` outside the mutable `Entity` base contract. Repository abstractions live in `TechInventory.Application/Abstractions/Repositories/`, enforcing append-only behavior at both the interface seam and `AppDbContext` save pipeline.

**Shape:**
- `AuditEvent`: immutable public properties (`Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, `BeforePayload`, `AfterPayload`). No public setters or mutators. Private EF Core constructor only.
- `ImportBatch`: immutable public fields, derived `ProcessedCount` / `HasErrors` helpers, private EF constructor.
- `IAuditEventRepository`: `AppendAsync` + query-only methods; no update/delete surface.
- `AppDbContext` save guard: reject modified or deleted `AuditEvent` rows as second-line safeguard.

**Implications:**
- AuditEvent append-only contract locked at construction. No public API for state changes.
- Repository contracts in Application layer formalize the seam above Domain where `Result<T>`, paging DTOs, and query criteria live without leaking EF Core.
- Concrete repositories deferred to Phase 2 (T16) after handler patterns settle.

**Open questions for future rounds:**
1. Dedicated immutable-domain-base type for append-only records, or keep stand-alone?
2. Narrow `TechInventory.Application/Abstractions/Repositories/` namespace split before handlers land?
3. All immutable persistence records get DbContext guards, or only security-sensitive append-only data?

---

### D-018: SQLite Integration Isolation + Hermetic E2E Contract

**By:** Hudson (DevOps / Platform)  
**Date:** 2026-05-18  
**Status:** Decided & implemented  
**Related:** `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs`, `Taskfile.yml`, `scripts/verify.sh`

**Decision:** Backend integration tests use **in-process SQLite with one fresh file per test class**, parameterized by test class marker type. `task test:e2e` owns the hermetic Playwright lifecycle: `docker compose up -d --build` → wait for `/health/ready` → run Playwright → `docker compose down -v`.

**Rationale:**
- SQLite in-process matches production behavior (embedded in API) better than Testcontainers.
- Per-test-class database keeps integration suites isolated without per-test-method startup cost.
- One-shot E2E task gives CI and local laptops identical contract, including mandatory teardown.

**Consequences:**
- Future integration test classes follow pattern: `SomeFeatureTests : IClassFixture<IntegrationTestFactory<SomeFeatureTests>>`
- SQLite file maps cleanly to owning test class; no cross-test contamination.
- Hicks registers `AppDbContext` against `ConnectionStrings:Default`; TODO hook in factory `ConfigureServices(...)` is override point if needed.
- Apone expands HTTP coverage on same factory once migrations land; `task test:integration` is entry point.

**Trade-off noted:** Per-test-class isolation adds minimal overhead but guarantees data cleanliness. Testcontainers avoided for simplicity.

---

### D-019: Reflection-Based Contract Test Pattern

**By:** Apone (QA)  
**Date:** 2026-05-18  
**Status:** Decided & implemented  
**Related:** `tests/TechInventory.UnitTests/Support/ContractReflectionAssertions.cs`, `tests/TechInventory.UnitTests/Domain/AuditEventTests.cs`, `tests/TechInventory.UnitTests/Application/Abstractions/RepositoryInterfaceContractTests.cs`

**Pattern:** Contract-first testing for Domain entities and Application abstractions using reflection:
1. Resolve target type by full name from owning assembly
2. Use shared deterministic sample values for reproducible construction
3. Assert at the seam: immutable public surface, async repository signatures, `CancellationToken` on every method, no `IQueryable` leakage
4. Add NSubstitute mockability checks so future handler tests inherit the same seam contract

**Why adopt team-wide:**
- QA locks contracts early without depending on implementation shape
- Tests stay stable across refactors (enforce public API, not private wiring)
- Future Domain entities, repositories, and handler abstractions reuse helper instead of re-learning reflection

**Implementation:**
- `tests/TechInventory.UnitTests/Support/ContractReflectionAssertions.cs` — Centralized reflection + type resolution + method introspection + NSubstitute setup
- `tests/TechInventory.UnitTests/Support/DeterministicSampleValues.cs` — Canonical test data (ULIDs, currency codes, dates, role enums)
- 8 AuditEvent append-only tests + 11 repository interface contract tests (19 total)

**Adoption guidelines:**
- Apply to immutable Domain entities (AuditEvent, ImportBatch, future audit/import records)
- Apply to all Application-layer abstractions (repository interfaces, handler request/response contracts)
- Pair with NSubstitute mockability gate to catch breaking interface changes early
- Keep tests readable by using descriptive assertion names (`AssertAsyncMethod`, `AssertNoCancellationTokenOmitted`)

**Team reuse note:** Append result-type and cancellation-token patterns to this decision when future phases land validation commands and query handlers.

---

### D-020: Audit Context & Repository Balance Strategy

**By:** Hicks (Backend / Domain)  
**Date:** 2026-05-18  
**Status:** Decided & implemented (T16–T19)  
**Related:** `src/TechInventory.Infrastructure/Persistence/Repositories/`, `src/TechInventory.Application/Behaviors/`, `src/TechInventory.Application/Auditing/IAuditContext.cs`, `specs/001-core-api/plan.md` §2.3, §3.1, §3.2

**Decision — Three-part strategy:**

1. **Generic repository base + specific read methods**
   - Keep shared add/get/update plumbing in `Repository<TEntity, TKey>` base.
   - Keep list/filter/paging/name-lookup logic in concrete repositories (`BrandRepository`, `DeviceRepository`, etc.).
   - Exact-ID lookups remain unit-of-work aware and can return inactive rows; list/default read paths filter inactive/soft-deleted rows unless the interface explicitly asks for `includeInactive`.

2. **Validation failure shape**
   - `ValidationBehavior<TRequest, TResponse>` returns `Result.Failure(new Error("Validation", "One or more validation failures occurred.", validationErrors))`.
   - `validationErrors` is a property-name → string[] dictionary on `Error`, ready for later ProblemDetails mapping without throwing exceptions.

3. **Audit payload capture strategy**
   - `IAuditable` stays a marker interface only (at `src/TechInventory.Application/Auditing/IAuditable.cs`).
   - Handlers populate scoped `IAuditContext` with entity type/id/action and optional BEFORE payload.
   - `AuditBehavior` serializes `BeforePayload` from that context and uses the request object as the default AFTER payload. Create operations therefore store JSON `null` for BEFORE and the command payload for AFTER unless a handler overrides AFTER explicitly.
   - Pipeline order: `ValidationBehavior` first, `AuditBehavior` last, so invalid requests short-circuit cleanly and never append audit rows.

**Rationale:**
- The generic base removes repetitive EF Core add/get/update plumbing without forcing every repository into a lowest-common-denominator query surface.
- `IAuditContext` keeps BEFORE-state capture with the handler that already knows the aggregate/query flow, avoiding a second database read inside the pipeline.
- The error dictionary gives the API layer a direct bridge to RFC 7807 validation details later, while staying inside the existing `Result` model today.

**Implementation:**
- 10 concrete repositories in `src/TechInventory.Infrastructure/Persistence/Repositories/` (`BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, `ImportBatchRepository`)
- `AuditSaveChangesInterceptor` wired through `AppDbContext.OnConfiguring()` stamps UTC `CreatedAt`/`ModifiedAt` and `CreatedBy`/`ModifiedBy`
- `ValidationBehavior` + `AuditBehavior` pipeline behaviors in `src/TechInventory.Application/Behaviors/` with test coverage
- All changes verified: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅

**Consequences:**
- Handlers must manage their own BEFORE snapshots if audit trails require transaction semantics beyond AFTER only
- Reference-entity list methods default to `includeInactive: false` but keep exact-ID paths open for reactivation flows
- Validation failures integrate cleanly with ProblemDetails; exceptions no longer propagate from validators

---

### D-021: MediatR Behavior Pipeline Order Verification Pattern

**By:** Apone (QA)  
**Date:** 2026-05-18  
**Status:** Decided & implemented  
**Related:** `tests/TechInventory.UnitTests/Application/Behaviors/`, `specs/001-core-api/plan.md` §2.3

**Pattern:** Verify MediatR behavior ordering by composing the real `ValidationBehavior<TRequest, TResponse>` around the real `AuditBehavior<TRequest, TResponse>` in a unit-speed integration test.

**Test shape:**
- Use an `IAuditable` request type.
- Inject a failing `IValidator<TRequest>` via NSubstitute.
- Inject `IAuditEventRepository`, `IUnitOfWork`, and `ICurrentUserService` substitutes into `AuditBehavior`.
- Execute `ValidationBehavior.Handle(...)` with the `AuditBehavior.Handle(...)` delegate as `next`.
- Assert the response is `Result.Failure("Validation")` and that `AppendAsync` / `SaveChangesAsync` were never called.

**Why keep it:**
- Proves the contract that matters: validation must short-circuit before audit side effects fire.
- Cheaper and less brittle than waiting for controller/handler scaffolding.
- Reusable pattern for future pipeline-order checks when more behaviors land.

**Verification:**
- Tests in `tests/TechInventory.UnitTests/Application/Behaviors/` confirm validation-before-audit ordering.
- Repo-root `dotnet test -c Release` now exercises both backend test projects (151 tests green).
- Coverage tracked alongside D-020: Domain 81.40%, Application 40.53%, Infrastructure 88.98%.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

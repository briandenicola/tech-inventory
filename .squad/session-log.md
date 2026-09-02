# Session Log — Tech Inventory Squad

## F045 PWA Device Navigation — Design → QC Audit → Independent Revision → Approval & Consolidation (2026-09-02)

**Headline:** F045 (standalone-PWA shell: bottom nav pill, anchored menu, grouped device list) approved after design review → QC rejection (B1–B6) → independent revision (Hicks) → full decision consolidation (Scribe).

### Session Summary

| Metric | Value |
|--------|-------|
| **Design Phase** | Ripley (D-175–D-179) |
| **QC Audit Verdict** | ❌ REJECTED (B1–B6) |
| **Implementation** | Vasquez (parallel with Apone E2E) |
| **E2E Coverage** | Apone: 126 test cases collected (`15-pwa-shell.spec.ts`) |
| **Visual Spec** | Drake (no new tokens, component-scoped contrast rules) |
| **Independent Revision** | Hicks: all B1–B6 fixed + architectural assertions |
| **Final Status** | ✅ APPROVED (ready for commit/push + CI E2E) |
| **Decisions Merged** | D-175 through D-187 (13 new decisions) |
| **Branch** | `feature/pwa-device-navigation` (uncommitted) |

### Key Deliverables

**Ripley's Design Review (D-175–D-179):**
- Presentation modes (`pwa`/`mobile`/`desktop`) gated on `isStandalonePwa()`, never viewport breakpoints
- F045 recorded as backlog entry; F027 narrowed; F023 annotated
- Implicit PWA category grouping (no URL pollution) + 500-row cap constraint
- `DeviceTable` split required (desktop table / mobile cards / PWA rows)
- Menu popover reuses desktop structure; option set preserved exactly

**QC Audit Blockers (Ripley REJECTED, then Hicks fixed):**

| Blocker | Ripley Finding | Hicks Fix | Test |
|---------|---|---|---|
| **B1** | Header z-index tie with sticky sub-headers | Closed → `--z-fixed` (30), extracted helper | `headerStacking.test.ts` |
| **B2** | Sentinel `groupBy=none` on every filter change in PWA | Type widened to literal `'none'`, URL builder extracted | `deviceFilterUrl.test.ts` (8 cases) |
| **B3** | View control hidden in app mode | Restored with conditional class, `DeviceTable` honors mode | `DeviceTable.test.ts` |
| **B4** | 500-row cap truncates desktop | Scoped to PWA caller via optional `maxRows` | `devices.test.ts` (650-row case) |
| **B5** | `role="group"` missing, tautological test | Added `role`, replaced with real structural tests | `AppBottomNav.test.ts` |
| **B6** | Popover containing-block risk | New `AppMenuPopover.containing-block.test.ts` | 3 assertions |

**Vasquez's Implementation (D-183/D-184):**
- `displayMode.svelte.ts` (reactive `isStandalonePwa()` wrapper)
- `AppBottomNav` (3.5rem pill, 3 equal-width items, Settings bubble)
- `AppMenuPopover` (anchored, no scroll-lock/backdrop)
- `DevicePwaRow` (two-line grouped rows)
- `DeviceTable` split (thin selector, `DeviceTableDesktop`/`Cards`/`PwaList`)
- Implicit grouping via `effectiveGroupBy` (separate from URL-written `urlFilters.groupBy`)
- Sentinel write-path fixed; view control restored

**Apone's E2E Contract (D-185):**
- `15-pwa-shell.spec.ts`: 21 scenarios × 6 projects = 126 test cases (all collected, execution deferred to CI)
- `AppShellPage.ts`: `emulateStandalonePwa()` via `page.addInitScript`, selector assumptions documented
- `seedDevice()` fixture: optional `model` override for row-contract verification
- Role coverage: Admin-only Playwright (Member/Viewer → component snapshot Vitest)

**Drake's Visual Spec (D-186):**
- No new global tokens; component-scoped custom properties
- Light theme: `rgb(255 255 255 / 0.92)` surface, `#515154` inactive, `#005bb5` active
- Dark theme: `rgb(29 29 31 / 0.88)` surface, `#d2d2d7` inactive, `#a3cdff` active
- Pill: `min-height: 3.5rem` × 3 items (flex equal thirds), label 10.6px with `letter-spacing: 0.005em` (critical: neutralizes global tightening)
- Highest risk: 10px labels on translucent blurred pill (both themes)

**Spec Amendments (D-180–D-182):**
- Settings bubble icon-only (geometry constraint); pill items remain labeled
- Theme block moved outside `role="menu"` boundary (axe compliance)
- 500-row cap scoped to PWA only (desktop unbounded)

### Validation Status

| Check | Backend | Frontend |
|-------|---------|----------|
| **Format** | ✅ `dotnet format --verify` | ✅ `pnpm run lint` |
| **TypeScript** | N/A | ✅ `pnpm run check` |
| **Build** | ✅ `dotnet build -c Release` | ✅ `pnpm run build` |
| **Unit Tests** | ✅ 278 passed | ✅ 560 Vitest passed |
| **Integration** | ✅ 216 passed (5 skipped) | N/A |
| **Vulnerability** | ✅ scan clean | ✅ no audit issues |
| **E2E Playwright** | N/A | ⏸ 126 cases collected (CI deferred) |

### No Regressions

- ✅ Desktop Playwright suite (no edits required per D-175 constraint)
- ✅ Mobile web (viewport-based split unchanged)
- ✅ Menu option set (roles preserved)
- ✅ Token storage (sessionStorage only)
- ✅ API (no backend changes; grouping 100% client-side)

### Deferred Work

**GitHub issue triage (#127–#139):** PDF-captured; no code work started. Next phase: agentic-development audit (watch-tracker-app, Aurearia, drinks-and-desserts codebases).

### Decision Consolidation

**13 new decisions merged into `.squad/decisions.md`:**
- D-175: Presentation modes gated on `isStandalonePwa()`
- D-176: F045 backlog record
- D-177: Implicit PWA grouping + 500-row cap
- D-178: `visibleColumns` assertion + `DeviceTable` split
- D-179: Menu popover structure reuse
- D-180: Settings icon-only narrowing
- D-181: Theme block order (outside `role="menu"`)
- D-182: 500-row cap scope to PWA
- D-183: Scope split (Vasquez/Apone/Drake/Hicks)
- D-184: Vasquez technical choices
- D-185: Apone E2E contract
- D-186: Drake visual spec
- D-187: Hicks QC revision fixes

**Inbox cleanup:** Removed 6 F045-related decision files (ripley-pwa-device-navigation, vasquez-pwa-device-shell, apone-pwa-shell-tests, drake-pwa-visual-rules, ripley-f045-qc-audit-amendments, hicks-f045-revision). Unrelated inbox entries preserved.

### Coordinator's Next Steps

1. Inspect consolidated decisions (D-175–D-187)
2. Create PR from `feature/pwa-device-navigation`
3. Await GitHub CI E2E Playwright execution (126 cases, full 6-project matrix)
4. Merge after E2E green

### Key Insight

QC audit + independent revision cycle validated architectural decisions (D-175 presentation-mode gating, D-182 PWA-scoped cap) and surfaced implicit assumptions (D-184 effective-vs-URL grouping pattern, D-185 standalone-PWA emulation strategy). All blockers were defensible; resolution strengthened test coverage and spec precision rather than bypassing requirements.

---

## Phase 1 Complete: 48/48 tasks shipped (2026-05-18)

**Headline:** Core API Phase 1 delivered end-to-end with full verify pipeline green.

### Summary Statistics

| Metric | Value |
|--------|-------|
| **Phase 1 Tasks** | 48/48 ✅ |
| **Backend Tests** | 369 passed / 1 skipped |
| **Coverage** | Domain 100.00% / App 91.58% / Infra 94.33% / Api 91.63% |
| **Test Trajectory** | 121 → 151 → 266 → 345 → 369 |
| **All Commits** | 7 SHAs landed this round |

### Round 7 Commits

| Commit | Author | Subject |
|--------|--------|---------|
| `e20a1bb` | Hudson | ci(workflow): T47 full verify chain on PR |
| `402eceb` | Hudson | chore(docs): T47 Hudson's CI audit findings |
| `ca85041` | Hudson | ci(workflow): T47 refine pre-commit hook |
| `65e1184` | Hudson | docs(ci): T47 CI setup checklist for Brian |
| `00fe492` | Hicks | feat(api): add import and export verticals (T29-T31, T39, T42, T48) |
| `9dbfd51` | Hicks | chore: session handoff (state files) |
| `fa0e696` | Apone | test: T45-T46 import/export/contract suites |

### Phase 1 Deliverables

**Backend Core API:**
- Clean Architecture: Domain (100% coverage) → Application (91.58%) → Infrastructure (94.33%) → Api (91.63%)
- MediatR handlers for all entity CRUD operations (T20–T28)
- FluentValidation + ValidationBehavior pipeline (T18)
- AuditEvent append-only with AuditBehavior (T11, T19)
- RFC 7807 ProblemDetails error serialization (T41)
- Development auth bypass for local testing (T22)

**Import/Export Verticals:**
- CSV import preview (parse, validate, suggest lookups) → commit (create lookups, persist batch, audit)
- CsvHelper-backed parsing with configurable size cap
- JSON/CSV export with async buffered streaming
- Stateless re-parse strategy (no preview tokens)

**Testing & Quality:**
- 369 backend tests (unit + integration); 1 skipped (export schema, intentional)
- OpenAPI contract + drift validation (T46)
- GitHub Actions CI: format → build → unit/integration → vuln scan → frontend checks → E2E
- Pre-commit hook: lint + security (~2-3s)
- Branch protection documentation for manual GitHub UI setup

**Documentation:**
- OpenAPI 3.1 spec committed and auto-generated from runtime
- 34 architectural decisions (D-001 through D-034) in `.squad/decisions.md`
- Agent histories updated with Round 7 work
- `.github/workflows/README.md` CI one-pager for developers

### Coverage Snapshot

| Layer | Coverage | Notes |
|-------|----------|-------|
| **Domain** | **100.00%** | All entities, value objects, invariants covered |
| **Application** | **91.58%** | Handlers, validators, behaviors; minor scaffolds uncovered |
| **Infrastructure** | **94.33%** | Repositories, EF Core configs, migrations; edge cases uncovered |
| **Api** | **91.63%** | Controllers, error mapping, auth bypass; edge paths uncovered |

### Known Gaps (Deferred to Phase 2)

- **Entra OIDC wiring:** Bearer token validation; real directory integration
- **SvelteKit UI:** All 13 critical user journeys; role enforcement; offline PWA
- **gitleaks ULID cleanup:** Current false-positive pattern in `.gitleaks.toml` to be refined
- **Branch protection enforcement:** Manual GitHub UI step (not CI-side; documented for Brian)

### Next Phase: Phase 2 — SvelteKit UI + Real Entra Auth

- Implement all 13 critical user journeys
- Wire real Entra OIDC + PKCE (replace Dev bypass)
- Full E2E accessibility + performance validation
- Client token management (memory/sessionStorage)
- PWA offline capability
- Role-based UI enforcement (Viewer/Member/Admin)

---

## Phase 1 Round 6 Outcomes (2026-05-18)

### Hicks 🔧 — Commits `48c1920` + `74a1e21` "feat(api): T32-T40 entity controllers + dev auth bypass" / "feat(api): T41 ProblemDetails middleware + Result-to-HTTP mapping"

**Tasks Completed:** T32, T33, T34, T35, T36, T37, T38, T40, T41

- **T32–T38**: Resource controllers for Devices, Brands, Categories (with `/categories/tree` hierarchy route), Owners, Locations, Networks, Tags
- **T40**: AuditEventsController (read-only list with filters)
- **T41**: Global ProblemDetails middleware (`IExceptionHandler`) + Result→HTTP mapping via `ControllerResultExtensions`
- Development-only auth bypass: `Auth:DevBypass=true` in `appsettings.Development.json` produces synthetic `dev-admin` principal with Admin role; startup throws if enabled outside Dev and logs warning
- Controllers marked `[Authorize]` by default; thin routing (no business logic)
- OpenAPI 3.1 served at `/openapi/v1.json`; Swagger UI at `/swagger`
- Smoke tests on `http://localhost:8080`:
  - `GET /openapi/v1.json` → 200 JSON
  - `GET /api/v1/devices` → `200 {"items":[],"totalCount":0,"page":1,"pageSize":25}`
  - `POST /api/v1/brands -d '{"name":"TestBrand2"}'` → 201 Created with Location header
  - `POST /api/v1/brands -d ''` → 400 Validation ProblemDetails
  - `GET /api/v1/brands/{invalid-uuid}` → 404 ProblemDetails

**Files Modified:** 28 files

**Verification:** All checks green:
- `dotnet format --verify-no-changes` ✅
- `dotnet build -c Release` ✅
- (repo-root `dotnet test -c Release` blocked by Apone's in-flight compile fix; cleared after)

**Decisions Documented:** D-022 (Dev Auth Bypass), D-023 (Controller Routing), D-024 (Category Tree), D-025 (PagedResponse), D-026 (ProblemDetails), D-027 (Result Mapping)

---

### Apone 🧪 — Commit `60f7ce6` "test: T32-T41 controller HTTP integration coverage"

**Tasks Completed:** T45 (integration test suite, controller endpoints)

**Outcomes:**
- +79 tests added (266 baseline → 345 total)
- 79 executable controller integration tests covering full CRUD, error paths, auth bypass, and ProblemDetails shaping
- Route contract locked by tests: `/api/v1/{resource}` CRUD, `/api/v1/categories/tree`, `/api/v1/devices/{id}/tags`, `PATCH /api/v1/devices/{id}/owner` → 204 No Content
- **Bug fixed** (exposed by tests): Category soft-delete cascade now correctly archives intermediate nodes
- Test environment stable auth subject (fixed GUID), Admin role

**Coverage Snapshot (Post-Round-6):**
| Layer | Coverage |
|-------|----------|
| Domain | **100.00%** (held) |
| Application | **90.28%** (↑ from 85.89%) |
| Infrastructure | **93.19%** (↑ from 88.98%) |
| **Api** | **94.87%** (new) |

**Test Results:**
- Backend: **345 passed / 0 skipped / 0 failed** (delta +79)
- All checks green: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅

---

### Squad Orchestration — Scribe 📝

- **Decisions processed:** 6 files merged into D-022–D-027 (security, routing, paging, response shapes, error mapping)
- **Agent history updated:** Hicks (T32–T41 full summary), Apone (79 tests + coverage + bug fix), Scribe (R6 work)
- **Session log:** This entry (Phase 1 Round 6 outcomes)
- **Tasks.md:** Marked T32–T41 as ✅; T39, T42, T46 remain open (import/export/auth)
- **Deleted inbox:** 6 decision files merged and removed

**Phase 1 Progress:** 37/48 tasks done (77%)

---

## Phase 1 Round 5 Outcomes (2026-05-18)

### Hicks 🔧 — Commit `1180cf6` "feat(handlers): T20-T28 device + reference entity CRUD handlers"

**Tasks Completed:** T20, T21, T22, T23, T24, T25, T26, T27, T28

- **T20–T28**: Full Application handler suite for Device, Brand, Category, Owner, Location, Network, Tag CRUD operations
  - `CreateDeviceCommand`, `UpdateDeviceCommand`, `DeleteDeviceCommand` with soft-delete (retired → disposed)
  - `GetDeviceByIdQuery`, `ListDevicesQuery` with pagination, filtering (brand/category/owner/location/network/status/tag), and sorting
  - Brand/Category/Owner/Location/Network CRUD handlers (Create/Update/Delete/Get/List)
  - Category handlers with recursive tree mapping; list paginates root nodes while preserving descendants
  - Owner delete blocks while any active device references the owner (preserves active-owner invariant)
  - `AddTagToDeviceCommand`, `RemoveTagFromDeviceCommand`, `ClaimDeviceOwnershipCommand` with join-entity audit metadata
  - New common type: `PagedResponse<T>` in `src/TechInventory.Application/Common/`
  - BEFORE-snapshot audit capture pattern crystallized across all mutations

**Files Modified:** 98 files touched

**Verification:** All checks green:
- `dotnet format --verify-no-changes` ✅
- `dotnet build -c Release` ✅
- `dotnet test -c Release` (182 succeeded, 78 skipped) ✅

---

### Apone 🧪 — Commit `6685cc6` "test: T20-T28 handler scaffolding + domain coverage recovery"

**Tasks Completed:** T43, T44 (handler contract tests + coverage recovery)

**Outcomes:**
- +115 tests added (151 baseline → 266 total)
- 102 skip-when-waiting handler scaffolds converted to executable xUnit/NSubstitute tests
- Handler-contract assumptions locked: active-reference validation, duplicate-name conflict detection, BEFORE-snapshot capture, owner delete-blocking
- Domain coverage regression from R4 fully recovered

**Coverage Snapshot (Post-Round-5):**
| Layer | Coverage |
|-------|----------|
| Domain | **100.00%** |
| Application | **85.89%** |
| Infrastructure | **88.98%** |

**Test Results:**
- Backend: **266 passed / 0 skipped**
- All checks green: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅

---

### Squad Orchestration — Scribe 📝

- **Decisions processed:** Inbox empty; no new D-### entries
- **Agent history updated:** Hicks (T20–T28 summary), Apone (coverage recovery), Scribe (R5 work)
- **Session log:** This entry (Phase 1 Round 5 outcomes)
- **Tasks.md:** Verified T20–T28 already marked ✅ (no changes needed)

**Phase 1 Progress:** 28/48 tasks done (58%)

---

## Phase 1 Round 4 Outcomes (2026-05-18)

### Hicks 🔧 — Commit `81f478d` "feat: add repositories and pipeline behaviors"

**Tasks Completed:** T16, T17, T18, T19

- **T16**: 10 concrete repositories implemented in `src/TechInventory.Infrastructure/Persistence/Repositories/`:
  - `BrandRepository`, `CategoryRepository`, `DeviceRepository`, `HouseholdRepository`, `OwnerRepository`, `LocationRepository`, `NetworkRepository`, `TagRepository`, `AuditEventRepository`, `ImportBatchRepository`
  - Shared `Repository<TEntity, TKey>` base with shared add/get/update plumbing
  - Exact-ID reads remain unit-of-work aware; list paths filter inactive/soft-deleted rows by default (configurable via `includeInactive` parameter)
  - `AuditEventRepository` exposes `AppendAsync` + queries only (no update/delete)

- **T17**: `AuditSaveChangesInterceptor` implementation
  - Wired through `AppDbContext.OnConfiguring()`
  - Stamps UTC `CreatedAt`/`ModifiedAt` and `CreatedBy`/`ModifiedBy` on all mutable `Entity`/`AggregateRoot` records
  - `ICurrentUserService` abstraction (currently `SystemCurrentUserService` for system actor)

- **T18**: `ValidationBehavior` pipeline
  - Returns `Result.Failure(new Error("Validation", "One or more validation failures occurred.", validationErrors))` 
  - `validationErrors` is property-name → string[] dictionary (no exceptions thrown)
  - Positioned first in MediatR pipeline

- **T19**: `AuditBehavior` pipeline
  - `IAuditable` marker interface at `src/TechInventory.Application/Auditing/IAuditable.cs`
  - Scoped `IAuditContext` captures BEFORE payloads without second DB read
  - BEFORE/AFTER strategy: BEFORE from context (serialized), AFTER from request object by default
  - Create operations store JSON `null` for BEFORE
  - Positioned last in MediatR pipeline (only fires on validation success)

**Decision Notes:** D-020 (Audit Context & Repository Balance) and D-021 (Pipeline Order Verification) merged into `decisions.md`

**Verification:** All checks green:
- `dotnet format --verify-no-changes` ✅
- `dotnet build -c Release` ✅
- `dotnet test -c Release` (151 tests) ✅

---

### Apone 🧪 — Test Coverage & Coverage Regression Flag

**Outcomes:**
- +30 tests added (121 baseline → 151 total)
- 58 new behavior + repository integration tests
- `ValidationBehavior` + `AuditBehavior` ordering verified (D-021)
- SQLite-backed repository tests via `IntegrationTestFactory<TMarker>` confirm soft-delete filtering and unit-of-work semantics
- All tests green: `dotnet test -c Release` ✅

**Coverage Snapshot (Post-Round-4):**
- Domain: 81.40% (regression from 96.45% pre-Round-4)
- Application: 40.53%
- Infrastructure: 88.98%

**⚠️ Regression Note:** Domain coverage dipped significantly. Likely Hicks's `AuditEvent`/`DbContext` additions not yet covered by test suite. **Flagged for explicit Round 5 audit** — Path A (continue task order, audit coverage) vs Path B (vertical slice priority).

**Decision Notes:** D-021 (Pipeline Order Verification) locked the behavior-composition pattern for future ordering checks.

---

### Squad Orchestration — Scribe 📝

- Merged 2 decision inbox files into `decisions.md` (D-020, D-021)
- Updated agent history files:
  - `agents/hicks/history.md`: Round 4 repositories + behaviors summary
  - `agents/apone/history.md`: Round 4 coverage snapshot + regression flag
  - `agents/scribe/history.md`: This session's work
- Created session log (this file)
- Deleted merged inbox files: `decisions/inbox/hicks-*.md`, `decisions/inbox/apone-*.md`

---

## Open Questions for Round 5

1. **Coverage regression audit** — Prioritize Domain coverage recovery (Path A) or defer in favor of vertical-slice handler + controller wiring (Path B)?
2. **Session log cadence** — Should this file track every round, or only major milestones?
3. **Archive strategy** — `decisions.md` at ~24KB now; plan archival to `decisions/archive/` after Round 5?

---

## Governance Notes

- All meaningful changes require team consensus (D-001 onwards established)
- Architectural decisions logged in `decisions.md` with D-### sequential IDs
- Agent-specific work tracked in `agents/{agent}/history.md` for retrospective
- Session log (this file) serves as coordination checkpoint between rounds

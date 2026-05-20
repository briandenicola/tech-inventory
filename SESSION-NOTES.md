# Session Notes

Append-only log. Newest entries at the top.

---

## 2026-05-20 — Hicks F035 era/decade report API

- Added `src\TechInventory.Application\Reports\Queries\GetEraReportQuery.cs`, `IReportingRepository.GetEraReportAsync(...)`, and `GET /api/v1/reports/eras` in `src\TechInventory.Api\Controllers\ReportsController.cs`, returning newest-first decade buckets with optional `categoryId` filtering plus `asOfDate` / `appliedCategoryId` metadata.
- Implemented the repository as a single EF Core projection over active devices with non-null purchase dates; bucketing uses `year / 10 * 10`, sums `PurchasePrice` into `totalValue`, includes pre-1980 and future purchase years, and limits `sampleDevices` to three names per decade.
- Added backend coverage in `tests\TechInventory.UnitTests\Application\ReportingQueryHandlerTests.cs`, `tests\TechInventory.IntegrationTests\Controllers\ReportsControllerTests.cs`, and new `tests\TechInventory.IntegrationTests\Repositories\ReportingRepositoryIntegrationTests.cs`; regenerated repo-root `openapi.yaml` so `/api/v1/reports/eras?categoryId=` is reflected in the committed spec.
- Validation: `dotnet format --verify-no-changes` ✅, targeted reporting + OpenAPI drift tests ✅, `dotnet test -c Release --no-build` ✅ (**438 total / 433 passed / 5 skipped / 0 failed**). `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` still fails only when Playwright teardown tries to call unavailable `docker` in this environment.

---

## 2026-05-20 — Vasquez F035 era report card + i18n

- Added `src\TechInventory.Web\src\lib\components\EraReportCard.svelte`, `EraReportDecade.svelte`, and `EraReportCard.test.ts`, then wired the new card into `src\TechInventory.Web\src\routes\(authenticated)\reports\+page.svelte` so `/reports` now shows a whimsical decade breakdown with gradient bars, sample-device chips, a category filter, and explicit loading/error/empty states.
- Extended `src\TechInventory.Web\src\lib\api\client.ts`, `api\types.ts`, and `src\TechInventory.Web\src\lib\utils\reports.ts` / `reports.test.ts` with the frozen `/api/v1/reports/eras` contract plus normalization helpers, and added the F035 labels under `src\TechInventory.Web\src\lib\i18n\en.json` (title, legend, decade labels, filter text, empty/error copy, table labels).
- Updated `docs\backlog.md` to mark `F035` in progress, appended Vasquez learnings, and dropped decision note `.squad\decisions\inbox\vasquez-era-report-card-contained-fetching.md` documenting why whimsical report cards should own their own fetch/filter state while reusing `referenceDataStore`.
- Validation: `pnpm run check` ✅, `pnpm run lint` ✅, focused `pnpm exec vitest run src/lib/components/EraReportCard.test.ts src/lib/utils/reports.test.ts` ✅, full `pnpm exec vitest run` ✅, `pnpm run build` ✅. Repo `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` still fails only when Playwright teardown tries to call unavailable `docker` in this environment.

---

## 2026-05-20 — Vasquez P003-T06 responsive admin tables

- Added `src\TechInventory.Web\src\lib\components\admin\ResponsiveAdminList.svelte` plus `ResponsiveAdminList.test.ts`, then rewired `admin/brands`, `admin/locations`, `admin/networks`, `admin/owners`, and `admin/tags` so they render single-column cards below `md` and semantic tables at `md+`; brands/locations keep Merge actions visible in the mobile card action rows.
- Reworked `admin/categories/+page.svelte` separately so the category tree remains on `md+`, but mobile now gets flat responsive cards with parent context instead of the previous overflowing tree rows; all admin headers stack cleanly, and action/form controls now use `min-h-11` / `h-11` touch targets.
- Validation: `pnpm run check` ✅, `pnpm run lint` ✅, `pnpm exec vitest run` ✅ (**338 passed / 1 skipped**, including new `ResponsiveAdminList` axe coverage), `pnpm run build` ✅.

---

## 2026-05-20 — Vasquez + Bishop silent SSO auto-login polish

- Updated `src\TechInventory.Web\src\routes\+layout.svelte` so app bootstrap now processes any MSAL redirect result, then attempts `acquireTokenSilent()` against the cached account before clearing auth. Successful silent restoration hydrates `/api/v1/owners/me` and skips auth entry routes by routing straight to `/devices`; `interaction_required`/missing-session cases now fall back cleanly to the existing login page instead of forcing an immediate redirect loop.
- Updated `src\TechInventory.Web\src\lib\auth\index.ts` to promote the first cached MSAL account to active state in this single-household app and to expose a silent-only token helper distinct from the normal API-call redirect fallback. `src\TechInventory.Web\src\routes\auth\login\+page.svelte` now shows a short “Checking your household session…” splash (with delayed spinner) while the silent check runs, so users never see a flash of the login CTA before an auto-redirect.
- Added focused coverage in `src\TechInventory.Web\src\lib\auth\index.test.ts` for cached-account promotion, silent success, `interaction_required` fallback, and the normal interactive redirect path. Validation: `pnpm run check` ✅, changed-file ESLint ✅, `pnpm exec vitest run src/lib/auth/index.test.ts` ✅ (**5 passed**), full `pnpm exec vitest run` ✅, `pnpm run build` ✅. Repo `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now gets through backend/frontend verification and still fails only when Playwright tries to call unavailable `docker` in this environment.

---

## 2026-05-20 — Hicks P003-T09/T10 merge + insurance backend alignment

- Finalized the backend half of `P003-T09` and `P003-T10`: `POST /api/v1/brands/merge`, `/api/v1/categories/merge`, and `/api/v1/locations/merge` are admin-only MediatR flows that validate distinct IDs, reassign device foreign keys, deactivate the source record, and append paired `AuditEvent` entries; category merge also reparents descendants and blocks descendant/depth conflicts.
- Added `GET /api/v1/reports/insurance` through `GetInsuranceReportQuery`, `IReportingRepository.GetInsuranceReportItemsAsync(...)`, and `ReportsController`, returning a dated CSV attachment with a generated-at comment line, active-device rows, optional `locationId` filter, and a `TOTAL` footer row.
- Regenerated repo-root `openapi.yaml` from the runtime document and changed the `IDeviceRepository` reassignment helpers to return `Result<int>` so the repository contract suite stays consistent with the Application-layer result-pattern rules.
- Validation: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅ (**431 total / 426 passed / 5 skipped / 0 failed**). `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now gets through format/build/test/vulnerability/frontend checks and fails only when Playwright tries to call unavailable `docker` in this environment.

---

## 2026-05-20 — Vasquez P003-T09/T12/T13 merge UI + reports

- Added `src\TechInventory.Web\src\lib\components\MergeEntityModal.svelte` plus `src\TechInventory.Web\src\lib\utils\referenceMerge.ts`, then wired merge actions into `admin/brands`, `admin/categories`, and `admin/locations` with admin-only buttons, live device-count confirmation text, toast success feedback, and reference-data refresh after a successful merge.
- Added `src\TechInventory.Web\src\routes\(authenticated)\reports\+page.svelte`, `ReportMetricCard.svelte`, `ReportBreakdownCard.svelte`, `WarrantyExpiryPanel.svelte`, and `src\TechInventory.Web\src\lib\utils\reports.ts` so `/reports` now shows summary cards, responsive breakdown bars, 30/60/90-day warranty filters, sortable expiry rows, and a new Reports nav item.
- Frontend-side reporting contracts are normalized to tolerate both Hicks's current backend field names (`totalActiveDeviceCount`, `devices`, `daysRemaining`) and the planned API contract (`totalDevices`, `items`, `daysUntilExpiry`) while the generated OpenAPI client catches up; merge/report endpoint wrappers live in `src\TechInventory.Web\src\lib\api\client.ts` with local type aliases in `api/types.ts` because `pnpm run generate:client` currently fails on unresolved report schema refs in `openapi.yaml`.
- Validation: `pnpm run check` ✅, `pnpm run lint` ✅, `pnpm exec vitest run src/lib/components/MergeEntityModal.test.ts src/lib/utils/reports.test.ts src/lib/navigation/appNav.test.ts` ✅ (**10 passed**), follow-up `pnpm exec vitest run src/lib/components/WarrantyExpiryPanel.test.ts` ✅ (**2 passed**, including `vitest-axe` coverage), full `pnpm exec vitest run` ✅ (**329 passed / 1 skipped**), `pnpm run build` ✅. Repo `scripts\verify.ps1` still fails on pre-existing backend issues: a locked `TechInventory.Api.exe` during build retries and `RepositoryInterfaceContractTests` failing on `IDeviceRepository`.

---

## 2026-05-20 — Vasquez P003-T07 audit modal + P003-T08 dark mode toggle

- Added `src\TechInventory.Web\src\lib\components\AuditLogModal.svelte` and wired it into `devices/[id]/+page.svelte`, `DeviceDetailModal.svelte`, and `admin/+page.svelte`, so Admin users can open paginated audit history in-place from device detail or the admin hub without losing context; device-scoped opens include the existing `DeviceAuditTrail.svelte` summary, and global admin opens include an entity-type filter.
- Added `src\TechInventory.Web\src\lib\stores\theme.svelte.ts`, updated `app.html` pre-hydration theme bootstrapping plus `app.css` dark-variant handling, and rewired `ThemeToggle.svelte` so Light / Dark / System now persist under `localStorage['theme-preference']`, apply both `data-theme` and `.dark` on `<html>`, and appear in the authenticated header menus as well as Settings.
- Added/updated i18n strings in `src\TechInventory.Web\src\lib\i18n\en.json`, plus focused theme tests in `src\TechInventory.Web\src\lib\stores\theme.svelte.test.ts` and `src\TechInventory.Web\src\lib\components\ThemeToggle.test.ts`.
- Validation: `pnpm exec vitest run src/lib/stores/theme.svelte.test.ts src/lib/components/ThemeToggle.test.ts` ✅ (**7 passed**); `pnpm run build` ✅. `pnpm run check` ended on workspace-wide frontend type-resolution issues in this shared environment before settling to the pre-existing `MergeEntityModal.svelte` warning only, and `pnpm run lint` likewise reports the existing `MergeEntityModal.svelte` warning with no new errors from T07/T08 changes.

---

## 2026-05-20 — Vasquez P003-T04 pull-to-refresh

- Added `src\TechInventory.Web\src\lib\components\PullToRefresh.svelte` plus `src\TechInventory.Web\src\lib\stores\pullToRefresh.ts`, then wrapped `src\TechInventory.Web\src\routes\(authenticated)\+layout.svelte` so authenticated routes get one shared touch gesture, spinner, `overscroll-behavior-y: contain`, and coarse-pointer gating.
- Devices list refresh now clears the infinite-scroll accumulation before refetching page 1; device detail/edit/new routes refresh their device/reference data; admin list pages reuse their existing `load*` functions as the registered callbacks.
- Added `pullToRefresh.*` strings to `src\TechInventory.Web\src\lib\i18n\en.json` and a focused `src\TechInventory.Web\src\lib\components\PullToRefresh.test.ts` suite covering coarse-pointer gating, scroll-top guard, trigger threshold, and axe-core.
- Validation: `pnpm exec vitest run src/lib/components/PullToRefresh.test.ts` ✅ (**4 passed**), `pnpm exec vitest run` ✅ (**256 passed / 1 skipped**), `pnpm run build` ✅. `pnpm run check` remains blocked by pre-existing `DeviceAuditTrail.test.ts` Node typing errors plus long-standing admin-page Zod/typing issues; targeted ESLint on changed non-admin files passed.

---

## 2026-05-20 — Vasquez P003-T05 consistent hamburger nav

- Audited every authenticated route under `src\TechInventory.Web\src\routes\(authenticated)` and confirmed there are no nested admin/device layouts overriding the app shell; the inconsistency came from stale shared navigation lists.
- Added `src\TechInventory.Web\src\lib\navigation\appNav.ts` and reused it in `(authenticated)/+layout.svelte` plus `/admin/+page.svelte`, so desktop nav, the mobile hamburger, and admin hub cards now share the same route order and include all admin sub-routes (`brands`, `categories`, `locations`, `networks`, `owners`, `tags`).
- Increased mobile touch targets to 44px (`h-11` / `min-h-11`) for the hamburger button, menu links, and sign-out action; moved related nav labels/descriptions into `src\TechInventory.Web\src\lib\i18n\en.json`.
- Validation: targeted ESLint on changed files ✅, `pnpm exec vitest run src/lib/navigation/appNav.test.ts` ✅ (4/4), `pnpm exec vitest run` ✅. `pnpm run check` still fails on the pre-existing `DeviceAuditTrail.test.ts` missing Node typings issue, and `pnpm run lint` still fails on pre-existing admin `any` / unused-var violations outside this fix.

---

## 2026-05-20 — Vasquez P003-T02 device tag assignment fix

- Added `src\TechInventory.Web\src\lib\components\DeviceTagSelector.svelte`, expanded `referenceDataStore` to include tags, and wired `DeviceForm.svelte` to load reference data on mount so device create/edit can actually render assignable tags.
- Extended `src\TechInventory.Web\src\lib\api\client.ts` with `devices.listTags/addTag/removeTag/syncTags`, then updated `/devices/new`, `/devices/[id]/edit`, and `/devices/[id]` to sync tag selections after save, preload existing tag assignments, and render assigned tags on the detail page.
- Fixed coupled navigation bugs in device CRUD surfaces by correcting Svelte interpolated hrefs in `DeviceTable.svelte`, `devices/[id]/+page.svelte`, and `devices/[id]/edit/+page.svelte`.
- Added DeviceForm coverage for tag rendering, preselection, submission, and retired-field disabling. Validation: `pnpm exec vitest run src/lib/components/DeviceForm.test.ts` ✅ (**22 passed / 1 skipped**), `pnpm exec vitest run` ✅ (**248 passed / 1 skipped**). `pnpm run lint` and `pnpm run check` remain blocked by pre-existing admin-page / Node typing issues already present in the shared workspace.

---

## 2026-05-20 — Hicks P003-T11 reporting APIs

- Added the reporting vertical under `src\TechInventory.Application\Reports`, `src\TechInventory.Infrastructure\Persistence\Repositories\ReportingRepository.cs`, and `src\TechInventory.Api\Controllers\ReportsController.cs`; `/api/v1/reports/summary`, `/api/v1/reports/warranties`, and `/api/v1/reports/spending` now dispatch through MediatR with repository-backed EF projections and no N+1 lookups.
- Added nullable `Device.WarrantyExpiry` end-to-end (Domain/Application/Api + EF migration `20260520165251_AddDeviceWarrantyExpiry`) so warranty tracking is persisted through existing device create/update/get contracts instead of being a reporting-only transient field.
- Added backend coverage in `tests\TechInventory.UnitTests\Application\ReportingQueryHandlerTests.cs` and `tests\TechInventory.IntegrationTests\Controllers\ReportsControllerTests.cs`, then regenerated repo-root `openapi.yaml` from the runtime document.
- Validation: `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release` ✅ (**398 total / 392 passed / 6 skipped / 0 failed**). `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` still fails on pre-existing frontend `svelte-check` errors in admin/device pages unrelated to the reporting backend changes.

---

## 2026-05-20 — Vasquez P003-T03 infinite scroll devices list

- Reworked `src\TechInventory.Web\src\routes\(authenticated)\devices\+page.svelte` so devices load progressively via IntersectionObserver while keeping the backend contract on `page` + `pageSize` untouched.
- Added a reduced-motion fallback that restores `PaginationControls`, plus a floating `BackToTopFab` that switches to non-smooth scrolling when motion reduction is enabled.
- Refactored `src\TechInventory.Web\src\lib\queries\devices.svelte.ts` with shared page/pageSize clamps (`<= 200`), imperative page fetching for infinite scroll, and closure-based filter wiring to avoid the prior Svelte capture warning.
- Added `BackToTopFab` + targeted tests for the FAB and query helpers. Validation: targeted ESLint/Vitest passed; full `pnpm exec vitest run` and `pnpm run lint` still surface unrelated pre-existing failures in `DeviceForm.test.ts` and admin pages.

---

## 2026-05-20 — Vasquez P003-T01 audit trail contrast fix

- Replaced the hard-coded audit trail block on `src\TechInventory.Web\src\routes\(authenticated)\devices\[id]\+page.svelte` with a dedicated `DeviceAuditTrail.svelte` component that uses semantic tokens from `src\TechInventory.Web\src\lib\tokens.css` for surface, text, secondary text, and border styling.
- Added `src\TechInventory.Web\src\lib\components\DeviceAuditTrail.test.ts` to cover rendering, missing-actor behavior, axe-core, and explicit WCAG AA contrast assertions for light/dark token pairs.
- Added `devices.detail.audit.*` strings to `src\TechInventory.Web\src\lib\i18n\en.json` and marked `P003-T01` complete in `specs\003-pwa-polish\tasks.md`.
- Validation: targeted ESLint on changed files passed; `pnpm exec vitest run src/lib/components/DeviceAuditTrail.test.ts` passed (4/4). Full `pnpm exec vitest run` still fails on pre-existing `DeviceForm.test.ts` regressions, and `pnpm run check` still fails on pre-existing admin/Zod issues in the shared workspace.

---

## 2026-05-18 — Bishop D-136 owner auto-provision on first sign-in

- Fixed `/api/v1/owners/me` so it no longer 404s for first-time principals: `OwnersController` now dispatches `EnsureCurrentOwnerProvisionedCommand`, which returns the existing owner by `EntraObjectId` or creates one on demand from the caller's claims.
- Extended `ICurrentUserService` / `HttpContextCurrentUserService` / `SystemCurrentUserService` with display-name + role helpers so claim-derived defaults stay behind the current-user abstraction. Auto-provision defaults are `ClaimTypes.Name` / `ClaimTypes.Role`, with fallbacks to `User {short}` and `Member`.
- Added unit coverage for the command (existing owner unchanged, missing owner provisioned, claim fallbacks, validator) plus integration coverage that `/api/v1/owners/me` auto-provisions on first call, returns the same owner on second call, and works for dev bypass with a fresh DB.
- Refreshed `openapi.yaml` for the `/api/v1/owners/me` contract, then verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release` (**388 total / 382 passed / 6 skipped / 0 failed**).
- Manual smoke passed after resetting `src\TechInventory.Api\techinventory.db`: `task db:migrate`, `task dev:api`, `curl.exe -s http://localhost:8080/api/v1/owners/me | ConvertFrom-Json` returned 200 with `displayName=dev-admin`, `role=Admin`, and `entraObjectId=11111111-1111-1111-1111-111111111111`.

---

## 2026-05-18 — Round 7 Phase 1 Close-Out (Scribe)

Phase 1 complete: 48/48 tasks shipped. Merged 7 decision inbox files (Hudson's 2 collisions renumbered as D-028/D-029; Hicks's 5 import/export decisions as D-030–D-034) into `.squad/decisions.md`. Updated agent histories for Hicks (T29-T31, T39, T42, T48 import/export + OpenAPI), Apone (T45-T46 integration/contract suites), Hudson (T47 CI verify chain), and Scribe (close-out). Added "Phase 1 complete" banner to `specs/001-core-api/tasks.md` and marked T29, T30, T31, T39, T42, T45, T46, T47, T48 as ✅ (all remaining tasks done). Appended Phase 1 complete round to `.squad/session-log.md` with summary statistics (48/48, 369 tests, coverage 100%/91.58%/94.33%/91.63%), commit table (7 SHAs), deliverables summary, known gaps, and Phase 2 next steps. Updated `.copilot-state.md` to note Phase 1 complete and that dev auth bypass is active for local API access. Deleted all 7 inbox decision files. Staged only `.squad/`, `specs/001-core-api/tasks.md`, `.copilot-state.md`, `SESSION-NOTES.md` — no backend/frontend changes. Commit message references all 7 SHAs and Phase 1 closure.

---

## 2026-05-18 — Hicks Round 7 import/export close-out + OpenAPI export
- Added the Phase 1 import/export backend verticals in `src\TechInventory.Application\Imports`, `src\TechInventory.Application\Exports`, `src\TechInventory.Api\Controllers\ImportsController.cs`, `src\TechInventory.Api\Controllers\ExportsController.cs`, and `src\TechInventory.Api\OpenApi\OpenApiDocumentExporter.cs`; preview/commit now share `DeviceImportProcessingService`, export uses a dedicated `IDeviceExportService`, and repo-root `openapi.yaml` is generated from the running API shape instead of hand-editing
- Expanded auditing for bulk import by changing `IAuditContext` / `AuditContext` / `AuditBehavior` to carry multiple entries, extracted shared `DeviceValidationRules`, registered CsvHelper-backed import processing in DI, added import file-size handling + 413 mapping, and seeded a default `Primary Household` (`USD`) at startup when the database is empty so device creation/import works in a fresh environment
- Smoke test on `http://localhost:8080` passed: `POST /api/v1/imports/preview` returned valid + invalid rows plus `lookupsToCreate`; `POST /api/v1/imports/commit` returned `201 Created` with a persisted batch; `GET /api/v1/imports` listed the new batch; `GET /api/v1/exports/devices?format=json` returned exported devices; `GET /api/v1/exports/devices?format=csv` returned device rows; `GET /openapi/v1.json` returned runtime OpenAPI JSON
- Verification from repo root on Windows passed: `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and `dotnet run --project src\TechInventory.Api\TechInventory.Api.csproj -c Release --no-build -- export-openapi`; final backend test summary is **370 total / 369 succeeded / 1 skipped / 0 failed**

---

## 2026-05-18 — Apone Round 7 import/export integration + OpenAPI contract coverage
- Added T45 coverage under `tests/TechInventory.IntegrationTests/Controllers/ImportsControllerTests.cs` and `ExportControllerTests.cs`, exercising preview/commit/list/get-by-id import flows, config-driven 413 handling, lookup auto-creation, CSV/JSON exports, filtered exports, and large-dataset export reads against the real SQLite `WebApplicationFactory` harness
- Added T46 coverage under `tests/TechInventory.IntegrationTests/Contract/` with reusable OpenAPI canonicalization/schema helpers, runtime-vs-committed drift assertions, and endpoint schema checks for brands/categories/devices/owners/locations/networks/tags/audit-events/import preview; the export 200-body schema assertion remains intentionally skipped until `/api/v1/exports/devices` advertises that response schema
- Verified `dotnet format --verify-no-changes` ✅, `dotnet build -c Release` ✅, `dotnet test -c Release --no-build` ✅ (`240` unit + `129` integration passed, `1` skipped), `dotnet list package --vulnerable --include-transitive` ✅, and fresh unit+integration Cobertura merges at **Domain 100.00% / Application 91.58% / Infrastructure 94.33% / Api 91.63%**
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now reaches Playwright after backend + frontend install/check/lint, but cannot finish in this environment because `docker` is unavailable for `scripts\run-e2e.ps1`

---

## 2026-05-18 — Hicks Round 6 controllers, ProblemDetails, and dev auth bypass
- Replaced the stub `src\TechInventory.Api\Controllers\DevicesController.cs` and added concrete `BrandsController`, `CategoriesController`, `OwnersController`, `LocationsController`, `NetworksController`, `TagsController`, and `AuditEventsController`; all routes now sit under `/api/v1/...`, are `[Authorize]`, and dispatch through MediatR with no business logic in controllers
- Added `src\TechInventory.Api\Common\ControllerResultExtensions.cs` plus `ExceptionHandling\ResultFailureException.cs` / `ApiExceptionHandler.cs` so controller success paths stay terse while `Result.Failure` and unhandled exceptions become RFC 7807 ProblemDetails; validation now returns an `errors` dictionary, 404/409 map cleanly, and 500s stay generic outside Development
- Added Development auth bypass via `Auth:DevBypass` in `appsettings.Development.json`, `DevBypassAuthenticationHandler`, `PlaceholderJwtAuthenticationHandler`, and `HttpContextCurrentUserService`; startup logs the required warning, audit rows now stamp the synthetic `oid`, and OpenAPI moved to `/openapi/v1.json` while Swagger UI remains at `/swagger`
- Smoke test on `http://localhost:8080` passed: `GET /openapi/v1.json` 200 JSON, `GET /api/v1/devices` returned `{"items":[],"totalCount":0,"page":1,"pageSize":25}`, `POST /api/v1/brands` with `{"name":"TestBrand2"}` returned 201 Created, `POST /api/v1/brands` with empty name returned 400 Validation ProblemDetails, `GET /api/v1/brands/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` returned 404 ProblemDetails, and `GET /api/v1/audit-events` showed the created brand audit event with actor `11111111-1111-1111-1111-111111111111`
- Verification: `dotnet format --verify-no-changes` ✅, `dotnet build src\TechInventory.Api\TechInventory.Api.csproj -c Release` ✅, repo-root `dotnet build -c Release` and `dotnet test -c Release` currently fail because Apone's in-flight `tests\TechInventory.IntegrationTests\Controllers\DevAuthBypassTests.cs(94,40)` does not compile in this shared workspace; `dotnet test -c Release` still ran 240 passing tests before that compile stop

---

## 2026-05-18 — Apone Round 5 domain recovery + T20-T28 handler tests
- Added targeted Domain coverage recovery in `tests\TechInventory.UnitTests\Domain\` for `AuditEvent` default-timestamp rejection, `ImportBatch` EF/private-constructor + UTC convenience-constructor behavior, and `Currency.ToString()`; removed a dead duplicate child-depth guard from `src\TechInventory.Domain\Entities\Category.cs`
- Converted all T20–T28 handler scaffolds under `tests\TechInventory.UnitTests\Application\` into executable xUnit + FluentAssertions + NSubstitute tests once Hicks's handlers landed, including CRUD/query suites for Devices, Brands, Categories, Owners, Locations, Networks, Tags, device-tag add/remove, and ClaimDeviceOwnership
- Verified backend commands on Windows: `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, and fresh XPlat Cobertura runs for unit + integration projects; merged coverage snapshot is Domain 100.00%, Application 85.89%, Infrastructure 88.98%
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1` now reaches frontend linting cleanly but cannot finish in this environment because `docker` is unavailable for `scripts\run-e2e.ps1`; backend verify steps and frontend install/check/lint all succeeded before that environment failure
- Final backend test summary: **266 passed, 0 skipped** (test delta vs pre-Round-5 baseline: +115)

---

## 2026-05-18 — Hicks T20-T28 application handlers, paging responses, and ownership/tag flows
- Added concrete Application command/query packages under `src/TechInventory.Application\Devices`, `Brands`, `Categories`, `Owners`, `Locations`, `Networks`, and `Tags`; every handler now returns `Result`/`Result<T>`, every request has a FluentValidation validator, and list queries standardize on the new `PagedResponse<T>` DTO
- Device work shipped `CreateDeviceCommand`, `UpdateDeviceCommand`, `DeleteDeviceCommand`, `GetDeviceByIdQuery`, `ListDevicesQuery`, `AddTagToDeviceCommand`, `RemoveTagFromDeviceCommand`, and `ClaimDeviceOwnershipCommand`; creates resolve the single household for default currency, update/delete/ownership/tag-removal stash BEFORE payloads in `IAuditContext`, and delete now supports retired → disposed transitions
- Category handlers established the tree contract for Round 5: list paginates root nodes while preserving descendants, update rejects cycles and rebalances descendant depths, and delete cascades archive state through the subtree. Owner delete now blocks when any device still references the owner so the active-owner invariant remains intact
- Verified from repo root on Windows with `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`; current workspace summary was **182 succeeded / 78 skipped** because Apone's in-flight scaffolding tests are present locally alongside the committed suite
- Next: Apone can finish consumer-side handler tests against the concrete request/response types; Hicks can pick up T29+ import/export handlers or controller surface after coordination

---

## 2026-05-18 — Hicks T16-T19 repositories, audit stamping, and MediatR behaviors
- Added `AddApplication()` / `AddInfrastructure()` wiring so the API now registers concrete repositories, `AuditSaveChangesInterceptor`, `ICurrentUserService`, `IUnitOfWork`, scoped `IAuditContext`, and MediatR pipeline behaviors in the intended order (Validation first, Audit last)
- Implemented `Repository<TEntity, TKey>` plus all concrete Infrastructure repositories under `src/TechInventory.Infrastructure/Persistence/Repositories/`; exact-ID reads stay unit-of-work aware, list queries hide inactive reference rows by default, `IAuditEventRepository.AppendAsync` remains save-free, and Device list defaults exclude disposed rows unless explicitly filtered back in
- Added `AuditSaveChangesInterceptor`, `IAuditable`, `IAuditContext`, `ValidationBehavior`, and `AuditBehavior`; validation failures now return `Error.Code = "Validation"` with an `Error.ValidationErrors` dictionary, and audit BEFORE/AFTER payloads come from handler-populated `IAuditContext` + request JSON fallback
- Verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release` all pass from the repo root on Windows; attempted `scripts/verify.ps1`, but it stalled during the build phase after repeated waits, so final validation used the requested direct dotnet commands
- Next: Hicks can move into T20+ command/query handlers while Apone targets the new repository/behavior seams for broader Application coverage

---

## 2026-05-18 — Apone T44/T45 behavior + repository coverage follow-through
- Added direct Application behavior tests under `tests/TechInventory.UnitTests/Application/Behaviors/` for ValidationBehavior aggregation/pass-through, AuditBehavior success/failure/no-op cases, and a composed Validation→Audit pipeline assertion that failing validation never writes an AuditEvent
- Added SQLite-backed repository integration coverage under `tests/TechInventory.IntegrationTests/Repositories/` for Brand/Category/Owner/Location/Network/Tag CRUD + active-filter + audit-stamp checks, Device CRUD/filter/audit-stamp checks, and AuditEvent append-only persistence checks
- Hardened `tests/TechInventory.IntegrationTests/IntegrationTestFactory.cs` cleanup retries so SQLite file locks from `WebApplicationFactory` disposal do not fail otherwise-green integration runs
- Verified `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release`, `dotnet test tests\TechInventory.IntegrationTests\TechInventory.IntegrationTests.csproj -c Release`, `dotnet test -c Release`, and `dotnet test -c Release --collect:"XPlat Code Coverage"`
- Coverage snapshot from the latest merged unit+integration reports: Domain 81.40%, Application 40.53%, Infrastructure 88.98%

---

## 2026-05-18 — Hicks T11-T15 audit event, persistence, and repository seams
- Added immutable `AuditEvent` and `ImportBatch` domain entities under `src/TechInventory.Domain/Entities/`; `AuditEvent` now exposes `Actor`, `EntityType`, `EntityId`, `Action`, `Timestamp`, `BeforePayload`, and `AfterPayload` with no public mutation surface
- Added Application-layer repository abstractions plus shared `Result<T>`, `Result`, `PageRequest`, and `PagedResult<T>` under `src/TechInventory.Application/`; `IAuditEventRepository` is append/query only and repository contracts never expose `IQueryable`
- Added `AppDbContext`, EF Core entity configurations, a design-time factory, append-only save guards for `AuditEvent` / `ImportBatch`, and the `InitialCoreApi` migration under `src/TechInventory.Infrastructure/Persistence/`
- Updated Hicks/Apone contract coverage so repo-root `dotnet test -c Release` exercises the backend tests via `TechInventory.slnx`; patched the reflection helpers/tests to handle the new repository paging/result shapes cleanly
- Verified `dotnet ef migrations add InitialCoreApi`, `dotnet ef database update`, `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --filter FullyQualifiedName~RepositoryInterfaceContractTests`, and `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --filter FullyQualifiedName~AuditEvent`

## 2026-05-18 — Apone T11/T15 contract coverage
- Added `tests/TechInventory.UnitTests/Domain/AuditEventTests.cs` for append-only `AuditEvent` construction, UTC timestamp, public-surface immutability, and payload-transition guard clauses
- Added `tests/TechInventory.UnitTests/Application/Abstractions/RepositoryInterfaceContractTests.cs` plus `Support/ContractReflectionAssertions.cs` to lock repository async/`CancellationToken`/no-`IQueryable` seams and the `IAuditEventRepository.AppendAsync`-only mutation contract
- Added `tests/TechInventory.UnitTests` to `TechInventory.slnx` so repo-root `dotnet test -c Release` now executes the backend test projects instead of only building source projects
- Verified `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release`, `dotnet test tests\TechInventory.UnitTests\TechInventory.UnitTests.csproj -c Release --collect:"XPlat Code Coverage"`, and `dotnet test -c Release`
- Coverage snapshot from the unit suite: Domain 96.45% line coverage, Application 0% line coverage (interfaces/results/paging scaffolding present but no executable Application tests yet)

## 2026-05-18 — Hudson SQLite integration harness + hermetic E2E contract
- Added `IntegrationTestFactory<TMarker>` under `tests/TechInventory.IntegrationTests/` so each test class gets its own SQLite file, future EF Core migrations auto-apply when present, and cleanup removes the database plus WAL/SHM sidecars
- Repointed the existing `/health` smoke test to the new factory and verified `task test:integration` passes end-to-end against the new harness
- Added `task test:integration` and `task test:e2e`; `task test:e2e` now owns compose bring-up, readiness wait on `/health/ready`, Playwright execution against `http://localhost:3000`, and teardown via `scripts/run-e2e.ps1` / `scripts/run-e2e.sh`
- Split `scripts/verify.ps1` / `scripts/verify.sh` into unit → integration → vulnerability/frontend checks → hermetic Playwright order
- Next: Hicks lands `AppDbContext` + first migration against `ConnectionStrings:Default`; Apone expands factory-backed integration coverage once migrations exist

## 2026-05-18 — Hicks Phase 1 domain reference entities T06-T10
- Added `Category`, `Owner`, `Location`, `Network`, `Tag`, and `DeviceTag` under `src/TechInventory.Domain/Entities/` with trimmed-name guards, archive/reactivate methods, and normalized-name helpers for later repository uniqueness checks
- Category now keeps `ParentId` plus validated `Depth` (1-3) so the max-depth invariant is enforced in Domain; `Owner` carries `OwnerRole` and optional `EntraObjectId`; `DeviceTag` uses `IsActive` instead of hard deletes
- Replaced Apone's placeholder skips with executable domain tests for T06-T10; `tests/TechInventory.UnitTests` now runs 93 passing tests with zero skips, and Domain line coverage is 97.6%
- Verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, targeted unit/integration runs, and `./scripts/verify.ps1`
- Next: Hicks can take T11-T17 (AuditEvent, ImportBatch, AppDbContext/configs, repositories, audit stamping)

## 2026-05-18 — Hudson pre-commit security gate
- Added a repo-managed pre-commit hook at `.githooks/pre-commit` backed by `scripts/check-security.mjs` and a pinned `.gitleaks.toml` config
- Added `task hooks:install` plus cross-platform `scripts/install-gitleaks.ps1` / `scripts/install-gitleaks.sh` so fresh clones can wire hooks in one command
- Updated `.github/workflows/ci.yml` to install pinned gitleaks and mirror the hook against PR/push diffs
- Verified `task hooks:install`, `node .\scripts\check-security.mjs --diff-range HEAD`, and an isolated test repo commit rejection for an auth-token localStorage write attempt
- `./scripts/verify.ps1` still fails on the pre-existing frontend `vite.config.ts` type mismatch and unused `@ts-expect-error`
- Next: fix the frontend Vite/Vitest type conflict so the full verify pipeline is green again

## 2026-05-18 — Apone QA follow-through
- Added spec-driven Domain tests for `Currency`, `Household`, and `Device` under `tests/TechInventory.UnitTests/Domain/`
- Enabled the `/health` integration smoke test with `WebApplicationFactory<Program>` and exposed `Program` for test hosting
- Added Playwright token-storage enforcement under `tests/e2e/security/` plus reusable storage-inspection helper and skill note
- Validated with `dotnet test tests/TechInventory.UnitTests -c Release`, `dotnet test tests/TechInventory.IntegrationTests -c Release`, and `node .\\node_modules\\@playwright\\test\\cli.js test security/token-storage.spec.ts --reporter=line`
- Attempted `./scripts/verify.sh`, but this Windows session has no `bash`; used targeted validation instead
- Next: expand T44/T45 as Hicks lands Application handlers and more API surface

## 2026-05-18 — Hicks Phase 1 domain core T01-T05
- Added Domain primitives (`Entity`, `AggregateRoot`, `ValueObject`, `Guard`) plus shared enums under `src/TechInventory.Domain/`
- Added `Currency` value object with ISO 4217 allowlist validation, `Household` with `DefaultCurrency`, `Device` with household-default currency creation flow, and `Brand`
- Verified `dotnet build -c Release` and `dotnet test -c Release --no-build` passed after the Domain changes; fixed a pre-existing whitespace issue in `src/TechInventory.Api/Program.cs` so `dotnet format --verify-no-changes` can pass again

## 2026-05-18 — Vasquez auth token storage lint gate
- Added a custom flat-config ESLint rule in `src/TechInventory.Web/eslint.config.js` to block token-like `localStorage.setItem/getItem/removeItem` keys and to forbid any `localStorage` use inside `src/lib/auth/` and `src/lib/api/`
- Locked MSAL cache policy to `BrowserCacheLocation.SessionStorage` in `src/lib/auth/msal.ts`
- Verified the rule fired with temporary lint fixtures, then removed them and reran `pnpm run lint` successfully
- `pnpm run check` still fails on the pre-existing unused `@ts-expect-error` in `src/TechInventory.Web/vite.config.ts`

## YYYY-MM-DD — Bootstrap
- Created project structure
- Populated constitution, PRD, backlog README
- Next: run `/constitution` to validate

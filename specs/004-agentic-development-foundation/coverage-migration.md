---
id: 004-agentic-development-foundation
document: coverage-migration
task: T103
tier: T2
status: COMPLETE — evidence recorded (T103 matrix); §12 records T102 execution complete; §13 T101 revision record added (Apone, 2026-09-02)
produced_by: Apone (Tester / QA)
produced_at: 2026-09-02
updated_at: 2026-09-02 (§13.9 T101 revision record added — Apone, closing Ripley's B1/B2 blockers)
requested_by: briandenicola
base_sha: d303cd6537392e2489222d5a0d5c946f39f2af0c
supersedes: e2e-classification.md (deleted — carried no authority, `validation.md` §3.7)
---

# Coverage Migration Matrix and Deletion Map — T103 / AC-007

This document is the **deletion authority for T101** and the **work list for
T102**. It is analysis only: it deletes nothing, authors nothing, and records no
gate.

**Governing decision.** Playwright is retired (`brief.md` §2.1, approved by
`briandenicola` 2026-09-02). Nothing below proposes a merge-blocking, scheduled,
release, or optional automated browser suite. There is **no future automated
Playwright role**, and no row recommends preserving automated Playwright
execution in any form.

**Reset.** A prior stopped run produced `e2e-classification.md` against the
*pre-retirement* brief. That file has been **deleted**; none of its conclusions,
identifiers, or recommendations is carried forward. Every fact below was
re-derived from the working tree at `d303cd6` on 2026-09-02.

**Rule applied to every row.** A test is removed only if this document names
either (a) the layer + file where its behaviour lives or will live, or (b) an
explicitly accepted gap with a named owner and cadence. No spec is deleted
without a row.

---

## 1. Method and Evidence Base

| Step | What was done | Result |
| --- | --- | --- |
| File inventory | `git ls-files tests/e2e` + recursive listing | **28 tracked files**; plus untracked/ignored `node_modules/` (221 files) and `playwright-report/index.html` |
| Test counting | Static regex count per file, then reconciled against a real `--list` | See §2.2 — static and runtime counts agree |
| Collection attempt | `npx playwright test --list` in `tests/e2e` | **exit 1**, `Total: 0 tests in 0 files`, **4 × `TypeError: test.todo is not a function`** |
| Partial collection | `npx playwright test --list "journeys/(01\|02\|03\|04\|05\|06\|07\|08\|13\|14\|15)" "security/"` | **`Total: 360 tests in 12 files`** (60 per project × 6 projects) |
| `testMatch` probe | `npx playwright test --list --project=chromium-desktop theme-fouc` | **`Total: 0 tests in 0 files`** — resolves U-08 |
| Replacement survey | Enumerated the surviving suites | Vitest **68 files / 558 `it`**, 37 of them axe-checked; .NET integration **38 files / 198 `[Fact]`/`[Theory]`**; .NET unit **39 files / 263** |
| Reference sweep | Recursive case-insensitive search for `playwright` / `tests/e2e` / `\be2e\b` | **110 files** outside `node_modules`, `bin/`, `obj/` — classified in §5 |
| Contract cross-check | Compared `tests/e2e/fixtures/api.ts` to `CreateDeviceRequest` | `seedDevice()` omits required `OwnerId` **and** `LocationId` — drift confirmed |
| Theme cross-check | Compared `theme-fouc.spec.ts` to `src/app.html` | Spec seeds `ti.userPrefs.v1.*`; the pre-hydration script reads `theme-preference` — **second drift instance** |

Installed harness versions: `@playwright/test` **1.60.0**, `playwright-core`
**1.60.0**, `@axe-core/playwright` **4.11.3**, `axe-core` **4.11.4**
(`tests/e2e/package-lock.json`). The suite uses **npm**, not the repo's pnpm.

---

## 2. Static Inventory

### 2.1 Every Playwright artefact

**Spec files — 17** (15 `journeys/`, 1 `security/`, 1 at `tests/e2e` root).

**Support files — 11 tracked:**

| Path | Lines | Role |
| --- | --- | --- |
| `tests/e2e/playwright.config.ts` | 78 | 6 projects (Chromium/WebKit/Firefox × desktop/mobile); `testMatch` = `journeys/**`, `security/**` only |
| `tests/e2e/package.json` | 20 | `@playwright/test`, `@axe-core/playwright` (npm, separate from pnpm workspace) |
| `tests/e2e/package-lock.json` | 103 | npm lock for the above |
| `tests/e2e/README.md` | 89 | Declares "Playwright is the only E2E framework — constitution-level rule" and the 13-journey list |
| `tests/e2e/fixtures/api.ts` | 101 | `seedBrand` / `seedCategory` / `seedDevice` — **`seedDevice` is contract-drifted** |
| `tests/e2e/fixtures/auth.ts` | 140 | F025 local-account login; injects `ti_local_token` / `ti_local_meta`; overrides `request` |
| `tests/e2e/fixtures/axe.ts` | 19 | `runAxe(page)` via `AxeBuilder` |
| `tests/e2e/fixtures/network.ts` | 33 | Non-localhost request abort — **imported by zero specs** |
| `tests/e2e/pages/AppShellPage.ts` | ~180 | F045 page object + `matchMedia('(display-mode: standalone)')` override |
| `tests/e2e/pages/README.md` | 33 | POM convention; lists 5 page objects, **only 1 exists** |
| `tests/e2e/security/storage-inspection.ts` | 16 | `inspectStorage(page)` + `tokenKeyPattern` |

**Untracked/ignored:** `tests/e2e/node_modules/` (221 files, incl. browser
launcher scripts), `tests/e2e/playwright-report/index.html`.

**Outside `tests/e2e/` — harness plumbing:** `scripts/run-e2e.ps1`,
`scripts/run-e2e.sh`, `scripts/verify.ps1` step 9/9, `scripts/verify.sh` step
9/9, `Taskfile.yml` targets `test:e2e:run` / `test:e2e` / `test`,
`.github/workflows/ci.yml` (dispatch-only), `docker-compose.e2e.yml`,
`.env.e2e`, `.gitignore` (`playwright-report/`, `test-results/` — **written
twice**), `.dockerignore`.

### 2.2 Test counts per spec — static count reconciled with `--list`

| # | Spec | `test(` | `test.todo` | `test.fixme` | Collected (1 project) | × 6 projects |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 01 | `journeys/01-sign-in.spec.ts` | 2 | 0 | 0 | **2** | 12 |
| 02 | `journeys/02-sign-in-denied.spec.ts` | 2 | 0 | 1 | **3** | 18 |
| 03 | `journeys/03-create-device.spec.ts` | 3 | 0 | 0 | **3** | 18 |
| 04 | `journeys/04-edit-device.spec.ts` | 3 | 0 | 0 | **3** | 18 |
| 05 | `journeys/05-delete-device.spec.ts` | 3 | 0 | 0 | **3** | 18 |
| 06 | `journeys/06-browse-filter.spec.ts` | 4 | 0 | 0 | **4** | 24 |
| 07 | `journeys/07-detail-view.spec.ts` | 4 | 0 | 0 | **4** | 24 |
| 08 | `journeys/08-import-csv.spec.ts` | 2 | 0 | 0 | **2** | 12 |
| 09 | `journeys/09-export-csv.spec.ts` | 0 | **4** | 0 | **crashes collection** | 0 |
| 10 | `journeys/10-reference-data-admin.spec.ts` | 0 | **5** | 0 | **crashes collection** | 0 |
| 11 | `journeys/11-role-enforcement.spec.ts` | 0 | **5** | 0 | **crashes collection** | 0 |
| 12 | `journeys/12-offline-app-shell.spec.ts` | 0 | **5** | 0 | **crashes collection** | 0 |
| 13 | `journeys/13-a11y-smoke.spec.ts` | 1 (loop × 11 routes) | 0 | 0 | **11** | 66 |
| 14 | `journeys/14-import-model-display.spec.ts` | 2 | 0 | 0 | **2** | 12 |
| 15 | `journeys/15-pwa-shell.spec.ts` | 22 | 0 | 0 | **22** | 132 |
| S1 | `security/token-storage.spec.ts` | 1 | 0 | 0 | **1** | 6 |
| T1 | `theme-fouc.spec.ts` | 3 | 0 | 0 | **0 — outside `testMatch`** | 0 |
| | **Total** | **52** | **19** | **1** | **60 / project** | **360** |

**Reconciliation.** With 09–12 excluded, `--list` reports exactly
`Total: 360 tests in 12 files`. With them included, `--list` reports
`Total: 0 tests in 0 files` and exits **1**. The 19 `test.todo` declarations are
therefore not "pending tests" — they are **four module-load crashes** that make
the *entire* suite uncollectable, including the 60 that would otherwise run.

**Executed, ever, in CI: 0.** `.github/workflows/ci.yml` is `workflow_dispatch`
only; Quality Gate never invokes Playwright.

### 2.3 Two independent contract drifts prove the harness was unverified

1. **`seedDevice()`** posts `{name, brandId, categoryId, serialNumber, notes,
   model, status}` while `CreateDeviceRequest` requires non-nullable `OwnerId`
   **and** `LocationId`. Journeys **04, 05, 07, 15** depend on it; all would
   fail at seed time even after the collection crash is fixed.
2. **`theme-fouc.spec.ts`** seeds `localStorage['ti.userPrefs.v1.<id>']` with
   `{themePreference:'dark'}`, but `src/app.html`'s pre-hydration script reads
   `localStorage['theme-preference']` (also the key `theme.svelte.ts` writes).
   The assertion would only pass by accident on a dark-preferring OS.

Neither drift was caught, because nothing ever ran. This is the reason T102's
replacements must make drift a **compile or contract error**, not a runtime
assertion.

---

## 3. Coverage Migration Matrix

Legend — **Destination layer:** `HTTP` = real HTTP integration/contract test
(real app + real SQLite, no mocked API) · `COMP` = Vitest + Testing Library
(+ axe-core) component/unit test · `MAN` = manual validation checklist item
(§7) · `DROP` = removed with no replacement, because it asserts no product
contract · `GAP` = accepted gap with a named owner (§8).

Work-item IDs: `H-nn` = backend test to author in T102 · `C-nn` = frontend test
to author in T102 · `M-nn` = manual checklist item · `G-nn` = accepted gap.

### 3.1 Journey 01 — Sign in · 2 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| unauthenticated request to `/devices` redirects to `/auth/login` | Route-group guard bounces an unauthenticated visitor after auth bootstrap | **Yes** — real authz affordance | `COMP` | `src/lib/auth/index.test.ts` (5), `src/routes/auth/login/page.test.ts`, `src/lib/stores/auth.test.ts` (partial) | **C-01** — direct test of `(authenticated)/+layout.ts` guard: unauthenticated + `isLoading=false` ⇒ redirect to `/auth/login`; authenticated ⇒ no redirect |
| root route renders without a `pageerror` | "Nothing throws on boot" | **No** — no product contract; a compile/build smoke wearing a browser costume | `DROP` | `pnpm run build` + `pnpm run check` + 68 Vitest files already render these components | — |

**PRD §7.5.4 #1 residue.** The real **Entra ID** redirect sign-in/sign-out was
*never* covered by this spec — the file's own header records that the E2E stack
uses the F025 local-account path instead. That residue is **`MAN` → M-11**, not
a regression introduced by retirement.

### 3.2 Journey 02 — Sign in denied / sign out · 2 tests + 1 `fixme`

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| user pill dropdown exposes a Sign Out action | Menu exposes an enabled, correctly named Sign Out `menuitem` | **Yes** | `COMP` | **Already covered** — `AppMenuPopover.test.ts` *"calls onSignOut and closes the menu when Sign Out is activated"*, *"renders items for an Admin in the exact order…"* | none |
| dropdown is keyboard-reachable and Escape closes it | Enter opens, Escape closes, focus returns | **Yes** | `COMP` | **Already covered** — `AppMenuPopover.test.ts` *"opens the panel … focusing the first item"*, *"closes on Escape and returns focus to the trigger"*, *"supports ArrowDown/ArrowUp/Home/End roving focus"* | none |
| `test.fixme` — Sign Out clears session and redirects | `clearAuth()` drops MSAL + `ti_local_*` sessionStorage keys and the next navigation bounces to `/auth/login` | **Yes** — never ran at any layer | `COMP` | none | **C-02** — assert `clearAuth()` clears both stores and the guard then redirects |
| *(spec header)* role-less user is denied gracefully — **PRD #2, never implemented** | 401/403 for a token with no role, and a helpful UI message | **Yes** | `HTTP` + `COMP` | **API already covered** — `AuthIntegrationTests.TokenWithNoRoles_Returns401Unauthorized`, `ViewerRoleAuthorizationTests` (Viewer-role 403 coverage; see §12.1 H-04/B1 correction — the `.ViewerRoleOnAdminEndpoint_Returns403Forbidden` test this row previously cited was a permanently-`Skip`-attributed stub that never executed and has been removed) | **C-03** — login/error surface renders the denial message for a role-less session |

### 3.3 Journey 03 — Create device · 3 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| opens the Add Device modal from the list CTA | CTA opens a labelled `dialog` | **Yes** | `COMP` | **Already covered** — `AddDeviceModal.test.ts`, `DeviceListAddActions.test.ts`, `AddDeviceFab.test.ts` | none |
| modal closes on Escape, X, and Cancel | Three dismissal affordances | **Yes** | `COMP` | **Already covered** — `AddDeviceModal.test.ts` (+ axe) | none |
| Admin creates a device end-to-end and sees it in the list | Form → `POST /api/v1/devices` → list reflects it | **Yes** | `HTTP` + `COMP` | `DevicesControllerTests.CreateDevice_WhenValid_Returns201WithLocation`; `DeviceForm.test.ts`; `queries/devices.test.ts`; `DeviceTable.test.ts` | **H-01** — round-trip: create then `GET /devices?search=` returns the new row (closes the seed-drift risk with a typed builder) |
| — | Cross-layer "modal save then list repaint" in a real browser | Residual | `GAP` **G-01** | — | covered by **M-12** spot check |

### 3.4 Journey 04 — Edit device · 3 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| navigates to the edit form from the detail page | Detail page exposes an `Edit` link to `/devices/{id}/edit` | **Yes** | `COMP` | partial — `DeviceDetailFields.test.ts` (3 tests, fields only); the detail route itself has **no test** | **C-04** — detail page action affordances (Edit link target, Delete button, Claim/Release) |
| rename persists and shows in the list | `PUT /api/v1/devices/{id}` persists; list reflects | **Yes** | `HTTP` | **Already covered** — `DevicesControllerTests.UpdateDevice_WhenValid_ReturnsUpdatedDevice` | none |
| Cancel returns to detail without saving | Cancel discards the edit | **Yes** | `COMP` | none — `UnsavedChangesModal.svelte` has **no test** | **C-05** — Cancel on `DeviceForm` emits no mutation and navigates back |

### 3.5 Journey 05 — Delete device · 3 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| Delete is gated on type-to-confirm **and** reason ≥ 10 chars (constitution §4.4) | Confirm stays disabled until both satisfied | **Yes** — a constitutional rule | `COMP` | **Already covered** — `DeleteDeviceModal.test.ts` (+ axe) | none |
| Cancel leaves the device untouched | No mutation on cancel | **Yes** | `COMP` | **Already covered** — `DeleteDeviceModal.test.ts` | none |
| Deleted device disappears from the active list (soft delete) | `DELETE` marks inactive; default `status=Active` list hides it | **Yes** | `HTTP` | `DevicesControllerTests.DeleteDevice_WhenFound_Returns204`, `.DeleteDevice_WhenMissing_Returns404ProblemDetails`, `Repositories/DeviceVisibilityRegressionTests` | **H-02** — assert `DELETE /devices/{id}` with a reason writes an `AuditEvent` carrying that reason (bulk delete has this via `BulkDeleteDevices_WhenValid_…AuditsEach`; **single delete does not**) |

### 3.6 Journey 06 — Browse and filter · 4 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| search box writes `?search=` | Debounced filter → URL | **Yes** | `COMP` | **Already covered** — `utils/deviceFilterUrl.test.ts` (8 tests, full round-trip rules) | none |
| status filter writes `?status=Active` | Checkbox → URL | **Yes** | `COMP` | **Already covered** — `deviceFilterUrl.test.ts`, `DeviceFilters.test.ts` | none |
| Clear All resets every filter param | Reset clears all query params | **Yes** | `COMP` | partial — `DeviceFilters.test.ts` | **C-06** — Clear All produces a URL with no `search` / `status` / `brandId` / `page` |
| reload preserves filter state from the URL | URL is the source of truth after reload | **Yes** | `COMP` | **Already covered** — `stores/viewState.test.ts` (11 tests incl. *"navigation state persists across page loads in same session"*, *"preserves complex filter state including sort and pagination"*) | none |

### 3.7 Journey 07 — Detail view · 4 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| detail page renders breadcrumbs back to the list | Breadcrumb `navigation` landmark with Home / Devices links + `aria-current` crumb | **Yes** — a11y landmark contract | `COMP` | none — **no breadcrumb component or test exists**; markup is inline in the route | **C-07** — breadcrumb landmark, link targets, `aria-current="page"`, axe-clean |
| Admin sees Edit and Delete buttons | Role-gated actions render for Admin | **Yes** | `COMP` | partial — `DeviceActionsMenu.test.ts`, `DevicePwaRow.test.ts` *"does not render the actions ellipsis when the current user has no permitted actions"* | folded into **C-04** |
| Admin sees Claim when the device has no owner | Ownership affordance is conditional | **Yes** | `COMP` | **Already covered** — `ClaimOwnershipModal.test.ts`, `ReleaseOwnershipModal.test.ts` | none |
| breadcrumb link returns to `/devices` | Navigation works | **Yes** | `COMP` | none | folded into **C-07** |
| *(PRD #7)* reference data resolved to human-readable labels | FK ids render as labels | **Yes** | `HTTP` + `COMP` | `DevicesControllerTests.GetDeviceById_WhenFound_ReturnsDevice`; `DeviceDetailFields.test.ts` | folded into **C-08** (§3.14) |

### 3.8 Journey 08 — Import CSV · 2 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| wizard chrome: 3-step indicator, drop zone, `accept=csv` | Import UI structure | **Yes** | `COMP` | none — `admin/import/+page.svelte` has **no test** | **C-09** — step list has 3 items, file input accepts CSV, is labelled, axe-clean |
| upload → preview (`total rows`) → commit → device in list | Full import pipeline | **Yes** | `HTTP` | **Already covered** — `ImportsControllerTests` (10 tests: preview valid/malformed/missing-lookups/413, commit persists + audits, auto-creates lookups, batch listing) | none |
| — | Real browser file-picker mechanics (`<input type=file>` + drag-drop) | Residual | `GAP` **G-02** | jsdom cannot exercise a real file picker | covered by **M-12** |

### 3.9 Journey 09 — Export CSV · **stub**, 4 × `test.todo`, 0 executable

| `test.todo` | Behaviour intended | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| Admin can initiate CSV export from the device list | Export CTA exists and is reachable | **Yes** | `COMP` | partial — `admin/export/InsuranceExportCard.test.ts` | **C-10** — export page CTAs render, are labelled, axe-clean |
| Export respects the current filter state | Filter → export parity | **Yes** | `HTTP` | **Already covered** — `ExportControllerTests.ExportDevices_WhenFilteredByActiveStatus_ReturnsOnlyActiveRows` | none |
| Downloaded CSV is valid and parses cleanly | Content type, attachment, parseable rows | **Yes** | `HTTP` | **Already covered** — `.ExportDevicesAsCsv_WhenRequested_ReturnsAttachmentWithAllRows`, `.ExportDevicesAsJson_…`, `.ExportDevicesAsCsv_WhenNameStartsWithFormulaTrigger_IsNeutralizedWithLeadingQuote`, `.ExportDevices_WhenLargeDatasetRequested_ReturnsStreamedSuccessfulResponse` | none |
| Exported data matches the filtered view | Same predicate on screen and in the file | **Yes** | `HTTP` | as above | **H-03** — assert the export query string and the list query string resolve to the same row set |
| — | Browser download completes and opens in a spreadsheet | Residual | `GAP` **G-03** | — | covered by **M-13** |

**Verdict:** this stub asserted nothing and blocked everything. Its intent is
**already 80% live** in `ExportControllerTests`. Deleting it loses no coverage.

### 3.10 Journey 10 — Reference data admin · **stub**, 5 × `test.todo`, 0 executable

| `test.todo` | Behaviour intended | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| navigate to reference-data management | Admin routes reachable | **Yes** | `COMP` | **Already covered** — `admin/admin-page.test.ts`, `admin/ResponsiveAdminList.test.ts`, `appNav.render.test.ts` | none |
| create a new Location | `POST /api/v1/locations` | **Yes** | `HTTP` | **Already covered** — `LocationsControllerTests` (+ Brands/Categories/Networks/Owners/Tags controller tests) | none |
| new Location appears in the device form dropdown immediately | Reference cache invalidation after mutation | **Yes** — a real cross-cutting bug class | `COMP` | none | **C-11** — creating a reference entity invalidates the reference query so `DeviceForm` re-renders the new option |
| edit existing reference data | `PUT` round-trip | **Yes** | `HTTP` | **Already covered** — reference controller tests + `admin/*/schema.test.ts` (×4) | none |
| delete unused reference data | `DELETE` + in-use conflict | **Yes** | `HTTP` | **Already covered** — reference controller tests, `ReferenceBulkDeleteAuthorizationTests`, `BulkDeleteReferenceModal.test.ts`, `MergeEntityModal.test.ts` | none |

### 3.11 Journey 11 — Role enforcement · **stub**, 5 × `test.todo`, 0 executable

| `test.todo` | Behaviour intended | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| Viewer does not see the edit affordance | Role-gated UI | **Yes** | `COMP` | **Already covered** — `AppBottomNav.test.ts` *"hides the Add action for Viewer role"*, `AppMenuPopover.test.ts` *"hides Admin-only and Member-only items for a Viewer"*, `DevicePwaRow.test.ts` | none |
| Viewer does not see the delete affordance | Role-gated UI | **Yes** | `COMP` | as above + `DeviceActionsMenu.test.ts` | **C-12** — `DeviceTable` / detail actions hidden for Viewer (the two surfaces not yet asserted) |
| direct navigation to an edit route is refused for a Viewer | Client route guard is not the only gate | **Yes** | `COMP` + `HTTP` | partial | **C-13** — `/devices/{id}/edit` guard refuses a Viewer session |
| API returns 403 for unauthorized actions | Server is the real gate | **Yes** | `HTTP` | partial at time of analysis — `AuditEventsAuthorizationTests`, `SettingsAuthorizationTests`, `ReferenceMergeAuthorizationTests`, `ReferenceBulkDeleteAuthorizationTests` (all **Member**-role); the sole Viewer-role citation this row previously relied on, `AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden`, carried a permanent `Skip` attribute and never executed — it has since been removed (§12.1 H-04/B1 correction) | **H-04** — add a `ViewerRoleIntegrationTestFactory` (mirroring `MemberRoleIntegrationTestFactory`) and assert **403 for Viewer** on device create/update/delete, import commit, and reference mutation. **Delivered as `ViewerRoleAuthorizationTests`**, further expanded under B1 to also cover ordinary Brands/Categories/Locations/Networks/Tags/Owners create/update/delete (previously bare `[Authorize]`, silently allowing Viewer mutation) |
| Member can use edit/delete affordances | Positive role case | **Yes** | `COMP` + `HTTP` | partial — `AppBottomNav.test.ts` *"shows the Add action for Admin and Member roles"*, `SettingsAuthorizationTests.GetDisplaySettings_WhenCallerIsMember_ReturnsOk` | folded into **H-04** |

**Note.** `docs/threat-model.md` (V4.1.2) and `docs/security-baseline.md` §
cite "Playwright test #11" as the access-control policy test. **That test never
existed as executable code.** T101 must revise those two citations to point at
`H-04` — this is a documentation correction, not a coverage loss.

### 3.12 Journey 12 — Offline app shell · **stub**, 5 × `test.todo`, 0 executable

| `test.todo` | Behaviour intended | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| PWA shell loads when the API is offline | `navigateFallback: '/offline'` serves the shell | **Yes** | `COMP` + `MAN` | none — `src/routes/offline/+page.svelte` has **no test** | **C-14** — `/offline` route renders its message, is axe-clean; **C-15** — assert the workbox config contract in `vite.config.ts` (`navigateFallback='/offline'`, denylist `^/api/`, `^/health`, `^/openapi`) · residual real-SW behaviour → **M-06** |
| cached device data is viewable offline | `StaleWhileRevalidate` on `GET /api/v1/*` | **Yes** | `COMP` + `MAN` | none | folded into **C-15** (cache name, `maxEntries`, `maxAgeSeconds`, `statuses:[0,200]`) · real behaviour → **M-07** |
| create/edit/delete are queued or refused gracefully offline | POST/PUT/DELETE are `NetworkOnly` — i.e. **refused, never queued** | **Yes**, and the stub's wording is wrong | `COMP` + `MAN` | none | folded into **C-15** (all three methods `NetworkOnly`) + **C-16** — mutation failure surfaces an error toast, no silent success · real behaviour → **M-08** |
| user sees a clear offline indication | Offline UX affordance | **Yes** | `COMP` | none | folded into **C-14** |
| queued actions are processed when the connection returns | **Aspirational — the product has no queue.** `NetworkOnly` means there is nothing to replay | **No** | `DROP` | — | — |
| — | Real service-worker registration, install prompt, update prompt | Residual, browser-only | `GAP` **G-04** | `PwaUpdatePrompt.svelte` has **no test** | **C-17** (component render/dismiss only) + **M-05**, **M-09** |

### 3.13 Journey 13 — Accessibility smoke · 11 collected cases

One parameterized test over 11 routes: `/devices`, `/admin/audit`,
`/admin/brands`, `/admin/categories`, `/admin/owners`, `/admin/locations`,
`/admin/networks`, `/admin/tags`, `/admin/import`, `/admin/export`, `/offline`.

| Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- |
| Every critical route passes axe-core with **zero** violations (constitution §6.5.7) | **Yes** — a constitutional gate | `COMP` | **Substantially covered** — **37 of 68** Vitest files already assert zero axe violations, incl. every list/modal/nav component on those routes; `appNav.render.test.ts` covers the admin nav shell | **C-18** — route-level axe harnesses (following the existing `AppNavMenuHarness.svelte` precedent) for the pages not yet composed in a test: `/devices`, `/devices/{id}`, `/admin/import`, `/admin/export`, `/admin/audit`, `/offline` |
| Engine-specific a11y differences (WebKit/Firefox AOM) | Residual | `GAP` **G-05** | axe-core in jsdom is engine-independent | covered by **M-14**, **M-15** |

**This is the only journey whose deletion measurably reduces automated
coverage** — from "11 whole-route scans that never ran" to "37 component scans
that run on every push, plus 6 new route harnesses". Net coverage **increases**;
what is genuinely lost is *composed-page* scanning in a real engine, recorded as
**G-05**.

### 3.14 Journey 14 — F034 imported fields display · 2 tests

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| synthetic CSV: preview table exposes Model + Purpose **before** commit; detail page renders Model, Purpose, Notes, OS, IP, MAC, Product URL, Version | **Yes** — this is the exact regression F034 was raised for ("every device looks identical") | `HTTP` + `COMP` | partial — `SharePointCsvImportTests.CommitImport_SharePointCsv_ProcessesAllStatusMappingsAndExtendedFields` covers persistence; `DeviceDetailFields.test.ts` has **3 tests and asserts none of these fields** | **C-08** — `DeviceDetailFields` renders all eight F034 fields with labels, and elides empties · **C-19** — import preview row expansion shows Model/Purpose per row |
| canonical `data/Devices.csv`: a known row's Model + Purpose render | **Yes** — real-data regression guard | `HTTP` | partial — `SharePointCsvImportTests` uses its own fixture, not `data/Devices.csv` | **H-05** — commit `data/Devices.csv` through the import endpoint and assert the "Mohu Leaf Stitch" row persists `Model="Leaf Stitch"`, `Purpose="Master TV"` |

### 3.15 Journey 15 — PWA app shell (F045) · 22 tests

The largest spec, authored **ahead of** the implementation and never executed.
Vasquez's implementation subsequently landed **with its own Vitest coverage**, so
most of these assertions are already live at the component layer.

| Test group (count) | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| Mode gating is standalone-only, not viewport-only (3) | `display-mode: standalone` ⇒ bottom nav; mobile-web and desktop ⇒ no bottom nav | **Yes** — D-175's core invariant | `COMP` | **Already covered** — `stores/displayMode.svelte.test.ts` (8 tests: standalone regardless of width, desktop/mobile resolution, SSR default, live media-query changes, listener teardown) | none · real standalone mode → **M-03**, **M-04** |
| Bottom-nav structure and a11y (6) | Home/Add/Reports in one `role="group"` pill, Settings outside it, labels, `aria-current`, 44 × 44 hit target, axe-clean | **Yes** | `COMP` | **Already covered** — `AppBottomNav.test.ts` (11 tests, incl. the `role="group"` membership assertion, the ≥44px hit-area assertion, `aria-current` per item, and axe) | none |
| Title-bar controls + search placement (3) | App mode keeps H1 + Filter, drops both Add affordances; search drops the `md:max-w-lg` cap in app mode but keeps it in desktop web | **Partly** — the affordance rules are contractual; the **measured pixel width** is CSS layout, which jsdom cannot compute | `COMP` + `GAP` **G-06** | partial — `DeviceListAddActions.test.ts`, `AddDeviceFab.test.ts` | **C-20** — in app mode the desktop CTA and FAB are absent and the nav Add is present · width measurement → **G-06** / **M-03** |
| Implicit category grouping default D-177 (2) | App mode groups by category without writing `groupBy` to the URL or counting as an active filter; `?groupBy=none` opts out | **Yes** | `COMP` | **Already covered** — `utils/deviceFilterUrl.test.ts` (7 of its 8 tests are exactly this), `utils/groupDevices.test.ts` | none |
| Two-line device row + ellipsis actions (1) | Name on line 1, brand·model on line 2, actions menu separately reachable, opening it does not open the detail modal | **Yes** | `COMP` | **Already covered** — `DevicePwaRow.test.ts` (11 tests, incl. line order, fallbacks, no nested interactives, real `<a href>`, actions menu reveal, axe) | none |
| Compact hamburger popover D-179 (3) | Admin option set + order, Enter/Escape focus contract, outside click closes, no full-screen dialog, no body scroll lock | **Yes** | `COMP` | **Already covered** — `AppMenuPopover.test.ts` (13 tests) and `AppMenuPopover.containing-block.test.ts` | none |
| Desktop browser regression (2) | No bottom toolbar on desktop; table renders; Table Columns preference hides a column | **Yes** | `COMP` | **Already covered** — `DeviceTable.test.ts`, `stores/tableColumns.test.ts`, `displayMode.svelte.test.ts` | none |
| App-mode a11y at iPhone viewport (2) | Zero axe violations in app mode; no primary control obscured by the fixed bottom nav | **Partly** — axe is covered; "obscured by the fixed nav" is **layout geometry** | `COMP` + `GAP` **G-06** | `AppBottomNav.test.ts` axe case | geometry → **G-06** / **M-03** |

**Net:** 19 of 22 assertions already exist at the component layer; 2 need
**C-20**; 3 pixel-geometry assertions become **G-06**, spot-checked by **M-03**.

### 3.16 `security/token-storage.spec.ts` · 1 test

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| a **mocked** login page keeps token-like keys out of `localStorage` and allows MSAL keys in `sessionStorage` | The test routes `http://localhost/mock-login`, serves its own HTML, writes its own keys, then asserts on the keys it just wrote. **It never exercises the application.** | **No, as written** — it is a tautology. The *policy* it stands for is valuable | `COMP` + `MAN` | **Already covered, and better** — `src/lib/auth/msal.test.ts` *"keeps tokens in sessionStorage"* asserts `msalConfig.cache.cacheLocation === BrowserCacheLocation.SessionStorage`; plus the ESLint rule in `eslint.config.js`, the `.githooks/pre-commit` scan, and gitleaks | none automated; real-browser proof of the live redirect flow → `GAP` **G-08** / **M-11** |

**Consequence for D-010's "four-gate" model.** Gate 3 ("Playwright asserts real
auth flows never persist tokens to `localStorage`") is retired. The remaining
gates are ESLint (ENFORCED at lint), pre-commit + gitleaks (ENFORCED at commit),
`msal.test.ts` (ENFORCED at unit), and code review (REVIEWED) — **four gates
remain**, with the ceremonial one replaced by a real one. `eslint.config.js`'s
header comment and `.squad/skills/token-storage-inspection/SKILL.md` must be
revised accordingly (§5).

### 3.17 `theme-fouc.spec.ts` · 3 tests · **collected: 0** — resolves U-08

**U-08 resolution.** The file sits at `tests/e2e/` root while `testMatch` is
`['journeys/**/*.spec.ts', 'security/**/*.spec.ts']`, so Playwright never
collects it — confirmed by `--list --project=chromium-desktop theme-fouc` ⇒
`Total: 0 tests in 0 files`. There is **no recorded reason** for the placement;
the file was added for F029 and never wired in. It is treated here as an
ordinary matrix row, exactly as `tasks.md` requires. **U-08 is closed.**

| Test | Behaviour asserted | Valuable? | Destination | Existing replacement | T102 work |
| --- | --- | --- | --- | --- | --- |
| sets `data-theme="dark"` before first paint | Pre-hydration script resolves the stored preference before paint | **Yes** — but **the test is drifted**: it seeds `ti.userPrefs.v1.<id>`, while `app.html` reads `theme-preference` | `COMP` | partial — `stores/theme.svelte.test.ts` (3 tests) covers preference resolution/persistence, **not** the inline `app.html` script | **C-21** — extract/assert the pre-hydration contract: key `theme-preference`, `dark` ⇒ `dataset.theme='dark'` + `.dark` class; `system` ⇒ follows `prefers-color-scheme` |
| body background is not white immediately after navigation | Same behaviour, weaker assertion | **Duplicate** | `DROP` | folded into **C-21** | — |
| "screenshot shows no light flash" — asserts only `expect(screenshot).toBeDefined()` | **Nothing.** The comment concedes visual regression "is not automated here" | **No — vacuous** | `DROP` | — | — |
| — | Absence of a real first-paint flash on a real engine | Residual | `GAP` **G-07** | — | covered by **M-10** |

---

## 4. Deletion Map — Order for T101

Deletion is safe in this order because each wave removes only artefacts whose
behaviour is already recorded above. **Wave 0 is the precondition: this
document.**

| Wave | Remove | Precondition | Why safe |
| --- | --- | --- | --- |
| **D1** | `tests/e2e/journeys/09,10,11,12` (4 stubs, 19 `test.todo`) | §3.9–§3.12 | Zero executable assertions; they are the *cause* of the collection crash |
| **D2** | `tests/e2e/theme-fouc.spec.ts` · `tests/e2e/fixtures/network.ts` | §3.17; `network.ts` imported by **zero** specs | Never collected / never imported |
| **D3** | Remaining 12 specs — `journeys/01–08,13,14,15`, `security/token-storage.spec.ts` | §3.1–§3.8, §3.13–§3.16 rows all closed | Every behaviour has a destination row |
| **D4** | Support files — `fixtures/api.ts`, `fixtures/auth.ts`, `fixtures/axe.ts`, `pages/AppShellPage.ts`, `pages/README.md`, `security/storage-inspection.ts`, `README.md` | D3 complete (nothing imports them) | `fixtures/api.ts` is **deleted, not repaired** (`brief.md` AC-006). `AppShellPage.ts`'s `matchMedia` override is superseded by `displayMode.svelte.test.ts`'s injected-standalone option |
| **D5** | Harness config + manifests — `playwright.config.ts`, `package.json`, `package-lock.json`, `node_modules/`, `playwright-report/` → the whole `tests/e2e/` tree | D4 complete | Nothing left to configure |
| **D6** | Invocations — `scripts/run-e2e.ps1`, `scripts/run-e2e.sh` (delete); `scripts/verify.ps1` / `verify.sh` step 9/9; `Taskfile.yml` `test:e2e:run`, `test:e2e`, and the `test:e2e:run` line inside `test`; `.github/workflows/ci.yml` | D5 complete | Nothing to invoke |
| **D7** | Promises + wording — see §5 | D6 complete | Docs must not outlive the thing they describe |
| **D8** | Add the stale-reference guard, then close **#89** citing retirement + migration, linking this document and the T102 replacements | D7 complete, T102 complete | AC-005 |

**Ordering constraint.** D6 must not precede D5: `task test` currently delegates
to `test:e2e:run`, and removing the target while specs remain would leave a
green-looking `task test` that silently skips a suite — the exact failure this
package exists to end.

**Do not delete in T101** (decide in T104 instead): `docker-compose.e2e.yml`,
`.env.e2e`, and the `.gitleaks.toml` allowlist entry for `.env.e2e`. These
describe a hermetic containerized stack that is useful for real-HTTP verification
independent of any browser. They must be **decoupled and renamed** away from
"e2e", not deleted alongside the harness, or T104 loses a working stack it may
want. If T104 decides against them, they are removed there with their own row.

---

## 5. Repository Reference Classification

**110 files** outside `node_modules/`, `bin/`, `obj/` match
`playwright` / `tests/e2e` / `\be2e\b`.

### 5.1 DELETE in T101

| Path | Refs | Note |
| --- | ---: | --- |
| `tests/e2e/**` (28 tracked files) | all | The harness |
| `scripts/run-e2e.ps1` | 9 | Exists only to run Playwright |
| `scripts/run-e2e.sh` | 6 | Same |
| `.squad/skills/playwright-e2e-scaffolding/SKILL.md` | 25 | A reusable recipe for standing the harness back up — directly contradicts `brief.md` §2.1 |

### 5.2 REVISE in T101

| Path | Refs | Required change |
| --- | ---: | --- |
| `Taskfile.yml` | 8 | Delete `test:e2e:run` + `test:e2e`; drop the `test:e2e:run` step from `test` |
| `scripts/verify.ps1` | 2 | Delete step 9/9; renumber |
| `scripts/verify.sh` | 2 | Delete step 9/9; renumber |
| `.github/workflows/ci.yml` | 1 | The mute comment names the Playwright/Docker step as the reason for disablement; T104 decides whether the workflow survives at all |
| `.github/workflows/README.md` | 8 | Remove the E2E stage, the "E2E smoke / Playwright / Enforced" table row, and the E2E troubleshooting section |
| `.github/T47-CI-SETUP-CHECKLIST.md` | 8 | Same; `tasks.md` T105 already requires this file be executed or archived |
| `.github/copilot-instructions.md` | 6 | Remove the `npx playwright` commands and the "Playwright is the only E2E framework" convention |
| `docs/testing.md` | 31 | **Largest revision.** Also contains three claims that are false *today*: `task test:a11y` (**no such task**), spec files at `tests/e2e/specs/*.spec.ts` (**wrong path**), and `tests/e2e/a11y` (**does not exist**) |
| `docs/security-baseline.md` | 3 | Repoint the token-storage test and "Playwright test #11" citations at `msal.test.ts` and **H-04** |
| `docs/threat-model.md` | 3 | Repoint V4.1.2 and the offline-cache claim at **H-04** and **M-06…M-09** |
| `docs/auth-design.md` | 1 | Remove the "Playwright tests (4 critical journeys)" line |
| `docs/known-issues.md` | 5 | "Deferred to E2E (Round 9)" is now deferred to nothing — resolve or reclassify as an accepted gap |
| `README.md` | 3 | Remove `task test:e2e` from the command table |
| `.gitignore` | 2 (×2 duplicated) | Remove `playwright-report/` / `test-results/` — and the duplication |
| `.dockerignore` | 1 | Remove `playwright-report/` |
| `src/TechInventory.Web/eslint.config.js` | 1 | Gate 3 of the four-gate comment must name `msal.test.ts`, not Playwright (§3.16) |
| `.squad/skills/token-storage-inspection/SKILL.md` | 1 | Rewrite around the unit-level assertion, or delete |
| `.squad/agents/apone/charter.md` | 6 | "Playwright is the only E2E framework — constitution-level" is now false |
| `.squad/agents/vasquez/charter.md` | 1 | Remove the Playwright ownership boundary |
| `.squad/routing.md` | 1 | Remove Playwright from Apone's routing line |
| `.squad/team.md` | 1 | Remove Playwright from the stack list |
| `.squad/templates/machine-capabilities.md` | 1 | `needs:browser` capability no longer has a consumer |
| **8 Vitest deferral comments** — `admin/{brands,locations,networks,tags}/schema.test.ts:5`, `ClaimOwnershipModal.test.ts:181`, `ReleaseOwnershipModal.test.ts:174`, `DeleteDeviceModal.test.ts:310`, `DeviceForm.test.ts:439` | 8 | **Orphaned promises.** Each defers coverage to a suite that will not exist. Each must become a real test or a named accepted gap in T102 — tracked as **C-22** |

### 5.3 REVISE in T104 (verification surface) — not T101

`docker-compose.e2e.yml` (14), `.env.e2e`, `.gitleaks.toml` (3),
`docker-compose.yml` (1), `.env.example` (1) — see §4's "do not delete" note.
The two `src/` server-side matches
(`LocalAdminSeedHostedService.cs`, `HmacJwtLocalTokenIssuer.cs`) are **F025
break-glass comments that say "dev/E2E only"** — the security constraint is real
and independent of Playwright; reword "E2E" to "local/containerized test
stacks", nothing more.

### 5.4 REVISE as an ADR candidate — **not** an authorization

| Path | Refs | Why it is not a T101 edit |
| --- | ---: | --- |
| `.specify/memory/constitution.md` §6.5.7 / §7 / DoD | 9 | Highest-authority document (§0). It states "**Playwright** is the required E2E framework — no substitutes" and "E2E run twice locally without failure". Retirement contradicts a normative constitutional clause and needs an ADR, per `plan.md` §6 |
| `docs/prd.md` §7.5.2 / §7.5.3 / §7.5.4 | 14 | §7.5.4 says **"Removing a journey from this list requires an ADR"**. §8 below preserves the traceability; the amendment itself is **surfaced, not performed** |

### 5.5 RETAIN as historical evidence only — **no edit** (narrowed by Apone, T101 revision)

> **Reviewer correction resolved (Ripley, 2026-09-02, `validation.md` §7 B2;
> closed by Apone in the T101 revision cycle).** The `specs/_backlog/**`
> entry previously in this list was **misclassified**. Constitution §0 ranks
> `specs/_backlog/F0XX-*.md` as authority source #6 — a forward-looking
> definition of done, not a historical record. A repository-wide
> case-insensitive audit found **16 backlog files** (not merely the ten
> Ripley sampled) carrying a Playwright reference — some unchecked
> acceptance-criteria promises, some narrative mentions of a now-deleted
> journey, one a "carve-out" note: `F020`, `F020b`, `F021`, `F021b`, `F022`,
> `F023`, `F024`, `F024b`, `F026`, `F027`, `F028`, `F029`, `F030`, `F031`,
> `F034`, `F045`. Every reference in all 16 files has been rewritten to name
> its real post-retirement destination — Vitest component test, backend HTTP
> integration test, `docs/testing/manual-pwa-validation.md` manual check, or
> a declared accepted gap (mirroring **G-09**'s pattern) — never left as an
> unautomatable promise and never silently dropped. Where the described
> behaviour was already delivered by an existing T102 replacement (e.g.
> `F034`'s SharePoint-CSV smoke test → **H-05**/**C-19**; `F021`'s
> `/admin/audit` 403 → `AuditEventsAuthorizationTests`/`ViewerRoleAuthorizationTests`;
> `F024`'s bulk-update walkthrough →
> `DevicesControllerTests.BulkUpdateDevices_WhenValid_…`), the checkbox was
> checked against that real, verified test rather than left open. **Zero**
> `playwright` references remain anywhere under `specs/_backlog/**` — verified
> by a repository-wide case-insensitive search.
>
> **`specs/_backlog/**` is reclassified `REVISE`, not `RETAIN`.** The guard's
> exemption is narrowed from a blanket `specs/` prefix to an explicit,
> named allowlist of the historical/work-package files below —
> `specs/_backlog/**` and any other unlisted `specs/**` path (present or
> future) is **not** exempt and fails the guard like any other repository
> path on a Playwright reference. See
> `scripts/check-stale-playwright-references.mjs`'s `EXEMPT_SPEC_PATHS`
> and its test file's "does NOT exempt specs/_backlog/**" /
> "does NOT exempt an arbitrary specs/ path" regression tests.

`specs/004-agentic-development-foundation/{brief,plan,tasks,validation,evidence}.md`
and this file; `specs/001-core-api/{plan,tasks}.md`,
`specs/002-frontend-mvp/{plan,spec,tasks}.md`, `specs/003-pwa-polish/tasks.md`;
`.squad/decisions.md` (25 refs),
`.squad/decisions/decisions.md`, `.squad/decisions/merged/*` (3),
`.squad/session-log.md`, `.squad/agents/*/history.md` (6 files);
`SESSION-NOTES.md` (23), `.copilot-state.md` (4).

**These are records of what was decided and done — history, not instruction
(`plan.md` §2.9).** Rewriting them would destroy the evidence trail for #89 and
PR #140. **The T101 stale-reference guard must therefore scope itself to
manifests, scripts, workflows, config and test trees — and explicitly exempt**
the named files above (**not** `specs/` as a whole)**, `.squad/decisions*`,
`.squad/agents/*/history.md`, `SESSION-NOTES.md` and `.copilot-state.md`.** A
guard that fails on history would make the honest record unmaintainable — but
a guard that exempts unbuilt backlog is the defect this correction closes.

### 5.5a Backlog reference disposition (Apone, T101 revision)

| Backlog file | Status | Disposition |
| --- | --- | --- |
| `F020-user-profile-settings.md` | backlog | Unchecked → HTTP integration test (persistence) + Vitest component test (header re-render), reload-persistence path added to the manual checklist |
| `F020b-user-profile-extras.md` | backlog | Unchecked → Vitest component tests per tab, cross-tab pass added to the manual checklist |
| `F021-admin-audit-log-viewer.md` | shipped (v1) | **Checked** — `/admin/audit` 403 already delivered by `AuditEventsAuthorizationTests`/`ViewerRoleAuthorizationTests`; `/admin/logs` half cross-referenced to F021b |
| `F021b-admin-logs-viewer.md` | backlog | Unchecked → HTTP integration test (403 status), mirroring the F021 pattern |
| `F022-user-default-sort-filter-prefs.md` | backlog | Unchecked → Vitest unit (merge logic) + HTTP integration test (round-trip); real sign-out/sign-in session added to the manual checklist (**G-08** family) |
| `F023-group-devices-by-dimension.md` | shipped (v1) | Partially checked — grouping/sorting Vitest unit already delivered (`groupDevices.test.ts`); the grouped-click-to-modal journey declared an accepted gap in the **G-09** family, compensated by existing component tests + manual **M-14**/**M-15** |
| `F024-multi-select-bulk-actions.md` | shipped (v1) | **Checked** — both Vitest units and the HTTP integration walkthrough already delivered (`BulkActionBar.test.ts`, `DevicesControllerTests.BulkUpdateDevices_WhenValid_…`); historical carve-out note reworded off Playwright |
| `F024b-bulk-actions-power-user.md` | backlog | Unchecked → HTTP integration test (bulk-set + audit correlation) + Vitest component test (selection/undo UI) |
| `F026-pwa-quick-win-ux-pack.md` | backlog | Unchecked (×2) → Vitest component assertions, no browser suite to add a journey to |
| `F027-global-nav-overhaul.md` | backlog | Unchecked → Vitest component tests (drawer, focus-trap, breakpoint switch); real-device pass added to the manual checklist |
| `F028-infinite-scroll-pull-to-refresh.md` | backlog | Unchecked → Vitest component test mirroring the existing `PullToRefresh` pattern; real touch-gesture pass added to the manual checklist |
| `F029-dark-mode-theme-contrast.md` | backlog | Unchecked → cross-referenced to the existing **C-21** pre-hydration contract test + manual **M-10** |
| `F030-device-tagging-fix.md` | backlog | Unchecked (×3) → HTTP integration test + Vitest component test on the tag-picker modal |
| `F031-merge-reference-data.md` | backlog | Unchecked → HTTP integration test (merge round trip) + Vitest component test (destructive-confirm dialog) |
| `F034-orphaned-device-fields-display.md` | shipped | **Checked** — the originally planned browser smoke test's coverage was already delivered by **H-05**/**C-08**/**C-19** in T102; historical note reworded, explicitly states the original journey was deleted in T101 |
| `F045-pwa-shell-and-device-list.md` | ready (design reviewed) | §6.2 rewritten from a Playwright test plan to a post-retirement note re-homing every assertion to the existing §6.1 Vitest harnesses and manual-checklist **M-03**/**M-14**/**M-15**; R8 and the acceptance-checklist line reworded off Playwright |

No journey was silently deleted and no automated coverage was invented: every
"Checked" row above cites a real, already-existing test; every still-open row
names a concrete future destination layer, not a vague deferral.



### 5.6 Unrelated wording / unavoidable

`src/TechInventory.Web/pnpm-lock.yaml` — `@vitest/browser-playwright` appears
**twice** as an *optional peer dependency of `vitest` itself*, not as an
installed package. It cannot be removed without dropping Vitest, and it causes
**no browser download**. The stale-reference guard **must** whitelist optional
peer-dependency metadata in lockfiles, or retirement can never go green. This is
the single most likely false positive in T101's guard.

---

## 6. T102 Replacement Workload by Layer

### 6.1 Backend — real HTTP integration / contract (`H-nn`) — 5 items

| ID | Test to author | Home |
| --- | --- | --- |
| **H-01** | Create-then-read round trip via typed request builders, so a missing `OwnerId`/`LocationId` is a **compile error** (replaces `fixtures/api.ts`) | `Controllers/DevicesControllerTests.cs` + a new typed builder in `Support/` |
| **H-02** | `DELETE /devices/{id}` with a reason writes an `AuditEvent` carrying that reason | `Controllers/DevicesControllerTests.cs` |
| **H-03** | Export query string and list query string resolve to the same row set | `Controllers/ExportControllerTests.cs` |
| **H-04** | `ViewerRoleIntegrationTestFactory` + **403 for Viewer** on device create/update/delete, import commit, reference mutation | `Support/` + `Controllers/` |
| **H-05** | Commit the canonical `data/Devices.csv` and assert the "Mohu Leaf Stitch" row's `Model` / `Purpose` | `Controllers/SharePointCsvImportTests.cs` |

### 6.2 Frontend — Vitest component/unit (`C-nn`) — 22 items

| ID | Test to author |
| --- | --- |
| **C-01** | `(authenticated)/+layout.ts` guard redirect for an unauthenticated session |
| **C-02** | `clearAuth()` clears MSAL + `ti_local_*`, and the guard then redirects |
| **C-03** | Role-less session renders the denial message |
| **C-04** | Device detail page action affordances (Edit link, Delete, Claim/Release) |
| **C-05** | `DeviceForm` Cancel discards and navigates back |
| **C-06** | Clear All empties every filter query param |
| **C-07** | Breadcrumb landmark: links, `aria-current`, axe-clean |
| **C-08** | `DeviceDetailFields` renders all eight F034 fields; empties elided |
| **C-09** | Import wizard chrome: 3-step list, labelled CSV input, axe-clean |
| **C-10** | Export page CTAs render, labelled, axe-clean |
| **C-11** | Creating a reference entity invalidates the reference query for `DeviceForm` |
| **C-12** | `DeviceTable` / detail actions hidden for Viewer |
| **C-13** | `/devices/{id}/edit` guard refuses a Viewer |
| **C-14** | `/offline` route renders, indicates offline state, axe-clean |
| **C-15** | Workbox config contract in `vite.config.ts`: `navigateFallback='/offline'`, denylist, `NetworkOnly` for POST/PUT/DELETE, `StaleWhileRevalidate` GET cache options |
| **C-16** | Offline mutation surfaces an error toast — never a silent success |
| **C-17** | `PwaUpdatePrompt` renders and dismisses |
| **C-18** | Route-level axe harnesses for `/devices`, `/devices/{id}`, `/admin/import`, `/admin/export`, `/admin/audit`, `/offline` |
| **C-19** | Import preview row expansion shows Model + Purpose per row |
| **C-20** | App mode hides the desktop Add CTA and the FAB; nav Add present |
| **C-21** | `app.html` pre-hydration theme contract (`theme-preference` key) |
| **C-22** | Resolve the 8 orphaned "deferred to E2E" comments (§5.2) — each becomes a test or a named gap |

### 6.3 Manual (`M-nn`) — 15 checks, §7 · Accepted gaps (`G-nn`) — 9, §8

### 6.4 Summary

| Layer | New items | Behaviours already covered | Net |
| --- | ---: | ---: | --- |
| HTTP integration / contract | **5** | 12 journey behaviours already live across `ExportControllerTests`, `ImportsControllerTests`, `DevicesControllerTests`, reference controllers, `AuthIntegrationTests` | Small, high-value |
| Component / unit | **22** | 33 journey behaviours already live across 68 Vitest files / 558 cases, 37 axe-checked | Largest block; all mechanical |
| Manual checklist | **15** | — | New, and honestly declared |
| Accepted gaps | **9** | — | Named, owned |
| **Deleted with no replacement** | — | **5 assertions** (§3.1 root-render smoke, §3.12 offline queue replay, §3.17 body-bg duplicate + vacuous screenshot, §3.16 tautological token-storage) | Each justified in its row |

---

## 7. Manual PWA Validation Checklist

> **Published as of T102 completion:** the operational, fillable version of
> this checklist lives at
> [`../../docs/testing/manual-pwa-validation.md`](../../docs/testing/manual-pwa-validation.md).
> This section remains the analysis record of how each check was derived;
> §12.3 records completion.

**Owner:** `briandenicola` (single-operator household deployment).
**Class:** `REVIEWED` — never reported as automated coverage (`brief.md` §2.1,
`plan.md` §2.5).
**Cadence:** run **before each release tag**, and **on any change** to
`vite.config.ts` (PWA block), `src/app.html`, `src/routes/offline/**`,
`AppBottomNav.svelte`, `AppMenuPopover.svelte`, `DevicePwaRow.svelte`,
`PwaUpdatePrompt.svelte`, or the MSAL config.
**Record:** tick-list in the release notes; a missed run is recorded as an
explicit exception under `plan.md` §2.10 (T105).

| # | Check | Covers |
| --- | --- | --- |
| **M-01** | Install the app on iOS Safari (Add to Home Screen); it launches standalone with the correct name/icon | install, manifest |
| **M-02** | Install on desktop Chromium; it launches standalone | install |
| **M-03** | In the **installed** app on a phone: the bottom nav pill + Settings bubble render, every nav target is comfortably tappable, and the last list row is not hidden behind the nav | J-15 geometry (**G-06**), D-175 |
| **M-04** | In a **mobile browser tab** (not installed): no bottom nav, Add FAB present | D-175 negative case |
| **M-05** | After deploying a new build, the update prompt appears and reloads to the new version | service-worker update (**G-04**) |
| **M-06** | Go offline, navigate to an uncached route: the `/offline` shell is served, not a browser error page | `navigateFallback` (**G-04**) |
| **M-07** | Offline: a previously viewed device list still renders from cache | `StaleWhileRevalidate` |
| **M-08** | Offline: attempt a create/edit/delete — it fails **visibly** with an error, and no change appears to have been saved | `NetworkOnly` mutations |
| **M-09** | Restore connectivity: data refreshes without a manual reload | recovery |
| **M-10** | With dark theme stored, hard-reload: no light flash before first paint | FOUC (**G-07**) |
| **M-11** | Real Entra ID sign-in **and** sign-out in a browser; DevTools → Application shows **no** token-like key in `localStorage` | PRD #1, D-010 gate (**G-08**) |
| **M-12** | Create a device from the modal, then import a small CSV via the real file picker; both appear in the list | **G-01**, **G-02** |
| **M-13** | Export CSV: the download completes and opens cleanly in a spreadsheet | **G-03** |
| **M-14** | WebKit/Safari spot check: `/devices`, a device detail page, and the filter sheet render correctly (safe-area insets, sticky headers) | **G-05** |
| **M-15** | Firefox spot check: `/devices` + filter sheet; keyboard-only pass reaches the nav, opens the menu, and Escape restores focus | **G-05** |

**15 checks.** No check duplicates something a Vitest or HTTP test already
asserts; each exists because the risk is `display-mode`, service-worker,
first-paint, real-download, or engine-specific — the exact categories
`plan.md` §2.6 assigns to a manual checklist.

---

## 8. Accepted Gaps Register

Every gap is owned by `briandenicola` and reviewed at the cadence in §7.

| ID | Gap — what is no longer automated | Why it cannot go lower | Compensating control |
| --- | --- | --- | --- |
| **G-01** | Modal save → list repaint in a real browser | Cross-layer composition; jsdom does not run the real router + cache together | **H-01** + **C-20** + **M-12** |
| **G-02** | Real file-picker / drag-drop upload mechanics | Browser-owned input surface | **C-09** + `ImportsControllerTests` + **M-12** |
| **G-03** | Browser download of an exported CSV | Browser-owned download pipeline | `ExportControllerTests` (5) + **H-03** + **M-13** |
| **G-04** | Service-worker registration, install prompt, update prompt, offline navigation | No service worker exists in jsdom | **C-14**–**C-17** (config + component contracts) + **M-05**–**M-09** |
| **G-05** | Engine-specific rendering and accessibility (WebKit / Firefox) | Single-engine test runner | 37 axe-checked component tests + **C-18** + **M-14**, **M-15** |
| **G-06** | Pixel geometry — 44 × 44 hit targets *as laid out*, search-width cap, control not obscured by the fixed nav | jsdom computes no layout | `AppBottomNav.test.ts` hit-area assertion (style-level) + **M-03** |
| **G-07** | Absence of a real first-paint theme flash | Requires a real paint | **C-21** + **M-10** |
| **G-08** | Real Entra redirect flow end to end | External IdP + real browser redirect | `msal.test.ts`, ESLint, pre-commit, gitleaks + **M-11** |
| **G-09** | Route-level axe composition test for `/devices` (the one route of **C-18**'s six not authored) | `devices/+page.svelte` is 1,000+ lines and composes ~15 stores/queries (`useDevices`, `userPrefs`, `viewState`, `pullToRefresh`, `deviceCreate`, `displayMode`, `appNav`, …) plus ~10 child components; a deterministic jsdom composition harness was judged disproportionate to author and maintain versus the risk it retires (same reasoning as **C-20**/§12.2) | The other 5 of 6 **C-18** route harnesses (`/devices/{id}`, `/admin/import`, `/admin/export`, `/admin/audit`, `/offline`) plus 37 component-level axe-checked Vitest files (including every child component `/devices` composes: `DeviceTable`, `DeviceFilters`, `PaginationControls`, `AddDeviceModal`, `DeviceDetailModal`, `BulkActionBar`, `BulkUpdateModal`, `BulkDeleteModal`) + **M-14**, **M-15** (WebKit/Firefox spot checks of `/devices` itself) |

Each gap is **declared**, not silent. None is presented as automated coverage.

---

## 9. PRD §7.5.4 Journey Traceability After Retirement

`docs/prd.md` §7.5.4 requires an ADR to remove a journey. **No journey is
removed here** — each is re-homed. The table is the ADR candidate's evidence.

| PRD journey | Post-retirement home | Automated? |
| --- | --- | --- |
| 1 Sign in | **C-01** (guard) + `msal.test.ts` + **M-11** (real Entra) | partly |
| 2 Sign in denied | `AuthIntegrationTests` (401/403) + **C-03** | yes |
| 3 Create device | `DevicesControllerTests` + **H-01** + `AddDeviceModal`/`DeviceForm` tests | yes |
| 4 Edit device | `UpdateDevice_WhenValid_…` + **C-04**, **C-05** | yes |
| 5 Delete device | `DeleteDevice_*`, `DeviceVisibilityRegressionTests`, **H-02**, `DeleteDeviceModal.test.ts` | yes |
| 6 Browse and filter | `deviceFilterUrl.test.ts`, `viewState.test.ts`, **C-06** | yes |
| 7 Detail view | `GetDeviceById_*`, **C-04**, **C-07**, **C-08** | yes |
| 8 Import CSV | `ImportsControllerTests` (10), **C-09**, **C-19** | yes |
| 9 Export CSV | `ExportControllerTests` (5), **H-03**, **C-10**, **M-13** | yes |
| 10 Reference data admin | reference controller tests, `schema.test.ts` ×4, **C-11** | yes |
| 11 Role enforcement | `ViewerRoleAuthorizationTests`, 4 Member authorization suites, **H-04**, **C-12**, **C-13** | yes |
| 12 Offline app shell | **C-14**–**C-17** + **M-06**–**M-09** | partly (declared **G-04**) |
| 13 Accessibility smoke | 37 axe-checked Vitest files + **C-18** (5 of 6 route harnesses; `/devices` deferred, **G-09**) + **M-14**, **M-15** | partly (declared **G-09**) |

**10 of 13 journeys end up more covered than they are today** (today: zero
executed). Three (1, 12, and 13) retain a declared manual/accepted-gap
component.

**ADR candidate, not an authorization:** `docs/prd.md` §7.5.2/§7.5.3/§7.5.4 and
`.specify/memory/constitution.md` §6.5.7/§7 must be amended to remove
"Playwright is required". **T103 surfaces this; it does not perform or authorize
it.** `plan.md` §6 already names it.

---

## 10. Measurable Retirement Completion

Retirement is complete — and only then may `brief.md` §2.1 move from `REVIEWED`
to `ENFORCED` — when **all ten** are simultaneously true and observed:

| # | Condition | How it is observed |
| --- | --- | --- |
| 1 | `tests/e2e/` does not exist | `git ls-files tests/e2e` returns **0** (currently 28) |
| 2 | No Playwright package in any manifest | `tests/e2e/package.json` and its lock are gone; no `playwright` dependency in `package.json` or `src/TechInventory.Web/package.json` (**already true today** for both) |
| 3 | No Playwright config, fixture, page object, or spec anywhere | Repo-wide search returns 0 hits outside the §5.5 history exemption and the §5.6 lockfile peer metadata |
| 4 | No script invokes Playwright | `scripts/run-e2e.*` deleted; `scripts/verify.*` contain no browser stage |
| 5 | No Task target invokes Playwright | `Taskfile.yml` has no `test:e2e*`; `task test` does not delegate to one |
| 6 | No workflow invokes Playwright or downloads browsers | `.github/workflows/**` grep is clean; no `playwright install` step |
| 7 | **Clean-checkout verification downloads no browser** | Fresh clone → install → `task verify`: no `playwright install`, no browser binary fetch, no `~/.cache/ms-playwright` write |
| 8 | No normative document claims Playwright is required | `.specify/memory/constitution.md`, `docs/prd.md`, `docs/testing.md`, `.github/copilot-instructions.md`, `.squad/**` charters carry no such claim; remaining hits are §5.5 history only |
| 9 | The stale-reference guard exists, passes, and **has been tamper-tested** | T101 adds it; T105 records a deliberate-break run in which it fails (a guard with no break evidence does not count — `plan.md` §2.10, R-7) |
| 10 | **Issue #89 is closed citing retirement + migration**, linking this document and the T102 replacements | The close comment references §3's matrix and names the H-/C-/M- items; `evidence.md` **U-07 closed as moot** |

**Explicitly not a completion condition:** any Playwright run, anywhere, in any
mode.

---

## 11. Non-Claims

1. **No file has been deleted, edited, or authored outside this work package.**
   T103's entire diff is: `e2e-classification.md` removed, this file added, and
   T103 status/evidence lines in `tasks.md` / `plan.md` / `validation.md`.
2. **No gate has been implemented, run, or passed.** This document records
   *evidence*, not a gate (`validation.md` §1).
3. **The Playwright suite has never been observed passing and will not be.** The
   only runs performed here were `--list` collection probes (exit 1, and a
   filtered exit 0), executed read-only to reconcile counts. No browser was
   launched and no browser was downloaded.
4. **The "already covered" column is a static reading of test names and
   assertions**, not a coverage-tool measurement. It is sufficient to authorize
   deletion because every row also names a T102 item or an accepted gap; it is
   not a claim of line coverage.
5. **This document does not amend the PRD or the constitution**, and does not
   authorize their amendment. §9 surfaces the ADR candidate only.
6. **No row proposes a future automated Playwright role** in any form — merge
   gate, scheduled, release, or optional.

---

## 12. T102 Completion Record

**Status: `DONE` — independently verified and APPROVED by Ripley 2026-09-02
(`validation.md` §6). T101 is authorized to begin.**

T102 execution followed this matrix. Originally recorded here by Apone
(Tester / QA) 2026-09-02, cross-checked against the executing agents' result
artefacts and re-verified directly at that time (backend: `dotnet test
tests/TechInventory.IntegrationTests -c Debug` → 240 passed, 5 skipped, 0
failed; frontend: `pnpm vitest run --no-file-parallelism` → 83 files / 645
tests passed). **Both figures are superseded** by the two revision cycles
below and by the reviewer's own final run: backend Unit **278/278**,
Integration **292 passed / 4 skipped / 296 total, 0 failed**; frontend
**83 files / 649 tests passed**. Any reader citing this section must use the
current figures, not the original ones retained above for history.

**Revision note (Ripley review, findings B1–B3; corrected by Bishop).** Ripley
rejected Apone's consolidation of this section for three defects: **B1** —
`BrandsController`, `CategoriesController`, `LocationsController`,
`NetworksController`, `TagsController`, `OwnersController` still carried bare
`[Authorize]` on their ordinary Create/Update/Delete actions (Viewer could
mutate reference data), and the sole HTTP-level Viewer-403 regression test,
`AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden`, carried a
permanent `Skip` attribute and had never executed; **B3** — `canClaim`/
`canRelease` in `deviceRowActions.svelte.ts` and `DeviceDetailModal.svelte` had
no role gate at all, so a Viewer could claim/release device ownership, and a
test/comment in `deviceRowActions.svelte.test.ts` asserted this was
"intentional"; **B2** — this section (and `tasks.md`, `validation.md`) claimed
**C-18** ("route-level axe harnesses for `/devices`, `/devices/{id}`,
`/admin/import`, `/admin/export`, `/admin/audit`, `/offline`") and "21 of 22"
`C-nn` items fully **Done**, when no `/devices/+page.test.ts` axe harness ever
existed (only 5 of the 6 named routes do). Bishop applied
`AuthorizationPolicies.AdminOrMember` to the 6 reference controllers' ordinary
mutations, removed the permanently-skipped test and replaced its coverage with
real, executing `ViewerRoleAuthorizationTests` cases, added a role gate to
claim/release on both surfaces and rewrote their tests, and corrected this
section (below), `tasks.md`, and `validation.md` to match reality. See
`t102-bishop-revision.md` (session artefact) for the full disposition.

**Second revision note (Ripley re-review, second REJECTED verdict, findings
B3/B2-R/B4; corrected by Hicks; Bishop and Apone locked out this cycle).**
Ripley's re-review found Bishop's B3 fix incomplete and this section's
evidence still overstated:

- **B3 (incomplete gate).** `devices/[id]/+page.svelte` derives its own
  independent `canClaim`/`canRelease` booleans — a **third** copy separate
  from `deviceRowActions.svelte.ts` and `DeviceDetailModal.svelte`, the only
  two surfaces Bishop's B3 fix touched. This third copy was never gated, so a
  Viewer navigating directly to a device's own detail route (not through the
  modal) could still see and use Claim/Release — the exact defect class B3
  was meant to close, surviving in the one surface the fix didn't reach.
  Hicks added the same `canEdit &&` gate already used in
  `DeviceDetailModal.svelte` to both derivations in `+page.svelte`, and added
  three cases to `devices/[id]/page.test.ts`: a Viewer-negative case (Viewer
  owns the device; no actions menu renders at all), an Admin-positive case
  (Release Ownership renders), and a Member-positive case (Claim Ownership
  renders for a non-owned device) — preserving the existing Admin/Member
  behaviour while proving Viewer read-only. A parallel search of every other
  device action surface (`devices/+page.svelte` bulk toolbar, admin reference
  list pages, `DevicePwaRow.svelte`) found no further ungated Viewer mutation
  affordance of this kind; the one adjacent item noted —
  `devices/+page.svelte`'s bulk-field-change buttons render regardless of
  role, though the backend already requires `AdminOrMember`/`Admin` on
  `bulk/update`/`bulk/delete` — is a UI-affordance-only gap, not a security
  hole, and is out of this fix's scope (a list-route bulk-action surface, not
  the direct detail route B3 named).
- **B2-R (truthful C-04/C-12 evidence).** This section previously counted
  **C-04** ("detail page action affordances … Claim/Release") and **C-12**
  (`DeviceTable` / detail actions hidden for Viewer) inside the "20 of 22
  `C-` items Done" figure both before and after Bishop's fix, with no
  qualification — but until Hicks's fix above, `devices/[id]/+page.svelte`'s
  own Claim/Release derivation had **no test at all**, so that claim of
  completeness was false for one of the three surfaces C-04/C-12 cover. Both
  are now genuinely Done across all three surfaces
  (`DeviceDetailModal.svelte`, `deviceRowActions.svelte.ts`,
  `devices/[id]/+page.svelte`), evidenced by durable, re-runnable repository
  tests named above and in §12.2 below — not solely by a session artefact.
  The frontend test count previously re-cited here as "645/645" is corrected
  to the actual current count (§12.2).
- **B4 (OpenAPI 403 contract).** Ripley's review cited **21**
  `AdminOrMember`-gated mutations undocumented for a `403` response. A full
  re-enumeration by Hicks against every `[Authorize(Policy =
  AuthorizationPolicies.AdminOrMember)]` addition on this branch found **26**,
  not 21 — Brands/Categories/Locations/Networks/Tags/Owners
  Create/Update/Delete (18), `DevicesController`
  Create/Update/Delete/AddTag/RemoveTag/ClaimOwnership/BulkUpdateDevices (7),
  and `ImportsController.CommitImport` (1). The true count is corrected here
  as the same truthful-evidence principle as the test-count fix above. All 26
  gained `[ProducesResponseType(typeof(ProblemDetails),
  StatusCodes.Status403Forbidden)]`, in the same ascending-status-code
  position already used by the pre-existing Admin-only `BulkDelete*` actions.
  `openapi.yaml` was regenerated through the repository's canonical
  generation mechanism (`dotnet run --project
  src/TechInventory.Api/TechInventory.Api.csproj -- export-openapi`, the
  command `task openapi:export` runs) and diffed structurally with
  `scripts/compare-openapi.py` against the previously committed file:
  **exactly 26 differences**, every one a `responses.403` addition, nothing
  else changed. `src/TechInventory.Web/src/lib/api/generated/types.ts` was
  regenerated from the updated spec via `pnpm run generate:client` (286
  additive lines, 0 deletions). A new parameterized xUnit `[Theory]`,
  `OpenApiDriftTests.AdminOrMemberGatedOperation_DeclaresForbiddenResponse`
  (26 `[InlineData]` cases, one per operation above), asserts each operation's
  `403` response is present in the committed contract — independent of, and
  in addition to, the existing whole-document drift assertion, so a future
  removal of a single operation's `403` response fails on its own rather than
  only as part of a full-document byte diff.

Full re-verification after this revision (including the optional
Viewer-positive-GET control, §12.1 H-04): backend `dotnet test -c Release` —
Unit 278/278, Integration 292/296 (4 pre-existing skips, 0 failed, including
the 26 new B4 `[Theory]` cases, the 7 new Viewer-positive-GET cases, and the
pre-existing `OpenApiDriftTests` suite; one run also observed a single
non-reproducing failure in the unrelated, pre-existing
`OwnersControllerTests.GetCurrentOwner_AutoProvisionsOnFirstCall_
AndReturnsSameOwnerOnSecondCall` under full-suite parallel execution — passes
both in isolation and on immediate full-suite re-run, so recorded as
pre-existing test-parallelism flake, not a regression from this revision);
`dotnet format --verify-no-changes` clean; frontend `pnpm run check` 0
errors/warnings, `pnpm run lint` clean, `pnpm run test -- --run` 83 files /
649 tests passed, `pnpm run build` succeeds (PWA service worker generates, 90
precache entries). See `t102-hicks-final-revision.md` (session artefact) for
the full disposition; the durable evidence is the test names, controller
files, and spec paths cited above and in §12.1/§12.2/§12.4 below, not the
artefact itself.

### 12.1 Backend `H-nn` — all 5 complete

| ID | Status | Note |
| --- | --- | --- |
| H-01 | **Done** | `DevicesControllerTests.CreateDevice_WhenValid_IsReturnedByDeviceSearch` |
| H-02 | **Done** | `DevicesControllerTests.DeleteDevice_WithReason_HidesFromDefaultListAndAuditsReason` |
| H-03 | **Done** | `ExportControllerTests.ExportDevices_AndDeviceList_ResolveToSameRowSetForSharedPredicate` |
| H-04 | **Done** (completed after the authorization fix below, then expanded again under Ripley's B1 finding, then again for the optional Viewer-positive-GET control below) | `ViewerRoleAuthorizationTests` (44 cases: 18 original merge/bulk-delete/audit-events/Device/Import mutation cases + 1 release-ownership symmetry case + 18 parameterized reference-entity Create/Update/Delete cases added for B1 + 7 `GetCollection_WhenCallerIsViewer_ReturnsOk` positive-control cases added by Hicks, across Brands/Categories/Locations/Networks/Tags/Owners) + `DeviceAndImportMemberAuthorizationTests` (Member-positive control) |
| H-05 | **Done** | `SharePointCsvImportTests.CommitImport_CanonicalDevicesCsv_MohuLeafStitchRowPersistsModelAndPurpose` |

**H-04 defect and fix (evidence, not scope drift).** Hicks's H-04 work used
real HTTP against the real authorization pipeline and proved `DevicesController`
create/update/delete and `ImportsController` commit carried only a bare
`[Authorize]` (any authenticated role), so **Viewer could mutate data** —
a constitution §5.2 violation (Viewer = read-only) that the retired,
never-collecting Playwright suite (`journeys/11-role-enforcement.spec.ts`, all
`test.todo`/`skip`) had never actually verified either. This was reported as a
blocker, not silently fixed. `briandenicola` explicitly approved a tightly
coupled production fix: a new `AuthorizationPolicies.AdminOrMember` policy
applied to the 7 affected `DevicesController` actions and `ImportsController`
commit (`viewer-auth-fix-results.md` — session artefact, not present in this
repository; the durable evidence is `ViewerRoleAuthorizationTests` and
`DeviceAndImportMemberAuthorizationTests` named in §12.1 above). Full-suite
re-verification after the fix:
**Unit 278/278, Integration 240/245 (5 pre-existing skips, 0 failed)** *(that
run's figures; the current suite is 292/296 with 4 skips — see §12 preamble)*, `dotnet
format --verify-no-changes` clean, `OpenApiDriftTests` zero-drift. This is
recorded here as **evidence of what real HTTP replacement testing found**, not
as new T102 scope: the fix stayed inside the authorization policy this task was
already proving, touched no unrelated controller, and was authorized before
being made.

**B1 correction (Ripley review; Bishop fix).** The same defect class survived
in the 6 reference-entity controllers: `BrandsController`, `CategoriesController`,
`LocationsController`, `NetworksController`, `TagsController`, and
`OwnersController` (`CreateOwner`/`UpdateOwner`/`DeleteOwner` only — the `/me`
self-service endpoints correctly stay open to all roles) all carried bare
`[Authorize]` on Create/Update/Delete, so **Viewer could mutate reference
data** — merge and bulk-delete on the same controllers were already
Admin-only and needed no change. `briandenicola`'s prior approval of the
tightly coupled `AdminOrMember` enforcement (above) is applied consistently
here as the same, already-approved fix — not a new authorization decision.
The only HTTP-level regression test asserting a Viewer-403 on these routes,
`AuthIntegrationTests.ViewerRoleOnAdminEndpoint_Returns403Forbidden`, carried a
permanent `[Fact(Skip = "...")]` attribute and had **never executed** — no
prior citation of it as coverage was accurate. It has been **removed**, and
real, executing coverage now lives in `ViewerRoleAuthorizationTests`
(18 new `[Theory]` cases across the 6 controllers × Create/Update/Delete, plus
a `ReleaseDeviceOwnership_WhenCallerIsViewer_ReturnsForbidden` case for
symmetry with the existing claim test). Member/Admin positive paths are
unaffected — every existing reference controller test class
(`BrandsControllerTests`, etc.) already runs under the default Admin test
identity. Re-verified: targeted `dotnet test` (`ViewerRoleAuthorizationTests`,
`AuthIntegrationTests`, the 6 reference controller test classes,
`DeviceAndImportMemberAuthorizationTests`) — 118 total, 115 passed, 3 skipped
(pre-existing, unrelated), 0 failed.

**B4 correction (Ripley second review; Hicks fix).** The OpenAPI contract
committed at `openapi.yaml` did not document a `403 Forbidden` response for
any of the 26 operations gated by `AuthorizationPolicies.AdminOrMember` (see
the second revision note above for the full enumeration and true count). All
26 controller actions now carry
`[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]`,
`openapi.yaml` and `src/TechInventory.Web/src/lib/api/generated/types.ts` are
regenerated and re-verified structurally clean of unexpected drift, and
`OpenApiDriftTests.AdminOrMemberGatedOperation_DeclaresForbiddenResponse` (26
`[Theory]` cases) pins the contract going forward. Re-verified: targeted
`dotnet test --filter FullyQualifiedName~OpenApiDriftTests` — 36 passed, 1
pre-existing skip, 0 failed.

### 12.2 Frontend `C-nn` — 20 of 22 complete; C-18 (partial) and C-20 accepted gaps

C-01–C-17, C-19, C-21, C-22 (20 items): **Done**, re-verified —
`pnpm vitest run --no-file-parallelism` 83/83 files, **649/649 tests**
(previously re-cited here as 645/645; corrected per the B2-R finding above —
**C-04** and **C-12** specifically are only now genuinely Done across all
three action surfaces, evidenced by the 3 new cases in
`devices/[id]/page.test.ts` cited in the second revision note above); `pnpm
run check` 0 errors/warnings; `pnpm run lint` clean; `pnpm run build`
succeeds (PWA SW generates, 90 precache entries).

**C-18** (route-level axe harnesses for `/devices`, `/devices/{id}`,
`/admin/import`, `/admin/export`, `/admin/audit`, `/offline`) is **5 of 6
Done, not 6 of 6 as previously recorded (Ripley B2 finding)**: real axe
harnesses exist and pass for `devices/[id]/page.test.ts`,
`admin/import/page.test.ts`, `admin/export/page.test.ts`,
`admin/audit/page.test.ts`, and `offline/page.test.ts`, but no
`devices/+page.test.ts` was ever authored — grep-confirmed, there is no test
file for the `/devices` list route at all. The sixth route is now recorded as
**G-09** (§8): `devices/+page.svelte` is 1,000+ lines composing ~15
stores/queries and ~10 child components, and a deterministic jsdom
composition harness for the whole route was judged disproportionate versus
the risk it retires, given every child component it assembles is already
independently axe-checked. This is the same shape of judgment call as
**C-20** below, and follows the same accepted-gap register.

**C-20** (app-mode hides the desktop Add CTA and FAB; nav Add present) is
accepted as a gap, not a failure, because the result artefact gives sufficient
rationale and names the covering primitives: `DeviceListAddActions.svelte` is
dead code (zero production imports, grep-confirmed) — the real gate is one
inline boolean (`displayMode.isPwa`) in the 1,000+-line `devices/+page.svelte`.
Its constituent parts are already independently unit-tested
(`displayMode.svelte.test.ts`, `AddDeviceFab.test.ts`); composing the whole
route to exercise one boolean was judged disproportionate. This gap follows
the same shape and owner as G-01–G-09 (§8) and is folded under that register.

### 12.3 Manual checklist — complete

The 15-check manual validation checklist from §7 is published as the durable,
owned operational document at
[`../../docs/testing/manual-pwa-validation.md`](../../docs/testing/manual-pwa-validation.md)
(owner `briandenicola`, `REVIEWED`, not merge-blocking). §7 above remains the
analysis record of how each check was derived from the retired suite; the
linked file is the one to actually run and fill in.

### 12.4 T102 disposition summary

| Category | Count | Disposition |
| --- | --- | --- |
| `H-` backend | 5 | 5 Done |
| `C-` frontend | 22 | 20 Done, 2 accepted-gap items (**C-18** partial — `/devices` route harness, **G-09**; **C-20**) |
| `M-` manual checklist | 15 | 15 published, complete as a checklist (not automated) |
| `G-` accepted gaps | 9 (+ C-20 folded in) | all owned by `briandenicola`, compensating controls named in §8 |
| OpenAPI `403` contract (B4) | 26 `AdminOrMember`-gated operations | 26 of 26 documented, verified structurally drift-free, pinned by `OpenApiDriftTests.AdminOrMemberGatedOperation_DeclaresForbiddenResponse` |

**T102 status: `DONE`, reviewer-verified and APPROVED (Ripley, 2026-09-02 —
`validation.md` §6; T101 authorized to begin).** Every coverage-migration row
maps to passing
replacement coverage (H-/C-), an explicitly accepted gap with a named owner
(G-01–G-09, and C-20 by the same reasoning), or the published manual checklist
(M-01–M-15). No row is unresolved, and no row overstates what exists: **C-18**
is recorded as 5 of 6 with the sixth an owned, compensated gap, not as fully
Done. **C-04** and **C-12** are recorded as genuinely Done only as of the
second revision note in §12 above — see there for the specific defect the
first revision missed and the durable test evidence that closes it.

---

## 13. T101 Completion Record

Implemented by Hudson, 2026-09-02, on branch `chore/agentic-development-foundation`.
**Status: `REJECTED` at the independent reviewer gate (Ripley, 2026-09-02) —
the self-recorded `DONE` below is withdrawn.** Two blockers must be closed by
**Apone** (Hudson locked out): **B1** stale `task test:e2e` / `task test:e2e:run`
instructions surviving in `.github/T47-CI-SETUP-CHECKLIST.md`, and **B2** the
guard's blanket `specs/` exemption hiding ten unchecked `specs/_backlog/**`
promises to author new Playwright tests — which also requires §5.5 below to be
amended, reclassifying `specs/_backlog/**` from RETAIN to REVISE. Everything
else in this section was independently re-verified and stands. Full verdict:
[`validation.md`](./validation.md) §7.

### 13.1 Deletion — cross-checked against §4 waves D1–D5, D8

`git ls-files tests/e2e` before deletion returned exactly 28 tracked files,
matching the matrix rows row-for-row: no file was deleted that lacked a §4
row, and nothing in §4 was left undeleted.

| Deleted | Matrix wave |
| --- | --- |
| `tests/e2e/journeys/*.spec.ts` (15 files) | D1–D2 |
| `tests/e2e/security/*.spec.ts` | D1 |
| `tests/e2e/fixtures/*.ts`, `tests/e2e/pages/*.ts`, `tests/e2e/pages/README.md` | D3–D4 |
| `tests/e2e/theme-fouc.spec.ts` (outside `testMatch`, U-08) | D2 |
| `tests/e2e/package.json`, `tests/e2e/package-lock.json`, `tests/e2e/playwright.config.ts`, `tests/e2e/README.md` | D5 |
| `scripts/run-e2e.ps1`, `scripts/run-e2e.sh` | D8 (script invocations) |
| `.squad/skills/playwright-e2e-scaffolding/` (entire skill directory) | D8 (support tooling) |

`tests/e2e/node_modules/` and `tests/e2e/playwright-report/` were already
gitignored/untracked — deleting the parent directory removed them with no
separate authorization needed.

### 13.2 Dependency and manifest removal

- Root `package.json` and `src/TechInventory.Web/package.json` never
  depended on Playwright — only the now-deleted `tests/e2e/package.json`
  did. No manifest edit was needed there.
- `pnpm-lock.yaml`'s only remaining Playwright-named string is
  `@vitest/browser-playwright`, vitest's own **optional peer dependency**
  (§5.6) — never installed, confirmed by the clean-install proof below.
- `Taskfile.yml`: removed `test:e2e:run` and `test:e2e`, and the
  `test:e2e:run` step inside `test`; added `check:stale-refs`.
- `scripts/verify.ps1` / `scripts/verify.sh`: step 9/9 (previously the
  Playwright run against the hermetic compose stack) now runs the
  stale-reference guard and its test; step count held at 9.
- `.github/workflows/ci.yml`: the muting comment reworded off Playwright by
  name while preserving the "not re-evaluated, T104's call" framing.

**Clean-install proof (condition 7 of §10):**

```
cd src/TechInventory.Web
Remove-Item -Recurse -Force node_modules
pnpm install --frozen-lockfile
```

Result: 617 packages resolved, **0 downloaded**, no `@playwright/test` or
`playwright` package present in the resulting tree. The machine's
pre-existing `%LOCALAPPDATA%\ms-playwright` browser-cache directory's
`LastWriteTime` was unchanged by the install (dated 2026-08-24, predating
this session) — proving the install did not trigger a browser download.

### 13.3 Stale-reference guard

`scripts/check-stale-playwright-references.mjs` — a case-insensitive keyword
scan across every git-tracked file, plus two independent structural
hard-fails that do not rely on the keyword at all: any tracked path under
`tests/e2e/`, and any `playwright.config.*` file anywhere in the repo.

Exemptions (all deliberate, each citing the classification section that
authorizes it): `specs/**`; `.squad/decisions.md`, `.squad/decisions/**`,
`.squad/session-log.md`; `.squad/agents/*/history.md`; `SESSION-NOTES.md`,
`.copilot-state.md`; `docs/testing/manual-pwa-validation.md` (must keep
naming Playwright to explain why the manual checklist exists — never
delete); `.env.e2e`, `docker-compose.e2e.yml` (content revision deferred to
T104, §5.3); `.specify/memory/constitution.md`, `docs/prd.md` (ADR required
to amend, §5.4 — surfaced, not performed, by this task); the guard's own
script and test file; `pnpm-lock.yaml` lines naming
`@vitest/browser-playwright`; and lines that only reference the guard's own
filename (`check-stale-playwright-references[.test].mjs`) wherever it is
invoked or documented.

Wired into `Taskfile.yml` (`check:stale-refs`) and both `verify.ps1`/
`verify.sh` (step 9/9) — the nearest existing verification surface, per this
task's scope; **T104 still owns the unified verification interface** and
this wiring does not pre-empt that consolidation.

Repository-native automated coverage: `scripts/check-stale-playwright-references.test.mjs`,
using Node's built-in `node:test` + `node:assert/strict` (no new framework
introduced) — **15/15 tests pass**, covering the clean-pass case, every
exemption individually, both structural hard-fails, the lockfile
peer-dependency exemption (and that it still flags a real installed
Playwright entry), the guard-self-filename exemption (and that it still
flags a genuine Playwright mention sharing the same line), and a negative
control proving unrelated docs/charters are *not* exempted.

**Live run against the full repository:**

```
Stale-reference guard passed: 0 active Playwright references across 901 tracked file(s).
```

**Not claimed:** tamper-tested status. No deliberate-break run was performed
or recorded here — that is explicitly T105's job per §10 condition 9 and
`plan.md`'s T105 row. This guard exists and passes; it has not yet been
proven to actually fail when it should.

### 13.4 Verification-promise removal — files edited

Every file below had at least one Playwright/browser-E2E reference removed
or reworded to reflect retirement, without deleting historical facts:

`docs/testing.md` (full section rewrite: ToC, Quick Start, Non-negotiables,
"Choosing the Right Test Type" table, the former "Playwright E2E Tests" and
"Accessibility Tests" sections, Authentication-in-Tests table, Debugging,
Flaky Test Policy, "Writing a New Critical Journey"), `README.md`,
`.github/copilot-instructions.md`, `.github/T47-CI-SETUP-CHECKLIST.md`,
`.github/workflows/README.md`, `.github/workflows/ci.yml`,
`docs/security-baseline.md`, `docs/threat-model.md`, `docs/auth-design.md`,
`docs/known-issues.md` (the `t23-deferred-form-tests` entry fully resolved —
T102 fixed the underlying test-fixture bug; the original jsdom-reactivity
diagnosis was wrong, corrected here rather than merely de-referenced),
`.gitignore`, `.dockerignore`, `src/TechInventory.Web/eslint.config.js`,
`.squad/agents/apone/charter.md`, `.squad/agents/vasquez/charter.md`,
`.squad/routing.md`, `.squad/team.md`,
`.squad/templates/machine-capabilities.md`,
`.squad/skills/token-storage-inspection/SKILL.md` (rewritten around the
unit-level MSAL-cache-configuration assertion pattern, per
`coverage-migration.md` §3.16).

`.github/pull_request_template.md` and
`.copilot/skills/test-discipline/SKILL.md` were checked directly and already
contained no Playwright reference — no edit was needed.

### 13.5 AC-005 exceptions — recorded, not silent

> **Reviewer correction (Ripley, T101 re-review 2026-09-02, `validation.md`
> §7.8.5).** The framing below is corrected: AC-005 enumerates seven
> surfaces — `package.json`, `pnpm-lock.yaml`, `Taskfile.yml`, `scripts/**`,
> `.github/workflows/**`, `docs/testing.md`, and the PR template. Neither
> `.specify/memory/constitution.md` nor `docs/prd.md` is on that list, so
> item 1 below is **out of AC-005's scope**, not an **exception to it**.
> AC-005's "zero references" is met on all seven enumerated surfaces.

AC-005 requires zero active references on its seven enumerated surfaces, and
that is satisfied. Two further items are recorded here rather than papered
over:

1. **`.specify/memory/constitution.md` and `docs/prd.md`** still name
   Playwright as mandatory. §5.4 classifies amending them as an **ADR
   candidate, not a T101 authorization** — this task surfaces that gap and
   exempts the guard from failing on it; it does not resolve it. Out of
   AC-005's scope; recorded as a **mandatory package-closure precondition**
   for `briandenicola` (`validation.md` §7.3.3, §7.8.5).
2. **`.squad/log/**` and `.squad/orchestration-log/**`** contain dated
   historical session logs not individually enumerated in §5.5's RETAIN
   list. They were exempted from the guard on the same historical-record
   principle already applied to `.squad/session-log.md` and
   `.squad/agents/*/history.md` — already gitignored for future writes,
   narrative past tense, dated filenames. This is a judgment call, not a
   §5.5 citation, and is flagged here explicitly for independent-reviewer
   confirmation or override, per this task's instruction to report
   material ambiguities rather than decide them silently.

### 13.6 U-07 closed as moot

`validation.md` U-07 ("whether the E2E suite passes once #89's collection
crash and fixture drift are fixed") is closed as moot: the suite no longer
exists as an executable contract, so the question has no remaining subject.

### 13.7 Issue #89

Closed by `briandenicola` prior to this session (state reason
`NOT_PLANNED`), with no evidence comment recorded at close time. A durable
retirement + migration evidence comment, citing this document and
`validation.md` AC-005, was added at
<https://github.com/briandenicola/tech-inventory/issues/89#issuecomment-5515917506>.
The issue was not reopened — it was already in the state the user wanted;
only the missing evidence record was added.

### 13.8 Validation run

`node scripts/check-stale-playwright-references.mjs` → 0/901; `node --test
scripts/check-stale-playwright-references.test.mjs` → 15/15; clean
`pnpm install --frozen-lockfile` → 0 downloads; `dotnet build -c Release` →
0 errors; `dotnet test -c Release` → 278 unit + 292 integration passed (4
pre-existing skips, unrelated to this task); `pnpm run lint` → clean;
`pnpm test -- --run` → 83 files / 649 tests passed, including
`DeviceForm.test.ts` 27/27 (confirming the §13.4 known-issues.md correction
is real, not just claimed).

### 13.9 T101 Revision — Apone, 2026-09-02 (closing Ripley's B1 and B2)

Ripley rejected T101 (`validation.md` §7) with two blockers. Hudson was
locked out of the revision cycle; Apone (Tester/QA) revised. Nothing sound
from T101/T102 was touched — no repair of the retired harness, no ADR-gated
`constitution.md`/`docs/prd.md` edit, T104 not started.

**B1 — `.github/T47-CI-SETUP-CHECKLIST.md` stale `task test:e2e` /
`task test:e2e:run` instructions.** Fixed without claiming T104's future
unified verification surface already exists:
- The "Full verification" command block's `task verify` comment now says
  what `verify.sh`/`verify.ps1` step 9/9 actually runs today (the
  stale-reference guard) instead of "E2E".
- The `### "API readiness check failed…"` / `task test:e2e` /
  `task test:e2e:run` troubleshooting block (coverage-migration.md §5.2's
  required "E2E troubleshooting section" removal, left undone by Hudson) is
  removed, replaced by an explicit dated note naming what was removed and why,
  with no restored replacement invented and T104 named as the open scope.
- The Phase 1 Quality Gate Summary and the Workflow Files reference table
  both had their `verify.sh` description corrected off "E2E" to the
  actual current pipeline (format → build → tests → vuln scan → frontend
  check/lint → stale-reference guard), with a new table row naming the guard
  as the E2E stage's replacement.

**B2 — the guard's blanket `specs/` exemption hid live `specs/_backlog/**`
Playwright promises.** `scripts/check-stale-playwright-references.mjs`'s
`EXEMPT_PATH_PREFIXES` no longer contains `specs/`; a new `EXEMPT_SPEC_PATHS`
allowlist names only the twelve historical/work-package files enumerated in
§5.5 (six `specs/004-agentic-development-foundation/*.md`, two
`specs/001-core-api/*.md`, three `specs/002-frontend-mvp/*.md`, one
`specs/003-pwa-polish/tasks.md`). Any other `specs/**` path — every
`specs/_backlog/**` file and any future `specs/0XX-*` package — is no longer
exempt and fails the guard on a Playwright reference exactly like any other
tracked file.

A repository-wide case-insensitive audit (not limited to Ripley's ten-file
sample) found **16** `specs/_backlog/**` files carrying a reference —
`F020`, `F020b`, `F021`, `F021b`, `F022`, `F023`, `F024`, `F024b`, `F026`,
`F027`, `F028`, `F029`, `F030`, `F031`, `F034`, `F045`. Every reference in
every one of the 16 was rewritten to name a concrete post-retirement
destination — HTTP integration test, Vitest component test, a manual
`docs/testing/manual-pwa-validation.md` addition, or a declared accepted gap
in the **G-09** family — never left as an unautomatable promise. Three
already-shipped features (`F021`, `F024`, `F034`) had their checkbox
**checked** because the described behaviour is already covered by an
existing, verified T102 test (cited by name); nothing was checked without
first confirming the test exists (`coverage-migration.md` §5.5a has the
full per-file table). Zero `playwright` references remain anywhere under
`specs/_backlog/**`, confirmed by both a repository-wide
case-insensitive text search and the live guard run below.

**Guard tests.** `scripts/check-stale-playwright-references.test.mjs` grew
from 15 to **18** cases: the old "exempts specs/…" test was split so it no
longer asserts a `specs/_backlog/**` fixture passes; two new cases assert
(a) a synthetic `specs/_backlog/F031-merge-reference-data.md` fixture
containing a Playwright line **fails** the guard, and (b) an arbitrary
`specs/001-core-api/spec.md` / a hypothetical future `specs/005-*/plan.md`
— neither on the named allowlist — **also fails**, closing the "a brand new
`specs/` file… would sail through" hole Ripley flagged. `node --test
scripts/check-stale-playwright-references.test.mjs` → **18/18 pass**.

**Live guard run.** `node scripts/check-stale-playwright-references.mjs` →
`0 active Playwright references across 901 tracked file(s)`.

**Tamper test (backlog branch, this revision).** Appended
`` - [ ] Add a new Playwright journey covering merge undo. `` to the already
committed-in-this-revision `specs/_backlog/F031-merge-reference-data.md`.
Guard → **exit 1**, `specs/_backlog/F031-merge-reference-data.md:130`.
Restored via a pre-tamper backup copy; `git hash-object` on the file was
**`5517f1f9b0a57ca6c80c36c4367b75383f8e25c0` before appending and after
restoring — byte-identical**. Guard back to **0/901**. Ripley's prior
config-branch and tree-branch structural tamper tests (`validation.md` §7.1)
are untouched by this revision and were not re-run — this revision only
changed the keyword-scan exemption list, not `isReintroducedHarnessPath`.

**Repository-wide reference count.** 35 files (case-insensitive, all of
disk, not just `git ls-files`, since `specs/004-agentic-development-foundation/`
is currently untracked): every one classifies as (a) an explicit guard
exemption (`.copilot-state.md`, `.env.e2e`, `constitution.md`, `docs/prd.md`,
`.squad/decisions*`, `.squad/agents/*/history.md` ×6, `SESSION-NOTES.md`,
`docs/testing/manual-pwa-validation.md`, `docker-compose.e2e.yml`, the 12
named historical spec files, the guard's own script + test file, the
`pnpm-lock.yaml` peer-dependency line), or (b) a file that only names the
guard's own filename as a required self-reference (`.github/T47-CI-SETUP-CHECKLIST.md`,
`.github/workflows/README.md`, `scripts/verify.sh`, `scripts/verify.ps1`,
`Taskfile.yml` — individually confirmed no non-filename "playwright" text
remains in any of the five). **Zero** files fall outside these two
categories. `.specify/memory/constitution.md` and `docs/prd.md` remain
untouched, per instruction — their amendment is Ripley's recorded
package-closure precondition (`validation.md` §7.3.3), not this revision's
scope.

**Not touched:** Hudson's T101 work in §13.1–13.7 above, T102's completion
record (§12), any application/product code, `constitution.md`, `docs/prd.md`.
T104 was not started.

**Full record:** `.squad/agents/apone/history.md` (learnings),
`.squad/decisions/inbox/apone-t101-revision.md` (team decision record).
Re-review requested from Ripley.


### 13.10 T101 Re-review — Ripley, 2026-09-02 — **APPROVED, T101 `DONE`**

The independent reviewer re-ran the evidence rather than accepting §13.9.
Full verdict: `validation.md` §7.8.

- **B1 closed** — `.github/T47-CI-SETUP-CHECKLIST.md` re-read end to end: no
  `task test:e2e` / `task test:e2e:run`, no readiness-poll troubleshooting
  block, and the `verify` descriptions match `verify.sh:47-49` /
  `verify.ps1:54-57` step 9/9 as it actually runs. No pre-claim of T104.
- **B2 closed** — blanket `specs/` prefix gone; all twelve `EXEMPT_SPEC_PATHS`
  entries audited and confirmed closed-phase records (`001-core-api` `P1
  COMPLETE`; `002-frontend-mvp` `Shipped`; `003-pwa-polish/tasks.md:159` a
  single dated log line). Independent whole-disk scan: **56** files carry the
  word, **39 tracked**, **zero** under `specs/_backlog/**` and zero in any
  manifest, script, workflow, config, instruction, or executable test.
  §13.9's "35 files" is understated — the classification holds, the count
  does not (`validation.md` §7.8.6 F-2).
- **Checkbox flips verified against real code by name**, not by citation:
  `AuditEventsAuthorizationTests.cs:18`, `ViewerRoleAuthorizationTests.cs:104`,
  `DevicesControllerTests.cs:556`, `SharePointCsvImportTests.cs:25` (asserting
  `Model`/`Purpose` at `:62-63`), `BulkActionBar.test.ts:36,41`,
  `DeviceTable.test.ts:102-127`, `groupDevices.test.ts`. No invented coverage.
- **Reviewer-run evidence:** guard **0/901**; guard tests **18/18**; three
  tamper tests — an active `specs/_backlog/**` file (F026, deliberately not
  Apone's F031), a brand-new unlisted `specs/005-*` path, and a structural
  `tests/e2e/` revival — each exit 1 with the exact file:line, each restored
  byte-identically (`git hash-object` match) and `git status --porcelain`
  diffed to **0 lines** against the pre-review baseline.
- **Consequence:** T101 is `DONE`, AC-005 is met, and **T104 is authorized to
  begin** — conditional on `validation.md` §7.8.5: T104 must not claim its
  verification surface satisfies the constitution's still-standing Playwright
  mandate, and must carry the ADR package-closure precondition forward
  visibly. Findings F-1..F-4 (`validation.md` §7.8.6) are carried to the F045
  owner, T104, and T105.

## 14. T104 Completion Record — Hudson, 2026-09-02 — `VALIDATING` (**REJECTED, then REVISED by Hicks, pending re-review**)

> **Reviewer gate 2026-09-02 — Apone: REJECTED** (`validation.md` §10).
> The file-level record below is accurate as a description of what was
> changed. Three blockers prevented AC-008 from being met: B-1
> `check:client-drift` failed with provably zero drift, so `task verify` had
> never been observed to complete; B-2 `check:vulnerable` could not fail while
> `.github/workflows/README.md:161` documented it as "Enforced"; B-3 `ci.yml`
> depended on PyYAML without installing it. The collected-test floors were
> tamper-tested by the reviewer and **approved** (`validation.md` §10.2).
> **Revision owner Hicks closed all three blockers 2026-09-02 — see §14.4.
> Not self-approved; awaiting Apone re-review.**

Full acceptance evidence is `validation.md` §9; this section records the
exact file-level changes for reviewer convenience and does not duplicate
that evidence. **Not self-marked `DONE`** — `plan.md` §2.3 reserves that
transition for a human/independent reviewer.

### 14.1 Files changed

**Created:** `scripts/check-test-floors.mjs`, `.config/dotnet-tools.json`.

**Rewritten:** `Taskfile.yml` (added `build:backend`/`build:frontend`,
`check:format`/`check:frontend`/`check:client-drift`/`check:openapi-drift`/
`check:migration-drift`/`check:vulnerable`, rewrote `test:unit`/
`test:integration`/added `test:frontend`, added `verify:fast`/
`verify:contracts`/`verify:full`/`verify`); `scripts/verify.ps1` and
`scripts/verify.sh` (both now thin `task verify` wrappers, replacing the old
9-step duplicated pipelines); `.github/workflows/quality-gate.yml` (the
`dotnet` and `web` jobs collapsed into one `verify` job calling
`task verify`); `.github/workflows/ci.yml` (installs Task, header comment
corrected); `.github/workflows/README.md` (full rewrite describing the
Task-based pipeline and measured floors).

**Deleted:** `.env.e2e`, `docker-compose.e2e.yml` (no real non-browser role
— confirmed via grep, all HTTP integration tests are in-process
`WebApplicationFactory<Program>`, no Docker); `scripts/check-openapi-drift.sh`
(logic duplicated `Taskfile.yml`'s `check:openapi-drift`; nothing else
referenced it outside itself and one historical citation in `evidence.md`,
left untouched).

**Small edits:** `scripts/check-stale-playwright-references.mjs` (removed
the now-dangling `.env.e2e`/`docker-compose.e2e.yml` exemption entries;
header comment corrected to record that T104 deleted rather than revised
them); `scripts/check-stale-playwright-references.test.mjs` (flipped the
corresponding test from "exempts" to "does NOT exempt", locking in the new
behaviour as a regression guard, test count still 18/18); `.gitleaks.toml`
(removed the dead `.env.e2e` allowlist path entry); `docker-compose.yml`
(corrected a stale comment naming `task test:e2e`, which no longer exists).

### 14.2 What T104 did not touch

`.specify/memory/constitution.md`, `docs/prd.md` — untouched, per the
standing condition that T104 may not deepen or claim to resolve the
Playwright-mandate contradiction (`validation.md` §7.8.5). No T101/T102
file, test, or the manual PWA checklist was modified. No GitHub repository
setting (branch protection, required checks, CODEOWNERS) was read or
changed — that is T105's scope. No commit was made and nothing was pushed,
per this task's explicit instruction; the working tree is left for
independent review.

### 14.3 Guard re-verification after T104's file deletions

Live guard run after all T104 edits: **`0 active Playwright references
across 898 tracked file(s)`** (down from 901, matching the three files T104
deleted: `.env.e2e`, `docker-compose.e2e.yml`, `scripts/check-openapi-drift.sh`).
Guard test suite: **18/18** (`check-stale-playwright-references.test.mjs`)
after retargeting the `.env.e2e`/`docker-compose.e2e.yml` test case.

Full detail, measured test floors, Docker-requirement statement, and the
validation-run transcript are in `validation.md` §9.

### 14.4 Revision — Hicks, 2026-09-02 — closes B-1/B-2/B-3

Full acceptance evidence for the revision is `validation.md` §11; this
subsection records the exact file-level changes for reviewer convenience.

**Created:** `scripts/check-client-drift.mjs` (+ `scripts/check-client-drift.test.mjs`,
9/9 passing), `scripts/check-vulnerable.mjs` (+ `scripts/check-vulnerable.test.mjs`,
13/13 passing).

**Rewritten (task bodies only, not wholesale):** `Taskfile.yml`'s
`check:client-drift` target now runs `node scripts/check-client-drift.mjs`
(previously `pnpm run generate:client` + `git diff --exit-code` against
index/HEAD); `check:vulnerable` now runs `node scripts/check-vulnerable.mjs`
(previously the bare, unparsed `dotnet list package --vulnerable
--include-transitive`). No other task in `Taskfile.yml` was touched.

**Small edits:** `.github/workflows/ci.yml` (one new step — installs PyYAML
before "Run verification pipeline", identical to `quality-gate.yml`'s
existing step); `.github/workflows/README.md` (`verify:contracts` step list,
the Security Gates table's vulnerability-scan row, and debugging-guide items
4 and 8 corrected to describe the new fail-closed/snapshot-restore
behaviour instead of the old ceremonial/index-HEAD-mismatched descriptions).

**Not touched:** any T101/T102 file, the manual PWA checklist, any
application/product code, `constitution.md`, `docs/prd.md`, or any of
Apone's non-blocking findings F-5–F-9 (none was a prerequisite to closing
B-1/B-2/B-3). No GitHub repository setting was read or changed — still
T105's scope, and T105 is still not authorized to begin. No commit was made
and nothing was pushed.

**Verification, this machine, Windows, no Docker, no browser:** `task
verify:fast`, `task verify:contracts`, `task verify:full`, and `task verify`
(the authoritative alias) all ran to completion and **exited 0** — the first
time in this work package that the authoritative entrypoint has been
observed to complete. `check:client-drift` and `check:vulnerable` were each
also live-tamper-tested directly (a real stale-client edit caught and
restored; a real vulnerable-package probe returning exit 1) in addition to
their new unit-test suites. Full transcript: `validation.md` §11.5.

GitHub Actions execution remains unobserved until a push — disclosed, not
claimed.

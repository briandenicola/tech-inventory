# Tasks — 002 Frontend MVP + Real Entra OIDC

**Spec**: `specs/002-frontend-mvp/spec.md`
**Plan**: `specs/002-frontend-mvp/plan.md`
**Status**: Draft

---

## Task List

### Round 0: Foundation + Client Generation

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T01 | [Brian] Entra App Registration | Brian | Register Tech Inventory app in household Workforce Entra tenant. Define app roles: `admin`, `member`, `viewer`. Configure redirect URI: `https://localhost:3000/auth/callback` (dev) + production URL. | App client ID + tenant ID available; app roles defined; PR with IDs in `appsettings.json` merged. | — | Spec §7.2, D-001 |
| ✅ T02 | [P] Generate TypeScript API client | Vasquez | Generate TS client from `openapi.yaml` using `openapi-typescript-codegen`. Output to `src/TechInventory.Web/src/lib/api/generated/` (gitignored). Add `package.json` script `generate:client`. Wire base URL + auth headers via `src/lib/api/client.ts`. | Client generated; imports work; `pnpm run generate:client` succeeds; wrapper configures MSAL token injection. | — | Spec §4.2 |
| ✅ T03 | [P] Expand design tokens | Vasquez | Expand `src/lib/tokens.css` to ~100 CSS custom properties: color scales (primary, secondary, error, warning, success), spacing (4px grid), typography (font families, sizes, weights, line-heights), border radius, shadows, transitions. | Tokens cover all UI needs; Tailwind config consumes tokens; ESLint `no-arbitrary-values` enforces token usage. | — | Spec §4.4 |
| ✅ T04 | [P] Expand i18n catalog | Vasquez | Populate `src/lib/i18n/en.json` with ~200 keys covering: auth (sign in, sign out, errors), device list/create/edit/delete, reference entities, import/export, common UI (save, cancel, confirm, loading, empty states). | Catalog covers all Phase 2 UI strings; ESLint `no-hardcoded-strings` rule enforces catalog usage. | — | Spec §4.5 |
| ✅ T05 | Configure MSAL.js for Entra Workforce | Vasquez | Update `src/lib/auth/msal.ts` with Workforce tenant authority, client ID (from T01), redirect URIs, scopes (`openid profile email offline_access`). Configure `sessionStorage` cache location. Wire PKCE. | MSAL config complete; `pnpm run check` passes; no `localStorage` usage (ESLint gate active). | T01 | Spec §4.1, D-010 |
| ✅ T05a | App icon design + PWA/Entra asset set | Drake | Design master SVG icon — household tech inventory concept (house silhouette with device-grid interior). Rasterize to: `icon-240.png` (Entra portal upload), `icon-192.png`, `icon-512.png`, `icon-maskable-512.png` (PWA per D-036), `favicon.ico` + `favicon.svg`. Land all assets in `src/TechInventory.Web/static/icons/`. Update `static/manifest.webmanifest` icon entries. Commit the rasterization script for reproducibility. | All icon assets in `static/icons/`; manifest references them; PWA manifest passes Chrome DevTools "App" panel validation; Brian can upload `icon-240.png` to Entra app registration "Branding & properties" successfully. | — | Spec §4.4, D-036, Constitution §6.5.8 |

---

### Round 1: Backend Auth Swap (Bishop)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T06 | Add JWT Bearer validation middleware | Bishop | Wire `JwtBearerDefaults.AuthenticationScheme` in `Program.cs`. Configure authority (`login.microsoftonline.com/{tenant-id}/v2.0`), audience (client ID), token validation params (issuer/audience/lifetime/signature). Add authorization policies: `Admin`, `Member`, `Viewer` (based on `app_roles` claim). | JWT validation active; `Auth:DevBypass=false` in `appsettings.json`; integration tests pass with synthetic JWT. | T01 | Spec §7.2 |
| ✅ T07 | Implement `HttpContextCurrentUserService` | Bishop | Replace `SystemCurrentUserService` with `HttpContextCurrentUserService`. Extract `oid` from `ClaimsPrincipal`, look up `Owner` by `EntraObjectId`. Cache per request (scoped service). Implement first-login onboarding: create `Owner` if not found (extract `oid`, `email`, `name`, `app_roles[0]` from JWT). | Current user service wired; audit metadata stamps with real user; first-login creates `Owner` record; `AuditEvent` logged for first login. | T06 | Spec §7.3 |
| ✅ T08 | Integration tests for JWT validation | Apone+Bishop | Add integration tests: valid JWT → 200 OK; expired JWT → 401; invalid signature → 401; missing `app_roles` → 401; empty `app_roles` → 401; `admin` role can access Admin-only endpoints; `viewer` role blocked from write endpoints. | 6/11 auth tests green (all negative cases + dev-bypass paths); 5 happy-path tests deferred — see `docs/known-issues.md#auth-jwt-happy-path-tests`. JWT token factory helper reusable. | T06, T07 | Spec §6.1 |

---

### Round 2: Login + Protected Route Shell (Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T09 | Wire MSAL.js login/logout flow | Vasquez | Implement login button (`+page.svelte` or `+layout.svelte`). Call `msalInstance.loginRedirect()` on click. Handle redirect callback in `/auth/callback` route. Store JWT in sessionStorage via MSAL. Implement logout: clear sessionStorage + call `msalInstance.logoutRedirect()`. | Login redirects to Entra; callback extracts token; sessionStorage contains JWT; logout clears session. | T05, T06 | Spec §4.1, J1, J3 |
| ✅ T10 | Create auth store (current user context) | Vasquez | Svelte store `src/lib/stores/auth.ts`: `currentUser` (id, email, displayName, role), `isAuthenticated`, `isLoading`. Populate on mount via `/api/v1/owners/me` call (new endpoint — see T11). | Auth store populated; components can subscribe to `$currentUser`; role available for UI adaptation. | T09 | Spec §4.3 |
| ✅ T11 | [Backend] Add `/api/v1/owners/me` endpoint | Bishop | New endpoint: `GET /api/v1/owners/me` returns `OwnerResponse` for current user (`ICurrentUserService` → `Owner` lookup). | Endpoint returns current user; integration test green. | T07 | Spec §7.3 |
| ✅ T12 | Protected route guard | Vasquez | SvelteKit load function (`+page.ts` or `+layout.ts`) checks `$currentUser.isAuthenticated`. Redirect to `/auth/login` if not authenticated. Optionally check role (e.g., Admin-only routes redirect non-admins to `/403`). | Unauthenticated users redirected to login; authenticated users see protected content; role-based redirects work. | T10 | Spec §5, J13 |
| ✅ T13 | App shell layout (header + nav) | Vasquez | Create `+layout.svelte`: header with logo, user display name, sign-out button. Nav links (Devices, Import, Export, Admin — visibility based on role). Mobile hamburger menu. Footer with version + links. | App shell renders; nav adapts to role; mobile-responsive; axe-core zero violations. | T10, T12 | Spec §4.4, §6.1 |

---

### Round 3: Devices List Page (Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T14 | Devices list query hook | Vasquez | `src/lib/queries/devices.ts`: `useDevices(filters)` hook. Call `/api/v1/devices` via generated client. Return paginated response. Handle loading/error states. | Query hook works; loading/error states propagate; cache invalidates on mutation. | T02 | Spec §4.3 |
| ✅ T15 | Devices list page (`/devices`) | Vasquez | Render paginated table: columns (Name, Brand, Category, Owner, Status, Purchase Date). Empty state if no devices. Loading skeleton. Pagination controls (prev/next, page size selector). | List renders; empty state; loading skeleton; pagination works; <200 lines component. | T14 | Spec §5, J4 |
| ✅ T16 | Filters sidebar (search + facets) | Vasquez | Sidebar: search input (name/serial free-text), brand dropdown, category dropdown, owner dropdown, location dropdown, network dropdown, status multi-select, purchase year range. URL-backed via `$page.url.searchParams`. | Filters apply; URL updates; reload preserves filters; dropdowns populated from reference data. | T14, T15 | Spec §5, J4 |
| ✅ T17 | Sort controls (column headers) | Vasquez | Sortable column headers: Name, Purchase Date, Created At. Click toggles asc/desc. URL-backed sort state. | Sort works; URL reflects sort; reload preserves sort; icon indicates direction. | T15 | Spec §5, J4 |
| ✅ T18 | Device list component tests | Apone | Vitest + Testing Library: test loading state, empty state, error state, success state (with mock data), pagination, filters, sort. | 8+ component tests green; axe-core zero violations per state. *Deferrals: 4 E2E-deferred (mobile cards/drawer/focus trap, DeviceFilters complexity).* | T15, T16, T17 | Spec §6.2 |

---

### Round 4: Device Detail + Create + Edit (Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T19 | Device detail page (`/devices/[id]`) | Vasquez | Render device detail: all fields, resolved reference names (brand/category/owner/location/network), tags, audit trail (created/modified timestamps). Breadcrumbs. Edit/Delete buttons (role-aware). | Detail renders; breadcrumbs; role-aware affordances; axe-core zero violations. | T14 | Spec §5, J5 |
| ✅ T20 | Device create page (`/devices/new`) | Vasquez | Form: name, brand (dropdown), serial, category (tree select), owner (dropdown), location (dropdown), network (dropdown), purchase date, purchase price, currency (dropdown with household default pre-filled), notes. Zod schema mirrors backend FluentValidation. Inline validation on blur. | Form renders; validation on blur; household default currency pre-filled; submit creates device; redirect to detail on success. | T02, T14 | Spec §5, J6 |
| ✅ T21 | Device edit page (`/devices/[id]/edit`) | Vasquez | Pre-populate form with existing device data. Retired devices show read-only badge; only notes/disposal editable. Submit updates device. Toast notification on success. | Edit form works; retired-device guard active; toast on success; redirect to detail. | T19, T20 | Spec §5, J7 |
| ✅ T22 | Delete device confirmation modal | Vasquez | Modal: "Type device name to confirm" input. Disabled submit until match. Call `DELETE /api/v1/devices/{id}` with reason in body. Toast on success; redirect to list. | Modal works; confirmation required; device soft-deleted; redirect to list; toast shown. | T19 | Spec §5, J8 |
| ✅ T23 | Device form component tests | Apone | Vitest tests: create form validation, edit form pre-population, retired-device edit guard, delete confirmation, currency default. | 6+ component tests green; axe-core zero violations. | T20, T21, T22 | Spec §6.2 |
| | | | | **Note:** 2 DeviceForm submit tests skipped — Svelte 5 `bind:value` on `<select>` jsdom limitation; covered by E2E T46. | | |

---

### Round 5: Ownership Flows (Vasquez)

**Progress:** 3/3 ✅ (T24, T25, T26 — Complete)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T24 | Claim ownership button + modal | Vasquez | On device detail: "Claim Ownership" button (visible if device unowned or owned by another user). Modal: confirm claim. Call `PATCH /api/v1/devices/{id}/owner` with `{ newOwnerId: currentUser.id }`. Toast on success; detail page refreshes. | Button visible; modal confirms; ownership updated; audit trail shows change; toast shown. | T19 | Spec §5, J9 |
| ✅ T25 | Release ownership button + modal | Vasquez | On device detail (if current user is owner): "Release Ownership" button. Modal: confirm release. Call `PATCH /api/v1/devices/{id}/owner` with `{ newOwnerId: null }`. Toast on success. | Button visible; modal confirms; ownership cleared; audit trail shows release; toast shown. | T19, T24 | Spec §5, J9 |
| ✅ T26 | Ownership component tests | Apone | Vitest tests: claim button visibility (role + ownership state), release button visibility, modal confirmation, API call, toast notification. | 5+ component tests green; axe-core zero violations. | T24, T25 | Spec §6.2 |

---

### Round 6: Reference Entity Management UIs (Vasquez + Hicks)

**Progress:** 6/7 ✅ (T27, T28, T29, T30, T31, T32 — Brands/Categories/Locations/Networks/Owners/Tags admin pages complete; T33 partial — 4/6 entity tests, Categories/Owners schema tests follow-up)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| ✅ T27 | Brands admin UI (`/admin/brands`) | Vasquez | Paginated list + "Add Brand" button → modal/page. Form: name, website, notes. Edit: inline or modal. Delete: soft-delete (deactivate). Toggle "Show Inactive". | Brands CRUD works; inactive toggle works; toast on success; axe-core zero violations. | T02 | Spec §5, J12 |
| ✅ T28 | Categories admin UI (`/admin/categories`) | Vasquez | Tree view with expand/collapse. Add (root or child — parent selector). Edit (name, parent, icon). Delete (cascades deactivation to subtree). | Categories CRUD works; tree renders; cascade delete works; axe-core zero violations. | T02 | Spec §5, J12 |
| ✅ T29 | Owners admin UI (`/admin/owners`) | Vasquez | List with role badge. Add: name, email, role, Entra object ID (optional). Edit: role change. Deactivate (blocks if devices still reference). | Owners CRUD works; role badge visible; active-owner delete block works; axe-core zero violations. | T02 | Spec §5, J12 |
| ✅ T30 | Locations admin UI (`/admin/locations`) | Vasquez | List. Add/edit: name, type (Home/Storage/External). Deactivate. | Locations CRUD works; type dropdown works; axe-core zero violations. | T02 | Spec §5, J12 |
| ✅ T31 | Networks admin UI (`/admin/networks`) | Vasquez | List. Add/edit: name, description. Deactivate. | Networks CRUD works; axe-core zero violations. | T02 | Spec §5, J12 |
| ✅ T32 | Tags admin UI (`/admin/tags`) | Vasquez | List. Add/edit: name, color (color picker or preset). Deactivate. | Tags CRUD works; color picker/preset works; axe-core zero violations. | T02 | Spec §5, J12 |
| T33 | Reference entity component tests | Apone | Vitest tests: brands/categories/owners/locations/networks/tags CRUD forms, validation, inactive toggle, delete guards. | 12+ component tests green (2 per entity); axe-core zero violations. | T27-T32 | Spec §6.2 |

**T33 Status:** 4 of 6 entities covered (61 schema tests in brands/locations/networks/tags). Categories + Owners schema tests deferred to follow-up Apone round (Zod schemas exist at `src/lib/schemas/{category,owner}.ts` per D-116, awaiting test coverage).

| T33 | Reference entity component tests | Apone | Vitest tests: brands/categories/owners/locations/networks/tags CRUD forms, validation, inactive toggle, delete guards. | 12+ component tests green (2 per entity); axe-core zero violations. | T27-T32 | Spec §6.2 |

---

### Round 7: Import Wizard (Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T34 | Import page shell (`/admin/import`) | Vasquez | Step 1: File upload (drag-drop or button). Client-side CSV preview (first 10 rows). "Next" button → preview validation. | Upload works; drag-drop works; preview renders; 10MB size limit enforced (client-side); axe-core zero violations. | T02 | Spec §5, J10 |
| T35 | Preview validation step | Vasquez | Call `POST /api/v1/imports/preview` with uploaded file. Display: valid rows count, invalid rows (with error messages), missing lookups (brands/categories/owners/locations to create). "Commit" button. | Preview renders; row-level errors shown; missing lookups listed; "Commit" button enabled if valid rows exist. | T34 | Spec §5, J10 |
| T36 | Commit import step | Vasquez | Call `POST /api/v1/imports/commit`. Show loading spinner. On success: display `ImportBatch` summary (processed count, error count). Link to devices list. Toast notification. | Commit works; spinner shows; summary renders; redirect to list; toast shown. | T35 | Spec §5, J10 |
| T37 | Import wizard component tests | Apone | Vitest tests: file upload, preview validation, commit, error states, empty state, 10MB limit, loading state. | 7+ component tests green; axe-core zero violations. | T34, T35, T36 | Spec §6.2 |

---

### Round 8: Export Downloads (Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T38 | Export page (`/admin/export`) | Vasquez | Format selector (CSV/JSON radio buttons). Filter panel (same as devices list: brand, category, status, search, year range). "Download" button. Call `GET /api/v1/exports/devices?format=csv&...filters`. Browser downloads file. Toast on success. | Export page works; filters apply; CSV/JSON downloads; filename includes timestamp; toast shown. | T02, T16 | Spec §5, J11 |
| T39 | Export component tests | Apone | Vitest tests: format selection, filter application, download trigger, error state, empty state. | 5+ component tests green; axe-core zero violations. | T38 | Spec §6.2 |

---

### Round 9: Polish (Vasquez + Apone)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T40 | Empty states for all routes | Vasquez | Design + implement empty states: Devices list (no devices), Brands list (no brands), Import (no files uploaded), Export (no devices to export). Illustration + message + CTA button. | Empty states on all routes; illustrations consistent; axe-core zero violations. | T15-T38 | Spec §6.2 |
| T41 | Error states for all routes | Vasquez | Design + implement error states: API call failed (retry button), 404 (device not found), 403 (unauthorized), network error. User-safe messages (never leak server internals). | Error states on all routes; retry buttons work; messages user-safe; axe-core zero violations. | T15-T38 | Spec §6.2 |
| T42 | Loading skeletons | Vasquez | Replace loading spinners with skeleton loaders: Devices list (table skeleton), Device detail (card skeleton), Reference entity lists. | Skeletons on all routes; consistent design; axe-core zero violations. | T15-T32 | Spec §6.2 |
| T43 | Toast notification system | Vasquez | Svelte store `src/lib/stores/toast.ts`: queue of notifications (message, type: success/error/warning/info, duration). Component `Toast.svelte` displays toasts (auto-dismiss after 5s; manual close button). Wire to all mutation success/error paths. | Toast system works; auto-dismiss; manual close; multiple toasts queue; axe-core zero violations (ARIA live region). | All prior | Spec §6.2 |
| T44 | Axe-core sweep (all routes) | Apone | Run axe-core on every route (via Playwright or component tests). Fix violations: missing labels, insufficient contrast, missing ARIA, non-keyboard-operable elements. | Zero axe-core violations on all routes. | T15-T43 | Spec §6.1, §6.2 |

---

### Round 10: E2E Hardening (Apone)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T45 | E2E: Auth journeys (J1, J2, J3, J13) | Apone | Playwright tests: `tests/e2e/auth/sign-in.spec.ts`, `sign-in-denied.spec.ts`, `sign-out.spec.ts`, `role-enforcement.spec.ts`. Page Object Model for auth fixtures. Run on 6 projects (3 browsers × 2 viewports). | 4 auth E2E tests green on all 6 projects. | T09-T13 | Spec §5, J1-J3, J13 |
| T46 | E2E: Device CRUD journeys (J4-J8) | Apone | Playwright tests: `tests/e2e/devices/browse-list.spec.ts`, `view-detail.spec.ts`, `create-device.spec.ts`, `edit-device.spec.ts`, `delete-device.spec.ts`. POM for device pages. | 5 device E2E tests green on all 6 projects. | T15-T22 | Spec §5, J4-J8 |
| T47 | E2E: Ownership journey (J9) | Apone | Playwright test: `tests/e2e/devices/claim-ownership.spec.ts`. Claim + release scenarios. | 1 ownership E2E test green on all 6 projects. | T24, T25 | Spec §5, J9 |
| T48 | E2E: Import journey (J10) | Apone | Playwright test: `tests/e2e/admin/import-csv.spec.ts`. Upload CSV, preview, commit. Verify devices created. | 1 import E2E test green on all 6 projects. | T34-T36 | Spec §5, J10 |
| T49 | E2E: Export journey (J11) | Apone | Playwright test: `tests/e2e/admin/export-data.spec.ts`. Select format, apply filters, download, verify file contents. | 1 export E2E test green on all 6 projects. | T38 | Spec §5, J11 |
| T50 | E2E: Reference entity journey (J12) | Apone | Playwright test: `tests/e2e/admin/manage-brands.spec.ts` (+ categories, owners, locations, networks, tags). CRUD for each entity. | 6 reference entity E2E tests green on all 6 projects. | T27-T32 | Spec §5, J12 |

---

### Round 11: PWA + Lighthouse CI (Hudson + Vasquez)

| ID | Task | Owner | Description | Definition of Done | Dependencies | Ref |
|----|------|-------|-------------|-------------------|--------------|-----|
| T51 | PWA manifest + icons | Vasquez | Create `static/manifest.webmanifest`: name, short_name, icons (192, 512, maskable), theme_color, background_color, display=standalone. Generate icons (maskable + standard). | Manifest valid; icons present; installable on iOS/Android/desktop. | — | Spec §6.1, Constitution §6.5.8 |
| T52 | Service worker (offline app shell) | Vasquez | Use `vite-plugin-pwa`. Precache app shell (HTML, CSS, JS chunks). Runtime cache for API GETs (stale-while-revalidate, 5-min TTL). Offline page for uncached navigation. Update prompt on new SW. | SW precaches shell; offline page works; update prompt shown; mutations fail gracefully offline. | T51 | Spec §6.1, Constitution §6.5.8 |
| T53 | Lighthouse CI on critical routes | Hudson | Add Lighthouse CI to workflow. Run on: `/devices` (list), `/devices/{id}` (detail). Assert: Performance ≥ 90, Accessibility = 100, Best Practices ≥ 95, SEO ≥ 90. | Lighthouse CI passes on both routes; CI fails if scores drop below thresholds. | T15, T19 | Spec §6.2, Constitution §6.5.9 |

---

## Task Count: **53 tasks**

### Round Breakdown:
- **Round 0 (Foundation)**: 6 tasks (T01-T05, T05a) — [Brian] + [P] parallel
- **Round 1 (Backend Auth)**: 3 tasks (T06-T08) — Bishop
- **Round 2 (Login + Shell)**: ✅ 5 tasks (T09-T13) — Vasquez + Bishop
- **Round 3 (Devices List)**: 5/5 ✅ (T14-T18 complete; T18 with 4 documented E2E deferrals) — Vasquez + Apone
- **Round 4 (Device CRUD)**: 5/5 ✅ (T19-T23 complete; T23 with 2 documented E2E deferrals) — Vasquez + Apone
- **Round 5 (Ownership)**: 2/3 ✅ (T24-T25 complete; T26 component tests queued for Apone) — Vasquez + Apone
- **Round 6 (Reference Entities)**: 7 tasks (T27-T33) — Vasquez + Apone
- **Round 7 (Import)**: 4 tasks (T34-T37) — Vasquez + Apone
- **Round 8 (Export)**: 2 tasks (T38-T39) — Vasquez + Apone
- **Round 9 (Polish)**: 5 tasks (T40-T44) — Vasquez + Apone
- **Round 10 (E2E)**: 6 tasks (T45-T50) — Apone
- **Round 11 (PWA + Lighthouse)**: 3 tasks (T51-T53) — Vasquez + Hudson

---

## Cast Assignments

| Agent | Task Count | Scope |
|-------|------------|-------|
| **Vasquez** (Frontend) | 33 | TS client, design tokens, i18n, MSAL config, login/logout, app shell, devices list/create/edit/delete, ownership flows, reference entity UIs (6), import wizard, export, polish (empty/error/loading states, toast), PWA manifest + service worker |
| **Bishop** (Backend Auth) | 4 | JWT bearer validation, `HttpContextCurrentUserService`, `/api/v1/owners/me`, integration tests |
| **Apone** (QA / E2E) | 12 | Component tests (devices, forms, ownership, reference entities, import, export), axe-core sweep, E2E (auth, devices CRUD, ownership, import, export, reference entities) |
| **Hudson** (DevOps) | 1 | Lighthouse CI |
| **Brian** | 1 | [Manual] Entra app registration |
| **Hicks** (Backend) | 2 | (Optional) Backend support for T27-T32 if new endpoints needed |

---

## Dependency Order

```
T01 (Entra app registration) blocks T05, T06
T02 (TS client) blocks T14, T20, T27-T32, T34, T38
T05 (MSAL config) blocks T09
T06 (JWT validation) blocks T07, T08, T09
T07 (`HttpContextCurrentUserService`) blocks T08, T10, T11
T09 (login/logout) blocks T10, T12, T13
T10 (auth store) blocks T12, T13
T14 (devices query) blocks T15, T16, T17, T19, T20
T15 (list page) blocks T16, T17, T18
T19 (detail page) blocks T20, T21, T22, T24, T25
T20 (create form) blocks T21, T23
T27-T32 (reference UIs) can run in parallel
T34 (import shell) blocks T35, T36, T37
T38 (export page) blocks T39
All UI tasks (T15-T43) block T44 (axe sweep)
All UI tasks block T45-T50 (E2E)
T51 (manifest) blocks T52 (service worker)
T15 + T19 block T53 (Lighthouse CI)
```

---

## Notes

- **[Brian]** = Manual task requiring Brian's hands (GitHub UI, Entra portal, DNS, TLS).
- **[P]** = Parallelizable with peers in the same round.
- **Scopes disjoint**: Vasquez owns frontend; Bishop owns backend auth; Apone owns test authorship; Hudson owns CI.
- **Backend API is complete** (Phase 1 48/48 done); no new endpoints needed except T11 (`/api/v1/owners/me`).
- **Round 0 open questions** (Spec §8) must be answered before launching Round 2+.
- **Phase 2 definition of done**: All 53 tasks complete; 13 critical E2E journeys green on 6 browser projects; Lighthouse CI passing on 2 routes; zero axe-core violations; PWA installable.

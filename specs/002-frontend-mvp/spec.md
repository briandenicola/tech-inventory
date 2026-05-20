# Spec 002 — Frontend MVP + Real Entra OIDC

**Status**: Shipped (production-validated 2026-05-19)
**Phase**: Canonical **P2 — Frontend MVP + Auth** under PRD §13 as rewritten 2026-05-19. *(Originally drafted as "Phase 2 (Weeks 6–9) per PRD §13" under the pre-rewrite numbering; references to other "Phase N" labels inside this document reflect the original plan and are preserved as historical record. Work that this spec deferred to "Phase 3 / Phase 4" is now tracked under **P4 — Continuous Iteration** via `specs/_backlog/`.)*
**Owner**: Ripley (Lead/Architect)
**Last Updated**: 2026-05-18

---

## 1. Overview

Phase 2 delivers the **SvelteKit PWA frontend** consuming the Phase 1 API, replacing the development auth bypass with **real Microsoft Entra ID (Workforce tenant) OIDC + PKCE authentication**. This phase makes the Tech Inventory application usable by Brian and family members through a modern, accessible, mobile-first web interface while establishing the production authentication surface.

---

## 2. Goals & Non-Goals

### 2.1 Goals

- **Real Entra ID Authentication**: MSAL.js-based OIDC + PKCE flow with Workforce tenant (per D-001)
- **Generated TypeScript API Client**: From `openapi.yaml` — no hand-written fetch calls
- **Core Device Management UI**: List (paginated + filterable + sortable), create, edit, detail, soft-delete
- **Reference Entity Management**: Full CRUD UIs for Brands, Categories, Owners, Locations, Networks, Tags
- **Import Wizard**: CSV preview → validation feedback → commit flow
- **Export Downloads**: CSV/JSON exports for devices and reference data
- **Ownership Flows**: Claim/release device ownership with audit trail visibility
- **Installable PWA**: Manifest + service worker for offline app shell (online-only mutations)
- **Accessibility Baseline**: Zero axe-core violations on every route; WCAG 2.2 AA compliance
- **E2E Coverage**: Playwright tests for all 13 critical user journeys (PRD §7.5.4)
- **Role-Based UI Adaptation**: Admin/Member/Viewer affordances matched to backend RBAC

### 2.2 Non-Goals (Deferred to Phase 3+)

- ❌ Advanced offline support — queue mutations for replay (v2)
- ❌ Photo attachment upload/viewing (v2 — PRD §15)
- ❌ Timeline view / "Tech Eras" visualization (Phase 4 polish)
- ❌ Saved views per user (Phase 4)
- ❌ Warranty expiration reminders (v2)
- ❌ Bulk edit UI (v2 — API bulk operations exist, UI deferred)
- ❌ Visual regression testing (Phase 5 hardening)
- ❌ Admin user management UI — role assignment via Entra portal only (v1.1)
- ❌ Dashboard / metrics / charts (Phase 4)
- ❌ Dark mode toggle (v1.1 — respect `prefers-color-scheme` only)
- ❌ Service worker precache for API responses (Phase 3)

---

## 3. Constraints from Constitution

> "API-first: every capability via versioned REST (`/api/v1/...`) before any UI" — Constitution §2.1

✅ **Satisfied**: Phase 1 delivered full API surface (48/48 tasks complete).

> "TypeScript API client **generated from OpenAPI** — no hand-written `fetch` calls" — Constitution §6.5.2

✅ **Implementation**: Generate client from `openapi.yaml` using `openapi-typescript-codegen` or `kiota`.

> "No `any` — use `unknown` + narrowing" — Constitution §6.5.1

✅ **Enforcement**: TypeScript strict mode + ESLint `no-explicit-any` rule.

> "Components < 200 lines, single-purpose, support loading/empty/error/success states" — Constitution §6.5.4

✅ **Review gate**: PR template includes component line-count check.

> "All user-facing strings in i18n catalogs (`src/lib/i18n/en.json`); no hard-coded strings" — Constitution §6.5.12

✅ **Enforcement**: ESLint `no-hardcoded-strings` custom rule (mirroring D-011 pattern).

> "Design tokens in `src/lib/tokens.css`; no magic Tailwind values like `mt-[13px]`" — Constitution §6.5.5

✅ **Enforcement**: ESLint `no-arbitrary-values` Tailwind rule.

> "MSAL.js for Entra ID; PKCE; tokens in memory or sessionStorage (**never** localStorage)" — Constitution §6.5.10

✅ **Enforcement**: Four-gate model (D-010) already deployed in Phase 1 Round 1.

> "Playwright is the only E2E framework; tests in `tests/e2e/` with Page Object Model" — Constitution §6.5.14

✅ **Critical journeys** (PRD §7.5.4): 13 user journeys → 13+ Playwright spec files.

> "axe-core in unit + E2E tests; **zero violations** required to merge" — Constitution §6.5.6

✅ **Implementation**: `vitest-axe` in component tests; Playwright `axe-core` in E2E.

> "Lighthouse Performance score ≥ 90; Accessibility = 100; Best Practices ≥ 95; SEO ≥ 90" — Constitution §6.5.9

✅ **CI gate**: Lighthouse CI on Devices List + Detail routes.

---

## 4. Architecture

### 4.1 Authentication Flow

**MSAL.js OIDC + PKCE** (per `docs/auth-design.md` v2.0 and D-009):

1. User clicks "Sign In" → MSAL.js redirects to `login.microsoftonline.com/{tenant-id}`
2. Entra authenticates (MFA if configured) → redirect back with auth code
3. Client exchanges code for JWT (PKCE verifier) → API validates JWT signature/issuer/audience/expiry
4. API extracts `oid`, `email`, `app_roles` from JWT; creates/updates `Owner` record on first login
5. Subsequent requests include `Authorization: Bearer <JWT>` header (via generated client)
6. JWT stored in **sessionStorage only** (never localStorage — D-010)
7. MSAL.js handles silent token refresh (`acquireTokenSilent()`) before expiry
8. Sign-out clears sessionStorage + calls Entra logout endpoint

**Backend Auth Surface Change**:
- **Remove** `Auth:DevBypass` guard from `appsettings.json` (keep in `appsettings.Development.json` for local dev)
- **Add** JWT bearer validation middleware: `JwtBearerDefaults.AuthenticationScheme` with Entra authority/audience
- **Wire** `ICurrentUserService` to extract `ClaimsPrincipal` → `oid` → `Owner` lookup

### 4.2 Generated TypeScript Client

**Tool**: `openapi-typescript-codegen` (alternative: `kiota` if .NET codegen preferred)

**Source**: `openapi.yaml` at repo root (regenerated via `task openapi:export`)

**Output**: `src/TechInventory.Web/src/lib/api/generated/` (gitignored; regenerated on build)

**Wrapper**: `src/TechInventory.Web/src/lib/api/client.ts` configures base URL, auth headers via MSAL token

**Regeneration trigger**: `openapi.yaml` file hash change → CI fails if client stale

### 4.3 State Management

**Server State**: TanStack Query style (SWR with cache/retry/stale-while-revalidate):
- `src/lib/queries/devices.ts` — `useDevices()`, `useDevice(id)`, `useCreateDevice()`, etc.
- Mutations invalidate query cache on success
- Loading/error states extracted from query hooks

**UI State**: Svelte stores (`src/lib/stores/`) for:
- Current user context (role, display name, avatar URL)
- Toast notifications queue
- Filter/sort state (URL-backed via `$page.url.searchParams`)
- Form draft state (unsaved changes warning)

**No global server state singletons** — always via query library.

### 4.4 Design Tokens & Tailwind

**Tokens** (`src/lib/tokens.css`): Already scaffolded with ~60 CSS custom properties (Apple-esque palette)

**Tailwind config** (`tailwind.config.ts`): Consume tokens via `theme.extend.colors`, `theme.extend.spacing`

**No arbitrary values**: ESLint enforces `no-arbitrary-values` — all spacing/colors must reference tokens

**Dark mode**: Respect `prefers-color-scheme` (v1); manual toggle deferred to v1.1

### 4.5 Internationalization

**Catalog**: `src/lib/i18n/en.json` (already scaffolded with minimal keys)

**Loader**: `src/lib/i18n/index.ts` provides `t(key)` function

**Coverage**: All user-facing strings in catalog; ESLint `no-hardcoded-strings` rule enforces

**v1 ships English only**; architecture supports adding locales (fallback chain: user → browser → `en`)

---

## 5. User Journeys (Critical — 13 from PRD §7.5.4)

Each journey maps to ≥1 Playwright test. UI must handle loading/empty/error/success states for each.

### J1: Sign In (Entra ID)
- **Actor**: Family Member (any role)
- **Flow**: Click "Sign In" → redirect to Entra → authenticate (MFA if configured) → redirect back → JWT in sessionStorage → see Devices List
- **Acceptance**: JWT stored in sessionStorage (not localStorage); `localStorage` scan = empty; user display name visible in header
- **Playwright**: `tests/e2e/auth/sign-in.spec.ts` (6 projects: Chromium/WebKit/Firefox × desktop/mobile)

### J2: Sign In Denied (No Role Assignment)
- **Actor**: User without `admin`/`member`/`viewer` role in Entra
- **Flow**: Entra authenticates → API rejects JWT (empty `app_roles`) → 401 Unauthorized → show "Access Denied" message
- **Acceptance**: Error message does not leak server internals; sign-out button visible
- **Playwright**: `tests/e2e/auth/sign-in-denied.spec.ts`

### J3: Sign Out
- **Actor**: Any authenticated user
- **Flow**: Click "Sign Out" → clear sessionStorage → call Entra logout endpoint → redirect to home page
- **Acceptance**: sessionStorage empty; JWT cleared; user redirected to `/`
- **Playwright**: `tests/e2e/auth/sign-out.spec.ts`

### J4: Browse Devices List
- **Actor**: Viewer (read-only)
- **Flow**: Navigate to `/devices` → see paginated list (25 per page, default sort by name) → apply filters (brand, category, status, search) → results update → URL reflects filter state → reload preserves view
- **Acceptance**: First paint < 500ms on dev hardware; empty state if no devices; loading skeleton during fetch; filter state in URL query params; pagination controls visible if > 25 devices
- **Playwright**: `tests/e2e/devices/browse-list.spec.ts`

### J5: View Device Detail
- **Actor**: Viewer
- **Flow**: Click device in list → navigate to `/devices/{id}` → see full device details (brand/category/owner/location/network names resolved)
- **Acceptance**: All reference data resolved to human-readable labels (not UUIDs); breadcrumbs show path back to list; 404 page if device not found
- **Playwright**: `tests/e2e/devices/view-detail.spec.ts`

### J6: Create Device
- **Actor**: Member (write own)
- **Flow**: Click "Add Device" → fill form (name, brand, serial, purchase date, price, currency, category, owner, location, network, notes) → submit → device created → redirect to detail view
- **Acceptance**: Validation errors inline on blur; disabled submit during save; optimistic loading state; toast notification on success; household default currency pre-filled; brand/category dropdowns populated from reference data
- **Playwright**: `tests/e2e/devices/create-device.spec.ts`

### J7: Edit Device
- **Actor**: Member (own devices only) or Admin (all devices)
- **Flow**: Open device detail → click "Edit" → modify fields → submit → changes persisted → see updated detail view
- **Acceptance**: Retired devices show "read-only" badge; only notes/disposal editable if retired; validation on submit; toast notification on success
- **Playwright**: `tests/e2e/devices/edit-device.spec.ts`

### J8: Delete Device (Soft-Delete Confirmation)
- **Actor**: Member (own devices) or Admin (all devices)
- **Flow**: Open device detail → click "Delete" → confirm (type device name or "DELETE") → device soft-deleted (status → Disposed) → removed from active list
- **Acceptance**: Destructive confirmation required; toast notification; redirect to list; device no longer in list (unless "Show Disposed" filter active)
- **Playwright**: `tests/e2e/devices/delete-device.spec.ts`

### J9: Claim Device Ownership
- **Actor**: Member
- **Flow**: Open device detail (owned by another user or unassigned) → click "Claim Ownership" → confirm → device owner updated to current user
- **Acceptance**: Ownership change logged in audit trail (visible on detail page); toast notification; owner badge updates
- **Playwright**: `tests/e2e/devices/claim-ownership.spec.ts`

### J10: Import CSV (Preview + Commit)
- **Actor**: Admin
- **Flow**: Navigate to `/import` → upload CSV file → see preview (valid rows + invalid rows with error messages + missing lookups to create) → confirm → commit → devices imported → see ImportBatch summary
- **Acceptance**: Preview shows row-level validation errors; missing brands/categories/owners/locations listed; commit creates missing lookups + devices; 10MB file size limit enforced (413 error if exceeded); empty state if no rows; loading spinner during upload
- **Playwright**: `tests/e2e/admin/import-csv.spec.ts`

### J11: Export CSV/JSON
- **Actor**: Admin
- **Flow**: Navigate to `/export` → select format (CSV or JSON) → apply filters (same as list view) → click "Download" → file downloads
- **Acceptance**: Downloaded file parses cleanly; contains filtered rows; filename includes timestamp; toast notification on success
- **Playwright**: `tests/e2e/admin/export-data.spec.ts`

### J12: Manage Reference Entities (Brands/Manufacturers/Categories/Models/Owners/Locations)
- **Actor**: Admin
- **Flow**: Navigate to `/admin/brands` (or categories, owners, locations, networks, tags) → see paginated list → click "Add" → fill form → submit → created → see in list → click row → edit → submit → updated → click "Deactivate" → confirm → soft-deleted (removed from active list)
- **Acceptance**: Each reference entity has dedicated CRUD UI; categories show tree structure; owners show role badge; inactive entities hidden by default (toggle "Show Inactive" to reveal)
- **Playwright**: `tests/e2e/admin/manage-brands.spec.ts` (repeat pattern for other entities)

### J13: Role Enforcement (Viewer Cannot Edit/Delete)
- **Actor**: Viewer
- **Flow**: Sign in as Viewer → browse devices list → open device detail → "Edit" and "Delete" buttons **not visible** → attempt direct navigation to `/devices/{id}/edit` → 403 Forbidden page
- **Acceptance**: UI hides edit/delete affordances for Viewer role; direct navigation to protected routes returns 403; API enforces authorization (client-side hiding is not security — server validates)
- **Playwright**: `tests/e2e/auth/role-enforcement.spec.ts`

---

## 6. Acceptance Criteria

### 6.1 Functional

1. **Authentication**: Entra ID OIDC + PKCE flow complete end-to-end; JWT validation on API; first-login creates `Owner` record
2. **Authorization**: Role-based UI adaptation (Admin/Member/Viewer); API enforces RBAC on every endpoint
3. **Device CRUD**: List (paginated, filtered, sorted) + create + edit + detail + soft-delete all functional
4. **Reference Entity CRUD**: Brands, Categories, Owners, Locations, Networks, Tags — all have admin CRUD UIs
5. **Ownership Flows**: Claim/release device ownership with audit trail
6. **Import**: CSV preview + commit with row-level validation feedback + missing-lookup creation
7. **Export**: CSV/JSON downloads with applied filters
8. **Offline App Shell**: PWA manifest + service worker precache app shell (not API responses); installable on iOS/Android/desktop
9. **Accessibility**: Zero axe-core violations on every route; keyboard-operable; focus visible; WCAG 2.2 AA compliant
10. **E2E Coverage**: All 13 critical user journeys pass in Playwright (6 projects: 3 browsers × 2 viewports)

### 6.2 Non-Functional

1. **Performance**: First Contentful Paint ≤ 1.5s, LCP ≤ 2.5s, TTI ≤ 3.0s on 4G (Lighthouse CI enforced)
2. **Lighthouse Scores**: Performance ≥ 90, Accessibility = 100, Best Practices ≥ 95, SEO ≥ 90
3. **Bundle Size**: Initial JS ≤ 150KB gzipped; per-route chunks ≤ 50KB gzipped
4. **Test Coverage**: Component tests for every interactive component; E2E for all 13 critical journeys
5. **Browser Support**: Last 2 versions Chrome, Edge, Safari, Firefox; iOS 16+; Android Chrome last 2
6. **Responsive**: Mobile-first (360px min width); breakpoints at 640/768/1024/1280
7. **Security**: No tokens in localStorage (D-010); CSP strict; no third-party scripts; Subresource Integrity on external resources
8. **Error Handling**: User-safe error messages (never leak server internals); toast notifications for success/failure; empty states; retry affordances

---

## 7. Auth Surface (Backend Changes)

### 7.1 Remove Dev Bypass from Production

**File**: `src/TechInventory.Api/appsettings.json`

**Change**: Ensure `Auth:DevBypass` is **false** (or omitted) in production config

**Keep**: `appsettings.Development.json` retains `Auth:DevBypass: true` for local dev

### 7.2 Add JWT Bearer Validation

**File**: `src/TechInventory.Api/Program.cs`

**Implementation**:
```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["Auth:TenantId"]}/v2.0";
        options.Audience = builder.Configuration["Auth:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireClaim("app_roles", "admin"));
    options.AddPolicy("Member", policy => policy.RequireClaim("app_roles", "admin", "member"));
    options.AddPolicy("Viewer", policy => policy.RequireClaim("app_roles", "admin", "member", "viewer"));
});
```

**Config** (`appsettings.json`):
```json
{
  "Auth": {
    "TenantId": "<REPLACE_WITH_HOUSEHOLD_TENANT_ID>",
    "ClientId": "<REPLACE_WITH_ENTRA_APP_CLIENT_ID>"
  }
}
```

**Note**: Tenant ID + Client ID are **not secrets** (public in JWT issuer); safe to commit. Client secret is **never** in client code (only in backend if needed for server-to-server flows — not required for PKCE).

### 7.3 Wire `ICurrentUserService` to ClaimsPrincipal

**Interface** (already exists from Phase 1): `src/TechInventory.Application/Abstractions/Services/ICurrentUserService.cs`

**Implementation**: `src/TechInventory.Infrastructure/Services/HttpContextCurrentUserService.cs`

**Logic**:
1. Extract `oid` claim from `HttpContext.User.Claims`
2. Look up `Owner` by `EntraObjectId` (cached per request)
3. Return `Owner.Id` as current user ID for audit/RBAC

**First-Login Onboarding**:
- If `Owner` not found by `oid`, create new `Owner` record:
  - `EntraObjectId` ← JWT `oid`
  - `Email` ← JWT `email`
  - `Role` ← first element of JWT `app_roles` array
  - `DisplayName` ← JWT `name`
  - `IsActive` ← `true`
- Log `AuditEvent { action: "UserFirstLogin", userId: <new_owner_id>, ... }`

---

## 8. Open Questions for Brian

### Q1: Theme / Light Mode Preference
**Question**: Should v1 include a manual dark mode toggle, or respect `prefers-color-scheme` only?

**Options**:
- A) Respect `prefers-color-scheme` only (simplest; defer manual toggle to v1.1)
- B) Include manual toggle with persisted preference (adds localStorage for theme only — acceptable?)

**Recommendation**: **Option A** — v1 respects OS preference; manual toggle in v1.1 reduces Phase 2 scope.

**Decision needed**: Before Round 0 (T01 may need to wire theme context).

---

### Q2: Installable PWA from Day One?
**Question**: Should Phase 2 ship with service worker precache + manifest (installable PWA), or defer to Phase 3?

**Options**:
- A) Phase 2 includes manifest + minimal service worker (offline app shell, no API caching)
- B) Phase 2 skips service worker; PWA installability deferred to Phase 3

**Recommendation**: **Option A** — manifest + minimal SW are low-cost wins; constitution requires PWA (§6.5.8).

**Decision needed**: Before Round 0 (affects task breakdown).

---

### Q3: Mobile Breakpoint Min Width?
**Question**: What minimum viewport width should the UI support?

**Options**:
- A) 360px (iPhone SE, Pixel 5 range) — standard mobile baseline
- B) 375px (iPhone 13 mini, modern iOS default)
- C) 320px (legacy devices)

**Recommendation**: **Option A (360px)** — aligns with constitution §6.5.7 and Tailwind `sm` breakpoint.

**Decision needed**: Before Round 2 (component design phase).

---

### Q4: CSV Column Ordering Convention for Export?
**Question**: Should exported CSV columns match import column order (if known), or use a canonical order?

**Options**:
- A) Canonical order (Name, Serial, Brand, Category, Owner, ...) — consistent across exports
- B) Import-matching order (mirrors uploaded CSV column order if available)
- C) User-configurable (Phase 3 feature)

**Recommendation**: **Option A** — canonical order simplifies implementation; user-configurable deferred to v2.

**Decision needed**: Before T31 (export UI).

---

### Q5: Entra Tenant ID / Client ID Provisioning?
**Question**: How should Brian provision Entra app registration values into the API config?

**Options**:
- A) Manual `appsettings.json` edit (document in Phase 2 runbook)
- B) Docker secrets mount (`.env` file at runtime)
- C) Brian creates PR with values in `appsettings.json` (Tenant ID + Client ID are public, not secrets)

**Recommendation**: **Option C** — Tenant ID and Client ID are not secrets (visible in JWT); safe to commit in `appsettings.json`. Client secret (if ever needed) goes in Docker secrets only.

**Decision needed**: Before Round 1 (Bishop's backend auth task).

---

## 9. Out of Scope (Explicitly Deferred)

| Item | Deferred To | Rationale |
|------|-------------|-----------|
| Photo attachments (upload/viewing) | v2 (PRD §15) | Not in Phase 2 per PRD §13 |
| Timeline view / "Tech Eras" | Phase 4 | Polish feature; core CRUD higher priority |
| Saved views per user | Phase 4 | Requires user preferences persistence |
| Warranty expiration reminders | v2 | Business logic + notification plumbing |
| Bulk edit UI | v2 | API exists, UI deferred |
| Visual regression testing | Phase 5 | Hardening phase |
| Admin user management UI | v1.1 | Entra portal sufficient for v1 |
| Dashboard / metrics / charts | Phase 4 | Reporting feature |
| Dark mode manual toggle | v1.1 | v1 respects OS preference only |
| Service worker API response caching | Phase 3 | Offline-first complexity |
| Advanced offline support (mutation queue) | v2 | Requires conflict resolution strategy |
| Multi-language support | v2 | Architecture ready; English only for v1 |
| Bulk device import via drag-drop | v2 | CSV upload sufficient for v1 |
| Advanced search (full-text, boolean queries) | v2 | List filters sufficient for v1 |

---

## 10. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | 2026-05-18 | Ripley | Initial draft for Phase 2 (Frontend MVP + Real Entra OIDC) |

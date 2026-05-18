# Plan — 002 Frontend MVP + Real Entra OIDC

**Spec**: `specs/002-frontend-mvp/spec.md`
**Tasks**: `specs/002-frontend-mvp/tasks.md`
**Status**: Draft

---

## 1. Overview

Phase 2 ships the **SvelteKit PWA frontend** consuming the Phase 1 API, replacing the dev auth bypass with **production Microsoft Entra ID OIDC + PKCE authentication**. This makes Tech Inventory usable by Brian and family members with a modern, accessible, mobile-first UI covering all core device management + reference entity admin + import/export workflows.

**Scope**: 53 tasks across 11 rounds (~3-4 weeks of work)

**Outcome**: Installable PWA; 13 critical user journeys E2E tested; zero axe-core violations; Lighthouse scores ≥ 90/100/95/90

---

## 2. Timeline & Rounds

### Rough Timeline (3-4 weeks)

| Round | Tasks | Duration | Focus | Blocking |
|-------|-------|----------|-------|----------|
| **R0** | T01-T05 | 1-2 days | Foundation (client gen, tokens, i18n, MSAL config) + **[Brian]** Entra setup | T01 blocks everything |
| **R1** | T06-T08 | 1-2 days | Backend JWT validation (Bishop) | T06 blocks T09 |
| **R2** | T09-T13 | 2-3 days | Login/logout + app shell (Vasquez) | T09 blocks all protected routes |
| **R3** | T14-T18 | 2-3 days | Devices list (Vasquez + Apone) | Foundation for R4-R5 |
| **R4** | T19-T23 | 3-4 days | Device detail + create + edit + delete (Vasquez + Apone) | Major vertical |
| **R5** | T24-T26 | 1-2 days | Ownership claim/release (Vasquez + Apone) | Complements R4 |
| **R6** | T27-T33 | 3-4 days | Reference entity admin UIs (6 entities, parallel) | Vasquez can parallelize |
| **R7** | T34-T37 | 2-3 days | Import wizard (Vasquez + Apone) | Admin feature |
| **R8** | T38-T39 | 1 day | Export downloads (Vasquez + Apone) | Admin feature |
| **R9** | T40-T44 | 2-3 days | Polish (empty/error/loading states, toast, axe sweep) | Vasquez + Apone |
| **R10** | T45-T50 | 3-4 days | E2E hardening (13 Playwright specs × 6 projects) | Apone sequential |
| **R11** | T51-T53 | 1-2 days | PWA manifest + service worker + Lighthouse CI | Vasquez + Hudson |

**Total estimated duration**: 20-28 days (assuming some parallelism within rounds)

**Critical path**: T01 → T05 → T09 → T14 → T19 → T45-T50

---

## 3. Parallelism Strategy

**Mirroring Phase 1 fan-out approach**: 2-3 agents per round, scopes disjoint.

### Round 0 (Foundation)
- **Vasquez**: T02, T03, T04, T05 (all [P] parallel after T01)
- **Brian**: T01 (Entra app registration) — **blocks T05 and T06**

### Round 1 (Backend Auth)
- **Bishop**: T06, T07, T08 (sequential: JWT → CurrentUserService → integration tests)

### Round 2 (Login + Shell)
- **Vasquez**: T09, T10, T12, T13 (login, auth store, route guard, app shell)
- **Bishop**: T11 (`/api/v1/owners/me` endpoint) — parallel with Vasquez's T09-T10

### Round 3 (Devices List)
- **Vasquez**: T14, T15, T16, T17 (query hook, list page, filters, sort)
- **Apone**: T18 (component tests) — starts as soon as T15-T17 land

### Round 4 (Device CRUD)
- **Vasquez**: T19, T20, T21, T22 (detail, create, edit, delete)
- **Apone**: T23 (component tests) — parallel with Vasquez's tail

### Round 5 (Ownership)
- **Vasquez**: T24, T25 (claim, release)
- **Apone**: T26 (component tests) — parallel

### Round 6 (Reference Entities)
- **Vasquez**: T27-T32 (6 reference entity UIs — **can be parallelized** by splitting into 2-3 sub-rounds: brands/categories, then owners/locations/networks/tags)
- **Apone**: T33 (component tests) — starts after T27-T32 land

### Round 7 (Import)
- **Vasquez**: T34, T35, T36 (import wizard steps)
- **Apone**: T37 (component tests) — parallel with Vasquez's tail

### Round 8 (Export)
- **Vasquez**: T38 (export page)
- **Apone**: T39 (component tests) — parallel

### Round 9 (Polish)
- **Vasquez**: T40, T41, T42, T43 (empty/error/loading states, toast system) — can parallelize by state type
- **Apone**: T44 (axe-core sweep) — starts after T40-T43 land; fixes violations in tight loop with Vasquez

### Round 10 (E2E)
- **Apone**: T45-T50 (6 Playwright specs covering 13 critical journeys) — **mostly sequential** (auth → devices → ownership → import → export → reference entities) but Apone can batch similar specs

### Round 11 (PWA + Lighthouse)
- **Vasquez**: T51, T52 (manifest, service worker)
- **Hudson**: T53 (Lighthouse CI) — parallel with Vasquez's T52

---

## 4. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **T01 delay (Entra setup)** — Brian's hands required for Azure Portal app registration | Med | High | **Mitigation**: Document exact steps in runbook; Vasquez/Bishop can proceed with T02-T04 in parallel; T05 waits for T01 completion. **Fallback**: Use dev bypass temporarily if Entra registration blocked. |
| **Docker dependency for E2E** — Playwright requires running Compose stack; Brian's primary dev machine may not have Docker | Med | Med | **Mitigation**: E2E runs in CI (ubuntu-latest has Docker). Local E2E optional for developers. Apone runs E2E in CI or on Docker-equipped machine. **Fallback**: Apone uses remote dev server if Docker unavailable locally. |
| **MSAL.js token-cache complexity** — sessionStorage + silent refresh can be finicky | High | Med | **Mitigation**: Follow MSAL.js docs exactly; leverage D-010 four-gate enforcement (ESLint + pre-commit + Playwright + code review) to catch localStorage leaks. Test silent refresh explicitly in T09. **Fallback**: If silent refresh fails, fall back to full re-login (acceptable for v1). |
| **SvelteKit SSR vs SPA decision for PWA** — Constitution requires PWA; SvelteKit default is SSR | Low | Low | **Mitigation**: Use SvelteKit adapter-static or adapter-node with client-side rendering for protected routes. Service worker handles offline shell (T52). **Decision**: SvelteKit adapter-node already deployed (D-004); SSR for public routes, CSR for authenticated routes (standard pattern). |
| **Scope creep** — 53 tasks is ambitious; risk of timeline slip | High | Med | **Mitigation**: Strict adherence to Spec §2.2 Non-Goals. Defer polish to Phase 3 if timeline pressure. E2E is non-negotiable (constitution §7.4). **Fallback**: Phase 2.5 for polish if needed; core auth + device CRUD + E2E must ship. |
| **Accessibility violations discovered late** — axe-core sweep (T44) at end of R9 may find violations requiring rework | Med | Low | **Mitigation**: Run axe-core continuously during component tests (T18, T23, T26, T33, T37, T39). Apone catches violations early. T44 is final sweep, not first check. |
| **Lighthouse CI flakiness** — Performance scores can vary by CI runner load | Med | Low | **Mitigation**: Run Lighthouse CI on scheduled runs (not blocking PRs initially). Establish baseline scores first. Hudson tunes thresholds after observing CI environment behavior. |
| **MSAL.js version compatibility** — MSAL.js v3.30 is latest; Entra Workforce tenant may have quirks | Low | Med | **Mitigation**: Bishop tests JWT validation with synthetic token first (T06, T08). Vasquez tests MSAL.js redirect flow in isolation (T09). Phase 1 Round 1 already validated MSAL cache location (D-010). **Fallback**: Downgrade MSAL.js to v2 if v3 incompatible (unlikely). |

---

## 5. Definition of Phase 2 Done

Phase 2 is **done** when:

- [ ] All 53 tasks complete (T01-T53)
- [ ] **Backend auth swap**: `Auth:DevBypass=false` in `appsettings.json`; JWT bearer validation active; `HttpContextCurrentUserService` wired
- [ ] **Frontend auth**: MSAL.js login/logout functional; JWT in sessionStorage (never localStorage); current user context populated
- [ ] **Core UI**: Devices list + detail + create + edit + delete + ownership claim/release all functional
- [ ] **Reference entity admin**: 6 reference entity UIs (brands/categories/owners/locations/networks/tags) all functional
- [ ] **Import/export**: CSV import wizard + CSV/JSON export downloads functional
- [ ] **13 critical E2E journeys** (PRD §7.5.4): All green on 6 Playwright projects (Chromium/WebKit/Firefox × desktop/mobile)
- [ ] **Zero axe-core violations** on every route (constitution §6.5.6)
- [ ] **Lighthouse CI passing** on 2 critical routes (`/devices`, `/devices/{id}`): Performance ≥ 90, Accessibility = 100, Best Practices ≥ 95, SEO ≥ 90
- [ ] **PWA installable**: Manifest + service worker precache app shell; installable on iOS/Android/desktop
- [ ] **Component test coverage**: Every interactive component has Vitest tests (loading/empty/error/success states)
- [ ] **CI quality gate green**: `pnpm run check`, `pnpm run lint`, `pnpm run test`, Playwright E2E, Lighthouse CI all pass
- [ ] **Brian can sign in** with household Entra account; CRUD devices; import CSV; export data — end-to-end smoke test successful
- [ ] **No new secrets committed**: `gitleaks` pre-commit hook + CI scan pass
- [ ] **OpenAPI client stale check**: CI fails if `openapi.yaml` changes without regenerating TS client

**Exit criteria**: Brian confirms Phase 2 delivers a usable PWA for family members; Phase 3 (infrastructure hardening) can proceed.

---

## 6. Open Questions (Blocking Round 0 Start)

Per Spec §8, these questions must be answered before launching:

### Q1: Theme / Light Mode Preference
**Status**: **NEEDS BRIAN'S DECISION**

**Options**:
- A) Respect `prefers-color-scheme` only (defer manual toggle to v1.1) — **Recommended**
- B) Include manual toggle with persisted preference (adds localStorage for theme only)

**Blocker**: T01 (if answer is B, theme context wiring starts in R0).

---

### Q2: Installable PWA from Day One?
**Status**: **RECOMMENDED: YES** (Constitution §6.5.8 requires PWA)

**Options**:
- A) Phase 2 includes manifest + minimal service worker (offline app shell, no API caching) — **Recommended**
- B) Phase 2 skips service worker; PWA installability deferred to Phase 3

**Decision**: **Option A** assumed in task breakdown (T51, T52). If Brian wants Option B, remove T51-T52 from Phase 2.

**Blocker**: T51 (if answer is B, PWA deferred to Phase 3).

---

### Q3: Mobile Breakpoint Min Width?
**Status**: **RECOMMENDED: 360px** (Constitution §6.5.7; Tailwind `sm` default)

**Options**:
- A) 360px (iPhone SE, Pixel 5 range) — **Recommended**
- B) 375px (iPhone 13 mini, modern iOS default)
- C) 320px (legacy devices)

**Decision**: **Option A** assumed.

**Blocker**: T03 (design tokens include responsive breakpoints).

---

### Q4: CSV Column Ordering Convention for Export?
**Status**: **RECOMMENDED: Canonical Order**

**Options**:
- A) Canonical order (Name, Serial, Brand, Category, Owner, ...) — **Recommended**
- B) Import-matching order (mirrors uploaded CSV column order if available)
- C) User-configurable (Phase 3 feature)

**Decision**: **Option A** assumed.

**Blocker**: T38 (export page implementation).

---

### Q5: Entra Tenant ID / Client ID Provisioning?
**Status**: **RECOMMENDED: PR with values in `appsettings.json`** (not secrets; safe to commit)

**Options**:
- A) Manual `appsettings.json` edit (document in Phase 2 runbook)
- B) Docker secrets mount (`.env` file at runtime)
- C) Brian creates PR with values in `appsettings.json` (Tenant ID + Client ID are public) — **Recommended**

**Decision**: **Option C** assumed.

**Blocker**: T01 (Brian's Entra app registration task outputs Tenant ID + Client ID).

---

## 7. Cast Assignments Summary

| Agent | Task Count | Primary Scope |
|-------|------------|---------------|
| **Vasquez** (Frontend Lead) | 33 | TS client generation, design tokens, i18n, MSAL config, login/logout, app shell, devices CRUD UI, ownership UI, reference entity UIs (6), import wizard, export page, polish (empty/error/loading/toast), PWA manifest + service worker |
| **Bishop** (Backend Auth) | 4 | JWT bearer validation, `HttpContextCurrentUserService`, `/api/v1/owners/me` endpoint, integration tests |
| **Apone** (QA / E2E Lead) | 12 | Component tests (devices, forms, ownership, reference entities, import, export), axe-core sweep, E2E (13 critical journeys across 6 browser projects) |
| **Hudson** (DevOps) | 1 | Lighthouse CI integration |
| **Brian** (Product Owner) | 1 | **[Manual]** Entra app registration in Azure Portal (T01) |
| **Hicks** (Backend) | 2 | (Optional) Backend support for reference entity endpoints if new APIs needed beyond Phase 1 surface |

**Lead responsibilities**:
- **Vasquez**: Owns frontend delivery; coordinates with Apone on component test shape; reviews PRs from other frontend contributors (if any).
- **Bishop**: Owns backend auth delivery; coordinates with Vasquez on JWT token flow; reviews backend PRs.
- **Apone**: Owns test authorship and E2E delivery; coordinates with Vasquez on component test fixtures; reports accessibility violations early.

---

## 8. Handoff Notes

### From Phase 1 (Complete)
- **API Surface**: 48/48 tasks done; full OpenAPI spec committed at `openapi.yaml`
- **Auth Bypass Active**: `Auth:DevBypass=true` in `appsettings.Development.json` — local API accessible without JWT
- **Integration Test Harness**: SQLite per-test-class isolation; `IntegrationTestFactory<T>` reusable
- **CI Pipeline**: Full verify chain (format → build → test → vuln scan) green
- **Frontend Scaffold**: SvelteKit + MSAL.js + ESLint token-storage gate already deployed (commit `b254a7a`)

### To Phase 3 (Infrastructure Hardening)
- **Docker Compose Production**: Healthchecks, resource limits, secrets, TLS termination (external proxy)
- **Backup Strategy**: Nightly SQLite backups with quarterly restore drills
- **Monitoring**: Seq logs + OpenTelemetry traces + healthcheck dashboard
- **Deployment Runbook**: Brian's home server deploy steps

### To Phase 4 (Polish & Reporting)
- **Dashboard**: Metrics, charts, "recently added", "warranty expiring soon"
- **Timeline View**: "Tech Eras" visualization
- **Saved Views**: Per-user filter/sort persistence
- **Advanced Search**: Full-text, boolean queries

---

## 9. Success Metrics (Post-Phase 2)

| Metric | Target | How Measured |
|--------|--------|--------------|
| **13 Critical E2E Journeys Pass** | 100% on 6 browser projects | Playwright CI reports |
| **Zero Axe-Core Violations** | All routes | `vitest-axe` + Playwright axe-core |
| **Lighthouse Scores** | Performance ≥ 90, A11y = 100, BP ≥ 95, SEO ≥ 90 | Lighthouse CI on `/devices`, `/devices/{id}` |
| **PWA Installable** | iOS + Android + desktop | Manual smoke test |
| **JWT Token Never in localStorage** | 100% | Playwright token-storage assertion (T09) |
| **Brian Can CRUD Devices** | End-to-end smoke | Manual verification |
| **Family Member Can Sign In** | Entra ID flow complete | Manual verification |
| **Import CSV Succeeds** | 100 rows → 100 devices created | Manual smoke test |
| **Export CSV Downloads** | Filtered 50 devices → CSV file parses cleanly | Manual smoke test |
| **Component Test Coverage** | Every interactive component | `pnpm run test` reports |

---

## 10. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | 2026-05-18 | Ripley | Initial draft for Phase 2 (Frontend MVP + Real Entra OIDC) |

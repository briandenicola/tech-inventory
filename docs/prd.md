# Tech Inventory — Product Requirements Document

**Status**: Draft v0.1
**Owner**: Brian Denicola
**Last Updated**: 2025-05-17

---

## 1. Vision

A **self-hosted, family-friendly appliance, devices, and technology inventory tracker** that captures the lifecycle of every device passing through the household — from purchase through retirement — into a single, searchable, beautiful source of truth.


---

## 2. Problem Statement

Over two decades of accumulating technology in a SharePoint List has:

- **Not mobile friendly**
- **Not extensible** to include newer AI capabilities 
- **Data quality issues** inherit with SharePoint Lists
- **Lost institutional knowledge** when devices are gifted, sold, recycled, or stolen
- **No safe family-shared view** — current tools are either personal or fully public
- **Privacy concerns** — device serials, locations, and ownership shouldn't live in a third-party SaaS

### Pain Points
| Persona | Pain |
|---|---|
| Admin (you) | Sharing device information with external concerns |
| Family member | Can't performan simple CRUD operations on mobile devices |
| Family member | SharePoint lists are visually boring and difficult to use

---

## 3. Goals & Non-Goals

### Goals (v1)
- ✅ Import SharePoint CSV data losslessly
- ✅ Provide a fast, modern web UI + installable PWA
- ✅ Authenticate family members via Microsoft Entra ID
- ✅ Run entirely self-hosted on home infrastructure
- ✅ task all functionality available via versioned REST API
- ✅ Preserve full history — nothing is hard-deleted
- ✅ Export all data and images
- ✅ Observaibility 
- ✅ Administrator and User setting screens to manage the application
- 🔄 Lifecycle workflows (in-use → retired → recycled/sold/gifted)
- 📍 Location tracking (home / lent out / stored / disposed)

### Goals (v1.5)
- 📸 Photo attachments per device
- 🔔 AI analysis of device image to automation data entry

### Non-Goals
- ❌ Multi-tenant SaaS — single household only
- ❌ Enterprise CMDB features (discovery, agents, configuration drift)
- ❌ Public sharing or social features
- ❌ Mobile-native apps (PWA is sufficient)
- ❌ Real-time collaboration / multi-user editing locks
- ❌ Integration marketplace

---

## 4. Target Users

### Primary: Household Admin
- **Profile**: Technical, runs home infrastructure, organized
- **Needs**: Import, edit, audit, full control, API access
- **Role**: `Admin` (full read/write/delete, user management)

### Secondary: Adult Family Members
- **Profile**: Moderately technical, occasional use
- **Needs**: Look up devices, claim ownership, request changes
- **Role**: `Member` (read all, write own)

### Tertiary: Younger Family Members
- **Profile**: Casual, mobile-first
- **Needs**: "What is this thing?" / "Whose is this?"
- **Role**: `Viewer` (read-only, limited fields)

### Anti-persona
- ❌ External users, guests, vendors, contractors — explicitly excluded

---

## 5. Key Use Cases (User Stories)

### Admin
- **U1**: As Admin, I import the SharePoint CSV so historical data is preserved.
- **U2**: As Admin, I edit any device record so I can correct or enrich data.
- **U3**: As Admin, I view audit history so I can see who changed what.
- **U4**: As Admin, I export filtered data so I can use it elsewhere.
- **U5**: As Admin, I retire a device so its lifecycle is tracked.
- **U6**: As Admin, I assign roles so family members get appropriate access.
- **U7**: As Admin, I manage the list of **Locations** (e.g., "Living Room",
  "Garage", "Storage Bin A3") so that family members select from a consistent
  set when recording where a device lives.
- **U8**: As Admin, I manage the list of **Networks** (e.g., "Home Wi-Fi 5GHz",
  "IoT VLAN", "Guest") so that devices can be tagged with the network they
  connect to from a curated list.
- **U9**: As Admin, I manage the list of **Categories** (e.g., "Phone",
  "Laptop", "Smart Home > Lighting") so that every device is classified
  consistently from a hierarchical taxonomy.
- **U10**: As Admin, I can create a full export of all items in the household

### Member
- **U11**: As Member, I search by name/brand/year so I find what I need fast.
- **U12**: As Member, I claim ownership of a device so records reflect reality.
- **U13**: As Member, I add notes to my devices so context is preserved.
- **U14**: As Member, I view a timeline of household tech for nostalgia/context.
- **U15**: As Member, I can perform CRUD operations on any device in the household 
- **U16**: As Member, I can appliy one or more category labels to a device

### Viewer
- **U17**: As Viewer, I browse devices on my phone so I can identify things.
- **U18**: As Viewer, I see what's currently in use vs retired.
- **U19**: As Member, I can upload a profile image for my account

### System
- **U20**: As System, I authenticate users via Entra ID so no passwords are stored.
- **U21**: As System, I log every mutation so a full audit trail exists.
- **U22**: As System, I work offline (read-only) so the PWA is useful on flaky networks.

---

## 6. Functional Requirements

### F1. Data Import
- Accept SharePoint CSV exports
- Map columns to canonical schema (configurable)
- Dry-run mode with preview
- Partial-success handling (per-row errors)
- Audit record per import batch

### F2. Device Management
- CRUD on devices with rich metadata (see Data Model section)
- Soft delete (status → `Retired` or `Disposed`)
- Bulk operations (tag, retire, export)
- Free-text search + faceted filters (brand, category, year, owner, status, location)
- Saved views per user

### F3. Browse Experiences
- **Dashboard**: counts, charts, "recently added", "warranty expiring soon" (v2)
- **List view**: sortable table, paginated, filterable
- **Timeline view**: grouped by year and "tech era" (e.g., iPhone Era, Smart-Home Era, AI Era)
- **Detail view**: all metadata, history, related devices, photos (v2)
- **Quietly elegant. Mid-2010s Apple in spirit**: minimal, typographic, uncluttered.

### F4. Export
- CSV, JSON formats
- Filtered or full
- Includes audit metadata optionally

### F5. Authentication & Authorization
- Local Account for default administrator 
- Microsoft Entra ID OIDC (External ID tenant for family)
- Roles: `Admin`, `Member`, `Viewer`
- Per-endpoint policy enforcement (default-deny)
- Session timeout configurable
- Sign-out on all devices (admin override)

### F6. API
- REST, JSON, OpenAPI 3.1 documented at `/swagger`
- URL-versioned (`/api/v1/...`)
- Pagination, filtering, sorting per RFC conventions
- Rate-limited per user
- ProblemDetails (RFC 7807) error responses

### F7. Audit & History
- Every mutation logged (who, what, when, before/after)
- Audit query endpoint (Admin only)
- Immutable audit table (append-only)

### F8. Admin Console
- User list with roles, last login
- Import history
- Export data
- System health view

---

## 7. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Performance** | P95 API response < 300ms on home LAN; PWA TTI < 2s |
| **Availability** | 99% (best-effort, single-host); graceful offline read |
| **Scale** | 10,000 devices; 10 concurrent users; 50 RPS sustained |
| **Security** | OWASP ASVS L2; OWASP API Top 10 (2023) verified; SBOM per release |
| **Privacy** | Data never leaves the host; no telemetry to third parties |
| **Accessibility** | WCAG 2.2 AA on web client |
| **Browser support** | Last 2 versions of Chrome, Edge, Safari, Firefox |
| **Internationalization** | English v1; i18n-ready architecture |
| **Observability** | Structured logs (Serilog), OpenTelemetry traces, healthchecks |
| **Backup** | Nightly SQL backups; restore tested quarterly |
| **Recovery** | RTO 4h; RPO 24h |
| **Maintainability** | Test coverage ≥ 85%; ADRs for material decisions |

## 7.1       Local Testing & Validation

Every contributor — human or AI — must be able to validate a change end-to-end
on their own machine before opening a pull request. CI exists to *enforce*
quality, not to *discover* it. If a test only runs in CI, it is not part of
this project's testing strategy.

### 7.5.1 Principles
- **Local-first**: every test type runnable in this section must run on a
  developer laptop with one documented command, without cloud dependencies
- **Hermetic**: tests must not depend on external services, internet access,
  or shared state. The full stack runs in Docker Compose locally
- **Fast feedback**: unit tests complete in seconds; the full suite in
  minutes, not tens of minutes
- **Deterministic**: a passing test today passes tomorrow. Flaky tests are
  treated as failing tests and fixed or quarantined within one working day
- **Reproducible**: any failure observed locally is reproducible by another
  developer with the same commit checked out

### 7.5.2 Required Test Types

| Type | Tool | Scope | Required? |
|---|---|---|---|
| Unit (backend) | xUnit + FluentAssertions | Domain + Application layers | ✅ Mandatory |
| Unit (frontend) | Vitest + Testing Library | Components, stores, utilities | ✅ Mandatory |
| Integration (API) | xUnit + Testcontainers | API + real PostgreSQL | ✅ Mandatory |
| Contract | Schemathesis or equivalent | OpenAPI spec ↔ live API | ✅ Mandatory |
| **End-to-End (UI)** | **Playwright** | **Full PWA against real stack** | ✅ **Mandatory** |
| Accessibility | axe-core (in Vitest + Playwright) | All UI routes | ✅ Mandatory |
| Performance | Lighthouse CI | Key routes (see §6.5.9) | ✅ Mandatory |
| Visual regression | Playwright snapshots | High-value views only | ⚠️ Optional (v2) |
| Load | k6 or NBomber | Throughput baselines | ⚠️ Optional (v2) |

### 7.5.3 Playwright (Mandatory E2E)
- **Playwright** is the required end-to-end testing tool. No alternative
  framework (Cypress, Selenium, Puppeteer, etc.) is accepted.
- Tests live in `tests/e2e/` at the repo root
- Tests run against the **full local Docker Compose stack**, not a mocked API
- **Browser matrix (minimum)**: Chromium, WebKit, Firefox — all three must
  pass in CI. Local dev may run Chromium-only for speed
- **Viewport matrix**: at minimum one mobile (375×667) and one desktop
  (1280×800) viewport per critical flow
- **Authentication**: a documented Playwright fixture provisions a test
  user via Entra ID test tenant or a documented local-dev auth bypass
  (the bypass is **never** available in production builds — enforced at
  the API layer)
- **Network policy**: Playwright tests must not reach external hosts; any
  outbound call to a non-localhost address fails the test
- **Data setup**: tests create their own data via API calls or a documented
  seed endpoint; tests must not rely on a pre-existing dataset
- **Tear-down**: each test cleans up data it created, or runs against a
  fresh database snapshot per test file
- **Traces & video**: Playwright traces and videos retained on failure;
  uploaded as CI artifacts; locally available in `playwright-report/`
- **Page Object Model**: shared UI affordances captured in `tests/e2e/pages/`;
  test files describe scenarios, not low-level selectors

### 7.5.4 Critical User Journeys (Playwright Coverage Required)
The following journeys **must** have at least one passing Playwright test
before any v1 release:

1. **Sign in** (Entra ID happy path; sign-out)
2. **Sign in denied** (user without role assignment is refused gracefully)
3. **Create device** (Member fills form, saves, sees device in list)
4. **Edit device** (Member changes fields, saves, sees changes persisted)
5. **Delete device** (Member deletes with confirmation; device gone from list)
6. **Browse and filter** (Viewer applies filters, results update, URL reflects
   state, reload preserves view)
7. **Detail view** (Viewer opens a device, sees all reference data resolved
   to human-readable labels)
8. **Import CSV** (Admin uploads a CSV; preview shown; errors surfaced;
   commit succeeds)
9. **Export CSV** (Admin exports current filtered view; downloaded file
   parses cleanly)
10. **Reference data admin** (Admin creates a new Location; it appears
    immediately in the device-create form)
11. **Role enforcement** (Viewer cannot see edit/delete affordances;
    direct navigation to edit routes is refused)
12. **Offline app shell** (PWA shell loads when API is unreachable;
    cached data viewable; mutations queued or refused gracefully)
13. **Accessibility smoke** (every route above passes axe-core with
    zero violations)

Additional journeys are added to this list as features ship. Removing a
journey from this list requires an ADR.

### 7.5.5 The `task test` Contract
A developer must be able to run the following from a clean checkout:

```bash
task up        # bring up the full local stack (API, web, db)
task test      # run all mandatory tests against the running stack
task down      # tear down
```

---

## 8. Data Model (High-Level)

> Detailed schema lives in `specs/001-core-api/data-model.md`.

### Core Entities
- **Device**: id, name, model, serial, brand, category, purchaseDate, purchasePrice, currency, status, ownerId, locationId, notes, retiredDate, disposalMethod, audit columns
- **Brand**: id, name, website, notes
- **Category**: id, name, parentId (hierarchical), icon
- **Owner**: id, displayName, entraObjectId, role
- **Location**: id, name, type (home/storage/external)
- **Tag**: id, name, color
- **DeviceTag**: deviceId, tagId
- **Attachment** (v2): id, deviceId, filename, contentType, size, uploadedBy
- **AuditEvent**: id, entityType, entityId, action, userId, timestamp, payload (JSON)
- **ImportBatch**: id, filename, importedBy, rowCount, status, log

### Key Invariants
- A Device's `ownerId` must reference an active Owner
- A retired Device is read-only except for `notes` and `disposalMethod`
- AuditEvents are append-only — never updated or deleted

---

## 9. Technical Architecture (Summary)

> Full detail in `docs/architecture.md`.

- **Backend**: ASP.NET Core 10 Web API, Clean Architecture (Domain / Application / Infrastructure / API)
- **Database**: sqllite, EF Core code-first migrations
- **Auth**: Microsoft Entra ID (External ID), OIDC + PKCE
- **Web client**: SvelteKit PWA, MSAL.js, generated TS client from OpenAPI
- **Deployment**: Docker Compose, self-hosted, Caddy reverse proxy with TLS
- **Observability**: Serilog → file/Seq, OpenTelemetry, healthchecks
- **CI/CD**: GitHub Actions, container images to GHCR

---

## 10. Constraints & Assumptions

### Constraints
- Must run on home infrastructure (single Docker host)
- Must use Entra ID — no other identity providers
- Must not require an internet connection for core read operations
- Initial dataset is the SharePoint CSV — schema must accommodate its quirks

### Assumptions
- Family is comfortable using Entra ID (or willing to learn once)
- Home network is reasonably reliable for write operations
- Admin will perform backups verification
- < 10,000 devices total over project lifetime

---

## 11. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Entra ID setup friction for family | High | Med | Step-by-step onboarding doc; magic-link fallback consideration in v2 |
| Self-hosted downtime | Med | Low | Offline read in PWA; weekly backup verification |
| Data loss | Low | High | Nightly backups, quarterly restore drills, off-host backup copy |
| SharePoint CSV schema surprises | High | Med | Dry-run import; flexible column mapping |
| Scope creep | High | Med | Strict backlog discipline; this PRD as gate |
| AI agent drift during development | Med | Med | Spec Kit + constitution + audits (see development docs) |
| Family doesn't adopt | Med | Low | Mobile-first PWA; minimize required interaction |
| Container/host compromise | Low | High | Non-root containers, read-only FS, network isolation, regular patching |

---

## 12. Success Metrics

### Launch Criteria (v1)
- [ ] 100% of SharePoint CSV imported without data loss
- [ ] All 3 user roles functional with Entra ID
- [ ] PWA installable on iOS and Android
- [ ] Deployable via single `docker compose up`
- [ ] CI quality gate green (tests, security scans, SBOM)
- [ ] Restore from backup verified

### Health Metrics (Post-Launch)
- 📈 Devices catalogued (target: 100% of household by 90 days)
- 📈 Family member sign-ins per month (target: ≥ 1/member)
- 📈 P95 API latency (target: < 300ms)
- 📈 CI quality gate pass rate (target: 100%)
- 📈 Audit findings: 0 High/Critical open > 7 days

---

## 13. Release Plan

> **Phase model rewritten 2026-05-19** to match what actually shipped. The
> original Phase 0–5 ordering (Foundation → Core API → Auth → Infrastructure →
> Web PWA → Hardening) is preserved in `git log` and in historical session
> notes for archeology; this section is the canonical going-forward truth.
> Cross-cutting work that landed during multiple phases (containerization,
> CI/CD, observability, Entra auth) is attributed to where it actually
> shipped rather than to a planned-but-never-spec'd phase.

### Phase 0 — Foundation (shipped)
- Project bootstrap, Spec Kit, constitution, PRD, threat model, references
- Backlog populated
- Local dev environment (Docker Compose, Taskfile, hermetic test stack)
- Pre-commit hooks (gitleaks), CI scaffold, squad established

### Phase 1 — Core API (shipped) → `specs/001-core-api`
- Domain model, EF Core, code-first migrations
- CRUD endpoints + OpenAPI document
- CSV import endpoint + pipeline
- Audit log (append-only `AuditEvent`)
- Unit + integration test harness with per-class SQLite isolation

### Phase 2 — Frontend MVP + Auth (shipped 2026-05-19) → `specs/002-frontend-mvp`
- SvelteKit PWA: device list/detail/create/edit, reference-entity admin,
  import/export, audit-log viewer (F021 v1)
- MSAL.js with `sessionStorage` token cache (four-gate enforcement per D-010)
- Real Microsoft Entra ID OIDC + PKCE (replaced `Auth:DevBypass` in prod
  binary); roles `Viewer` / `Member` / `Admin`
- Local-admin fallback (F025 v1b, ADR D-140)
- F020 (Profile/display-name), F022 (saved sort/filter prefs, localStorage),
  F023 (group-devices-by-dimension), F024 (multi-select bulk actions)
- *Originally split in the old plan as P2 (Auth) + P4 (Web PWA); they
  shipped together as one coherent slice.*

### Phase 3 — Production Launch (shipped 2026-05-19)
- Production cutover to `https://inventory.denicolafamily.com` (external
  TLS proxy / openresty)
- Multi-stream production commit `35103d4`, Entra issuer fixes (`9877a6f`),
  CI e2e + Trivy alpine upgrade (`e091c7f`), claim-mapping fix (`42e53a2`)
- Brian validated end-to-end: Entra sign-in as Admin, owner auto-provisioned
  on first sign-in (D-136), devices page renders, role-gated UI works
- *Originally planned as P3 (Infrastructure, Week 8) + P5 (Hardening &
  Launch, Weeks 13–14); shipped inside the frontend MVP effort.*

### Phase 4 — Continuous Iteration (current)
**Backlog-driven; no further linear phases planned.** Work pulls from
`specs/_backlog/` (F-numbered entries). Each entry triages → promotes via
Spec Kit `/specify` when it's worth a full spec, or ships directly when
small. The roll-up of in-flight and shipped backlog items lives in
`specs/_backlog/README.md`.

Current Phase-4 themes (each tracked as one or more `F0XX-*.md`):
- **PWA field-test follow-ups** — F026–F033 (captured 2026-05-19)
- **Reporting** — F032 (insurance-grade PDF) is the first concrete entry
  pulling what the old plan called "Phase 4 Polish & Reporting" into the
  backlog queue
- **Photo / vision** — F018 (AI draft) + F033 (capture + auto-creation)
- **Conversational** — F019 (household AI chatbot)
- **Admin surface follow-ups** — F021b (`/admin/logs`), F025b (local-admin
  power features)

### v2 Candidates (post-launch, not yet in backlog)
- Photo attachments (now subsumed under F033)
- Warranty reminders
- Depreciation tracking
- Location/lifecycle workflows
- Bulk-edit UI (partially shipped via F024)

> Active feature ideas tracked in `specs/_backlog/`.

---

## 14. Open Questions

- [ ] External ID vs. Workforce tenant — which is right for family use?
- [ ] Backup destination — second host on LAN, external drive, or encrypted off-site?
- [ ] Should disposed devices be hidden by default or shown with strikethrough?
- [ ] Currency handling — single household currency or per-device?
- [ ] Should kids' Viewer role hide purchase prices?
- [ ] Photo storage — DB blob, host volume, or object storage (MinIO)?

---

## 15. Glossary

| Term | Definition |
|---|---|
| **Device** | Any physical technology item being tracked |
| **Tech Era** | Curated time band (e.g., "Smart-Home Era 2014–2018") for timeline grouping |
| **Owner** | Family member to whom a device is assigned |
| **Location** | Where a device physically lives |
| **Retired** | Device no longer in active use but still tracked |
| **Disposed** | Device physically gone (sold, gifted, recycled, stolen, lost) |
| **Entra ID** | Microsoft's cloud identity platform |
| **PWA** | Progressive Web App — installable web app |
| **PRD** | This document |

---

## 16. References

- `.specify/memory/constitution.md` — engineering & security rules
- `docs/architecture.md` — technical architecture
- `docs/threat-model.md` — STRIDE analysis
- `specs/_backlog/` — feature pipeline
- `specs/00X-*/spec.md` — active feature specs
- OWASP ASVS, OWASP API Top 10 — security baselines
- WCAG 2.2 AA — accessibility baseline

---

## 17. Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 0.1 | 2025-01-XX | [You] | Initial draft |
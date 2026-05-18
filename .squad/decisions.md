# Squad Decisions

## Active Decisions

### D-001: Use Entra External ID (Not Workforce) — **PROPOSED**

**Proposed by:** Bishop (Security & Auth Specialist)  
**Date:** 2026-05-18  
**Status:** Awaiting Brian approval  
**Related:** PRD §14, `docs/auth-design.md`

Family authentication should use **Microsoft Entra External ID (CIAM model), not Workforce ID.** External ID is designed for consumer/partner scenarios, supports self-service sign-up (MSA, Google, Apple), and costs 70% less (~$1.50–7.50/mo vs. $20–40/mo for Workforce).

**Trade-offs:** Simpler ops and consumer-friendly UX (upside) vs. no significant downside for family use case.

**Next:** Brian approves or requests revision. Hicks updates spec to reference External ID. Phase 2 spike: Set up External ID tenant in Azure Portal.

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

### D-008: Currency Strategy — **OPEN**

**Proposed by:** Ripley (Lead Architect)  
**Date:** 2026-05-18  
**Status:** Open — **BLOCKS T04 in Phase 1 tasks.md**  
**Related:** PRD §14 (open question), `specs/001-core-api/spec.md`

**Two options:**

1. **Single currency** — Household-wide setting (e.g., USD); `Device.PurchasePrice` is decimal, currency implied.
2. **Per-device currency** — `Device.PurchasePrice` (decimal) + `Device.Currency` (ISO 4217 string, 3 chars, nullable); household default in config; CSV import stores whatever exists.

**Considerations:** Single-household app (most purchases one currency) but imported SharePoint data may have mixed currencies (gifts, travel). Per-device avoids data loss; minimal schema cost (3 chars/row). No exchange-rate conversion needed (display as stored).

**Proposal:** Per-device with household default. Migration to single-currency later is trivial if needed.

**Decision needed from:** Brian — confirm per-device approach or mandate single-currency.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

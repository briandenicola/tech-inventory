# Tech Inventory E2E Tests (Playwright)

**Playwright is the only E2E framework** — no alternatives accepted (constitution-level rule).

## Prerequisites

1. **Full stack running**: `task up` (API + Web + DB) from repo root
2. **Node.js** installed (v18+ recommended)
3. **Playwright browsers** installed (see below)

## Setup

```bash
cd tests/e2e

# Install dependencies
node "C:\Utils\node-v24.14.0\node_modules\npm\bin\npm-cli.js" install

# Install Playwright browsers (first time only)
npx playwright install --with-deps
```

## Running Tests

```bash
# Run all tests (headless, all browsers)
npx playwright test

# Run single test file
npx playwright test journeys/03-create-device.spec.ts

# Run with UI mode (interactive)
npx playwright test --ui

# Run in headed mode (see browser)
npx playwright test --headed

# Run specific browser only
npx playwright test --project=chromium-desktop

# Debug a single test
npx playwright test journeys/03-create-device.spec.ts --debug
```

## Test Organization

- **`journeys/`** — The 13 critical user journeys (PRD §7.5.4). All must pass before v1.
- **`pages/`** — Page Object Model; shared UI affordances extracted here.
- **`fixtures/`** — Test utilities: auth, network isolation, axe-core.

## Browser Matrix

CI runs all six project configurations:
- Chromium (desktop 1280×800 + mobile 375×667)
- WebKit (desktop 1280×800 + mobile 375×667)
- Firefox (desktop 1280×800 + mobile 375×667)

Local dev may run Chromium-only for speed: `npx playwright test --project=chromium-desktop`

## Reports

After a test run:

```bash
# View HTML report
npx playwright show-report
```

Reports land in `playwright-report/`. Traces and videos retained on failure.

## Network Isolation

All tests enforce **localhost-only** network access. Any outbound call to a non-localhost address fails the test. This prevents accidental dependencies on third-party services.

## Authentication

TODO: Wire once Bishop delivers Entra ID test tenant fixture OR local-dev auth bypass.

See `fixtures/auth.ts` for placeholder. Reference: `docs/auth-design.md`.

## Accessibility

Journey #13 verifies all routes pass axe-core with **zero violations**. Use the `runAxe(page)` helper from `fixtures/axe.ts`.

## The 13 Critical Journeys

All must be green before v1 (PRD §7.5.4):

1. Sign in (Entra happy path; sign-out)
2. Sign in denied (no role)
3. Create device (Member)
4. Edit device (Member)
5. Delete device (Member, confirmation)
6. Browse and filter (Viewer; URL state, reload preserves view)
7. Detail view (Viewer; reference data resolved to labels)
8. Import CSV (Admin; preview, errors, commit)
9. Export CSV (Admin; filtered view parses cleanly)
10. Reference data admin (Admin creates Location, appears in form)
11. Role enforcement (Viewer cannot see edit/delete, direct nav refused)
12. Offline app shell (cached data viewable, mutations queued/refused gracefully)
13. Accessibility smoke (every route above passes axe-core, zero violations)

Removing a journey from this list requires an ADR.

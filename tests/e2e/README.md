# Tech Inventory E2E Tests (Playwright)

**Playwright is the only E2E framework** — no alternatives accepted (constitution-level rule).

## Prerequisites

1. **Node.js** installed (v18+ recommended)
2. **Playwright browsers** installed (see below)
3. For manual runs, either:
   - start the full stack with `task up`, then run `task test` or `npx playwright test`, or
   - use the one-shot `task test:e2e` command to bring the stack up, wait for `/health/ready`, run Playwright, and tear it back down

## Setup

```bash
cd tests/e2e

# Install dependencies
npm ci

# Install Playwright browsers (first time only)
npx playwright install --with-deps
```

## Running Tests

```bash
# Run the hermetic compose-backed contract end-to-end
cd ../..
task test:e2e

# Or, with the stack already running, execute Playwright directly
cd tests/e2e
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
- **`security/`** — Focused security assertions (for example, token-storage enforcement).
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

Tests do **not** go through the real Entra redirect. Two paths are wired:

- **Dev-bypass JWT fixture** — `fixtures/auth.ts` exposes
  `useAuthenticatedPage(role)`, which mints a test JWT against the API's
  dev-only `JwtBearer` scheme and primes sessionStorage before navigation.
  Use this for every journey that needs an authenticated user.
- **Local-account fallback** — for journeys that specifically exercise F025
  v1b, sign in through `LocalLoginForm` against `POST /api/v1/auth/local/login`;
  the token lands in sessionStorage under `ti_local_token` and the same
  `useAuthenticatedPage` helper covers the rest of the journey.

End-to-end auth design lives in `docs/auth-design.md` (Workforce tenant +
F025 v1b §6); per-test usage of the fixture is documented inline in
`fixtures/auth.ts`.

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

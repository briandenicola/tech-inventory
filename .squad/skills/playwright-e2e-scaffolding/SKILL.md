# SKILL: Playwright E2E Scaffolding

**Purpose:** Stand up a Playwright E2E test suite with network isolation, axe-core integration, and Page Object Model structure.

**When to use:**
- New greenfield project needs E2E tests
- Existing project wants to add Playwright (no other E2E framework present)
- Need to enforce cross-browser (Chromium/WebKit/Firefox) + cross-viewport (desktop/mobile) coverage

**Inputs:**
- `BASE_URL` (default: `http://localhost:5173` or configurable)
- List of critical user journeys (optional; will stub with `test.todo()`)
- Browser matrix preference (default: Chromium, WebKit, Firefox)
- Viewport sizes (default: desktop 1280×800, mobile 375×667)

**Outputs:**
- `tests/e2e/` directory structure:
  - `playwright.config.ts` with multi-browser, multi-viewport projects
  - `pages/` (Page Object Model convention + README)
  - `fixtures/` (auth placeholder, network isolation, axe-core helper)
  - `journeys/` (stubbed test files for each critical journey)
  - `README.md` (setup + run instructions)
- Updated `.gitignore` with `playwright-report/`, `test-results/`
- `package.json` with `@playwright/test` and `@axe-core/playwright`

## Steps

1. **Create directory structure:**
   ```bash
   mkdir -p tests/e2e/{pages,fixtures,journeys}
   cd tests/e2e
   npm init -y
   npm install --save-dev @playwright/test @axe-core/playwright
   ```

2. **Generate `playwright.config.ts`:**
   - Define `projects` array with 6 configurations (3 browsers × 2 viewports)
   - Set `baseURL`, `trace`, `video`, `screenshot` policies
   - Comment out `webServer` auto-start (developers must start stack manually)

3. **Create fixtures:**
   - `fixtures/auth.ts` — placeholder for test auth (extend later)
   - `fixtures/network.ts` — `page.route('**/*', ...)` to block non-localhost
   - `fixtures/axe.ts` — `runAxe(page)` wrapper around AxeBuilder

4. **Stub critical journeys:**
   - For each journey: create `journeys/NN-slug.spec.ts` with `test.describe.skip()` and `test.todo()` placeholders
   - Document journey purpose in file header comment

5. **Document conventions:**
   - `pages/README.md` — Page Object Model rules (actions not selectors, `data-testid` preference)
   - `tests/e2e/README.md` — Prerequisites, setup, run commands, browser matrix

6. **Update `.gitignore`:**
   ```
   playwright-report/
   test-results/
   ```

7. **Verify:**
   ```bash
   npx playwright install chromium  # Local dev
   npx playwright test --list       # Should list all journey stubs without errors
   ```

## Example `playwright.config.ts` Snippet

```typescript
export default defineConfig({
  testDir: './journeys',
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5173',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium-desktop',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 800 } },
    },
    {
      name: 'chromium-mobile',
      use: { ...devices['Pixel 5'], viewport: { width: 375, height: 667 } },
    },
    // ... webkit, firefox variants
  ],
});
```

## Example Network Isolation Fixture

```typescript
// fixtures/network.ts
import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
    await page.route('**/*', (route) => {
      const url = new URL(route.request().url());
      const isLocalhost = url.hostname === 'localhost' || url.hostname === '127.0.0.1';
      if (isLocalhost) {
        route.continue();
      } else {
        route.abort('failed');
      }
    });
    await use(page);
  },
});
```

## Example axe-core Helper

```typescript
// fixtures/axe.ts
import { Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

export async function runAxe(page: Page) {
  const results = await new AxeBuilder({ page }).analyze();
  return results.violations;
}

// Usage in test:
// const violations = await runAxe(page);
// expect(violations).toEqual([]);  // Zero violations to merge
```

## Integration with CI

```yaml
# GitHub Actions snippet
- name: Install Playwright browsers
  run: |
    cd tests/e2e
    npx playwright install --with-deps

- name: Run Playwright tests
  run: |
    cd tests/e2e
    npx playwright test

- name: Upload Playwright report
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: playwright-report
    path: tests/e2e/playwright-report/
```

## Maintenance Notes

- **Adding a journey:** Create `journeys/NN-new-journey.spec.ts`, follow existing stub pattern
- **Changing viewports:** Update `playwright.config.ts` projects; ensure design tokens align
- **Browser updates:** `npx playwright install` pulls latest stable browsers
- **Flaky tests:** Fix within one working day or quarantine with `.skip` + tracking issue

## Related Patterns

- **Page Object Model:** Extract reusable UI affordances to `pages/` as methods, not raw selectors
- **Test data ownership:** Each test creates/cleans its own data; no shared fixtures across files
- **Hermetic testing:** No external network calls; use `fixtures/network.ts` enforcement

## Success Criteria

✅ `npx playwright test --list` shows all journey stubs  
✅ Config loads without errors  
✅ Running a stub test (even if skipped) doesn't crash  
✅ Network isolation blocks non-localhost calls  
✅ axe-core helper returns violations array  
✅ README documents setup and run commands clearly  

## Anti-Patterns to Avoid

❌ **Auto-starting stack from `webServer` config** — breaks `task test` contract (stack should be running already)  
❌ **Hardcoding selectors in test files** — use Page Objects  
❌ **Mocking API responses in E2E** — defeats the purpose; use real API  
❌ **Skipping browsers in CI** — all three (Chromium, WebKit, Firefox) must pass  
❌ **Ignoring flaky tests** — treat as bugs, not noise  

---

**Reusability:** This skill is project-agnostic. Adapt viewport sizes, journey count, and baseURL per project needs.

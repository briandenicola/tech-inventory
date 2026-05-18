import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Tech Inventory E2E tests.
 * 
 * Browser matrix: Chromium, WebKit, Firefox (CI runs all three; local dev may use Chromium-only).
 * Viewport matrix: mobile (375×667) and desktop (1280×800) per critical flow.
 * Network policy: all tests fail on non-localhost outbound calls (enforced via page.route).
 * 
 * DO NOT auto-start servers from here — use `task up` + `task test` or the one-shot `task test:e2e` contract.
 */
export default defineConfig({
  testDir: '.',
  testMatch: ['journeys/**/*.spec.ts', 'security/**/*.spec.ts'],
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['line']
  ],

  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:3000',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium-desktop',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 800 },
      },
    },
    {
      name: 'chromium-mobile',
      use: {
        ...devices['Pixel 5'],
        viewport: { width: 375, height: 667 },
      },
    },
    {
      name: 'webkit-desktop',
      use: {
        ...devices['Desktop Safari'],
        viewport: { width: 1280, height: 800 },
      },
    },
    {
      name: 'webkit-mobile',
      use: {
        ...devices['iPhone 12'],
        viewport: { width: 375, height: 667 },
      },
    },
    {
      name: 'firefox-desktop',
      use: {
        ...devices['Desktop Firefox'],
        viewport: { width: 1280, height: 800 },
      },
    },
    {
      name: 'firefox-mobile',
      use: {
        ...devices['Pixel 5'],
        viewport: { width: 375, height: 667 },
      },
    },
  ],

  // DO NOT uncomment webServer — use `task up` / `task test` or `task test:e2e`
  // webServer: {
  //   command: 'task up',
  //   url: 'http://localhost:3000',
  //   reuseExistingServer: !process.env.CI,
  // },
});

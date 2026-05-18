/**
 * Authentication fixture for Playwright tests.
 * 
 * TODO: Wire this once Bishop delivers Entra ID test tenant documentation OR local-dev auth bypass.
 * 
 * Two supported modes:
 * 1. **Entra ID test tenant** (preferred for CI): provision a test user via documented fixture
 * 2. **Local-dev auth bypass** (developer convenience): use documented bypass flag 
 *    (NEVER available in production builds — enforced at API layer)
 * 
 * Reference: docs/auth-design.md (Bishop's territory)
 */

import { test as base } from '@playwright/test';

export const test = base.extend({
  // TODO: Extend with authenticated context once auth is wired
  // authenticatedPage: async ({ page }, use) => { ... }
});

export { expect } from '@playwright/test';

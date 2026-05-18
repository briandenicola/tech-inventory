/**
 * Journey 2: Sign in denied (user without role assignment is refused gracefully)
 * 
 * PRD §7.5.4: Critical user journey #2
 * Verifies users without role assignments are denied access with a helpful message.
 */

import { test, expect } from '@playwright/test';

test.describe.skip('Journey 2: Sign in denied', () => {
  test.todo('User without role assignment sees access denied message');
  test.todo('Access denied message is helpful and actionable');
});

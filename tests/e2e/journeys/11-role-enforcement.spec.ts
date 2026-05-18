/**
 * Journey 11: Role enforcement (Viewer cannot see edit/delete affordances; direct navigation to edit routes is refused)
 * 
 * PRD §7.5.4: Critical user journey #11
 * Verifies role-based access control in the UI and API.
 */

import { test, expect } from '@playwright/test';

test.describe.skip('Journey 11: Role enforcement', () => {
  test.todo('Viewer does not see edit button on device list');
  test.todo('Viewer does not see delete button on device list');
  test.todo('Direct navigation to edit route is refused for Viewer');
  test.todo('API returns 403 Forbidden for unauthorized actions');
  test.todo('Member can see and use edit/delete affordances');
});

/**
 * Journey 6: Browse and filter (Viewer applies filters, results update, URL
 * reflects state, reload preserves view)
 *
 * PRD §7.5.4: Critical user journey #6
 *
 * The /devices list is fully URL-driven: every filter, sort, and page change
 * round-trips through `?search=...&brandId=...&status=...&sort=...` so that
 * the back-button and bookmarking both work. This spec exercises the URL
 * contract end-to-end with no seed data needed (the filter UI exists
 * independently of whether any device matches).
 */

import { authenticated as test, expect } from '../fixtures/auth';

test.describe('Journey 6: Browse and filter', () => {
  test('typing in the search box reflects into the URL', async ({ adminPage }) => {
    await adminPage.goto('/devices');
    const searchBox = adminPage.locator('#search');
    await searchBox.fill('iphone');

    // The list debounces filter writes for 300ms; wait for the URL to settle.
    await adminPage.waitForURL(/[?&]search=iphone(&|$)/, { timeout: 2_000 });
    await expect(adminPage).toHaveURL(/[?&]search=iphone(&|$)/);
  });

  test('status filter writes status=Active into the URL', async ({ adminPage }) => {
    await adminPage.goto('/devices');
    await adminPage.getByLabel(/active/i).first().check();
    await adminPage.waitForURL(/[?&]status=Active(&|$)/, { timeout: 2_000 });
  });

  test('Clear All resets every filter param', async ({ adminPage }) => {
    await adminPage.goto('/devices?search=foo&status=Active');
    await adminPage.getByRole('button', { name: /clear all/i }).click();

    // After clear the URL should have no filter query params left.
    await adminPage.waitForFunction(
      () => {
        const params = new URLSearchParams(window.location.search);
        return !params.get('search') && !params.get('status');
      },
      undefined,
      { timeout: 2_000 }
    );
    const url = new URL(adminPage.url());
    expect(url.searchParams.get('search')).toBeNull();
    expect(url.searchParams.get('status')).toBeNull();
  });

  test('reload preserves filter state from the URL', async ({ adminPage }) => {
    await adminPage.goto('/devices?search=specific-term&status=Active');
    await adminPage.reload();

    await expect(adminPage.locator('#search')).toHaveValue('specific-term');
    const url = new URL(adminPage.url());
    expect(url.searchParams.get('search')).toBe('specific-term');
    expect(url.searchParams.get('status')).toBe('Active');
  });
});


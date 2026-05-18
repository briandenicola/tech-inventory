import { test, expect } from '@playwright/test';
import { inspectStorage, tokenKeyPattern } from './storage-inspection';

test.describe('Token storage enforcement', () => {
  test('mocked login keeps token-like keys out of localStorage and allows MSAL sessionStorage keys', async ({ page }) => {
    await page.route('http://localhost/mock-login', async (route) => {
      await route.fulfill({
        contentType: 'text/html',
        body: `
          <button id="login">Mock sign in</button>
          <script>
            window.localStorage.clear();
            window.sessionStorage.clear();
            document.getElementById('login').addEventListener('click', () => {
              window.localStorage.setItem('theme', 'dark');
              window.sessionStorage.setItem('msal.account.keys', '["home-account-id"]');
              window.sessionStorage.setItem('msal.accessToken', 'opaque-token');
            });
          </script>
        `
      });
    });

    await page.goto('http://localhost/mock-login');
    await page.getByRole('button', { name: 'Mock sign in' }).click();

    const storage = await inspectStorage(page);
    const localTokenKeys = storage.localStorageKeys.filter((key) => tokenKeyPattern.test(key));

    expect(localTokenKeys).toEqual([]);
    expect(storage.sessionStorageKeys.some((key) => /msal/i.test(key))).toBe(true);
  });
});

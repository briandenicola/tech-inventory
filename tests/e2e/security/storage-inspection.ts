import type { Page } from '@playwright/test';

export const tokenKeyPattern = /token|jwt|access|refresh|id_token|msal/i;

export type StorageSnapshot = {
  localStorageKeys: string[];
  sessionStorageKeys: string[];
};

export async function inspectStorage(page: Page): Promise<StorageSnapshot> {
  return page.evaluate(() => ({
    localStorageKeys: Object.keys(window.localStorage),
    sessionStorageKeys: Object.keys(window.sessionStorage)
  }));
}

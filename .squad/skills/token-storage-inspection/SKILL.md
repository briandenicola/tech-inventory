# Token Storage Inspection

Use this pattern when a Playwright test must prove auth state never lands in `localStorage`.

## Steps
1. Clear both browser storages before the auth action.
2. Drive the login or mocked-login flow.
3. Use `page.evaluate` to return `Object.keys(window.localStorage)` and `Object.keys(window.sessionStorage)`.
4. Fail if any localStorage key matches `/token|jwt|access|refresh|id_token|msal/i`.
5. Allow `sessionStorage` to contain MSAL keys.

## Current Project Helper
- `tests/e2e/security/storage-inspection.ts`
- `tests/e2e/security/token-storage.spec.ts`

## Notes
- Assert on keys, not values.
- Keep the regex centralized so every token-storage test uses the same policy gate.
- When real auth is wired, replace the mocked page setup only; keep the storage assertions unchanged.

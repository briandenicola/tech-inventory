# Token Storage Inspection

Use this pattern when a unit test must prove auth state never lands in `localStorage`.

**Harness note:** this pattern originally described a browser-automation
assertion. That harness is retired
(`specs/004-agentic-development-foundation/brief.md` §2.1); the durable
replacement is a unit-level assertion on the MSAL cache configuration
(`coverage-migration.md` §3.16). The real, live redirect flow is a manual
checklist item (**G-08**/**M-11** in
`docs/testing/manual-pwa-validation.md`), not an automated one.

## Steps
1. Assert the auth client's cache configuration directly — no browser, no
   mocked page. For MSAL.js: `msalConfig.cache.cacheLocation === BrowserCacheLocation.SessionStorage`.
2. Do not assert on live `localStorage`/`sessionStorage` keys in a jsdom
   unit test — jsdom doesn't reproduce a real redirect flow, and asserting
   on keys you wrote yourself is a tautology, not a real check (this was the
   defect in the original browser-automation version of this test).
3. Keep the config-shape assertion centralized so every token-storage test
   uses the same policy gate.
4. When the real browser redirect flow needs verifying, that's the manual
   checklist (`docs/testing/manual-pwa-validation.md` **M-11**), not a unit
   test — record the run there, don't invent a browser check to replace it.

## Current Project Helper
- `src/TechInventory.Web/src/lib/auth/msal.test.ts`

## Notes
- Assert on config, not on values written by the test itself.
- The ESLint `no-restricted-syntax` rule (`eslint.config.js`) and the
  pre-commit/gitleaks scan are the other two enforced gates in the four-gate
  model; code review is the fourth.

# D-TBD: Phase 2 T05 — MSAL.js v3 Configuration Decisions

**Agent:** Vasquez (Frontend Lead)  
**Date:** 2026-05-19  
**Task:** T05 — Configure MSAL.js for Entra Workforce  
**Related:** specs/002-frontend-mvp/spec.md §4.1, docs/auth-design.md §2, D-039 (Tenant/Client ID provisioning)

---

## 1. MSAL Version — v3.28.0

**Choice:** Use MSAL.js v3.28.0 (already installed in package.json from Round 0).

**Rationale:**
- v3 is latest stable (@azure/msal-browser)
- v4 does not exist yet (v3.x is current as of 2026)
- v2 rejected (old API, missing features)
- v3.28.0 specifically chosen as it's the pinned version from initial scaffold

**Alternative considered:** Upgrading to latest v3.x patch (e.g., 3.30+). Deferred — 3.28.0 is stable and sufficient for T05; upgrade in Phase 3 if needed.

---

## 2. Cache Location — sessionStorage (NEVER localStorage)

**Choice:** `cacheLocation: BrowserCacheLocation.SessionStorage`

**Rationale:**
- Constitution §7: "Tokens in memory or sessionStorage, **never** localStorage"
- D-002: Token storage decision — four-gate enforcement (ESLint, pre-commit, Playwright, code review)
- sessionStorage is tab-scoped; cleared on tab close; better XSS defense than localStorage
- Aligns with OWASP ASVS L2 (V2.10.2)

**Alternative rejected:** localStorage (blocked by constitution + ESLint rule `security/no-auth-token-localstorage` from Phase 1 Round 1).

**Implementation:** `storeAuthStateInCookie: false` — modern browsers handle PKCE redirects without cookie fallback; cleaner auth state isolation.

---

## 3. Auth Flow — Redirect (NOT Popup)

**Choice:** `acquireTokenRedirect` with silent re-acquisition fallback.

**Rationale:**
- Popups blocked by default in most browsers (poor UX)
- Redirect is constitution-aligned default: "Use `acquireTokenRedirect` not popup — popups are blocked / poor UX" (copilot-instructions.md)
- docs/auth-design.md §3 shows redirect flow as canonical design

**Flow implemented:**
1. Silent token acquisition (`acquireTokenSilent`) — tries cached token or refresh token
2. If `InteractionRequiredAuthError`, fall back to `acquireTokenRedirect` (user redirected to Entra)
3. After redirect, `handleRedirectPromise()` processes auth code on app load

**Alternative rejected:** `acquireTokenPopup` — blocked by browsers, worse mobile UX, requires user interaction.

---

## 4. Redirect URI — Derived from `window.location.origin`

**Choice:** `redirectUri: window.location.origin` (dynamic).

**Rationale:**
- Zero config drift between dev (http://localhost:5173) and prod (deployed URL)
- Entra app registration has both URIs whitelisted
- SvelteKit SSR-safe: `typeof window !== 'undefined'` guard prevents SSR crashes
- Simpler than environment variables for this value (no VITE_REDIRECT_URI needed)

**Trade-off:** Azure Portal must register BOTH dev + prod URIs. Brian completed T01 with both registered.

**Alternative rejected:** Hardcoded `http://localhost:5173` + env var for prod. Adds config complexity; dynamic derivation is simpler and safer.

---

## 5. Token Acquisition — Silent + Redirect Fallback

**Choice:** `acquireTokenSilent` first, `acquireTokenRedirect` on interaction required.

**Rationale:**
- Silent acquisition is fast (uses cached token or refresh token) — no user interaction needed
- MSAL v3+ automatically handles token refresh if cached token is near expiry
- If silent fails with `InteractionRequiredAuthError` (token expired, consent needed), redirect to Entra for interactive auth
- Constitution: "acquireTokenRedirect with silent re-acquisition for API calls"

**Error handling:**
- If no active account (user not signed in), return null — Round 2 route guard (T12) will redirect to login before protected API calls
- Network failures or other errors are rethrown (handled by calling code)

**Alternative rejected:** Always redirect (slower; bad UX). Always popup (blocked; worse mobile UX).

---

## 6. Bootstrap Pattern — onMount in +layout.svelte

**Choice:** Wire `initializeMsal()` + `handleRedirectPromise()` in root `+layout.svelte` `onMount`.

**Rationale:**
- MSAL v3+ requires `initialize()` before any other operation (throws if called out-of-order)
- `handleRedirectPromise()` must run on every page load to process auth redirect callback
- `onMount` is client-only (SSR-safe) — MSAL.js requires browser APIs (window, sessionStorage)
- Root layout ensures bootstrap runs once before any route renders
- Bootstrap errors logged but don't block app render (auth failures surface when user accesses protected routes)

**Alternative rejected:** `+layout.ts` load function. Works but less ergonomic (must return empty object; `onMount` is more idiomatic for side-effect-only bootstrap).

---

## 7. Scope Configuration — API scope + openid + profile

**Choice:**
- `loginRequest.scopes`: `[API_SCOPE, 'openid', 'profile']` (full sign-in)
- `apiTokenRequest.scopes`: `[API_SCOPE]` (silent re-acquisition for API calls)

**Rationale:**
- `API_SCOPE` = `api://60341158-b5af-4216-8140-a4c321f1e79c/access_as_user` (Entra app ID URI + delegated permission)
- `openid` + `profile` required for OIDC user info (email, name) — needed for first-login onboarding (T07 in Round 1)
- `apiTokenRequest` omits openid/profile (narrower scope; faster silent acquisition)

**Alternative rejected:** Omitting openid/profile from loginRequest. Bishop's Round 1 backend (T07) needs user claims from JWT for onboarding.

---

## 8. Tenant/Client ID Provisioning — Inline Constants (Per D-039)

**Choice:** Commit Tenant ID + Client ID + Authority + API scope as inline constants in `msal.ts`.

**Rationale:**
- D-039 (Decision 5 in coordinator doc): These are public values visible in OAuth redirects + JWT issuer/audience — NOT secrets
- OIDC + PKCE flow is a public client (no client secret required)
- Simpler than environment variables (no `.env` file, no VITE_ prefix pollution)
- Documented in decision doc so future maintainers don't try to "fix" the apparent leak

**Values committed:**
```ts
const ENTRA_TENANT_ID = 'b2108b29-ea40-4fee-b229-e3100835667e';
const ENTRA_CLIENT_ID = '60341158-b5af-4216-8140-a4c321f1e79c';
const API_SCOPE = 'api://60341158-b5af-4216-8140-a4c321f1e79c/access_as_user';
```

**Alternative rejected:** Environment variables. Adds indirection; requires .env.example; no security benefit (values are public).

---

## 9. API Client Integration — setApiConfig Hook

**Choice:** Wire `acquireApiToken` into API client via `setApiConfig({ getAuthToken: acquireApiToken })`.

**Rationale:**
- API client (client.ts) has existing hook: `clientConfig.getAuthToken` (designed in T02 for T05 wiring)
- Bootstrap module (`src/lib/api/index.ts`) imports client + auth, wires them together
- Root layout imports `$lib/api` (side-effect import) to execute wiring at app startup
- Clean separation: auth module doesn't know about API client; API client doesn't know about MSAL; bootstrap glues them

**Alternative rejected:** Import acquireApiToken directly in client.ts. Tighter coupling; harder to test; violates SRP.

---

## 10. Deviations from docs/auth-design.md

**None.** T05 implements exactly what auth-design.md §2 (MSAL.js Configuration) and §3 (OIDC + PKCE flow) specify.

**One clarification:** auth-design.md shows environment variables (`import.meta.env.VITE_ENTRA_CLIENT_ID`). Per D-039, we commit inline constants instead (public values, not secrets). This is an implementation detail; the design intent (Workforce tenant, PKCE, sessionStorage) is unchanged.

---

## Next Steps

- **T09 (Round 2):** Login/logout buttons + UI flow (Vasquez)
- **T06-T08 (Round 1):** Backend JWT validation + first-login onboarding (Bishop + Apone)
- **T10 (Round 2):** Auth store + current user context (Vasquez)
- **T12 (Round 2):** Protected route guard (Vasquez)

---

## Promotion Notes for Scribe

- Assign decision number (next available after D-039; likely D-040)
- Cross-link to D-039 (Tenant/Client ID provisioning), D-002 (token storage), specs/002-frontend-mvp/spec.md §4.1
- Move this file from `.squad/decisions/inbox/` → merge into `.squad/decisions.md`

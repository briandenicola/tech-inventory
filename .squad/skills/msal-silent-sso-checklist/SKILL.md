# SKILL: MSAL Silent SSO Checklist

**Purpose:** Audit or polish the Entra/MSAL login bootstrap so returning users re-enter smoothly without breaking logout, cold starts, or protected-route redirects.

**When to use:**
- Silent-first auth bootstrap in SvelteKit layouts
- MSAL session restore work
- Login/logout regressions involving second tabs, timeouts, or redirect loops

## Steps

1. **Hydrate local fallback first**
   - Restore any local break-glass session before touching MSAL.
   - Keep token storage in `sessionStorage` or memory only.

2. **Process redirects before silent auth**
   - `handleRedirectPromise()` must run before route redirects or login-page decisions.
   - If a redirect result returns an account, promote it immediately.

3. **Use two silent paths**
   - Use `acquireTokenSilent(...)` when the tab already has a cached MSAL account.
   - Use `ssoSilent(loginRequest)` when a second tab has no per-tab MSAL cache but the browser still has an Entra session cookie.

4. **Bound bootstrap time**
   - Put a short timeout on the root bootstrap silent attempt so `/auth/login` cannot spin forever.
   - Keep normal mid-session token refresh on MSAL's default timing.

5. **Make logout sticky**
   - Suppress silent SSO after explicit sign-out until the next deliberate sign-in.
   - If `logoutRedirect()` fails, clear the MSAL cache in-place before routing back to login.

6. **Backstop protected routes**
   - Load-time guards are not enough when auth starts in `isLoading=true`.
   - Add a client-side redirect backstop once auth bootstrap settles and no user exists.

## Validation

- Returning tab with cached MSAL account goes straight into the app.
- Fresh second tab uses `ssoSilent` and skips the login screen when Entra session cookies still exist.
- Network/iframe failure reveals the sign-in button after the timeout instead of hanging.
- Explicit logout lands on login and does not silently re-enter until the user clicks sign in.
- Mid-session API calls still use silent token refresh without bouncing to `/auth/login`.

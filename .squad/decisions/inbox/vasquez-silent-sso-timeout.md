# D-165: Silent SSO Bootstrap Timeout

**Proposed by:** Vasquez (Frontend Developer)
**Date:** 2026-05-21
**Status:** Proposed (awaiting Brian's approval)
**Related:** D-050, D-150, F038

## Decision

- Apply a `3000ms` timeout to the root-layout silent SSO bootstrap before the login page is revealed.
- Keep the timeout scoped to first-load bootstrap only; normal mid-session token refresh keeps MSAL's default timing.
- If the timeout elapses, treat the user as unauthenticated for UX purposes and show the sign-in button/local fallback instead of spinning forever.

## Rationale

Silent SSO is a polish feature, not something that should trap the app behind an indefinite splash when Entra or the iframe path is unavailable. Three seconds is long enough to restore a healthy cached session on normal networks, but short enough that a cold start without a session still feels responsive.

## Implications

- `tryAcquireApiTokenSilent(...)` accepts an optional timeout for bootstrap callers.
- The login page keeps its existing delayed spinner (`600ms`) so no-session visits still feel like a quick miss rather than a broken load.
- Future silent-auth work should preserve the same rule: bounded bootstrap, unbounded mid-session refresh.

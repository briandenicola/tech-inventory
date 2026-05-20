### 2026-05-19T01:42Z: D-136 — Auto-provision owners on first sign-in
**By:** Bishop (via Copilot)
**What:**
- Added EnsureCurrentOwnerProvisionedCommand: upserts an Owner row keyed by EntraObjectId from claims; returns existing or newly created
- OwnersController.GetCurrentOwner now dispatches this command instead of the read-only query — /owners/me always succeeds for any authenticated principal
- Display name + role derived from claims (ClaimTypes.Name, ClaimTypes.Role); fallbacks: "User {short}" / Member
- Extended ICurrentUserService with GetDisplayName() / GetRoleClaim() helpers
- Tests cover: existing owner returned unchanged, missing owner auto-provisioned with claim defaults, dev-admin can hit /owners/me with a fresh DB
**Why:** /owners/me was 404-ing for the dev bypass user (and would 404 for every real Entra user on first sign-in) because nothing provisioned the Owner row. Auto-provision on first call is the standard OIDC pattern and matches the constitution's "single-household, Entra-authenticated" shape — every authenticated principal is implicitly a household member.
**Follow-ups (not done this round):**
- First-user-as-Admin policy: should the first-ever sign-in be force-promoted to Admin? Currently trusting the role claim. Defer to Brian.
**Pairs with:** D-133 (Hicks CORS), D-134 (Vasquez relative URL), D-135 (Hudson reverse proxy)

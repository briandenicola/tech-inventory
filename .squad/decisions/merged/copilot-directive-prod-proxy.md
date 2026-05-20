### 2026-05-18T20:20Z: User directive — Production architecture
**By:** Brian (via Copilot)
**What:**
- Production web URL: `https://inventory.denicolafamily.com`
- API is on the same Docker network, NOT exposed externally
- Web container reverse-proxies `/api/*` requests to the API container internally
- Therefore: browser sees all API calls as same-origin in production (no CORS needed across origins)
- CORS in production is defense-in-depth only; the real wall is the proxy boundary

**Why:** User-set production deployment shape — informs CORS policy, frontend API base URL strategy, and docker-compose web container configuration.

**Implications for in-flight + upcoming work:**
1. **Hicks (CORS fix, in-flight):** Config-driven origins is correct. Production appsettings should list `https://inventory.denicolafamily.com` for belt-and-suspenders, even though same-origin via proxy means CORS doesn't fire in practice. Dev origin remains `http://localhost:5173`.
2. **Vasquez (follow-up):** Frontend API client base URL must be environment-aware — absolute (`http://localhost:8080`) in dev, RELATIVE (`/api/v1/...`) in prod so the proxy can pick it up. Likely a Vite env var (`VITE_API_BASE_URL`) defaulting to empty in prod.
3. **Hudson (follow-up):** docker-compose `web` service needs a reverse proxy layer (nginx, Caddy, or SvelteKit's `adapter-node` with a custom server) that forwards `/api/*` to `http://api:8080/api/*` on the internal docker network. SvelteKit static build + nginx is one common path; SvelteKit Node adapter with built-in proxy is another.

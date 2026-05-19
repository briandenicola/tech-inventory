### 2026-05-19T01:39Z: D-134 — Env-aware API client base URL (prod same-origin, dev cross-origin)
**By:** Vasquez (via Copilot)
**What:**
- API client default base URL now `''` (relative) in prod builds, `http://localhost:8080` in dev
- `VITE_API_BASE_URL` env var still wins if set (per-environment override)
- Added `src/TechInventory.Web/.env.development` so dev workflow is explicit
**Why:** Production deploys SvelteKit static bundle behind a web container that reverse-proxies `/api/*` to the API container on the internal docker network. Browser sees all calls as same-origin (`https://inventory.denicolafamily.com/api/v1/...`); dev still needs absolute URL because Vite runs on :5173 and API on :8080.
**Pairs with:** D-133 (Hicks CORS for dev), D-135 (Hudson web-container reverse proxy)

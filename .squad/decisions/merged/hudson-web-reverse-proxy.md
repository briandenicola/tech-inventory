### 2026-05-19T01:29Z: D-135 — Web container as nginx + reverse proxy (prod same-origin)
**By:** Hudson (via Copilot)
**What:**
- Web Dockerfile: build stage produces SvelteKit static bundle; runtime stage is nginx:alpine serving the build + proxying /api/* → http://api:8080 on techinv-net
- Added src/TechInventory.Web/nginx.conf (SPA fallback + /api/ reverse proxy)
- docker-compose.yml: web service now ports 3000:80, removed broken PUBLIC_API_URL, dropped deprecated version key
- Added appsettings.Production.json with Cors:AllowedOrigins for defense-in-depth
- Fixed latent bug: previous Dockerfile used `node build` despite adapter-static — would never have started
**Why:** Production deploy is single-origin behind Brian's external TLS-terminating reverse proxy at https://inventory.denicolafamily.com. The web container must serve the SPA AND proxy /api/* internally so the browser only ever sees one origin.
**Pairs with:** D-133 (Hicks CORS for dev), D-134 (Vasquez relative API base URL)
**Follow-up:** Re-run docker-compose validation once Docker is available in this environment; current CLI session cannot execute `docker`/`nginx -t` because neither binary is installed locally.

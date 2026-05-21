# Deployment Runbook

> Production deployment for Tech Inventory on a self-hosted Linux host behind
> Brian's **Nginx Proxy Manager (NPM)** at `https://inventory.denicolafamily.com`.
>
> Architecture refs: D-135 (web container = nginx reverse proxy),
> D-139 (same-origin via NPM), D-140 (F025 v1b break-glass admin).
> See also: `docs/operations.md`, `docs/architecture.md`.

---

## 1. Prerequisites

On the deployment host:

- **Docker Engine ≥ 24** and **Docker Compose v2** (`docker compose version`).
- **Network access to `ghcr.io`** for image pulls.
- **Nginx Proxy Manager** already deployed on the LAN with a valid wildcard
  or per-host Let's Encrypt cert (NPM handles renewals).

On Microsoft Entra ID — **App registration** with:

- **Redirect URI** (SPA platform): `https://inventory.denicolafamily.com/auth/callback`
- **Expose an API** → application ID URI `api://<client-id>`
- **App roles** `Admin`, `Member`, `Viewer` defined and assigned to at least
  one user (the first sign-in needs to land on an Admin).
- Full walkthrough: `docs/operations.md` → *Entra App Registration*.

---

## 2. First-time setup

Two flavors — pick one.

**Flavor A — full repo checkout** (gives you the Taskfile + helper scripts):

```bash
git clone https://github.com/briandenicola/tech-inventory.git
cd tech-inventory
cp .env.example .env
$EDITOR .env
```

**Flavor B — compose-only** (lighter on the deploy host):

```bash
mkdir -p ~/stacks/tech-inventory && cd ~/stacks/tech-inventory
curl -fsSL https://raw.githubusercontent.com/briandenicola/tech-inventory/main/docker-compose.yml -o docker-compose.yml
curl -fsSL https://raw.githubusercontent.com/briandenicola/tech-inventory/main/.env.example -o .env
$EDITOR .env
```

In `.env`, fill in:

| Key | What to put |
| --- | --- |
| `Auth__Entra__TenantId`        | Entra App registration → Overview → *Directory (tenant) ID* |
| `Auth__Entra__ClientId`        | Entra App registration → Overview → *Application (client) ID* |
| `Auth__Entra__Authority`       | `https://login.microsoftonline.com/<tenant-guid>/v2.0` |
| `Auth__Entra__Audiences__0`    | `api://<client-id>` |
| `Auth__Entra__Audiences__1`    | `<client-id>` (bare GUID — Entra sometimes stamps this form into `aud`) |
| `Auth__Local__SigningKey`      | `openssl rand -base64 48` — paste the output |
| `Cors__AllowedOrigins__0`      | `https://inventory.denicolafamily.com` (already the default) |
| `IMAGE_TAG`                    | `latest` for now; you'll pin in §7 |

Leave the seed knobs (`Auth__Local__Seed*`) at their defaults for now — §5
covers turning them on for the very first sign-in.

---

## 3. Pull & start

```bash
# One-time per host: authenticate to GHCR so `docker compose pull` works.
docker login ghcr.io
# Username = your GitHub username
# Password = a PAT with read:packages scope (classic) OR a fine-grained
#            token with "Read access to packages" on this repo.

docker compose pull
docker compose up -d
docker compose logs -f api          # ctrl-c when you see "Now listening on: http://[::]:8080"
```

Sanity checks:

```bash
docker compose ps                                     # both services Up and healthy
docker compose exec web wget -qO- http://api:8080/health/ready   # API readiness from inside the network
curl -fsS http://<host-ip>:3000/health                # web nginx liveness from the LAN
```

If `${VAR:?…}` blocks compose at this step with `error while interpolating
environment variables`, your `.env` is missing a required key — the error
message names it.

---

## 4. NPM upstream config

Configure a **Proxy Host** in NPM for `inventory.denicolafamily.com`:

| Field | Value |
| --- | --- |
| **Domain Names** | `inventory.denicolafamily.com` |
| **Scheme** | `http` |
| **Forward Hostname / IP** | `<deploy-host-LAN-IP>` |
| **Forward Port** | `3000` |
| **Cache Assets** | ☐ off (web container sends its own long-cache headers) |
| **Block Common Exploits** | ☑ on |
| **Websockets Support** | ☑ on (PWA service-worker update channel) |
| **SSL** | ☑ Let's Encrypt cert · ☑ Force SSL · ☑ HTTP/2 · ☑ HSTS Enabled |

**Custom Nginx Configuration** tab — paste this so the API sees the real
client IP in audit logs and CSV uploads aren't truncated:

```nginx
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host $host;

# F009 CSV import accepts up to 10 MiB per appsettings.json — give the
# upstream some headroom for multipart overhead.
client_max_body_size 16m;

# Long-running streamed responses (server-sent events, future websockets)
# deserve more than the default 60s read timeout.
proxy_read_timeout 300s;
proxy_send_timeout 300s;
```

The TLS chain terminates at NPM; the stack speaks plain HTTP on `:3000`.
**Do NOT** publish the API port (`:8080`) on the host — NPM has no business
seeing the API directly, and the compose file deliberately doesn't map it.

---

## 5. First sign-in

The intended path is **Entra first**:

1. Open `https://inventory.denicolafamily.com`.
2. Sign in with the Entra account you assigned the `Admin` role to.
3. You should land on the dashboard. Stop here if it works.

If Entra is misconfigured (or you're locked out), use **break-glass local
admin** (per F025 v1b / D-140):

```bash
# In .env, flip these four knobs ON:
Auth__Local__SeedEnabled=true
Auth__Local__SeedAllowInProd=true
Auth__Local__SeedUsername=rescue
Auth__Local__SeedPassword=<strong-temp-password>

# Reload the API so it reads the new env.
docker compose up -d api
docker compose logs --tail=50 api | grep -i seed
# Expect: "Seeded local Admin 'rescue'…"
```

Then:

1. Open the app, click **Use a local account instead**.
2. Sign in as `rescue` with the temp password.
3. You'll be forced to change the password on first login — pick a strong one.
4. **Decommission the seed** immediately:

   ```bash
   # In .env, set these back / blank:
   Auth__Local__SeedEnabled=false
   Auth__Local__SeedAllowInProd=false
   Auth__Local__SeedUsername=
   Auth__Local__SeedPassword=

   docker compose up -d api
   ```

5. Keep `Auth__Local__SigningKey` set — it's required to validate the local
   JWT the rescue user just got.

Full procedure with rotation runbook: `docs/operations.md` →
*Break-glass local admin* and `docs/deployment.md` (this file) is referenced
from there for the env-cleanup half.

---

## 6. Updating

```bash
docker compose pull             # fetch the new :latest (or pinned tag)
docker compose up -d            # recreate containers whose image changed
docker compose logs -f api      # watch the migration apply + readiness flip green
```

EF Core migrations apply automatically on API startup before the container
goes healthy, so a schema-bumping release is just `pull` + `up -d`.

**Before pulling**, skim the release notes for breaking changes — especially
anything that touches `.env` shape (new required keys, renamed env vars, or
removed knobs).

---

## 7. Pinning versions

`IMAGE_TAG=latest` is the lazy default. For real production stability:

```bash
# In .env:
IMAGE_TAG=v1.0.0
```

Then `docker compose up -d` deploys *exactly that build* and survives
re-creates without surprise upgrades. Update the value when you decide to
move forward; revert to the previous tag to roll back:

```bash
# Roll back:
sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=v0.0.9/' .env
docker compose pull
docker compose up -d
```

> **Schema-incompatible rollbacks**: if the rollback target predates a
> migration, the API will refuse to start (EF detects the newer schema).
> Restore the database from your last backup (§9) *before* rolling the
> image back.

---

## 8. Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `docker compose pull` returns `denied` / `unauthorized` | GHCR login expired or PAT missing `read:packages` | Re-run `docker login ghcr.io` with a fresh PAT |
| `error while interpolating environment variables: required variable XYZ is missing a value` | The `${VAR:?…}` guard tripped — a required env var isn't set | Open `.env`, set the named key, `docker compose up -d` again |
| Sign-in loops, browser keeps bouncing back to Entra | Redirect URI mismatch | Confirm `https://inventory.denicolafamily.com/auth/callback` is a **SPA** redirect URI on the Entra app (not Web, not Public client) |
| API logs `Auth:Entra:Authority is required` and exits | `.env` was rebuilt without the Entra block, or `ASPNETCORE_ENVIRONMENT` isn't Production | Refill from `.env.example`; the compose file always sets `ASPNETCORE_ENVIRONMENT=Production` so you should not see this unless `.env` is incomplete |
| API logs `Auth:Local:SeedEnabled is true in Production without Auth:Local:SeedAllowInProd. Refusing to start.` | You enabled the seed but forgot the prod-safety toggle | Set both `Auth__Local__SeedEnabled=true` AND `Auth__Local__SeedAllowInProd=true` for an intentional break-glass; otherwise unset both |
| `https://inventory.denicolafamily.com` returns 502 | Web container isn't reachable on host:3000 | `docker compose ps` — confirm `web` is `Up (healthy)`; from the NPM host run `curl http://<deploy-host>:3000/health` |
| NPM proxy times out on large CSV imports | Default `client_max_body_size` / `proxy_read_timeout` too small | Apply the *Custom Nginx Configuration* snippet from §4 |
| Second `docker compose up -d` re-seeds the same break-glass user | You left `Auth__Local__SeedEnabled=true` in `.env` | Decommission the seed per §5 — set the four `Auth__Local__Seed*` keys back to defaults |
| `web` container restarts complaining about `/var/run/nginx.pid` permissions | You're on an old web image that ran nginx as root | Pull `:latest` (current image uses `nginxinc/nginx-unprivileged:1.27.5-alpine` and writes pid to `/tmp`) |

Where to look first when something's off:

```bash
docker compose ps
docker compose logs --tail=200 api
docker compose logs --tail=200 web
docker compose exec api wget -qO- http://localhost:8080/health/ready
```

---

## 9. Backup

Brian's external backup workflow handles SQLite snapshots — that lives
outside this repo and outside this stack. Point that process at the
`techinv-data` named volume (or, more precisely, at the live
`/data/techinv.db` inside the API container).

**Ad-hoc snapshot** when you want one right now (uses SQLite's online
`.backup` so it's safe to run against a live DB):

```bash
docker compose exec api sqlite3 /data/techinv.db \
  ".backup /data/techinv-$(date +%Y%m%d-%H%M%S).db"

# Then copy it off the volume to wherever your backup target lives:
docker compose cp api:/data/. ./snapshots/
```

The snapshot file lands inside the same `techinv-data` volume, alongside
`techinv.db`. Move it out-of-host via your normal backup tooling — don't
leave snapshots accumulating in the volume.

> If you ever need continuous WAL replication (Litestream and similar)
> back in the stack, it can be added as an opt-in service later — it was
> intentionally pulled out for this round so NPM + Entra remain the only
> moving security pieces.

---

## 10. Related documents

- `docs/operations.md` — break-glass admin runbook, password rotation,
  signing-key rotation
- `docs/architecture.md` — system design + container topology
- `docs/security-baseline.md` — auth + secret handling rules
- `docs/threat-model.md` — STRIDE for the deployed stack
- `.squad/decisions.md` — D-135, D-139, D-140 set the production shape

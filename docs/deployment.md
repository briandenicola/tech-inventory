# Deployment Runbook

> Production deployment guide for Tech Inventory on a self-hosted Linux host
> behind Brian's **Nginx Proxy Manager** (NPM) at
> `https://inventory.denicolafamily.com`.
>
> Architecture reference: D-135 (web container = nginx reverse proxy),
> D-139 (same-origin via NPM), D-140 (F025 v1b break-glass admin).
> See also: `docs/operations.md`, `docs/architecture.md`.

---

## 1. Prerequisites

On the deployment host:

- **Docker Engine ≥ 24** and **Docker Compose v2.24+**
  (`!reset` YAML override syntax in `docker-compose.prod.yml` requires v2.24+;
  check with `docker compose version`).
- **Nginx Proxy Manager** already deployed and reachable, with:
  - A proxy host for `inventory.denicolafamily.com` forwarding to the host
    machine on port `3000` (see §8 below for the NPM cheat sheet).
  - A valid TLS certificate (Let's Encrypt is fine — NPM handles renewals).
- **Outbound network** to `ghcr.io` for image pulls.
- **Optional:** an S3-compatible bucket for off-host backups (Backblaze B2,
  Wasabi, AWS S3 — anything Litestream supports). Skip this if local-disk
  backups via `BACKUP_PATH` are sufficient.

On Microsoft Entra ID:

- **App registration** with:
  - **Redirect URI** (SPA platform): `https://inventory.denicolafamily.com/auth/callback`
  - **Expose an API** → application ID URI: `api://<client-id>`
  - **App roles** defined: `Admin`, `Member`, `Viewer` (assignable to users)
  - At least one user assigned to the `Admin` role for the initial sign-in.

---

## 2. One-Time Bootstrap

```bash
# Clone the repo on the deployment host
git clone https://github.com/briandenicola/tech-inventory.git
cd tech-inventory

# Copy the env template and fill in real values
cp .env.example .env
$EDITOR .env

# Generate a strong local-admin signing key — paste the output into
# Auth__Local__SigningKey in .env (and never commit it).
openssl rand -base64 48

# Pre-create the backup directory if you'll enable the Litestream sidecar
mkdir -p ./backups
```

**Required `.env` values** (everything else has sensible defaults):

| Key | Source |
| --- | --- |
| `Auth__Entra__Authority`              | `https://login.microsoftonline.com/<tenant-guid>/v2.0` |
| `Auth__Entra__TenantId`               | Entra App Registration → Overview → *Directory (tenant) ID* |
| `Auth__Entra__ClientId`               | Entra App Registration → Overview → *Application (client) ID* |
| `Auth__Entra__Audiences__0`           | `api://<client-id>` (matches Expose an API → Application ID URI) |
| `Auth__Entra__Audiences__1`           | `<client-id>` (bare GUID — Entra issues both forms) |
| `Auth__Local__SigningKey`             | Output of `openssl rand -base64 48` — **SECRET** |
| `TECHINV_IMAGE_TAG`                   | `latest` (or pin to a `v*` tag for reproducibility) |

Decide on backup strategy:

- **Local-disk only:** leave `LITESTREAM_REPLICA_URL` empty. Backups land in
  `${BACKUP_PATH:-./backups}/techinv/` on the host.
- **Local + off-host:** set `LITESTREAM_REPLICA_URL`,
  `LITESTREAM_ACCESS_KEY_ID`, `LITESTREAM_SECRET_ACCESS_KEY` in `.env`.

---

## 3. Pull & Roll

The standard deploy command is:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

To also start the Litestream backup sidecar:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml \
    --profile backup pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml \
    --profile backup up -d
```

Verify health:

```bash
# Containers
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# API responds on the internal network via the web container
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec web \
    wget -qO- http://api:8080/health/ready

# Public endpoint via NPM
curl -fsS https://inventory.denicolafamily.com/health   # web nginx /health
```

---

## 4. Database Migrations

**EF Core migrations apply automatically on API startup.**
`src/TechInventory.Api/Program.cs` runs `dbContext.Database.MigrateAsync()`
before binding the HTTP listener, so a fresh deploy or a deploy that bumps
the schema simply works on the next container restart.

Implications:

- **First-ever boot** creates the schema and seeds the default Household row.
- **Schema-changing release** — pull the new image, `up -d`; the new API
  container migrates the volume's `techinv.db` before it accepts traffic.
- **Manual migration** is normally unnecessary. If you ever need to run it
  out-of-band (e.g., before swapping images during a maintenance window):

  ```bash
  # On a workstation with the .NET SDK + repo checkout at the target commit
  dotnet ef database update \
      --project src/TechInventory.Infrastructure/TechInventory.Infrastructure.csproj \
      --startup-project src/TechInventory.Api/TechInventory.Api.csproj \
      --connection "Data Source=/path/to/techinv.db"
  ```

---

## 5. Rollback

Every container build is tagged with both `latest` (when on `main`) and the
short commit SHA. Tag pushes (`v*`) also produce semver tags. Pin the
desired version via `TECHINV_IMAGE_TAG` and re-`up`:

```bash
# Roll forward — typical case
TECHINV_IMAGE_TAG=v1.3.0 \
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Roll back to a previous tag
TECHINV_IMAGE_TAG=v1.2.2 \
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Roll back to a specific commit (use the short SHA from the build)
TECHINV_IMAGE_TAG=sha-54f8c6e \
    docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

> Persist `TECHINV_IMAGE_TAG` in `.env` after a successful rollback so
> subsequent `up -d` calls don't snap back to `latest`.

**Schema-incompatible rollback:** if the rollback target predates a
migration, the API will refuse to start (EF detects the newer schema). In
that case, restore the database to a snapshot taken before the migration
(see §6) and then roll the image back.

---

## 6. Backup & Restore

The Litestream sidecar (opt-in via `--profile backup`) continuously
streams the SQLite WAL to:

1. A **local file replica** under `${BACKUP_PATH:-./backups}/techinv/`
   (always on when the sidecar runs).
2. An **optional S3-compatible replica** when `LITESTREAM_REPLICA_URL` is set.

Retention is governed by `BACKUP_RETENTION_DAYS` (default 30).

**Verify the latest snapshot:**

```bash
task backup:verify
# Prints the snapshot generations and timestamps.
# Exits non-zero if the sidecar isn't running.
```

**Manual snapshot inspection** (without Taskfile):

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec backup \
    litestream snapshots -config /etc/litestream.yml /data/techinv.db
```

**Restore the latest snapshot** (Linux/macOS host):

```bash
task backup:restore
# Interactive: stops API, restores to /data/techinv.db, restarts API.
# Old DB is preserved at /data/techinv.db.pre-restore in the volume.
```

**Restore manually** (any host):

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml stop api
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile backup \
    run --rm backup \
    litestream restore -config /etc/litestream.yml \
        -o /data/techinv.db.restored \
        /data/techinv.db
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile backup \
    run --rm --entrypoint sh backup \
    -c 'mv /data/techinv.db /data/techinv.db.pre-restore \
         && mv /data/techinv.db.restored /data/techinv.db'
docker compose -f docker-compose.yml -f docker-compose.prod.yml start api
```

**Restore from a specific point in time:**

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile backup \
    run --rm backup \
    litestream restore -config /etc/litestream.yml \
        -timestamp 2026-05-19T03:00:00Z \
        -o /data/techinv.db.restored \
        /data/techinv.db
```

---

## 7. Break-Glass Admin Recovery

If Entra ID is unreachable (tenant misconfigured, secret rotated wrong,
Azure outage), use the local-admin fallback. Full procedure is in
[`docs/operations.md` → Break-Glass Local Admin](./operations.md#break-glass-local-admin-f025-v1b).

**One-shot summary:**

1. Set `Auth__Local__SeedEnabled=true`, `Auth__Local__SeedUsername=rescue`,
   `Auth__Local__SeedPassword=<strong-temp>`, `Auth__Local__SeedAllowInProd=true`
   in `.env`.
2. `docker compose ... up -d api` — watch logs for the CRITICAL "Seeded local
   Admin" line.
3. Sign in via *Use a local account instead*, rotate the password.
4. **Decommission the seed:** unset the four `Auth__Local__Seed*` keys in
   `.env`, restart the API. Keep `Auth__Local__SigningKey` set — it's
   required to validate existing local JWTs.

---

## 8. Nginx Proxy Manager (NPM) Cheat Sheet

Configure a **Proxy Host** for `inventory.denicolafamily.com` with:

| NPM field | Value |
| --- | --- |
| **Domain Names** | `inventory.denicolafamily.com` |
| **Scheme** | `http` |
| **Forward Hostname / IP** | `<host-ip>` (the LAN IP of the box running this stack) |
| **Forward Port** | `3000` |
| **Cache Assets** | ☐ Off (web container handles long-cache headers itself) |
| **Block Common Exploits** | ☑ On |
| **Websockets Support** | ☑ On (MSAL doesn't need it but the PWA service-worker update channel benefits) |
| **SSL** | ☑ Request a new Let's Encrypt cert. ☑ Force SSL. ☑ HTTP/2 Support. ☑ HSTS Enabled. |
| **Custom Nginx Configuration** | See snippet below |

**Custom Nginx Configuration** (paste into NPM's *Custom Nginx Configuration*
tab — preserves real client IP so the API's audit log shows the actual user):

```nginx
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host $host;

# Upload size — F009 CSV import accepts up to 10 MiB per appsettings.json
client_max_body_size 16m;
```

> The TLS chain terminates at NPM. The stack's web container speaks plain
> HTTP on `:3000`; do **not** expose `:8080` (API) on the host — NPM should
> never see the API directly. The `docker-compose.prod.yml` override drops
> the dev-time `8080:8080` mapping for exactly this reason.

---

## 9. Log Inspection

```bash
# Tail everything
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f

# Just the API (Serilog structured JSON + stdout)
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f api

# Just the web (nginx access + error log)
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f web

# Just the backup sidecar
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f backup

# Last 200 lines of the API, no follow
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs --tail=200 api
```

**API Serilog rolling files** are written to the `/app/logs` tmpfs inside
the API container (`techinventory-YYYYMMDD.txt`). They survive container
restarts only as long as the container isn't recreated. For durable log
retention, point `OTEL_EXPORTER_OTLP_ENDPOINT` / `OpenTelemetry__OtlpEndpoint`
at a Seq or OpenTelemetry collector and consume traces there.

**What to grep for after a deploy:**

| Pattern | Meaning |
| --- | --- |
| `Starting Tech Inventory API`         | API came up cleanly |
| `Application failed to start`         | API died — read the stack trace immediately above |
| `Seeded the default single-household` | First-ever boot ran |
| `[F025] Seeded local Admin`           | Break-glass seed fired (rotate + decommission) |
| `must change password`                | Local user hasn't rotated yet |

---

## 10. Common Failure Modes

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `https://inventory.denicolafamily.com` returns 502 | Web container isn't reachable on port 3000 | `docker compose ... ps` — confirm `web` is `Up` and `healthy`; check NPM forward host/port |
| Sign-in loops, redirects keep firing | Entra redirect URI mismatch | Confirm `https://inventory.denicolafamily.com/auth/callback` is registered as a SPA redirect URI in the Entra app |
| API logs `Auth:Entra:Authority is required` | `.env` missing Entra values | Refill from `.env.example`, restart `api` |
| API refuses to start with `SeedAllowInProd` error | `SeedEnabled=true` in prod without `SeedAllowInProd=true` | Either set both for intentional seed, or unset both for normal operation |
| Litestream snapshot age keeps growing | Sidecar not running, or no `--profile backup` on last `up -d` | Re-run `docker compose ... --profile backup up -d backup`; check `task backup:verify` |
| `read_only: true` on api causes startup failure mentioning `/app/logs` | Tmpfs mount lost (e.g., on an older Compose) | Confirm Compose ≥ 2.24 and the tmpfs entries in `docker-compose.prod.yml` are intact |

---

## 11. Related Documents

- `docs/architecture.md` — system design + container topology
- `docs/operations.md` — break-glass admin, password rotation
- `docs/security-baseline.md` — auth + secret handling rules
- `docs/threat-model.md` — STRIDE for the deployed stack
- `.squad/decisions.md` — D-135, D-139, D-140 set the production shape

# Skill: Hermetic Compose Dev Stack

**Domain:** DevOps / Platform  
**Author:** Hudson  
**Created:** 2026-05-18

## Problem

Modern web applications require a full integration environment for testing — API, frontend, database, and potentially other services. Developers need this stack to:
- Start with one command
- Match production behavior closely
- Be reproducible (no "works on my machine")
- Isolate from host environment (no global installs, port conflicts, etc.)

## Solution Pattern

A **hermetic Docker Compose dev stack** where:
1. **Everything runs in containers** — API, frontend, database (or volume-mounted file like SQLite).
2. **One-command lifecycle** — `task up` / `task down` / `task test` (via Taskfile).
3. **Production-like images** — multi-stage Dockerfiles that build production artifacts, not dev servers.
4. **Healthchecks everywhere** — every service declares a healthcheck; dependent services use `depends_on: { condition: service_healthy }`.
5. **Non-root, read-only FS** — containers run as UID 1000, with `tmpfs` for `/tmp` and minimal writable mounts.
6. **Sensible defaults** — resource limits, restart policies, named volumes, custom network.

## Implementation Steps

### 1. Taskfile (Task automation)

Create `Taskfile.yml` at repo root with core commands:
- `up`: `docker compose up -d --build`
- `down`: `docker compose down -v`
- `test`: Run tests against the running stack (document prerequisite: `task up` first)
- `logs`: `docker compose logs -f`
- `ps`: `docker compose ps`

Use `silent: false` and `method: timestamp` for clear output.

### 2. docker-compose.yml (Dev stack definition)

```yaml
version: '3.9'

services:
  api:
    build:
      context: .
      dockerfile: path/to/Api/Dockerfile
    ports:
      - "8080:8080"
    environment:
      # Inject env vars (DB connection, etc.)
    volumes:
      - data-volume:/data  # If using file-based DB like SQLite
      - api-tmp:/tmp
    tmpfs:
      - /tmp
    user: "1000:1000"
    restart: unless-stopped
    networks:
      - app-net
    healthcheck:
      test: ["CMD", "wget", "--spider", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 10s
    deploy:
      resources:
        limits:
          memory: 512M

  web:
    build:
      context: .
      dockerfile: path/to/Web/Dockerfile
    ports:
      - "3000:3000"
    depends_on:
      api:
        condition: service_healthy
    user: "1000:1000"
    restart: unless-stopped
    networks:
      - app-net
    healthcheck:
      test: ["CMD", "wget", "--spider", "http://localhost:3000/"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 10s
    deploy:
      resources:
        limits:
          memory: 256M

networks:
  app-net:
    name: app-net
    driver: bridge

volumes:
  data-volume:
    name: app-data
  api-tmp:
    name: app-api-tmp
```

**Key decisions:**
- Named volumes (not host mounts) for portability.
- Healthcheck dependencies (`service_healthy`) ensure correct startup order.
- Non-root user (`1000:1000`) + read-only FS + tmpfs for `/tmp`.

### 3. Multi-stage Dockerfiles

#### Backend (e.g., .NET)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.sln ./
COPY src/**/*.csproj ./src/
RUN dotnet restore
COPY src/ ./src/
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache wget  # for healthcheck
RUN adduser -u 1000 -D appuser && mkdir -p /data && chown appuser /data /app
COPY --from=build /app/publish .
USER 1000
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK CMD wget --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "App.dll"]
```

#### Frontend (e.g., SvelteKit + adapter-node)

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile || pnpm install
COPY . ./
RUN pnpm run build

FROM node:22-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache wget
RUN adduser -u 1000 -D appuser && chown -R appuser /app
COPY --from=build --chown=appuser /app/build ./build
COPY --from=build --chown=appuser /app/package.json ./
COPY --from=build --chown=appuser /app/node_modules ./node_modules
USER 1000
EXPOSE 3000
ENV NODE_ENV=production PORT=3000
HEALTHCHECK CMD wget --spider http://localhost:3000/ || exit 1
CMD ["node", "build"]
```

**Key patterns:**
- Multi-stage: separate `build` and `runtime` for smaller final images.
- Non-root: `adduser -u 1000` + `USER 1000`.
- Healthcheck: baked into Dockerfile (also defined in compose for orchestration).
- Defensive COPY: use globs like `pnpm-lock.yaml*` to tolerate missing files during parallel scaffolding.

### 4. .dockerignore

Exclude build artifacts, node_modules, .git, docs, etc. to keep build context lean:

```
bin/
obj/
node_modules/
.svelte-kit/
.git/
.env
.env.*
!.env.example
*.log
*.db
coverage/
```

### 5. .env.example

Document expected environment variables:

```
# Database
ConnectionStrings__Default=Data Source=/data/app.db

# API
ASPNETCORE_ENVIRONMENT=Development

# Observability
OTEL_EXPORTER_OTLP_ENDPOINT=

# Auth (placeholders)
AzureAd__TenantId=your-tenant-id
AzureAd__ClientId=your-client-id
```

Never commit `.env` — add it to `.gitignore`.

### 6. Validation

Run `docker compose config` to validate syntax (requires Docker locally). If Docker isn't installed, validation will happen in CI — no local blocker.

### 7. GitHub Actions CI

```yaml
name: CI
on: [push, pull_request]
jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
      - run: corepack enable
      - run: ./scripts/verify.sh
```

Cache NuGet + pnpm stores for faster builds.

## When to Use

- **Multi-service applications** where integration testing requires the full stack.
- **Containerized deployments** (dev/staging/prod all use Docker).
- **Team environments** where "works on my machine" is unacceptable.

## When NOT to Use

- **Simple CLIs or libraries** with no service dependencies.
- **Embedded systems** where Docker isn't available.
- **Local-first tools** (e.g., CLI apps) where containerization adds friction.

## Benefits

1. **Reproducibility**: Same stack on every dev machine and in CI.
2. **Isolation**: No global installs, no port conflicts.
3. **Production parity**: Dev images closely match prod (same base, same build process).
4. **Fast onboarding**: New devs run `task up` and get a working stack in minutes.
5. **Testability**: Integration tests run against a real stack, not mocks.

## Trade-offs

- **Rebuild cycle**: Changes require image rebuild (mitigated by fast builds + caching).
- **Docker dependency**: Developers must have Docker installed.
- **Complexity**: More moving parts than "npm start". Worth it for multi-service apps, overkill for single-service apps.

## References

- [Taskfile.dev](https://taskfile.dev/) — Task automation
- [Docker Compose](https://docs.docker.com/compose/) — orchestration
- [12-Factor App](https://12factor.net/) — config via env vars, process isolation
- Tech Inventory implementation: `Taskfile.yml`, `docker-compose.yml`, `src/*/Dockerfile`

## Variants

- **Database containers**: For PostgreSQL/MySQL, add a `db` service (not needed for SQLite).
- **Hot reload in dev**: Use volume mounts to sync source code into containers (trades hermetic builds for faster iteration).
- **Prod compose file**: Separate `compose.prod.yml` with pinned image digests, no build context, external volumes.

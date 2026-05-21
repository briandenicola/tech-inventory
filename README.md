# Tech Inventory

[![Quality Gate](https://github.com/briandenicola/tech-inventory/actions/workflows/quality-gate.yml/badge.svg?branch=main)](https://github.com/briandenicola/tech-inventory/actions/workflows/quality-gate.yml)
[![CI Pipeline](https://github.com/briandenicola/tech-inventory/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/briandenicola/tech-inventory/actions/workflows/ci.yml)
[![Release Container Images](https://github.com/briandenicola/tech-inventory/actions/workflows/release-images.yml/badge.svg)](https://github.com/briandenicola/tech-inventory/actions/workflows/release-images.yml)
[![Container Image Security Scan](https://github.com/briandenicola/tech-inventory/actions/workflows/security-scan.yml/badge.svg?branch=main)](https://github.com/briandenicola/tech-inventory/actions/workflows/security-scan.yml)

A self-hosted family device and appliance inventory tracker. Single-household, authenticated via Microsoft Entra ID, deployed as Docker Compose on home infrastructure.

## Architecture

- **Backend**: ASP.NET Core 10 Web API — Clean Architecture (Domain → Application → Infrastructure → Api), MediatR + FluentValidation, Serilog, OpenTelemetry
- **Frontend**: SvelteKit PWA with TypeScript (strict), Tailwind CSS, MSAL.js authentication, generated TypeScript API client (no hand-written fetch)
- **Database**: SQLite via EF Core code-first migrations
- **Deployment**: Docker Compose (API + Web + DB). The web container is an nginx reverse proxy that serves the SvelteKit static bundle and forwards `/api/*` to the API on the internal network — browsers see a single origin in production. An external proxy (Nginx Proxy Manager) terminates TLS in front of it. See [`docs/architecture.md`](docs/architecture.md), [`docs/deployment.md`](docs/deployment.md), and ADR **D-139**.
- **Auth**: Microsoft Entra ID (workforce tenant) via OIDC + PKCE as the primary identity provider, with a local-account break-glass fallback (F025 v1b — Argon2id + HS256 JWT). See [`docs/auth-design.md`](docs/auth-design.md) and [`docs/operations.md`](docs/operations.md#break-glass-local-admin-f025-v1b).

### Recent shipped slices

- **F025 v1b** — Break-glass local admin (`POST /api/v1/auth/local/{login,change-password}`, Argon2id hashing, env-var seed hosted service, force-rotation middleware). Operator runbook: [`docs/operations.md`](docs/operations.md#break-glass-local-admin-f025-v1b).
- **F024 v1** — Multi-select bulk actions on the device list.
- **F023 v1** — Group devices by Category / Owner / Purchase year.
- **F022 v1** — Per-user default sort/filter preferences (localStorage).
- **F021 v1** — Admin audit log viewer (`/admin/audit`).
- **F020 v1** — User profile (display name).

## Local Development

### Prerequisites

- [Docker](https://www.docker.com/) & Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 9 if fallback)
- [Node.js 22](https://nodejs.org/) + [pnpm](https://pnpm.io/) (install via `corepack enable`)
- [Task](https://taskfile.dev/) — install via:
  - **Windows**: `choco install go-task`
  - **macOS**: `brew install go-task/tap/go-task`
  - **Linux**: See [taskfile.dev/installation](https://taskfile.dev/installation)

### Quick Start

```bash
# Install the repo-managed pre-commit hook and pinned gitleaks binary
task hooks:install

# Start the full dev stack (API, Web, DB)
task up

# Run all mandatory tests (requires stack to be running)
task test

# Or run just the backend integration suite / one-shot hermetic E2E
task test:integration
task test:e2e

# Tear down the stack
task down

# Run the verification pipeline (format, build, test, security scan)
./scripts/verify.sh   # or .\scripts\verify.ps1 on Windows
```

### Git Hooks

Run `task hooks:install` once per clone. It downloads the pinned `gitleaks` binary into `.tools/gitleaks`, sets `git config core.hooksPath .githooks`, and installs the pre-commit scan that blocks auth-token `localStorage` usage plus secrets in staged files.

### Available Commands

| Command       | Description                                          |
|---------------|------------------------------------------------------|
| `task hooks:install` | Install the pre-commit hook and pinned gitleaks binary |
| `task up`            | Start the full dev stack (API + Web + DB)              |
| `task down`          | Stop and remove all containers, networks, volumes      |
| `task test`          | Run all mandatory tests against the running stack      |
| `task test:integration` | Run backend integration tests with isolated SQLite files |
| `task test:e2e`      | Bring up the compose stack, run Playwright, tear down  |
| `task build`         | Build backend and frontend                             |
| `task lint`          | Lint backend and frontend                              |
| `task verify`        | Run the full verification pipeline                     |
| `task logs`          | Tail logs from all running containers                  |
| `task ps`            | Show running containers                                |
| `task clean`         | Remove build artifacts and volumes                     |

### Ports

- **API**: `http://localhost:8080`
- **Web**: `http://localhost:3000`

### Healthchecks

- **API Liveness**: `http://localhost:8080/health`
- **API Readiness**: `http://localhost:8080/health/ready`

## Container Images

Published to [GitHub Container Registry](https://github.com/briandenicola?tab=packages&repo_name=tech-inventory) via [`release-images.yml`](.github/workflows/release-images.yml). Both images are publicly pullable (no `docker login` required).

**Image Tagging Strategy:**
- **Tag push** (`v*.*.*`): `:latest`, `:vX.Y.Z` (release), `:sha-<short>` (git commit)
- **Main branch push**: `:main` (rolling dev), `:sha-<short>` (git commit)  
  - Note: `:latest` is pinned to semver releases only, not main HEAD
- **Workflow dispatch**: `:latest`, `:<inputs.tag>` (custom tag), `:sha-<short>`

For production, pin to a specific release tag (e.g., `IMAGE_TAG=v1.0.0`) in `.env` — see `docs/deployment.md` §7.

| Image | Registry link | Example pull |
|-------|---------------|--------------|
| API   | [`ghcr.io/briandenicola/tech-inventory-api`](https://github.com/users/briandenicola/packages/container/package/tech-inventory-api) | `docker pull ghcr.io/briandenicola/tech-inventory-api:v1.0.0` |
| Web   | [`ghcr.io/briandenicola/tech-inventory-web`](https://github.com/users/briandenicola/packages/container/package/tech-inventory-web) | `docker pull ghcr.io/briandenicola/tech-inventory-web:v1.0.0` |

Pin a release by setting `IMAGE_TAG=v1.0.0` in `.env`. See [`docs/deployment.md`](docs/deployment.md) for the full home-server deploy runbook.

## Documentation

- [Product Requirements](docs/prd.md)
- [Architecture](docs/architecture.md)
- [Authentication Design](docs/auth-design.md) — Entra ID + local break-glass fallback
- [Security Baseline](docs/security-baseline.md)
- [Threat Model](docs/threat-model.md)
- [Operations Runbook](docs/operations.md) — break-glass admin, day-2 operations
- [Deployment Runbook](docs/deployment.md) — production deploy, NPM, backups, rollback
- [Testing Guide](docs/testing.md)
- [Constitution](.specify/memory/constitution.md)
- [Architecture Decisions](.squad/decisions.md) — append-only ADR ledger (D-001 …)

## Contributing

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for conventions and workflow.

## License

See [LICENSE](LICENSE).

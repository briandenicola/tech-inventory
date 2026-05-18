# Tech Inventory

A self-hosted family device and appliance inventory tracker. Single-household, authenticated via Microsoft Entra ID, deployed as Docker Compose on home infrastructure.

## Architecture

- **Backend**: ASP.NET Core 10 Web API — Clean Architecture (Domain → Application → Infrastructure → Api)
- **Frontend**: SvelteKit PWA with TypeScript, Tailwind CSS, MSAL.js authentication
- **Database**: SQLite via EF Core code-first migrations
- **Deployment**: Docker Compose (API + Web + DB); external reverse proxy terminates TLS

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
# Start the full dev stack (API, Web, DB)
task up

# Run all tests (requires stack to be running)
task test

# Tear down the stack
task down

# Run the verification pipeline (format, build, test, security scan)
./scripts/verify.sh   # or .\scripts\verify.ps1 on Windows
```

### Available Commands

| Command       | Description                                          |
|---------------|------------------------------------------------------|
| `task up`     | Start the full dev stack (API + Web + DB)            |
| `task down`   | Stop and remove all containers, networks, volumes    |
| `task test`   | Run all tests against the running stack              |
| `task build`  | Build backend and frontend                           |
| `task lint`   | Lint backend and frontend                            |
| `task verify` | Run the full verification pipeline                   |
| `task logs`   | Tail logs from all running containers                |
| `task ps`     | Show running containers                              |
| `task clean`  | Remove build artifacts and volumes                   |

### Ports

- **API**: `http://localhost:8080`
- **Web**: `http://localhost:3000`

### Healthchecks

- **API Liveness**: `http://localhost:8080/health`
- **API Readiness**: `http://localhost:8080/health/ready`

## Documentation

- [Product Requirements](docs/prd.md)
- [Architecture](docs/architecture.md)
- [Constitution](.specify/memory/constitution.md)

## Contributing

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for conventions and workflow.

## License

See [LICENSE](LICENSE).

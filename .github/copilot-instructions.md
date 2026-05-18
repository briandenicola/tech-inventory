# Copilot Instructions

## Project Overview

**Tech Inventory** — a self-hosted family device/appliance inventory tracker.
Single-household, authenticated via Microsoft Entra ID, deployed as Docker Compose on home infrastructure.

## Architecture

- **Backend**: ASP.NET Core 10 Web API — Clean Architecture layers: `Domain` → `Application` → `Infrastructure` → `Api`
- **Frontend**: SvelteKit PWA with TypeScript strict mode, Tailwind CSS, MSAL.js auth
- **Database**: SQLite via EF Core code-first migrations
- **Deployment**: Docker Compose (API + Web + DB); external reverse proxy terminates TLS
- **API client**: TypeScript client generated from OpenAPI spec — no hand-written fetch calls

Key architectural rules (constitution §2):
- API-first: every capability via versioned REST (`/api/v1/...`) before any UI
- Dependencies point inward only; Domain layer has zero framework dependencies
- MediatR for non-trivial commands/queries; thin controllers with no business logic
- Repository pattern; no raw SQL; all queries parameterized via EF Core
- Soft delete via status flag; AuditEvent table is append-only

## Build, Test, and Lint Commands

### Backend (.NET)

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release                          # all tests
dotnet test --filter "FullyQualifiedName~MyTest" # single test
dotnet format --verify-no-changes               # lint check
dotnet format                                   # auto-fix formatting
dotnet list package --vulnerable                # security audit
```

### Frontend (SvelteKit)

```bash
cd src/TechInventory.Web
pnpm install                                    # install deps (pnpm, not npm)
npx playwright install --with-deps              # first-time E2E setup
pnpm run check                                  # tsc --noEmit + svelte-check
pnpm run lint                                   # ESLint
pnpm run test                                   # Vitest unit tests
pnpm run test -- --run src/lib/MyFile.test.ts   # single test file
npx playwright test                             # E2E (requires running stack)
npx playwright test tests/e2e/mytest.spec.ts    # single E2E test
```

### Full Verify Pipeline

```bash
./scripts/verify.sh   # format check → build → test → vulnerability scan
```

### Local Stack (Docker Compose)
- All local automation will be handled by [Task](https://taskfile.dev/)
- [Taskdev Schema](https://taskfile.dev/docs/reference/schema)

```bash
task up      # start full stack (API, web, db)
task test    # run all mandatory tests against running stack
task down    # tear down
```

## Key Conventions

### .NET / C#
- File-scoped namespaces, primary constructors, `record` for DTOs/value objects
- Nullable reference types enabled; treat warnings as errors in Release
- Async all the way — no `.Result` or `.Wait()`; `CancellationToken` on every async method
- `Result<T>` for expected failures; exceptions only for truly exceptional conditions
- API errors as ProblemDetails (RFC 7807)
- FluentValidation for all command/query inputs (pipeline behavior)
- Serilog structured logging; never log secrets/PII
- Conventional Commits; one logical change per commit; reference spec section

### SvelteKit / TypeScript
- pnpm as package manager; Vite build tool
- No `any` — use `unknown` + narrowing
- Zod schemas for client validation (mirror server-side FluentValidation)
- Server state via query library (TanStack Query style); UI state in Svelte stores
- Components < 200 lines, single-purpose, support loading/empty/error/success states
- All strings in i18n catalogs (`src/lib/i18n/en.json`); no hard-coded strings
- Design tokens in `src/lib/tokens.css`; no magic Tailwind values like `mt-[13px]`
- Playwright is the only E2E framework; tests in `tests/e2e/` with Page Object Model

### Testing
- 85% minimum line coverage on Domain + Application layers
- xUnit + FluentAssertions + NSubstitute for backend unit tests
- Vitest + Testing Library for frontend unit/component tests
- Playwright for E2E (Chromium + WebKit + Firefox in CI)
- axe-core accessibility checks in both unit and E2E — zero violations to merge
- Tests own their data; no shared fixtures across files; no mocked DB in integration tests

### Security
- Default deny on every endpoint; roles: `Admin`, `Member`, `Viewer`
- OIDC + PKCE via Entra ID; tokens in memory/sessionStorage, never localStorage
- No third-party analytics or scripts without ADR
- `gitleaks` pre-commit hook; Trivy container scan; SBOM per release

## Document Hierarchy

When in doubt, defer in this order (constitution §0):
1. `.specify/memory/constitution.md` (highest authority)
2. `docs/prd.md` (product intent)
3. `specs/00X-*/spec.md` (current feature spec)
4. `specs/00X-*/plan.md` → `tasks.md` (active work)
5. Agent's judgment (lowest — must be voiced)

## Session Protocol

### Always
- Read `.specify/memory/constitution.md` before any task
- Read `.copilot-state.md` at session start
- Quote spec sections you're implementing in commit messages
- Run `./scripts/verify.sh` before declaring done
- Update `tasks.md`, `SESSION-NOTES.md`, `.copilot-state.md` at session end
- When a spec mentions an `R<N>` reference (see `docs/references.md`),
  consult that reference at its pinned SHA before designing or coding
- Cite reference borrowings as `R<N>:<path>@<sha>` in commit messages

### Never
- Use `any` / `dynamic` / `@ts-ignore` without inline justification
- Commit secrets or `.env` files
- Skip tests or disable lint rules silently
- Paraphrase files from memory — re-read or quote
- Continue if context feels stale — request `/compact` or restart
- Invent APIs, package names, or file paths
- Copy code from references without explicit ADR approval

### Session Handoff
START: Read `.copilot-state.md` → `SESSION-NOTES.md` (last entry) → current `tasks.md` → state next action and wait for confirmation.

END: Update `tasks.md` → append to `SESSION-NOTES.md` → update `.copilot-state.md` → commit with `chore: session handoff`.

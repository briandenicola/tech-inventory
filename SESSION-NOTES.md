# Session Notes

Append-only log. Newest entries at the top.

---

## 2026-05-18 — Hicks Phase 1 domain reference entities T06-T10
- Added `Category`, `Owner`, `Location`, `Network`, `Tag`, and `DeviceTag` under `src/TechInventory.Domain/Entities/` with trimmed-name guards, archive/reactivate methods, and normalized-name helpers for later repository uniqueness checks
- Category now keeps `ParentId` plus validated `Depth` (1-3) so the max-depth invariant is enforced in Domain; `Owner` carries `OwnerRole` and optional `EntraObjectId`; `DeviceTag` uses `IsActive` instead of hard deletes
- Replaced Apone's placeholder skips with executable domain tests for T06-T10; unit suite is now 32 passing tests with zero skips
- Verified `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test -c Release`, targeted unit/integration runs, and `./scripts/verify.ps1`
- Next: Hicks can take T11-T17 (AuditEvent, ImportBatch, AppDbContext/configs, repositories, audit stamping)

## 2026-05-18 — Hudson pre-commit security gate
- Added a repo-managed pre-commit hook at `.githooks/pre-commit` backed by `scripts/check-security.mjs` and a pinned `.gitleaks.toml` config
- Added `task hooks:install` plus cross-platform `scripts/install-gitleaks.ps1` / `scripts/install-gitleaks.sh` so fresh clones can wire hooks in one command
- Updated `.github/workflows/ci.yml` to install pinned gitleaks and mirror the hook against PR/push diffs
- Verified `task hooks:install`, `node .\scripts\check-security.mjs --diff-range HEAD`, and an isolated test repo commit rejection for an auth-token localStorage write attempt
- `./scripts/verify.ps1` still fails on the pre-existing frontend `vite.config.ts` type mismatch and unused `@ts-expect-error`
- Next: fix the frontend Vite/Vitest type conflict so the full verify pipeline is green again

## 2026-05-18 — Apone QA follow-through
- Added spec-driven Domain tests for `Currency`, `Household`, and `Device` under `tests/TechInventory.UnitTests/Domain/`
- Enabled the `/health` integration smoke test with `WebApplicationFactory<Program>` and exposed `Program` for test hosting
- Added Playwright token-storage enforcement under `tests/e2e/security/` plus reusable storage-inspection helper and skill note
- Validated with `dotnet test tests/TechInventory.UnitTests -c Release`, `dotnet test tests/TechInventory.IntegrationTests -c Release`, and `node .\\node_modules\\@playwright\\test\\cli.js test security/token-storage.spec.ts --reporter=line`
- Attempted `./scripts/verify.sh`, but this Windows session has no `bash`; used targeted validation instead
- Next: expand T44/T45 as Hicks lands Application handlers and more API surface

## 2026-05-18 — Hicks Phase 1 domain core T01-T05
- Added Domain primitives (`Entity`, `AggregateRoot`, `ValueObject`, `Guard`) plus shared enums under `src/TechInventory.Domain/`
- Added `Currency` value object with ISO 4217 allowlist validation, `Household` with `DefaultCurrency`, `Device` with household-default currency creation flow, and `Brand`
- Verified `dotnet build -c Release` and `dotnet test -c Release --no-build` passed after the Domain changes; fixed a pre-existing whitespace issue in `src/TechInventory.Api/Program.cs` so `dotnet format --verify-no-changes` can pass again

## 2026-05-18 — Vasquez auth token storage lint gate
- Added a custom flat-config ESLint rule in `src/TechInventory.Web/eslint.config.js` to block token-like `localStorage.setItem/getItem/removeItem` keys and to forbid any `localStorage` use inside `src/lib/auth/` and `src/lib/api/`
- Locked MSAL cache policy to `BrowserCacheLocation.SessionStorage` in `src/lib/auth/msal.ts`
- Verified the rule fired with temporary lint fixtures, then removed them and reran `pnpm run lint` successfully
- `pnpm run check` still fails on the pre-existing unused `@ts-expect-error` in `src/TechInventory.Web/vite.config.ts`

## YYYY-MM-DD — Bootstrap
- Created project structure
- Populated constitution, PRD, backlog README
- Next: run `/constitution` to validate

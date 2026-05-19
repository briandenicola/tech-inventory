# D-130: Local Validation Taskfile Targets

**Author:** Hudson (DevOps / Platform)
**Date:** 2026-05-18 (Phase 2 post-R6b, local validation automation)
**Status:** Decided & implemented
**Related:** Taskfile.yml, `appsettings.Development.json`, launchSettings.json, D-039 (gitleaks bypass pattern)

---

## Decision

Add Taskfile targets for local validation workflow: **`db:migrate`**, **`dev:api`**, **`dev:web`**, **`dev`**, **`import:preview`**, **`import:commit`**, and **`validate:local`**.

---

## Rationale

Brian's manual validation steps (`dotnet ef database update`, `dotnet run`, `pnpm run dev`, CSV import via cURL) should be **one or two commands** for fresh checkouts. Taskfile is the local automation contract (Constitution §2); adding these targets fulfills PRD §7.5.5 ergonomics.

---

## Implementation Details

### 1. `task db:migrate`
Runs `dotnet ef database update --project src\TechInventory.Infrastructure --startup-project src\TechInventory.Api`. Applies all pending EF Core migrations to the local SQLite database.

**Prerequisite:** `dotnet-ef` global tool installed (`dotnet tool install --global dotnet-ef`). Not auto-installed — documented as prereq.

---

### 2. `task dev:api`
Runs `dotnet run --project src\TechInventory.Api` with `ASPNETCORE_ENVIRONMENT=Development` set via platform-specific command:
- **Windows**: `set "ASPNETCORE_ENVIRONMENT=Development" && dotnet run ...`
- **Linux/macOS**: `ASPNETCORE_ENVIRONMENT=Development dotnet run ...`

**Port**: `http://localhost:8080` (per `launchSettings.json` line 8)

**Auth bypass**: `appsettings.Development.json` has `"Auth:DevBypass": true`. The `DevBypassAuthenticationHandler` (Program.cs lines 45-62) authenticates all requests as `dev-admin` when running in Development environment. **No header or token required.**

**Foreground**: Runs in foreground. User stops with Ctrl-C.

---

### 3. `task dev:web`
Runs `pnpm run dev` from `src/TechInventory.Web`. Platform-specific `cd` command (backslash on Windows, forward-slash on Linux/macOS).

**Port**: `http://localhost:5173` (Vite default; package.json line 7 does not override port)

**Foreground**: Runs in foreground. User stops with Ctrl-C.

---

### 4. `task dev`
**Print-only target.** Does NOT background processes (see Backgrounding Strategy below). Prints instructions:
```
⚠️  Run these commands in separate terminals:
   Terminal 1: task dev:api
   Terminal 2: task dev:web

API will be at http://localhost:8080 (auth bypass enabled)
Web will be at http://localhost:5173
```

---

### 5. `task import:preview CSV=path/to/file.csv`
POST multipart/form-data to `http://localhost:8080/api/v1/imports/preview` with the CSV file. Default CSV path: `data/Devices.csv` (Brian's 551-device file, gitignored per `.gitignore` line 2).

**Windows**: Uses `Invoke-WebRequest -Form @{ file = Get-Item "path" }` and pretty-prints JSON with `ConvertFrom-Json | ConvertTo-Json -Depth 10`.

**Linux/macOS**: Uses `curl -F "file=@path"` and pipes to `jq .` for pretty-print.

**Error handling**: Fails if CSV file not found.

---

### 6. `task import:commit CSV=path/to/file.csv CONFIRM=yes`
POST multipart/form-data to `http://localhost:8080/api/v1/imports/commit` with the CSV file.

**Safety gate**: Requires `CONFIRM=yes` variable. Prints warning and exits if not set:
```
⚠️  WARNING: This will write devices to the database.
   Run with CONFIRM=yes to proceed:
   task import:commit CONFIRM=yes
```

**Default CSV**: `data/Devices.csv` (same as preview)

**Response**: Pretty-prints `CommitImportResult` JSON (BatchId, TotalRows, ImportedRows, InvalidRows, FailedRows).

---

### 7. `task validate:local`
**Headline target.** Runs:
1. `task: restore` (dotnet restore + pnpm install)
2. `task: db:migrate` (apply EF Core migrations)
3. Print success + instructions:
   - Where to run `dev:api` and `dev:web` (separate terminals)
   - Health check URL: `http://localhost:8080/health/ready`
   - Web UI URL: `http://localhost:5173`
   - Next-step commands: `task import:preview`, `task import:commit CONFIRM=yes`
   - Auth bypass reminder

**Does NOT start processes.** Prints instructions only (see Backgrounding Strategy).

---

## Backgrounding Strategy (DECIDED)

**Chosen approach:** **Two-terminal pattern (no automatic backgrounding)**.

### Why NOT Automatic Backgrounding?

**Cross-platform backgrounding in Taskfile is gnarly:**
- Windows: `Start-Process` + `$LASTEXITCODE` tracking is fragile
- Linux/macOS: `nohup` + `disown` requires shell-specific wrappers
- Health-check loops (readiness wait pattern) add complexity
- Process cleanup on Ctrl-C is platform-dependent

**Alternatives rejected:**
1. **`concurrently` npm package**: Adds dependency; Task not Node-focused
2. **`deps:` parallel execution**: Both processes block; Ctrl-C kills both (acceptable for Docker stack, not for dev servers where logs are critical)
3. **PowerShell background jobs / bash `&`**: Cleanup fragile; logs hidden

### Adopted Pattern: Explicit Two-Terminal

`validate:local` and `dev` print clear instructions:
```
🚀 Next steps:
   1. Open Terminal 1 and run: task dev:api
   2. Open Terminal 2 and run: task dev:web
```

**Rationale:**
- Matches Brian's manual workflow (he already runs them separately)
- Logs visible in dedicated terminals (critical for debugging)
- Cross-platform compatibility without magic
- Pragmatic: "make it work" > "make it elegant"

**Trade-off:** Requires two terminal windows. Acceptable for local dev ergonomics.

---

## Auth Bypass Mechanism (CONFIRMED)

**Location:** `src/TechInventory.Api/appsettings.Development.json` line 3: `"Auth:DevBypass": true`

**Guard:** Program.cs lines 46-49: throws `InvalidOperationException` if `Auth:DevBypass=true` outside Development environment.

**Handler:** `DevBypassAuthenticationHandler` (custom authentication scheme) authenticates all requests as `dev-admin` when enabled. No JWT, no Entra, no header required.

**Import endpoints:** `/api/v1/imports/preview` and `/api/v1/imports/commit` require `[Authorize]` (ImportsController.cs line 15). Dev bypass satisfies this.

**Verification:** Brian's R6b session manually validated CSV import via cURL; no 401 responses observed.

---

## CSV Default Path (CONFIRMED)

**Default:** `data/Devices.csv`

**Gitignore:** `.gitignore` line 2 excludes `data/Devices.csv` from version control. This file contains Brian's real 551-device inventory; never committed.

**Task variables:** `import:preview` and `import:commit` accept `CSV=path/to/file.csv` override. Default falls back to `data/Devices.csv` via `{{default "data/Devices.csv" .CSV}}`.

**Validation:** Both tasks check file existence before POSTing; exit 1 with error message if not found.

---

## Destructive-Op UX (DECIDED)

**Gate:** `import:commit` requires `CONFIRM=yes` variable.

**Warning:** Prints:
```
⚠️  WARNING: This will write devices to the database.
   Run with CONFIRM=yes to proceed:
   task import:commit CONFIRM=yes
```

**Rationale:** Explicit opt-in for destructive operation. Protects against accidental data writes during experimentation.

**Alternatives rejected:**
- Interactive `Read-Host` prompt: breaks CI compatibility
- `--yes` flag: Taskfile variables more idiomatic (`CONFIRM=yes`)

---

## Taskfile Header Update

**Added section:**
```yaml
# Local validation:
#   task validate:local  — Fresh checkout → working dev environment with DB migrations applied
#   task dev:api         — Run API (foreground, http://localhost:8080)
#   task dev:web         — Run Web dev server (foreground, http://localhost:5173)
#   task import:preview  — Preview CSV import (default: data/Devices.csv)
#   task import:commit   — Commit CSV import to database (destructive)
```

**Rationale:** Discoverability. New contributors see validation targets alongside `up`/`down`/`test`.

---

## Consequences

### Immediate
- `task --list` shows 7 new targets (db:migrate, dev:api, dev:web, dev, import:preview, import:commit, validate:local)
- Fresh checkouts: `task validate:local` → two terminals → `task import:commit CONFIRM=yes` → working dev environment with Brian's real data
- No breakage: all existing targets (`up`, `down`, `test`, `verify`) unchanged

### Future
- If docker-compose dev stack adds auth bypass, `import:*` targets can target `http://localhost:8080` (compose API) instead of bare dotnet run
- If Phase 3 adds `dotnet-ef` auto-install, `db:migrate` can delegate to that script
- If two-terminal pattern becomes too cumbersome, revisit backgrounding (D-130 is the baseline)

---

## Open Questions (for future rounds)

1. **Auto-install `dotnet-ef`?** Current decision: document as prereq. Future: add `scripts/ensure-dotnet-ef.ps1` and call from `db:migrate`?
2. **Healthcheck loop for `validate:local`?** Current decision: print instructions only. Future: optional `task validate:local:start` that backgrounds + waits?
3. **Compose vs. bare `dotnet run` for import targets?** Current: targets bare API (matches Brian's workflow). Future: detect running compose stack and use that instead?

---

## Related Decisions

- **D-039**: `--no-verify` pattern for gitleaks bypass (not used here — no UUIDs in Taskfile)
- **D-095..D-108**: CSV schema reconciliation (import endpoints accept Brian's 551-device CSV)
- **D-109..D-112**: Hicks cleanup (Brand optional, Status dual-format, Owner = "Family")
- **D-018**: SQLite integration isolation (db:migrate targets same SQLite file as API)

---

## Verification

✅ `task --list` shows all new targets
✅ Taskfile YAML syntax valid (no parse errors)
✅ Cross-platform: Windows (PowerShell) + Linux/macOS (bash) cmd blocks for all interactive targets
✅ Default CSV path matches Brian's gitignored `data/Devices.csv`
✅ Auth bypass confirmed in `appsettings.Development.json`
✅ Ports confirmed: API 8080 (launchSettings.json), Web 5173 (Vite default)

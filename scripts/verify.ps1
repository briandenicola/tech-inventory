# Tech Inventory — Verification Pipeline (Windows)
# PowerShell wrapper for verify.sh steps

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Tech Inventory — Verification Pipeline" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Backend verification
Write-Host "🔍 [1/7] Checking .NET code formatting..." -ForegroundColor Yellow
dotnet format --verify-no-changes
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "🔨 [2/7] Building .NET solution..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "🧪 [3/7] Running .NET tests..." -ForegroundColor Yellow
dotnet test -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "🔒 [4/7] Scanning for vulnerable packages..." -ForegroundColor Yellow
dotnet list package --vulnerable --include-transitive
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Frontend verification
Write-Host ""
Write-Host "📦 [5/7] Installing frontend dependencies..." -ForegroundColor Yellow
Push-Location src\TechInventory.Web
pnpm install --frozen-lockfile
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

Write-Host ""
Write-Host "🔍 [6/7] Checking frontend (tsc + svelte-check)..." -ForegroundColor Yellow
pnpm run check
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

Write-Host ""
Write-Host "✨ [7/7] Linting frontend..." -ForegroundColor Yellow
pnpm run lint
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

Pop-Location

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "✅ Verification complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

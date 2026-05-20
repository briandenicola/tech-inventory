$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$originalLocation = Get-Location
$e2eLocationPushed = $false
$originalBaseUrl = $env:BASE_URL

# Combined compose invocation: prod compose + e2e override + stub env file.
# The env file satisfies docker-compose.yml's `${VAR:?}` required-var guards
# (which abort interpolation before any service starts); the override file
# swaps GHCR images for local builds, forces Development env (so the F025
# LocalAdminSeedHostedService is willing to run without SeedAllowInProd), and
# seeds the local admin Playwright fixtures sign in as.
# Hoisted to script scope so the `finally` cleanup block can reuse it.
$composeArgs = @(
    "compose",
    "--env-file", (Join-Path $repoRoot ".env.e2e"),
    "-f", (Join-Path $repoRoot "docker-compose.yml"),
    "-f", (Join-Path $repoRoot "docker-compose.e2e.yml")
)

function Wait-ForReady {
    param(
        [string]$ReadyUrl,
        [int]$MaxAttempts = 60,
        [int]$DelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $ReadyUrl -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            if ($attempt -eq $MaxAttempts) {
                throw "API readiness check failed at $ReadyUrl after $MaxAttempts attempts."
            }
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    throw "API readiness check failed at $ReadyUrl after $MaxAttempts attempts."
}

try {
    Set-Location $repoRoot

    docker @composeArgs up -d --build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Wait-ForReady -ReadyUrl "http://localhost:8080/health/ready"

    Push-Location (Join-Path $repoRoot "tests\e2e")
    $e2eLocationPushed = $true

    npm ci
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $env:BASE_URL = "http://localhost:3000"
    npx playwright test
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    if ($e2eLocationPushed) {
        Pop-Location
    }

    if ($null -eq $originalBaseUrl) {
        Remove-Item Env:BASE_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:BASE_URL = $originalBaseUrl
    }

    Set-Location $repoRoot
    docker @composeArgs down -v
    Set-Location $originalLocation
}

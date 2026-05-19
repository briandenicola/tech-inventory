$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$originalLocation = Get-Location
$e2eLocationPushed = $false
$originalBaseUrl = $env:BASE_URL
$originalDevBypass = $env:VITE_AUTH_DEV_BYPASS

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

    # E2E tests rely on the dev-bypass shim (no MSAL bounce off Microsoft).
    # Compose forwards this as a build ARG to the web Dockerfile.
    $env:VITE_AUTH_DEV_BYPASS = "true"

    docker compose up -d --build
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

    if ($null -eq $originalDevBypass) {
        Remove-Item Env:VITE_AUTH_DEV_BYPASS -ErrorAction SilentlyContinue
    }
    else {
        $env:VITE_AUTH_DEV_BYPASS = $originalDevBypass
    }

    Set-Location $repoRoot
    docker compose down -v
    Set-Location $originalLocation
}

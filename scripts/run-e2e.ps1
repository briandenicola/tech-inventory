$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$originalLocation = Get-Location
$e2eLocationPushed = $false
$originalBaseUrl = $env:BASE_URL

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

    Set-Location $repoRoot
    docker compose down -v
    Set-Location $originalLocation
}

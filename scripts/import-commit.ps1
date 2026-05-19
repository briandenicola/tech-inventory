# Commit CSV import to database (DESTRUCTIVE)

param(
    [Parameter(Mandatory=$true)]
    [string]$CsvPath,
    
    [Parameter(Mandatory=$true)]
    [string]$Confirm
)

$ErrorActionPreference = "Stop"

if ($Confirm -ne "yes") {
    Write-Host "⚠️  WARNING: This will write devices to the database." -ForegroundColor Yellow
    Write-Host "   Run with CONFIRM=yes to proceed:" -ForegroundColor Yellow
    Write-Host "   task import:commit CONFIRM=yes" -ForegroundColor Cyan
    exit 1
}

if (-Not (Test-Path $CsvPath)) {
    throw "CSV file not found: $CsvPath"
}

Write-Host "💾 Committing import from: $CsvPath" -ForegroundColor Green
Write-Host ""

$response = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/imports/commit" `
    -Method POST `
    -Form @{ file = Get-Item $CsvPath } `
    -UseBasicParsing

$json = $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
Write-Host $json

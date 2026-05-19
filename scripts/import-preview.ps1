# Preview CSV import via multipart/form-data POST to /api/v1/imports/preview

param(
    [Parameter(Mandatory=$true)]
    [string]$CsvPath
)

$ErrorActionPreference = "Stop"

if (-Not (Test-Path $CsvPath)) {
    throw "CSV file not found: $CsvPath"
}

Write-Host "📄 Previewing import from: $CsvPath" -ForegroundColor Cyan
Write-Host ""

$response = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/imports/preview" `
    -Method POST `
    -Form @{ file = Get-Item $CsvPath } `
    -UseBasicParsing

$json = $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
Write-Host $json

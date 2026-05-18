param(
    [string]$Version = "8.30.1"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$repoRoot = Split-Path -Parent $PSScriptRoot
$installDir = Join-Path $repoRoot ".tools\gitleaks"
$extractDir = Join-Path $installDir "_extract"

New-Item -ItemType Directory -Force -Path $installDir | Out-Null

$arch = if ($env:PROCESSOR_ARCHITECTURE -match "ARM64") { "arm64" } else { "x64" }
$assetName = "gitleaks_${Version}_windows_${arch}.zip"
$downloadUrl = "https://github.com/gitleaks/gitleaks/releases/download/v$Version/$assetName"
$archivePath = Join-Path $installDir $assetName
$binaryPath = Join-Path $installDir "gitleaks.exe"

if (Test-Path $extractDir) {
    Remove-Item $extractDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath
& tar -xf $archivePath -C $extractDir
if ($LASTEXITCODE -ne 0) {
    throw "Failed to extract $assetName"
}

$downloadedBinary = Get-ChildItem -Path $extractDir -Filter "gitleaks.exe" -Recurse | Select-Object -First 1
if ($null -eq $downloadedBinary) {
    throw "Unable to locate gitleaks.exe in $assetName"
}

Copy-Item $downloadedBinary.FullName $binaryPath -Force
Remove-Item $archivePath -Force
Remove-Item $extractDir -Recurse -Force

& $binaryPath version

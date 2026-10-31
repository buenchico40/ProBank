# Prepara el entorno local en Windows.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet restore src/ProBank.sln
dotnet restore test/ProBank.Tests.sln
Push-Location test/e2e; npm install; npx playwright install chromium; Pop-Location
Write-Host "Listo. Abre Run and Debug > 'Api + Web'."

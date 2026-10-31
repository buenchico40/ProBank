# Licencia de DevExtreme (25.2): DevExpress_License.txt trae una clave LCX (la de los productos .NET); en el navegador
# DevExtreme necesita la clave LCP que se obtiene de ella con la herramienta oficial devextreme-license (sin conexión).
# Este script la convierte, la guarda en los user-secrets de ProBank.Web y, con -Ambiente, la despliega en Azure.
# La clave nunca se escribe en el repositorio.
# Uso: pwsh scripts/licencia-devextreme.ps1 [-Archivo <ruta a DevExpress_License.txt>] [-Ambiente dev]
#      Sin -Archivo usa %AppData%\DevExpress\DevExpress_License.txt o la variable DevExpress_License.
param([string]$Archivo, [ValidateSet("dev", "qa", "prod")] [string]$Ambiente)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
$version = "25.2.10" # la misma de _Layout.cshtml: la clave LCP se valida contra la versión

if ($Archivo) { $env:DevExpress_LicensePath = (Resolve-Path $Archivo).Path }
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) "devextreme-license-$PID.js"
try {
    npx --yes -p "devextreme@$version" devextreme-license --non-modular --no-gitignore --force --out $tmp
    $lcp = [regex]::Match((Get-Content $tmp -Raw), 'licenseKey:\s*"([^"]+)"').Groups[1].Value
}
finally { Remove-Item $tmp -ErrorAction SilentlyContinue }
if (-not $lcp -or $lcp -eq "TRIAL") { throw "No se obtuvo una licencia válida para DevExtreme $version (revise el archivo o la suscripción)." }

dotnet user-secrets --project src/ProBank.Web set "DevExtreme:LicenseKey" $lcp | Out-Null
Write-Host "Licencia de DevExtreme guardada en los user-secrets de ProBank.Web (reinicie la Web local)."
if ($Ambiente) { pwsh scripts/azure/desplegar.ps1 -Ambiente $Ambiente -LicenciaDevExtreme $lcp }

# Prepara ProBank para ejecutarse en la máquina local, sin Azure:
# SQL Server LocalDB + base publicada desde el .dacpac (incluye roles) + usuarios de prueba + user-secrets.
# Uso: pwsh scripts/local/setup-local.ps1 [-Clave "<clave de los usuarios de prueba>"] [-LicenciaDevExtreme "<clave de licencia>"]
#      (luego Run and Debug > "Api + Web")
param([string]$Clave = "ProBank-Local-2026!", [string]$LicenciaDevExtreme = $env:PROBANK_DEVEXTREME_LICENCIA)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "../..")

$instancia = "MSSQLLocalDB"
$base = "ProBank"
$cs = "Server=(localdb)\$instancia;Database=$base;Integrated Security=True;TrustServerCertificate=True"

if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) {
    dotnet tool install -g microsoft.sqlpackage
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

sqllocaldb start $instancia | Out-Null
dotnet build src/ProBank.Database/ProBank.Database.sqlproj -nologo -v q
# El proyecto apunta a Azure SQL; LocalDB es SQL Server, por eso se permite la plataforma distinta.
sqlpackage /Action:Publish /SourceFile:src/ProBank.Database/bin/Debug/ProBank.Database.dacpac `
    /TargetConnectionString:$cs /p:AllowIncompatiblePlatform=True /Quiet:True

Write-Host "Creando usuarios de prueba..."
$env:PROBANK_SQL = $cs
$env:PROBANK_CLAVE = $Clave
try {
    $usuarios = @(
        @("dev.local", "Desarrollador local", "Administrador,Gerencia de TI,Consulta"),
        @("tecnico.local", "Técnico de prueba", "Consulta"),
        @("aprobador.local", "Aprobador de prueba", "Gerencia de TI")
    )
    foreach ($u in $usuarios) {
        dotnet run --project tools/ProBank.Setup -- usuario --usuario $u[0] --nombre $u[1] --roles $u[2]
        if ($LASTEXITCODE -ne 0) { throw "No se pudo crear $($u[0])" }
    }
} finally { Remove-Item Env:PROBANK_CLAVE, Env:PROBANK_SQL -ErrorAction SilentlyContinue }

# Clave de firma del token: aleatoria por máquina, nunca en el repositorio.
$jwt = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets --project src/ProBank.Api set "ConnectionStrings:ProBank" $cs | Out-Null
dotnet user-secrets --project src/ProBank.Api set "Jwt:Clave" $jwt | Out-Null
if ($LicenciaDevExtreme) { dotnet user-secrets --project src/ProBank.Web set "DevExtreme:LicenseKey" $LicenciaDevExtreme | Out-Null }
dotnet dev-certs https --trust | Out-Null

Write-Host "Listo. Base $base en (localdb)\$instancia. Ingresa como dev.local con la clave '$Clave'."
Write-Host "  Run and Debug > 'Api + Web', o: dotnet run --project src/ProBank.Api  y  dotnet run --project src/ProBank.Web"

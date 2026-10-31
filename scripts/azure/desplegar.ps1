# Despliega ProBank en Azure desde la máquina de quien despliega (Owner de la suscripción, sesión de az CLI).
# 1) backend de Terraform  2) infraestructura con acceso temporal desde tu IP  3) base (.dacpac) y permisos
# 4) administrador inicial  5) código de Api y Web  6) se vuelve a cerrar el acceso público.
# Uso: pwsh scripts/azure/desplegar.ps1 [-Ambiente dev] [-Admin admin.probank] [-CorreoAlertas tu@correo] [-RestablecerAdmin]
#      [-LicenciaDevExtreme <clave>]  (o la variable de entorno PROBANK_DEVEXTREME_LICENCIA; sin ella se conserva la ya configurada)
param(
    [ValidateSet("dev", "qa", "prod")] [string]$Ambiente = "dev",
    [string]$Admin = "admin.probank",
    [string]$CorreoAlertas,
    [string]$Ubicacion = "eastus",
    [switch]$RestablecerAdmin,
    [string]$LicenciaDevExtreme = $env:PROBANK_DEVEXTREME_LICENCIA
)
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
Set-Location (Join-Path $PSScriptRoot "../..")
$raiz = (Get-Location).Path
$tf = Join-Path $raiz "infra/envs/$Ambiente"
$salida = Join-Path ([System.IO.Path]::GetTempPath()) "probank-deploy-$Ambiente"
if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) { $env:PATH += ";$env:USERPROFILE\.dotnet\tools" }

function Paso($t) { Write-Host "`n== $t" -ForegroundColor Cyan }
function Tf { & terraform "-chdir=$tf" @args; if ($LASTEXITCODE -ne 0) { throw "terraform $($args[0]) falló" } }

$yo = az ad signed-in-user show --query "{id:id, upn:userPrincipalName}" -o json | ConvertFrom-Json
$sub = az account show --query id -o tsv
$ip = (Invoke-RestMethod https://api.ipify.org).Trim()
if (-not $CorreoAlertas) { $CorreoAlertas = $yo.upn }
$env:TF_VAR_admin_grupo_sql = (@{ nombre = $yo.upn; object_id = $yo.id } | ConvertTo-Json -Compress)
$env:TF_VAR_correo_alertas = $CorreoAlertas
$env:ARM_SUBSCRIPTION_ID = $sub
# Licencia de DevExtreme: la que se pasa o, si no, la que ya tiene la Web (para que un despliegue no la borre)
if (-not $LicenciaDevExtreme -and (az group exists -n "rg-probank-$Ambiente") -eq "true") {
    $LicenciaDevExtreme = az webapp config appsettings list -g "rg-probank-$Ambiente" -n "app-probank-$Ambiente-web" `
        --query "[?name=='DevExtreme__LicenseKey'].value | [0]" -o tsv 2>$null
}
$env:TF_VAR_devextreme_licencia = "$LicenciaDevExtreme"
if (-not $LicenciaDevExtreme) { Write-Host "Sin licencia de DevExtreme: la Web mostrará el aviso de evaluación." -ForegroundColor Yellow }
Write-Host "Suscripción $sub · despliega $($yo.upn) · IP temporal $ip"

Paso "1/6 Backend de Terraform (rg-probank-tfstate / stprobanktfstate)"
az group create -n rg-probank-tfstate -l $Ubicacion --tags sistema=ProBank -o none
if ((az storage account check-name -n stprobanktfstate --query nameAvailable -o tsv) -eq "true") {
    az storage account create -n stprobanktfstate -g rg-probank-tfstate -l $Ubicacion --sku Standard_LRS `
        --min-tls-version TLS1_2 --allow-blob-public-access false --allow-shared-key-access false -o none
    az storage account blob-service-properties update --account-name stprobanktfstate -g rg-probank-tfstate --enable-versioning true -o none
}
$st = az storage account show -n stprobanktfstate -g rg-probank-tfstate --query id -o tsv
if (-not (az role assignment list --assignee $yo.id --scope $st --role "Storage Blob Data Contributor" --query "[0].id" -o tsv)) {
    az role assignment create --assignee-object-id $yo.id --assignee-principal-type User --role "Storage Blob Data Contributor" --scope $st -o none
    Start-Sleep 60 # propagación de RBAC
}
az storage container create -n tfstate --account-name stprobanktfstate --auth-mode login -o none

Paso "2/6 Infraestructura (acceso temporal desde $ip)"
Tf init -input=false -reconfigure
$grupo = "rg-probank-$Ambiente"
if ((az group exists -n $grupo) -eq "true" -and -not ((Tf state list) -match '^azurerm_resource_group\.this$')) {
    Tf import azurerm_resource_group.this "/subscriptions/$sub/resourceGroups/$grupo"
}
try {
    Tf apply -input=false -auto-approve "-var=ip_despliegue=$ip"
    $o = Tf output -json | ConvertFrom-Json
    $sqlFqdn = $o.sql_servidor.value; $base = $o.sql_base.value; $api = $o.api_nombre.value; $web = $o.web_nombre.value

    Paso "3/6 Base de datos: .dacpac y permisos de la identidad de la API"
    dotnet build src/ProBank.Database/ProBank.Database.sqlproj -c Release -nologo -v q
    $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    $csSinAuth = "Server=tcp:$sqlFqdn,1433;Database=$base;Encrypt=True;Connect Timeout=60"
    sqlpackage /Action:Publish /SourceFile:src/ProBank.Database/bin/Release/ProBank.Database.dacpac `
        /TargetConnectionString:$csSinAuth /AccessToken:$token /Quiet:True

    # La API entra con su identidad administrada: usuario contenido y solo EXECUTE sobre los esquemas.
    # Con SID = clientId de la identidad no hace falta que el servidor SQL lea el directorio (Directory Readers).
    $apiPrincipal = az webapp identity show -g $grupo -n $api --query principalId -o tsv
    $apiClientId = az ad sp show --id $apiPrincipal --query appId -o tsv
    $permisos = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$api')
BEGIN
    DECLARE @crear nvarchar(400) = N'CREATE USER ' + QUOTENAME(N'$api') + N' WITH SID = '
        + CONVERT(nvarchar(40), CONVERT(varbinary(16), CONVERT(uniqueidentifier, '$apiClientId')), 1) + N', TYPE = E;';
    EXEC (@crear);
END;
DECLARE @s sysname, @sql nvarchar(400);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.schema_id BETWEEN 5 AND 16383 AND s.principal_id = 1;
OPEN c; FETCH NEXT FROM c INTO @s;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'GRANT EXECUTE ON SCHEMA::' + QUOTENAME(@s) + N' TO ' + QUOTENAME(N'$api') + N';';
    EXEC (@sql);
    FETCH NEXT FROM c INTO @s;
END;
CLOSE c;
"@
    $cn = New-Object System.Data.SqlClient.SqlConnection $csSinAuth
    $cn.AccessToken = $token
    $cn.Open()
    try { $cmd = $cn.CreateCommand(); $cmd.CommandText = $permisos; [void]$cmd.ExecuteNonQuery() } finally { $cn.Close() }

    Paso "4/6 Administrador inicial ($Admin)"
    $existe = $false
    $cn.Open()
    try { $cmd = $cn.CreateCommand(); $cmd.CommandText = "SELECT COUNT(*) FROM seg.Usuario WHERE Usuario = '$Admin' AND ClaveHash IS NOT NULL"; $existe = [int]$cmd.ExecuteScalar() -gt 0 } finally { $cn.Close() }
    $claveAdmin = $null
    if (-not $existe -or $RestablecerAdmin) {
        $abc = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789'
        $claveAdmin = -join (1..20 | ForEach-Object { $abc[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($abc.Length)] }) + "-Pb1"
        $env:PROBANK_SQL = "Server=tcp:$sqlFqdn,1433;Database=$base;Authentication=Active Directory Default;Encrypt=True;Connect Timeout=60"
        $env:PROBANK_CLAVE = $claveAdmin
        try {
            dotnet run --project tools/ProBank.Setup -c Release -- usuario --usuario $Admin --nombre "Administrador ProBank" `
                --roles "Administrador,Gerencia de TI,Consulta"
        } finally { Remove-Item Env:PROBANK_SQL, Env:PROBANK_CLAVE -ErrorAction SilentlyContinue }
        # Se muestra en cuanto se crea: si un paso posterior falla, la clave no se pierde.
        Write-Host "Usuario: $Admin · Clave inicial: $claveAdmin  (guárdala ahora; no se vuelve a mostrar)" -ForegroundColor Yellow
    } else { Write-Host "Ya existe; no se cambia su clave (usa -RestablecerAdmin para generar una nueva)." }

    Paso "5/6 Código de Api y Web (Run From Package desde Storage)"
    # Las apps leen su .zip con su identidad; no se usa el SCM, así que no depende de la red de quien despliega.
    $cuenta = ([uri]$o.paquetes_url.value).Host.Split('.')[0]
    Remove-Item $salida -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($p in @(@{ Proyecto = "ProBank.Api"; App = $api }, @{ Proyecto = "ProBank.Web"; App = $web })) {
        $dir = Join-Path $salida $p.Proyecto
        dotnet publish "src/$($p.Proyecto)" -c Release -o $dir -nologo -v q
        $zip = "$dir.zip"
        Compress-Archive -Path "$dir/*" -DestinationPath $zip -Force
        $PSNativeCommandUseErrorActionPreference = $false # los reintentos revisan $LASTEXITCODE
        foreach ($intento in 1..6) { # el rol sobre el contenedor puede tardar en propagarse
            az storage blob upload --account-name $cuenta --container-name despliegues --name "$($p.Proyecto).zip" `
                --file $zip --overwrite true --auth-mode login -o none 2>$null
            if ($LASTEXITCODE -eq 0) { break }
            if ($intento -eq 6) { throw "No se pudo subir $($p.Proyecto).zip" }
            Write-Host "Esperando permisos del contenedor (intento $intento)..."; Start-Sleep 30
        }
        $PSNativeCommandUseErrorActionPreference = $true
        az webapp restart -g $grupo -n $p.App -o none
        Write-Host "$($p.App) desplegada."
    }
}
finally {
    Paso "6/6 Cerrando el acceso público temporal"
    Tf apply -input=false -auto-approve "-var=ip_despliegue="
}

Paso "Listo"
Write-Host "Web: $($o.web_url.value)"

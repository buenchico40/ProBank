# ProBank

Sistema modular para los procesos de apoyo del banco: Seguridad, Coactivas, Portafolio, Presupuesto,
Cumplimiento y auditoría, Bóveda, Marketing, Consultas dinámicas, Versiones de aplicaciones
y componentes, y Reportes.

## Abrir en Visual Studio Code

1. Abre la carpeta raíz con `code .` (o `File › Open Workspace from File… › probank.code-workspace`).
2. Acepta instalar las extensiones recomendadas (`.vscode/extensions.json`).
3. Opcional: `Dev Containers: Reopen in Container` para tener .NET 10, Node 22, Terraform y Azure CLI sin instalar nada.

## Requisitos locales

| Herramienta | Versión |
|---|---|
| .NET SDK | 10.0 (fijado en `global.json`) |
| Node.js | 22 LTS (pruebas Playwright) |
| Terraform | 1.9 o superior |
| Azure CLI | 2.60 o superior |
| SQL Server | 2022 local, en contenedor o Azure SQL |
| DevExpress (.NET) | 26.1 con su licencia: paquetes de Reporting desde el instalador o el feed de DevExpress |

## Primeros pasos

```bash
# 1. Restaurar y compilar
dotnet build src/ProBank.sln

# 2. Pruebas unitarias
dotnet test test/ProBank.Tests.sln

# 3. Base de datos (proyecto SQL SDK-style)
dotnet build src/ProBank.Database/ProBank.Database.sqlproj   # genera el .dacpac
# publicar con SqlPackage o la extensión SQL Database Projects

# 4. Base local, usuarios de prueba y secretos de desarrollo (ver "Ejecutar en la máquina local")
pwsh scripts/local/setup-local.ps1

# 5. Ejecutar Api y Web juntos: en VS Code, Run and Debug › "Api + Web"

# 6. Pruebas de punta a punta (Playwright, TypeScript)
cd test/e2e && npm install && npx playwright install && npx playwright test
```

## Ejecutar en la máquina local (sin Azure)

```powershell
pwsh scripts/local/setup-local.ps1   # LocalDB + .dacpac + usuarios de prueba + user-secrets (cadena y Jwt:Clave)
# luego Run and Debug › "Api + Web"  (Web: https://localhost:7001 · Api: https://localhost:7101)
```

- Usuarios de prueba: `dev.local` (Administrador, Gerencia de TI, Consulta), `tecnico.local` y `aprobador.local`;
  todos con la clave `ProBank-Local-2026!` (cámbiala con `-Clave`). Solo existen en LocalDB.
- En local no se envía correo (Graph) ni hay Key Vault ni Application Insights. La llave maestra de Always Encrypted (Bóveda)
  es un certificado del usuario de Windows (`CN=ProBank Always Encrypted (local)`) que crea `ProBank.Setup llaves`.

## Usuarios

Inicio de sesión con usuario y contraseña ([ADR 0010](docs/adr/0010-usuario-y-clave.md)). Los usuarios, sus roles y
los permisos se administran en **Seguridad** (la clave temporal de un usuario nuevo se muestra una sola vez). Fuera de la
aplicación, por ejemplo para el primer administrador (la cadena va en `PROBANK_SQL`; la clave se pide por consola):

```powershell
dotnet run --project tools/ProBank.Setup -- usuario --usuario ana.perez --nombre "Ana Pérez" --roles "Gerencia de TI,Consulta"
```

## Desplegar en Azure

```powershell
pwsh scripts/azure/desplegar.ps1 -Ambiente dev
```

Crea el backend de Terraform, aplica `infra/envs/<ambiente>` abriendo SQL, Key Vault y la API solo a tu IP mientras dura
el despliegue, publica el `.dacpac`, da a la identidad de la API `EXECUTE` sobre los esquemas, crea `admin.probank`
(muestra su clave una sola vez), sube Api y Web y vuelve a cerrar el acceso. dev usa el perfil de costo mínimo
(App Service F1, oferta gratuita de Azure SQL, sin red privada); qa y prod usan red privada.

## Estructura

| Carpeta | Contenido |
|---|---|
| `spec/` | Especificación viva por módulo, trazabilidad y modelo de datos |
| `docs/` | Arquitectura, decisiones (ADR), evidencia de uso de IA y runbooks |
| `src/` | Solución .NET 10: Web, Api, Application, Domain, Infrastructure, Contracts y Database |
| `infra/` | Terraform: módulos de Azure y ambientes dev, qa y prod |
| `test/` | Pruebas unitarias, integración, Playwright (TypeScript) y carga |
| `tools/` | ProBank.Setup: administrador inicial y reseteo de clave |
| `prototype/` | Maqueta DevExtreme navegable (referencia visual de cada pantalla) |
| `scripts/` | Arranque del entorno local |

## Estado de la implementación

| Parte | Estado |
|---|---|
| Diseño (cabecera, menú en acordeón, pie, Panel, grillas y fichas) | Igual al prototipo, con DevExtreme (`wwwroot/js/probank.js`, `site.css`) |
| Seguridad (usuarios, roles y permisos, parámetros de acceso, auditoría, perfil) | De punta a punta; permisos por rol aplicados en el menú, la API y los SP |
| Coactivas (oficios, coactivas, bloqueos, levantamientos, transferencias, carga masiva, coactivas por cliente) | De punta a punta |
| Portafolio (ítems, hitos, tareas, aprobaciones, cronograma Gantt, acuerdos semanales, informes con DevExpress Reporting, documentos adjuntos, parametrización) | De punta a punta |
| Marketing (imágenes promocionales del Panel y del inicio de sesión, comunicados estructurados) | De punta a punta |
| Presupuesto por área, con áreas configurables, inicialmente TI (partidas y ejecución, ajustes y movimientos, renovaciones con aviso, solicitudes de compra, pagos con distribución, contratos y facturas, reporte PDF de servicios, pagos y presupuestado) | De punta a punta |
| Consultas dinámicas (bases de datos con secreto en Key Vault, catálogo con definición leída del SP, parámetros fijos o variables, aprobaciones, resultados y exportación en 5 formatos, auditoría, envíos programados por correo) | De punta a punta (pendiente: enlace que vence para archivos grandes y grupos de Entra ID) |
| Configuración (parámetros generales por categoría: correo por Microsoft 365 o SMTP con prueba de envío, conservación de resultados y adjuntos de Consultas dinámicas; cambios auditados) | De punta a punta |
| Demás módulos | Esquema de base de datos y página de inicio del área; reglas en `spec/modulos/` y pantallas en `prototype/` |
| Pruebas unitarias | Seguridad, claves, coactivas y carga masiva, tareas e informes, adjuntos, marketing, presupuesto TI, aprobación, variables de fecha, versiones y programación |
| Playwright | 8 pruebas pasan contra la maqueta (`--project=prototipo`); el resto queda como `test.fixme` por requisito |
| Terraform | Módulos y ambientes dev, qa y prod |

## Reglas que no se negocian

- Acceso a datos solo con procedimientos almacenados (Dapper, `CommandType.StoredProcedure`).
- Usuario y contraseña con PBKDF2 y bloqueo por intentos ([ADR 0010](docs/adr/0010-usuario-y-clave.md)); el rol activo se valida en la API y en los SP.
- Secretos y cadenas de conexión en Azure Key Vault; nunca en el repositorio.
- Toda acción queda en auditoría.

## DevExtreme

El Front-end usa DevExtreme 25.2 (jQuery) desde el CDN de DevExpress. Sin licencia se muestra el aviso de evaluación.
`DevExpress_License.txt` (del [Download Manager de DevExpress](https://www.devexpress.com/ClientCenter/DownloadManager/))
trae una clave **LCX**; en el navegador DevExtreme 25.2 necesita la clave **LCP** que se obtiene de ella con la
herramienta oficial `devextreme-license`. La clave nunca va en el repositorio:

```powershell
pwsh scripts/licencia-devextreme.ps1 [-Archivo <DevExpress_License.txt>]            # local (user-secrets)
pwsh scripts/licencia-devextreme.ps1 [-Archivo <DevExpress_License.txt>] -Ambiente dev  # además despliega en Azure
```

Los despliegues siguientes (`desplegar.ps1`) conservan la licencia ya configurada en la Web App.

## DevExpress Reporting

El informe de seguimiento de Portafolio es un reporte de DevExpress Reporting (XtraReports) que la API exporta a PDF
(`src/ProBank.Infrastructure/Reportes`). Los paquetes `DevExpress.Reporting.Core` y `DevExpress.Drawing.Skia` (para
App Service Linux) no están en nuget.org y su compilación valida la licencia de DevExpress .NET:

- **Local:** el instalador de DevExpress 26.1 registra su feed local y deja `%AppData%\DevExpress\DevExpress_License.txt`.
- **GitHub Actions:** secretos `DEVEXPRESS_NUGET_FEED` (`https://nuget.devexpress.com/<clave>/api/v3/index.json`) y
  `DEVEXPRESS_LICENSE` (contenido de `DevExpress_License.txt`), que usan `ci.yml` y `deploy.yml`.

El visor web de DevExpress Reporting 26.1 exige DevExtreme 26.1; mientras la Web use 25.2 el informe se muestra como PDF.
# ProBank

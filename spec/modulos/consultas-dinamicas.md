# Consultas dinámicas

**Propósito.** Ejecutar procedimientos almacenados parametrizados y exportar o enviar sus resultados.

## Reglas

| ID | Regla |
|---|---|
| SQL-01 | Solo procedimientos almacenados; no se aceptan queries libres. |
| SQL-02 | Bases de datos configurables; la cadena se guarda en Key Vault y nunca se muestra; se elige una al ejecutar. |
| SQL-03 | Aprobadores por ejecución, opcionales; si son varios, aprueban todos; quien solicita no aprueba. |
| SQL-04 | Parámetros con valor fijo o variable: Hoy, Ayer, inicio y fin de semana, mes, mes anterior, trimestre y año; Año actual, Año anterior, Mes actual. |
| SQL-05 | Exportación a Excel, CSV, PDF, JSON y texto delimitado. |
| SQL-06 | Auditoría de cada solicitud, aprobación, rechazo, ejecución (exitosa o fallida) y exportación, con parámetros resueltos, filas, duración e IP. |
| SQL-07 | Programación diaria, semanal o mensual; las variables se resuelven en cada corrida. |
| SQL-08 | Envío por correo con Microsoft Graph desde un buzón de servicio a usuarios y grupos de Entra ID; archivos grandes van como enlace que vence. |

## Reglas de operación

- **Bases de datos** (`CNX-NNN`): nombre, servidor, base, autenticación (usuario SQL de solo lectura o identidad administrada) y el
  **nombre** del secreto de Key Vault con la cadena de conexión (SQL-02). La cadena la carga operaciones en Key Vault; ProBank no la
  guarda, no la muestra y la API solo tiene permiso de lectura de secretos. *Probar conexión* abre la conexión y queda en la auditoría.
  Una base en uso no se elimina: se desactiva. En desarrollo, sin Key Vault, el secreto se lee de `Secretos:<nombre>` en user-secrets.
- **Consultas** (`QRY-NNN`): procedimiento almacenado `esquema.nombre` (SQL-01), bases permitidas, roles que la ejecutan, aprobadores
  opcionales, tiempo máximo (5 a 600 s) y límite de filas (1 a 100.000). *Leer definición* toma en una base permitida los parámetros
  (`sys.parameters`) y las columnas del primer resultado (`sp_describe_first_result_set`) sin ejecutar el procedimiento; los parámetros
  nuevos nacen obligatorios y los que ya existían conservan su configuración. *Configurar parámetros*: etiqueta, control (texto, número,
  fecha o lista con opciones), obligatorio y valor por defecto fijo o variable (SQL-04).
  **Borrador** → *Publicar* (con definición leída, una base activa y un rol) → **Publicada** → *Retirar* → **Borrador**. Cambiar el
  procedimiento o las bases de una consulta publicada, o que cambien sus parámetros al releer la definición, la devuelve a Borrador.
  Solo se elimina una consulta en borrador que nunca se ejecutó.
- **Ejecución**: el usuario elige una consulta publicada para su rol activo (el Administrador, todas), una base permitida y los valores;
  las variables se resuelven en ese momento y quedan con su valor en el texto auditable («Desde=Inicio de año → 01/01/2026»).
  Sin aprobadores se ejecuta al confirmar. Con aprobadores queda **Pendiente de aprobación**; aprueban todos, en cualquier orden;
  quien solicita no aprueba la suya; un rechazo (con motivo) la cierra; aprobada, el solicitante la ejecuta una vez con los valores
  que se aprobaron (SQL-03). Estados: Pendiente de aprobación, Aprobada, Rechazada, Ejecutada o Fallida (con el motivo, por ejemplo
  tiempo máximo excedido).
- **Resultados**: se guardan en el almacén (Blob Storage, contenedor `adjuntos`, `rpt/ejecuciones/EJE-NNNNNN.json`) para verlos y
  exportarlos sin volver a ejecutar. Los datos los ve y exporta solo quien solicitó la ejecución; los aprobadores autorizan, no consultan.
  Exportación a Excel (valores numéricos y de fecha reales), CSV, PDF, JSON y texto delimitado (SQL-05).
- **Auditoría de ejecuciones** (SQL-06): solicitud, aprobación, rechazo, ejecución (exitosa o fallida), cada exportación y cada envío,
  con usuario, parámetros resueltos, filas, duración e IP. La ven quienes tienen `audejec`, que además ven todas las ejecuciones.
- **Envíos programados** (`PRG-NNNN`, en la ficha de la consulta): base, diario, semanal (días) o mensual (día; 0 = último día), hora de
  Ecuador, formato, destinatarios y parámetros fijos o variables, que se resuelven en cada corrida (SQL-07). Solo para consultas
  publicadas **sin aprobadores**: al agregarle aprobadores, sus envíos pasan a pausa. El programador de la API corre cada minuto, reserva
  cada envío para que dos instancias no lo repitan, ejecuta, guarda el resultado y lo envía por correo con Graph desde el buzón de
  servicio o por el servidor SMTP del banco, según Configuración › Parámetros generales (SQL-08, ADR 0011); en desarrollo (`Correo:CarpetaLocal`) deja el
  correo como `.eml` en una carpeta.
- Permisos: `conexiones`, `consultas` (aprobar = publicar o retirar), `ejecuciones` (crear, aprobar, exportar) y `audejec`.

### Pendiente

- SQL-08: enviar los archivos grandes como enlace que vence (hoy, si superan el máximo de Configuración, el correo sale sin adjunto e
  indica la ejecución para descargarla en ProBank) y elegir grupos de Entra ID como destinatarios (hoy se eligen correos de usuarios
  de ProBank o se escriben).
- La conservación de los resultados guardados se define en Configuración › Parámetros generales (CFG-04; 90 días al instalar).

## Datos

rpt.Conexion, rpt.Consulta, rpt.ConsultaConexion, rpt.ConsultaRol, rpt.ConsultaAprobador, rpt.Parametro, rpt.Ejecucion,
rpt.EjecucionAprobador, rpt.EjecucionEvento, rpt.Programacion, rpt.ProgramacionDestinatario. Solo procedimientos almacenados
(`<esquema>.usp_<Entidad>_<Accion>`). Procedimientos de reporte de ejemplo sobre ProBank: `rpt.usp_Pagos_PorProveedor` y
`rpt.usp_Coactivas_PorEntidad`.

## Pruebas

- Unitarias: `test/ProBank.UnitTests/ConsultasDinamicas`.
- `test/e2e/specs/consultas.spec.ts` con una etiqueta por requisito (por ejemplo `@SQL-01`); las de la aplicación usan una base con el
  secreto `PROBANK_E2E_SECRETO` (por defecto `cnx-probank`) y se omiten si no está disponible.

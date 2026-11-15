# Decisiones

**Propósito.** Registrar las decisiones que se toman en el banco con sus aprobaciones firmadas, quiénes están al tanto y la
trazabilidad completa, y exportarlas a un PDF firmado.

## Reglas

| ID | Regla |
|---|---|
| DEC-01 | Cada decisión se registra con tipo, título, descripción (alcance, montos y condiciones), área y fecha; enviada, su contenido queda sellado. |
| DEC-02 | El tipo de decisión define los aprobadores y su orden (en secuencia o todos a la vez). |
| DEC-03 | Cada aprobador aprueba o rechaza con firma electrónica: confirma su identidad con su clave y queda un sello SHA-256 verificable. |
| DEC-04 | La decisión se exporta a PDF con su detalle, firmas y trazabilidad, firmado digitalmente con el certificado del banco. |
| DEC-05 | Notificados: personas que ven la decisión (sin firmar); se eligen al registrarla y se pueden agregar después. |
| DEC-06 | Trazabilidad de cada decisión: registro, ediciones, envío, firmas, cambios de aprobadores y notificados, consultas y exportaciones. |
| DEC-07 | Un aprobador puede modificar la decisión: nace una versión nueva y todos los aprobadores vuelven a firmar. |
| DEC-08 | Archivos de respaldo: se agregan en borrador y quedan fijos desde el envío; el PDF muestra sus nombres. |

## Reglas de operación

- **Tipos de decisión** (`TDE-NN`, DEC-02): nombre, descripción, flujo (en secuencia: firman por orden; todos a la vez) y aprobadores
  con su orden. Los cambios rigen para
  las decisiones que se registren o cambien de tipo después.
- **Registro** (`DEC-NNNN`, DEC-01 y DEC-05): al guardarla en borrador se proponen los aprobadores de su tipo (activos y
  nunca quien la registra) y se eligen los notificados. El alcance, los montos y las condiciones van en la descripción, con editor
  enriquecido (títulos, negritas, listas, citas, tablas y enlaces); el servidor guarda el HTML limpio con una lista cerrada de
  etiquetas (HtmlSanitizer: sin scripts, estilos ni eventos) y el PDF la muestra con su formato. En borrador
  la edita solo quien la registró; si cambia el tipo,
  se vuelven a proponer los aprobadores. *Enviar a aprobación* exige al menos un aprobador y fija la huella SHA-256 del contenido
  (código, versión, tipo, título, descripción, área y fecha; los archivos no entran): **Borrador → En aprobación → Aprobada, Rechazada o Anulada**.
- **Aprobadores** (DEC-03): quien registró la decisión los agrega en borrador o en aprobación; los **quita solo mientras nadie haya
  firmado** (en ninguna versión): después solo se agregan. Quien registra una decisión nunca la aprueba.
- **Mis aprobaciones**: *Por firmar* (las que esperan su firma ahora), *Firmadas por mí* (las que aprobó o rechazó, en cualquier
  versión, con su resultado y la fecha de su firma; desde ahí ve el detalle y exporta el PDF) y *Notificadas a mí*.
- **Notificados** (DEC-05): la ven en *Mis aprobaciones › Notificadas a mí* y cada vez que la abren queda en la trazabilidad (y la
  última consulta se ve en la decisión). Se agregan en cualquier estado salvo anulada; se quitan solo en borrador. No exportan el PDF.
- **Firma** (DEC-03 y DEC-07): cuando le toca (en secuencia, tras el anterior), el aprobador elige con su clave:
  - *Aprobar* (comentario opcional) o *Rechazar* (con motivo): un rechazo cierra la decisión; cuando todos aprueban queda Aprobada.
  - *Modificar*: corrige el contenido con un motivo. Nace la versión siguiente con otra huella, las firmas de la versión anterior se
    conservan en el historial (con su sello, verificable contra la huella de su versión) y todos vuelven a firmar desde el primero.
  - La clave incorrecta no registra nada y queda en la auditoría. El sello SHA-256 une la huella de la versión con el usuario, el
    resultado, la fecha y hora, el comentario y la IP; la decisión muestra si cada sello y la huella siguen coincidiendo («Íntegra»).
- **Archivos de respaldo** (DEC-08): quien la registró los agrega y quita en borrador (hasta 20, de hasta 20 MB; PDF, Office, imágenes,
  texto, CSV, XML o ZIP, y se verifica que el contenido corresponda a la extensión). Se guardan en Blob Storage con su huella
  SHA-256, que identifica cada archivo pero no forma parte de la huella de la decisión: la huella y las firmas cubren solo la
  información registrada. Desde el envío los archivos quedan fijos (ya no se agregan ni se quitan). Los descargan quienes ven la
  decisión y cada descarga queda en la trazabilidad.
- **Anular**: quien la registró (o quien tiene el permiso de anular) la anula con motivo mientras no se cierre.
- **Trazabilidad** (DEC-06): `dec.DecisionEvento`, solo inserciones, se ve en la decisión y en el PDF; además todo va a la auditoría.
- **PDF** (DEC-04): DevExpress Reporting, con el mismo diseño de los demás reportes: estado, archivos, firmas e integridad; detalle;
  archivos de respaldo (nombre, tamaño y huella); aprobaciones con su sello; notificados; versiones y firmas anteriores; trazabilidad
  y verificación. Lo firma el certificado del
  banco (Key Vault, secreto `firma-documentos`; en local sin Key Vault, uno autofirmado de desarrollo): el lector de PDF muestra la
  firma y avisa si el archivo cambió. Lo exportan quien la registró, sus aprobadores y quien tiene `decisiones` · exportar.
- Permisos: `decisiones` (ver todas, crear, editar, eliminar = anular, exportar), `pendientes` (ver y aprobar = firmar; al instalar,
  todos los roles) y `tiposdec`.

### Pendiente

- Avisos por correo al aprobador cuando le toca firmar y a los notificados cuando se cierra la decisión.

## Datos

dec.TipoDecision, dec.TipoAprobador, dec.Decision, dec.DecisionAprobador, dec.DecisionNotificado, dec.DecisionVersion,
dec.FirmaAnterior, dec.DecisionEvento, dec.DecisionAdjunto. Funciones `dec.fn_Huella`, `dec.fn_Sello` y `dec.fn_PuedeVer`. Solo procedimientos
almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Decisiones` (reglas, firma y modificación con clave, permisos de participantes).
- `test/e2e/specs/decisiones.spec.ts` con una etiqueta por requisito (por ejemplo `@DEC-03`); la prueba de firma entra con el
  aprobador de `PROBANK_E2E_APROBADOR` y `PROBANK_E2E_APROBADOR_CLAVE` y se omite si no están.

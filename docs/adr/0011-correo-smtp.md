# Correo por Microsoft Graph o SMTP, configurado en la aplicación

- **Estado:** Aceptada (reemplaza a [0008](0008-correo-graph.md))
- **Fecha:** 2026-11

## Decisión

Los correos (envíos programados de Consultas dinámicas, SQL-08) salen por el proveedor elegido en **Configuración › Parámetros
generales** (`cfg.Parametro`, categoría Correo), que se lee en cada envío: un cambio se aplica sin reiniciar ni volver a desplegar.

- **Graph** (por defecto): Microsoft 365 con `sendMail` desde el buzón de servicio. La API usa su identidad administrada; el permiso
  de aplicación `Mail.Send` se limita a ese buzón con una política de acceso de aplicación de Exchange Online.
- **Smtp**: servidor de correo del banco o relay (servidor, puerto, cifrado StartTls, TlsDirecto o Ninguna, usuario y remitente).
  Sin usuario envía como relay autorizado por IP; con usuario, la clave se lee de Key Vault: el parámetro guarda solo el nombre del
  secreto, que debe empezar con `smtp-`, y no se acepta usuario sin cifrado.
- **Carpeta**: solo desarrollo (hay `Correo:CarpetaLocal` en la configuración del servidor); deja cada correo como `.eml`.

Los valores se validan al guardarlos (`ReglasConfiguracion`) y cada cambio queda en la auditoría con su valor anterior. *Probar envío
de correo* envía un correo de prueba con lo guardado. Si un parámetro no tiene valor, se toma el de la configuración del servidor
(por ejemplo `Correo:BuzonServicio` de Terraform).

## Motivo

El banco tiene Microsoft 365 y también un servidor SMTP propio, y quiere administrarlos sin desplegar. Graph no guarda contraseñas y
limita el permiso a un buzón, por eso es el predeterminado. Para SMTP se usa MailKit (STARTTLS y TLS directo), no
`System.Net.Mail.SmtpClient`, que Microsoft no recomienda para código nuevo.

## Consecuencias

- Quien pueda editar la configuración puede cambiar por dónde salen los correos con resultados de consultas: el permiso `config` es
  solo del Administrador al instalar y cada cambio se audita. El prefijo `smtp-` impide usar otro secreto de Key Vault (por ejemplo,
  la clave de los tokens) como clave SMTP y enviarlo a un servidor externo.
- Con Microsoft 365 no se usa SMTP con autenticación básica (Microsoft la está retirando en Exchange Online): para Microsoft 365, Graph.
- Cualquier otro cambio se registra como un ADR nuevo que reemplaza a este.

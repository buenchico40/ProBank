# Inicio de sesión con usuario y contraseña

- **Estado:** Aceptada
- **Fecha:** 2026-10
- **Reemplaza:** [0001](0001-entra-id.md) en lo que toca al inicio de sesión de usuarios

## Contexto

El tenant de Entra ID disponible no permite registrar aplicaciones, así que no se pueden crear los registros de
ProBank API y ProBank Web (OIDC, roles de aplicación, scope `access_as_user`).

## Decisión

- Los usuarios entran con usuario y contraseña guardados en `seg.Usuario`: PBKDF2-SHA512, 600 000 iteraciones,
  sal de 32 bytes, mínimo 14 caracteres (`ProBank.Domain.Common.Claves`).
- La API valida la clave (`POST api/auth/ingresar`) y emite un JWT HMAC-SHA256 de 60 minutos con `name`, `roles`,
  `probank_uid` e `ipaddr`. La clave de firma vive en Key Vault (`jwt-clave`) y App Service la lee por referencia.
- La Web guarda el token en su cookie de sesión (HttpOnly, Secure, SameSite=Strict) y lo envía en cada llamada.
- 5 intentos fallidos bloquean la cuenta 15 minutos. Cada intento, exitoso o no, queda en `aud.Auditoria`.
- El mensaje de error no distingue entre usuario inexistente y clave incorrecta.
- Los usuarios se crean con `ProBank.Setup usuario` (y el administrador restringido por IP con `ProBank.Setup admin`).

## Lo que se mantiene en Entra ID

Las identidades administradas de App Service para llegar a Azure SQL, Key Vault y Storage: no son inicio de sesión
de personas y evitan guardar contraseñas de servicio.

## Consecuencias

- Sin MFA ni acceso condicional; si se recupera la posibilidad de registrar aplicaciones, conviene volver a 0001.
- Falta una pantalla para cambiar la contraseña y administrar usuarios (módulo Seguridad).
- La clave de firma se puede rotar en Key Vault; al reiniciar la API todas las sesiones vencen.

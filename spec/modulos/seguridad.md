# Seguridad

**Propósito.** Controlar quién entra, con qué rol y qué puede hacer.

## Reglas

| ID | Regla |
|---|---|
| SEG-01 | Un usuario tiene varios roles y un rol predeterminado; cambia el rol activo desde su perfil (y puede dejarlo como predeterminado). |
| SEG-02 | Autenticación con usuario y contraseña de ProBank ([ADR 0010](../../docs/adr/0010-usuario-y-clave.md)); la API emite el token de la sesión. |
| SEG-03 | Permisos por opción del menú × acción (ver, crear, editar, eliminar, exportar, aprobar) y por función propia (revocar sesiones, restablecer contraseña, desbloquear…). Se validan en la API y en los SP; el Administrador los tiene siempre todos. |
| SEG-04 | Cambio de IP en la sesión revoca la sesión. |
| SEG-05 | Auditoría general de toda acción, no editable ni eliminable. |
| SEG-06 | Contraseña de al menos 14 caracteres y sin el nombre de usuario (PBKDF2-SHA512). Tras N intentos fallidos la cuenta se bloquea M minutos (parámetros de acceso). |
| SEG-07 | Al crear o restablecer un usuario se genera una clave temporal que se muestra una vez y debe cambiarse al ingresar. Revocar sesiones, cambiar la clave, los roles o el estado invalida los tokens emitidos (sello de sesión). |

## Protecciones

- Solo un Administrador asigna o quita el rol Administrador, restablece la clave de otro Administrador o cambia sus propios roles.
- Nadie se desactiva a sí mismo y siempre queda al menos un Administrador activo.
- El rol Administrador no se renombra, no se desactiva y sus permisos no se editan.

## Pantallas

Usuarios, Roles y permisos (matriz módulo / opción / acción), Parámetros de acceso y Auditoría, con el diseño de
`prototype/ProBank_DevExtreme.html`. El perfil (cabecera) muestra la sesión y permite cambiar de rol y de contraseña.

## Datos

seg.Usuario, seg.Rol, seg.UsuarioRol, seg.Opcion, seg.OpcionAccion, seg.Permiso, seg.ParametroAcceso, aud.Auditoria.
Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`); los de escritura validan el permiso del rol
activo (`seg.fn_TienePermiso`) y registran la auditoría en la misma transacción.

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Seguridad` y `ClavesTests`.
- Integración: `test/ProBank.IntegrationTests/IngresoTests` y `SesionTests`.
- `test/e2e/specs/seguridad.spec.ts` con una etiqueta por requisito (por ejemplo `@SEG-01`).

# Certificados y secretos

**Propósito.** Vencimientos, rotación y acceso auditado a credenciales.

## Reglas

| ID | Regla |
|---|---|
| CRE-01 | Certificados, cuentas técnicas y secretos con vencimiento y rotación. |
| CRE-02 | Mostrar o copiar un valor es un permiso propio, solo para el grupo de acceso, y queda auditado. |
| CRE-03 | Valores cifrados con Always Encrypted; CMK en Key Vault. |

## Datos

cred.GrupoAcceso, cred.Certificado, cred.CuentaTecnica, cred.Secreto. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/credenciales.spec.ts` con una etiqueta por requisito (por ejemplo `@CRE-01`).

# Bóveda

**Propósito.** Bóveda de certificados, cuentas técnicas y secretos: vencimientos, rotación y acceso auditado a sus valores.

## Reglas

| ID | Regla |
|---|---|
| CRE-01 | Certificados, cuentas técnicas y secretos con vencimiento y rotación. |
| CRE-02 | Mostrar o copiar un valor es un permiso propio, solo para el grupo de acceso, y queda auditado. |
| CRE-03 | Valores cifrados con Always Encrypted; CMK en Key Vault. |

## Reglas de operación

- **Grupos de acceso** (`GRP-NN`): nombre, descripción, estado y miembros (usuarios activos de ProBank; al menos uno). Todos los
  miembros ven, copian y cambian las credenciales del grupo; un grupo inactivo no da acceso a nadie y no se desactiva mientras
  tenga credenciales activas. Quién entra y quién sale queda en la auditoría.
- **Certificados digitales** (`CER-NNNN`, CRE-01): nombre común, tipo (SSL/TLS, Firma de código, Cliente, Firma SAML), emisor,
  vencimiento, almacén, responsable, días de aviso, huella (SHA-1 o SHA-256), nombres alternativos y uso. Estado calculado:
  **Vigente**, **Por vencer** (dentro de los días de aviso), **Vencido** o **Inactivo**. *Registrar renovación* pide el nuevo
  vencimiento (posterior a hoy) y la huella del certificado nuevo, y deja el historial. ProBank no guarda certificados ni claves
  privadas: solo su vigencia.
- **Cuentas técnicas** (CRE-01): cuenta de servicio de Entra ID, del AD on-premise, de base de datos o identidad administrada de
  Azure; sistema, responsable, privilegios, grupo de acceso y próxima rotación. Estado: **Activo**, **Por rotar** (15 días antes),
  **Rotación vencida** o **Inactivo**. La identidad administrada no tiene contraseña, grupo ni rotación.
- **Secretos** (CRE-01): cadena de conexión, clave de API, secreto de cliente o token SAS; almacén, sistema, grupo, días de aviso
  y expiración. Mismo estado que los certificados. Nombre único por almacén.
- **Valor** de una cuenta o un secreto (CRE-02 y CRE-03):
  - *Cambiar* (permiso editar y ser miembro del grupo): al menos 16 caracteres (hay generador), vigencia de 30, 60, 90, 180 o 365
    días (próxima rotación o expiración) y nota opcional. Cada cambio crea una versión; solo la vigente conserva el valor.
  - *Mostrar* y *Copiar* son permisos propios (`mostrar`, `copiar`) y además exigen ser miembro del grupo. La base registra la
    entrega (usuario, versión, fecha, IP) en el historial de accesos y en la auditoría **antes** de devolver el valor; el intento de
    quien no es miembro también queda en la auditoría. El valor se muestra 30 segundos y no se guarda en la página.
  - Cambiar el grupo de una credencial cambia quién ve su valor: solo lo hace un miembro del grupo actual.
  - Los listados y el historial nunca traen el valor.
- **Cifrado** (CRE-03, ADR 0003): `cred.CredencialVersion.Valor` es `nvarchar(4000)` con Always Encrypted RANDOMIZED. Solo la API
  descifra: abre esa conexión con *Column Encryption Setting=Enabled* y la llave maestra (`CMK_ProBank`) está en Key Vault (en la
  máquina local, un certificado del usuario de Windows). La llave de columna (`CEK_ProBank`) se guarda en la base cifrada con la CMK.
  Cada ambiente crea sus llaves una vez con `ProBank.Setup llaves`, antes de publicar el `.dacpac`, que se publica con
  `/p:ExcludeObjectTypes=ColumnMasterKeys;ColumnEncryptionKeys` para no tocarlas (ver `docs/runbooks/rotacion-llaves.md`).
- Permisos: `grupos`, `certificados`, `cuentas` y `secretos` (ver, crear, editar, exportar), y `mostrar` y `copiar` en cuentas y
  secretos. Al instalar los tiene el rol Seguridad de la información (y el Administrador).

### Pendiente

- Avisos de vencimiento en el Panel y por correo (hoy el estado se ve en cada listado).
- Reporte de vencimientos de certificados y secretos.

## Datos

cred.GrupoAcceso, cred.GrupoMiembro, cred.Certificado, cred.CertificadoRenovacion, cred.CuentaTecnica, cred.Secreto,
cred.CredencialVersion (valor cifrado), cred.CredencialAcceso. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Boveda` (estados, huella, validaciones y permisos de mostrar, copiar y cambiar).
- `test/e2e/specs/boveda.spec.ts` con una etiqueta por requisito (por ejemplo `@CRE-01`); se omiten en la maqueta.

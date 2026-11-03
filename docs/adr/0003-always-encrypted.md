# Always Encrypted con CMK en Key Vault

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Decisión

Columnas sensibles cifradas con RANDOMIZED; la llave maestra vive en Key Vault.

## Motivo

Ni el DBA ve los valores en claro.

## Consecuencias

- La API abre con *Column Encryption Setting=Enabled* solo las conexiones que leen o escriben columnas cifradas (Bóveda), para no
  sumar a las demás consultas la ida a `sp_describe_parameter_encryption`. Los parámetros cifrados van con el tipo exacto de la
  columna (`nvarchar(4000)`) y los SP solo los insertan o devuelven: no los comparan ni los transforman.
- Las llaves son de cada ambiente: `ProBank.Setup llaves` crea la CMK (Key Vault; en local, un certificado del usuario de Windows) y
  la CEK una vez, y el `.dacpac` se publica sin tocarlas (`/p:ExcludeObjectTypes=ColumnMasterKeys;ColumnEncryptionKeys`).
  Rotación en `docs/runbooks/rotacion-llaves.md`.
- Cualquier excepción se registra como un ADR nuevo que reemplaza a este.

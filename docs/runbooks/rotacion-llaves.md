# Rotación de llaves y secretos

## Llaves de Always Encrypted (Bóveda)

- **Qué hay:** la llave maestra `CMK_ProBank` vive fuera de la base (Key Vault: llave `cmk-always-encrypted`, salida `cmk_id` de
  Terraform; en local, el certificado `CN=ProBank Always Encrypted (local)` del usuario de Windows). La llave de columna
  `CEK_ProBank` está en la base, cifrada con la CMK, y cifra `cred.CredencialVersion.Valor`.
- **Primera vez en un ambiente:** `ProBank.Setup llaves` (con `--keyvault <cmk_id>` en Azure) antes de publicar el `.dacpac`;
  `scripts/azure/desplegar.ps1` y `scripts/local/setup-local.ps1` ya lo hacen. Si la base ya tiene la CEK no cambia nada.
- **Publicar la base:** siempre con `/p:ExcludeObjectTypes=ColumnMasterKeys;ColumnEncryptionKeys`. El proyecto trae llaves de
  referencia solo para compilar; sin la exclusión, sqlpackage intentaría reemplazar las del ambiente.
- **Permisos:** la API necesita *Key Vault Crypto User* sobre el Key Vault (descifra la CEK) y quien crea las llaves, permiso
  para cifrar con la llave (*Key Vault Administrator* o *Crypto Officer*). El DBA no necesita acceso a la llave: sin ella solo ve
  bytes cifrados.
- **Rotar la CMK:** crear una versión nueva de la llave en Key Vault y, en una ventana de mantenimiento, ejecutar
  `Invoke-SqlColumnMasterKeyRotation` (módulo SqlServer) de `CMK_ProBank` a una CMK nueva con la ruta de esa versión; la API sigue
  funcionando durante la rotación porque la CEK queda cifrada con las dos. Al terminar, `Complete-SqlColumnMasterKeyRotation`.
- **Rotar la CEK:** solo si se sospecha que se expuso. Con el módulo SqlServer (`New-SqlColumnEncryptionKey` y
  `Set-SqlColumnEncryption`) en una ventana de mantenimiento; vuelve a cifrar la columna completa.
- **Si se pierde la CMK, se pierden los valores:** Key Vault tiene borrado temporal y protección de purga (`purge_protection`, apagada solo en dev); no borre ni desactive la llave.

## Otros secretos

- Cadenas de conexión de Consultas dinámicas: actualizar el secreto en Key Vault; ProBank toma la versión nueva al vencer la caché (5 minutos).
- Secreto del registro de aplicación de Entra ID: preferir identidad administrada; si existe secreto, rotarlo antes del vencimiento.

# Rotación de llaves y secretos

- CMK de Always Encrypted: crear una versión nueva en Key Vault y rotar con `Invoke-SqlColumnMasterKeyRotation` en una ventana de mantenimiento.
- Cadenas de conexión de Consultas dinámicas: actualizar el secreto en Key Vault; ProBank toma la versión nueva al vencer la caché (5 minutos).
- Secreto del registro de aplicación de Entra ID: preferir identidad administrada; si existe secreto, rotarlo antes del vencimiento.

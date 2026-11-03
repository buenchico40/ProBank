# ProBank.Database

Proyecto SQL SDK-style (`Microsoft.Build.Sql`). `dotnet build` genera `ProBank.Database.dacpac`.

- Un esquema por módulo (`Schemas/<esquema>/Tables`, `StoredProcedures`, `Functions`).
- Nomenclatura de SP: `<esquema>.usp_<Entidad>_<Accion>`.
- La aplicación solo tiene `EXECUTE` sobre los esquemas; no tiene `SELECT/INSERT/UPDATE/DELETE` directos.
- `aud` y `rpt.EjecucionEvento` solo reciben inserciones.
- Always Encrypted: `Security/` trae la CMK y la CEK solo de referencia para compilar; cada ambiente crea las suyas con
  `ProBank.Setup llaves` antes de publicar (ver `docs/runbooks/rotacion-llaves.md`).
- Publicar: `SqlPackage /Action:Publish /SourceFile:bin/Debug/ProBank.Database.dacpac /TargetConnectionString:"..." "/p:ExcludeObjectTypes=ColumnMasterKeys;ColumnEncryptionKeys"`.

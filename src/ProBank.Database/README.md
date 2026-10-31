# ProBank.Database

Proyecto SQL SDK-style (`Microsoft.Build.Sql`). `dotnet build` genera `ProBank.Database.dacpac`.

- Un esquema por módulo (`Schemas/<esquema>/Tables`, `StoredProcedures`, `Functions`).
- Nomenclatura de SP: `<esquema>.usp_<Entidad>_<Accion>`.
- La aplicación solo tiene `EXECUTE` sobre los esquemas; no tiene `SELECT/INSERT/UPDATE/DELETE` directos.
- `aud` y `rpt.EjecucionEvento` solo reciben inserciones.
- Publicar: `SqlPackage /Action:Publish /SourceFile:bin/Debug/ProBank.Database.dacpac /TargetConnectionString:"..."`.

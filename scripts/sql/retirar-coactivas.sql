-- Retiro del módulo Coactivas: se corre UNA vez por ambiente, DESPUÉS de publicar el .dacpac sin el módulo
-- (el .dacpac no borra objetos que ya no están en el proyecto). BORRA los datos de coactivas sin posibilidad de recuperarlos:
-- respalde la base antes. Las opciones, funciones, permisos y el rol los limpia el propio despliegue (Seed.PostDeployment.sql).
-- La auditoría (aud.Auditoria) se conserva.
-- Uso: sqlcmd -S <servidor> -d <base> -I -b -i scripts/sql/retirar-coactivas.sql
SET NOCOUNT ON; SET XACT_ABORT ON;

IF OBJECT_ID('rpt.usp_Panel_Resumen') IS NOT NULL
   AND OBJECT_DEFINITION(OBJECT_ID('rpt.usp_Panel_Resumen')) LIKE '%coa.vw_Coactiva%'
    THROW 50000, N'Publique primero el .dacpac sin Coactivas: rpt.usp_Panel_Resumen todavía usa el esquema coa.', 1;

BEGIN TRAN;

-- Consultas dinámicas que usaban el procedimiento de ejemplo: vuelven a borrador y sus envíos programados se pausan
UPDATE p SET Estado = 'Pausada'
FROM rpt.Programacion p JOIN rpt.Consulta c ON c.ConsultaId = p.ConsultaId
WHERE c.Procedimiento = N'rpt.usp_Coactivas_PorEntidad' AND p.Estado = 'Activa';
UPDATE rpt.Consulta SET Estado = 'Borrador' WHERE Procedimiento = N'rpt.usp_Coactivas_PorEntidad' AND Estado = 'Publicada';
IF OBJECT_ID('rpt.usp_Coactivas_PorEntidad') IS NOT NULL DROP PROCEDURE rpt.usp_Coactivas_PorEntidad;

-- Objetos del esquema coa: primero las llaves foráneas, luego vistas, procedimientos, funciones y tablas
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.foreign_keys WHERE OBJECT_SCHEMA_NAME(referenced_object_id) = 'coa' OR OBJECT_SCHEMA_NAME(parent_object_id) = 'coa';
SELECT @sql += N'DROP ' + CASE type WHEN 'V' THEN N'VIEW' WHEN 'P' THEN N'PROCEDURE' WHEN 'U' THEN N'TABLE' ELSE N'FUNCTION' END
             + N' ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.objects
WHERE schema_id = SCHEMA_ID('coa') AND type IN ('V', 'P', 'U', 'FN', 'IF', 'TF')
ORDER BY CASE type WHEN 'V' THEN 1 WHEN 'P' THEN 2 WHEN 'U' THEN 4 ELSE 3 END;
EXEC sys.sp_executesql @sql;
IF SCHEMA_ID('coa') IS NOT NULL DROP SCHEMA coa;

COMMIT;
PRINT N'Módulo Coactivas retirado.';

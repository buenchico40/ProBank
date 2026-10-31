-- SEG-03: reemplaza los permisos de un rol. @Permisos: pares "opcion:accion" separados por coma.
-- Toda opción con alguna acción o función recibe también "ver". Los permisos del Administrador no se editan.
CREATE PROCEDURE seg.usp_Permiso_Guardar
    @RolId INT, @Permisos VARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'roles', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar permisos.', 1;
    DECLARE @Rol VARCHAR(60) = (SELECT Nombre FROM seg.Rol WHERE RolId = @RolId);
    IF @Rol IS NULL THROW 50404, N'El rol no existe.', 1;
    IF @Rol = 'Administrador' THROW 50422, N'El rol Administrador tiene siempre todos los permisos.', 1;

    DECLARE @p TABLE (Opcion VARCHAR(60), Accion VARCHAR(60), PRIMARY KEY (Opcion, Accion));
    INSERT @p (Opcion, Accion)
    SELECT DISTINCT LEFT(v, CHARINDEX(':', v) - 1), SUBSTRING(v, CHARINDEX(':', v) + 1, 60)
    FROM (SELECT LTRIM(RTRIM(value)) AS v FROM STRING_SPLIT(@Permisos, ',')) s
    WHERE CHARINDEX(':', v) > 1;
    IF EXISTS (SELECT 1 FROM @p p WHERE NOT EXISTS (SELECT 1 FROM seg.OpcionAccion a WHERE a.Opcion = p.Opcion AND a.Accion = p.Accion))
        THROW 50422, N'Algún permiso no corresponde a una opción y acción del menú.', 1;
    INSERT @p (Opcion, Accion)
    SELECT DISTINCT x.Opcion, 'ver' FROM @p x WHERE NOT EXISTS (SELECT 1 FROM @p y WHERE y.Opcion = x.Opcion AND y.Accion = 'ver');

    DECLARE @Antes INT = (SELECT COUNT(*) FROM seg.Permiso WHERE RolId = @RolId);
    DECLARE @Despues INT = (SELECT COUNT(*) FROM @p), @Opciones INT = (SELECT COUNT(DISTINCT Opcion) FROM @p);
    BEGIN TRAN;
    DELETE seg.Permiso WHERE RolId = @RolId;
    INSERT seg.Permiso (RolId, Opcion, Accion) SELECT @RolId, Opcion, Accion FROM @p;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Edición de permisos',
            CONCAT(@Rol, N' · ', @Antes, N' → ', @Despues, N' permisos · ', @Opciones, N' opciones visibles'), @Ip);
    COMMIT;
END;

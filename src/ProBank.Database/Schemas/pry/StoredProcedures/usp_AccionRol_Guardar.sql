-- PRY-09: roles (nombres separados por coma, al menos uno activo) que pueden ejecutar la acción.
CREATE PROCEDURE pry.usp_AccionRol_Guardar
    @Accion VARCHAR(40), @Roles VARCHAR(800), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'paramproy', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para cambiar la parametrización.', 1;
    DECLARE @Nombre NVARCHAR(100) = (SELECT Nombre FROM pry.Accion WHERE Clave = @Accion);
    IF @Nombre IS NULL THROW 50404, N'La acción no existe.', 1;
    DECLARE @r TABLE (RolId INT PRIMARY KEY);
    INSERT @r SELECT RolId FROM seg.fn_RolesValidos(@Roles);
    IF NOT EXISTS (SELECT 1 FROM @r) THROW 50422, N'Seleccione al menos un rol activo.', 1;
    DECLARE @Antes NVARCHAR(800) = (SELECT STRING_AGG(r.Nombre, ', ') FROM pry.AccionRol ar JOIN seg.Rol r ON r.RolId = ar.RolId WHERE ar.Accion = @Accion);
    BEGIN TRAN;
    DELETE pry.AccionRol WHERE Accion = @Accion;
    INSERT pry.AccionRol (Accion, RolId) SELECT @Accion, RolId FROM @r;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Parametrización «' + @Nombre + N'»',
            CONCAT(ISNULL(@Antes, N'—'), N' → ', (SELECT STRING_AGG(r.Nombre, ', ') FROM @r x JOIN seg.Rol r ON r.RolId = x.RolId)), @Ip);
    COMMIT;
END;

-- Crea (@RolId NULL) o actualiza un rol. El rol Administrador no se renombra ni se desactiva.
-- Al renombrar se actualiza el rol predeterminado de los usuarios (se guarda por nombre).
CREATE PROCEDURE seg.usp_Rol_Guardar
    @RolId INT = NULL, @Nombre VARCHAR(60), @Descripcion NVARCHAR(300) = NULL, @Estado VARCHAR(20),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'roles', CASE WHEN @RolId IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para administrar roles.', 1;
    IF EXISTS (SELECT 1 FROM seg.Rol WHERE Nombre = @Nombre AND RolId <> ISNULL(@RolId, 0))
        THROW 50409, N'Ya existe un rol con ese nombre.', 1;

    BEGIN TRAN;
    IF @RolId IS NULL
    BEGIN
        INSERT seg.Rol (Nombre, Descripcion, Estado) VALUES (@Nombre, NULLIF(@Descripcion, N''), @Estado);
        SET @Id = SCOPE_IDENTITY();
        INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
        VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Nuevo rol', @Nombre, @Ip);
    END
    ELSE
    BEGIN
        DECLARE @Anterior VARCHAR(60) = (SELECT Nombre FROM seg.Rol WITH (UPDLOCK) WHERE RolId = @RolId);
        IF @Anterior IS NULL THROW 50404, N'El rol no existe.', 1;
        IF @Anterior = 'Administrador' AND (@Nombre <> 'Administrador' OR @Estado <> 'Activo')
            THROW 50422, N'El rol Administrador no se puede renombrar ni desactivar.', 1;
        UPDATE seg.Rol SET Nombre = @Nombre, Descripcion = NULLIF(@Descripcion, N''), Estado = @Estado WHERE RolId = @RolId;
        IF @Anterior <> @Nombre UPDATE seg.Usuario SET RolPredeterminado = @Nombre WHERE RolPredeterminado = @Anterior;
        SET @Id = @RolId;
        INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
        VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Edición de rol',
                CONCAT(@Anterior, CASE WHEN @Anterior <> @Nombre THEN N' → ' + @Nombre ELSE N'' END, N' · estado: ', @Estado), @Ip);
    END;
    COMMIT;
END;

-- SEG-01: datos, roles, rol predeterminado y estado. Si cambian los roles o el estado, las sesiones abiertas
-- del usuario se revocan (SEG-07) para que el nuevo token refleje el cambio. Siempre queda un Administrador activo.
CREATE PROCEDURE seg.usp_Usuario_Actualizar
    @Token UNIQUEIDENTIFIER, @Nombre NVARCHAR(150), @Correo VARCHAR(256) = NULL, @Roles VARCHAR(800), @RolPredeterminado VARCHAR(60),
    @Estado VARCHAR(20), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @UsuarioId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'usuarios', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar usuarios.', 1;

    DECLARE @Usuario VARCHAR(60), @EstadoAnterior VARCHAR(20);
    SELECT @UsuarioId = UsuarioId, @Usuario = Usuario, @EstadoAnterior = Estado FROM seg.Usuario WITH (UPDLOCK) WHERE Token = @Token;
    IF @UsuarioId IS NULL THROW 50404, N'El usuario no existe.', 1;

    DECLARE @r TABLE (RolId INT PRIMARY KEY, Nombre VARCHAR(60));
    INSERT @r SELECT RolId, Nombre FROM seg.fn_RolesValidos(@Roles);
    IF (SELECT COUNT(*) FROM @r) <> (SELECT COUNT(DISTINCT LTRIM(RTRIM(value))) FROM STRING_SPLIT(@Roles, ',') WHERE LTRIM(RTRIM(value)) <> '')
        THROW 50422, N'Algún rol no existe o está inactivo.', 1;

    DECLARE @Antes VARCHAR(800) = (SELECT STRING_AGG(r.Nombre, ',') WITHIN GROUP (ORDER BY r.Nombre)
                                   FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId WHERE ur.UsuarioId = @UsuarioId);
    DECLARE @Despues VARCHAR(800) = (SELECT STRING_AGG(Nombre, ',') WITHIN GROUP (ORDER BY Nombre) FROM @r);
    DECLARE @CambianRoles BIT = CASE WHEN ISNULL(@Antes, '') = ISNULL(@Despues, '') THEN 0 ELSE 1 END;
    DECLARE @AdminAntes BIT = CASE WHEN EXISTS (SELECT 1 FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId
                                                WHERE ur.UsuarioId = @UsuarioId AND r.Nombre = 'Administrador') THEN 1 ELSE 0 END;
    DECLARE @AdminDespues BIT = CASE WHEN EXISTS (SELECT 1 FROM @r WHERE Nombre = 'Administrador') THEN 1 ELSE 0 END;

    IF @AdminAntes <> @AdminDespues AND @RolActivo <> 'Administrador'
        THROW 50403, N'Solo un Administrador puede asignar o quitar el rol Administrador.', 1;
    IF @UsuarioId = @ActorId AND @CambianRoles = 1 AND @RolActivo <> 'Administrador'
        THROW 50403, N'No puedes cambiar tus propios roles.', 1;
    IF @UsuarioId = @ActorId AND @Estado <> 'Activo'
        THROW 50422, N'No puedes desactivar tu propio usuario.', 1;

    BEGIN TRAN;
    UPDATE seg.Usuario
    SET Nombre = @Nombre, Correo = NULLIF(@Correo, ''), RolPredeterminado = @RolPredeterminado, Estado = @Estado,
        SelloSesion = SelloSesion + CASE WHEN @CambianRoles = 1 OR @Estado <> @EstadoAnterior THEN 1 ELSE 0 END
    WHERE UsuarioId = @UsuarioId;
    IF @CambianRoles = 1
    BEGIN
        DELETE seg.UsuarioRol WHERE UsuarioId = @UsuarioId AND RolId NOT IN (SELECT RolId FROM @r);
        INSERT seg.UsuarioRol (UsuarioId, RolId) SELECT @UsuarioId, x.RolId FROM @r x
        WHERE NOT EXISTS (SELECT 1 FROM seg.UsuarioRol ur WHERE ur.UsuarioId = @UsuarioId AND ur.RolId = x.RolId);
    END;
    IF NOT EXISTS (SELECT 1 FROM seg.Usuario u JOIN seg.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
                   JOIN seg.Rol r ON r.RolId = ur.RolId AND r.Nombre = 'Administrador'
                   WHERE u.Estado = 'Activo' AND u.ClaveHash IS NOT NULL)
        THROW 50422, N'Debe quedar al menos un Administrador activo.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Edición de usuario',
            CONCAT(@Usuario, N' · estado: ', @Estado,
                   CASE WHEN @CambianRoles = 1 THEN CONCAT(N' · roles: ', ISNULL(@Antes, N'—'), N' → ', @Despues) ELSE N'' END), @Ip);
    COMMIT;
END;

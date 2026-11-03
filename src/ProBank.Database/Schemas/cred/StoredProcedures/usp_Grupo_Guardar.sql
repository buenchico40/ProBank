-- CRE-02: crea (@Token NULL, código GRP-NN) o edita un grupo de acceso con sus miembros (@Miembros: ids de usuario separados
-- por comas). Un grupo con credenciales activas no se desactiva: dejaría a esas credenciales sin quién pueda verlas.
CREATE PROCEDURE cred.usp_Grupo_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(300) = NULL, @Estado VARCHAR(10), @Miembros VARCHAR(MAX),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'grupos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar grupos de acceso.', 1;
    IF @Estado NOT IN ('Activo', 'Inactivo') THROW 50422, N'El estado es Activo o Inactivo.', 1;

    DECLARE @Nuevos TABLE (UsuarioId INT PRIMARY KEY);
    INSERT @Nuevos (UsuarioId)
    SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT) FROM STRING_SPLIT(ISNULL(@Miembros, ''), ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    IF EXISTS (SELECT 1 FROM @Nuevos n LEFT JOIN seg.Usuario u ON u.UsuarioId = n.UsuarioId AND u.Estado = 'Activo' WHERE u.UsuarioId IS NULL)
        THROW 50422, N'Los miembros deben ser usuarios activos de ProBank.', 1;

    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM cred.GrupoAcceso WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Nombre AND (@Token IS NULL OR Token <> @Token))
        THROW 50409, N'Ya existe un grupo de acceso con ese nombre.', 1;
    DECLARE @GrupoId INT, @Codigo VARCHAR(10);
    IF @Token IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM cred.GrupoAcceso WITH (UPDLOCK, HOLDLOCK)), 0);
        SET @Codigo = 'GRP-' + CASE WHEN @n < 10 THEN '0' ELSE '' END + CAST(@n AS VARCHAR(10));
        SET @TokenGuardado = NEWID();
        INSERT cred.GrupoAcceso (Token, Codigo, Nombre, Descripcion, Estado) VALUES (@TokenGuardado, @Codigo, @Nombre, @Descripcion, @Estado);
        SET @GrupoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @GrupoId = GrupoId, @Codigo = Codigo FROM cred.GrupoAcceso WITH (UPDLOCK) WHERE Token = @Token;
        IF @GrupoId IS NULL THROW 50404, N'El grupo de acceso no existe.', 1;
        IF @Estado = 'Inactivo' AND (EXISTS (SELECT 1 FROM cred.CuentaTecnica WHERE GrupoId = @GrupoId AND Activo = 1)
                                  OR EXISTS (SELECT 1 FROM cred.Secreto WHERE GrupoId = @GrupoId AND Activo = 1))
            THROW 50409, N'El grupo tiene credenciales activas: asígnelas a otro grupo o desactívelas antes de desactivar el grupo.', 1;
        UPDATE cred.GrupoAcceso SET Nombre = @Nombre, Descripcion = @Descripcion, Estado = @Estado WHERE GrupoId = @GrupoId;
        SET @TokenGuardado = @Token;
    END;

    -- Miembros: quién entra y quién sale queda en la auditoría
    DECLARE @Altas NVARCHAR(MAX) = (SELECT STRING_AGG(u.Usuario, ', ') FROM @Nuevos n JOIN seg.Usuario u ON u.UsuarioId = n.UsuarioId
                                    WHERE NOT EXISTS (SELECT 1 FROM cred.GrupoMiembro m WHERE m.GrupoId = @GrupoId AND m.UsuarioId = n.UsuarioId));
    DECLARE @Bajas NVARCHAR(MAX) = (SELECT STRING_AGG(u.Usuario, ', ') FROM cred.GrupoMiembro m JOIN seg.Usuario u ON u.UsuarioId = m.UsuarioId
                                    WHERE m.GrupoId = @GrupoId AND NOT EXISTS (SELECT 1 FROM @Nuevos n WHERE n.UsuarioId = m.UsuarioId));
    DELETE m FROM cred.GrupoMiembro m WHERE m.GrupoId = @GrupoId AND NOT EXISTS (SELECT 1 FROM @Nuevos n WHERE n.UsuarioId = m.UsuarioId);
    INSERT cred.GrupoMiembro (GrupoId, UsuarioId)
    SELECT @GrupoId, n.UsuarioId FROM @Nuevos n WHERE NOT EXISTS (SELECT 1 FROM cred.GrupoMiembro m WHERE m.GrupoId = @GrupoId AND m.UsuarioId = n.UsuarioId);

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda',
            CASE WHEN @Token IS NULL THEN N'Nuevo grupo de acceso ' ELSE N'Edición del grupo de acceso ' END + @Codigo,
            CONCAT(@Nombre, N' · ', @Estado, N' · ', (SELECT COUNT(*) FROM @Nuevos), N' miembros',
                   N' · entran: ' + @Altas, N' · salen: ' + @Bajas), @Ip);
    COMMIT;
END;

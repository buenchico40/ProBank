-- SEG-01: crea un usuario con sus roles y una clave temporal que debe cambiar al ingresar (SEG-07).
-- @Roles: nombres separados por coma. Solo un Administrador asigna el rol Administrador.
CREATE PROCEDURE seg.usp_Usuario_Crear
    @Usuario VARCHAR(60), @Nombre NVARCHAR(150), @Correo VARCHAR(256) = NULL, @Roles VARCHAR(800), @RolPredeterminado VARCHAR(60),
    @ClaveHash VARBINARY(64), @ClaveSal VARBINARY(32),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Token UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'usuarios', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para crear usuarios.', 1;
    IF EXISTS (SELECT 1 FROM seg.Usuario WHERE Usuario = @Usuario)
        THROW 50409, N'Ya existe un usuario con ese nombre.', 1;

    DECLARE @r TABLE (RolId INT PRIMARY KEY, Nombre VARCHAR(60));
    INSERT @r SELECT RolId, Nombre FROM seg.fn_RolesValidos(@Roles);
    IF (SELECT COUNT(*) FROM @r) <> (SELECT COUNT(DISTINCT LTRIM(RTRIM(value))) FROM STRING_SPLIT(@Roles, ',') WHERE LTRIM(RTRIM(value)) <> '')
        THROW 50422, N'Algún rol no existe o está inactivo.', 1;
    IF EXISTS (SELECT 1 FROM @r WHERE Nombre = 'Administrador') AND @RolActivo <> 'Administrador'
        THROW 50403, N'Solo un Administrador puede asignar el rol Administrador.', 1;

    BEGIN TRAN;
    SET @Token = NEWID();
    INSERT seg.Usuario (Token, Usuario, Nombre, Correo, RolPredeterminado, ClaveHash, ClaveSal, ClaveCambiada, DebeCambiarClave)
    VALUES (@Token, @Usuario, @Nombre, NULLIF(@Correo, ''), @RolPredeterminado, @ClaveHash, @ClaveSal, SYSDATETIME(), 1);
    DECLARE @Id INT = SCOPE_IDENTITY();
    INSERT seg.UsuarioRol (UsuarioId, RolId) SELECT @Id, RolId FROM @r;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Nuevo usuario', CONCAT(@Usuario, N' · roles: ', @Roles), @Ip);
    COMMIT;
END;

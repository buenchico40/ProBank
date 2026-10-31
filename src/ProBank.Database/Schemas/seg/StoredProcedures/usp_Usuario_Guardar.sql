-- Lo usa ProBank.Setup (comando "usuario"): crea o actualiza un usuario con contraseña y reemplaza sus roles.
-- @Roles: nombres separados por coma, p. ej. 'Gerencia de TI,Consulta'.
CREATE PROCEDURE seg.usp_Usuario_Guardar
    @Usuario VARCHAR(60), @Nombre NVARCHAR(150), @Correo VARCHAR(256) = NULL,
    @ClaveHash VARBINARY(64), @ClaveSal VARBINARY(32), @Roles VARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    MERGE seg.Usuario AS t
    USING (SELECT @Usuario AS Usuario) AS s ON t.Usuario = s.Usuario
    WHEN MATCHED THEN UPDATE SET Nombre = @Nombre, Correo = @Correo, ClaveHash = @ClaveHash, ClaveSal = @ClaveSal,
        ClaveCambiada = SYSDATETIME(), DebeCambiarClave = 0,
        Estado = 'Activo', IntentosFallidos = 0, BloqueadoHasta = NULL, SelloSesion = t.SelloSesion + 1
    WHEN NOT MATCHED THEN INSERT (Usuario, Nombre, Correo, ClaveHash, ClaveSal, ClaveCambiada, RolPredeterminado)
        VALUES (@Usuario, @Nombre, @Correo, @ClaveHash, @ClaveSal, SYSDATETIME(), NULLIF(LTRIM(RTRIM(LEFT(@Roles, CHARINDEX(',', @Roles + ',') - 1))), ''));

    DECLARE @UsuarioId INT = (SELECT UsuarioId FROM seg.Usuario WHERE Usuario = @Usuario);
    DELETE seg.UsuarioRol WHERE UsuarioId = @UsuarioId;
    INSERT seg.UsuarioRol (UsuarioId, RolId)
    SELECT @UsuarioId, r.RolId FROM seg.Rol r
    WHERE r.Nombre IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@Roles, ','));

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (@UsuarioId, 'Administrador', 'Seguridad', N'Usuario creado o actualizado', CONCAT(@Usuario, N' · ', @Roles), NULL);
    COMMIT;
END;

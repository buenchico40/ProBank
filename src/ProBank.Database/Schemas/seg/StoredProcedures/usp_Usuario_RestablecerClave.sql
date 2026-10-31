-- SEG-07: un administrador asigna una clave temporal; el usuario debe cambiarla al ingresar y se revocan sus sesiones.
CREATE PROCEDURE seg.usp_Usuario_RestablecerClave
    @Token UNIQUEIDENTIFIER, @ClaveHash VARBINARY(64), @ClaveSal VARBINARY(32),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @UsuarioId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'usuarios', 'clave') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para restablecer contraseñas.', 1;
    DECLARE @Usuario VARCHAR(60);
    SELECT @UsuarioId = UsuarioId, @Usuario = Usuario FROM seg.Usuario WHERE Token = @Token;
    IF @UsuarioId IS NULL THROW 50404, N'El usuario no existe.', 1;
    IF @RolActivo <> 'Administrador' AND EXISTS (SELECT 1 FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId
                                                 WHERE ur.UsuarioId = @UsuarioId AND r.Nombre = 'Administrador')
        THROW 50403, N'Solo un Administrador puede restablecer la contraseña de otro Administrador.', 1;

    BEGIN TRAN;
    UPDATE seg.Usuario
    SET ClaveHash = @ClaveHash, ClaveSal = @ClaveSal, ClaveCambiada = SYSDATETIME(), DebeCambiarClave = 1,
        IntentosFallidos = 0, BloqueadoHasta = NULL, SelloSesion = SelloSesion + 1
    WHERE UsuarioId = @UsuarioId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Restablecimiento de contraseña', @Usuario, @Ip);
    COMMIT;
END;

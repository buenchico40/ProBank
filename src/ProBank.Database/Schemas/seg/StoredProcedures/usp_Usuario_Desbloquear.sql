-- SEG-06: quita el bloqueo por intentos fallidos antes de que venza.
CREATE PROCEDURE seg.usp_Usuario_Desbloquear
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'usuarios', 'desbloquear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para desbloquear usuarios.', 1;
    DECLARE @UsuarioId INT, @Usuario VARCHAR(60);
    SELECT @UsuarioId = UsuarioId, @Usuario = Usuario FROM seg.Usuario WHERE Token = @Token;
    IF @UsuarioId IS NULL THROW 50404, N'El usuario no existe.', 1;
    BEGIN TRAN;
    UPDATE seg.Usuario SET IntentosFallidos = 0, BloqueadoHasta = NULL WHERE UsuarioId = @UsuarioId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Desbloqueo de usuario', @Usuario, @Ip);
    COMMIT;
END;

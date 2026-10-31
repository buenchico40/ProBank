-- SEG-07 (función propia «Revocar sesiones»): sube el sello; los tokens emitidos antes dejan de valer.
CREATE PROCEDURE seg.usp_Usuario_RevocarSesiones
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @UsuarioId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'usuarios', 'revocar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para revocar sesiones.', 1;
    DECLARE @Usuario VARCHAR(60);
    SELECT @UsuarioId = UsuarioId, @Usuario = Usuario FROM seg.Usuario WHERE Token = @Token;
    IF @UsuarioId IS NULL THROW 50404, N'El usuario no existe.', 1;
    BEGIN TRAN;
    UPDATE seg.Usuario SET SelloSesion = SelloSesion + 1 WHERE UsuarioId = @UsuarioId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Revocación de sesiones', @Usuario, @Ip);
    COMMIT;
END;

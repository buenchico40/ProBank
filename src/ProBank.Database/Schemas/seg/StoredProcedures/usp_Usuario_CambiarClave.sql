-- El propio usuario cambia su clave (la API ya verificó la actual). Revoca sus sesiones: vuelve a ingresar con la nueva.
CREATE PROCEDURE seg.usp_Usuario_CambiarClave
    @UsuarioId INT, @ClaveHash VARBINARY(64), @ClaveSal VARBINARY(32), @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    UPDATE seg.Usuario
    SET ClaveHash = @ClaveHash, ClaveSal = @ClaveSal, ClaveCambiada = SYSDATETIME(), DebeCambiarClave = 0, SelloSesion = SelloSesion + 1
    WHERE UsuarioId = @UsuarioId AND Estado = 'Activo';
    IF @@ROWCOUNT = 0 THROW 50404, N'El usuario no existe o está inactivo.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (@UsuarioId, @RolActivo, 'Seguridad', N'Cambio de contraseña', NULL, @Ip);
    COMMIT;
END;

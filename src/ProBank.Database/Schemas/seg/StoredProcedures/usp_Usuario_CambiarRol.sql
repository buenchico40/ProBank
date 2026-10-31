-- SEG-01: cambio del rol activo desde el perfil; opcionalmente queda como predeterminado para el próximo ingreso.
CREATE PROCEDURE seg.usp_Usuario_CambiarRol
    @UsuarioId INT, @Rol VARCHAR(60), @Predeterminado BIT, @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId AND r.Estado = 'Activo'
                   WHERE ur.UsuarioId = @UsuarioId AND r.Nombre = @Rol)
        THROW 50403, N'El rol no está asignado a tu usuario.', 1;
    BEGIN TRAN;
    IF @Predeterminado = 1 UPDATE seg.Usuario SET RolPredeterminado = @Rol WHERE UsuarioId = @UsuarioId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (@UsuarioId, @Rol, 'Seguridad', CONCAT(N'Cambio de rol activo a ', @Rol),
            CASE WHEN @Predeterminado = 1 THEN N'Queda como rol predeterminado' END, @Ip);
    COMMIT;
END;

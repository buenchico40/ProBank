-- Anula un movimiento pendiente (lo aprobado ya cambió el presupuesto: se corrige con otro movimiento).
CREATE PROCEDURE com.usp_Movimiento_Anular
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'movimientos', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para anular movimientos.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @Codigo = Codigo, @Estado = Estado FROM com.Movimiento WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'El movimiento no existe.', 1;
    IF @Estado <> 'Pendiente' THROW 50422, N'Solo se anulan movimientos pendientes; lo aprobado se corrige con otro movimiento.', 1;
    UPDATE com.Movimiento SET Estado = 'Anulada' WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Anulación del movimiento ' + @Codigo, NULL, @Ip);
    COMMIT;
END;

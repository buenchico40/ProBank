-- Anula un pago. Si ya estaba Pagado, deja de descontar de sus partidas y lo que falte pagar vuelve a ser comprometido de la
-- partida de la solicitud (que debe tener disponible).
CREATE PROCEDURE com.usp_Pago_Anular
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'pagos', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para anular pagos.', 1;
    BEGIN TRAN;
    DECLARE @PagoId INT, @Codigo VARCHAR(20), @Estado VARCHAR(20), @SolicitudPartida INT;
    SELECT @PagoId = pg.PagoId, @Codigo = pg.Codigo, @Estado = pg.Estado, @SolicitudPartida = s.PartidaId
    FROM com.Pago pg WITH (UPDLOCK) JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId WHERE pg.Token = @Token;
    IF @PagoId IS NULL THROW 50404, N'El pago no existe.', 1;
    IF @Estado = 'Anulado' THROW 50422, N'El pago ya está anulado.', 1;
    UPDATE com.Pago SET Estado = 'Anulado' WHERE PagoId = @PagoId;
    IF @Estado = 'Pagado' AND @SolicitudPartida IS NOT NULL
    BEGIN
        DECLARE @p VARCHAR(20) = CAST(@SolicitudPartida AS VARCHAR(20));
        EXEC com.usp_Partida_ExigirDisponible @p;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Anulación del pago ' + @Codigo, CONCAT(N'Estaba ', LOWER(@Estado)), @Ip);
    COMMIT;
END;

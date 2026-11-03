-- COM-04: marca un pago como Pagado y su distribución descuenta de cada partida. Si la solicitud es presupuestada, la
-- distribución debe cuadrar con el monto, en partidas activas que no queden con disponible negativo. Un pago con fecha
-- futura queda programado (Pendiente) hasta su fecha.
CREATE PROCEDURE com.usp_Pago_Pagar
    @Token UNIQUEIDENTIFIER, @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'pagos', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar pagos.', 1;
    BEGIN TRAN;
    DECLARE @PagoId INT, @Codigo VARCHAR(20), @Estado VARCHAR(20), @Monto DECIMAL(18,2), @Presupuestado BIT, @Fecha DATE;
    SELECT @PagoId = pg.PagoId, @Codigo = pg.Codigo, @Estado = pg.Estado, @Monto = pg.Monto, @Presupuestado = s.Presupuestado, @Fecha = pg.Fecha
    FROM com.Pago pg WITH (UPDLOCK) JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId WHERE pg.Token = @Token;
    IF @PagoId IS NULL THROW 50404, N'El pago no existe.', 1;
    IF @Estado <> 'Pendiente' THROW 50422, N'Solo se paga un pago pendiente.', 1;
    IF @Fecha > @Hoy THROW 50422, N'El pago está programado para una fecha futura: se marca como Pagado desde esa fecha.', 1;
    DECLARE @Distribuido DECIMAL(18,2) = ISNULL((SELECT SUM(Monto) FROM com.PagoDistribucion WHERE PagoId = @PagoId), 0);
    IF @Presupuestado = 1 AND @Distribuido <> @Monto THROW 50422, N'La distribución por partida debe cuadrar con el monto del pago.', 1;
    IF EXISTS (SELECT 1 FROM com.PagoDistribucion d JOIN com.Partida pa ON pa.PartidaId = d.PartidaId WHERE d.PagoId = @PagoId AND pa.Estado <> 'Activo')
        THROW 50422, N'Las partidas de la distribución deben estar activas.', 1;
    UPDATE com.Pago SET Estado = 'Pagado', PagadoPorId = @ActorId, FechaPagado = SYSDATETIME() WHERE PagoId = @PagoId;
    DECLARE @Partidas VARCHAR(MAX) = (SELECT STRING_AGG(CAST(PartidaId AS VARCHAR(20)), ',') FROM com.PagoDistribucion WHERE PagoId = @PagoId);
    IF @Partidas IS NOT NULL EXEC com.usp_Partida_ExigirDisponible @Partidas;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', N'Pago realizado ' + @Codigo, CONCAT(@Monto, N' · descuenta de ', ISNULL(@Partidas, N'ninguna partida')), @Ip);
    COMMIT;
END;

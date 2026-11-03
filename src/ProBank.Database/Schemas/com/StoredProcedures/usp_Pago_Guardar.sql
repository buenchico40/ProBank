-- Registra (@Token NULL) o corrige un pago pendiente de una solicitud aprobada, con su distribución por partida.
-- @Distribucion: JSON [{"partida":"<token>","monto":0.00}] (vacío si la solicitud no es presupuestada). El monto no supera lo que
-- falta pagar de la solicitud contando los demás pagos no anulados; la distribución debe cuadrar al marcarlo Pagado.
CREATE PROCEDURE com.usp_Pago_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @SolicitudToken UNIQUEIDENTIFIER, @Proveedor NVARCHAR(150), @Fecha DATE, @Monto DECIMAL(18,2),
    @Distribucion NVARCHAR(MAX) = N'[]', @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'pagos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar pagos.', 1;
    BEGIN TRAN;
    DECLARE @SolicitudId INT, @Total DECIMAL(18,2), @Presupuestado BIT, @Aprobacion VARCHAR(20), @Anulada BIT;
    SELECT @SolicitudId = SolicitudId, @Total = Monto, @Presupuestado = Presupuestado, @Aprobacion = Aprobacion, @Anulada = Anulada
    FROM com.Solicitud WITH (UPDLOCK) WHERE Token = @SolicitudToken;
    IF @SolicitudId IS NULL THROW 50404, N'La solicitud no existe.', 1;
    IF @Anulada = 1 OR @Aprobacion <> 'Aprobada' THROW 50422, N'Solo se pagan solicitudes aprobadas.', 1;

    DECLARE @PagoId INT;
    IF @Token IS NOT NULL
    BEGIN
        DECLARE @Estado VARCHAR(20), @SolicitudActual INT;
        SELECT @PagoId = PagoId, @Estado = Estado, @SolicitudActual = SolicitudId FROM com.Pago WITH (UPDLOCK) WHERE Token = @Token;
        IF @PagoId IS NULL THROW 50404, N'El pago no existe.', 1;
        IF @Estado <> 'Pendiente' THROW 50422, N'Solo se corrigen pagos pendientes.', 1;
        IF @SolicitudActual <> @SolicitudId THROW 50422, N'La solicitud de un pago no cambia.', 1;
    END;
    IF @Monto + ISNULL((SELECT SUM(Monto) FROM com.Pago WHERE SolicitudId = @SolicitudId AND Estado <> 'Anulado' AND PagoId <> ISNULL(@PagoId, 0)), 0) > @Total
        THROW 50422, N'El pago supera lo que falta pagar de la solicitud.', 1;

    DECLARE @d TABLE (PartidaId INT NULL, Monto DECIMAL(18,2));
    INSERT @d SELECT pa.PartidaId, j.Monto
    FROM OPENJSON(@Distribucion) WITH (Partida UNIQUEIDENTIFIER '$.partida', Monto DECIMAL(18,2) '$.monto') j
    LEFT JOIN com.Partida pa ON pa.Token = j.Partida AND pa.Estado = 'Activo';
    IF @Presupuestado = 0 AND EXISTS (SELECT 1 FROM @d) THROW 50422, N'Una solicitud no presupuestada no descuenta de partidas: el pago va sin distribución.', 1;
    IF EXISTS (SELECT 1 FROM @d WHERE PartidaId IS NULL) THROW 50422, N'Las partidas de la distribución deben existir y estar activas.', 1;
    IF EXISTS (SELECT 1 FROM @d WHERE Monto IS NULL OR Monto <= 0) THROW 50422, N'Cada monto de la distribución debe ser mayor que 0.', 1;
    IF EXISTS (SELECT 1 FROM @d GROUP BY PartidaId HAVING COUNT(*) > 1) THROW 50422, N'Una partida aparece dos veces en la distribución.', 1;
    IF ISNULL((SELECT SUM(Monto) FROM @d), 0) > @Monto THROW 50422, N'La distribución supera el monto del pago.', 1;

    IF @PagoId IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT com.Pago (Token, SolicitudId, Proveedor, Fecha, Monto, RegistradoPorId) VALUES (@TokenGuardado, @SolicitudId, @Proveedor, @Fecha, @Monto, @ActorId);
        SET @PagoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE com.Pago SET Proveedor = @Proveedor, Fecha = @Fecha, Monto = @Monto WHERE PagoId = @PagoId;
        DELETE com.PagoDistribucion WHERE PagoId = @PagoId;
        SET @TokenGuardado = @Token;
    END;
    INSERT com.PagoDistribucion (PagoId, PartidaId, Monto) SELECT @PagoId, PartidaId, Monto FROM @d;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Token IS NULL THEN N'Nuevo pago ' ELSE N'Edición del pago ' END + Codigo,
           CONCAT(Monto, N' · ', (SELECT COUNT(*) FROM @d), N' partida(s)'), @Ip
    FROM com.Pago WHERE PagoId = @PagoId;
    COMMIT;
END;

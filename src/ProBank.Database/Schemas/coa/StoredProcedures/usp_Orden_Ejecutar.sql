-- COA-01: ejecuta una orden de levantamiento o transferencia sobre los bloqueos vigentes de la coactiva.
-- @Montos: JSON [{"bloqueoId":n,"monto":x}]. Cada monto no supera lo vigente del bloqueo; el total no supera lo que
-- queda por ejecutar de la orden (si tiene monto). La transferencia exige la cuenta de la entidad y descuenta el saldo.
CREATE PROCEDURE coa.usp_Orden_Ejecutar
    @OrdenId INT, @Fecha DATE, @Montos NVARCHAR(MAX), @FormaPago VARCHAR(40) = NULL, @CuentaDestino NVARCHAR(150) = NULL,
    @Comprobante NVARCHAR(60) = NULL, @Observacion NVARCHAR(500) = NULL, @Hoy DATE,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Total DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @Tipo VARCHAR(20), @CoactivaId INT, @Ordenado DECIMAL(18,2), @Ejecutado DECIMAL(18,2), @Estado VARCHAR(20), @Oficio VARCHAR(30), @OfAnulado BIT, @Proceso VARCHAR(50);
    SELECT @Tipo = o.Tipo, @CoactivaId = o.CoactivaId, @Ordenado = o.MontoOrdenado, @Ejecutado = o.Ejecutado, @Estado = o.Estado,
           @Oficio = f.Numero, @OfAnulado = f.Anulado, @Proceso = c.NumeroProceso
    FROM coa.vw_Orden o JOIN coa.Oficio f ON f.OficioId = o.OficioId JOIN coa.Coactiva c WITH (UPDLOCK, HOLDLOCK) ON c.CoactivaId = o.CoactivaId
    WHERE o.OrdenId = @OrdenId;
    IF @Tipo IS NULL THROW 50404, N'La orden no existe.', 1;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', CASE @Tipo WHEN 'Levantamiento' THEN 'levantar' ELSE 'transferir' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para ejecutar esta orden.', 1;
    IF @Tipo = 'Bloqueo' THROW 50422, N'Los bloqueos se registran desde la coactiva.', 1;
    IF @OfAnulado = 1 OR @Estado = 'Anulado' THROW 50422, N'El oficio o la coactiva están anulados.', 1;
    IF @Fecha > @Hoy THROW 50422, N'La fecha no puede ser posterior a hoy.', 1;
    IF @Tipo = 'Transferencia' AND NULLIF(LTRIM(@CuentaDestino), N'') IS NULL THROW 50422, N'Indica la cuenta de la entidad.', 1;

    DECLARE @m TABLE (BloqueoId INT PRIMARY KEY, Monto DECIMAL(18,2));
    INSERT @m SELECT BloqueoId, Monto FROM OPENJSON(@Montos) WITH (BloqueoId INT '$.bloqueoId', Monto DECIMAL(18,2) '$.monto') WHERE Monto > 0;
    SET @Total = ISNULL((SELECT SUM(Monto) FROM @m), 0);
    IF @Total <= 0 THROW 50422, N'Indica el monto en al menos una cuenta.', 1;
    IF EXISTS (SELECT 1 FROM @m m LEFT JOIN coa.vw_Bloqueo b ON b.BloqueoId = m.BloqueoId AND b.CoactivaId = @CoactivaId
               WHERE b.BloqueoId IS NULL OR m.Monto > b.Vigente)
        THROW 50422, N'Algún monto supera lo bloqueado vigente o no corresponde a la coactiva.', 1;
    IF @Ordenado IS NOT NULL AND @Ejecutado + @Total > @Ordenado THROW 50422, N'El total supera lo que queda por ejecutar de la orden.', 1;

    INSERT coa.Movimiento (BloqueoId, OrdenId, Tipo, Monto, Fecha, FormaPago, CuentaDestino, Comprobante, Detalle, UsuarioId)
    SELECT BloqueoId, @OrdenId, @Tipo, Monto, @Fecha,
           CASE WHEN @Tipo = 'Transferencia' THEN @FormaPago END, CASE WHEN @Tipo = 'Transferencia' THEN @CuentaDestino END,
           CASE WHEN @Tipo = 'Transferencia' THEN NULLIF(@Comprobante, N'') END, NULLIF(@Observacion, N''), @ActorId
    FROM @m;
    -- Los fondos transferidos salen de la cuenta del cliente
    IF @Tipo = 'Transferencia'
        UPDATE p SET SaldoReferencial = CASE WHEN p.SaldoReferencial > x.Monto THEN p.SaldoReferencial - x.Monto ELSE 0 END
        FROM coa.Producto p JOIN (SELECT b.ProductoId, SUM(m.Monto) AS Monto FROM @m m JOIN coa.Bloqueo b ON b.BloqueoId = m.BloqueoId GROUP BY b.ProductoId) x
             ON x.ProductoId = p.ProductoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas',
            CASE @Tipo WHEN 'Levantamiento' THEN N'Levantamiento de bloqueo' ELSE N'Transferencia de fondos' END,
            CONCAT(@Total, N' · ', @Proceso, N' · oficio ', @Oficio), @Ip);
    COMMIT;
END;

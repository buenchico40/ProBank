-- Datos del reporte de presupuesto de un área y un año (PDF): el área, sus partidas con la ejecución, los servicios
-- (renovaciones) y los pagos del año. Los servicios y pagos no presupuestados no tienen partida (ni área): se incluyen los
-- del año en el reporte de cada área. Exige exportar el presupuesto y deja la generación en la auditoría.
CREATE PROCEDURE com.usp_Presupuesto_Reporte
    @Area VARCHAR(30), @Anio SMALLINT, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'presupuesto', 'exportar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para exportar el presupuesto.', 1;
    IF NOT EXISTS (SELECT 1 FROM com.Area WHERE Codigo = @Area) THROW 50404, N'El área no existe.', 1;

    DECLARE @Partidas TABLE (PartidaId INT PRIMARY KEY);
    INSERT @Partidas SELECT PartidaId FROM com.Partida WHERE Area = @Area AND Anio = @Anio;

    -- 1. Área
    SELECT Codigo, Nombre FROM com.Area WHERE Codigo = @Area;

    -- 2. Partidas con su ejecución
    EXEC com.usp_Partida_Listar @Anio = @Anio, @Area = @Area;

    -- 3. Servicios: los de una partida del área y el año, y los no presupuestados que vencen en el año. Pagado: pagos
    --    realizados en el año de las solicitudes que nacieron de su requerimiento.
    SELECT r.Codigo, r.Servicio, r.Proveedor, r.Vence, r.MontoEstimado, r.Presupuestado, pa.Codigo AS Partida, pa.Rubro AS PartidaRubro,
           r.Estado, r.DiasAviso,
           ISNULL((SELECT SUM(pg.Monto) FROM com.Pago pg JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId
                   WHERE s.RenovacionId = r.RenovacionId AND pg.Estado = 'Pagado' AND YEAR(pg.Fecha) = @Anio), 0) AS Pagado,
           (SELECT TOP (1) s.Codigo FROM com.Solicitud s WHERE s.RenovacionId = r.RenovacionId AND s.Anulada = 0 ORDER BY s.SolicitudId DESC) AS Solicitud
    FROM com.Renovacion r
    LEFT JOIN com.Partida pa ON pa.PartidaId = r.PartidaId
    WHERE r.PartidaId IN (SELECT PartidaId FROM @Partidas) OR (r.PartidaId IS NULL AND YEAR(r.Vence) = @Anio)
    ORDER BY r.Vence, r.Codigo;

    -- 4. Pagos del año (sin los anulados) que tocan una partida del área o no son presupuestados. Monto: lo que corresponde
    --    al área (su distribución; si aún no se distribuye, el monto del pago).
    SELECT pg.Codigo, pg.Fecha, pg.Proveedor, s.Codigo AS Solicitud, s.Descripcion, r.Servicio, s.Presupuestado,
           ISNULL((SELECT STRING_AGG(pa.Codigo, ', ') WITHIN GROUP (ORDER BY pa.Codigo)
                   FROM com.PagoDistribucion d JOIN com.Partida pa ON pa.PartidaId = d.PartidaId
                   WHERE d.PagoId = pg.PagoId AND d.PartidaId IN (SELECT PartidaId FROM @Partidas)),
                  (SELECT pa.Codigo FROM com.Partida pa WHERE pa.PartidaId = s.PartidaId)) AS Partidas,
           pg.Monto AS MontoPago,
           ISNULL((SELECT SUM(d.Monto) FROM com.PagoDistribucion d WHERE d.PagoId = pg.PagoId AND d.PartidaId IN (SELECT PartidaId FROM @Partidas)), pg.Monto) AS Monto,
           pg.Estado
    FROM com.Pago pg
    JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId
    LEFT JOIN com.Renovacion r ON r.RenovacionId = s.RenovacionId
    WHERE YEAR(pg.Fecha) = @Anio AND pg.Estado <> 'Anulado'
      AND (s.Presupuestado = 0
           OR s.PartidaId IN (SELECT PartidaId FROM @Partidas)
           OR EXISTS (SELECT 1 FROM com.PagoDistribucion d WHERE d.PagoId = pg.PagoId AND d.PartidaId IN (SELECT PartidaId FROM @Partidas)))
    ORDER BY pg.Fecha, pg.Codigo;

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CONCAT(N'Reporte de presupuesto ', @Area, N' ', @Anio), N'PDF de partidas, servicios y pagos', @Ip);
END;

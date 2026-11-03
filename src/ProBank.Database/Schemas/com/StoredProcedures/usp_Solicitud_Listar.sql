-- Solicitudes de compra con lo pagado y su estado (com.vw_Solicitud). Origen: la renovación que la generó o «Requerimiento».
-- Con @Token devuelve solo esa y, en dos resultados más, sus pagos y sus documentos.
CREATE PROCEDURE com.usp_Solicitud_Listar @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.Token, s.Codigo, s.Descripcion, s.Area, s.Proveedor, s.Presupuestado, p.Token AS PartidaToken, p.Codigo AS Partida, p.Rubro AS PartidaRubro,
           s.Monto, s.Pagado, s.PorPagar, ISNULL(r.Codigo, 'Requerimiento') AS Origen, s.Aprobacion, s.Estado, s.Comentario,
           u.Nombre AS Solicitante, s.FechaRegistro, a.Nombre AS AprobadoPor, s.FechaAprobacion,
           (SELECT COUNT(*) FROM com.Documento d WHERE d.SolicitudId = s.SolicitudId) AS Documentos
    FROM com.vw_Solicitud s
    LEFT JOIN com.Partida p ON p.PartidaId = s.PartidaId
    LEFT JOIN com.Renovacion r ON r.RenovacionId = s.RenovacionId
    JOIN seg.Usuario u ON u.UsuarioId = s.SolicitanteId
    LEFT JOIN seg.Usuario a ON a.UsuarioId = s.AprobadoPorId
    WHERE @Token IS NULL OR s.Token = @Token
    ORDER BY s.SolicitudId DESC;

    IF @Token IS NULL RETURN;
    SELECT pg.Token, pg.Codigo, pg.Fecha, pg.Monto, pg.Estado
    FROM com.Pago pg JOIN com.Solicitud s ON s.SolicitudId = pg.SolicitudId WHERE s.Token = @Token ORDER BY pg.PagoId DESC;

    EXEC com.usp_Documento_Listar 'Solicitud', @Token;
END;

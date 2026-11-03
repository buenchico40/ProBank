-- Ajustes y movimientos con sus partidas (y el disponible actual de cada una).
CREATE PROCEDURE com.usp_Movimiento_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Token, m.Codigo, m.Fecha, m.Tipo, m.Monto, m.Motivo, m.Estado, m.Comentario,
           o.Token AS OrigenToken, o.Codigo AS Origen, o.Rubro AS OrigenRubro, o.Disponible AS OrigenDisponible,
           d.Token AS DestinoToken, d.Codigo AS Destino, d.Rubro AS DestinoRubro, d.Disponible AS DestinoDisponible,
           r.Nombre AS RegistradoPor, m.FechaRegistro, x.Nombre AS ResueltoPor, m.FechaResolucion
    FROM com.Movimiento m
    LEFT JOIN com.vw_Partida o ON o.PartidaId = m.PartidaOrigenId
    LEFT JOIN com.vw_Partida d ON d.PartidaId = m.PartidaDestinoId
    JOIN seg.Usuario r ON r.UsuarioId = m.RegistradoPorId
    LEFT JOIN seg.Usuario x ON x.UsuarioId = m.ResueltoPorId
    ORDER BY m.MovimientoId DESC;
END;

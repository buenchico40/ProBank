-- COM-03: renovaciones con los días para vencer y su estado: Retirado, Requerimiento enviado, Vencido, Por vencer (dentro
-- de los días de aviso del servicio) o Vigente. Incluye la última solicitud de compra que generó.
CREATE PROCEDURE com.usp_Renovacion_Listar @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.Token, r.Codigo, r.Servicio, r.Proveedor, r.Vence, DATEDIFF(DAY, @Hoy, r.Vence) AS DiasParaVencer, r.MontoEstimado,
           r.Presupuestado, p.Token AS PartidaToken, p.Codigo AS Partida, p.Rubro AS PartidaRubro, r.DiasAviso,
           CASE WHEN r.Estado <> 'Activo' THEN r.Estado
                WHEN r.Vence < @Hoy THEN 'Vencido'
                WHEN DATEDIFF(DAY, @Hoy, r.Vence) <= r.DiasAviso THEN 'Por vencer'
                ELSE 'Vigente' END AS Estado,
           s.Codigo AS Solicitud, s.Estado AS SolicitudEstado
    FROM com.Renovacion r
    LEFT JOIN com.Partida p ON p.PartidaId = r.PartidaId
    OUTER APPLY (SELECT TOP (1) v.Codigo, v.Estado FROM com.vw_Solicitud v WHERE v.RenovacionId = r.RenovacionId ORDER BY v.SolicitudId DESC) s
    ORDER BY r.Vence, r.RenovacionId;
END;

-- SQL-06: auditoría de ejecuciones de un rango de fechas: cada solicitud, aprobación, rechazo, ejecución (exitosa o fallida),
-- exportación y envío por correo, con parámetros resueltos, filas, duración e IP.
CREATE PROCEDURE rpt.usp_EjecucionEvento_Listar @Desde DATE, @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ev.Fecha, ISNULL(u.Nombre, N'Programador de consultas') AS Usuario, ev.Evento, e.Codigo AS Ejecucion,
           q.Codigo + N' · ' + q.Nombre AS Consulta, c.Nombre AS Base, ev.Detalle, ev.Filas, ev.DuracionMs, ev.Resultado, ev.Ip
    FROM rpt.EjecucionEvento ev
    JOIN rpt.Ejecucion e ON e.EjecucionId = ev.EjecucionId
    JOIN rpt.Consulta q ON q.ConsultaId = e.ConsultaId
    JOIN rpt.Conexion c ON c.ConexionId = e.ConexionId
    LEFT JOIN seg.Usuario u ON u.UsuarioId = ev.UsuarioId
    WHERE ev.Fecha >= @Desde AND ev.Fecha < DATEADD(DAY, 1, @Hasta)
    ORDER BY ev.EventoId DESC;
END;

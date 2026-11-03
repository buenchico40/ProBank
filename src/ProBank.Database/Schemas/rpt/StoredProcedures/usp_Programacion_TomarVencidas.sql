-- SQL-07: toma las programaciones activas vencidas de consultas publicadas. Las reserva (próxima ejecución en una hora y
-- «En curso») en la misma sentencia, para que dos instancias de la API no ejecuten la misma; MarcarEjecutada fija la real.
CREATE PROCEDURE rpt.usp_Programacion_TomarVencidas @Ahora DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Tomadas TABLE (ProgramacionId INT PRIMARY KEY);
    UPDATE p SET ProximaEjecucion = DATEADD(HOUR, 1, @Ahora), UltimoResultado = 'En curso'
    OUTPUT inserted.ProgramacionId INTO @Tomadas
    FROM rpt.Programacion p WITH (UPDLOCK, READPAST)
    JOIN rpt.Consulta c ON c.ConsultaId = p.ConsultaId AND c.Estado = 'Publicada'
    WHERE p.Estado = 'Activa' AND p.ProximaEjecucion <= @Ahora;

    SELECT p.ProgramacionId, p.Codigo, c.Codigo AS ConsultaCodigo, cx.Codigo AS ConexionCodigo, p.Frecuencia, CAST(p.Hora AS TIME(0)) AS Hora,
           p.Dias, CAST(p.DiaDelMes AS INT) AS DiaDelMes, p.Formato,
           ISNULL((SELECT STRING_AGG(d.Correo, ',') FROM rpt.ProgramacionDestinatario d WHERE d.ProgramacionId = p.ProgramacionId), '') AS Destinatarios,
           p.ParametrosJson, p.ResponsableId
    FROM @Tomadas t
    JOIN rpt.Programacion p ON p.ProgramacionId = t.ProgramacionId
    JOIN rpt.Consulta c ON c.ConsultaId = p.ConsultaId
    JOIN rpt.Conexion cx ON cx.ConexionId = p.ConexionId;
END;

-- SQL-07: toma las programaciones activas vencidas y las bloquea para que dos instancias no las ejecuten a la vez.
CREATE PROCEDURE rpt.usp_Programacion_TomarVencidas @Ahora DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProgramacionId, c.Codigo AS ConsultaCodigo, p.ConexionId, p.Frecuencia, CAST(p.Hora AS TIME(0)) AS Hora, p.Dias,
           CAST(p.DiaDelMes AS INT) AS DiaDelMes, p.Formato,
           (SELECT STRING_AGG(d.Correo, ',') FROM rpt.ProgramacionDestinatario d WHERE d.ProgramacionId = p.ProgramacionId) AS Destinatarios,
           p.ParametrosJson, p.ResponsableId
    FROM rpt.Programacion p WITH (UPDLOCK, READPAST)
    JOIN rpt.Consulta c ON c.ConsultaId = p.ConsultaId AND c.Estado = 'Publicada'
    WHERE p.Estado = 'Activa' AND p.ProximaEjecucion <= @Ahora;
END;

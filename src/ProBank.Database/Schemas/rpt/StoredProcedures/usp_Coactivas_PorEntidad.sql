-- Reporte para Consultas dinámicas: coactivas por entidad con su monto, lo bloqueado vigente y lo transferido.
-- @Entidad y @Estado opcionales ('Todas' / 'Todos' o NULL = sin filtro).
CREATE PROCEDURE rpt.usp_Coactivas_PorEntidad @Entidad NVARCHAR(150) = NULL, @Estado VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.NumeroProceso AS Proceso, e.Nombre AS Entidad, cl.Identificacion, cl.Nombre AS Cliente, v.Monto, v.Bloqueado AS BloqueadoVigente,
           v.Transferido, v.Cobertura, v.Estado
    FROM coa.vw_Coactiva v
    JOIN coa.Entidad e ON e.EntidadId = v.EntidadId
    JOIN coa.Cliente cl ON cl.ClienteId = v.ClienteId
    WHERE (NULLIF(@Entidad, N'Todas') IS NULL OR e.Nombre = @Entidad)
      AND (NULLIF(@Estado, 'Todos') IS NULL OR v.Estado = @Estado)
    ORDER BY e.Nombre, v.NumeroProceso;
END;

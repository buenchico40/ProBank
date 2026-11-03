-- CFG-04: resultados guardados de ejecuciones con más de @Dias días (Configuración › Consultas dinámicas). De a 200 por vez.
CREATE PROCEDURE rpt.usp_Ejecucion_ResultadosVencidos @Dias INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (200) EjecucionId, Codigo, RutaResultado
    FROM rpt.Ejecucion
    WHERE RutaResultado IS NOT NULL AND Fecha < DATEADD(DAY, -@Dias, SYSDATETIME())
    ORDER BY EjecucionId;
END;

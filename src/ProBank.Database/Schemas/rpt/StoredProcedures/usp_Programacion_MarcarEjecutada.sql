-- Deja el resultado de la corrida y la próxima ejecución calculada por el programador.
CREATE PROCEDURE rpt.usp_Programacion_MarcarEjecutada @ProgramacionId INT, @Proxima DATETIME2(0), @Resultado VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE rpt.Programacion SET ProximaEjecucion = @Proxima, UltimaEjecucion = SYSDATETIME(), UltimoResultado = LEFT(@Resultado, 100)
    WHERE ProgramacionId = @ProgramacionId;
END;

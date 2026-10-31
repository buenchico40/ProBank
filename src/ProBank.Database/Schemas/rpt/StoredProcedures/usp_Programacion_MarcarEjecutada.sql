CREATE PROCEDURE rpt.usp_Programacion_MarcarEjecutada @ProgramacionId INT, @Proxima DATETIME2(0), @Resultado VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE rpt.Programacion SET ProximaEjecucion = @Proxima, UltimoResultado = @Resultado WHERE ProgramacionId = @ProgramacionId;
END;

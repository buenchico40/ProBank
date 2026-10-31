CREATE PROCEDURE rpt.usp_Ejecucion_Cerrar @EjecucionId BIGINT, @Filas INT, @DuracionMs INT, @Estado VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE rpt.Ejecucion SET Filas = @Filas, DuracionMs = @DuracionMs, Estado = @Estado WHERE EjecucionId = @EjecucionId;
END;

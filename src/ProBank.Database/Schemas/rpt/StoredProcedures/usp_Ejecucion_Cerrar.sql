-- Cierra una ejecución: Ejecutada (con filas, duración y dónde quedó el resultado) o Fallida (con el motivo).
CREATE PROCEDURE rpt.usp_Ejecucion_Cerrar
    @EjecucionId BIGINT, @Filas INT = NULL, @DuracionMs INT = NULL, @Truncado BIT = 0, @RutaResultado NVARCHAR(400) = NULL,
    @Mensaje NVARCHAR(500) = NULL, @Estado VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE rpt.Ejecucion SET Filas = @Filas, DuracionMs = @DuracionMs, Truncado = @Truncado, RutaResultado = @RutaResultado,
           Mensaje = @Mensaje, Estado = @Estado
    WHERE EjecucionId = @EjecucionId;
END;

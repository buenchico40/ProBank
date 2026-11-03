-- CFG-04: el resultado guardado ya se borró del almacén: la ejecución queda sin resultado y el evento en su bitácora.
CREATE PROCEDURE rpt.usp_Ejecucion_QuitarResultado @EjecucionId BIGINT, @Dias INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    UPDATE rpt.Ejecucion SET RutaResultado = NULL WHERE EjecucionId = @EjecucionId AND RutaResultado IS NOT NULL;
    IF @@ROWCOUNT > 0
        INSERT rpt.EjecucionEvento (EjecucionId, UsuarioId, Evento, Detalle, Resultado)
        VALUES (@EjecucionId, NULL, 'Resultado eliminado', CONCAT(N'Conservación de resultados: ', @Dias, N' días'), 'Exitosa');
    COMMIT;
END;

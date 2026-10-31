-- SQL-06: bitácora inmutable de cada evento (solicitud, aprobación, rechazo, ejecución, exportación, envío por correo).
CREATE PROCEDURE rpt.usp_EjecucionEvento_Registrar
    @EjecucionId BIGINT, @UsuarioId INT, @Evento VARCHAR(30), @Detalle NVARCHAR(2000), @Resultado VARCHAR(30),
    @Filas INT = NULL, @DuracionMs INT = NULL, @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT rpt.EjecucionEvento (EjecucionId, UsuarioId, Evento, Detalle, Filas, DuracionMs, Resultado, Ip)
    VALUES (@EjecucionId, NULLIF(@UsuarioId, 0), @Evento, @Detalle, @Filas, @DuracionMs, @Resultado, @Ip);
END;

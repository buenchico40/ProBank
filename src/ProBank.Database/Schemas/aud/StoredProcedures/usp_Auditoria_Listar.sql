-- Seguridad › Auditoría (SEG-05): solo lectura, por rango de fechas; como máximo 5 000 filas, las más recientes primero.
CREATE PROCEDURE aud.usp_Auditoria_Listar
    @Desde DATE, @Hasta DATE, @Modulo VARCHAR(60) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (5000) a.AuditoriaId, a.Fecha, ISNULL(u.Usuario, N'—') AS Usuario, u.Nombre, a.RolActivo, a.Modulo, a.Accion, a.Detalle, a.Ip
    FROM aud.Auditoria a
    LEFT JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE a.Fecha >= @Desde AND a.Fecha < DATEADD(DAY, 1, @Hasta) AND (@Modulo IS NULL OR a.Modulo = @Modulo)
    ORDER BY a.AuditoriaId DESC;
END;

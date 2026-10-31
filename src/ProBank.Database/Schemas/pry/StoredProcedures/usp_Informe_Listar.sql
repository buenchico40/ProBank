CREATE PROCEDURE pry.usp_Informe_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.InformeId, f.Codigo, f.Tipo, f.Desde, f.Hasta, f.Incluye, f.Estado, f.FechaGenerado, g.Nombre AS GeneradoPor
    FROM pry.Informe f LEFT JOIN seg.Usuario g ON g.UsuarioId = f.GeneradoPorId
    ORDER BY f.Desde DESC, f.InformeId DESC;
END;

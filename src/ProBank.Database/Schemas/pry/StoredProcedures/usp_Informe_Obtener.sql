CREATE PROCEDURE pry.usp_Informe_Obtener @InformeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT InformeId, Codigo, Tipo, Desde, Hasta, Incluye, Estado, CAST(CASE WHEN RutaPdf IS NULL THEN 0 ELSE 1 END AS BIT) AS TienePdf FROM pry.Informe WHERE InformeId = @InformeId;
END;

CREATE PROCEDURE pry.usp_Informe_Obtener @InformeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT InformeId, Codigo, Tipo, Desde, Hasta, Incluye, Estado, ContenidoHtml FROM pry.Informe WHERE InformeId = @InformeId;
END;

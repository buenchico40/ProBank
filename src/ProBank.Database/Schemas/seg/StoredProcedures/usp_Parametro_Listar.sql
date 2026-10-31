CREATE PROCEDURE seg.usp_Parametro_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Clave, Nombre, TRY_CAST(Valor AS INT) AS Valor, Minimo, Maximo FROM seg.ParametroAcceso ORDER BY Orden;
END;

-- Un documento adjunto (para descargarlo) con su ámbito y la ruta de su archivo en Blob Storage.
CREATE PROCEDURE pry.usp_Adjunto_Obtener @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE WHEN ItemId IS NOT NULL THEN 'Item' ELSE 'Tarea' END AS Ambito, Nombre, TipoContenido, Ruta
    FROM pry.Adjunto WHERE Token = @Token;
END;

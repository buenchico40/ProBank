-- Un documento para descargarlo: de qué es (para el permiso de ver) y la ruta de su archivo.
CREATE PROCEDURE com.usp_Documento_Obtener @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE WHEN SolicitudId IS NOT NULL THEN 'Solicitud' ELSE 'Pago' END AS Dueno, Nombre, TipoContenido, Ruta
    FROM com.Documento WHERE Token = @Token;
END;

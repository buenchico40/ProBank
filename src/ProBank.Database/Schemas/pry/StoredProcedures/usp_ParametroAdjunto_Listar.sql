-- Tipos de archivo y tamaño máximo de los adjuntos de ítems y de tareas.
CREATE PROCEDURE pry.usp_ParametroAdjunto_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Ambito, Extensiones, MaximoMB FROM pry.ParametroAdjunto ORDER BY Ambito;
END;

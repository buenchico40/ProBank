-- Áreas del presupuesto con cuántas partidas tienen; @SoloActivas para los selectores.
CREATE PROCEDURE com.usp_Area_Listar @SoloActivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.Codigo, a.Nombre, a.Activa, (SELECT COUNT(*) FROM com.Partida p WHERE p.Area = a.Codigo) AS Partidas
    FROM com.Area a
    WHERE @SoloActivas = 0 OR a.Activa = 1
    ORDER BY a.Nombre;
END;

-- Audiencias para promociones y comunicados: «Todos» y los roles activos.
CREATE PROCEDURE mkt.usp_Audiencia_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT N'Todos' AS Nombre UNION ALL SELECT Nombre FROM seg.Rol WHERE Estado = 'Activo';
END;

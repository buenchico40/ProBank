-- Partidas con su ejecución (com.vw_Partida), de un año o de todos, y de un área o de todas.
CREATE PROCEDURE com.usp_Partida_Listar @Anio SMALLINT = NULL, @Area VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.Token, v.Codigo, CAST(v.Anio AS INT) AS Anio, v.Area, v.Rubro, v.Asignado, v.Ajustes, v.Comprometido, v.Pagado, v.Disponible, v.Ejecucion, v.Estado,
           c.Nombre AS CreadoPor, v.FechaRegistro, a.Nombre AS AprobadoPor, v.FechaAprobacion
    FROM com.vw_Partida v
    JOIN seg.Usuario c ON c.UsuarioId = v.CreadoPorId
    LEFT JOIN seg.Usuario a ON a.UsuarioId = v.AprobadoPorId
    WHERE (@Anio IS NULL OR v.Anio = @Anio) AND (@Area IS NULL OR v.Area = @Area)
    ORDER BY v.Anio DESC, v.Codigo;
END;

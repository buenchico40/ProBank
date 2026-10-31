-- PRY-07: cronograma de todos los ítems abiertos o de uno (@ItemToken): ítems, hitos y tareas con su periodo y avance.
CREATE PROCEDURE pry.usp_Cronograma_Listar @ItemToken UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @i TABLE (ItemId INT PRIMARY KEY);
    INSERT @i SELECT ItemId FROM pry.Item WHERE (@ItemToken IS NULL AND Estado NOT IN ('Anulado', 'Borrador')) OR Token = @ItemToken;

    SELECT i.ItemId, i.Token, i.Codigo, i.Titulo, i.Tipo, i.Estado FROM pry.Item i JOIN @i x ON x.ItemId = i.ItemId ORDER BY i.ItemId;
    SELECT h.HitoId, h.ItemId, h.Nombre, h.Orden, h.FechaInicio, h.FechaFin FROM pry.Hito h JOIN @i x ON x.ItemId = h.ItemId ORDER BY h.ItemId, h.Orden;
    SELECT t.Token, t.Codigo, t.Titulo, t.ItemId, i.Codigo AS Item, t.HitoId, t.Area, u.Nombre AS Tecnico, t.FechaInicio, t.FechaTerminacion,
           CASE WHEN t.Estado = 'Completada' THEN 100 ELSE t.Avance END AS Avance, t.Estado
    FROM pry.Tarea t JOIN @i x ON x.ItemId = t.ItemId JOIN pry.Item i ON i.ItemId = t.ItemId JOIN seg.Usuario u ON u.UsuarioId = t.TecnicoId
    ORDER BY t.FechaInicio, t.FechaTerminacion;
END;

-- Tareas de un ítem, de un técnico («Mis tareas») o todas, con su circuito de aprobación vigente.
CREATE PROCEDURE pry.usp_Tarea_Listar @ItemToken UNIQUEIDENTIFIER = NULL, @TecnicoId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.Token, t.Codigo, t.Titulo, i.Token AS ItemToken, i.Codigo AS Item, t.HitoId, h.Nombre AS Hito, t.Area, t.TecnicoId, u.Nombre AS Tecnico,
           t.Orden, CAST(t.FechaInicio AS DATETIME2) AS FechaInicio, CAST(t.FechaTerminacion AS DATETIME2) AS FechaTerminacion,
           CASE WHEN t.Estado = 'Completada' THEN 100 ELSE t.Avance END AS Avance, t.Estado,
           (SELECT STRING_AGG(a.Nombre, ', ') FROM pry.TareaAprobador ta JOIN seg.Usuario a ON a.UsuarioId = ta.UsuarioId WHERE ta.TareaId = t.TareaId) AS Aprobadores,
           (SELECT STRING_AGG(CAST(ta.UsuarioId AS VARCHAR(10)), ',') FROM pry.TareaAprobador ta WHERE ta.TareaId = t.TareaId) AS AprobadorIds,
           CASE WHEN NOT EXISTS (SELECT 1 FROM pry.TareaAprobador ta WHERE ta.TareaId = t.TareaId) THEN 'No requiere'
                ELSE CONCAT((SELECT COUNT(*) FROM pry.Aprobacion ap WHERE ap.TareaId = t.TareaId AND ap.Decision = 'Aprobada'
                               AND ap.Ronda = (SELECT MAX(Ronda) FROM pry.Aprobacion x WHERE x.TareaId = t.TareaId)),
                            ' de ', (SELECT COUNT(*) FROM pry.TareaAprobador ta WHERE ta.TareaId = t.TareaId)) END AS AvanceAprobacion
    FROM pry.Tarea t
    JOIN pry.Item i ON i.ItemId = t.ItemId
    JOIN seg.Usuario u ON u.UsuarioId = t.TecnicoId
    LEFT JOIN pry.Hito h ON h.HitoId = t.HitoId
    WHERE (@ItemToken IS NULL OR i.Token = @ItemToken) AND (@TecnicoId IS NULL OR t.TecnicoId = @TecnicoId)
    ORDER BY i.ItemId DESC, t.Orden;
END;

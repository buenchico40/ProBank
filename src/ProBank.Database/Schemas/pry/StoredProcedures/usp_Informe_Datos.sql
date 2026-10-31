-- Bitácora y acuerdos de un periodo para el informe de seguimiento.
-- 1) resumen por ítem  2) bitácora de tareas  3) acuerdos (estado al cierre del periodo)  4) temas de las revisiones.
CREATE PROCEDURE pry.usp_Informe_Datos @Desde DATE, @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Fin DATETIME2(0) = DATEADD(DAY, 1, CAST(@Hasta AS DATETIME2(0)));
    SELECT i.Codigo AS Item, i.Titulo, i.Tipo, i.Estado,
           x.Tareas, x.Completadas,
           CASE WHEN x.Tareas = 0 THEN 0 ELSE CAST(ROUND(100.0 * x.Completadas / x.Tareas, 0) AS INT) END AS Avance,
           (SELECT COUNT(DISTINCT e.TareaId) FROM pry.TareaEvento e JOIN pry.Tarea t ON t.TareaId = e.TareaId
            WHERE t.ItemId = i.ItemId AND e.Fecha >= @Desde AND e.Fecha < @Fin) AS TareasConMovimiento,
           (SELECT COUNT(*) FROM pry.Acuerdo a JOIN pry.Revision r ON r.RevisionId = a.RevisionId WHERE a.ItemId = i.ItemId AND r.Fecha BETWEEN @Desde AND @Hasta) AS Acuerdos,
           (SELECT COUNT(*) FROM pry.Acuerdo a WHERE a.ItemId = i.ItemId AND a.FechaCumplido BETWEEN @Desde AND @Hasta) AS Cumplidos,
           (SELECT COUNT(*) FROM pry.Acuerdo a WHERE a.ItemId = i.ItemId AND (a.Estado = 'Pendiente' OR a.FechaCumplido > @Hasta) AND a.FechaCumplimiento < @Hasta) AS Vencidos
    FROM pry.Item i
    CROSS APPLY (SELECT COUNT(*) AS Tareas, ISNULL(SUM(CASE WHEN t.Estado = 'Completada' THEN 1 ELSE 0 END), 0) AS Completadas FROM pry.Tarea t WHERE t.ItemId = i.ItemId) x
    WHERE i.Estado NOT IN ('Borrador', 'Anulado')
      AND (EXISTS (SELECT 1 FROM pry.Tarea t JOIN pry.TareaEvento e ON e.TareaId = t.TareaId WHERE t.ItemId = i.ItemId AND e.Fecha >= @Desde AND e.Fecha < @Fin)
           OR EXISTS (SELECT 1 FROM pry.Revision r WHERE r.ItemId = i.ItemId AND r.Fecha BETWEEN @Desde AND @Hasta)
           OR EXISTS (SELECT 1 FROM pry.Acuerdo a WHERE a.ItemId = i.ItemId AND (a.Estado = 'Pendiente' OR a.FechaCumplido >= @Desde) AND a.FechaCumplimiento <= @Hasta))
    ORDER BY i.Codigo;

    SELECT e.Fecha, i.Codigo AS Item, t.Codigo AS Tarea, t.Titulo, e.Evento, e.Detalle, u.Nombre AS Usuario
    FROM pry.TareaEvento e JOIN pry.Tarea t ON t.TareaId = e.TareaId JOIN pry.Item i ON i.ItemId = t.ItemId JOIN seg.Usuario u ON u.UsuarioId = e.UsuarioId
    WHERE e.Fecha >= @Desde AND e.Fecha < @Fin
    ORDER BY i.Codigo, e.Fecha;

    SELECT r.Fecha AS FechaRevision, i.Codigo AS Item, a.Codigo, a.Descripcion, u.Nombre AS Responsable, a.FechaCumplimiento,
           CASE WHEN a.Estado = 'Cumplido' AND a.FechaCumplido <= @Hasta THEN 'Cumplido' WHEN a.FechaCumplimiento < @Hasta THEN 'Vencido' ELSE 'Pendiente' END AS Estado
    FROM pry.Acuerdo a JOIN pry.Revision r ON r.RevisionId = a.RevisionId JOIN pry.Item i ON i.ItemId = a.ItemId JOIN seg.Usuario u ON u.UsuarioId = a.ResponsableId
    WHERE r.Fecha BETWEEN @Desde AND @Hasta
       OR (r.Fecha < @Desde AND (a.Estado = 'Pendiente' OR a.FechaCumplido >= @Desde))
    ORDER BY i.Codigo, r.Fecha, a.AcuerdoId;

    SELECT r.Fecha, i.Codigo AS Item, r.Temas FROM pry.Revision r JOIN pry.Item i ON i.ItemId = r.ItemId
    WHERE r.Fecha BETWEEN @Desde AND @Hasta AND r.Temas IS NOT NULL ORDER BY i.Codigo, r.Fecha;
END;

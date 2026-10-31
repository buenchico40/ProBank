-- Proyectos, mejoras y bugs con responsables y PRY-06: avance = tareas completadas ÷ total de tareas.
CREATE PROCEDURE pry.usp_Item_Listar @SoloAbiertos BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.Token, i.Codigo, i.Titulo, i.Tipo, i.Estado,
           (SELECT STRING_AGG(u.Nombre, ', ') WITHIN GROUP (ORDER BY u.Nombre) FROM pry.ItemResponsable r JOIN seg.Usuario u ON u.UsuarioId = r.UsuarioId WHERE r.ItemId = i.ItemId) AS Responsables,
           (SELECT STRING_AGG(CAST(r.UsuarioId AS VARCHAR(10)), ',') FROM pry.ItemResponsable r WHERE r.ItemId = i.ItemId) AS ResponsableIds,
           x.Tareas, x.Completadas,
           CASE WHEN x.Tareas = 0 THEN 0 ELSE CAST(ROUND(100.0 * x.Completadas / x.Tareas, 0) AS INT) END AS Avance
    FROM pry.Item i
    CROSS APPLY (SELECT COUNT(*) AS Tareas, ISNULL(SUM(CASE WHEN t.Estado = 'Completada' THEN 1 ELSE 0 END), 0) AS Completadas
                 FROM pry.Tarea t WHERE t.ItemId = i.ItemId) x
    WHERE @SoloAbiertos = 0 OR i.Estado NOT IN ('Anulado','Completado')
    ORDER BY i.ItemId DESC;
END;

-- Ficha del ítem: datos, responsables, hitos y acuerdos (las tareas salen de usp_Tarea_Listar).
CREATE PROCEDURE pry.usp_Item_Obtener @Token UNIQUEIDENTIFIER, @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ItemId INT = (SELECT ItemId FROM pry.Item WHERE Token = @Token);
    SELECT i.Token, i.Codigo, i.Titulo, i.Tipo, i.Estado, i.DescripcionHtml, i.FechaRegistro, u.Nombre AS CreadoPor
    FROM pry.Item i LEFT JOIN seg.Usuario u ON u.UsuarioId = i.CreadoPorId WHERE i.ItemId = @ItemId;

    SELECT u.UsuarioId, u.Usuario, u.Nombre,
           (SELECT STRING_AGG(r.Nombre, ', ') FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId WHERE ur.UsuarioId = u.UsuarioId) AS Roles
    FROM pry.ItemResponsable ir JOIN seg.Usuario u ON u.UsuarioId = ir.UsuarioId WHERE ir.ItemId = @ItemId ORDER BY u.Nombre;

    SELECT h.HitoId, h.Nombre, h.Orden, h.FechaInicio, h.FechaFin, (SELECT COUNT(*) FROM pry.Tarea t WHERE t.HitoId = h.HitoId) AS Tareas
    FROM pry.Hito h WHERE h.ItemId = @ItemId ORDER BY h.Orden;

    EXEC pry.usp_Acuerdo_Listar @Hoy = @Hoy, @ItemId = @ItemId;
END;

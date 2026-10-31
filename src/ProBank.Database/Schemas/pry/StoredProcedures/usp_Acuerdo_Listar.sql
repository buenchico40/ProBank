-- PRY-08: acuerdos semanales (todos o de un ítem). Un acuerdo pendiente con la fecha de cumplimiento pasada aparece Vencido.
CREATE PROCEDURE pry.usp_Acuerdo_Listar @Hoy DATE, @ItemId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.AcuerdoId, a.Codigo, i.Token AS ItemToken, i.Codigo AS Item, i.Titulo AS ItemTitulo, r.RevisionId, r.Fecha AS FechaRevision, r.Temas,
           a.Descripcion, a.ResponsableId, u.Nombre AS Responsable, a.FechaCumplimiento, a.FechaCumplido,
           CASE WHEN a.Estado = 'Pendiente' AND a.FechaCumplimiento < @Hoy THEN 'Vencido' ELSE a.Estado END AS Estado
    FROM pry.Acuerdo a
    JOIN pry.Revision r ON r.RevisionId = a.RevisionId
    JOIN pry.Item i ON i.ItemId = a.ItemId
    JOIN seg.Usuario u ON u.UsuarioId = a.ResponsableId
    WHERE @ItemId IS NULL OR a.ItemId = @ItemId
    ORDER BY r.Fecha DESC, a.AcuerdoId;
END;

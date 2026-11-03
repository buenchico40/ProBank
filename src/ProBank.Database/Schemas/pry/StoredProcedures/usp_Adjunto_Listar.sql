-- Documentos adjuntos de un ítem (@Ambito = 'Item') o de una tarea ('Tarea'), del más reciente al más antiguo.
CREATE PROCEDURE pry.usp_Adjunto_Listar @Ambito VARCHAR(10), @DuenoToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.Token, a.Nombre, a.Descripcion, a.TipoContenido, a.Tamano, u.Nombre AS SubidoPor, a.FechaRegistro
    FROM pry.Adjunto a
    JOIN seg.Usuario u ON u.UsuarioId = a.SubidoPorId
    LEFT JOIN pry.Item i ON i.ItemId = a.ItemId
    LEFT JOIN pry.Tarea t ON t.TareaId = a.TareaId
    WHERE (@Ambito = 'Item' AND i.Token = @DuenoToken) OR (@Ambito = 'Tarea' AND t.Token = @DuenoToken)
    ORDER BY a.AdjuntoId DESC;
END;

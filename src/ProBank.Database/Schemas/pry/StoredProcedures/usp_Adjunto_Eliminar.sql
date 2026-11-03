-- Quita un documento adjunto y devuelve la ruta de su archivo para borrarlo de Blob Storage.
-- Ítem: quien edita ítems. Tarea: el técnico asignado o quien edita tareas.
CREATE PROCEDURE pry.usp_Adjunto_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Ruta VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @AdjuntoId INT, @Nombre NVARCHAR(200), @Codigo VARCHAR(20), @EsItem BIT, @TecnicoId INT;
    SELECT @AdjuntoId = a.AdjuntoId, @Nombre = a.Nombre, @Ruta = a.Ruta, @Codigo = ISNULL(i.Codigo, t.Codigo),
           @EsItem = CASE WHEN a.ItemId IS NOT NULL THEN 1 ELSE 0 END, @TecnicoId = t.TecnicoId
    FROM pry.Adjunto a LEFT JOIN pry.Item i ON i.ItemId = a.ItemId LEFT JOIN pry.Tarea t ON t.TareaId = a.TareaId
    WHERE a.Token = @Token;
    IF @AdjuntoId IS NULL THROW 50404, N'El documento no existe.', 1;
    IF @EsItem = 1 AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para quitar documentos del ítem.', 1;
    IF @EsItem = 0 AND @TecnicoId <> @ActorId AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'tareas', 'editar') = 0
        THROW 50403, N'Solo el técnico asignado o quien edita tareas puede quitar documentos.', 1;
    DELETE pry.Adjunto WHERE AdjuntoId = @AdjuntoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Documento quitado de ' + @Codigo, @Nombre, @Ip);
    COMMIT;
END;

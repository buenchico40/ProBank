-- Corrige la descripción de un documento adjunto. Ítem: quien edita ítems. Tarea: el técnico asignado o quien edita tareas.
CREATE PROCEDURE pry.usp_Adjunto_Actualizar
    @Token UNIQUEIDENTIFIER, @Descripcion NVARCHAR(300), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @AdjuntoId INT, @Nombre NVARCHAR(200), @Codigo VARCHAR(20), @EsItem BIT, @TecnicoId INT;
    SELECT @AdjuntoId = a.AdjuntoId, @Nombre = a.Nombre, @Codigo = ISNULL(i.Codigo, t.Codigo),
           @EsItem = CASE WHEN a.ItemId IS NOT NULL THEN 1 ELSE 0 END, @TecnicoId = t.TecnicoId
    FROM pry.Adjunto a WITH (UPDLOCK) LEFT JOIN pry.Item i ON i.ItemId = a.ItemId LEFT JOIN pry.Tarea t ON t.TareaId = a.TareaId
    WHERE a.Token = @Token;
    IF @AdjuntoId IS NULL THROW 50404, N'El documento no existe.', 1;
    IF @EsItem = 1 AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar documentos del ítem.', 1;
    IF @EsItem = 0 AND @TecnicoId <> @ActorId AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'tareas', 'editar') = 0
        THROW 50403, N'Solo el técnico asignado o quien edita tareas puede editar sus documentos.', 1;
    UPDATE pry.Adjunto SET Descripcion = @Descripcion WHERE AdjuntoId = @AdjuntoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Descripción de documento de ' + @Codigo, CONCAT(@Nombre, N' · ', @Descripcion), @Ip);
    COMMIT;
END;

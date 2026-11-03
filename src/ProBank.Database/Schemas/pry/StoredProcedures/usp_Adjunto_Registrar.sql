-- Registra un documento ya guardado en Blob Storage, de un ítem (@Ambito = 'Item') o de una tarea ('Tarea'); máximo 20 por dueño.
-- Ítem: quien crea o edita ítems. Tarea: el técnico asignado o quien edita tareas.
CREATE PROCEDURE pry.usp_Adjunto_Registrar
    @Ambito VARCHAR(10), @DuenoToken UNIQUEIDENTIFIER, @Token UNIQUEIDENTIFIER, @Nombre NVARCHAR(200), @Descripcion NVARCHAR(300), @TipoContenido VARCHAR(100), @Tamano BIGINT,
    @Ruta VARCHAR(300), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @ItemId INT, @TareaId INT, @Codigo VARCHAR(20), @TecnicoId INT;
    IF @Ambito = 'Item'
    BEGIN
        IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'editar') = 0 AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'crear') = 0
            THROW 50403, N'Tu rol activo no tiene permiso para adjuntar documentos al ítem.', 1;
        SELECT @ItemId = ItemId, @Codigo = Codigo FROM pry.Item WITH (UPDLOCK) WHERE Token = @DuenoToken;
        IF @ItemId IS NULL THROW 50404, N'El proyecto, mejora o bug no existe.', 1;
    END
    ELSE
    BEGIN
        SELECT @TareaId = TareaId, @Codigo = Codigo, @TecnicoId = TecnicoId FROM pry.Tarea WITH (UPDLOCK) WHERE Token = @DuenoToken;
        IF @TareaId IS NULL THROW 50404, N'La tarea no existe.', 1;
        IF @TecnicoId <> @ActorId AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'tareas', 'editar') = 0
            THROW 50403, N'Solo el técnico asignado o quien edita tareas puede adjuntar documentos.', 1;
    END;
    IF (SELECT COUNT(*) FROM pry.Adjunto WHERE ItemId = @ItemId OR TareaId = @TareaId) >= 20 THROW 50422, N'Ya tiene el máximo de 20 documentos.', 1;
    INSERT pry.Adjunto (Token, ItemId, TareaId, Nombre, Descripcion, TipoContenido, Tamano, Ruta, SubidoPorId)
    VALUES (@Token, @ItemId, @TareaId, @Nombre, @Descripcion, @TipoContenido, @Tamano, @Ruta, @ActorId);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Documento adjunto a ' + @Codigo, CONCAT(@Nombre, N' · ', @Tamano, N' bytes'), @Ip);
    COMMIT;
END;

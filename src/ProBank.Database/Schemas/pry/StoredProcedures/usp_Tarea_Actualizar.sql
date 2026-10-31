-- Edita la tarea (PRY-09: «Editar tarea»). Los aprobadores solo cambian mientras la tarea está Pendiente.
CREATE PROCEDURE pry.usp_Tarea_Actualizar
    @Token UNIQUEIDENTIFIER, @HitoId INT = NULL, @Titulo NVARCHAR(200), @Area VARCHAR(40), @TecnicoId INT, @Orden INT,
    @FechaInicio DATE, @FechaTerminacion DATE, @Avance INT, @DescripcionHtml NVARCHAR(MAX) = NULL, @Aprobadores VARCHAR(400) = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'tareas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar tareas.', 1;
    IF pry.fn_PuedeAccion(@RolActivo, 'editar_tarea') = 0
        THROW 50403, N'Tu rol activo no está habilitado para «Editar tarea» (Portafolio › Parametrización).', 1;
    BEGIN TRAN;
    DECLARE @TareaId INT, @ItemId INT, @Estado VARCHAR(20), @Codigo VARCHAR(20);
    SELECT @TareaId = TareaId, @ItemId = ItemId, @Estado = Estado, @Codigo = Codigo FROM pry.Tarea WITH (UPDLOCK) WHERE Token = @Token;
    IF @TareaId IS NULL THROW 50404, N'La tarea no existe.', 1;
    IF @Estado = 'Completada' THROW 50422, N'La tarea está completada.', 1;
    IF @HitoId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pry.Hito WHERE HitoId = @HitoId AND ItemId = @ItemId) THROW 50422, N'El hito no pertenece al ítem.', 1;
    IF NOT EXISTS (SELECT 1 FROM seg.Usuario WHERE UsuarioId = @TecnicoId AND Estado = 'Activo') THROW 50422, N'El técnico no existe o está inactivo.', 1;
    UPDATE pry.Tarea SET HitoId = @HitoId, Titulo = @Titulo, Area = @Area, TecnicoId = @TecnicoId, Orden = @Orden, FechaInicio = @FechaInicio,
        FechaTerminacion = @FechaTerminacion, Avance = @Avance, DescripcionHtml = @DescripcionHtml
    WHERE TareaId = @TareaId;
    IF @Aprobadores IS NOT NULL
    BEGIN
        DECLARE @a TABLE (UsuarioId INT PRIMARY KEY);
        INSERT @a SELECT DISTINCT u.UsuarioId FROM seg.Usuario u WHERE u.Estado = 'Activo' AND u.UsuarioId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@Aprobadores, ','));
        DECLARE @Cambian BIT = CASE WHEN EXISTS (SELECT UsuarioId FROM @a EXCEPT SELECT UsuarioId FROM pry.TareaAprobador WHERE TareaId = @TareaId)
                                      OR EXISTS (SELECT UsuarioId FROM pry.TareaAprobador WHERE TareaId = @TareaId EXCEPT SELECT UsuarioId FROM @a) THEN 1 ELSE 0 END;
        IF @Cambian = 1
        BEGIN
            IF @Estado <> 'Pendiente' THROW 50422, N'Los aprobadores solo se cambian mientras la tarea está Pendiente.', 1;
            IF EXISTS (SELECT 1 FROM @a) AND pry.fn_PuedeAccion(@RolActivo, 'exigir_aprobacion') = 0
                THROW 50403, N'Tu rol activo no está habilitado para «Exigir aprobación» (Portafolio › Parametrización).', 1;
            DELETE pry.TareaAprobador WHERE TareaId = @TareaId;
            INSERT pry.TareaAprobador (TareaId, UsuarioId) SELECT @TareaId, UsuarioId FROM @a;
        END;
    END;
    INSERT pry.TareaEvento (TareaId, UsuarioId, Evento, Detalle) VALUES (@TareaId, @ActorId, N'Tarea editada', CONCAT(N'Avance ', @Avance, N' %'));
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Edición de la tarea ' + @Codigo, @Titulo, @Ip);
    COMMIT;
END;

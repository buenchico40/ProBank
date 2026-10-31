-- PRY-03 y PRY-04: crea la tarea dentro del ítem con su técnico (de cualquier área) y sus aprobadores (CSV, puede venir vacío).
-- PRY-09: asignar aprobadores exige que el rol activo esté habilitado para «Exigir aprobación».
CREATE PROCEDURE pry.usp_Tarea_Crear
    @Token UNIQUEIDENTIFIER, @ItemId INT, @HitoId INT = NULL, @Titulo NVARCHAR(200), @Area VARCHAR(40), @TecnicoId INT, @Orden INT,
    @FechaInicio DATE, @FechaTerminacion DATE, @DescripcionHtml NVARCHAR(MAX) = NULL, @UsuarioId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45),
    @Aprobadores VARCHAR(400) = '', @Codigo VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@UsuarioId, @RolActivo, 'tareas', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para crear tareas.', 1;
    IF LEN(@Aprobadores) > 0 AND pry.fn_PuedeAccion(@RolActivo, 'exigir_aprobacion') = 0
        THROW 50403, N'Tu rol activo no está habilitado para «Exigir aprobación» (Portafolio › Parametrización).', 1;
    IF @HitoId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pry.Hito WHERE HitoId = @HitoId AND ItemId = @ItemId)
        THROW 50422, N'El hito no pertenece al ítem.', 1;
    IF NOT EXISTS (SELECT 1 FROM seg.Usuario WHERE UsuarioId = @TecnicoId AND Estado = 'Activo') THROW 50422, N'El técnico no existe o está inactivo.', 1;
    BEGIN TRAN;
    INSERT pry.Tarea (Token, ItemId, HitoId, Titulo, DescripcionHtml, Area, TecnicoId, Orden, FechaInicio, FechaTerminacion, Estado, CreadoPorId)
    VALUES (@Token, @ItemId, @HitoId, @Titulo, @DescripcionHtml, @Area, @TecnicoId, @Orden, @FechaInicio, @FechaTerminacion, 'Pendiente', @UsuarioId);
    DECLARE @TareaId INT = SCOPE_IDENTITY();
    INSERT pry.TareaAprobador (TareaId, UsuarioId)
    SELECT DISTINCT @TareaId, u.UsuarioId FROM seg.Usuario u WHERE u.Estado = 'Activo' AND u.UsuarioId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@Aprobadores, ','));
    SELECT @Codigo = Codigo FROM pry.Tarea WHERE TareaId = @TareaId;
    INSERT pry.TareaEvento (TareaId, UsuarioId, Evento, Detalle)
    SELECT @TareaId, @UsuarioId, N'Tarea creada y asignada', CONCAT(N'Técnico: ', Nombre) FROM seg.Usuario WHERE UsuarioId = @TecnicoId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@UsuarioId, 0), @RolActivo, 'Portafolio', N'Nueva tarea ' + @Codigo, CONCAT(i.Codigo, N' · ', @Titulo), @Ip FROM pry.Item i WHERE i.ItemId = @ItemId;
    COMMIT;
END;

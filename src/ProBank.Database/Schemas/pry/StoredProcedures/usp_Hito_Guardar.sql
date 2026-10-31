-- Reemplaza los hitos del ítem. @Hitos: JSON [{hitoId, nombre, orden, inicio, fin}]; hitoId null = nuevo.
-- Un hito con tareas no se puede quitar.
CREATE PROCEDURE pry.usp_Hito_Guardar
    @ItemToken UNIQUEIDENTIFIER, @Hitos NVARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar el ítem.', 1;
    DECLARE @ItemId INT, @Codigo VARCHAR(20);
    SELECT @ItemId = ItemId, @Codigo = Codigo FROM pry.Item WHERE Token = @ItemToken;
    IF @ItemId IS NULL THROW 50404, N'El proyecto, mejora o bug no existe.', 1;
    DECLARE @h TABLE (HitoId INT NULL, Nombre NVARCHAR(150), Orden INT, Inicio DATE NULL, Fin DATE NULL);
    INSERT @h SELECT HitoId, Nombre, Orden, Inicio, Fin
    FROM OPENJSON(@Hitos) WITH (HitoId INT '$.hitoId', Nombre NVARCHAR(150) '$.nombre', Orden INT '$.orden', Inicio DATE '$.inicio', Fin DATE '$.fin');
    IF EXISTS (SELECT 1 FROM @h WHERE NULLIF(LTRIM(Nombre), N'') IS NULL) THROW 50422, N'Cada hito necesita un nombre.', 1;
    IF EXISTS (SELECT 1 FROM @h WHERE Inicio IS NOT NULL AND Fin IS NOT NULL AND Fin < Inicio) THROW 50422, N'La fecha de fin del hito no puede ser anterior al inicio.', 1;
    IF EXISTS (SELECT 1 FROM @h x WHERE x.HitoId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pry.Hito h WHERE h.HitoId = x.HitoId AND h.ItemId = @ItemId))
        THROW 50422, N'Algún hito no pertenece al ítem.', 1;
    IF EXISTS (SELECT 1 FROM pry.Hito h WHERE h.ItemId = @ItemId AND h.HitoId NOT IN (SELECT HitoId FROM @h WHERE HitoId IS NOT NULL)
               AND EXISTS (SELECT 1 FROM pry.Tarea t WHERE t.HitoId = h.HitoId))
        THROW 50422, N'No se puede quitar un hito que tiene tareas.', 1;
    BEGIN TRAN;
    DELETE pry.Hito WHERE ItemId = @ItemId AND HitoId NOT IN (SELECT HitoId FROM @h WHERE HitoId IS NOT NULL);
    UPDATE h SET Nombre = x.Nombre, Orden = x.Orden, FechaInicio = x.Inicio, FechaFin = x.Fin FROM pry.Hito h JOIN @h x ON x.HitoId = h.HitoId;
    INSERT pry.Hito (ItemId, Nombre, Orden, FechaInicio, FechaFin) SELECT @ItemId, Nombre, Orden, Inicio, Fin FROM @h WHERE HitoId IS NULL;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Hitos del ítem ' + @Codigo, CONCAT((SELECT COUNT(*) FROM @h), N' hito(s)'), @Ip);
    COMMIT;
END;

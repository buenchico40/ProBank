-- Crea (@Token NULL) o edita un proyecto, mejora o bug. PRY-02: uno o varios responsables (IDs separados por coma).
-- PRY-09: crear exige que el rol activo esté habilitado para «Crear proyecto, mejora o bug».
CREATE PROCEDURE pry.usp_Item_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Tipo VARCHAR(10), @Titulo NVARCHAR(200), @DescripcionHtml NVARCHAR(MAX) = NULL,
    @Responsables VARCHAR(400), @Estado VARCHAR(20), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45),
    @TokenGuardado UNIQUEIDENTIFIER OUTPUT, @Codigo VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar proyectos, mejoras o bugs.', 1;
    IF @Token IS NULL AND pry.fn_PuedeAccion(@RolActivo, 'crear_item') = 0
        THROW 50403, N'Tu rol activo no está habilitado para «Crear proyecto, mejora o bug» (Portafolio › Parametrización).', 1;
    DECLARE @r TABLE (UsuarioId INT PRIMARY KEY);
    INSERT @r SELECT DISTINCT u.UsuarioId FROM seg.Usuario u
    WHERE u.Estado = 'Activo' AND u.UsuarioId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@Responsables, ','));
    IF NOT EXISTS (SELECT 1 FROM @r) THROW 50422, N'Asigna al menos un responsable activo.', 1;

    BEGIN TRAN;
    DECLARE @ItemId INT;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT pry.Item (Token, Tipo, Titulo, DescripcionHtml, Estado, CreadoPorId) VALUES (@TokenGuardado, @Tipo, @Titulo, @DescripcionHtml, @Estado, @ActorId);
        SET @ItemId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SELECT @ItemId = ItemId FROM pry.Item WITH (UPDLOCK) WHERE Token = @Token;
        IF @ItemId IS NULL THROW 50404, N'El proyecto, mejora o bug no existe.', 1;
        IF @Estado = 'Completado' AND EXISTS (SELECT 1 FROM pry.Tarea WHERE ItemId = @ItemId AND Estado <> 'Completada')
            THROW 50422, N'No se puede completar mientras tenga tareas sin completar.', 1;
        UPDATE pry.Item SET Tipo = @Tipo, Titulo = @Titulo, DescripcionHtml = @DescripcionHtml, Estado = @Estado WHERE ItemId = @ItemId;
        SET @TokenGuardado = @Token;
    END;
    DELETE pry.ItemResponsable WHERE ItemId = @ItemId AND UsuarioId NOT IN (SELECT UsuarioId FROM @r);
    INSERT pry.ItemResponsable (ItemId, UsuarioId) SELECT @ItemId, UsuarioId FROM @r x
    WHERE NOT EXISTS (SELECT 1 FROM pry.ItemResponsable ir WHERE ir.ItemId = @ItemId AND ir.UsuarioId = x.UsuarioId);
    SET @Codigo = (SELECT Codigo FROM pry.Item WHERE ItemId = @ItemId);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', CASE WHEN @Token IS NULL THEN N'Nuevo ítem ' ELSE N'Edición del ítem ' END + @Codigo,
            CONCAT(@Titulo, N' · ', @Estado), @Ip);
    COMMIT;
END;

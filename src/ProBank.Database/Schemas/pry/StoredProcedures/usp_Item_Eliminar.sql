-- Solo se eliminan borradores sin tareas ni revisiones (PRY-09: «Eliminar borradores»).
CREATE PROCEDURE pry.usp_Item_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'proyectos', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar proyectos, mejoras o bugs.', 1;
    IF pry.fn_PuedeAccion(@RolActivo, 'eliminar_borrador') = 0
        THROW 50403, N'Tu rol activo no está habilitado para «Eliminar borradores» (Portafolio › Parametrización).', 1;
    DECLARE @ItemId INT, @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @ItemId = ItemId, @Codigo = Codigo, @Estado = Estado FROM pry.Item WHERE Token = @Token;
    IF @ItemId IS NULL THROW 50404, N'El proyecto, mejora o bug no existe.', 1;
    IF @Estado <> 'Borrador' THROW 50422, N'Solo se pueden eliminar borradores.', 1;
    IF EXISTS (SELECT 1 FROM pry.Tarea WHERE ItemId = @ItemId) OR EXISTS (SELECT 1 FROM pry.Revision WHERE ItemId = @ItemId)
        THROW 50422, N'El borrador ya tiene tareas o acuerdos; cambie su estado a Anulado.', 1;
    BEGIN TRAN;
    DELETE pry.Hito WHERE ItemId = @ItemId;
    DELETE pry.ItemResponsable WHERE ItemId = @ItemId;
    DELETE pry.Item WHERE ItemId = @ItemId;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Eliminación del borrador ' + @Codigo, NULL, @Ip);
    COMMIT;
END;

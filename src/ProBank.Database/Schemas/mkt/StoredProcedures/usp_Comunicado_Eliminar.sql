-- Solo se eliminan borradores; lo publicado se retira.
CREATE PROCEDURE mkt.usp_Comunicado_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'comunicados', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar comunicados.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Titulo NVARCHAR(200);
    SELECT @Codigo = Codigo, @Estado = Estado, @Titulo = Titulo FROM mkt.Comunicado WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'El comunicado no existe.', 1;
    IF @Estado <> 'Borrador' THROW 50422, N'Solo se eliminan borradores; lo publicado se retira.', 1;
    DELETE mkt.Comunicado WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Eliminación del comunicado ' + @Codigo, @Titulo, @Ip);
    COMMIT;
END;

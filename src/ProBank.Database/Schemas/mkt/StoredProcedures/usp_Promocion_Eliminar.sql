-- Solo se eliminan borradores (lo publicado se retira). Devuelve la ruta de su imagen para borrarla de Blob Storage.
CREATE PROCEDURE mkt.usp_Promocion_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Ruta VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'promos', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar imágenes promocionales.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Titulo NVARCHAR(200);
    SELECT @Codigo = Codigo, @Estado = Estado, @Titulo = Titulo, @Ruta = ImagenRuta FROM mkt.Promocion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La imagen promocional no existe.', 1;
    IF @Estado <> 'Borrador' THROW 50422, N'Solo se eliminan borradores; lo publicado se retira.', 1;
    DELETE mkt.Promocion WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Eliminación de la imagen promocional ' + @Codigo, @Titulo, @Ip);
    COMMIT;
END;

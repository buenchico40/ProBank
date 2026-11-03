-- Asigna la imagen ya guardada en Blob Storage y devuelve la anterior para borrarla.
CREATE PROCEDURE mkt.usp_Promocion_GuardarImagen
    @Token UNIQUEIDENTIFIER, @Ruta VARCHAR(300), @Tipo VARCHAR(30), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @RutaAnterior VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'promos', 'editar') = 0 AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'promos', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para cambiar la imagen.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20);
    SELECT @Codigo = Codigo, @RutaAnterior = ImagenRuta FROM mkt.Promocion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La imagen promocional no existe.', 1;
    UPDATE mkt.Promocion SET ImagenRuta = @Ruta, ImagenTipo = @Tipo WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Imagen de ' + @Codigo, CASE WHEN @RutaAnterior IS NULL THEN N'Imagen cargada' ELSE N'Imagen reemplazada' END, @Ip);
    COMMIT;
END;

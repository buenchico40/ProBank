-- Retira una imagen publicada (programada, vigente o vencida): deja de mostrarse.
CREATE PROCEDURE mkt.usp_Promocion_Retirar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'promos', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para retirar imágenes promocionales.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @Codigo = Codigo, @Estado = Estado FROM mkt.Promocion WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La imagen promocional no existe.', 1;
    IF @Estado <> 'Publicado' THROW 50422, N'Solo se retira lo publicado.', 1;
    UPDATE mkt.Promocion SET Estado = 'Retirado' WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Retiro de ' + @Codigo, NULL, @Ip);
    COMMIT;
END;

-- Publica un comunicado: exige vigencia completa que no haya terminado. Devuelve el estado resultante
-- (Programado si empieza después de hoy).
CREATE PROCEDURE mkt.usp_Comunicado_Publicar
    @Token UNIQUEIDENTIFIER, @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @EstadoVigente VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'comunicados', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para publicar comunicados.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Desde DATE, @Hasta DATE;
    SELECT @Codigo = Codigo, @Estado = Estado, @Desde = Desde, @Hasta = Hasta FROM mkt.Comunicado WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'El comunicado no existe.', 1;
    IF @Estado = 'Publicado' THROW 50422, N'Ya está publicado.', 1;
    IF @Desde IS NULL OR @Hasta IS NULL THROW 50422, N'Defina la vigencia (desde y hasta) antes de publicar.', 1;
    IF @Hasta < @Hoy THROW 50422, N'La vigencia ya terminó: corrija las fechas antes de publicar.', 1;
    UPDATE mkt.Comunicado SET Estado = 'Publicado', PublicadoPorId = @ActorId, FechaPublicacion = SYSDATETIME() WHERE Token = @Token;
    SET @EstadoVigente = mkt.fn_EstadoVigente('Publicado', @Desde, @Hasta, @Hoy);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Publicación de ' + @Codigo,
            CONCAT(CONVERT(CHAR(10), @Desde, 103), N' – ', CONVERT(CHAR(10), @Hasta, 103)), @Ip);
    COMMIT;
END;

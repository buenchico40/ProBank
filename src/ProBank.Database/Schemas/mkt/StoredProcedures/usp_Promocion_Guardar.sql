-- Crea (@Token NULL) o edita una imagen promocional. Lo publicado conserva su estado y debe mantener la vigencia completa.
CREATE PROCEDURE mkt.usp_Promocion_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Titulo NVARCHAR(200), @Ubicacion NVARCHAR(20), @TextoAlternativo NVARCHAR(250) = NULL, @Enlace NVARCHAR(400) = NULL,
    @Desde DATE = NULL, @Hasta DATE = NULL, @Audiencia NVARCHAR(60), @Orden INT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'promos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar imágenes promocionales.', 1;
    IF @Audiencia <> N'Todos' AND NOT EXISTS (SELECT 1 FROM seg.Rol WHERE Nombre = @Audiencia AND Estado = 'Activo')
        THROW 50422, N'La audiencia debe ser «Todos» o un rol activo.', 1;
    BEGIN TRAN;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT mkt.Promocion (Token, Titulo, Ubicacion, TextoAlternativo, Enlace, Desde, Hasta, Audiencia, Orden, CreadoPorId)
        VALUES (@TokenGuardado, @Titulo, @Ubicacion, @TextoAlternativo, @Enlace, @Desde, @Hasta, @Audiencia, @Orden, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Estado VARCHAR(20) = (SELECT Estado FROM mkt.Promocion WITH (UPDLOCK) WHERE Token = @Token);
        IF @Estado IS NULL THROW 50404, N'La imagen promocional no existe.', 1;
        IF @Estado = 'Publicado' AND (@Desde IS NULL OR @Hasta IS NULL) THROW 50422, N'Lo publicado debe tener la vigencia completa (desde y hasta).', 1;
        UPDATE mkt.Promocion SET Titulo = @Titulo, Ubicacion = @Ubicacion, TextoAlternativo = @TextoAlternativo, Enlace = @Enlace,
               Desde = @Desde, Hasta = @Hasta, Audiencia = @Audiencia, Orden = @Orden
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Marketing',
           CASE WHEN @Token IS NULL THEN N'Nueva imagen promocional ' ELSE N'Edición de la imagen promocional ' END + Codigo,
           CONCAT(Titulo, N' · ', Ubicacion, N' · ', Audiencia), @Ip
    FROM mkt.Promocion WHERE Token = @TokenGuardado;
    COMMIT;
END;

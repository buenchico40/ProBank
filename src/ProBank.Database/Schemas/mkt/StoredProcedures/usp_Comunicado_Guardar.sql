-- Crea (@Token NULL) o edita un comunicado. Lo publicado conserva su estado y debe mantener la vigencia completa.
CREATE PROCEDURE mkt.usp_Comunicado_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Titulo NVARCHAR(200), @Tipo NVARCHAR(20), @Resumen NVARCHAR(300), @ContenidoHtml NVARCHAR(MAX) = NULL,
    @Contacto NVARCHAR(150) = NULL, @Desde DATE = NULL, @Hasta DATE = NULL, @Audiencia NVARCHAR(60), @Destacado BIT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'comunicados', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar comunicados.', 1;
    IF @Audiencia <> N'Todos' AND NOT EXISTS (SELECT 1 FROM seg.Rol WHERE Nombre = @Audiencia AND Estado = 'Activo')
        THROW 50422, N'La audiencia debe ser «Todos» o un rol activo.', 1;
    BEGIN TRAN;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT mkt.Comunicado (Token, Titulo, Tipo, Resumen, ContenidoHtml, Contacto, Desde, Hasta, Audiencia, Destacado, CreadoPorId)
        VALUES (@TokenGuardado, @Titulo, @Tipo, @Resumen, @ContenidoHtml, @Contacto, @Desde, @Hasta, @Audiencia, @Destacado, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Estado VARCHAR(20) = (SELECT Estado FROM mkt.Comunicado WITH (UPDLOCK) WHERE Token = @Token);
        IF @Estado IS NULL THROW 50404, N'El comunicado no existe.', 1;
        IF @Estado = 'Publicado' AND (@Desde IS NULL OR @Hasta IS NULL) THROW 50422, N'Lo publicado debe tener la vigencia completa (desde y hasta).', 1;
        UPDATE mkt.Comunicado SET Titulo = @Titulo, Tipo = @Tipo, Resumen = @Resumen, ContenidoHtml = @ContenidoHtml, Contacto = @Contacto,
               Desde = @Desde, Hasta = @Hasta, Audiencia = @Audiencia, Destacado = @Destacado
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Marketing',
           CASE WHEN @Token IS NULL THEN N'Nuevo comunicado ' ELSE N'Edición del comunicado ' END + Codigo,
           CONCAT(Titulo, N' · ', Tipo, N' · ', Audiencia, CASE WHEN Destacado = 1 THEN N' · destacado' END), @Ip
    FROM mkt.Comunicado WHERE Token = @TokenGuardado;
    COMMIT;
END;

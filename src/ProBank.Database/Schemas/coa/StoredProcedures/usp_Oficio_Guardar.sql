-- Crea (@Token NULL) o edita un oficio. La fecha de recepción no puede ser posterior a hoy. Un oficio anulado no se edita.
CREATE PROCEDURE coa.usp_Oficio_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Numero VARCHAR(30), @Recibido DATE, @RemitidoPor NVARCHAR(150), @ContenidoHtml NVARCHAR(MAX) = NULL,
    @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'oficios', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar oficios.', 1;
    IF @Recibido > @Hoy THROW 50422, N'La fecha de recepción no puede ser posterior a hoy.', 1;
    IF EXISTS (SELECT 1 FROM coa.Oficio WHERE Numero = @Numero AND Token <> ISNULL(@Token, '00000000-0000-0000-0000-000000000000'))
        THROW 50409, N'Ya existe un oficio con ese número.', 1;

    BEGIN TRAN;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT coa.Oficio (Token, Numero, Recibido, RemitidoPor, ContenidoHtml, RegistradoPorId)
        VALUES (@TokenGuardado, @Numero, @Recibido, @RemitidoPor, @ContenidoHtml, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Anulado BIT = (SELECT Anulado FROM coa.Oficio WITH (UPDLOCK) WHERE Token = @Token);
        IF @Anulado IS NULL THROW 50404, N'El oficio no existe.', 1;
        IF @Anulado = 1 THROW 50422, N'El oficio está anulado.', 1;
        UPDATE coa.Oficio SET Numero = @Numero, Recibido = @Recibido, RemitidoPor = @RemitidoPor, ContenidoHtml = @ContenidoHtml WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', CASE WHEN @Token IS NULL THEN N'Registro de oficio ' ELSE N'Edición de oficio ' END + @Numero,
            CONCAT(N'Recibido ', CONVERT(CHAR(10), @Recibido, 103), N' · ', @RemitidoPor), @Ip);
    COMMIT;
END;

-- CRE-01: crea (@Token NULL, código CER-NNNN) o edita un certificado. Solo se registra su vigencia; el certificado queda en su almacén.
CREATE PROCEDURE cred.usp_Certificado_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @NombreComun NVARCHAR(200), @Tipo NVARCHAR(30), @Emisor NVARCHAR(150), @Vence DATE,
    @Almacen NVARCHAR(150), @Responsable NVARCHAR(100), @DiasAviso INT, @Huella VARCHAR(64) = NULL, @NombresAlternativos NVARCHAR(500) = NULL,
    @UsadoPor NVARCHAR(200) = NULL, @Activo BIT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'certificados', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar certificados.', 1;
    BEGIN TRAN;
    IF @Activo = 1 AND EXISTS (SELECT 1 FROM cred.Certificado WITH (UPDLOCK, HOLDLOCK)
                               WHERE NombreComun = @NombreComun AND Tipo = @Tipo AND Almacen = @Almacen AND Activo = 1 AND (@Token IS NULL OR Token <> @Token))
        THROW 50409, N'Ya hay un certificado activo con ese nombre común, tipo y almacén: registre su renovación en lugar de crear otro.', 1;
    DECLARE @Codigo VARCHAR(10);
    IF @Token IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM cred.Certificado WITH (UPDLOCK, HOLDLOCK)), 0);
        SET @Codigo = 'CER-' + RIGHT('000' + CAST(@n AS VARCHAR(10)), CASE WHEN @n < 10000 THEN 4 ELSE 10 END);
        SET @TokenGuardado = NEWID();
        INSERT cred.Certificado (Token, Codigo, NombreComun, Tipo, Emisor, Vence, Almacen, Responsable, DiasAviso, Huella, NombresAlternativos, UsadoPor, Activo)
        VALUES (@TokenGuardado, @Codigo, @NombreComun, @Tipo, @Emisor, @Vence, @Almacen, @Responsable, @DiasAviso, @Huella, @NombresAlternativos, @UsadoPor, @Activo);
    END
    ELSE
    BEGIN
        SET @Codigo = (SELECT Codigo FROM cred.Certificado WITH (UPDLOCK) WHERE Token = @Token);
        IF @Codigo IS NULL THROW 50404, N'El certificado no existe.', 1;
        UPDATE cred.Certificado SET NombreComun = @NombreComun, Tipo = @Tipo, Emisor = @Emisor, Vence = @Vence, Almacen = @Almacen,
               Responsable = @Responsable, DiasAviso = @DiasAviso, Huella = @Huella, NombresAlternativos = @NombresAlternativos,
               UsadoPor = @UsadoPor, Activo = @Activo
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda',
            CASE WHEN @Token IS NULL THEN N'Nuevo certificado ' ELSE N'Edición del certificado ' END + @Codigo,
            CONCAT(@NombreComun, N' · ', @Tipo, N' · vence ', CONVERT(VARCHAR(10), @Vence, 103), N' · aviso ', @DiasAviso, N' días · ', @Almacen,
                   CASE WHEN @Activo = 0 THEN N' · inactivo' END), @Ip);
    COMMIT;
END;

-- CRE-01: registra la renovación de un certificado activo: nuevo vencimiento (posterior a hoy) y huella del nuevo certificado.
CREATE PROCEDURE cred.usp_Certificado_Renovar
    @Token UNIQUEIDENTIFIER, @Vence DATE, @Huella VARCHAR(64), @Hoy DATE,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'certificados', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar renovaciones de certificados.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(10), @Nombre NVARCHAR(200), @VenceAnterior DATE, @HuellaAnterior VARCHAR(64), @Activo BIT;
    SELECT @Id = CertificadoId, @Codigo = Codigo, @Nombre = NombreComun, @VenceAnterior = Vence, @HuellaAnterior = Huella, @Activo = Activo
    FROM cred.Certificado WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'El certificado no existe.', 1;
    IF @Activo = 0 THROW 50409, N'El certificado está inactivo: actívelo antes de registrar su renovación.', 1;
    IF @Vence <= @Hoy THROW 50422, N'El nuevo vencimiento debe ser posterior a hoy.', 1;
    IF @Huella = @HuellaAnterior THROW 50422, N'La huella es la del certificado actual: indique la del certificado nuevo.', 1;

    INSERT cred.CertificadoRenovacion (CertificadoId, VenceAnterior, VenceNuevo, HuellaAnterior, HuellaNueva, UsuarioId)
    VALUES (@Id, @VenceAnterior, @Vence, @HuellaAnterior, @Huella, @ActorId);
    UPDATE cred.Certificado SET Vence = @Vence, Huella = @Huella WHERE CertificadoId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Bóveda', N'Renovación del certificado ' + @Codigo,
            CONCAT(@Nombre, N' · vencía ', CONVERT(VARCHAR(10), @VenceAnterior, 103), N' → vence ', CONVERT(VARCHAR(10), @Vence, 103),
                   N' · huella ', @Huella), @Ip);
    COMMIT;
END;

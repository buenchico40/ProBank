-- Registra un documento ya guardado en Blob Storage: el contrato de una solicitud o una factura o la distribución de un pago.
-- Lo hace quien crea o edita solicitudes (contrato) o pagos (facturas y distribución); no en lo anulado.
CREATE PROCEDURE com.usp_Documento_Registrar
    @DuenoToken UNIQUEIDENTIFIER, @Token UNIQUEIDENTIFIER, @Tipo NVARCHAR(20), @Nombre NVARCHAR(200), @TipoContenido VARCHAR(100), @Tamano BIGINT,
    @Ruta VARCHAR(300), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Opcion VARCHAR(20) = CASE WHEN @Tipo = N'Contrato' THEN 'solicitudes' ELSE 'pagos' END;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, @Opcion, 'editar') = 0 AND seg.fn_TienePermiso(@ActorId, @RolActivo, @Opcion, 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para adjuntar este documento.', 1;
    BEGIN TRAN;
    DECLARE @SolicitudId INT, @PagoId INT, @Codigo VARCHAR(20);
    IF @Tipo = N'Contrato'
    BEGIN
        SELECT @SolicitudId = SolicitudId, @Codigo = Codigo FROM com.Solicitud WHERE Token = @DuenoToken AND Anulada = 0;
        IF @SolicitudId IS NULL THROW 50404, N'La solicitud no existe o está anulada.', 1;
    END
    ELSE
    BEGIN
        SELECT @PagoId = PagoId, @Codigo = Codigo FROM com.Pago WHERE Token = @DuenoToken AND Estado <> 'Anulado';
        IF @PagoId IS NULL THROW 50404, N'El pago no existe o está anulado.', 1;
    END;
    IF (SELECT COUNT(*) FROM com.Documento WHERE SolicitudId = @SolicitudId OR PagoId = @PagoId) >= 20 THROW 50422, N'Ya tiene el máximo de 20 documentos.', 1;
    INSERT com.Documento (Token, SolicitudId, PagoId, Tipo, Nombre, TipoContenido, Tamano, Ruta, SubidoPorId)
    VALUES (@Token, @SolicitudId, @PagoId, @Tipo, @Nombre, @TipoContenido, @Tamano, @Ruta, @ActorId);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CONCAT(@Tipo, N' adjunto a ', @Codigo), CONCAT(@Nombre, N' · ', @Tamano, N' bytes'), @Ip);
    COMMIT;
END;

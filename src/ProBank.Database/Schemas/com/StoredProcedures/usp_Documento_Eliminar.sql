-- Quita un documento y devuelve la ruta de su archivo para borrarlo de Blob Storage.
CREATE PROCEDURE com.usp_Documento_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Ruta VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @Id INT, @Tipo NVARCHAR(20), @Nombre NVARCHAR(200), @Codigo VARCHAR(20);
    SELECT @Id = d.DocumentoId, @Tipo = d.Tipo, @Nombre = d.Nombre, @Ruta = d.Ruta, @Codigo = ISNULL(s.Codigo, p.Codigo)
    FROM com.Documento d LEFT JOIN com.Solicitud s ON s.SolicitudId = d.SolicitudId LEFT JOIN com.Pago p ON p.PagoId = d.PagoId
    WHERE d.Token = @Token;
    IF @Id IS NULL THROW 50404, N'El documento no existe.', 1;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, CASE WHEN @Tipo = N'Contrato' THEN 'solicitudes' ELSE 'pagos' END, 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para quitar este documento.', 1;
    DELETE com.Documento WHERE DocumentoId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CONCAT(@Tipo, N' quitado de ', @Codigo), @Nombre, @Ip);
    COMMIT;
END;

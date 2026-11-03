-- Documentos de una solicitud (@Dueno = 'Solicitud': contrato) o de un pago ('Pago': facturas y distribución).
CREATE PROCEDURE com.usp_Documento_Listar @Dueno VARCHAR(10), @DuenoToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.Token, d.Tipo, d.Nombre, d.TipoContenido, d.Tamano, u.Nombre AS SubidoPor, d.FechaRegistro
    FROM com.Documento d
    JOIN seg.Usuario u ON u.UsuarioId = d.SubidoPorId
    LEFT JOIN com.Solicitud s ON s.SolicitudId = d.SolicitudId
    LEFT JOIN com.Pago p ON p.PagoId = d.PagoId
    WHERE (@Dueno = 'Solicitud' AND s.Token = @DuenoToken) OR (@Dueno = 'Pago' AND p.Token = @DuenoToken)
    ORDER BY d.Tipo, d.DocumentoId DESC;
END;

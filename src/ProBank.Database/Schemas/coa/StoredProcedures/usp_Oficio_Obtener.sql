-- Oficio con su contenido y, en un segundo resultado, sus órdenes (cada coactiva del oficio con su avance).
CREATE PROCEDURE coa.usp_Oficio_Obtener
    @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OficioId INT = (SELECT OficioId FROM coa.Oficio WHERE Token = @Token);
    SELECT v.Token, v.Numero, v.Recibido, v.RemitidoPor, f.ContenidoHtml, v.Ordenes, v.Entidades, v.MontoCoactivas, v.Ejecutado, v.Estado,
           u.Nombre AS RegistradoPor, f.FechaRegistro
    FROM coa.vw_Oficio v JOIN coa.Oficio f ON f.OficioId = v.OficioId JOIN seg.Usuario u ON u.UsuarioId = f.RegistradoPorId
    WHERE v.OficioId = @OficioId;

    EXEC coa.usp_Orden_ListarPorOficio @OficioId;
END;

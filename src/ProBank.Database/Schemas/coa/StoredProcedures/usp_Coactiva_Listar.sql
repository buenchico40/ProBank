-- Coactivas › Coactivas (o las de un cliente, por identificación).
CREATE PROCEDURE coa.usp_Coactiva_Listar
    @Identificacion VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Token, c.NumeroProceso, f.Numero AS Oficio, f.Token AS OficioToken, f.Recibido AS OficioRecibido, e.Nombre AS Entidad,
           cl.Nombre AS Cliente, cl.Identificacion, c.Monto, c.Bloqueado, c.Levantado, c.Transferido, c.Cobertura, c.Estado, c.ProductosPermitidos
    FROM coa.vw_Coactiva c
    JOIN coa.Oficio f ON f.OficioId = c.OficioId
    JOIN coa.Entidad e ON e.EntidadId = c.EntidadId
    JOIN coa.Cliente cl ON cl.ClienteId = c.ClienteId
    WHERE @Identificacion IS NULL OR cl.Identificacion = @Identificacion
    ORDER BY c.CoactivaId DESC;
END;

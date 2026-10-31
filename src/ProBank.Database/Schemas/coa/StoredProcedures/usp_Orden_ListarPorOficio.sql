-- Órdenes de un oficio (una fila por coactiva y orden), en el orden en que se registraron.
CREATE PROCEDURE coa.usp_Orden_ListarPorOficio
    @OficioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrdenId, o.Tipo, c.Token AS CoactivaToken, c.NumeroProceso, e.Nombre AS Entidad, cl.Nombre AS Cliente, cl.Identificacion,
           c.Monto AS MontoCoactiva, o.MontoOrdenado, c.Bloqueado, o.Ejecutado, o.Estado, o.Observacion
    FROM coa.vw_Orden o
    JOIN coa.vw_Coactiva c ON c.CoactivaId = o.CoactivaId
    JOIN coa.Entidad e ON e.EntidadId = c.EntidadId
    JOIN coa.Cliente cl ON cl.ClienteId = c.ClienteId
    WHERE o.OficioId = @OficioId
    ORDER BY o.OrdenId;
END;

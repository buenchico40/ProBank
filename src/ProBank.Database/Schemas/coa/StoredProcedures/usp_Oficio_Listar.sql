-- Coactivas › Oficios: totales y estado calculados en coa.vw_Oficio.
CREATE PROCEDURE coa.usp_Oficio_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Token, Numero, Recibido, RemitidoPor, Ordenes, Bloqueos, Levantamientos, Transferencias, Entidades, MontoCoactivas, Ejecutado, Estado
    FROM coa.vw_Oficio
    ORDER BY Recibido DESC, Numero DESC;
END;

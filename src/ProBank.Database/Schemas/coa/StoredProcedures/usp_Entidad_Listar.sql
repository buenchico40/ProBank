CREATE PROCEDURE coa.usp_Entidad_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EntidadId, Ruc, Nombre, CuentaDestino, Estado FROM coa.Entidad ORDER BY Nombre;
END;

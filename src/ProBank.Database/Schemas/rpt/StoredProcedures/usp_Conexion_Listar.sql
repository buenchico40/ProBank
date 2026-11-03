-- SQL-02: bases de datos configuradas, con cuántas consultas las usan. Nunca devuelve la cadena (solo el nombre del secreto).
-- Con @Codigo devuelve además las consultas que pueden usarla.
CREATE PROCEDURE rpt.usp_Conexion_Listar @Codigo VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, c.Nombre, c.Servidor, c.BaseDatos, c.Autenticacion, c.SecretoKeyVault, c.Estado,
           (SELECT COUNT(*) FROM rpt.ConsultaConexion cc WHERE cc.ConexionId = c.ConexionId) AS Consultas
    FROM rpt.Conexion c
    WHERE @Codigo IS NULL OR c.Codigo = @Codigo
    ORDER BY c.Codigo;

    IF @Codigo IS NULL RETURN;
    SELECT q.Codigo, q.Nombre, q.Estado
    FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId JOIN rpt.Consulta q ON q.ConsultaId = cc.ConsultaId
    WHERE c.Codigo = @Codigo ORDER BY q.Codigo;
END;

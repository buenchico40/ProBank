-- SEG-03: matriz módulo / opción / acción o función con lo concedido al rol (@RolId 0 = rol nuevo, nada concedido).
CREATE PROCEDURE seg.usp_Permiso_ListarPorRol
    @RolId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.Modulo, o.Opcion, o.Nombre AS OpcionNombre, a.Accion, a.Nombre AS AccionNombre, a.Tipo,
           CAST(CASE WHEN p.RolId IS NULL THEN 0 ELSE 1 END AS BIT) AS Concedido
    FROM seg.Opcion o
    JOIN seg.OpcionAccion a ON a.Opcion = o.Opcion
    LEFT JOIN seg.Permiso p ON p.RolId = @RolId AND p.Opcion = a.Opcion AND p.Accion = a.Accion
    ORDER BY o.Orden, a.Orden;
END;

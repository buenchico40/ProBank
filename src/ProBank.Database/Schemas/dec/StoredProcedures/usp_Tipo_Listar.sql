-- DEC-02: tipos de decisión (o solo los activos) y, en el segundo resultado, sus aprobadores en orden.
CREATE PROCEDURE dec.usp_Tipo_Listar @SoloActivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.Codigo, t.Nombre, t.Descripcion, t.Secuencial, t.Activo,
           (SELECT COUNT(*) FROM dec.TipoAprobador a WHERE a.TipoId = t.TipoId) AS Aprobadores,
           (SELECT COUNT(*) FROM dec.Decision d WHERE d.TipoId = t.TipoId) AS Decisiones
    FROM dec.TipoDecision t
    WHERE @SoloActivos = 0 OR t.Activo = 1
    ORDER BY t.Nombre;

    SELECT t.Codigo AS Tipo, u.UsuarioId, u.Usuario, u.Nombre, u.Estado, a.Orden
    FROM dec.TipoAprobador a
    JOIN dec.TipoDecision t ON t.TipoId = a.TipoId
    JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE @SoloActivos = 0 OR t.Activo = 1
    ORDER BY t.Codigo, a.Orden, u.Nombre;
END;

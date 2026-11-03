-- Parámetros globales con su definición, valor y último cambio.
CREATE PROCEDURE cfg.usp_Parametro_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Clave, p.Categoria, p.Nombre, p.Descripcion, p.Tipo, p.Opciones, p.Minimo, p.Maximo, p.Prefijo, p.Valor,
           u.Nombre AS ModificadoPor, p.FechaModificacion
    FROM cfg.Parametro p LEFT JOIN seg.Usuario u ON u.UsuarioId = p.ModificadoPorId
    ORDER BY p.Orden;
END;

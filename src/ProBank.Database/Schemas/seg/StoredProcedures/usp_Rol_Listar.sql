-- Seguridad › Roles y permisos: opciones del menú visibles (permiso "ver") y usuarios activos por rol.
CREATE PROCEDURE seg.usp_Rol_Listar
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Total INT = (SELECT COUNT(*) FROM seg.Opcion);
    SELECT r.RolId, r.Nombre, r.Descripcion, r.Estado,
           (SELECT COUNT(*) FROM seg.Permiso p WHERE p.RolId = r.RolId AND p.Accion = 'ver') AS Opciones,
           @Total AS TotalOpciones,
           (SELECT COUNT(*) FROM seg.UsuarioRol ur JOIN seg.Usuario u ON u.UsuarioId = ur.UsuarioId
            WHERE ur.RolId = r.RolId AND u.Estado = 'Activo') AS Usuarios
    FROM seg.Rol r
    ORDER BY CASE WHEN r.Nombre = 'Administrador' THEN 0 ELSE 1 END, r.Nombre;
END;

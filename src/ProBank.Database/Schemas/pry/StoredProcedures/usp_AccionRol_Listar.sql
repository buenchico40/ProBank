-- PRY-09: acciones de Portafolio con los roles que pueden ejecutarlas.
CREATE PROCEDURE pry.usp_AccionRol_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.Clave, a.Nombre, a.Estado,
           (SELECT STRING_AGG(r.Nombre, ',') WITHIN GROUP (ORDER BY r.Nombre) FROM pry.AccionRol ar JOIN seg.Rol r ON r.RolId = ar.RolId WHERE ar.Accion = a.Clave) AS Roles
    FROM pry.Accion a ORDER BY a.Orden;

    SELECT r.Nombre, r.Descripcion, r.Estado,
           (SELECT COUNT(*) FROM seg.UsuarioRol ur JOIN seg.Usuario u ON u.UsuarioId = ur.UsuarioId WHERE ur.RolId = r.RolId AND u.Estado = 'Activo') AS Usuarios
    FROM seg.Rol r ORDER BY r.Nombre;
END;

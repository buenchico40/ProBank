-- Perfil del usuario conectado: datos, roles asignados y permisos (opción y acción) de su rol activo.
CREATE PROCEDURE seg.usp_Usuario_ObtenerPerfil
    @UsuarioId INT, @RolActivo VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Usuario, Nombre, Correo, RolPredeterminado, Estado, DebeCambiarClave, ClaveCambiada,
           UltimaConexion, UltimaIp, ConexionAnterior, IpAnterior
    FROM seg.Usuario WHERE UsuarioId = @UsuarioId;

    SELECT r.Nombre FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId AND r.Estado = 'Activo'
    WHERE ur.UsuarioId = @UsuarioId ORDER BY r.Nombre;

    SELECT p.Opcion, p.Accion
    FROM seg.Permiso p
    JOIN seg.Rol r ON r.RolId = p.RolId AND r.Nombre = @RolActivo AND r.Estado = 'Activo'
    WHERE EXISTS (SELECT 1 FROM seg.UsuarioRol ur WHERE ur.UsuarioId = @UsuarioId AND ur.RolId = r.RolId);
END;

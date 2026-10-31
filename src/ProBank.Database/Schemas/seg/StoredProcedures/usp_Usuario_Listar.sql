-- Seguridad › Usuarios. El bloqueo se calcula con la hora del servidor.
CREATE PROCEDURE seg.usp_Usuario_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Token, u.Usuario, u.Nombre, u.Correo, u.RolPredeterminado, u.Estado,
           CAST(CASE WHEN u.BloqueadoHasta > SYSDATETIME() THEN 1 ELSE 0 END AS BIT) AS Bloqueado,
           u.DebeCambiarClave, u.UltimaConexion, u.UltimaIp, u.IpPermitida,
           (SELECT STRING_AGG(r.Nombre, ',') WITHIN GROUP (ORDER BY r.Nombre)
            FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId WHERE ur.UsuarioId = u.UsuarioId) AS Roles
    FROM seg.Usuario u
    ORDER BY u.Usuario;
END;

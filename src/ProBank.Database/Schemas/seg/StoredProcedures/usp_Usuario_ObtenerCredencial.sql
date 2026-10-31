-- Inicio de sesión con usuario y contraseña: devuelve el hash y la sal para que la API verifique la clave,
-- el sello y la duración de la sesión (SEG-07), y en un segundo resultado los roles activos del usuario.
CREATE PROCEDURE seg.usp_Usuario_ObtenerCredencial
    @Usuario VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    -- El bloqueo se evalúa con la hora del servidor, la misma que usa usp_Usuario_RegistrarIngreso.
    SELECT UsuarioId, Usuario, Nombre, ClaveHash, ClaveSal, IpPermitida, Estado,
           CAST(CASE WHEN BloqueadoHasta > SYSDATETIME() THEN 1 ELSE 0 END AS BIT) AS Bloqueado,
           RolPredeterminado, DebeCambiarClave, SelloSesion,
           seg.fn_Parametro('MinutosSesion', 60) AS MinutosSesion
    FROM seg.Usuario WHERE Usuario = @Usuario;

    SELECT r.Nombre
    FROM seg.Usuario u
    JOIN seg.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
    JOIN seg.Rol r ON r.RolId = ur.RolId AND r.Estado = 'Activo'
    WHERE u.Usuario = @Usuario
    ORDER BY r.Nombre;
END;

-- Últimos inicios de sesión de un usuario (detalle desplegable de Seguridad › Usuarios).
CREATE PROCEDURE seg.usp_Usuario_ListarIngresos
    @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (10) a.Fecha, a.Ip,
           CASE WHEN a.Accion = N'Inicio de sesión' THEN 'Exitoso' ELSE 'Fallido' END AS Resultado, a.Detalle
    FROM aud.Auditoria a
    JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE u.Token = @Token AND a.Modulo = 'Seguridad' AND a.Accion IN (N'Inicio de sesión', N'Inicio de sesión fallido')
    ORDER BY a.AuditoriaId DESC;
END;

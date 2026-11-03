-- CRE-02: miembros (con sus roles) y credenciales de un grupo de acceso.
CREATE PROCEDURE cred.usp_Grupo_Detalle @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @GrupoId INT = (SELECT GrupoId FROM cred.GrupoAcceso WHERE Token = @Token);

    SELECT u.UsuarioId, u.Usuario, u.Nombre, u.Estado,
           ISNULL((SELECT STRING_AGG(r.Nombre, ', ') WITHIN GROUP (ORDER BY r.Nombre)
                   FROM seg.UsuarioRol ur JOIN seg.Rol r ON r.RolId = ur.RolId WHERE ur.UsuarioId = u.UsuarioId), '') AS Roles,
           m.Desde
    FROM cred.GrupoMiembro m JOIN seg.Usuario u ON u.UsuarioId = m.UsuarioId
    WHERE m.GrupoId = @GrupoId
    ORDER BY u.Nombre;

    SELECT 'cuenta' AS Clase, c.Token, c.Nombre, N'Cuenta técnica' AS Tipo, c.Sistema, c.Activo
    FROM cred.CuentaTecnica c WHERE c.GrupoId = @GrupoId
    UNION ALL
    SELECT 'secreto', s.Token, s.Nombre, N'Secreto · ' + s.Tipo, s.Sistema, s.Activo
    FROM cred.Secreto s WHERE s.GrupoId = @GrupoId
    ORDER BY Nombre;
END;

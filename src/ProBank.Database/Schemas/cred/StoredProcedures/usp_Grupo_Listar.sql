-- CRE-02: grupos de acceso con sus miembros y credenciales asignadas (o solo uno con @Token). EsMiembro: el usuario que consulta.
CREATE PROCEDURE cred.usp_Grupo_Listar @ActorId INT, @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.Token, g.Codigo, g.Nombre, g.Descripcion, g.Estado,
           (SELECT COUNT(*) FROM cred.GrupoMiembro m WHERE m.GrupoId = g.GrupoId) AS Miembros,
           (SELECT COUNT(*) FROM cred.CuentaTecnica c WHERE c.GrupoId = g.GrupoId) + (SELECT COUNT(*) FROM cred.Secreto s WHERE s.GrupoId = g.GrupoId) AS Credenciales,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM cred.GrupoMiembro m WHERE m.GrupoId = g.GrupoId AND m.UsuarioId = @ActorId) THEN 1 ELSE 0 END AS BIT) AS EsMiembro
    FROM cred.GrupoAcceso g
    WHERE @Token IS NULL OR g.Token = @Token
    ORDER BY g.Codigo;
END;

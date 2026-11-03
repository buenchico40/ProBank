-- CRE-02: el usuario integra el grupo de acceso y el grupo está activo (un grupo inactivo no da acceso a nadie).
CREATE FUNCTION cred.fn_EsMiembro (@UsuarioId INT, @GrupoId INT)
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1 FROM cred.GrupoMiembro m JOIN cred.GrupoAcceso g ON g.GrupoId = m.GrupoId AND g.Estado = 'Activo'
        WHERE m.GrupoId = @GrupoId AND m.UsuarioId = @UsuarioId) THEN 1 ELSE 0 END;
END;

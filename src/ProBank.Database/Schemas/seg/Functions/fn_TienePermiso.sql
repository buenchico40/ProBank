-- El rol activo se valida también en la base: los SP de escritura la usan antes de cambiar datos.
CREATE FUNCTION seg.fn_TienePermiso (@UsuarioId INT, @RolActivo VARCHAR(60), @Opcion VARCHAR(60), @Accion VARCHAR(60))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1 FROM seg.UsuarioRol ur
        JOIN seg.Rol r ON r.RolId = ur.RolId AND r.Nombre = @RolActivo AND r.Estado = 'Activo'
        JOIN seg.Permiso p ON p.RolId = r.RolId AND p.Opcion = @Opcion AND p.Accion = @Accion
        WHERE ur.UsuarioId = @UsuarioId) THEN 1 ELSE 0 END;
END;

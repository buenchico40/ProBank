-- PRY-09: el rol activo puede ejecutar la acción parametrizada (el Administrador siempre puede).
CREATE FUNCTION pry.fn_PuedeAccion (@RolActivo VARCHAR(60), @Accion VARCHAR(40))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN @RolActivo = 'Administrador' OR EXISTS (
        SELECT 1 FROM pry.AccionRol ar
        JOIN pry.Accion a ON a.Clave = ar.Accion AND a.Estado = 'Activo'
        JOIN seg.Rol r ON r.RolId = ar.RolId AND r.Estado = 'Activo' AND r.Nombre = @RolActivo
        WHERE ar.Accion = @Accion) THEN 1 ELSE 0 END;
END;

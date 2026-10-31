-- SEG-03: la API valida opción × acción del rol activo antes de cada comando o consulta.
CREATE PROCEDURE seg.usp_Permiso_Verificar
    @UsuarioId INT, @RolActivo VARCHAR(60), @Opcion VARCHAR(60), @Accion VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT seg.fn_TienePermiso(@UsuarioId, @RolActivo, @Opcion, @Accion);
END;

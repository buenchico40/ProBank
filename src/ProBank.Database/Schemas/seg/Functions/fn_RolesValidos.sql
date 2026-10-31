-- Roles activos de una lista separada por comas. Si algún nombre no existe o está inactivo, no aparece.
CREATE FUNCTION seg.fn_RolesValidos (@Roles VARCHAR(800))
RETURNS TABLE
AS
RETURN
    SELECT r.RolId, r.Nombre
    FROM seg.Rol r
    WHERE r.Estado = 'Activo'
      AND r.Nombre IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@Roles, ',') WHERE LTRIM(RTRIM(value)) <> '');

-- Audiencia: «Todos» o un rol. Administrador y Marketing ven todo lo vigente (para revisar lo publicado).
CREATE FUNCTION mkt.fn_VisiblePara (@Audiencia NVARCHAR(60), @RolActivo VARCHAR(60))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN @Audiencia = N'Todos' OR @Audiencia = @RolActivo OR @RolActivo IN ('Administrador', 'Marketing') THEN 1 ELSE 0 END;
END;

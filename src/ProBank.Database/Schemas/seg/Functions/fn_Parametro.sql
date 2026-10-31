-- Valor numérico de un parámetro de acceso (seg.ParametroAcceso); el segundo argumento es el valor si falta.
CREATE FUNCTION seg.fn_Parametro (@Clave VARCHAR(60), @Defecto INT)
RETURNS INT
AS
BEGIN
    RETURN ISNULL((SELECT TRY_CAST(Valor AS INT) FROM seg.ParametroAcceso WHERE Clave = @Clave), @Defecto);
END;

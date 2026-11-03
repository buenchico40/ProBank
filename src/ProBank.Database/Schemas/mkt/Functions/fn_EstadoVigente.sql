-- Estado que se muestra: lo publicado es Programado antes de su vigencia, Publicado durante ella y Vencido después.
CREATE FUNCTION mkt.fn_EstadoVigente (@Estado VARCHAR(20), @Desde DATE, @Hasta DATE, @Hoy DATE)
RETURNS VARCHAR(20)
AS
BEGIN
    RETURN CASE WHEN @Estado <> 'Publicado' THEN @Estado
                WHEN @Desde > @Hoy THEN 'Programado'
                WHEN @Hasta < @Hoy THEN 'Vencido'
                ELSE 'Publicado' END;
END;

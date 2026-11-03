-- Datos para servir la imagen: ruta y lo necesario para decidir quién puede verla (ubicación, audiencia y estado vigente).
CREATE PROCEDURE mkt.usp_Promocion_Imagen @Token UNIQUEIDENTIFIER, @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ImagenRuta, ImagenTipo, Ubicacion, Audiencia, mkt.fn_EstadoVigente(Estado, Desde, Hasta, @Hoy) AS Estado
    FROM mkt.Promocion WHERE Token = @Token AND ImagenRuta IS NOT NULL;
END;

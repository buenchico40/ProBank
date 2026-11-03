-- Una imagen promocional (para su ficha). La ruta de la imagen no sale de la base: se sirve con usp_Promocion_Imagen.
CREATE PROCEDURE mkt.usp_Promocion_Obtener @Token UNIQUEIDENTIFIER, @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Token, p.Codigo, p.Titulo, p.Ubicacion, p.TextoAlternativo, p.Enlace, p.Desde, p.Hasta, p.Audiencia, p.Orden,
           mkt.fn_EstadoVigente(p.Estado, p.Desde, p.Hasta, @Hoy) AS Estado, CAST(CASE WHEN p.ImagenRuta IS NULL THEN 0 ELSE 1 END AS BIT) AS TieneImagen,
           c.Nombre AS CreadoPor, p.FechaRegistro, u.Nombre AS PublicadoPor, p.FechaPublicacion
    FROM mkt.Promocion p
    JOIN seg.Usuario c ON c.UsuarioId = p.CreadoPorId
    LEFT JOIN seg.Usuario u ON u.UsuarioId = p.PublicadoPorId
    WHERE p.Token = @Token;
END;

-- Imágenes de la pantalla de inicio de sesión: publicadas, vigentes, con imagen y para todos (aún no hay rol).
CREATE PROCEDURE mkt.usp_Promocion_ListarInicioSesion @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Token, Titulo, TextoAlternativo, Enlace
    FROM mkt.Promocion
    WHERE Ubicacion = N'Inicio de sesión' AND Audiencia = N'Todos' AND ImagenRuta IS NOT NULL
      AND mkt.fn_EstadoVigente(Estado, Desde, Hasta, @Hoy) = 'Publicado'
    ORDER BY Orden, PromocionId;
END;

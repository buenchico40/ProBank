-- MKT-02: comunicados con su estado vigente, del más reciente al más antiguo (o solo uno con @Token).
CREATE PROCEDURE mkt.usp_Comunicado_Listar @Hoy DATE, @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Token, m.Codigo, m.Titulo, m.Tipo, m.Resumen, m.ContenidoHtml, m.Contacto, m.Desde, m.Hasta, m.Audiencia, m.Destacado,
           mkt.fn_EstadoVigente(m.Estado, m.Desde, m.Hasta, @Hoy) AS Estado, c.Nombre AS CreadoPor, m.FechaRegistro, u.Nombre AS PublicadoPor, m.FechaPublicacion
    FROM mkt.Comunicado m
    JOIN seg.Usuario c ON c.UsuarioId = m.CreadoPorId
    LEFT JOIN seg.Usuario u ON u.UsuarioId = m.PublicadoPorId
    WHERE @Token IS NULL OR m.Token = @Token
    ORDER BY m.ComunicadoId DESC;
END;

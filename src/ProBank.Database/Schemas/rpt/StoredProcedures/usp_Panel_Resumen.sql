-- Inicio / Panel: indicadores del usuario y del banco, gráficos y contenidos vigentes de Marketing para su rol activo.
CREATE PROCEDURE rpt.usp_Panel_Resumen
    @UsuarioId INT, @RolActivo VARCHAR(60), @Hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM coa.vw_Coactiva WHERE Estado = 'Activo') AS CoactivasActivas,
        -- Pendiente para el aprobador: tarea En revisión sin su decisión en la ronda vigente (si la última ronda terminó en
        -- rechazo y la tarea volvió a enviarse, la ronda vigente es una nueva, sin decisiones)
        (SELECT COUNT(*) FROM pry.Tarea t JOIN pry.TareaAprobador ta ON ta.TareaId = t.TareaId AND ta.UsuarioId = @UsuarioId
         CROSS APPLY (SELECT MAX(x.Ronda) AS Ronda FROM pry.Aprobacion x WHERE x.TareaId = t.TareaId) r
         WHERE t.Estado = 'EnRevision'
           AND NOT EXISTS (SELECT 1 FROM pry.Aprobacion a WHERE a.TareaId = t.TareaId AND a.AprobadorId = @UsuarioId AND a.Ronda = r.Ronda
                           AND NOT EXISTS (SELECT 1 FROM pry.Aprobacion z WHERE z.TareaId = t.TareaId AND z.Ronda = r.Ronda AND z.Decision = 'Rechazada'))) AS AprobacionesPendientes,
        (SELECT COUNT(*) FROM pry.Tarea WHERE TecnicoId = @UsuarioId AND Estado <> 'Completada') AS TareasAsignadas,
        (SELECT COUNT(*) FROM com.Renovacion WHERE Estado NOT IN ('Renovado', 'Anulado', 'Retirado')
         AND Vence >= @Hoy AND Vence <= DATEADD(DAY, DiasAviso, @Hoy)) AS RenovacionesPorVencer;

    -- COA-05: cobertura de las coactivas no anuladas
    SELECT x.Etiqueta, (SELECT COUNT(*) FROM coa.vw_Coactiva c WHERE c.Anulada = 0 AND c.Cobertura = x.Etiqueta) AS Valor
    FROM (VALUES (1, 'Total'), (2, 'Parcial'), (3, 'Sin bloqueo')) AS x (Orden, Etiqueta) ORDER BY x.Orden;

    SELECT x.Etiqueta, (SELECT COUNT(*) FROM pry.Item i WHERE i.Tipo = x.Etiqueta AND i.Estado NOT IN ('Anulado', 'Cerrado')) AS Valor
    FROM (VALUES (1, 'Proyecto'), (2, 'Mejora'), (3, 'Bug')) AS x (Orden, Etiqueta) ORDER BY x.Orden;

    SELECT PromocionId, Titulo, Hasta FROM mkt.Promocion
    WHERE Estado = 'Publicado' AND Desde <= @Hoy AND (Hasta IS NULL OR Hasta >= @Hoy) AND Audiencia IN ('Todos', @RolActivo)
    ORDER BY Orden;

    SELECT Codigo, Titulo, Tipo, ContenidoHtml, Desde, Hasta, Audiencia, Destacado FROM mkt.Comunicado
    WHERE Estado = 'Publicado' AND Desde <= @Hoy AND (Hasta IS NULL OR Hasta >= @Hoy) AND Audiencia IN ('Todos', @RolActivo)
    ORDER BY Destacado DESC, Desde DESC;
END;

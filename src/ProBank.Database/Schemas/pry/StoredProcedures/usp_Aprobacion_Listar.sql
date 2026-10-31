-- Portafolio › Aprobaciones: tareas en las que el usuario es aprobador y ya se enviaron a aprobar.
-- Estado de la solicitud vigente: Rechazada si alguien rechazó, Aprobada si aprobaron todos, Pendiente en otro caso.
-- Si la última ronda terminó en rechazo y la tarea está otra vez En revisión, se reenvió: es una ronda nueva sin decisiones.
CREATE PROCEDURE pry.usp_Aprobacion_Listar @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;
    WITH base AS (
        SELECT t.TareaId, t.Token, t.Codigo, t.Titulo, i.Codigo AS Item, u.Nombre AS SolicitadaPor, t.Estado AS EstadoTarea,
               ISNULL((SELECT MAX(a.Ronda) FROM pry.Aprobacion a WHERE a.TareaId = t.TareaId), 1) AS Ronda,
               (SELECT MAX(e.Fecha) FROM pry.TareaEvento e WHERE e.TareaId = t.TareaId AND e.Evento = N'Enviada a aprobar') AS Enviada,
               (SELECT COUNT(*) FROM pry.TareaAprobador ta WHERE ta.TareaId = t.TareaId) AS Aprobadores
        FROM pry.Tarea t
        JOIN pry.TareaAprobador yo ON yo.TareaId = t.TareaId AND yo.UsuarioId = @UsuarioId
        JOIN pry.Item i ON i.ItemId = t.ItemId
        JOIN seg.Usuario u ON u.UsuarioId = t.TecnicoId),
    ronda AS (
        SELECT b.*,
               (SELECT MAX(a.Fecha) FROM pry.Aprobacion a WHERE a.TareaId = b.TareaId AND a.Ronda = b.Ronda AND a.Decision = 'Rechazada') AS Rechazo,
               (SELECT COUNT(*) FROM pry.Aprobacion a WHERE a.TareaId = b.TareaId AND a.Ronda = b.Ronda AND a.Decision = 'Aprobada') AS Aprobadas,
               (SELECT STRING_AGG(ux.Nombre, ', ') FROM pry.Aprobacion a JOIN seg.Usuario ux ON ux.UsuarioId = a.AprobadorId
                WHERE a.TareaId = b.TareaId AND a.Ronda = b.Ronda AND a.Decision = 'Rechazada') AS RechazadaPor,
               (SELECT a.Decision FROM pry.Aprobacion a WHERE a.TareaId = b.TareaId AND a.Ronda = b.Ronda AND a.AprobadorId = @UsuarioId) AS Decision
        FROM base b WHERE b.Enviada IS NOT NULL),
    vigente AS (
        SELECT r.*, CASE WHEN r.Rechazo IS NOT NULL AND r.EstadoTarea = 'EnRevision' THEN 1 ELSE 0 END AS RondaNueva FROM ronda r)
    SELECT Token, Codigo, Titulo, Item, SolicitadaPor, Enviada,
           CASE WHEN RondaNueva = 1 THEN 'Pendiente' WHEN Rechazo IS NOT NULL THEN 'Rechazada' WHEN Aprobadas >= Aprobadores THEN 'Aprobada' ELSE 'Pendiente' END AS Estado,
           CASE WHEN RondaNueva = 1 THEN CONCAT(N'0 de ', Aprobadores) WHEN Rechazo IS NOT NULL THEN N'Rechazada por ' + RechazadaPor
                ELSE CONCAT(Aprobadas, N' de ', Aprobadores) END AS Aprobaciones,
           CASE WHEN RondaNueva = 1 THEN NULL ELSE Decision END AS MiDecision,
           EstadoTarea
    FROM vigente
    ORDER BY Enviada DESC;
END;

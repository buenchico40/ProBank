-- Detalle de la tarea para la pantalla: descripción, aprobadores con su decisión en la ronda vigente e historial.
CREATE PROCEDURE pry.usp_Tarea_Detalle @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TareaId INT = (SELECT TareaId FROM pry.Tarea WHERE Token = @Token);
    DECLARE @Ronda INT = ISNULL((SELECT MAX(Ronda) FROM pry.Aprobacion WHERE TareaId = @TareaId), 1);
    SELECT DescripcionHtml FROM pry.Tarea WHERE TareaId = @TareaId;
    SELECT u.UsuarioId, u.Usuario, u.Nombre, a.Decision, a.Fecha, a.Comentario
    FROM pry.TareaAprobador ta JOIN seg.Usuario u ON u.UsuarioId = ta.UsuarioId
    LEFT JOIN pry.Aprobacion a ON a.TareaId = ta.TareaId AND a.AprobadorId = ta.UsuarioId AND a.Ronda = @Ronda
    WHERE ta.TareaId = @TareaId ORDER BY u.Nombre;
    SELECT e.Fecha, e.Evento, e.Detalle, u.Usuario FROM pry.TareaEvento e JOIN seg.Usuario u ON u.UsuarioId = e.UsuarioId
    WHERE e.TareaId = @TareaId ORDER BY e.EventoId DESC;
END;

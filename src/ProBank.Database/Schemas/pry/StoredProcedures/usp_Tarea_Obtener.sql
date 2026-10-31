CREATE PROCEDURE pry.usp_Tarea_Obtener @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TareaId INT = (SELECT TareaId FROM pry.Tarea WHERE Token = @Token);
    SELECT TareaId, Token, ItemId, HitoId, Titulo, Area, TecnicoId, Orden, CAST(FechaInicio AS DATETIME2) AS FechaInicio,
           CAST(FechaTerminacion AS DATETIME2) AS FechaTerminacion, Avance, Estado, CreadoPorId, Codigo
    FROM pry.Tarea WHERE TareaId = @TareaId;
    SELECT UsuarioId FROM pry.TareaAprobador WHERE TareaId = @TareaId;
    -- Decisiones de la ronda vigente. Si la última ronda terminó en rechazo, la tarea empieza una ronda nueva al volver a enviarse.
    DECLARE @Ronda INT = ISNULL((SELECT MAX(Ronda) FROM pry.Aprobacion WHERE TareaId = @TareaId), 1);
    IF EXISTS (SELECT 1 FROM pry.Aprobacion WHERE TareaId = @TareaId AND Ronda = @Ronda AND Decision = 'Rechazada')
        SELECT AprobadorId, Decision, Fecha, Comentario FROM pry.Aprobacion WHERE 1 = 0;
    ELSE
        SELECT AprobadorId, Decision, Fecha, Comentario FROM pry.Aprobacion WHERE TareaId = @TareaId AND Ronda = @Ronda;
END;

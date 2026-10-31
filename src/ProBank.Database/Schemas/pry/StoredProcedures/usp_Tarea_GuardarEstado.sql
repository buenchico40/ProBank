-- Guarda el estado decidido por el dominio (enviar, aprobar, rechazar, completar) con la decisión del aprobador,
-- el evento del historial y la auditoría, en una sola transacción.
CREATE PROCEDURE pry.usp_Tarea_GuardarEstado
    @Token UNIQUEIDENTIFIER, @Estado VARCHAR(20), @Avance INT, @UsuarioId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45),
    @Evento NVARCHAR(200), @Detalle NVARCHAR(1000) = NULL,
    @DecisionAprobadorId INT = NULL, @Decision VARCHAR(10) = NULL, @Comentario NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @TareaId INT, @Anterior VARCHAR(20), @Codigo VARCHAR(20);
    SELECT @TareaId = TareaId, @Anterior = Estado, @Codigo = Codigo FROM pry.Tarea WITH (UPDLOCK) WHERE Token = @Token;
    IF @TareaId IS NULL THROW 50404, N'La tarea no existe.', 1;
    IF @Decision IS NOT NULL AND @DecisionAprobadorId = @UsuarioId AND @Anterior = 'EnRevision'
    BEGIN
        DECLARE @Ronda INT = ISNULL((SELECT MAX(Ronda) FROM pry.Aprobacion WHERE TareaId = @TareaId), 1);
        IF EXISTS (SELECT 1 FROM pry.Aprobacion WHERE TareaId = @TareaId AND Ronda = @Ronda AND Decision = 'Rechazada') SET @Ronda += 1;
        INSERT pry.Aprobacion (TareaId, AprobadorId, Ronda, Decision, Comentario) VALUES (@TareaId, @UsuarioId, @Ronda, @Decision, @Comentario);
    END;
    UPDATE pry.Tarea SET Estado = @Estado, Avance = @Avance WHERE TareaId = @TareaId;
    INSERT pry.TareaEvento (TareaId, UsuarioId, Evento, Detalle) VALUES (@TareaId, @UsuarioId, @Evento, @Detalle);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@UsuarioId, 0), @RolActivo, 'Portafolio', CONCAT(@Evento, N' · ', @Codigo), @Detalle, @Ip);
    COMMIT;
END;

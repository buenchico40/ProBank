-- DEC-03 y DEC-05: quien registró la decisión agrega o quita un aprobador (@Rol 'Aprobador') o un notificado ('Notificado').
-- Aprobadores: se agregan en borrador o en aprobación; se quitan solo mientras nadie haya firmado (ninguna versión) y siempre queda uno
-- si ya se envió. Notificados: se agregan en cualquier estado salvo anulada; se quitan solo en borrador.
-- @Orden: turno del aprobador agregado (vacío: al final). Todo queda en la trazabilidad y en la auditoría.
CREATE PROCEDURE dec.usp_Decision_CambiarParticipante
    @Token UNIQUEIDENTIFIER, @Rol VARCHAR(10), @Accion VARCHAR(10), @UsuarioId INT, @Orden INT = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Rol NOT IN ('Aprobador', 'Notificado') OR @Accion NOT IN ('Agregar', 'Quitar') THROW 50422, N'Solicitud no válida.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Creador INT;
    SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Creador = CreadoPorId FROM dec.Decision WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    IF @Creador <> @ActorId OR seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'editar') = 0
        THROW 50403, N'Solo quien registró la decisión cambia sus aprobadores y notificados.', 1;
    DECLARE @Persona NVARCHAR(220) = (SELECT Nombre + N' (' + Usuario + N')' FROM seg.Usuario WHERE UsuarioId = @UsuarioId);
    IF @Persona IS NULL THROW 50404, N'El usuario no existe.', 1;

    IF @Rol = 'Aprobador' AND @Accion = 'Agregar'
    BEGIN
        IF @Estado NOT IN (N'Borrador', N'En aprobación') THROW 50409, N'La decisión ya está cerrada: no se agregan aprobadores.', 1;
        IF @UsuarioId = @Creador THROW 50422, N'Quien registró la decisión no la aprueba.', 1;
        IF NOT EXISTS (SELECT 1 FROM seg.Usuario WHERE UsuarioId = @UsuarioId AND Estado = 'Activo') THROW 50422, N'El aprobador debe ser un usuario activo.', 1;
        IF EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @UsuarioId) THROW 50409, N'Ya es aprobador de esta decisión.', 1;
        IF @Orden IS NULL SET @Orden = 1 + ISNULL((SELECT MAX(Orden) FROM dec.DecisionAprobador WHERE DecisionId = @Id), 0);
        IF @Orden NOT BETWEEN 1 AND 99 THROW 50422, N'El orden va de 1 a 99.', 1;
        INSERT dec.DecisionAprobador (DecisionId, UsuarioId, Orden, Origen, AgregadoPorId) VALUES (@Id, @UsuarioId, @Orden, N'Agregado', @ActorId);
        SET @Persona = CONCAT(@Persona, N' · orden ', @Orden);
    END
    ELSE IF @Rol = 'Aprobador'
    BEGIN
        IF @Estado NOT IN (N'Borrador', N'En aprobación') THROW 50409, N'La decisión ya está cerrada: sus aprobadores no cambian.', 1;
        IF EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado <> N'Pendiente')
           OR EXISTS (SELECT 1 FROM dec.FirmaAnterior WHERE DecisionId = @Id)
            THROW 50409, N'Ya hay firmas en esta decisión: los aprobadores no se quitan, solo se agregan.', 1;
        IF NOT EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @UsuarioId) THROW 50404, N'No es aprobador de esta decisión.', 1;
        IF @Estado = N'En aprobación' AND (SELECT COUNT(*) FROM dec.DecisionAprobador WHERE DecisionId = @Id) = 1
            THROW 50422, N'Una decisión en aprobación necesita al menos un aprobador: agregue otro antes de quitar este.', 1;
        DELETE dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @UsuarioId;
    END
    ELSE IF @Accion = 'Agregar'
    BEGIN
        IF @Estado = N'Anulada' THROW 50409, N'La decisión está anulada.', 1;
        IF NOT EXISTS (SELECT 1 FROM seg.Usuario WHERE UsuarioId = @UsuarioId AND Estado = 'Activo') THROW 50422, N'El notificado debe ser un usuario activo.', 1;
        IF EXISTS (SELECT 1 FROM dec.DecisionNotificado WHERE DecisionId = @Id AND UsuarioId = @UsuarioId) THROW 50409, N'Ya está notificado de esta decisión.', 1;
        INSERT dec.DecisionNotificado (DecisionId, UsuarioId, AgregadoPorId) VALUES (@Id, @UsuarioId, @ActorId);
    END
    ELSE
    BEGIN
        IF @Estado <> N'Borrador' THROW 50409, N'La decisión ya se envió: los notificados no se quitan, solo se agregan.', 1;
        IF NOT EXISTS (SELECT 1 FROM dec.DecisionNotificado WHERE DecisionId = @Id AND UsuarioId = @UsuarioId) THROW 50404, N'No está notificado de esta decisión.', 1;
        DELETE dec.DecisionNotificado WHERE DecisionId = @Id AND UsuarioId = @UsuarioId;
    END;

    DECLARE @Evento NVARCHAR(40) = CONCAT(@Rol, CASE @Accion WHEN 'Agregar' THEN N' agregado' ELSE N' quitado' END);
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, @Evento, @Persona, @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', CONCAT(@Evento, N' en la decisión ', @Codigo), CONCAT(@Persona, N' · decisión ', @Estado), @Ip);
    COMMIT;
END;
